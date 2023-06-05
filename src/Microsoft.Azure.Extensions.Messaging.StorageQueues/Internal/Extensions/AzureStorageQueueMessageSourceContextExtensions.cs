// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;

/// <summary>
/// Azure Storage Queue Extensions for <see cref="MessageContext"/>.
/// </summary>
internal static class AzureStorageQueueMessageSourceContextExtensions
{
    /// <summary>
    /// Try to obtain <see cref="IAzureStorageQueueSource"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueSource">The <see langword="out"/> to store the <see cref="IAzureStorageQueueSource"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="IAzureStorageQueueSource"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by Try pattern.")]
    internal static bool TryGetAzureStorageQueueSource(this MessageContext context, out IAzureStorageQueueSource? queueSource)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? features);
        _ = Throw.IfNull(features);

        try
        {
            queueSource = features.Get<IAzureStorageQueueSource>();
            return queueSource != null;
        }
        catch (Exception)
        {
            queueSource = null;
            return false;
        }
    }

    /// <summary>
    /// Try to obtain <see cref="QueueClient"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueClient">The <see langword="out"/> to store the <see cref="QueueClient"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="QueueClient"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled by Try pattern.")]
    internal static bool TryGetAzureStorageQueueClient(this MessageContext context, out QueueClient? queueClient)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? features);
        _ = Throw.IfNull(features);

        try
        {
            queueClient = features.Get<QueueClient>();
            return queueClient != null;
        }
        catch (Exception)
        {
            queueClient = null;
            return false;
        }
    }

    /// <summary>
    /// Sets <see cref="QueueClient"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="client"><see cref="QueueClient"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    internal static void SetAzureStorageQueueClient(this MessageContext context, QueueClient client)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        sourceFeatures ??= new FeatureCollection();

        sourceFeatures.Set(client);
        context.SetMessageSourceFeatures(sourceFeatures);
    }

    /// <summary>
    /// Sets <see cref="QueueMessage"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueMessage"><see cref="QueueMessage"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    internal static void SetAzureStorageQueueMessage(this MessageContext context, QueueMessage queueMessage)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        sourceFeatures ??= new FeatureCollection();

        sourceFeatures.Set(queueMessage);
        context.SetMessageSourceFeatures(sourceFeatures);
        context.SetSourcePayload(queueMessage.Body.ToMemory());
    }

    /// <summary>
    /// Sets <see cref="IAzureStorageQueueSource"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueSource"><see cref="IAzureStorageQueueSource"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    internal static void SetAzureStorageQueueSource(this MessageContext context, IAzureStorageQueueSource queueSource)
    {
        _ = context.TryGetMessageSourceFeatures(out IFeatureCollection? sourceFeatures);
        sourceFeatures ??= new FeatureCollection();

        sourceFeatures.Set(queueSource);
        context.SetMessageSourceFeatures(sourceFeatures);
    }
}
