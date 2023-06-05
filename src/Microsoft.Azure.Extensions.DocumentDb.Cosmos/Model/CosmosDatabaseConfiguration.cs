// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Document.Cosmos.Model;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

internal sealed class CosmosDatabaseConfiguration
{
    /// <summary>
    /// Gets the global database name.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="string.Empty" />.
    /// The value is required.
    /// </remarks>
    internal string DatabaseName { get; }

    /// <summary>
    /// Gets the key to the account or resource token.
    /// </summary>
    internal string PrimaryKey { get; }

    /// <summary>
    /// Gets the database endpoint uri.
    /// </summary>
    internal Uri Endpoint { get; }

    /// <summary>
    /// Gets timeout before unused connection will be closed.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// By default, idle connections should be kept open indefinitely.
    /// Value must be greater than or equal to 10 minutes.
    /// Recommended values are between 20 minutes and 24 hours.
    /// Mainly useful for sparse infrequent access to a large database account.
    /// Works for all global and regional connections.
    /// </remarks>
    [TimeSpan("00:10:00", "30.00:00:00")]
    internal TimeSpan? IdleTcpConnectionTimeout { get; }

    /// <summary>
    /// Gets a list of preferred regions used for SDK to define failover order for global database.
    /// </summary>
    internal IReadOnlyList<string> FailoverRegions => _failoverRegions;

    /// <summary>
    /// Gets a connection key.
    /// </summary>
    internal ConnectionKey ConnectionKey { get; }

    /// <summary>
    /// Gets a value indicating whether the connection should use gateway mode or not.
    /// </summary>
    internal bool EnableGatewayMode { get; }

    /// <summary>
    /// Gets a value indicating whether to enable private port pool for TCP connections.
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.documents.client.connectionpolicy.portreusemode" />.
    /// </remarks>
    internal bool EnablePrivatePortPool { get; }

    /// <summary>
    /// Gets a value indicating whether TCP endpoint rediscovery should be enabled for CosmosDB connection.
    /// </summary>
    internal bool EnableTcpEndpointRediscovery { get; }

    /// <summary>
    /// Gets a cosmos serializer.
    /// </summary>
    internal CosmosSerializer CosmosSerializer { get; }

    /// <summary>
    /// Gets a value of database throughput.
    /// </summary>
    /// <remarks>
    /// If provided this value will be used on database creation to set the throughput limit.
    /// <seealso cref="DatabaseOptions.Throughput"/>.
    /// </remarks>
    internal int? Throughput { get; }

    private readonly string[] _failoverRegions;

    internal CosmosDatabaseConfiguration(DatabaseOptions options)
        : this(options,
              InternalThrows.IfNullOrWhitespace(options?.DatabaseName, "DatabaseName field is null or empty."),
              InternalThrows.IfNull(options.Endpoint, "Endpoint field is null or empty."),
              options.FailoverRegions.ToArray(),
              options.PrimaryKey)
    {
    }

#pragma warning disable S3236 // Caller information arguments should not be provided explicitly
    internal CosmosDatabaseConfiguration(string region, DatabaseOptions options, RegionalDatabaseOptions regionalOptions)
        : this(options,
              InternalThrows.IfNullOrWhitespace(regionalOptions.DatabaseName ?? options.DefaultRegionalDatabaseName,
                  $"DatabaseName field is null or empty for region [{region}]."),
              InternalThrows.IfNull(regionalOptions.Endpoint, $"Endpount field is null for region [{region}]."),
              regionalOptions.FailoverRegions.ToArray(),
              regionalOptions.PrimaryKey ?? options.PrimaryKey)
    {
    }

    internal CosmosDatabaseConfiguration(DatabaseOptions options, string database, Uri endpoint, string[] failoverRegions, string? primaryKey)
    {
        DatabaseName = database;
        PrimaryKey = InternalThrows.IfNullOrWhitespace(primaryKey, $"Primary key is null or empty for {endpoint.OriginalString}.");
        Endpoint = endpoint;
        IdleTcpConnectionTimeout = options.IdleTcpConnectionTimeout;
        Throughput = options.Throughput.Value;

        ConnectionKey = new ConnectionKey(Endpoint.ToString(), DatabaseName);

        _failoverRegions = failoverRegions;

        CosmosDatabaseOptions? cosmosOptions = options as CosmosDatabaseOptions;

        EnableGatewayMode = cosmosOptions?.EnableGatewayMode ?? true;
        EnablePrivatePortPool = cosmosOptions?.EnablePrivatePortPool ?? true;
        EnableTcpEndpointRediscovery = cosmosOptions?.EnableTcpEndpointRediscovery ?? true;

        CosmosSerializer = new CosmosSystemTextJsonSerializer(options.JsonSerializerOptions);
    }

    internal static CosmosDatabaseConfiguration GetGlobalConfiguration(DatabaseOptions options)
    {
        return new CosmosDatabaseConfiguration(options);
    }

    internal static IReadOnlyDictionary<string, CosmosDatabaseConfiguration> GetRegionalConfigurations(DatabaseOptions options)
    {
        Dictionary<string, CosmosDatabaseConfiguration> dictionary = new(options.RegionalDatabaseOptions.Count);

        foreach (KeyValuePair<string, RegionalDatabaseOptions> entry in options.RegionalDatabaseOptions)
        {
            RegionalDatabaseOptions regionalOptions = InternalThrows.IfNull(entry.Value, $"Region [{entry.Key}] is not configured.");

            CosmosDatabaseConfiguration configuration = new(entry.Key, options, regionalOptions);
            dictionary.Add(entry.Key, configuration);
        }

        IReadOnlyDictionary<string, CosmosDatabaseConfiguration> result = dictionary;
        return result.EmptyIfNull();
    }
#pragma warning restore S3236 // Caller information arguments should not be provided explicitly
}
