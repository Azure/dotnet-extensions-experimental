// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

internal class TestCosmosClient
{
    public Mock<Container> MockContainer { get; } = new();
    public Mock<CosmosClient> MockClient { get; } = new();
    public Mock<ContainerResponse> MockContainerResponse { get; } = new();
    public Mock<IInternalDatabase> MockAdapter { get; } = new();
    public Mock<Azure.Cosmos.Database> MockDatabase { get; } = new();
    public CosmosDatabase CosmosDatabase { get; }
    public TableOptions TableOptions { get; }

    public TestCosmosClient(
        DatabaseOptions databaseOptions,
        TableOptions containerOptions,
        ICosmosEncryptionProvider? encryption = null)
    {
        TableOptions = containerOptions;
        MockContainer.Setup(container => container.Database).Returns(MockDatabase.Object);
        MockDatabase.Setup(database => database.Client).Returns(MockClient.Object);
        CosmosDatabase = new(MockDatabase.Object, encryption, new CosmosDatabaseConfiguration(databaseOptions));

        MockAdapter.Setup(adapter => adapter.GetContainerAsync(
            It.IsAny<TableInfo>(), It.IsAny<RequestOptions<TestDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CosmosTable(CosmosDatabase, MockContainer.Object, new TableInfo(TableOptions)));
    }

    public BaseCosmosDocumentClient<TestDocument> CreateCosmosClient()
    {
        return new(TableOptions, MockAdapter.Object);
    }

    public DecoratedCosmosClient<TestDocument> CreateDecoratedCosmosClient(ICallDecorationPipeline<DecoratedCosmosContext> decorator)
    {
        return new(TableOptions, MockAdapter.Object, decorator);
    }

    public void MockReadItem(TestDocument document, HttpStatusCode code = HttpStatusCode.OK, bool returnDocument = false)
    {
        MockContainer.Setup(container => container.ReadItemAsync<TestDocument>(
            document.Id, new PartitionKey(document.User), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(TestCosmosResponse<TestDocument>.Create(returnDocument ? document.GetDocument() : null, code)));
    }

    public void MockCreateItem(TestDocument document, HttpStatusCode code = HttpStatusCode.OK, bool returnDocument = false)
    {
        MockContainer.Setup(container => container.CreateItemAsync(
            document, new PartitionKey(document.User), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(TestCosmosResponse<TestDocument>.Create(returnDocument ? document : null, code)));
    }

    public void MockReplaceItem(TestDocument document, HttpStatusCode code = HttpStatusCode.OK, bool returnDocument = false)
    {
        MockContainer.Setup(container => container.ReplaceItemAsync(
            document, document.Id, new PartitionKey(document.User), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(TestCosmosResponse<TestDocument>.Create(returnDocument ? document : null, code)));
    }

    public void MockReadThroughput(int? result = null)
    {
        MockContainerResponse.Setup(response => response.StatusCode)
            .Returns(HttpStatusCode.OK);

        MockContainer.Setup(container => container.ReadContainerAsync(
            It.IsAny<ContainerRequestOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(MockContainerResponse.Object));

        MockContainer.Setup(container => container.ReadThroughputAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(result));
    }

    internal static TableOptions GetContainerOptions(string testName, string? region = null)
    {
        return GetContainerOptions<TableOptions>(testName, region);
    }

    internal static T GetContainerOptions<T>(string testName, string? region = null)
        where T : TableOptions, new()
    {
        // While testing different platforms, same tests should coexist.
        long timeSuffix = DateTime.UtcNow.Ticks;

        return new()
        {
            TableName = $"{testName}-Container-{timeSuffix}",
            TimeToLive = TimeSpan.FromMinutes(11),
            PartitionIdPath = "/partition",
            IsRegional = region != null
        };
    }

    internal static Task<BaseCosmosDocumentClient<TestDocument>> CreateAndVerifyClientAsync(
        DatabaseOptions options, TableOptions containerOptions,
        bool createDatabases = true,
        ICosmosEncryptionProvider? cosmosEncryption = null)
    {
        return CreateAndVerifyClientAsync<TestDocument>(options, containerOptions, createDatabases, cosmosEncryption);
    }

    internal static async Task<BaseCosmosDocumentClient<T>> CreateAndVerifyClientAsync<T>(
        DatabaseOptions options, TableOptions containerOptions,
        bool createDatabases = true,
        ICosmosEncryptionProvider? cosmosEncryption = null,
        bool createContainer = false)
        where T : notnull
    {
        IHost fakeHost = FakeHost.CreateBuilder()
            .ConfigureServices((_, services) =>
            {
                var databaseBuilder = (DatabaseBuilder)services.GetCosmosDatabaseBuilder();

                if (createDatabases || createContainer)
                {
                    databaseBuilder.EnableResilience(
                        _ => Policy
                            .Handle<DatabaseRetryableException>()
                            .RetryAsync(20));
                }

                if (cosmosEncryption != null)
                {
                    databaseBuilder.EnableEncryption(_ => cosmosEncryption);
                }

                if (createDatabases)
                {
                    Assert.False(databaseBuilder.CreateMissingDatabases);
                    databaseBuilder.CreateDatabaseIfNotExists();
                    Assert.True(databaseBuilder.CreateMissingDatabases);
                }

                var tableConfigurer = databaseBuilder.Configure(_ => options)
                    .BuildDatabase();

                if (createContainer)
                {
                    tableConfigurer.CreateTableIfNotExists();
                }

                tableConfigurer.ConfigureTable(_ => containerOptions)
                    .AddReader<T>();
            }).Build();

        fakeHost.Start();

        var provider = fakeHost.Services;
        var database = provider.GetRequiredService<IDocumentDatabase>();
        var reader = provider.GetRequiredService<IDocumentReader<T>>();

        try
        {
            Assert.IsAssignableFrom<BaseCosmosDocumentClient<T>>(reader);
            return (BaseCosmosDocumentClient<T>)reader;
        }
        catch
        {
            if (createDatabases)
            {
                // delete databases in a case of uncontrolled exception happened (possibly striker flows)
                await database.DeleteDatabaseAsync(CancellationToken.None);
            }

            throw;
        }
    }

    internal static Task<BaseCosmosDocumentClient<TestDocument>> CreateAndVerifyClientAsync(
        string testName, TableOptions containerOptions,
        bool createDatabases = true)
    {
        return CreateAndVerifyClientAsync(TestCosmosAdapter.CreateDatabaseOptions(testName),
            containerOptions, createDatabases);
    }

    internal static Task<BaseCosmosDocumentClient<TestDocument>> CreateAndVerifyClientAsync(
        string testName,
        string? region = null,
        bool createDatabases = true)
    {
        TableOptions containerOptions = GetContainerOptions(testName, region);
        return CreateAndVerifyClientAsync(testName, containerOptions, createDatabases);
    }
}
