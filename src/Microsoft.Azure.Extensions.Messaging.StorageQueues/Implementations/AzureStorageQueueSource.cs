// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading;
using System.Threading.Tasks;
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
    internal const string DoesNotHaveQueueMessageInContext = $"Does not have {nameof(QueueMessage)} in the {nameof(MessageContext)}.";

    private readonly QueueClient _queueClient;
    private readonly AzureStorageQueueReadOptions _readOptions;
    private readonly Func<IFeatureCollection> _featureCreator;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageQueueSource"/> class.
    /// </summary>
    /// <param name="queueClient"><see cref="QueueClient"/>.</param>
    /// <param name="readOptions"><see cref="AzureStorageQueueReadOptions"/>.</param>
    /// <param name="featureCreator">A function to create a new <see cref="IFeatureCollection"/>.</param>
    public AzureStorageQueueSource(QueueClient queueClient, AzureStorageQueueReadOptions readOptions, Func<IFeatureCollection> featureCreator)
    {
        _queueClient = Throw.IfNull(queueClient);
        _readOptions = Throw.IfNull(readOptions);
        _featureCreator = Throw.IfNull(featureCreator);
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(MessageContext context, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        _ = context.TryGetAzureStorageQueueMessage(out QueueMessage? queueMessage);
        _ = Throw.IfNull(queueMessage);

        _ = context.TryGetAzureStorageQueueClient(out QueueClient? queueClient);
        _ = Throw.IfNull(queueClient);

        _ = await queueClient.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask PostponeAsync(MessageContext context, TimeSpan delay, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(context);

        _ = context.TryGetAzureStorageQueueMessage(out QueueMessage? queueMessage);
        _ = Throw.IfNull(queueMessage);

        _ = context.TryGetAzureStorageQueueClient(out QueueClient? queueClient);
        _ = Throw.IfNull(queueClient);

        _ = await queueClient.UpdateMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, queueMessage.Body, delay, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async virtual ValueTask<MessageContext> ReadAsync(CancellationToken cancellationToken)
    {
        var readMessage = await _queueClient.ReceiveMessageAsync(_readOptions.VisibilityTimeout, cancellationToken).ConfigureAwait(false);
        var queueMessage = readMessage.Value;

        // A more efficient implementation will use the Features from the pool.
        IFeatureCollection features = _featureCreator();
        MessageContext context = new(features);

        IFeatureCollection sourceFeatures = _featureCreator();
        context.SetMessageSourceFeatures(sourceFeatures);

        context.SetAzureStorageQueueClient(_queueClient);
        context.SetAzureStorageQueueMessage(queueMessage);
        context.SetAzureStorageQueueSource(this);

        AzureStorageQueueMessageActionFeature provider = new(context);
        context.SetMessageCompleteActionFeature(provider);
        context.SetMessagePostponeActionFeature(provider);

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
