// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using RequestOptions = System.Cloud.DocumentDb.RequestOptions;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The base implementation of Cosmos DB Client for table operations.
/// </summary>
/// <remarks>
/// This internal implementation is covering Cosmos interactions.
/// It does not implement other interface contracts like exception handling or retries.
/// </remarks>
internal class BaseCosmosClient
{
    /// <summary>
    /// Maximum items supported on a single transaction batch.
    /// </summary>
    internal const int MaxItemsOfTransactionBatch = 100;

    internal readonly TableInfo Table;
    internal readonly TableOptions TableOptions;
    internal readonly IInternalDatabase Database;

    private static readonly Azure.Cosmos.RequestOptions _cosmosEmptyRequestOptions = new();
    private static readonly ContainerRequestOptions _cosmosContainerEmptyRequest = new();

    public BaseCosmosClient(
        TableOptions options,
        IInternalDatabase database)
    {
        Table = new TableInfo(options);
        TableOptions = options;
        Database = database;
    }

    internal virtual async Task<IDatabaseResponse<TableOptions>> CreateTableAsync(
        RequestOptions request,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);
        var containerProperties = GetContainerProperties(container);
        var throughputProperties = Table.GetThroughputProperties();

        ContainerResponse response = await container.Database.CreateContainerAsync(
                containerProperties,
                throughputProperties,
                _cosmosEmptyRequestOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return response.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);
    }

    internal virtual async Task<IDatabaseResponse<bool>> UpdateTableSettingsAsync(
        RequestOptions request,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);
        var properties = GetContainerProperties(container);

        _ = await container
            .ReplaceContainerAsync(properties, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var throughputProperties = Table.GetThroughputProperties();
        ThroughputResponse throughputResponse = await container
            .ReplaceThroughputAsync(throughputProperties, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return throughputResponse.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);
    }

    internal virtual async Task<IDatabaseResponse<TableOptions>> DeleteTableAsync(
        RequestOptions request,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);

        ContainerResponse response = await container
            .DeleteContainerAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);
    }

    internal virtual async Task<IDatabaseResponse<TableOptions>> ReadTableSettingsAsync(
        RequestOptions request,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);

        ContainerResponse response = await container
            .ReadContainerAsync(_cosmosContainerEmptyRequest, cancellationToken)
            .ConfigureAwait(false);

        var result = response.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);

        if (result.Item != null)
        {
            var throughput = await container.ReadThroughputAsync(cancellationToken)
                .ConfigureAwait(false);

            result.Item.Throughput = new(throughput);
        }

        return result;
    }

    protected ContainerProperties GetContainerProperties(Container container)
    {
        ContainerProperties properties = new ContainerProperties(container.Id, Table.PartitionIdPath);

        if (Table.TimeToLive != Timeout.InfiniteTimeSpan)
        {
            properties.DefaultTimeToLive = (int)Table.TimeToLive.TotalSeconds;
        }

        CosmosTableOptions? containerOptions = TableOptions as CosmosTableOptions;
        UniqueKeyPolicy? uniqueKeyPolicy = containerOptions?.UniqueKeyPolicy;
        IndexingPolicy? indexingPolicy = containerOptions?.IndexingPolicy;

        if (uniqueKeyPolicy != null)
        {
            properties.UniqueKeyPolicy = uniqueKeyPolicy;
        }

        if (indexingPolicy != null)
        {
            properties.IndexingPolicy = indexingPolicy;
        }

        return properties;
    }

    protected Task<CosmosTable> GetCosmosContainerAsync(RequestOptions request, CancellationToken cancellationToken)
        => Database.GetContainerAsync(Table, request, cancellationToken);

    protected async Task<Container> GetContainerAsync(RequestOptions request, CancellationToken cancellationToken)
    {
        CosmosTable container = await GetCosmosContainerAsync(request, cancellationToken).ConfigureAwait(false);
        return container.Container;
    }
}
