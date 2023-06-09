// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;

internal class SingleDestinationMessageDelegate
{
    private readonly IMessageDestination _messageDestination;

    public SingleDestinationMessageDelegate(IMessageDestination messageDestination)
    {
        _messageDestination = Throw.IfNull(messageDestination);
    }

    /// <summary>
    /// The <see cref="MessageDelegate"/> implementation which writes message to the provided <see cref="IMessageDestination"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public async ValueTask InvokeAsync(MessageContext context)
    {
        context.SetDestinationPayload(context.SourcePayload);
        await _messageDestination.WriteAsync(context).ConfigureAwait(false);
    }
}
