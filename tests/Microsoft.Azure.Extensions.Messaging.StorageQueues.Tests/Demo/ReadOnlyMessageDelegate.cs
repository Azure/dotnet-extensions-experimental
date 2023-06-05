// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;

internal class ReadOnlyMessageDelegate : IMessageDelegate
{
    /// <summary>
    /// Read message.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public ValueTask InvokeAsync(MessageContext context)
    {
        ReadOnlyMemory<byte> payload = context.GetSourcePayload();
        return default;
    }
}
