// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Extensions.Document.Cosmos;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The base implementation of document database client for Cosmos DB.
/// </summary>
/// <remarks>
/// This internal implementation is covering Cosmos interactions.
/// It does not implement other interface contracts like exception handling or retries.
/// </remarks>
internal class BaseCosmosDocumentClient<TDocument> :
    BaseCosmosClient,
    IDocumentReader<TDocument>,
    IDocumentWriter<TDocument>
    where TDocument : notnull
{
    public BaseCosmosDocumentClient(
        TableOptions options,
        IInternalDatabase database)
        : base(options, database)
    {
    }

    /// <inheritdoc/>
    public async virtual Task<IDatabaseResponse<int>> CountDocumentsAsync(
        QueryRequestOptions<TDocument> request,
        Func<IQueryable<TDocument>, IQueryable<TDocument>>? condition,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);
        var conditionFunc = condition ?? (queryable => queryable);

        Response<int> response = await conditionFunc(
            container
                .GetItemLinqQueryable<TDocument>(requestOptions: request.GetQueryRequestOptions()))
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

        return response.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);
    }

    /// <inheritdoc/>
    public virtual async Task<IDatabaseResponse<TDocument>> CreateDocumentAsync(
        RequestOptions<TDocument> request,
        CancellationToken cancellationToken)
    {
        TDocument document = request.RequireDocument();
        var container = await GetCosmosContainerAsync(request, cancellationToken).ConfigureAwait(false);

        ItemRequestOptions? itemRequestOptions = await container.Database
            .GetEncrypedRequestOptionsAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var itemResponse = await container.Container.CreateItemAsync(
            document,
            request.GetPartitionKey(),
            itemRequestOptions,
            cancellationToken)
            .ConfigureAwait(false);

        return itemResponse.ToDatabaseResponse(container.Options.TableName, request.Region, container.Container.Database.Client.Endpoint);
    }

    /// <inheritdoc/>
    public virtual async Task<IDatabaseResponse<bool>> DeleteDocumentAsync(
        RequestOptions<TDocument> request,
        string id,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);

        ResponseMessage response = await container.DeleteItemStreamAsync(
                id,
                request.RequirePartitionKey(),
                request.GetItemRequestOptions(),
                cancellationToken)
            .ConfigureAwait(false);

        return response.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);
    }

    /// <inheritdoc/>
    public async virtual Task<IDatabaseResponse<IReadOnlyList<IDatabaseResponse<TDocument>>>> ExecuteTransactionalBatchAsync(
        RequestOptions<TDocument> request,
        IReadOnlyList<BatchItem<TDocument>> itemsToPerformTransactionalBatch,
        CancellationToken cancellationToken)
    {
        var container = await GetCosmosContainerAsync(request, cancellationToken).ConfigureAwait(false);

        if (itemsToPerformTransactionalBatch.Count > MaxItemsOfTransactionBatch)
        {
            CosmosThrow.DatabaseClientException($"Transaction batch items exceed the limitation of {MaxItemsOfTransactionBatch}.");
        }

        TransactionalBatch transactionalBatch = container.Container.CreateTransactionalBatch(request.RequirePartitionKey());

        foreach (BatchItem<TDocument> cosmosBatchItem in itemsToPerformTransactionalBatch)
        {
            TransactionalBatchItemRequestOptions? transactionalRequestOptions =
                await container.Database.GetEncryptedTransactionalRequestOptionsAsync(request, cosmosBatchItem.Item, cancellationToken)
                .ConfigureAwait(false);

            switch (cosmosBatchItem.Operation)
            {
                case BatchOperation.Create:
                    transactionalBatch = transactionalBatch.CreateItem(
                        cosmosBatchItem.Item,
                        transactionalRequestOptions);
                    break;

                case BatchOperation.Delete:
                    transactionalBatch = transactionalBatch.DeleteItem(
                        cosmosBatchItem.Id,
                        transactionalRequestOptions);
                    break;

                case BatchOperation.Replace:
                    transactionalBatch = transactionalBatch.ReplaceItem(
                        cosmosBatchItem.Id,
                        cosmosBatchItem.Item,
                        transactionalRequestOptions);
                    break;

                case BatchOperation.Upsert:
                    transactionalBatch = transactionalBatch.UpsertItem(
                        cosmosBatchItem.Item,
                        transactionalRequestOptions);
                    break;

                case BatchOperation.Read:
                    transactionalBatch = transactionalBatch.ReadItem(
                        cosmosBatchItem.Id,
                        transactionalRequestOptions);
                    break;
            }
        }

        using TransactionalBatchResponse response = await transactionalBatch
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return response.ToDatabaseResponse<TDocument>(container, request.Region, container.Container.Database.Client.Endpoint);
    }

    /// <inheritdoc/>
    public async virtual Task<IDatabaseResponse<IReadOnlyList<TOutputDocument>>> FetchDocumentsAsync<TOutputDocument>(
        QueryRequestOptions<TDocument> request,
        Func<IQueryable<TDocument>, IQueryable<TOutputDocument>>? condition,
        CancellationToken cancellationToken)
        where TOutputDocument : notnull
    {
        var container = await GetCosmosContainerAsync(request, cancellationToken).ConfigureAwait(false);
        var cosmosDatabase = container.Database;
        var cosmosContainer = container.Container;

        QueryRequestOptions queryRequestOptions = request.GetQueryRequestOptions();
        IQueryable<TOutputDocument> queryable;

        if (condition == null)
        {
            queryable = cosmosContainer.GetItemLinqQueryable<TOutputDocument>(
                continuationToken: request.ContinuationToken,
                requestOptions: queryRequestOptions);
        }
        else
        {
            queryable = condition(cosmosContainer.GetItemLinqQueryable<TDocument>(
                continuationToken: request.ContinuationToken,
                requestOptions: queryRequestOptions));
        }

        var iterator = cosmosDatabase.CosmosEncryptionProvider
            ?.ToEncryptionStreamIterator(cosmosContainer, queryable)
            ?? queryable.ToStreamIterator();

        List<TOutputDocument> results = new();
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        string? continuationToken = null;
        FetchMode fetchCondition = request.FetchCondition;
        double totalCost = 0;

        while (iterator.HasMoreResults)
        {
            using (ResponseMessage responseMessage = await iterator
                .ReadNextAsync(cancellationToken).ConfigureAwait(false))
            {
                _ = responseMessage.EnsureSuccessStatusCode();

                totalCost += responseMessage.Headers.RequestCharge;
                continuationToken = responseMessage.ContinuationToken;

                DocumentArray<TOutputDocument> pageResults = cosmosDatabase.Configuration.CosmosSerializer
                    .FromStream<DocumentArray<TOutputDocument>>(responseMessage.Content);

                // `pageResults` is not checked for null, below despite serializer implementation can return `null`.
                // That is done because in normal flows, `responseMessage.Content` is never null for success cases.
                // So that this branch can not be tested.
                // And above behavior can not be mocked since getting iterator uses external static extensions.
                if (pageResults.Documents != null)
                {
                    results.AddRange(pageResults.Documents);
                }

                statusCode = responseMessage.StatusCode;
            }

            if (fetchCondition == FetchMode.FetchSinglePage ||
               (fetchCondition == FetchMode.FetchMaxResults && results.Count >= queryRequestOptions.MaxItemCount))
            {
                break;
            }
        }

        var response = request.ToDatabaseResponse(
            results,
            container.Options.TableName,
            statusCode,
            continuationToken,
            cosmosDatabase.Database.Client.Endpoint,
            totalCost);
        return response;
    }

    /// <inheritdoc/>
    public async virtual Task<IDatabaseResponse<IReadOnlyList<TDocument>>> QueryDocumentsAsync(
        QueryRequestOptions<TDocument> request,
        Query query,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);
        FetchMode fetchCondition = request.FetchCondition;
        QueryRequestOptions queryRequestOptions = request.GetQueryRequestOptions();
        QueryDefinition queryDefinition = new QueryDefinition(query.QueryText);

        foreach (KeyValuePair<string, string> parameter in query.Parameters)
        {
            queryDefinition = queryDefinition.WithParameter(parameter.Key, parameter.Value);
        }

        FeedIterator<TDocument> feedIterator = container.GetItemQueryIterator<TDocument>(
                queryDefinition,
                request.ContinuationToken,
                queryRequestOptions);

        List<TDocument> results = new();
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        string? continuationToken = null;
        double totalCost = 0;

        while (feedIterator.HasMoreResults)
        {
            FeedResponse<TDocument> feedResponse = await feedIterator
                .ReadNextAsync(cancellationToken)
                .ConfigureAwait(false);

            totalCost += feedResponse.RequestCharge;
            results.AddRange(feedResponse);
            continuationToken = feedResponse.ContinuationToken;
            statusCode = feedResponse.StatusCode;

            if (fetchCondition == FetchMode.FetchSinglePage ||
               (fetchCondition == FetchMode.FetchMaxResults && results.Count >= queryRequestOptions.MaxItemCount))
            {
                break;
            }
        }

        var response = request.ToDatabaseResponse(
            results,
            container.Id,
            statusCode,
            continuationToken,
            container.Database.Client.Endpoint,
            totalCost);
        return response;
    }

    /// <inheritdoc/>
    public virtual async Task<IDatabaseResponse<TDocument>> InsertOrUpdateDocumentAsync(
        RequestOptions<TDocument> request,
        string id,
        Func<TDocument, TDocument> conflictResolveFunc,
        CancellationToken cancellationToken)
    {
        IDatabaseResponse<TDocument> readResponse =
            await ReadDocumentAsync(
                request,
                id,
                cancellationToken)
            .ConfigureAwait(false);

        if (readResponse.Item is null)
        {
            if (!readResponse.HasStatus(HttpStatusCode.NotFound))
            {
                // This code should not be reachable, adding this to cover unexpected responses.
                // Read is not expected to have other results except succeed or NotFound if item not exists.
                // Other results should produce an exception, which will be propagated to the customer.
                CosmosThrow.UnexpectedResult(
                    nameof(ReadDocumentAsync),
                    (HttpStatusCode)readResponse.Status,
                    readResponse.RequestInfo);
            }

            IDatabaseResponse<TDocument> createResponse =
                await CreateDocumentAsync(
                    request,
                    cancellationToken)
                .ConfigureAwait(false);

            if (createResponse.Succeeded)
            {
                return createResponse;
            }

            if (createResponse.HasStatus(HttpStatusCode.Conflict))
            {
                // Rather than manually retry read before write, let retry logic request again.
                CosmosThrow.DatabaseRetryableException(
                    "Item created from another process between read and create. Retry the operation.");
            }

            // This code should not be reachable, adding this to cover unexpected responses.
            // Create is not expected to have other results except succeed or Conflict if item exists.
            // Other results should produce an exception, which will be propagated to the customer.
            CosmosThrow.UnexpectedResult(
                nameof(CreateDocumentAsync),
                (HttpStatusCode)createResponse.Status,
                createResponse.RequestInfo);
        }

        TDocument updatedItem = conflictResolveFunc(readResponse.Item);

        if (updatedItem is null || updatedItem.Equals(readResponse.Item))
        {
            // Conflict resolve condition is not satisfied or returned the same item as in DB.
            return readResponse.WithStatus(HttpStatusCode.NotModified, true);
        }

        request.Document = updatedItem;
        request.ItemVersion = readResponse.ItemVersion;

        IDatabaseResponse<TDocument> replaceResponse =
            await ReplaceDocumentAsync(
                request,
                id,
                cancellationToken)
            .ConfigureAwait(false);

        if (replaceResponse.HasStatus(HttpStatusCode.PreconditionFailed))
        {
            // Means the Etag does not match because the document modified between read and replace.
            CosmosThrow.DatabaseRetryableException("Item modified between read and replace by another process. Retry the operation.");
        }

        return replaceResponse;
    }

    /// <inheritdoc/>
    public virtual async Task<IDatabaseResponse<TDocument>> ReadDocumentAsync(
        RequestOptions<TDocument> request,
        string id,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);

        ItemResponse<TDocument> itemResponse = await container
            .ReadItemAsync<TDocument>(id,
                request.RequirePartitionKey(),
                request.GetItemRequestOptions(),
                cancellationToken)
            .ConfigureAwait(false);

        return itemResponse.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);
    }

    /// <inheritdoc/>
    public virtual async Task<IDatabaseResponse<TDocument>> PatchDocumentAsync(
        RequestOptions<TDocument> request,
        string id,
        IReadOnlyList<System.Cloud.DocumentDb.PatchOperation> patchOperations,
        string? filter,
        CancellationToken cancellationToken)
    {
        var container = await GetContainerAsync(request, cancellationToken).ConfigureAwait(false);

        PatchItemRequestOptions? patchOptions = request.GetPatchRequestOptions(filter);

        ItemResponse<TDocument> itemResponse = await container
            .PatchItemAsync<TDocument>(id,
                request.RequirePartitionKey(),
                patchOperations.ToCosmosPatchOperations(),
                patchOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return itemResponse.ToDatabaseResponse(container.Id, request.Region, container.Database.Client.Endpoint);
    }

    /// <inheritdoc/>
    public virtual async Task<IDatabaseResponse<TDocument>> ReplaceDocumentAsync(
        RequestOptions<TDocument> request,
        string id,
        CancellationToken cancellationToken)
    {
        TDocument document = request.RequireDocument();
        var container = await GetCosmosContainerAsync(request, cancellationToken).ConfigureAwait(false);

        ItemRequestOptions? itemRequestOptions = await container.Database
            .GetEncrypedRequestOptionsAsync(request, cancellationToken)
            .ConfigureAwait(false);

        ItemResponse<TDocument> itemResponse =
            await container.Container.ReplaceItemAsync(
                document,
                id,
                request.GetPartitionKey(),
                itemRequestOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return itemResponse.ToDatabaseResponse(container.Options.TableName, request.Region, container.Container.Database.Client.Endpoint);
    }

    /// <inheritdoc/>
    public virtual async Task<IDatabaseResponse<TDocument>> UpsertDocumentAsync(
        RequestOptions<TDocument> request,
        CancellationToken cancellationToken)
    {
        TDocument document = request.RequireDocument();
        var container = await GetCosmosContainerAsync(request, cancellationToken).ConfigureAwait(false);

        ItemRequestOptions? itemRequestOptions = await container.Database
            .GetEncrypedRequestOptionsAsync(request, cancellationToken)
            .ConfigureAwait(false);

        ItemResponse<TDocument> itemResponse =
            await container.Container.UpsertItemAsync(
                document,
                request.GetPartitionKey(),
                itemRequestOptions,
                cancellationToken)
            .ConfigureAwait(false);

        return itemResponse.ToDatabaseResponse(container.Options.TableName, request.Region, container.Container.Database.Client.Endpoint);
    }
}
