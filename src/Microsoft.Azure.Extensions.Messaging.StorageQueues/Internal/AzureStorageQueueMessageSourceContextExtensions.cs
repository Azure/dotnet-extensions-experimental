// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
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
    internal static bool TryGetAzureStorageQueueSource(this MessageContext context, [NotNullWhen(true)] out IAzureStorageQueueSource? queueSource)
    {
        _ = Throw.IfNull(context);

        queueSource = context.SourceFeatures?.Get<IAzureStorageQueueSource>();
        return queueSource != null;
    }

    /// <summary>
    /// Try to obtain <see cref="QueueClient"/> from the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueClient">The <see langword="out"/> to store the <see cref="QueueClient"/>.</param>
    /// <returns><see cref="bool"/> and if <see langword="true"/>, a corresponding <see cref="QueueClient"/>.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    internal static bool TryGetAzureStorageQueueClient(this MessageContext context, [NotNullWhen(true)] out QueueClient? queueClient)
    {
        _ = Throw.IfNull(context);

        queueClient = context.SourceFeatures?.Get<QueueClient>();
        return queueClient != null;
    }

    /// <summary>
    /// Sets <see cref="QueueClient"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="client"><see cref="QueueClient"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    internal static void SetAzureStorageQueueClient(this MessageContext context, QueueClient client)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(client);

        context.AddSourceFeature(client);
    }

    /// <summary>
    /// Sets <see cref="QueueMessage"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueMessage"><see cref="QueueMessage"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    internal static void SetAzureStorageQueueMessage(this MessageContext context, QueueMessage queueMessage)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(queueMessage);

        context.AddSourceFeature(queueMessage);
    }

    /// <summary>
    /// Sets <see cref="IAzureStorageQueueSource"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="queueSource"><see cref="IAzureStorageQueueSource"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the arguments is null.</exception>
    internal static void SetAzureStorageQueueSource(this MessageContext context, IAzureStorageQueueSource queueSource)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(queueSource);

        context.AddSourceFeature(queueSource);
    }
}
