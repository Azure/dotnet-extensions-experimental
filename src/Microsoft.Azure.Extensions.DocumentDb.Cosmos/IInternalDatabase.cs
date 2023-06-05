// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The interface to describe internal database operations.
/// </summary>
internal interface IInternalDatabase
{
    /// <summary>
    /// Gets Cosmos container for table and request.
    /// </summary>
    /// <param name="table">The table information.</param>
    /// <param name="request">The request information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Cosmos DB container.</returns>
    Task<CosmosTable> GetContainerAsync(
        TableInfo table,
        RequestOptions request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets configured list of region names.
    /// </summary>
    /// <returns>The configured region names.</returns>
    IEnumerable<string> ConfiguredRegions { get; }
}
