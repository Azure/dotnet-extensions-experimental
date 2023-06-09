// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Microsoft.Shared.Diagnostics;
using RequestOptions = System.Cloud.DocumentDb.RequestOptions;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Implementation of <see cref="IDocumentDatabase"/> for Cosmos DB.
/// </summary>
/// <typeparam name="TContext">The context type, to be used for targeted DIs.</typeparam>
internal sealed class CosmosDocumentDatabase<TContext> : IDocumentDatabase<TContext>, IInternalDatabase
    where TContext : class
{
    internal const string CosmosGatewayAddress = "sqlx.cosmos.azure.com";
    internal const string CosmosDbTracesTypeName = "Microsoft.Azure.Cosmos.Core.Trace.DefaultTrace,Microsoft.Azure.Cosmos.Direct";
    internal static readonly string RequireLocatorError = @$"Table locator is required for the table {0} but not configured.
If you are using service provider to configure application, please inject an instance of {nameof(ITableLocator)}.
Otherwise use {nameof(DatabaseBuilder.EnableTableLocator)} to configure the locator.";

    /// <summary>
    /// The dictionary for cosmos connections.
    /// </summary>
    /// <remarks>
    /// Connections are heavyweight and can be shared across threads without locks.
    /// Do not declaring a separate internal connection manager,
    /// since it is adapter responsibility to manage connections.
    /// Also no other components will access this data.
    /// Declared it in lazy way to avoid concerns of creating twice on adding to dictionary.
    /// </remarks>
    internal static readonly ConcurrentDictionary<string, Task<CosmosClient>> EndpointUriToClientDict = new();

    internal static readonly SemaphoreSlim ClientCreationSemaphore = new(1, 1);
    internal readonly CosmosDatabaseConfiguration GlobalConfig;
    internal readonly IReadOnlyDictionary<string, CosmosDatabaseConfiguration> RegionalConfigs;
    internal readonly ICosmosEncryptionProvider? CosmosEncryptionProvider;
    internal readonly ConcurrentDictionary<ConnectionKey, Task<CosmosDatabase>> Databases = new();
    internal readonly ICallDecorationPipeline<DecoratedCosmosContext>? ClientDecorators;
    internal readonly ITableLocator? TableLocator;

    private static readonly RequestOptions<int> _emptyOptions = new();

    /// <inheritdoc/>
    public Task<IDatabaseResponse<TableOptions>> ReadTableSettingsAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => GetBaseClient<object>(tableOptions)
            .ReadTableSettingsAsync(requestOptions, cancellationToken);

    /// <inheritdoc/>
    public Task<IDatabaseResponse<bool>> UpdateTableSettingsAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => GetBaseClient<object>(tableOptions)
            .UpdateTableSettingsAsync(requestOptions, cancellationToken);

    /// <inheritdoc/>
    public Task<IDatabaseResponse<TableOptions>> CreateTableAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => GetBaseClient<object>(tableOptions)
            .CreateTableAsync(requestOptions, cancellationToken);

    /// <inheritdoc/>
    public Task<IDatabaseResponse<TableOptions>> DeleteTableAsync(
        TableOptions tableOptions,
        RequestOptions requestOptions,
        CancellationToken cancellationToken)
        => GetBaseClient<object>(tableOptions)
            .DeleteTableAsync(requestOptions, cancellationToken);

    public async Task<CosmosTable> GetContainerAsync(
        TableInfo table,
        RequestOptions request,
        CancellationToken cancellationToken)
    {
        table.ValidateRequest(request);

        if (table.IsLocatorRequired)
        {
            table = RequireTableLocator(table.TableName).LocateTable(table, request) ?? table;

            // Since parameters could be overridden, validate request again.
            table.ValidateRequest(request);
        }

        CosmosDatabase database = await GetDatabaseAsync(table, request, cancellationToken).ConfigureAwait(false);
        Container container = database.GetContainer(table.TableName);

        if (CosmosEncryptionProvider != null)
        {
            await CosmosEncryptionProvider
                .ConfigureEncryptionForContainerAsync(container, cancellationToken)
                .ConfigureAwait(false);
        }

        return new(database, container, table);
    }

    /// <summary>
    /// Method creates cosmos database from provided proxy.
    /// </summary>
    /// <param name="database">The cosmos database proxy.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Task"/>.</returns>
    internal static Task CreateIfNotExistsAsync(CosmosDatabase database, CancellationToken cancellationToken)
    {
        return database.Database.Client.CreateDatabaseIfNotExistsAsync(
            database.Database.Id, database.Configuration.Throughput, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// This method clean up system trace listeners created by Cosmos Client on creation.
    /// </summary>
    /// <remarks>
    /// Those listeners are not a lock free at Cosmos, and generating lock contentions across threads when any listener assigned.
    /// The issue is, even if a customer not assigns any listeners, .Net SDK adds one default listener.
    /// That is why those should be cleaned up in all cases.
    /// [Cosmos Issue 892](https://github.com/Azure/azure-cosmos-dotnet-v3/issues/892).
    /// [Cosmos Issue 2240](https://github.com/Azure/azure-cosmos-dotnet-v3/issues/2240).
    /// [MS Teams investigation results](https://domoreexp.visualstudio.com/MSTeams/_workitems/edit/2088401/).
    /// How cosmos team removes them internally for own [benchmark tests]
    /// (https://github.com/Azure/azure-cosmos-dotnet-v3/blob/88f3f4314dabde8a6eda5d72e0380fd59da12883/Microsoft.Azure.Cosmos.Samples/Tools/Benchmark/Program.cs#L246-L252).
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Microsoft.Azure.Cosmos.Core.Trace.DefaultTrace", "Microsoft.Azure.Cosmos.Direct")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    internal static void ClearTraceListeners(string typeName)
    {
        Type? defaultTrace = Type.GetType(typeName);
        TraceSource? traceSource = (TraceSource?)defaultTrace?.GetProperty("TraceSource")?.GetValue(null);

        if (traceSource != null)
        {
            traceSource.Switch.Level = SourceLevels.All;

            // Listeners can not be null.
            traceSource.Listeners.Clear();
        }
    }

    internal ITableLocator RequireTableLocator(string table)
    {
        if (TableLocator is null)
        {
            CosmosThrow.DatabaseClientException(
                string.Format(CultureInfo.InvariantCulture, RequireLocatorError, table));
        }

        return TableLocator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDocumentDatabase{T}"/> class.
    /// </summary>
    /// <param name="options">The database options.</param>
    /// <param name="cosmosAppLevelEncryption">The ALE encryption implementation.</param>
    /// <param name="clientDecorators">The client decorators.</param>
    /// <param name="containerLocator">The container locator.</param>
    internal CosmosDocumentDatabase(DatabaseOptions options,
        ICosmosEncryptionProvider? cosmosAppLevelEncryption = null,
        IReadOnlyList<ICosmosDecorator<DecoratedCosmosContext>>? clientDecorators = null,
        ITableLocator? containerLocator = null)
    {
        GlobalConfig = CosmosDatabaseConfiguration.GetGlobalConfiguration(options);
        RegionalConfigs = CosmosDatabaseConfiguration.GetRegionalConfigurations(options);
        CosmosEncryptionProvider = cosmosAppLevelEncryption;

        TableLocator = containerLocator;

        if (clientDecorators?.Count > 0)
        {
            ClientDecorators = clientDecorators.MakeCallDecorationPipeline();
        }
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(bool createIfNotExists, CancellationToken cancellationToken)
    {
        CosmosDatabase database = await GetOrCreateDatabaseConnectionAsync(GlobalConfig, cancellationToken)
            .ConfigureAwait(false);

        if (createIfNotExists)
        {
            await CreateIfNotExistsAsync(database, cancellationToken).ConfigureAwait(false);
        }

        foreach (KeyValuePair<string, CosmosDatabaseConfiguration> regionalOptions in RegionalConfigs)
        {
            database = await GetOrCreateDatabaseConnectionAsync(regionalOptions.Value, cancellationToken)
                .ConfigureAwait(false);

            if (createIfNotExists)
            {
                await CreateIfNotExistsAsync(database, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc/>
    public IDocumentReader<TDocument> GetDocumentReader<TDocument>(
        TableOptions options)
        where TDocument : notnull
        => GetBaseClient<TDocument>(options);

    /// <inheritdoc/>
    public IDocumentWriter<TDocument> GetDocumentWriter<TDocument>(
        TableOptions options)
        where TDocument : notnull
        => GetBaseClient<TDocument>(options);

    /// <inheritdoc/>
    public async Task<IDatabaseResponse<bool>> DeleteDatabaseAsync(CancellationToken cancellationToken)
    {
        foreach (KeyValuePair<ConnectionKey, Task<CosmosDatabase>> databaseTask in Databases)
        {
            CosmosDatabase database = await databaseTask.Value.ConfigureAwait(false);

            if (ClientDecorators != null)
            {
                var cosmosContext = new DecoratedCosmosContext(
                    nameof(DeleteDatabaseAsync),
                    _emptyOptions,
                    null,
                    database);

                // Decorate the incoming call.
                _ = await ClientDecorators.DoCallAsync((context, _, cancellationToken) =>
                        context.GetItemOf<CosmosDatabase>()
                            .Database.DeleteAsync(cancellationToken: cancellationToken),
                        cosmosContext,
                        _exceptionHandler,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                _ = await database.Database.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        return new CosmosDatabaseResponse<bool>(default, HttpStatusCode.OK, true);
    }

    private static readonly Func<Exception, DatabaseResponse> _exceptionHandler = ExceptionHandler;

    public IEnumerable<string> ConfiguredRegions => RegionalConfigs.Keys;

    internal static DatabaseResponse ExceptionHandler(Exception exception) => throw exception;

    /// <summary>
    /// Create the client connect to Cosmos DB account.
    /// </summary>
    /// <remarks>
    /// If called multiple times, only 1 of results can be awaited, to avoid overheads.
    /// </remarks>
    /// <param name="config">The cosmos database configuration.</param>
    private static CosmosClient CreateCosmosClient(CosmosDatabaseConfiguration config)
    {
        PortReuseMode portReuseMode = config.EnablePrivatePortPool
                ? PortReuseMode.PrivatePortPool : PortReuseMode.ReuseUnicastPort;

        string endpoint = config.Endpoint.ToString();
        CosmosClientBuilder clientBuilder = new CosmosClientBuilder(endpoint, config.PrimaryKey)
            .WithApplicationPreferredRegions(config.FailoverRegions);

        if (config.EnableGatewayMode
#if NETCOREAPP3_1_OR_GREATER
                && endpoint.Contains(CosmosGatewayAddress, StringComparison.Ordinal))
#else
                && endpoint.Contains(CosmosGatewayAddress))
#endif
        {
            clientBuilder = clientBuilder
                .WithConnectionModeGateway()
                .WithConsistencyLevel(Azure.Cosmos.ConsistencyLevel.Eventual);
        }
        else
        {
            // The client will use Tcp protocol on direct mode by default.
            clientBuilder = clientBuilder
                .WithConnectionModeDirect(
                    config.IdleTcpConnectionTimeout,
                    portReuseMode: portReuseMode,
                    enableTcpConnectionEndpointRediscovery: config.EnableTcpEndpointRediscovery);
        }

        if (config.CosmosSerializer != null)
        {
            clientBuilder = clientBuilder.WithCustomSerializer(config.CosmosSerializer);
        }

        CosmosClient cosmosClient = clientBuilder.Build();

        ClearTraceListeners(CosmosDocumentDatabase<TContext>.CosmosDbTracesTypeName);
        return cosmosClient;
    }

    /// <summary>
    /// Create the client connect to Cosmos DB account.
    /// </summary>
    /// <remarks>
    /// If called multiple times, only 1 of results can be awaited, to avoid overheads.
    /// </remarks>
    /// <param name="config">The cosmos database configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static async Task<CosmosClient> GetOrCreateCosmosClientAsync(
        CosmosDatabaseConfiguration config,
        CancellationToken cancellationToken)
    {
        string uri = config.Endpoint.ToString();
        try
        {
            // The operation under lock is very fast.
            // Lock is required to prevent dictionary to create 2 hot tasks
            // which will start executing even if only 1 placed to dictionary
            // and can lead to memory leak.
            await ClientCreationSemaphore
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            ConfiguredTaskAwaitable<CosmosClient> clientCreationTask;

            try
            {
                clientCreationTask = EndpointUriToClientDict
                    .GetOrAdd(
                        uri,
                        static (_, config) => Task.FromResult(CosmosDocumentDatabase<object>.CreateCosmosClient(config)),
                        config)
                    .ConfigureAwait(false);
            }
            finally
            {
                _ = ClientCreationSemaphore.Release();
            }

            // Do the longest part outside of sync semaphore.
            // After first GetOrAdd operation completed, concurrent dictionary guarantee second creation not happens,
            // and returns the same task, which can be safely awaited concurrently.
            CosmosClient cosmosClient = await clientCreationTask;

            return cosmosClient;
        }
        catch (Exception)
        {
            // Remove failed task from dictionary, so on retry it can be recreated.
            _ = EndpointUriToClientDict.TryRemove(uri, out _);
            throw;
        }
    }

    private BaseCosmosDocumentClient<TDocument> GetBaseClient<TDocument>(TableOptions options)
        where TDocument : notnull
    {
        options = Throw.IfNull(options);

        if (options.IsLocatorRequired)
        {
            // Verify locator is configured, if not - fail fast before the client actually used.
            _ = RequireTableLocator(options.TableName);
        }

        return ClientDecorators != null
            ? new DecoratedCosmosClient<TDocument>(options, this, ClientDecorators)
            : new BaseCosmosDocumentClient<TDocument>(options, this);
    }

    /// <summary>
    /// Gets cosmos database.
    /// </summary>
    /// <param name="containerOptions">The container configuration.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A <see cref="Task{CosmosDatabase}"/> representing <see cref="CosmosDatabase"/>.</returns>
    private Task<CosmosDatabase> GetDatabaseAsync(
        TableInfo containerOptions,
        RequestOptions request,
        CancellationToken cancellationToken)
    {
        return containerOptions.IsRegional
            ? GetRegionalDatabaseAsync(request.Region!, cancellationToken)
            : GetOrCreateDatabaseConnectionAsync(GlobalConfig, cancellationToken);
    }

    /// <summary>
    /// Get regional database.
    /// </summary>
    /// <param name="region">Indicates the specific database location to connect to. This location
    /// must have a database account and is not necessarily the same location as the node's location.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{CosmosDatabase}"/> representing <see cref="CosmosDatabase"/>.</returns>
    private Task<CosmosDatabase> GetRegionalDatabaseAsync(string region, CancellationToken cancellationToken)
    {
        if (!RegionalConfigs.TryGetValue(region, out CosmosDatabaseConfiguration? config))
        {
            CosmosThrow.DatabaseClientException($"Region [{region}] is not configured.");
        }

        return GetOrCreateDatabaseConnectionAsync(config, cancellationToken);
    }

    /// <summary>
    /// Gets or creates cosmos database proxy.
    /// </summary>
    /// <param name="config">The cosmos database config.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>The <see cref="Task{CosmosDatabase}"/> representing <see cref="CosmosDatabase"/>.</returns>
    private async Task<CosmosDatabase> GetOrCreateDatabaseConnectionAsync(
        CosmosDatabaseConfiguration config,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Databases.GetOrAdd(
                config.ConnectionKey,
                static (_, t) => t.clientAdapter.CreateDatabaseConnectionAsync(t.config, t.cancellationToken),
                (clientAdapter: this, config, cancellationToken)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Remove failed task from dictionary, so on retry it can be recreated.
            _ = Databases.TryRemove(config.ConnectionKey, out _);
            throw;
        }
    }

    /// <summary>
    /// Creates cosmos database proxy.
    /// </summary>
    /// <param name="config">The Cosmos database config.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="Task{CosmosDatabase}"/>.</returns>
    private async Task<CosmosDatabase> CreateDatabaseConnectionAsync(
        CosmosDatabaseConfiguration config,
        CancellationToken cancellationToken)
    {
        CosmosClient cosmosClient = await GetOrCreateCosmosClientAsync(config, cancellationToken)
            .ConfigureAwait(false);

        CosmosDatabase cosmosDatabase = new(cosmosClient.GetDatabase(config.DatabaseName), CosmosEncryptionProvider, config);
        return cosmosDatabase;
    }
}
