// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;

internal class MultipleDestinationMessageDelegate
{
    private readonly List<IMessageDestination> _messageDestinations;

    public MultipleDestinationMessageDelegate(List<IMessageDestination> messageDestinations)
    {
        _messageDestinations = Throw.IfNull(messageDestinations);
    }

    /// <summary>
    /// The <see cref="MessageDelegate"/> implementation which writes message to <see cref="List{IMessageDestination}"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public async ValueTask InvokeAsync(MessageContext context)
    {
        context.SetDestinationPayload(context.SourcePayload);
        foreach (IMessageDestination messageDestination in _messageDestinations)
        {
            await messageDestination.WriteAsync(context);
        }
    }
}
