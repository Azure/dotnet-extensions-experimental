// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Extensions.Document.Cosmos.Model;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The result interface for document storage responses.
/// </summary>
/// <typeparam name="T">The type of the item the response contains.</typeparam>
internal sealed class CosmosDatabaseResponse<T> : IDatabaseResponse<T>, ICosmosDatabaseResponse
    where T : notnull
{
    internal static bool IsSucceeded(HttpStatusCode statusCode)
    {
        return statusCode >= HttpStatusCode.OK // code 200
            && statusCode < HttpStatusCode.MultipleChoices; // code 300
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Do not want to pack attributes into a dummy struct.")]
    internal CosmosDatabaseResponse(
        RequestInfo requestInfo,
        HttpStatusCode statusCode,
        T? item,
        string? itemVersion = null,
        string? continuationToken = null,
        bool? succeededOverride = null,
        object? rawResponse = null,
        int? itemCount = null)
    {
        RequestInfo = requestInfo;
        Status = (int)statusCode;
        Item = item;
        ItemCount = Item != null! ? itemCount ?? 1 : 0;
        ItemVersion = itemVersion;
        ContinuationToken = continuationToken;
        Succeeded = succeededOverride ?? IsSucceeded(statusCode);
        RawResponse = rawResponse;
    }

    internal static CosmosDatabaseResponse<IReadOnlyList<TListItem>> MakeResponseFromList<TListItem>(
        RequestInfo requestInfo,
        HttpStatusCode statusCode,
        IReadOnlyList<TListItem> item,
        string? continuationToken = null,
        object? rawResponse = null)
    {
        return new(requestInfo, statusCode, item,
            continuationToken: continuationToken,
            rawResponse: rawResponse,
            itemCount: item.Count);
    }

    /// <summary>
    /// Gets the response status code.
    /// </summary>
    /// <remarks>
    /// This code is the <see cref="HttpStatusCode"/> for Cosmos DB.
    /// </remarks>
    public int Status { get; }

    /// <inheritdoc/>
    public T? Item { get; }

    /// <inheritdoc/>
    public RequestInfo RequestInfo { get; }

    /// <inheritdoc/>
    public string? ItemVersion { get; }

    /// <inheritdoc/>
    public bool Succeeded { get; }

    /// <inheritdoc/>
    public string? ContinuationToken { get; }

    /// <inheritdoc/>
    public object? RawResponse { get; }

    /// <inheritdoc/>
    public int ItemCount { get; }
}
