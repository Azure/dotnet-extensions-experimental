// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Provides extension methods for <see cref="MessageContext"/> to add support for Azure Storage Queue based <see cref="IMessageDestination"/>.
/// </summary>
public static class AzureStorageQueueMessageDestinationContextExtensions
{
    /// <summary>
    /// Sets the <see cref="AzureStorageQueueWriteOptions"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="writeOptions">The options for writing message to the Azure Storage Queue.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetAzureStorageQueueWriteOptions(this MessageContext context, AzureStorageQueueWriteOptions writeOptions)
    {
        _ = Throw.IfNull(context);
        context.AddDestinationFeature<AzureStorageQueueWriteOptions?>(writeOptions);
    }

    /// <summary>
    /// Gets the <see cref="AzureStorageQueueWriteOptions"/> from <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="writeOptions">The optional options for writing message to the Azure Storage Queue.</param>
    /// <returns><see cref="bool"/> value and if <see langword="true"/>, a corresponding options for writing to the Azure Storage Queue.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    public static bool TryGetAzureStorageQueueWriteOptions(this MessageContext context, [NotNullWhen(true)] out AzureStorageQueueWriteOptions? writeOptions)
    {
        _ = Throw.IfNull(context);

        writeOptions = context.DestinationFeatures?.Get<AzureStorageQueueWriteOptions?>();
        return writeOptions.HasValue;
    }
}
