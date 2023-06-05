// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;

internal class SingleDestinationMessageDelegate : IMessageDelegate
{
    private readonly IMessageDestination _messageDestination;

    public SingleDestinationMessageDelegate(IMessageDestination messageDestination)
    {
        _messageDestination = Throw.IfNull(messageDestination);
    }

    /// <summary>
    /// Read message.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public async ValueTask InvokeAsync(MessageContext context)
    {
        ReadOnlyMemory<byte> payload = context.GetSourcePayload();
        context.SetDestinationPayload(payload);
        await _messageDestination.WriteAsync(context);
    }
}
