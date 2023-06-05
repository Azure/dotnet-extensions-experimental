// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Extensions.Cosmos.DocumentStorage;

/// <summary>
/// The interface for Cosmos DB encryption provider.
/// </summary>
public interface ICosmosEncryptionProvider
{
    /// <summary>
    /// Configures encryption for container.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Task"/> representing result of asynchronous operation.</returns>
    Task ConfigureEncryptionForContainerAsync(Container container, CancellationToken cancellationToken);

    /// <summary>
    /// Get the request options with configured encryption.
    /// </summary>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a container schema.
    /// Operation results from database will be mapped to instance of this type.
    /// </typeparam>
    /// <typeparam name="TOptions">The type of cosmos db request options.</typeparam>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="itemRequestOptions">The cosmos request options.</param>
    /// <param name="cosmosEndpointUri">The cosmos endpoint uri.</param>
    /// <param name="document">The document to be encrypted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see cref="Task"/> representing result of asynchronous operation wrapping updated request options.</returns>
    Task<TOptions?> GetEncryptionItemRequestOptionsAsync<TDocument, TOptions>(
        System.Cloud.DocumentDb.RequestOptions requestOptions,
        TOptions? itemRequestOptions,
        Uri cosmosEndpointUri,
        TDocument document,
        CancellationToken cancellationToken)
        where TDocument : notnull
        where TOptions : Azure.Cosmos.RequestOptions;

    /// <summary>
    /// Converts queryable to encrypted stream iterator.
    /// </summary>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a container schema.
    /// Operation results from database will be mapped to instance of this type.
    /// </typeparam>
    /// <param name="container">The cosmos container.</param>
    /// <param name="queryable">The queryable to convert.</param>
    /// <returns>The feed iterator.</returns>
    FeedIterator ToEncryptionStreamIterator<TDocument>(Container container, IQueryable<TDocument> queryable);
}
