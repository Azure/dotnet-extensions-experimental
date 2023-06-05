// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Contains Cosmos DB related constants.
/// </summary>
[Experimental]
public static class CosmosConstants
{
    /// <summary>
    /// The none value constant to be used for partition key.
    /// </summary>
    /// <remarks>
    /// This constant should be used when one of the partition key components has no value.
    /// </remarks>
    [Experimental]
    public static readonly object NoPartitionKey = new();
}
