// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;

internal class ReadOnlyMessageDelegate
{
    /// <summary>
    /// The <see cref="MessageDelegate"/> implementation which reads message from the <paramref name="context"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public static ValueTask InvokeAsync(MessageContext context)
    {
        _ = context?.SourcePayload;
        return default;
    }
}
