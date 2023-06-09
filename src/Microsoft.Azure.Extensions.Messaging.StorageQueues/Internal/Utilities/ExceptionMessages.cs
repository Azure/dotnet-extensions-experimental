// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using Azure.Storage.Queues.Models;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;

/// <summary>
/// Defines messages for exceptions thrown by the library.
/// </summary>
internal sealed class ExceptionMessages
{
    public const string InvalidAzureStorageQueueMessageContext = $"The provided {nameof(MessageContext)} is not a {nameof(AzureStorageQueueMessageContext)}.";
    public const string NoQueueMessageRetrieved = $"No {nameof(QueueMessage)} obtained while reading message from the queue.";
    public const string NoQueueSourceOnMessageContext = $"No {nameof(IAzureStorageQueueSource)} is assigned to the provided {nameof(MessageContext)}.";
}
