// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Represents the state of the message processing in Azure Storage Queue.
/// </summary>
public enum AzureStorageQueueMessageProcessingState
{
    /// <summary>
    /// Message processing is in progress.
    /// </summary>
    Processing = 0,

    /// <summary>
    /// Message processing is completed.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Message processing is aborted.
    /// </summary>
    Aborted = 2,

    /// <summary>
    /// Message processing is postponed.
    /// </summary>
    Postponed = 3,
}
