// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;
internal class PassThroughMiddleware : IMessageMiddleware
{
    /// <inheritdoc/>
    public ValueTask InvokeAsync(MessageContext context, MessageDelegate nextHandler) => nextHandler.Invoke(context);
}
