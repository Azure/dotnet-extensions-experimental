// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Middlewares;

internal class SampleMiddleware : IMessageMiddleware
{
    private readonly IMessageDelegate _messageDelegate;

    public SampleMiddleware(IMessageDelegate messageDelegate)
    {
        _messageDelegate = messageDelegate;
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context, IMessageDelegate nextHandler)
    {
        await _messageDelegate.InvokeAsync(context).ConfigureAwait(false);

        await context.UpdateVisibilityTimeoutAsync(TimeSpan.FromSeconds(10), context.MessageCancelledToken).ConfigureAwait(false);
        await nextHandler.InvokeAsync(context).ConfigureAwait(false);
    }
}
