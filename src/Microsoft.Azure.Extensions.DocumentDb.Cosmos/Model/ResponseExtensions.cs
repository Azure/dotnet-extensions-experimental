// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Document.Cosmos.Model;
using Microsoft.Shared.Collections;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Document database response extensions.
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Determines if the response has a status.
    /// </summary>
    /// <param name="response">The response.</param>
    /// <param name="code">The status code to verify.</param>
    /// <returns>True if the response having the target status, false otherwise.</returns>
    public static bool HasStatus(this IDatabaseResponse response, HttpStatusCode code)
        => response?.Status == (int)code;

    internal static CosmosDatabaseResponse<T> ToDatabaseResponse<T>(
        this Response<T> response,
        string table,
        string? region,
        Uri endpoint)
        where T : notnull
        => new(response.GetRequest(table, region, endpoint),
            response.StatusCode,
            response.Resource,
            response.ETag,
            rawResponse: response);

    internal static CosmosDatabaseResponse<bool> ToDatabaseResponse(
        this ResponseMessage response,
        string table,
        string? region,
        Uri endpoint)
        => new(response.GetRequest(table, region, endpoint),
            response.StatusCode,
            response.IsSuccessStatusCode,
            continuationToken: response.ContinuationToken,
            rawResponse: response);

    internal static CosmosDatabaseResponse<bool> ToDatabaseResponse(
        this ThroughputResponse response,
        string table,
        string? region,
        Uri endpoint)
        => new(response.GetRequest(table, region, endpoint),
            response.StatusCode,
            !(response.IsReplacePending ?? false),
            response.ETag,
            rawResponse: response);

    internal static CosmosDatabaseResponse<T> ToDatabaseResponse<T>(
        this TransactionalBatchOperationResult<T> response,
        CosmosTable container,
        string? region,
        Uri endpoint)
        where T : notnull
        => new(response.GetRequest(container.Options.TableName, region, endpoint),
            response.StatusCode,
            response.Resource,
            response.ETag,
            rawResponse: response);

    internal static CosmosDatabaseResponse<IReadOnlyList<IDatabaseResponse<T>>> ToDatabaseResponse<T>(
        this TransactionalBatchResponse response,
        CosmosTable container,
        string? region,
        Uri endpoint)
        where T : notnull
    {
        List<IDatabaseResponse<T>> results = new(response.Count);

        for (int resultIndex = 0; resultIndex < response.Count; ++resultIndex)
        {
            var rawResult = response.GetOperationResultAtIndex<T>(resultIndex);
            IDatabaseResponse<T> result = rawResult.ToDatabaseResponse(container, region, endpoint);
            results.Add(result);
        }

        var aggregatedResult = CosmosDatabaseResponse<IReadOnlyList<T>>.MakeResponseFromList(
            response.GetRequest(container.Options.TableName, region, endpoint),
            response.StatusCode,
            ((IReadOnlyList<IDatabaseResponse<T>>)results).EmptyIfNull(),
            rawResponse: response);
        return aggregatedResult;
    }

    internal static CosmosDatabaseResponse<TableOptions> ToDatabaseResponse(
        this Response<ContainerProperties> response,
        string table,
        string? region,
        Uri endpoint)
        => new(response.GetRequest(table, region, endpoint),
            response.StatusCode,
            response.GetContainerOptions(table),
            response.ETag,
            rawResponse: response);

    internal static CosmosDatabaseResponse<IReadOnlyList<T>> ToDatabaseResponse<T>(
        this System.Cloud.DocumentDb.RequestOptions request,
        IReadOnlyList<T> result,
        string table,
        HttpStatusCode code,
        string? continuationToken,
        Uri endpoint,
        double cost)
        where T : notnull
        => CosmosDatabaseResponse<IReadOnlyList<T>>.MakeResponseFromList(
            request.GetRequest(table, cost, endpoint), code, result, continuationToken);

    internal static CosmosDatabaseResponse<T> WithStatus<T>(this IDatabaseResponse<T> response, HttpStatusCode code, bool? statusOverride = null)
        where T : notnull
        => new(response.RequestInfo,
            code,
            response.Item,
            response.ItemVersion,
            response.ContinuationToken,
            statusOverride,
            (response as ICosmosDatabaseResponse)?.RawResponse);

    private static CosmosTableOptions GetContainerOptions(this Response<ContainerProperties> response, string tableId)
    {
        CosmosTableOptions result = new()
        {
            TableName = response.Resource?.Id ?? tableId,
            TimeToLive = response.Resource?.DefaultTimeToLive != null
                ? TimeSpan.FromSeconds((double)response.Resource.DefaultTimeToLive)
                : TimeSpan.MaxValue,
            PartitionIdPath = response.Resource?.PartitionKeyPath,
            IndexingPolicy = response.Resource?.IndexingPolicy,
            UniqueKeyPolicy = response.Resource?.UniqueKeyPolicy,
        };

        return result;
    }

    private static RequestInfo GetRequest<TResponse>(this Response<TResponse> response, string table, string? region, Uri endpoint)
        => new(region, table, response.RequestCharge, endpoint);

    private static RequestInfo GetRequest(this System.Cloud.DocumentDb.RequestOptions request, string table, double cost, Uri endpoint)
        => new(request.Region, table, cost, endpoint);

    private static RequestInfo GetRequest(this ResponseMessage response, string table, string? region, Uri endpoint)
        => new(region, table, response.Headers.RequestCharge, endpoint);

    private static RequestInfo GetRequest(this TransactionalBatchOperationResult _, string table, string? region, Uri endpoint)
        => new(region, table, endpoint: endpoint);

    private static RequestInfo GetRequest(this TransactionalBatchResponse response, string table, string? region, Uri endpoint)
        => new(region, table, response.RequestCharge, endpoint);
}
