// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Options for writing messages to the Azure Storage Queue.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not required")]
public readonly struct AzureStorageQueueWriteOptions
{
    /// <summary>
    /// Gets the visibility timeout.
    /// </summary>
    public readonly TimeSpan? VisibilityTimeout { get; }

    /// <summary>
    /// Gets the time to live.
    /// </summary>
    public readonly TimeSpan? TimeToLive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageQueueWriteOptions"/> struct.
    /// </summary>
    /// <param name="visibilityTimeout">Visibility Timeout.</param>
    /// <param name="timeToLive">Time to live.</param>
    public AzureStorageQueueWriteOptions(TimeSpan? visibilityTimeout, TimeSpan? timeToLive)
    {
        VisibilityTimeout = visibilityTimeout;
        TimeToLive = timeToLive;
    }
}
