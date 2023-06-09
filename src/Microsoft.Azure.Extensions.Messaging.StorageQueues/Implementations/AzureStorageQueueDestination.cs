// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Writes message to Azure Storage Queue.
/// </summary>
/// <remarks>
/// For more information, refer to <see href="https://docs.microsoft.com/en-us/rest/api/storageservices/put-message">Put message</see>.
/// </remarks>
public class AzureStorageQueueDestination : IMessageDestination
{
    private readonly QueueClient _queueClient;
    private readonly AzureStorageQueueWriteOptions _writerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageQueueDestination"/> class.
    /// </summary>
    /// <param name="queueClient">The queue client.</param>
    /// <param name="writerOptions">The options for writing message to the Azure Storage Queue.</param>
    public AzureStorageQueueDestination(QueueClient queueClient, AzureStorageQueueWriteOptions writerOptions)
    {
        _queueClient = Throw.IfNull(queueClient);
        _writerOptions = Throw.IfNull(writerOptions);
    }

    /// <inheritdoc/>
    public virtual async ValueTask WriteAsync(MessageContext context)
    {
        _ = Throw.IfNullOrMemberNull(context, context?.DestinationPayload);

        _ = context.TryGetAzureStorageQueueWriteOptions(out AzureStorageQueueWriteOptions? writeOptions);
        TimeSpan? visibilityTimeoutToUse = writeOptions?.VisibilityTimeout ?? _writerOptions.VisibilityTimeout;
        TimeSpan? timeToLiveToUse = writeOptions?.TimeToLive ?? _writerOptions.TimeToLive;

        Response<SendReceipt> writeResponse = await _queueClient.SendMessageAsync(new BinaryData(context.DestinationPayload!.Value),
                                                                                  visibilityTimeoutToUse,
                                                                                  timeToLiveToUse,
                                                                                  context.MessageCancelledToken).ConfigureAwait(false);
        SendReceipt sendReceipt = writeResponse.Value;
        context.Features.Set(sendReceipt);
    }
}
