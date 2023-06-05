// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Moq;
using Xunit;
using ExtensionsDocument = System.Cloud.DocumentDb;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

[Collection(DocumentCosmosTestConstants.TestCollectionName)]
public class CosmosClientAdapterTests
{
    private const string CosmosGatewayAddress = CosmosDocumentDatabase<CosmosClientAdapterTests>.CosmosGatewayAddress;
    private readonly RequestOptions<TestDocument> _requestOptions = new();

    [Fact]
    public async Task GetClientAsyncTest()
    {
        var client = await TestCosmosClient.CreateAndVerifyClientAsync(
            nameof(GetClientAsyncTest), (string?)null, false);

        // Try create container method, it should fail. Since database does not exists yet.
        await client.CreateTableAsync(_requestOptions, CancellationToken.None)
            .TestFailure(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClientAsyncInvertedOptionsTest()
    {
        var testName = nameof(GetClientAsyncInvertedOptionsTest);

        var options = TestCosmosAdapter.CreateDatabaseOptions(testName);
        options.EnableGatewayMode = false;
        options.EnablePrivatePortPool = false;
        options.EnableTcpEndpointRediscovery = false;

        var containerOptions = TestCosmosClient.GetContainerOptions(testName);

        var client = await TestCosmosClient.CreateAndVerifyClientAsync(options, containerOptions, false);

        options.FailoverRegions = ImmutableList<string>.Empty;
        client = await TestCosmosClient.CreateAndVerifyClientAsync(options, containerOptions, false);
    }

    [Fact]
    public async Task ClientTypeTest()
    {
        var testName = nameof(ClientTypeTest);
        DatabaseOptions options = TestCosmosAdapter.CreateDatabaseOptions(testName);
        TableOptions containerOptions = TestCosmosClient.GetContainerOptions(testName, TestCosmosAdapter.TestRegion);

        // No decorators
        IDocumentDatabase adapter = new CosmosDocumentDatabase<CosmosClientAdapterTests>(options,
            clientDecorators: new List<ICosmosDecorator<DecoratedCosmosContext>>());
        await using TestDisposableResources<TestDocument> cleanup = new(adapter);

        await adapter.ConnectAsync(true, CancellationToken.None);
        var client = adapter.GetDocumentReader<TestDocument>(containerOptions);
        client.Should().NotBeAssignableTo<DecoratedCosmosClient<TestDocument>>();

        // Decorators present
        options = TestCosmosAdapter.CreateDatabaseOptions(testName + "2");
        adapter = new CosmosDocumentDatabase<CosmosClientAdapterTests>(options,
            clientDecorators: new List<ICosmosDecorator<DecoratedCosmosContext>> { new CosmosExceptionHandlingDecorator() });
        await using TestDisposableResources<TestDocument> cleanup2 = new(adapter);

        await adapter.ConnectAsync(true, CancellationToken.None);

        client = adapter.GetDocumentReader<TestDocument>(containerOptions);
        client.Should().BeAssignableTo<DecoratedCosmosClient<TestDocument>>();

        var writeClient = adapter.GetDocumentWriter<TestDocument>(containerOptions);
        writeClient.Should().BeAssignableTo<DecoratedCosmosClient<TestDocument>>();
    }

    [Fact]
    public async Task GetRegionalClientAsyncTest()
    {
        const string TestName = nameof(GetRegionalClientAsyncTest);

        var databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(TestName);
        var tableOptions = TestCosmosClient.GetContainerOptions(TestName);

        var database = TestCosmosAdapter.CreateCosmosAdapter(databaseOptions);

        await using TestDisposableResources<TestDocument> cleanup = new(database);

        await database.ConnectAsync(true, CancellationToken.None);

        // not regional
        tableOptions.IsRegional = false;
        var tableInfo = new TableInfo(tableOptions);

        var client = (BaseCosmosDocumentClient<TestDocument>)database.GetDocumentReader<TestDocument>(tableOptions);

        _ = await Assert.ThrowsAsync<DatabaseClientException>(() => client.Database.GetContainerAsync(
            tableInfo,
            new() { Region = TestCosmosAdapter.TestRegion },
            default));

        var container = await client.Database.GetContainerAsync(
            tableInfo,
            _requestOptions,
            default);
        container.Database.Configuration.Endpoint.AbsoluteUri.Should().NotContain(TestCosmosAdapter.TestRegion);

        tableOptions.IsRegional = true;
        tableInfo = new TableInfo(tableOptions);

        client = (BaseCosmosDocumentClient<TestDocument>)database.GetDocumentReader<TestDocument>(tableOptions);

        _ = await Assert.ThrowsAsync<DatabaseClientException>(
            () => client.Database.GetContainerAsync(
                tableInfo, new() { Region = "" }, default));

        // regional
        client = (BaseCosmosDocumentClient<TestDocument>)database.GetDocumentReader<TestDocument>(tableOptions);

        container = await client.Database.GetContainerAsync(
            tableInfo,
            new() { Region = TestCosmosAdapter.TestRegion },
            default);
        container.Database.Configuration.Endpoint.AbsoluteUri.Should().Contain(TestCosmosAdapter.TestRegion);
    }

    [Fact]
    public void ExceptionHandlerTest()
    {
        DatabaseClientException exception = new();
        Assert.Throws<DatabaseClientException>(() => CosmosDocumentDatabase<TestDocument>.ExceptionHandler(exception));
    }

    [Fact]
    public void RegionalParametersTest()
    {
        var testName = nameof(RegionalParametersTest);
        var databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);

        var adapter = TestCosmosAdapter.CreateCosmosAdapter(databaseOptions);
        adapter.RegionalConfigs.Count.Should().Be(1);
        CosmosDatabaseConfiguration cosmosConfig = adapter.RegionalConfigs[TestCosmosAdapter.TestRegion];

        cosmosConfig.EnableGatewayMode.Should().Be(databaseOptions.EnableGatewayMode);
        cosmosConfig.EnableTcpEndpointRediscovery.Should().Be(databaseOptions.EnableTcpEndpointRediscovery);
        cosmosConfig.EnablePrivatePortPool.Should().Be(databaseOptions.EnablePrivatePortPool);

        cosmosConfig.EnableGatewayMode.Should().Be(databaseOptions.EnableGatewayMode);
        cosmosConfig.EnableTcpEndpointRediscovery.Should().Be(databaseOptions.EnableTcpEndpointRediscovery);
        cosmosConfig.EnablePrivatePortPool.Should().Be(databaseOptions.EnablePrivatePortPool);
    }

    [Fact]
    public async Task GlobalDatabaseMissedParametersTest()
    {
        var testName = nameof(GlobalDatabaseMissedParametersTest);
        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);

        TableOptions containerOptions = TestCosmosClient.GetContainerOptions(testName);

        Uri? endpoint = databaseOptions.Endpoint;
        databaseOptions.Endpoint = null;
        _ = await Assert.ThrowsAsync<InvalidDataException>(
            () => TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions));
        databaseOptions.Endpoint = endpoint;

        string? pk = databaseOptions.PrimaryKey;
        databaseOptions.PrimaryKey = null;
        await ExpectExceptionAsync<InvalidDataException>(
            () => TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions),
            "Primary key is null or empty for https://");

        databaseOptions.PrimaryKey = pk;

        // create client with endpoint, but delete after

        IDocumentDatabase adapter = TestCosmosAdapter.CreateCosmosAdapter(databaseOptions);
        await using TestDisposableResources<TestDocument> cleanup = new(adapter);
        await adapter.ConnectAsync(true, CancellationToken.None);
    }

    private static async Task ExpectExceptionAsync<T>(Func<Task> testCode, string expectedMessage)
        where T : Exception
    {
        T exception = await Assert.ThrowsAsync<T>(testCode);
        exception.Message.Should().Contain(expectedMessage);
    }

    [Fact]
    public async Task RegionalDatabaseMissedParametersTest()
    {
        var testName = nameof(RegionalDatabaseMissedParametersTest);
        const string TestRegion = TestCosmosAdapter.TestRegion;

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);
        TableOptions containerOptions = TestCosmosClient.GetContainerOptions(testName, TestRegion);

        RegionalDatabaseOptions regionalDatabaseOptions = databaseOptions.RegionalDatabaseOptions[TestRegion];

        Uri? endpoint = regionalDatabaseOptions.Endpoint;

        // test client can not be created when endpoint is null
        regionalDatabaseOptions.Endpoint = null;
        await ExpectExceptionAsync<InvalidDataException>(
            () => TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false),
            $"Endpount field is null for region [{TestRegion}]");
        regionalDatabaseOptions.Endpoint = endpoint;

        // Both regional database name and default name is null
        string? database = regionalDatabaseOptions.DatabaseName;

        await TestRegionalDatabaseNullOrWhitespace(databaseOptions, containerOptions, regionalDatabaseOptions, null, null, TestRegion);
        await TestRegionalDatabaseNullOrWhitespace(databaseOptions, containerOptions, regionalDatabaseOptions, " ", null, TestRegion);
        await TestRegionalDatabaseNullOrWhitespace(databaseOptions, containerOptions, regionalDatabaseOptions, null, " ", TestRegion);

        // one of them is not null
        regionalDatabaseOptions.DatabaseName = database;
        databaseOptions.DefaultRegionalDatabaseName = null;

        await TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false);

        regionalDatabaseOptions.DatabaseName = null;
        databaseOptions.DefaultRegionalDatabaseName = database;

        await TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false);

        // Failover list is not a list
        regionalDatabaseOptions.FailoverRegions = ImmutableList<string>.Empty;
        await TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false);

        // Failover list is not a list
        databaseOptions.RegionalDatabaseOptions = new Dictionary<string, RegionalDatabaseOptions>();
        var client = await TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false);

        await ExpectExceptionAsync<DatabaseClientException>(
            () => client.Database.GetContainerAsync(new TableInfo(containerOptions), new() { Region = TestRegion }, default),
            $"Region [{TestRegion}] is not configured.");

        containerOptions.IsRegional = false;
        await TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false);
        databaseOptions.RegionalDatabaseOptions.Add(TestRegion, null!);
        containerOptions.IsRegional = true;

        await ExpectExceptionAsync<InvalidDataException>(
            () => TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false),
            $"Region [{TestRegion}] is not configured.");
    }

    private static async Task TestRegionalDatabaseNullOrWhitespace(
        DatabaseOptions databaseOptions,
        TableOptions containerOptions,
        RegionalDatabaseOptions regionalDatabaseOptions,
        string? databaseName, string? defaultName, string region)
    {
        regionalDatabaseOptions.DatabaseName = databaseName;
        databaseOptions.DefaultRegionalDatabaseName = defaultName;

        await ExpectExceptionAsync<InvalidDataException>(
            () => TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions, createDatabases: false),
            $"DatabaseName field is null or empty for region [{region}].");
    }

    [Fact]
    public async Task BaseDatabaseOptionsTest()
    {
        var testName = nameof(BaseDatabaseOptionsTest);
        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);
        TableOptions containerOptions = TestCosmosClient.GetContainerOptions(testName);

        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions);
        await client.DeleteDatabaseAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GatewayModeTest()
    {
        var testName = nameof(GatewayModeTest);

        var databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);

        databaseOptions.Endpoint = GetEndpointForTest(testName);
        Assert.True(databaseOptions.EnableGatewayMode);

        // EnableGatewayMode is not enough, endpoint should be right
        await TestGatewayMode(testName, databaseOptions, ConnectionMode.Direct);

        // Update the enpoint containing gateway marker.
        databaseOptions.Endpoint = new Uri($"https://localhost:8081/?m={testName}&a={CosmosGatewayAddress}");
        await TestGatewayMode(testName, databaseOptions, ConnectionMode.Gateway);

        databaseOptions.EnableGatewayMode = false;
        databaseOptions.Endpoint = new Uri($"https://localhost:8081/?m={testName}&b={CosmosGatewayAddress}");
        await TestGatewayMode(testName, databaseOptions, ConnectionMode.Direct);
    }

    [Fact]
    public async Task EncryptionTest()
    {
        var testName = nameof(EncryptionTest);
        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);
        TableOptions containerOptions = TestCosmosClient.GetContainerOptions(testName);
        databaseOptions.Endpoint = GetEndpointForTest(testName);

        Mock<ICosmosEncryptionProvider> encryptionProvider = new();

        encryptionProvider.Setup(ale => ale.ConfigureEncryptionForContainerAsync(It.IsAny<Container>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        IDocumentDatabase adapter = TestCosmosAdapter.CreateCosmosAdapter(databaseOptions, cosmosEncryption: encryptionProvider.Object);

        await using TestDisposableResources<TestDocument> cleanup = new(adapter);

        await adapter.ConnectAsync(true, CancellationToken.None);
        var client = adapter.GetDocumentReader<TestDocument>(containerOptions);
        Assert.IsAssignableFrom<BaseCosmosDocumentClient<TestDocument>>(client);

        BaseCosmosDocumentClient<TestDocument> cosmosClient = (BaseCosmosDocumentClient<TestDocument>)client;

        var container = await cosmosClient.Database.GetContainerAsync(new TableInfo(containerOptions), _requestOptions, CancellationToken.None);

        container.Database.CosmosEncryptionProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task EncryptionExceptionTest()
    {
        var testName = nameof(EncryptionExceptionTest);
        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);
        databaseOptions.Endpoint = GetEndpointForTest(testName);

        TableOptions options = TestCosmosClient.GetContainerOptions(testName);

        Mock<ICosmosEncryptionProvider> encryptionProvider = new Mock<ICosmosEncryptionProvider>();
        Exception exception = new DatabaseClientException("some error");
        encryptionProvider.Setup(ale => ale.ConfigureEncryptionForContainerAsync(It.IsAny<Container>(), default))
            .Throws(exception);

        IDocumentDatabase adapter = TestCosmosAdapter.CreateCosmosAdapter(databaseOptions, cosmosEncryption: encryptionProvider.Object);
        await using TestDisposableResources<TestDocument> cleanup = new(adapter);

        await adapter.ConnectAsync(true, CancellationToken.None);
        _ = await Assert.ThrowsAsync<DatabaseClientException>(() => adapter.CreateTableAsync(options, new(), default));
    }

    [Fact]
    public async Task DecoratorsTestAsync()
    {
        await DecoratorsTestAsyncFor(null);
        await DecoratorsTestAsyncFor(new ICosmosDecorator<DecoratedCosmosContext>[0]);
        await DecoratorsTestAsyncFor(new ICosmosDecorator<DecoratedCosmosContext>[] { new TestDecorator() });
        await DecoratorsTestAsyncFor(new ICosmosDecorator<DecoratedCosmosContext>[] { new TestDecorator(), new TestDecorator() });
    }

    private class TestDecorator : ICosmosDecorator<DecoratedCosmosContext>
    {
    }

    private static async Task DecoratorsTestAsyncFor(IReadOnlyList<ICosmosDecorator<DecoratedCosmosContext>>? decorators)
    {
        DatabaseOptions options = TestCosmosAdapter.CreateDatabaseOptions(nameof(DecoratorsTestAsyncFor));
        TableOptions containerOptions = TestCosmosClient.GetContainerOptions(nameof(DecoratorsTestAsyncFor));
        var adapter = new CosmosDocumentDatabase<CosmosClientAdapterTests>(options, null, decorators);
        await using TestDisposableResources<TestDocument> cleanup = new(adapter);
        await adapter.ConnectAsync(true, CancellationToken.None);
        var client = adapter.GetDocumentReader<TestDocument>(containerOptions);

        if (decorators != null && decorators.Count > 0)
        {
            adapter.ClientDecorators.Should().NotBeNull();
            client.Should().BeAssignableTo<DecoratedCosmosClient<TestDocument>>();
        }
        else
        {
            adapter.ClientDecorators.Should().BeNull();
            client.Should().BeAssignableTo<BaseCosmosDocumentClient<TestDocument>>();
            client.Should().NotBeAssignableTo<DecoratedCosmosClient<TestDocument>>();
        }
    }

    private static Uri GetEndpointForTest(string testName)
    {
        return new Uri($"https://localhost:8081/?m={testName}");
    }

    private async Task TestGatewayMode(string testName, DatabaseOptions databaseOptions, ConnectionMode expectedMode)
    {
        TableOptions containerOptions = TestCosmosClient.GetContainerOptions(testName);
        var client = await TestCosmosClient.CreateAndVerifyClientAsync(databaseOptions, containerOptions);
        var container = await client.Database.GetContainerAsync(new TableInfo(containerOptions), _requestOptions, CancellationToken.None);

        Assert.Equal(expectedMode, container.Database.Database.Client.ClientOptions.ConnectionMode);

        await client.DeleteDatabaseAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetClientAndInitializeFalseTest()
    {
        var client = await TestCosmosClient.CreateAndVerifyClientAsync(
            nameof(GetClientAndInitializeFalseTest),
            (string?)null, false);

        // Connections initialized, database does not exists. Still fails.
        await client.CreateTableAsync(_requestOptions, CancellationToken.None)
            .TestFailure(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClientAndInitializeDatabasesTest()
    {
        const string TestName = nameof(GetClientAndInitializeDatabasesTest);
        var tableOptions = TestCosmosClient.GetContainerOptions(TestName);
        var adapter = TestCosmosAdapter.CreateCosmosAdapter(nameof(GetClientAndInitializeDatabasesTest));

        await using TestDisposableResources<TestDocument> cleanup = new(adapter);
        await adapter.ConnectAsync(true, default);

        IDatabaseResponse<TableOptions> result = await adapter.CreateTableAsync(tableOptions, _requestOptions, CancellationToken.None);
        Assert.True(result.Succeeded);

        result = await adapter.DeleteTableAsync(tableOptions, _requestOptions, CancellationToken.None);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void GetCosmosConsistencyLevelTest()
    {
        Assert.Equal(Azure.Cosmos.ConsistencyLevel.Strong, RequestOptionsExtensions.ToCosmosConsistencyLevel(ExtensionsDocument.ConsistencyLevel.Strong));
        Assert.Equal(Azure.Cosmos.ConsistencyLevel.BoundedStaleness, RequestOptionsExtensions.ToCosmosConsistencyLevel(ExtensionsDocument.ConsistencyLevel.BoundedStaleness));
        Assert.Equal(Azure.Cosmos.ConsistencyLevel.Session, RequestOptionsExtensions.ToCosmosConsistencyLevel(ExtensionsDocument.ConsistencyLevel.Session));
        Assert.Equal(Azure.Cosmos.ConsistencyLevel.Eventual, RequestOptionsExtensions.ToCosmosConsistencyLevel(ExtensionsDocument.ConsistencyLevel.Eventual));
        Assert.Equal(Azure.Cosmos.ConsistencyLevel.ConsistentPrefix, RequestOptionsExtensions.ToCosmosConsistencyLevel(ExtensionsDocument.ConsistencyLevel.ConsistentPrefix));
        Assert.Null(((ExtensionsDocument.ConsistencyLevel?)null).ToCosmosConsistencyLevel());
    }

    private static class EmptyTraces
    {
        public static TraceSource? TraceSource { get; set; }
    }

    [Fact]
    public void ClearTracesTest()
    {
        string typeName = CosmosDocumentDatabase<string>.CosmosDbTracesTypeName;
        CosmosDocumentDatabase<string>.ClearTraceListeners(typeName);
        CosmosDocumentDatabase<string>.ClearTraceListeners(typeName);

        Type? defaultTrace = Type.GetType(typeName);
        TraceSource? traceSource = (TraceSource?)defaultTrace?.GetProperty("TraceSource")?.GetValue(null);

        traceSource.Should().NotBeNull();

        traceSource!.Switch.Level = SourceLevels.ActivityTracing;
        traceSource!.Listeners.Add(new Mock<TraceListener>().Object);

        CosmosDocumentDatabase<string>.ClearTraceListeners(typeName);
        traceSource!.Switch.Level.Should().Be(SourceLevels.All);
        traceSource!.Listeners.Count.Should().Be(0);

        // cleanup something not existent.
        CosmosDocumentDatabase<string>.ClearTraceListeners("ThereAreNoSuchType");

        // try existent class, just no TraceSource attributes
        CosmosDocumentDatabase<string>.ClearTraceListeners(typeof(Type)!.AssemblyQualifiedName!);

        CosmosDocumentDatabase<string>.ClearTraceListeners(typeof(EmptyTraces)!.AssemblyQualifiedName!);

        EmptyTraces.TraceSource = new("name");

        CosmosDocumentDatabase<string>.ClearTraceListeners(typeof(EmptyTraces)!.AssemblyQualifiedName!);
    }

    private class ExceptionEncryption : Exception, ICosmosEncryptionProvider
    {
        public Task ConfigureEncryptionForContainerAsync(Container container, CancellationToken cancellationToken) => throw this;
        public Task<TOptions?> GetEncryptionItemRequestOptionsAsync<TDocument, TOptions>(
            ExtensionsDocument.RequestOptions requestOptions, TOptions? itemRequestOptions,
            Uri cosmosEndpointUri, TDocument document, CancellationToken cancellationToken)
            where TDocument : notnull
            where TOptions : Azure.Cosmos.RequestOptions => throw this;
        public FeedIterator ToEncryptionStreamIterator<TDocument>(Container container, IQueryable<TDocument> queryable) => throw this;
    }

    [Fact]
    public async Task DisposeClientAfterCreatedTest()
    {
        var adapter = TestCosmosAdapter.CreateCosmosAdapter(nameof(DisposeClientAfterCreatedTest), cosmosEncryption: new ExceptionEncryption());
        await using TestDisposableResources<TestDocument> cleanup = new(adapter);

        await adapter.ConnectAsync(true, default);
        _ = await Assert.ThrowsAsync<ExceptionEncryption>(() => adapter.GetContainerAsync(new TableInfo(new() { TableName = "Test" }), _requestOptions, default));
    }

    [Fact]
    public async Task DisposeClientBeforeCreatedTest()
    {
        var options = TestCosmosAdapter.CreateDatabaseOptions(nameof(DisposeClientAfterCreatedTest));
        options.Endpoint = new Uri("", UriKind.Relative);

        var adapter = TestCosmosAdapter.CreateCosmosAdapter(options);
        await using TestDisposableResources<TestDocument> cleanup = new(adapter);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => adapter.ConnectAsync(true, default));
        exception.Message.Should().Contain("accountEndpoint");
    }
}
