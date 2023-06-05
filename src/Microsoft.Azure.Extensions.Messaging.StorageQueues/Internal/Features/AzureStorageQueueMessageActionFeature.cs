// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;

/// <summary>
/// <see cref="IAzureStorageQueueSource"/> based implementation for updating the message processing state in Azure Storage Queues.
/// </summary>
internal sealed class AzureStorageQueueMessageActionFeature : IMessageCompleteActionFeature, IMessagePostponeActionFeature
{
    private readonly MessageContext _messageContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageQueueMessageActionFeature"/> class.
    /// </summary>
    /// <param name="messageContext"><see cref="MessageContext"/>.</param>
    public AzureStorageQueueMessageActionFeature(MessageContext messageContext)
    {
        _messageContext = messageContext;
    }

    /// <inheritdoc/>
    public ValueTask MarkCompleteAsync(CancellationToken cancellationToken)
    {
        _ = _messageContext.TryGetAzureStorageQueueSource(out IAzureStorageQueueSource? queueSource);
        _ = Throw.IfNull(queueSource);

        return queueSource.DeleteAsync(_messageContext, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask PostponeAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        _ = _messageContext.TryGetAzureStorageQueueSource(out IAzureStorageQueueSource? queueSource);
        _ = Throw.IfNull(queueSource);

        return queueSource.PostponeAsync(_messageContext, delay, cancellationToken);
    }
}
