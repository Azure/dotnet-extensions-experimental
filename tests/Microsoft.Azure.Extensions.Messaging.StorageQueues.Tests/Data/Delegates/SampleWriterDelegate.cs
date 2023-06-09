// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Delegates;

internal class SampleWriterDelegate
{
    private readonly MessageDelegate _messageDelegate;
    private readonly AzureStorageQueueDestination _messageDestination;

    public SampleWriterDelegate(MessageDelegate messageDelegate, AzureStorageQueueDestination messageDestination)
    {
        _messageDelegate = messageDelegate;
        _messageDestination = messageDestination;
    }

    /// <summary>
    /// The <see cref="MessageDelegate"/> implementation.
    /// </summary>
    public async ValueTask InvokeAsync(MessageContext context)
    {
        await _messageDelegate.Invoke(context).ConfigureAwait(false);
        context.SetDestinationPayload(context.SourcePayload);

        await _messageDestination.WriteAsync(context).ConfigureAwait(false);
        context.SetAzureStorageQueueMessageProcessingState(AzureStorageQueueMessageProcessingState.Completed);
    }
}
