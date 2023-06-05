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
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using PatchOperation = System.Cloud.DocumentDb.PatchOperation;
using RequestOptions = System.Cloud.DocumentDb.RequestOptions;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The extension of <see cref="BaseCosmosDocumentClient{TDocument}"/> adding features on a top.
/// </summary>
/// <remarks>
/// APIs in <see cref="BaseCosmosDocumentClient{TDocument}"/> could call other APIs in the client.
/// To add subcalls proper support of extensions, we inherit the base client instead of holding a variable.
/// </remarks>
internal sealed class DecoratedCosmosClient<TDocument> : BaseCosmosDocumentClient<TDocument>
    where TDocument : notnull
{
    private readonly ICallDecorationPipeline<DecoratedCosmosContext> _decorator;

    internal static IDatabaseResponse<T> ProcessException<T>(Exception exception)
        where T : notnull
        => exception is CosmosException cosmosException
            ? new CosmosDatabaseResponse<T>(new(cost: cosmosException.RequestCharge), cosmosException.StatusCode, default, succeededOverride: false, rawResponse: exception)
            : new CosmosDatabaseResponse<T>(default, HttpStatusCode.InternalServerError, default, rawResponse: exception);

    private static readonly Func<Exception, IDatabaseResponse<int>> _processExceptionInt = ProcessException<int>;
    private static readonly Func<Exception, IDatabaseResponse<bool>> _processExceptionBool = ProcessException<bool>;
    private static readonly Func<Exception, IDatabaseResponse<TDocument>> _processExceptionDoc = ProcessException<TDocument>;
    private static readonly Func<Exception, IDatabaseResponse<IReadOnlyList<TDocument>>> _processExceptionDocList = ProcessException<IReadOnlyList<TDocument>>;
    private static readonly Func<Exception, IDatabaseResponse<TableOptions>> _processExceptionTableOptions = ProcessException<TableOptions>;
    private static readonly Func<Exception, IDatabaseResponse<IReadOnlyList<IDatabaseResponse<TDocument>>>> _processExceptionResponseList
        = ProcessException<IReadOnlyList<IDatabaseResponse<TDocument>>>;

    internal DecoratedCosmosClient(
        TableOptions options,
        IInternalDatabase database,
        ICallDecorationPipeline<DecoratedCosmosContext> decorator)
        : base(options, database)
    {
        _decorator = decorator;
    }

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<int>> CountDocumentsAsync(
        QueryRequestOptions<TDocument> requestOptions,
        Func<IQueryable<TDocument>, IQueryable<TDocument>>? condition,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync<IDatabaseResponse<int>>(
            (context, _, cancellationToken) => base.CountDocumentsAsync(
                (context.RequestOptions as QueryRequestOptions<TDocument>)!,
                context.GetItemOf<QueryConditionContextItem<TDocument>>().Condition,
                cancellationToken),
            nameof(CountDocumentsAsync),
            requestOptions,
            new QueryConditionContextItem<TDocument>(condition),
            _processExceptionInt,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<TDocument>> CreateDocumentAsync(
        RequestOptions<TDocument> requestOptions,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.CreateDocumentAsync(
                (context.RequestOptions as RequestOptions<TDocument>)!,
                cancellationToken),
            nameof(CreateDocumentAsync),
            requestOptions,
            null,
            _processExceptionDoc,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<bool>> DeleteDocumentAsync(
        RequestOptions<TDocument> requestOptions,
        string id,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.DeleteDocumentAsync(
                (context.RequestOptions as RequestOptions<TDocument>)!,
                context.GetItemOf<string>(),
                cancellationToken),
            nameof(DeleteDocumentAsync),
            requestOptions,
            id,
            _processExceptionBool,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<IReadOnlyList<IDatabaseResponse<TDocument>>>> ExecuteTransactionalBatchAsync(
        RequestOptions<TDocument> requestOptions,
        IReadOnlyList<BatchItem<TDocument>> itemsToPerformTransactionalBatch,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.ExecuteTransactionalBatchAsync(
                (context.RequestOptions as RequestOptions<TDocument>)!,
                context.GetItemOf<IReadOnlyList<BatchItem<TDocument>>>(),
                cancellationToken),
            nameof(ExecuteTransactionalBatchAsync),
            requestOptions,
            itemsToPerformTransactionalBatch,
            _processExceptionResponseList,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<IReadOnlyList<TOutputDocument>>> FetchDocumentsAsync<TOutputDocument>(
        QueryRequestOptions<TDocument> requestOptions,
        Func<IQueryable<TDocument>, IQueryable<TOutputDocument>>? condition,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.FetchDocumentsAsync(
                (context.RequestOptions as QueryRequestOptions<TDocument>)!,
                context.GetItemOf<QueryConditionContextItem<TOutputDocument>>().Condition,
                cancellationToken),
            nameof(FetchDocumentsAsync),
            requestOptions,
            new QueryConditionContextItem<TOutputDocument>(condition),
#pragma warning disable R9A034 // Optimize method group use to avoid allocations
            ProcessException<IReadOnlyList<TOutputDocument>>,
#pragma warning restore R9A034 // Optimize method group use to avoid allocations
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<TDocument>> InsertOrUpdateDocumentAsync(
        RequestOptions<TDocument> requestOptions,
        string id,
        Func<TDocument, TDocument> conflictResolveFunc,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync((context, _, cancellationToken) =>
            {
                InsertOrUpdateDocumentContextItem contextItem = context.GetItemOf<InsertOrUpdateDocumentContextItem>();

                return base.InsertOrUpdateDocumentAsync(
                    (context.RequestOptions as RequestOptions<TDocument>)!,
                    contextItem.Id,
                    contextItem.ConflictResolveFunc,
                    cancellationToken);
            },
            nameof(InsertOrUpdateDocumentAsync),
            requestOptions,
            new InsertOrUpdateDocumentContextItem(id, conflictResolveFunc),
            _processExceptionDoc,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<IReadOnlyList<TDocument>>> QueryDocumentsAsync(
        QueryRequestOptions<TDocument> requestOptions,
        Query query,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.QueryDocumentsAsync(
                (context.RequestOptions as QueryRequestOptions<TDocument>)!,
                context.GetItemOf<Query>(),
                cancellationToken),
            nameof(QueryDocumentsAsync),
            requestOptions,
            query,
            _processExceptionDocList,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<TDocument>> ReadDocumentAsync(
        RequestOptions<TDocument> requestOptions,
        string id,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync((context, _, cancellationToken) => base.ReadDocumentAsync(
                (context.RequestOptions as RequestOptions<TDocument>)!,
                context.GetItemOf<string>(),
                cancellationToken),
            nameof(ReadDocumentAsync),
            requestOptions,
            id,
            _processExceptionDoc,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<TDocument>> PatchDocumentAsync(
        RequestOptions<TDocument> requestOptions,
        string id,
        IReadOnlyList<PatchOperation> patchOperations,
        string? filter,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync((context, _, cancellationToken) =>
            {
                var patchDocumentContext = context.GetItemOf<PatchDocumentContextItem>();

                return base.PatchDocumentAsync(
                    (context.RequestOptions as RequestOptions<TDocument>)!,
                    patchDocumentContext.Id,
                    patchDocumentContext.PatchOperations,
                    patchDocumentContext.Filter,
                    cancellationToken);
            },
            nameof(ReadDocumentAsync),
            requestOptions,
            new PatchDocumentContextItem(id, patchOperations, filter),
            _processExceptionDoc,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<TDocument>> ReplaceDocumentAsync(
        RequestOptions<TDocument> requestOptions,
        string id,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.ReplaceDocumentAsync(
                (context.RequestOptions as RequestOptions<TDocument>)!,
                context.GetItemOf<string>(),
                cancellationToken),
            nameof(ReplaceDocumentAsync),
            requestOptions,
            id,
            _processExceptionDoc,
            cancellationToken);

    /// <inheritdoc/>
    public override Task<IDatabaseResponse<TDocument>> UpsertDocumentAsync(
        RequestOptions<TDocument> requestOptions,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.UpsertDocumentAsync(
                (context.RequestOptions as RequestOptions<TDocument>)!,
                cancellationToken),
            nameof(UpsertDocumentAsync),
            requestOptions,
            null,
            _processExceptionDoc,
            cancellationToken);

    /// <inheritdoc/>
    internal override Task<IDatabaseResponse<bool>> UpdateTableSettingsAsync(
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.UpdateTableSettingsAsync(
                context.RequestOptions,
                cancellationToken),
            nameof(UpdateTableSettingsAsync),
            requestOptions,
            null,
            _processExceptionBool,
            cancellationToken);

    /// <inheritdoc/>
    internal override Task<IDatabaseResponse<TableOptions>> ReadTableSettingsAsync(
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.ReadTableSettingsAsync(
                context.RequestOptions,
                cancellationToken),
            nameof(ReadTableSettingsAsync),
            requestOptions,
            null,
            _processExceptionTableOptions,
            cancellationToken);

    /// <inheritdoc/>
    internal override Task<IDatabaseResponse<TableOptions>> CreateTableAsync(
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.CreateTableAsync(
                context.RequestOptions,
                cancellationToken),
            nameof(CreateTableAsync),
            requestOptions,
            null,
            _processExceptionTableOptions,
            cancellationToken);

    /// <inheritdoc/>
    internal override Task<IDatabaseResponse<TableOptions>> DeleteTableAsync(
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => DecorateOperationCallAsync(
            (context, _, cancellationToken) => base.DeleteTableAsync(
                context.RequestOptions,
                cancellationToken),
            nameof(DeleteTableAsync),
            requestOptions,
            null,
            _processExceptionTableOptions,
            cancellationToken);

    private Task<T> DecorateOperationCallAsync<T>(
        Func<DecoratedCosmosContext, Func<Exception, T>, CancellationToken, Task<T>> operationGetter,
        string operationName,
        RequestOptions options,
        object? item,
        Func<Exception, T> exceptionHandler,
        CancellationToken cancellationToken)
        => _decorator.DoCallAsync(
            operationGetter,
            new(operationName, options, TableOptions, item),
            exceptionHandler,
            cancellationToken);

    private readonly struct InsertOrUpdateDocumentContextItem
    {
        public readonly string Id;
        public readonly Func<TDocument, TDocument> ConflictResolveFunc;

        public InsertOrUpdateDocumentContextItem(string id, Func<TDocument, TDocument> conflictResolveFunc)
        {
            Id = id;
            ConflictResolveFunc = conflictResolveFunc;
        }
    }

    private readonly struct PatchDocumentContextItem
    {
        public readonly string Id;
        public readonly IReadOnlyList<PatchOperation> PatchOperations;
        public readonly string? Filter;

        public PatchDocumentContextItem(string id,
            IReadOnlyList<PatchOperation> patchOperations,
            string? filter)
        {
            Id = id;
            PatchOperations = patchOperations;
            Filter = filter;
        }
    }

    private readonly struct QueryConditionContextItem<TOutputDocument>
    {
        public readonly Func<IQueryable<TDocument>, IQueryable<TOutputDocument>>? Condition;

        public QueryConditionContextItem(Func<IQueryable<TDocument>, IQueryable<TOutputDocument>>? condition)
        {
            Condition = condition;
        }
    }
}
