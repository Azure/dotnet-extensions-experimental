// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Reads message from the underlying Azure Storage Queue.
/// </summary>
/// <remarks>
/// For more information, see <see href="https://docs.microsoft.com/rest/api/storageservices/get-messages">Get messages</see>.
/// </remarks>
public class AzureStorageQueueSource : IAzureStorageQueueSource
{
    private readonly QueueClient _queueClient;
    private readonly AzureStorageQueueReadOptions _readOptions;
    private readonly Func<IFeatureCollection> _featureCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageQueueSource"/> class.
    /// </summary>
    /// <param name="queueClient">The queue client.</param>
    /// <param name="readOptions">The options for reading message from the Azure Storage Queue.</param>
    /// <param name="featureCreator">The function to obtain the <see cref="IFeatureCollection"/>.</param>
    public AzureStorageQueueSource(QueueClient queueClient, AzureStorageQueueReadOptions readOptions, Func<IFeatureCollection> featureCreator)
    {
        _queueClient = Throw.IfNull(queueClient);
        _readOptions = Throw.IfNull(readOptions);
        _featureCreator = Throw.IfNull(featureCreator);
    }

    /// <inheritdoc/>
    public ValueTask DeleteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);
        if (context is not AzureStorageQueueMessageContext)
        {
            Throw.InvalidOperationException(ExceptionMessages.InvalidAzureStorageQueueMessageContext);
        }

        return context.MarkCompleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask PostponeAsync(MessageContext context, TimeSpan delay, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);
        if (context is AzureStorageQueueMessageContext azureStorageQueueMessageContext)
        {
            await azureStorageQueueMessageContext.PostponeAsync(delay, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Throw.InvalidOperationException(ExceptionMessages.InvalidAzureStorageQueueMessageContext);
        }
    }

    /// <inheritdoc/>
    public async virtual ValueTask<MessageContext> ReadAsync(CancellationToken cancellationToken)
    {
        Response<QueueMessage>? readMessage = await _queueClient.ReceiveMessageAsync(_readOptions.VisibilityTimeout, cancellationToken).ConfigureAwait(false);
        QueueMessage? queueMessage = readMessage?.Value;

        if (queueMessage == null)
        {
            Throw.InvalidOperationException(ExceptionMessages.NoQueueMessageRetrieved);
        }

        // A more efficient implementation will use the Features from the pool.
        IFeatureCollection features = _featureCreator();
        AzureStorageQueueMessageContext context = new(_queueClient, queueMessage!, features, queueMessage!.Body.ToMemory());

        context.SetAzureStorageQueueClient(_queueClient);
        context.SetAzureStorageQueueMessage(queueMessage);
        context.SetAzureStorageQueueSource(this);
        context.SetMessagePostponeFeature(context);
        return context;
    }

    /// <inheritdoc/>
    public void Release(MessageContext context)
    {
        _ = Throw.IfNull(context);

        // A more efficient implementation will return the Features to the pool.
    }

    /// <inheritdoc/>
    public async ValueTask UpdateVisibilityTimeoutAsync(MessageContext context, TimeSpan newVisibilityTimeout, CancellationToken cancellationToken)
    {
        await PostponeAsync(context, newVisibilityTimeout, cancellationToken).ConfigureAwait(false);
        context.SetVisibilityDelay(newVisibilityTimeout);
    }
}
