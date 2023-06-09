// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Options for reading messages from the Azure Storage Queue.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not required")]
public readonly struct AzureStorageQueueReadOptions
{
    /// <summary>
    /// Gets the visibility timeout indicating when the message would be next available for another <see cref="MessageConsumer"/> to process.
    /// </summary>
    public readonly TimeSpan? VisibilityTimeout { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageQueueReadOptions"/> struct.
    /// </summary>
    /// <param name="visibilityTimeout"><see cref="VisibilityTimeout"/>.</param>
    public AzureStorageQueueReadOptions(TimeSpan visibilityTimeout)
    {
        VisibilityTimeout = visibilityTimeout;
    }
}
