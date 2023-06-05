// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Polly;
using Xunit;
using PatchOperation = System.Cloud.DocumentDb.PatchOperation;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public class ExtensionsTests
{
    [Fact]
    public void TestResponseExtensions()
    {
        IDatabaseResponse<string> response = new CosmosDatabaseResponse<string>(default, HttpStatusCode.Created, "");
        ((HttpStatusCode)response.Status).Should().Be(HttpStatusCode.Created);
        response.Item.Should().Be("");

        Uri endpoint = new Uri("http://localhost/");

        Headers headers = new Headers();

        Mock<ThroughputResponse> throughput = new Mock<ThroughputResponse>();
        throughput.Setup(throughput => throughput.Headers).Returns(headers);

        IDatabaseResponse<bool> documentStorageResponse = throughput.Object.ToDatabaseResponse("table", "region", endpoint);
        documentStorageResponse.Item.Should().BeTrue();

        headers.Add("x-ms-offer-replace-pending", "true");
        documentStorageResponse = throughput.Object.ToDatabaseResponse("table", "region", endpoint);
        documentStorageResponse.Item.Should().BeFalse();

        headers.Set("x-ms-offer-replace-pending", "false");
        documentStorageResponse = throughput.Object.ToDatabaseResponse("table", "region", endpoint);
        documentStorageResponse.Item.Should().BeTrue();

        IDatabaseResponse<string> nullResponse = null!;
        nullResponse.HasStatus(HttpStatusCode.OK).Should().BeFalse();

        var responseWithStatus = response.WithStatus(HttpStatusCode.PartialContent, false);
        responseWithStatus.Status.Should().Be((int)HttpStatusCode.PartialContent);
        responseWithStatus.Succeeded.Should().BeFalse();

        responseWithStatus = new MyResponse<string>().WithStatus(HttpStatusCode.PartialContent, false);
        responseWithStatus.Status.Should().Be((int)HttpStatusCode.PartialContent);
        responseWithStatus.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TestDecoratorExtentsions()
    {
        var result = new ICosmosDecorator<string>[0].SelectDecorators<IOnBeforeCosmosDecorator<string>, string>();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, "etag", "session")]
    [InlineData(false, null, null)]
    [InlineData(true, null, null)]
    [InlineData(false, null, "session")]
    [InlineData(true, null, "session")]
    [InlineData(true, "etag", null)]
    public void TestRequestOptionsExtensions(bool responseOnWrite, string? etag, string? session)
    {
        RequestOptions<TestDocument> requestOptions = new() { ItemVersion = etag, SessionToken = session, ContentResponseOnWrite = responseOnWrite };
        ItemRequestOptions? itemRequestOptions = requestOptions.GetItemRequestOptions();

        Assert.Equal(etag, itemRequestOptions?.IfMatchEtag);
        Assert.Equal(session, itemRequestOptions?.SessionToken);

        Assert.Equal(itemRequestOptions == null ? null : responseOnWrite, itemRequestOptions?.EnableContentResponseOnWrite);

        var exception = Assert.Throws<InvalidDataException>(() => requestOptions.RequirePartitionKey());
        exception.Message.Should().ContainAll("Partition key is null or empty in request.");

        TestRequestOptionsNoEncryption(responseOnWrite, etag, session, requestOptions);
    }

    [Fact]
    public void TestNoDocument()
    {
        RequestOptions<TestDocument> requestOptions = new();
        var exception = Assert.Throws<InvalidDataException>(() => requestOptions.RequireDocument());
        Assert.Equal("Document is required but null.", exception.Message);
    }

    [Fact]
    public void TestToCosmosPatchOperation()
    {
        string path = "/path";
        string stringValue = "sval";
        long longValue = 5;
        double doubleValue = 6;

        Validate(PatchOperation.Add(path, stringValue));
        Validate(PatchOperation.Remove(path));
        Validate(PatchOperation.Replace(path, stringValue));
        Validate(PatchOperation.Set(path, stringValue));
        Validate(PatchOperation.Increment(path, longValue));
        Validate(PatchOperation.Increment(path, doubleValue));

#if false
        PatchOperation invalidOperation = new PatchOperation((PatchOperationType)100, "path", null!);
        var exception = Assert.Throws<ArgumentException>(() => invalidOperation.ToCosmosPatchOperation());
        exception.Message.Should().Contain("Provided value is invalid: ");

        invalidOperation = new PatchOperation(PatchOperationType.Increment, "path", null!);
        exception = Assert.Throws<ArgumentException>(() => invalidOperation.ToCosmosPatchOperation());
        exception.Message.Should().Be("Increment value should be either long or double.");
#endif
        var l = new List<PatchOperation>();
        l.ToCosmosPatchOperations().Should().BeEmpty();
    }

    private static void Validate(PatchOperation operation)
    {
        IReadOnlyList<PatchOperation> patchOperations = new[] { operation };

        var result = patchOperations.ToCosmosPatchOperations();

        result.Count.Should().Be(1);
        result[0].Path.Should().Be(operation.Path);
        ((int)result[0].OperationType).Should().Be((int)operation.OperationType);
    }

    private static void TestRequestOptionsNoEncryption(bool responseOnWrite, string? etag, string? session, System.Cloud.DocumentDb.RequestOptions requestOptions)
    {
        ItemRequestOptions? writeItemRequestOptions = requestOptions.GetItemRequestOptions();

        if (string.IsNullOrWhiteSpace(etag) && string.IsNullOrWhiteSpace(session) && responseOnWrite)
        {
            writeItemRequestOptions.Should().BeNull();
        }
        else
        {
            writeItemRequestOptions.Should().NotBeNull();
        }

        Assert.Equal(etag, writeItemRequestOptions?.IfMatchEtag);
        Assert.Equal(session, writeItemRequestOptions?.SessionToken);
        Assert.Equal(writeItemRequestOptions == null ? null : responseOnWrite, writeItemRequestOptions?.EnableContentResponseOnWrite);

        TransactionalBatchItemRequestOptions? transactionalItemRequestOptions = requestOptions.GetTransactionalRequestOptions();

        if (string.IsNullOrWhiteSpace(etag) && responseOnWrite)
        {
            transactionalItemRequestOptions.Should().BeNull();
        }
        else
        {
            transactionalItemRequestOptions.Should().NotBeNull();
        }

        Assert.Equal(etag, transactionalItemRequestOptions?.IfMatchEtag);
        Assert.Equal(transactionalItemRequestOptions == null ? null : responseOnWrite, transactionalItemRequestOptions?.EnableContentResponseOnWrite);
    }

    [Fact]
    public void TestNullRequestOptions()
    {
        RequestOptions<int> requestOptions = new() { ContentResponseOnWrite = true };
        requestOptions.GetItemRequestOptions().Should().BeNull();
        requestOptions.GetTransactionalRequestOptions().Should().BeNull();
        requestOptions.GetPartitionKey().Should().BeNull();
    }

    [Fact]
    public void TestQueryRequestOptions()
    {
        var options = new QueryRequestOptions<int>();

        Assert.Null(options.ResponseContinuationTokenLimitInKb);
        Assert.Null(options.EnableScan);
        Assert.Null(options.EnableLowPrecisionOrderBy);
        Assert.Null(options.MaxBufferedItemCount);
        Assert.Null(options.MaxResults);
        Assert.Null(options.MaxConcurrency);
        Assert.Equal(0, options.Document);

        options = new QueryRequestOptions<int>
        {
            ResponseContinuationTokenLimitInKb = 1,
            EnableScan = true,
            EnableLowPrecisionOrderBy = true,
            MaxBufferedItemCount = 2,
            MaxResults = 3,
            MaxConcurrency = 4,
            Document = 5,
        };

        Assert.Equal(1, options.ResponseContinuationTokenLimitInKb);
        Assert.Equal(true, options.EnableScan);
        Assert.Equal(true, options.EnableLowPrecisionOrderBy);
        Assert.Equal(2, options.MaxBufferedItemCount);
        Assert.Equal(3, options.MaxResults);
        Assert.Equal(4, options.MaxConcurrency);
        Assert.Equal(5, options.Document);
    }

    [Theory]
    [InlineData(true, "region")]
    [InlineData(true, "")]
    [InlineData(true, null)]
    [InlineData(false, "region")]
    [InlineData(false, "")]
    [InlineData(false, null)]
    public void TestValidate(bool regional, string? region)
    {
        TableInfo options = new TableInfo(new TableOptions { IsRegional = regional });
        System.Cloud.DocumentDb.RequestOptions request = region != null ? new() { Region = region } : null!;

        bool hasRegion = !string.IsNullOrEmpty(region);
        bool valid = (hasRegion && regional) || (!hasRegion && !regional);

        if (valid)
        {
            // should not throw
            options.ValidateRequest(request);
        }
        else
        {
            _ = Assert.Throws<DatabaseClientException>(() => options.ValidateRequest(request));
        }
    }

    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(false, true, true, false)]
    [InlineData(false, true, false, false)]

    // for config exists = false, just one combination needed.
    [InlineData(false, false, false, false)]
    public void TestServiceBuilderExtensions(bool servicesAdded, bool configExists, bool generic, bool useContextForOptions)
    {
        IServiceCollection services = new ServiceCollection();

        var builder = services.GetCosmosDatabaseBuilder();

        if (servicesAdded)
        {
            if (generic)
            {
                builder
                    .EnableEncryption<MyEncryption>()
                    .EnableTableLocator<MyLocator>()
                    .EnableResilience(_ => Mock.Of<IAsyncPolicy>())
                    .AddDecorator<MyDecorator>();
            }
            else
            {
                builder
                    .EnableEncryption(_ => Mock.Of<ICosmosEncryptionProvider>())
                    .EnableTableLocator(_ => Mock.Of<ITableLocator>())
                    .EnableResilience(_ => Mock.Of<IAsyncPolicy>())
                    .AddDecorator(_ => Mock.Of<ICosmosDecorator<DecoratedCosmosContext>>());
            }
        }

        if (generic)
        {
            builder
                .Configure<ExtensionTestsDatabaseOptions>()
                .BuildDatabase<ExtensionTestsDatabaseOptions>()
                .ConfigureTable<MyTableOptions>()
                .AddReader<TestDocument>()
                .TableConfigurer // this and next calls are not mandatory, called to test the switch
                .ConfigureTable<MyTableOptions>()
                .AddWriter<TestDocument>()
                .DatabaseBuilder
                .Should().Equals(builder);
        }
        else
        {
            builder
                .Configure(useContextForOptions ? "context" : null)
                .BuildDatabase()
                .ConfigureTable<TableOptions>(useContextForOptions ? "context" : null)
                .AddReader<TestDocument>()
                .AddWriter<TestDocument>();
        }

        if (!configExists)
        {
            ServiceProvider provider = services.BuildServiceProvider();

            // Database creation should fail first
            Assert.Throws<ValidationException>(() => provider.GetRequiredService<IDocumentDatabase>());
            return;
        }

        TestGettingAdapter(services, servicesAdded, generic, useContextForOptions);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void TestConfigDisabiguity(bool defineSimple, bool defineGeneric1, bool defineGeneric2)
    {
        IServiceCollection services = new ServiceCollection();
        DatabaseOptions options = TestCosmosAdapter.CreateDatabaseOptions(nameof(TestConfigDisabiguity));

        if (defineSimple)
        {
            services.GetCosmosDatabaseBuilder()
                .Configure()
                .BuildDatabase();
            services.Configure(SupplyConfig<DatabaseOptions>(options));
        }

        if (defineGeneric1)
        {
            services.GetCosmosDatabaseBuilder()
                .Configure<ExtensionTestsDatabaseOptions>()
                .BuildDatabase<ExtensionTestsDatabaseOptions>();
            services.Configure(SupplyConfig<ExtensionTestsDatabaseOptions>(options));
        }

        if (defineGeneric2)
        {
            services.GetCosmosDatabaseBuilder()
                .Configure<TestQueryRequestOptionsDatabaseOptions>()
                .BuildDatabase<TestQueryRequestOptionsDatabaseOptions>();
            services.Configure(SupplyConfig<TestQueryRequestOptionsDatabaseOptions>(options));
        }

        ServiceProvider provider = services.BuildServiceProvider();

        TestGettingAdapter<IDocumentDatabase>(provider, defineSimple);
        TestGettingAdapter<IDocumentDatabase<ExtensionTestsDatabaseOptions>>(provider, defineGeneric1);
        TestGettingAdapter<IDocumentDatabase<TestQueryRequestOptionsDatabaseOptions>>(provider, defineGeneric2);
    }

    [Fact]
    public void PartitionKeyTest()
    {
        VerifyPk(null!);
        VerifyPk(new object?[] { null });
        VerifyPk(new object?[] { "123" });
        VerifyPk(new[] { CosmosConstants.NoPartitionKey });
        VerifyPk(new object?[] { true });
        VerifyPk(new object?[] { 123D });
        VerifyPk(new object?[] { 123 });
        VerifyPk(new object?[] { null, null });
        VerifyPk(new object?[] { "123", "4567" });
        VerifyPk(new object?[] { "123", 4567D, 123, true, null, CosmosConstants.NoPartitionKey });

        VerifyPkException(new object?[] { 'a' });
        VerifyPkException(new object?[] { new object[] { } });
        VerifyPkException(new object?[] { new List<int>() });
        VerifyPkException(new object?[] { "valid", new object() });
    }

    private static void VerifyPkException(IReadOnlyList<object?> components)
    {
        Exception exception = Assert.Throws<ArgumentException>(() => VerifyPk(components));
        Assert.StartsWith("Partition key components can be string, double, bool, null or CosmosTableOptions.NONEPK values only, got",
            exception.Message);
    }

    private static void VerifyPk(IReadOnlyList<object?> components)
    {
        System.Cloud.DocumentDb.RequestOptions options = new() { PartitionKey = components };
        PartitionKey? key = options.GetPartitionKey();

        string? skey =

            // ToString throws internal exception for this specific constant.
            // Despite works fine if it is in multicomponent key.
            key == PartitionKey.None ? "{}"
            : key?.ToString();

        if (components == null!)
        {
            Assert.Null(skey);
        }
        else
        {
            foreach (object? obj in components)
            {
                if (obj == CosmosConstants.NoPartitionKey)
                {
                    Assert.Contains("{}", skey);
                }
                else
                {
                    Assert.Contains($"{obj}".ToLowerInvariant(), skey);
                }
            }
        }
    }

    private static void TestGettingAdapter<T>(IServiceProvider provider, bool defined)
        where T : class
    {
        if (defined)
        {
            provider.GetRequiredService<T>().Should().NotBeNull();
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<T>());
        }
    }

    private static void TestGettingAdapter(IServiceCollection services, bool servicesAdded, bool generic, bool useContextForOptions)
    {
        DatabaseOptions options = TestCosmosAdapter.CreateDatabaseOptions(nameof(TestServiceBuilderExtensions));
        TableOptions tableOptions = TestCosmosClient.GetContainerOptions(nameof(TestServiceBuilderExtensions));

        if (generic)
        {
            services.Configure(SupplyConfig<ExtensionTestsDatabaseOptions>(options));
            services.Configure(SupplyTableConfig<MyTableOptions>(tableOptions));
        }
        else
        {
            if (useContextForOptions)
            {
                services.Configure("context", SupplyConfig<DatabaseOptions>(options));
                services.Configure("context", SupplyTableConfig<TableOptions>(tableOptions));
            }
            else
            {
                services.Configure(SupplyConfig<DatabaseOptions>(options));
                services.Configure(SupplyTableConfig<TableOptions>(tableOptions));
            }
        }

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        IDocumentDatabase adapter;

        if (generic)
        {
            adapter = serviceProvider.GetRequiredService<IDocumentDatabase<ExtensionTestsDatabaseOptions>>();
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IDocumentDatabase>());
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IDocumentDatabase<TestQueryRequestOptionsDatabaseOptions>>());
            TestAdapter<ExtensionTestsDatabaseOptions>(servicesAdded, adapter);
        }
        else
        {
            adapter = serviceProvider.GetRequiredService<IDocumentDatabase>();
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IDocumentDatabase<ExtensionsTests>>());
            TestAdapter<DatabaseBuilder>(servicesAdded, adapter);
        }

        IDocumentReader<TestDocument> reader = serviceProvider.GetRequiredService<IDocumentReader<TestDocument>>();
        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IDocumentReader<int>>());

        IDocumentWriter<TestDocument> writer = serviceProvider.GetRequiredService<IDocumentWriter<TestDocument>>();
        Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IDocumentWriter<int>>());
    }

    private static void TestAdapter<T>(bool servicesAdded, IDocumentDatabase adapter)
        where T : class
    {
        adapter.Should().NotBeNull();

        CosmosDocumentDatabase<T> clientAdapter = (CosmosDocumentDatabase<T>)adapter;

        if (servicesAdded)
        {
            clientAdapter.CosmosEncryptionProvider.Should().NotBeNull();
            clientAdapter.TableLocator.Should().NotBeNull();
        }
        else
        {
            clientAdapter.CosmosEncryptionProvider.Should().BeNull();
            clientAdapter.TableLocator.Should().BeNull();
        }
    }

    private static Action<T> SupplyConfig<T>(DatabaseOptions options)
        where T : DatabaseOptions
    {
        return databaseOptions =>
        {
            databaseOptions.DatabaseName = options.DatabaseName;
            databaseOptions.DefaultRegionalDatabaseName = options.DefaultRegionalDatabaseName;
            databaseOptions.Endpoint = options.Endpoint;
            databaseOptions.FailoverRegions = options.FailoverRegions;
            databaseOptions.IdleTcpConnectionTimeout = options.IdleTcpConnectionTimeout;
            databaseOptions.JsonSerializerOptions = options.JsonSerializerOptions;
            databaseOptions.PrimaryKey = options.PrimaryKey;
            databaseOptions.RegionalDatabaseOptions = options.RegionalDatabaseOptions;
        };
    }

    private static Action<T> SupplyTableConfig<T>(TableOptions options)
        where T : TableOptions
    {
        return tableOptions =>
        {
            tableOptions.TableName = options.TableName;
            tableOptions.IsRegional = options.IsRegional;
            tableOptions.Throughput = options.Throughput;
            tableOptions.TimeToLive = options.TimeToLive;
            tableOptions.PartitionIdPath = options.PartitionIdPath;
        };
    }

    private class MyTableOptions : TableOptions
    {
    }

    private class ExtensionTestsDatabaseOptions : DatabaseOptions
    {
        // empty
    }

    private class TestQueryRequestOptionsDatabaseOptions : DatabaseOptions
    {
        // empty
    }

    private class MyLocator : ITableLocator
    {
        public TableInfo? LocateTable(in TableInfo options, System.Cloud.DocumentDb.RequestOptions request)
        {
            return null;
        }
    }

    private class MyEncryption : ICosmosEncryptionProvider
    {
        public Task ConfigureEncryptionForContainerAsync(Container container, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<TOptions?> GetEncryptionItemRequestOptionsAsync<TDocument, TOptions>(
            System.Cloud.DocumentDb.RequestOptions requestOptions, TOptions? itemRequestOptions, Uri cosmosEndpointUri, TDocument document, CancellationToken cancellationToken)
            where TDocument : notnull
            where TOptions : Azure.Cosmos.RequestOptions
        {
            return Task.FromResult<TOptions?>(default);
        }

        public FeedIterator ToEncryptionStreamIterator<TDocument>(Container container, IQueryable<TDocument> queryable)
        {
            return null!;
        }
    }

    private class MyDecorator : IOnBeforeCosmosDecorator<DecoratedCosmosContext>
    {
        public void OnBefore(DecoratedCosmosContext context)
        {
            // empty
        }
    }

    private class MyResponse<T> : IDatabaseResponse<T>
        where T : notnull
    {
        public int Status { get; set; }

        public T? Item { get; set; }

        public RequestInfo RequestInfo { get; set; }

        public string? ItemVersion { get; set; }

        public bool Succeeded { get; set; }

        public string? ContinuationToken { get; set; }

        public int ItemCount { get; set; }
    }
}
