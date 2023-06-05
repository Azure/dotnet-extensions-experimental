// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

internal readonly struct CosmosTable
{
    public CosmosDatabase Database { get; }
    public Container Container { get; }
    public TableInfo Options { get; }

    public CosmosTable(CosmosDatabase database, Container container, TableInfo options)
    {
        Database = database;
        Container = container;
        Options = options;
    }
}
