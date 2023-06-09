// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;

/// <summary>
/// Azure Storage Queue implementation for <see cref="MessageContext"/>.
/// </summary>
internal sealed class AzureStorageQueueMessageContext : MessageContext, IMessagePostponeFeature
{
    private readonly QueueClient _queueClient;
    private readonly QueueMessage _queueMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageQueueMessageContext"/> class.
    /// </summary>
    /// <param name="queueClient">The queue client from where message has been retrieved.</param>
    /// <param name="queueMessage">The queue message.</param>
    /// <param name="features">The features.</param>
    /// <param name="sourcePayload">The source payload.</param>
    public AzureStorageQueueMessageContext(QueueClient queueClient, QueueMessage queueMessage, IFeatureCollection features, ReadOnlyMemory<byte> sourcePayload)
        : base(features, sourcePayload)
    {
        _queueClient = Throw.IfNull(queueClient);
        _queueMessage = Throw.IfNull(queueMessage);
    }

    /// <inheritdoc/>
    public override ValueTask MarkCompleteAsync(CancellationToken cancellationToken) =>
        new(_queueClient.DeleteMessageAsync(_queueMessage.MessageId, _queueMessage.PopReceipt, cancellationToken));

    /// <inheritdoc/>
    public ValueTask PostponeAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        new(_queueClient.UpdateMessageAsync(_queueMessage.MessageId, _queueMessage.PopReceipt, _queueMessage.Body, delay, cancellationToken));
}
