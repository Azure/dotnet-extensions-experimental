// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The interface for defining cosmos container options.
/// </summary>
/// <remarks>
/// This class extends <see cref="TableOptions"/> with Cosmos DB related policies.
/// The policies will be used when creating a container using API <see cref="IDocumentDatabase.CreateTableAsync"/>.
/// </remarks>
public class CosmosTableOptions : TableOptions
{
    /// <summary>
    /// Gets or sets a value indicating cosmos indexing policy.
    /// </summary>
    public IndexingPolicy? IndexingPolicy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating cosmos unique key policy.
    /// </summary>
    public UniqueKeyPolicy? UniqueKeyPolicy { get; set; }
}
