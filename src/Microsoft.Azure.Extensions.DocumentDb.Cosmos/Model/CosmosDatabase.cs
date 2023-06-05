// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using RequestOptions = System.Cloud.DocumentDb.RequestOptions;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Database instance of Cosmos DB.
/// </summary>
internal readonly struct CosmosDatabase
{
    private readonly ConcurrentDictionary<string, Container> _containersNameDict = new();

    internal CosmosDatabase(
        Azure.Cosmos.Database database,
        ICosmosEncryptionProvider? cosmosEncryptionProvider,
        CosmosDatabaseConfiguration configuration)
    {
        Database = database;
        CosmosEncryptionProvider = cosmosEncryptionProvider;
        Configuration = configuration;
    }

    /// <summary>
    /// Gets the cosmos database instance.
    /// </summary>
    public Azure.Cosmos.Database Database { get; }

    /// <summary>
    /// Gets cosmos application level encryption.
    /// </summary>
    public ICosmosEncryptionProvider? CosmosEncryptionProvider { get; }

    /// <summary>
    /// Gets the database configuration.
    /// </summary>
    public CosmosDatabaseConfiguration Configuration { get; }

    /// <summary>
    /// Get container by container name.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <returns>Container instance.</returns>
    public Container GetContainer(string containerName)
        => _containersNameDict.GetOrAdd(
            containerName,
            static (name, t) => t.Database.GetContainer(name),
            this);

    public Task<ItemRequestOptions?> GetEncrypedRequestOptionsAsync<TDocument>(
        RequestOptions<TDocument> request,
        CancellationToken cancellationToken)
        where TDocument : notnull
        => GetEncryptionRequestOptionsAsync(
            request,
            request.GetItemRequestOptions(),
            request.RequireDocument(),
            cancellationToken);

    public Task<TransactionalBatchItemRequestOptions?> GetEncryptedTransactionalRequestOptionsAsync<TDocument>(
        RequestOptions request,
        TDocument? document,
        CancellationToken cancellationToken)
        where TDocument : notnull
    {
        var options = request.GetTransactionalRequestOptions();

        if (document is null)
        {
            return Task.FromResult(options);
        }

        return GetEncryptionRequestOptionsAsync(request, options, document, cancellationToken);
    }

    private async Task<TOptions?> GetEncryptionRequestOptionsAsync<TOptions, TDocument>(
        RequestOptions request,
        TOptions? cosmosRequestOptions,
        TDocument document,
        CancellationToken cancellationToken)
        where TOptions : Azure.Cosmos.RequestOptions
        where TDocument : notnull
        => CosmosEncryptionProvider != null
            ? await CosmosEncryptionProvider.GetEncryptionItemRequestOptionsAsync(
                request,
                cosmosRequestOptions,
                Database.Client.Endpoint,
                document,
                cancellationToken)
                .ConfigureAwait(false)
            : cosmosRequestOptions;
}
