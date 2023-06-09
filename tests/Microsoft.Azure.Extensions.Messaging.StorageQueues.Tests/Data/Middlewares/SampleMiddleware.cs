// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Middlewares;

internal class SampleMiddleware : IMessageMiddleware
{
    private readonly MessageDelegate _messageDelegate;

    public SampleMiddleware(MessageDelegate messageDelegate)
    {
        _messageDelegate = Throw.IfNull(messageDelegate);
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler)
    {
        await _messageDelegate.Invoke(context).ConfigureAwait(false);

        await context.UpdateAzureStorageQueueVisibilityTimeoutAsync(TimeSpan.FromSeconds(10), context.MessageCancelledToken).ConfigureAwait(false);

        _ = Throw.IfNull(nextHandler);
        await nextHandler.Invoke(context).ConfigureAwait(false);
    }
}
