// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Model;

/// <summary>
/// The class representing configuration extensions specific to Cosmos database.
/// </summary>
public class CosmosDatabaseOptions : DatabaseOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection should use gateway mode or not.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true"/>, means connector will use gateway mode.
    /// </remarks>
    public bool EnableGatewayMode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable private port pool for TCP connections.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true"/>.
    /// <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.client.connectionpolicy.portreusemode" />.
    /// </remarks>
    public bool EnablePrivatePortPool { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether TCP endpoint rediscovery should be enabled for connection.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true"/>.
    /// </remarks>
    /// <value>
    /// True if the TCP endpoint rediscovery enabled. False otherwise.
    /// </value>
    public bool EnableTcpEndpointRediscovery { get; set; } = true;
}
