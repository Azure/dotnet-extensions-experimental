// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Document.Cosmos.Model;

/// <summary>
/// The interface defining custom response properties Cosmos DB provides.
/// </summary>
public interface ICosmosDatabaseResponse
{
    /// <summary>
    /// Gets the raw response from database.
    /// </summary>
    /// <remarks>
    /// This object gives an access to raw database response data, which could be useful for diagnostic purposes.
    /// </remarks>
    object? RawResponse { get; }
}
