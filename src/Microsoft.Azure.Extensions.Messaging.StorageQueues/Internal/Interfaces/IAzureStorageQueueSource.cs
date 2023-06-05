// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;

/// <summary>
/// Additional capabilities of an Azure Storage Queue beyond <see cref="IMessageSource"/>.
/// </summary>
internal interface IAzureStorageQueueSource : IMessageSource
{
    /// <summary>
    /// Deletes the message from the queue.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    ValueTask DeleteAsync(MessageContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Postpones the message processing by the provided <paramref name="delay"/>.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="delay">Delay <see cref="TimeSpan"/>.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    ValueTask PostponeAsync(MessageContext context, TimeSpan delay, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the visibility timeout for the message in the queue.
    /// </summary>
    /// <param name="context"><see cref="MessageContext"/>.</param>
    /// <param name="newVisibilityTimeout">The new <see cref="TimeSpan"/> representing updated visibility timeout.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="ValueTask"/>.</returns>
    ValueTask UpdateVisibilityTimeoutAsync(MessageContext context, TimeSpan newVisibilityTimeout, CancellationToken cancellationToken);
}
