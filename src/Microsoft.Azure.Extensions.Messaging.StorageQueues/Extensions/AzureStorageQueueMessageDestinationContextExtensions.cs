// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues;

/// <summary>
/// Extension methods for <see cref="MessageContext"/>.
/// </summary>
public static class AzureStorageQueueMessageDestinationContextExtensions
{
    /// <summary>
    /// Sets the <see cref="AzureStorageQueueWriteOptions"/> in the <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="writeOptions"><see cref="AzureStorageQueueWriteOptions"/>.</param>
    /// <exception cref="ArgumentNullException">If any of the parameters is null.</exception>
    public static void SetWriteOptions(this MessageContext context, AzureStorageQueueWriteOptions writeOptions)
    {
        _ = Throw.IfNull(writeOptions);

        _ = context.TryGetMessageDestinationFeatures(out IFeatureCollection? destinationFeatures);
        destinationFeatures ??= new FeatureCollection();

        destinationFeatures.Set(writeOptions);
        context.SetMessageDestinationFeatures(destinationFeatures);
    }

    /// <summary>
    /// Gets the <see cref="AzureStorageQueueWriteOptions"/> from <see cref="MessageContext"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="writeOptions"><see cref="AzureStorageQueueWriteOptions"/>.</param>
    /// <returns><see cref="bool"/> value indicating if writeOptions is populated or not.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="context"/> is null.</exception>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "TryGet Pattern.")]
    public static bool TryGetWriteOptions(this MessageContext context, out AzureStorageQueueWriteOptions? writeOptions)
    {
        _ = context.TryGetMessageDestinationFeatures(out IFeatureCollection? features);
        _ = Throw.IfNull(features);

        try
        {
            writeOptions = features.Get<AzureStorageQueueWriteOptions>();
            return writeOptions.HasValue;
        }
        catch (Exception)
        {
            writeOptions = null;
            return false;
        }
    }
}
