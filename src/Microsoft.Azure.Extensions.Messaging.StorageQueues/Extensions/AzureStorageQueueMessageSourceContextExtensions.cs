// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Provides extension methods for the <see cref="MessageContext"/> to add support for storing Azure Storage Queue properties with the message.
/// </summary>
public static class AzureStorageQueueMessageSourceContextExtensions
{
    /// <summary>
    /// Sets <see cref="AzureStorageQueueMessageProcessingState"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="state">The processing state of the message.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    public static void SetAzureStorageQueueMessageProcessingState(this MessageContext context, AzureStorageQueueMessageProcessingState state)
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);
        context.Features.Set<AzureStorageQueueMessageProcessingState?>(state);
    }

    /// <summary>
    /// Try to obtain <see cref="AzureStorageQueueMessageProcessingState"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="state">The optional processing state.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="AzureStorageQueueMessageProcessingState"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    public static bool TryGetAzureStorageQueueMessageProcessingState(this MessageContext context, [NotNullWhen(true)] out AzureStorageQueueMessageProcessingState? state)
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);

        state = context.Features.Get<AzureStorageQueueMessageProcessingState?>();
        return state.HasValue;
    }

    /// <summary>
    /// Try to obtain <see cref="QueueMessage"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="queueMessage">The optional queue message properties.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="QueueMessage"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    public static bool TryGetAzureStorageQueueMessage(this MessageContext context, [NotNullWhen(true)] out QueueMessage? queueMessage)
    {
        _ = Throw.IfNull(context);

        queueMessage = context.SourceFeatures?.Get<QueueMessage>();
        return queueMessage != null;
    }

    /// <summary>
    /// Try to obtain <see cref="TimeSpan"/> representing visibility timeout from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="visibilityTimeout">The optional time span representing the visibility timeout for the message <paramref name="context"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="TimeSpan"/> representing the visibility timeout for the message <paramref name="context"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    public static bool TryGetAzureStorageQueueVisibilityTimeout(this MessageContext context, [NotNullWhen(true)] out TimeSpan? visibilityTimeout)
    {
        _ = Throw.IfNullOrMemberNull(context, context?.Features);

        visibilityTimeout = context.Features.Get<IMessageVisibilityDelayFeature>()?.VisibilityDelay;
        return visibilityTimeout.HasValue;
    }

    /// <summary>
    /// Updates visibility timeout for the message in the queue.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="newVisibilityTimeout">The updated message visibility timeout.</param>
    /// <param name="cancellationToken">The cancellation token for updating visibility timeout operation.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    /// <exception cref="InvalidOperationException">If no <see cref="IAzureStorageQueueSource"/> is assigned to the provided <paramref name="context"/>.</exception>
    public static ValueTask UpdateAzureStorageQueueVisibilityTimeoutAsync(this MessageContext context, TimeSpan newVisibilityTimeout, CancellationToken cancellationToken)
    {
        _ = context.TryGetAzureStorageQueueSource(out IAzureStorageQueueSource? queueSource);
        if (queueSource == null)
        {
            Throw.InvalidOperationException(ExceptionMessages.NoQueueSourceOnMessageContext);
        }

        return queueSource.UpdateVisibilityTimeoutAsync(context, newVisibilityTimeout, cancellationToken);
    }
}
