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
/// Azure Storage Queue Extensions for <see cref="MessageContext"/>.
/// </summary>
public static class AzureStorageQueueMessageSourceContextExtensions
{
    /// <summary>
    /// Try to obtain <see cref="AzureStorageQueueMessageProcessingState"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="state">The <see langword="out"/> to store the <see cref="AzureStorageQueueMessageProcessingState"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="AzureStorageQueueMessageProcessingState"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by Try pattern.")]
    public static bool TryGetAzureStorageQueueMessageProcessingState(this MessageContext context, out AzureStorageQueueMessageProcessingState state)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);

        try
        {
            state = context.Features.Get<AzureStorageQueueMessageProcessingState>();
            return true;
        }
        catch (Exception)
        {
            state = AzureStorageQueueMessageProcessingState.Processing;
            return false;
        }
    }

    /// <summary>
    /// Try to obtain <see cref="QueueMessage"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueMessage">The <see langword="out"/> to store the <see cref="QueueMessage"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="QueueMessage"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by Try pattern.")]
    public static bool TryGetAzureStorageQueueMessage(this MessageContext context, out QueueMessage? queueMessage)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? features);
        _ = Throw.IfNull(features);

        try
        {
            queueMessage = features.Get<QueueMessage>();
            return queueMessage != null;
        }
        catch (Exception)
        {
            queueMessage = null;
            return false;
        }
    }

    /// <summary>
    /// Try to obtain <see cref="TimeSpan"/> representing visibility timeout from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="visibilityTimeout">The <see langword="out"/> to store the <see cref="TimeSpan"/> representing visibility timeout.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="TimeSpan"/> representing visibility timeout.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by Try pattern.")]
    public static bool TryGetVisibilityTimeout(this MessageContext context, out TimeSpan? visibilityTimeout)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? features);
        _ = Throw.IfNull(features);

        try
        {
            IMessageVisibilityDelayFeature? feature = features.Get<IMessageVisibilityDelayFeature>();
            visibilityTimeout = feature?.VisibilityDelay;
            return visibilityTimeout != null;
        }
        catch (Exception)
        {
            visibilityTimeout = null;
            return false;
        }
    }

    /// <summary>
    /// Sets <see cref="AzureStorageQueueMessageProcessingState"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="state"><see cref="AzureStorageQueueMessageProcessingState"/>.</param>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    public static void SetAzureStorageQueueMessageProcessingState(this MessageContext context, AzureStorageQueueMessageProcessingState state)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(context.Features);

        context.Features.Set(state);
    }

    /// <summary>
    /// Updates visibility timeout for the message in the queue.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="newVisibilityTimeout">The updated message visibility timeout.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    public static async ValueTask UpdateVisibilityTimeoutAsync(this MessageContext context, TimeSpan newVisibilityTimeout, CancellationToken cancellationToken)
    {
        _ = context.TryGetAzureStorageQueueSource(out IAzureStorageQueueSource? queueSource);
        _ = Throw.IfNull(queueSource);

        await queueSource.UpdateVisibilityTimeoutAsync(context, newVisibilityTimeout, cancellationToken).ConfigureAwait(false);
    }
}
