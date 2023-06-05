// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Shared.Collections;
using AzurePatchOperation = Microsoft.Azure.Cosmos.PatchOperation;
using ExtensionsDocument = System.Cloud.DocumentDb;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

internal static class RequestOptionsExtensions
{
    public static PartitionKey? GetPartitionKey(this ExtensionsDocument.RequestOptions request)
        => (request.PartitionKey?.Count ?? 0) == 0
            ? null
            : GetPartitionKey(request.PartitionKey!);

    public static PartitionKey RequirePartitionKey(this ExtensionsDocument.RequestOptions request)
    {
        var partitionKey = InternalThrows.IfNullOrEmpty<IReadOnlyList<object?>, object?>(
            request.PartitionKey,
            "Partition key is null or empty in request.");

        // No sense of caching, since it is unique per item and created once.
        // It can not be managed by object pools, since the object is immutable.
        return GetPartitionKey(partitionKey);
    }

    public static TDocument RequireDocument<TDocument>(this RequestOptions<TDocument> request)
        where TDocument : notnull => InternalThrows.IfNull(request.Document, "Document is required but null.");

    /// <summary>
    /// Converts <see cref="ExtensionsDocument.ConsistencyLevel"/> to <see cref="Azure.Cosmos.ConsistencyLevel"/>.
    /// </summary>
    /// <param name="consistencyLevel">The R9 consistency level.</param>
    /// <returns>The <see cref="Azure.Cosmos.ConsistencyLevel"/>.</returns>
    public static Azure.Cosmos.ConsistencyLevel? ToCosmosConsistencyLevel(this ExtensionsDocument.ConsistencyLevel? consistencyLevel) =>
        consistencyLevel switch
        {
            ExtensionsDocument.ConsistencyLevel.Strong => Azure.Cosmos.ConsistencyLevel.Strong,
            ExtensionsDocument.ConsistencyLevel.BoundedStaleness => Azure.Cosmos.ConsistencyLevel.BoundedStaleness,
            ExtensionsDocument.ConsistencyLevel.Session => Azure.Cosmos.ConsistencyLevel.Session,
            ExtensionsDocument.ConsistencyLevel.Eventual => Azure.Cosmos.ConsistencyLevel.Eventual,
            ExtensionsDocument.ConsistencyLevel.ConsistentPrefix => Azure.Cosmos.ConsistencyLevel.ConsistentPrefix,
            _ => null,
        };

    public static QueryRequestOptions GetQueryRequestOptions<TDocument>(this QueryRequestOptions<TDocument> options)
        where TDocument : notnull => new()
        {
            MaxItemCount = options.MaxResults,
            MaxConcurrency = options.MaxConcurrency,
            MaxBufferedItemCount = options.MaxBufferedItemCount,
            EnableLowPrecisionOrderBy = options.EnableLowPrecisionOrderBy,
            ResponseContinuationTokenLimitInKb = options.ResponseContinuationTokenLimitInKb,
            EnableScanInQuery = options.EnableScan,
            ConsistencyLevel = options.ConsistencyLevel.ToCosmosConsistencyLevel(),
            PartitionKey = options.GetPartitionKey(),
            SessionToken = options.SessionToken,
        };

    public static bool HasNonDefaults(this ExtensionsDocument.RequestOptions request)
        => !string.IsNullOrWhiteSpace(request.ItemVersion)
        || !string.IsNullOrWhiteSpace(request.SessionToken)
        || !request.ContentResponseOnWrite;

    public static ItemRequestOptions? GetItemRequestOptions(this ExtensionsDocument.RequestOptions request)
        => request.HasNonDefaults()
            ? new()
            {
                IfMatchEtag = request.ItemVersion,
                SessionToken = request.SessionToken,
                EnableContentResponseOnWrite = request.ContentResponseOnWrite
            }
            : null;

    public static PatchItemRequestOptions? GetPatchRequestOptions(this ExtensionsDocument.RequestOptions request, string? filter)
        => request.HasNonDefaults() || !string.IsNullOrWhiteSpace(filter) // Any value differs from default
            ? new()
            {
                IfMatchEtag = request.ItemVersion,
                SessionToken = request.SessionToken,
                EnableContentResponseOnWrite = request.ContentResponseOnWrite,
                FilterPredicate = filter
            }
            : null;

    public static TransactionalBatchItemRequestOptions? GetTransactionalRequestOptions(this ExtensionsDocument.RequestOptions request)
    {
        string? etag = request.ItemVersion;
        bool contentOnWrite = request.ContentResponseOnWrite;

        return !string.IsNullOrWhiteSpace(etag) || !contentOnWrite // Any value differs from default
            ? new() { IfMatchEtag = etag, EnableContentResponseOnWrite = contentOnWrite }
            : null;
    }

    public static IReadOnlyList<AzurePatchOperation> ToCosmosPatchOperations(this IReadOnlyList<ExtensionsDocument.PatchOperation> operations)
    {
        int count = operations.Count;

        if (count == 0)
        {
            return Empty.ReadOnlyList<AzurePatchOperation>();
        }

        var resultArray = new AzurePatchOperation[count];

        for (int index = 0; index < count; index++)
        {
            resultArray[index] = operations[index].ToCosmosPatchOperation();
        }

        return resultArray;
    }

    /// <summary>
    /// Checks validity of the request.
    /// </summary>
    /// <param name="options">The table options.</param>
    /// <param name="request">The request options.</param>
    /// <exception cref="DatabaseClientException">If request is not valid.</exception>
    public static void ValidateRequest(this TableInfo options, ExtensionsDocument.RequestOptions request)
    {
        if (!options.IsRegional != string.IsNullOrEmpty(request?.Region))
        {
            throw new DatabaseClientException(
                $"Request is not valid {nameof(options.IsRegional)} is not compatible with value of {nameof(request.Region)}.");
        }
    }

    public static AzurePatchOperation ToCosmosPatchOperation(this ExtensionsDocument.PatchOperation operation) =>
        operation.OperationType switch
        {
            ExtensionsDocument.PatchOperationType.Add => AzurePatchOperation.Add(operation.Path, operation.Value),
            ExtensionsDocument.PatchOperationType.Remove => AzurePatchOperation.Remove(operation.Path),
            ExtensionsDocument.PatchOperationType.Replace => AzurePatchOperation.Replace(operation.Path, operation.Value),
            ExtensionsDocument.PatchOperationType.Set => AzurePatchOperation.Set(operation.Path, operation.Value),
            ExtensionsDocument.PatchOperationType.Increment => GetIncrementOperation(operation.Path, operation.Value),
            _ => throw new ArgumentException($"Provided value is invalid: {operation.OperationType}.")
        };

    public static ThroughputProperties? GetThroughputProperties(this TableInfo options)
    {
        int? throughput = options.Throughput.Value;
        var properties = throughput.HasValue ? ThroughputProperties.CreateManualThroughput(throughput.Value) : null;
        return properties;
    }

    private static AzurePatchOperation GetIncrementOperation(string path, object value) =>
        value switch
        {
            long longValue => AzurePatchOperation.Increment(path, longValue),
            double doubleValue => AzurePatchOperation.Increment(path, doubleValue),
            _ => throw new ArgumentException("Increment value should be either long or double."),
        };

    private static PartitionKey GetPartitionKey(IReadOnlyList<object?> hierarchicalPartitionKeys)
    {
        if (hierarchicalPartitionKeys.Count == 1)
        {
            var value = hierarchicalPartitionKeys[0];

            if (value == CosmosConstants.NoPartitionKey)
            {
                return PartitionKey.None;
            }

            return value switch
            {
                null => PartitionKey.Null,
                string stringValue => new PartitionKey(stringValue),
                bool boolValue => new PartitionKey(boolValue),
                double doubleValue => new PartitionKey(doubleValue),
                int intValue => new PartitionKey(intValue),
                _ => throw new ArgumentException(
                        $"Partition key components can be string, double, bool, null or CosmosTableOptions.NONEPK values only, got [{value.GetType().FullName}].")
            };
        }

        // Below code is a part of hierarchical key feature, which is in preview.
        // If end user is not willing to include preview version below will fail.
        // That is an actual user request, based on adoption feedback.
        // That is why keeping above use case implemented in "legacy" way.
        PartitionKeyBuilder builder = new PartitionKeyBuilder();

        for (int keyIndex = 0; keyIndex < hierarchicalPartitionKeys.Count; keyIndex++)
        {
            object? value = hierarchicalPartitionKeys[keyIndex];

            if (value == CosmosConstants.NoPartitionKey)
            {
                _ = builder.AddNoneType();
            }
            else
            {
                _ = value switch
                {
                    null => builder.AddNullValue(),
                    string stringValue => builder.Add(stringValue),
                    bool boolValue => builder.Add(boolValue),
                    double doubleValue => builder.Add(doubleValue),
                    int intValue => builder.Add(intValue),
                    _ => throw new ArgumentException(
                        $"Partition key components can be string, double, bool, null or CosmosTableOptions.NONEPK values only, got [{value.GetType().FullName}].")
                };
            }
        }

        PartitionKey key = builder.Build();

        return key;
    }
}
