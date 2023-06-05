// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Delegates;

internal class SampleWriterDelegate : IMessageDelegate
{
    private readonly IMessageDelegate _messageDelegate;
    private readonly AzureStorageQueueDestination _messageDestination;

    public SampleWriterDelegate(IMessageDelegate messageDelegate, AzureStorageQueueDestination messageDestination)
    {
        _messageDelegate = messageDelegate;
        _messageDestination = messageDestination;
    }

    /// <inheritdoc/>
    public async ValueTask InvokeAsync(MessageContext context)
    {
        await _messageDelegate.InvokeAsync(context);

        context.SetMessageDestinationFeatures(new FeatureCollection());
        context.SetDestinationPayload(context.GetSourcePayload());

        await _messageDestination.WriteAsync(context);
        context.SetAzureStorageQueueMessageProcessingState(AzureStorageQueueMessageProcessingState.Completed);
    }
}
