// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Moq;
using Polly;
using Xunit;
using PatchOperation = System.Cloud.DocumentDb.PatchOperation;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

[Collection(DocumentCosmosTestConstants.TestCollectionName)]
public class BaseCosmosClientTests
{
    private readonly QueryRequestOptions<TestDocument> _request = new() { ContentResponseOnWrite = true };

    [Fact]
    public async Task ReadContainerFailsWhenNotExistsTest()
    {
        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient.CreateAndVerifyClientAsync(
            nameof(ReadContainerFailsWhenNotExistsTest));

        await using TestDisposableResources<TestDocument> cleanup = new(client);

        await client.ReadTableSettingsAsync(_request, CancellationToken.None)
            .TestFailure(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateContainerTest()
    {
        var testName = nameof(CreateContainerTest);

        TableOptions options = TestCosmosClient.GetContainerOptions(testName);
        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);

        // Created normal container
        await CreateContainerForOptionsTest(databaseOptions, options, deleteAfter: true);
    }

    [Fact]
    public async Task CreateContainerWithPoliciesTest()
    {
        var testName = nameof(CreateContainerWithPoliciesTest);

        var options = TestCosmosClient.GetContainerOptions<CosmosTableOptions>(testName);

        options.IndexingPolicy = new()
        {
            IndexingMode = IndexingMode.Consistent,
            Automatic = true,
        };

        options.UniqueKeyPolicy = new();
        UniqueKey uniqueKey = new UniqueKey();
        uniqueKey.Paths.Add("/uniqueField");
        options.UniqueKeyPolicy.UniqueKeys.Add(uniqueKey);

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);

        // Created normal container
        await CreateContainerForOptionsTest(databaseOptions, options, deleteAfter: true);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(500)]
    public async Task UpdateContainerThroughputTest(int? initialThroughput)
    {
        int defaultThroughput = 400;
        int newThroughput = 600;
        var testName = nameof(UpdateContainerThroughputTest);

        TableOptions options = TestCosmosClient.GetContainerOptions(testName);
        options.Throughput = new(initialThroughput);

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);
        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient.CreateAndVerifyClientAsync(
                        databaseOptions, options);

        IDatabaseResponse<bool> result = await client.UpdateTableSettingsAsync(_request, CancellationToken.None);

        Assert.True(result.HasStatus(HttpStatusCode.NotFound));
        Assert.False(result.Succeeded);
        Assert.False(result.Item);

        var database = await CreateContainerForOptionsTest(databaseOptions, options, initialThroughput);

        // at this point container exists with initial throughput
        await using TestDisposableResources<TestDocument> cleanup = new(database);

        IDatabaseResponse<TableOptions> response = await database.ReadTableSettingsAsync(options, _request, CancellationToken.None);

        Assert.Equal(initialThroughput ?? defaultThroughput, response.Item?.Throughput.Value);
        Assert.True(response.Succeeded);

        options.Throughput = new(newThroughput);
        result = await database.UpdateTableSettingsAsync(options, _request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.True(result.Item);

        response = await database.ReadTableSettingsAsync(options, _request, CancellationToken.None);
        Assert.Equal(newThroughput, response.Item?.Throughput.Value);
        Assert.True(response.Succeeded);
    }

    [Fact]
    public async Task DocumentTest()
    {
        const string TestName = nameof(DocumentTest);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(TestName);

        // at this point container exists with initial throughput
        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient
            .CreateAndVerifyClientAsync<TestDocument>(databaseOptions, options, createContainer: true);
        await using TestDisposableResources<TestDocument> cleanup = new(client);

        TestDocument document = new(TestName);
        TestDocument documentSameId = new(TestName, message: "other message");
        TestDocument documentOtherId = new($"{TestName}2", message: "different message");

        // read document failure
        await TestReadNotFound(client, document);
        await TestReadNotFound(client, documentSameId);
        await TestReadNotFound(client, documentOtherId);

        // create document
        await TestCreateSuccess(client, document);
        await TestCreateFailed(client, document);

        // read document success
        await TestReadSuccess(client, document);

        // upsert document
        await TestUpsertSuccess(client, document);

        // read document success
        await TestReadSuccess(client, document);

        // upsert documentSameId
        await TestUpsertSuccess(client, documentSameId);

        // read documentSameId success
        await TestReadSuccess(client, document, matches: false);
        await TestReadSuccess(client, documentSameId, matches: true);

        // replace documentSameId => document (no id change)
        await TestReplaceFailed(client, document, "bad id");
        await TestReplaceSuccess(client, document, documentSameId.Id!);

        // read document success
        await TestReadSuccess(client, document, matches: true);
        await TestReadSuccess(client, documentSameId, matches: false);

        // replace document => documentOtherId (id will change)
        await TestReplaceSuccess(client, documentOtherId, document.Id!);

        // read verify all
        await TestReadNotFound(client, document);
        await TestReadNotFound(client, documentSameId);
        await TestReadSuccess(client, documentOtherId);

        // create document again, not use upsert to double check no leftovers
        await TestCreateSuccess(client, document);

        // replace documentOtherId => document should fail, since document exists
        await TestReplaceFailed(client, document, documentOtherId.Id!, HttpStatusCode.Conflict);

        // test count
        await TestCount(client, 2);

        // test fetch / query
        await TestFetchSuccess(client, new[] { documentOtherId, document });
        await TestQuerySuccess(client, new[] { documentOtherId, document }, _request);

        await TestQuerySuccess(client, new[] { document },
            new Query("select * from c where c.id = @id",
            new Dictionary<string, string> { { "@id", document.Id! } }));

        await TestFetchSuccess(client, new[] { document },
            null, queryable => queryable.Skip(1));

        // test patch
        await TestPatchSuccess(client, document, "Patched message");
        await TestPatchSuccess(client, document, "Patched message 2", "from c where c.message = 'Patched message'");
        await TestPatchFail(client, document, "Patched message 3", "from c where c.message = 'Patched message'");

        // delete document
        await TestDeleteSuccess(client, document);
        await TestDeleteNotFound(client, document);

        await TestCount(client, 1);
        await TestFetchSuccess(client, new[] { documentOtherId });
        await TestQuerySuccess(client, new[] { documentOtherId }, _request);

        await TestDeleteSuccess(client, documentOtherId);
        await TestDeleteNotFound(client, documentOtherId);

        await TestCount(client, 0);
        await TestFetchSuccess(client, new TestDocument[0]);
        await TestQuerySuccess(client, new TestDocument[0], _request);
    }

    [Theory]
    [InlineData(3, 1)]
    [InlineData(3, 3)]
    [InlineData(3, 4)]
    public async Task TestPartialData(int numDocs, int pageSize)
    {
        try
        {
            await TestPartialDataForCondition(FetchMode.FetchMaxResults, numDocs, pageSize, Math.Min(numDocs, pageSize));
            await TestPartialDataForCondition(FetchMode.FetchSinglePage, numDocs, pageSize, Math.Min(numDocs, pageSize));
            await TestPartialDataForCondition(FetchMode.FetchAll, numDocs, pageSize, numDocs);
        }
        catch (CosmosOperationCanceledException)
        {
            // Test is flaky. Rarely it may happen that CosmosDB emulator "forgets" to respond
            // to the requests being made by the test. When that happens, after a preconfigured
            // time, a CosmosOperationCanceledException is thrown. We are fully ignoring the test
            // result when it happens. The test has many assertions that validate responses if
            // everything works as expected. Note we are not catching all. Just the cancellation.
            //
            // On a different note, all these tests should not be part of unit tests. These are
            // integration tests and should be marked as such. We don't control CosmosDB emulator,
            // it's a black box to us, hence we need to expect it will occasionally fail.
        }
    }

    [Fact]
    public async Task TestInsertOrUpdateMocked()
    {
        const string TestName = nameof(TestInsertOrUpdateMocked);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(TestName);

        TestCosmosClient testClient = new(databaseOptions, options);
        BaseCosmosDocumentClient<TestDocument> client = testClient.CreateCosmosClient();

        TestDocument document = new(TestName, message: "New Message");

        // test null item with status code not NotFound
        testClient.MockReadItem(document);

        await TestInsertException<DatabaseServerException>(client, document, "ReadDocumentAsync operation failed with http code [OK].");

        // test read not found and create failed with conflict
        testClient.MockReadItem(document, HttpStatusCode.NotFound);
        testClient.MockCreateItem(document, HttpStatusCode.Conflict);

        await TestInsertException<DatabaseRetryableException>(client, document, "Item created from another process between read and create. Retry the operation.");

        // test create failed but status code is not conflict
        testClient.MockCreateItem(document, HttpStatusCode.NotFound);

        await TestInsertException<DatabaseServerException>(client, document, "CreateDocumentAsync operation failed with http code [NotFound].");

        // Read Succeed branch
        testClient.MockReadItem(new TestDocument(document.Id!, message: "Old Message"), returnDocument: true);

        // Replace failed due to concurrent update
        testClient.MockReplaceItem(document, HttpStatusCode.PreconditionFailed);

        await TestInsertException<DatabaseRetryableException>(client, document, "Item modified between read and replace by another process. Retry the operation.");
    }

    private async Task TestPartialDataForCondition(FetchMode fetchCondition, int numDocumentsToInsert, int pageSize, int resultSize)
    {
        string testName = $"{nameof(TestPartialDataForCondition)}-{fetchCondition}-{pageSize}";
        TableOptions options = TestCosmosClient.GetContainerOptions(testName);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(testName);
        databaseOptions.RegionalDatabaseOptions.Clear();

        var client = await TestCosmosClient.CreateAndVerifyClientAsync<TestDocument>(databaseOptions, options, createContainer: true);
        await using TestDisposableResources<TestDocument> cleanup = new(client);

        TestDocument[] documentArray = new TestDocument[numDocumentsToInsert];

        for (int i = 0; i < numDocumentsToInsert; i++)
        {
            TestDocument document = new($"{testName}{i}", user: $"{i}");
            documentArray[i] = document;

            await TestCreateSuccess(client, document);
        }

        QueryRequestOptions<TestDocument> query = new()
        {
            MaxResults = pageSize,
            MaxBufferedItemCount = 1,
            MaxConcurrency = 1,
            FetchCondition = fetchCondition
        };

        await TestFetchSuccess(client, documentArray.Take(resultSize).ToArray(), query);
        await TestQuerySuccess(client, documentArray.Take(resultSize).ToArray(), query);
    }

    [Fact]
    public async Task FetchWithMockEncryptionTest()
    {
        const string TestName = nameof(FetchWithMockEncryptionTest);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(TestName);

        Mock<ICosmosEncryptionProvider> mockEncryption = new();

        TestCosmosClient testClient = new(databaseOptions, options, mockEncryption.Object);

        BaseCosmosDocumentClient<TestDocument> client = testClient.CreateCosmosClient();

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.FetchDocumentsAsync<TestDocument>(_request, null, CancellationToken.None));

        exception.Message.Should().StartWith("ToStreamFeedIterator is only supported on cosmos LINQ query operations");

        TestDocument document = TestDocument.GetDefault();

        testClient.MockCreateItem(document, HttpStatusCode.Created, true);

        await TestCreateSuccess(client, document);
    }

    [Fact]
    public async Task FetchContainerThroughputMockedTest()
    {
        const string TestName = nameof(FetchContainerThroughputMockedTest);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(TestName);

        Mock<ICosmosEncryptionProvider> mockEncryption = new();

        TestCosmosClient testClient = new(databaseOptions,
            options, mockEncryption.Object);

        BaseCosmosDocumentClient<TestDocument> client = testClient.CreateCosmosClient();

        testClient.MockReadThroughput();
        var result = await client.ReadTableSettingsAsync(_request, CancellationToken.None);
        result.HasStatus(HttpStatusCode.OK).Should().BeTrue();
        result.Item?.Throughput.Value.Should().BeNull();

        testClient.MockReadThroughput(5);
        result = await client.ReadTableSettingsAsync(_request, CancellationToken.None);
        result.HasStatus(HttpStatusCode.OK).Should().BeTrue();
        result.Item?.Throughput.Value.Should().Be(5);
    }

    [Fact]
    public async Task RegionsNotConfiguredTest()
    {
        const string TestName = nameof(RegionsNotConfiguredTest);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName, TestCosmosAdapter.TestRegion);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateGenericDatabaseOptions(TestName);

        // Remove regional config
        databaseOptions.RegionalDatabaseOptions.Clear();

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(() => TestCosmosClient
            .CreateAndVerifyClientAsync<TestDocument>(databaseOptions, options, createContainer: true));

        Assert.StartsWith("No regions configured for regional table.", exception.Message);

        options.IsRegional = false;
        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient
            .CreateAndVerifyClientAsync<TestDocument>(databaseOptions, options, createContainer: true);

        await using TestDisposableResources<TestDocument> cleanup = new(client);
    }

    [Fact]
    public async Task InsertOrUpdateTest()
    {
        const string TestName = nameof(InsertOrUpdateTest);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateGenericDatabaseOptions(TestName);

        // at this point container exists with initial throughput
        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient
            .CreateAndVerifyClientAsync<TestDocument>(databaseOptions, options, createContainer: true);
        await using TestDisposableResources<TestDocument> cleanup = new(client);

        TestDocument document = new(TestName);
        TestDocument documentSameId = new(TestName, message: "other message");
        TestDocument documentOtherId = new($"{TestName}2", message: "different message");

        // test insert
        await TestInsertOrUpdateSuccess(client, document, document.Id!, document.User!, HttpStatusCode.Created);
        await TestReadSuccess(client, document);

        // test update without id change
        // next 2 skipps updates because resolved object the same as updating or null.
        await TestInsertOrUpdateSuccess(client, document, document.Id!, document.User!, HttpStatusCode.NotModified, t => t);
#pragma warning disable CS8603 // Possible null reference return.
        await TestInsertOrUpdateSuccess(client, document, document.Id!, document.User!, HttpStatusCode.NotModified, t => null);
#pragma warning restore CS8603 // Possible null reference return.
        await TestInsertOrUpdateSuccess(client, documentSameId, document.Id!, document.User!, HttpStatusCode.OK);
        await TestReadSuccess(client, document, matches: false);
        await TestReadSuccess(client, documentSameId, matches: true);

        // test update with id change
        await TestInsertOrUpdateSuccess(client, documentOtherId, documentSameId.Id!, documentSameId.User!, HttpStatusCode.OK);
        await TestReadNotFound(client, document);
        await TestReadNotFound(client, documentSameId);
        await TestReadSuccess(client, documentOtherId);

        // cleanup
        await TestDeleteSuccess(client, documentOtherId);
        await TestDeleteNotFound(client, documentOtherId);
    }

    [Fact]
    public async Task MaxBatchOperationTest()
    {
        const string TestName = nameof(MaxBatchOperationTest);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName, TestCosmosAdapter.TestRegion);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(TestName);

        // at this point container exists with initial throughput
        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient
            .CreateAndVerifyClientAsync<TestDocument>(databaseOptions, options, createContainer: true);

        await using TestDisposableResources<TestDocument> cleanup = new(client);

        List<BatchItem<TestDocument>> batch = new();

        for (int i = 0; i < BaseCosmosClient.MaxItemsOfTransactionBatch; ++i)
        {
            batch.Add(new(BatchOperation.Create, new($"{i}")));
        }

        QueryRequestOptions<TestDocument> requestOptions = new()
        {
            PartitionKey = new[] { "default user" },
            Region = TestCosmosAdapter.TestRegion
        };

        var result = await client.ExecuteTransactionalBatchAsync(requestOptions, batch, CancellationToken.None);
        result.Succeeded.Should().BeTrue();

        var count = await client.CountDocumentsAsync(requestOptions, null, CancellationToken.None);
        count.Item.Should().Be(BaseCosmosClient.MaxItemsOfTransactionBatch);

        // bigger tasks should fail
        batch.Add(new(BatchOperation.Create, new($"100500")));
        var exception = await Assert.ThrowsAsync<DatabaseClientException>(
            () => client.ExecuteTransactionalBatchAsync(requestOptions, batch, CancellationToken.None));

        exception.Message.Should()
            .Be($"Transaction batch items exceed the limitation of {BaseCosmosClient.MaxItemsOfTransactionBatch}.");
    }

    [Fact]
    public async Task BatchOpetationsTest()
    {
        const string TestName = nameof(BatchOpetationsTest);
        TableOptions options = TestCosmosClient.GetContainerOptions(TestName);
        options.PartitionIdPath = $"/{TestDocument.PartitionKey}";

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(TestName);

        // at this point container exists with initial throughput
        BaseCosmosDocumentClient<TestDocument> client = await TestCosmosClient.CreateAndVerifyClientAsync<TestDocument>(
                        databaseOptions, options, createContainer: true);

        await using TestDisposableResources<TestDocument> cleanup = new(client);

        TestDocument document = new(TestName);
        TestDocument documentOtherId = new($"{TestName}2", message: "different message");

        TestDocument documentNew = new(TestName, message: "new message");
        TestDocument documentOtherIdNew = new($"{TestName}2", message: "new different message");

        TestDocument documentNewId = new($"{TestName}3", message: "new message");
        TestDocument documentOtherIdNewId = new($"{TestName}4", message: "new different message");

        await TestBatchOperationAsync(client, BatchOperation.Create, document, documentOtherId);
        await TestBatchOperationAsync(client, BatchOperation.Read, null, null,
            document.Id, documentOtherId.Id, document, documentOtherId);

        await TestBatchOperationAsync(client, BatchOperation.Upsert, documentNew, documentOtherIdNew);
        await TestBatchOperationAsync(client, BatchOperation.Read, null, null,
            document.Id, documentOtherId.Id, documentNew, documentOtherIdNew);

        await TestBatchOperationAsync(client, BatchOperation.Replace, documentNewId, documentOtherIdNewId, documentNew.Id, documentOtherIdNew.Id);
        await TestBatchOperationAsync(client, BatchOperation.Read, null, null,
            documentNewId.Id, documentOtherIdNewId.Id, documentNewId, documentOtherIdNewId);

        await TestBatchOperationAsync(client, BatchOperation.Delete, null, null,
            documentNewId.Id, documentOtherIdNewId.Id, null, null, document.User);

        await TestReadNotFound(client, document);
        await TestReadNotFound(client, documentOtherId);
        await TestReadNotFound(client, documentNewId);
        await TestReadNotFound(client, documentOtherIdNewId);

        List<BatchItem<TestDocument>> batchItems = Enumerable
            .Range(0, 200)
            .Select(index => new BatchItem<TestDocument>(BatchOperation.Create))
            .ToList();

        DatabaseClientException exception = await Assert.ThrowsAsync<DatabaseClientException>(() =>
            client.ExecuteTransactionalBatchAsync(_request, batchItems, CancellationToken.None));

        exception.Message.Should().Be("Transaction batch items exceed the limitation of 100.");
    }

    [Fact]
    public async Task TestResilience()
    {
        const int Retries = 20;

        DatabaseOptions databaseOptions = TestCosmosAdapter.CreateDatabaseOptions(nameof(TestResilience));
        var adapter = TestCosmosAdapter.CreateCosmosAdapter(databaseOptions,
            policy: Policy
                .Handle<Exception>()
                .RetryAsync(Retries));

        adapter.ClientDecorators.Should().NotBeNull();

        TestCosmosClient testClient = new TestCosmosClient(databaseOptions,
            TestCosmosClient.GetContainerOptions(nameof(TestResilience)));

        var client = testClient.CreateDecoratedCosmosClient(adapter.ClientDecorators!);

        int calls = 0;

        TestDocument document = new("id", user: "2");

        testClient.MockContainer
            .Setup(client => client.ReadItemAsync<TestDocument>("1", new("2"), null, CancellationToken.None))
            .Returns(() =>
            {
                calls += 1;
                Mock<ItemResponse<TestDocument>> mockResponse = new();
                mockResponse.Setup(response => response.Resource)
                    .Returns(new TestDocument($"{(calls < Retries ? throw new ArgumentException() : calls)}"));

                return Task.FromResult(mockResponse.Object);
            });

        var result = await client.ReadDocumentAsync(document.GetOptions(), "1", CancellationToken.None);
        Retries.Should().Be(int.Parse(result.Item?.Id ?? "0", CultureInfo.InvariantCulture));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification = "Test method.")]
    private static async Task TestBatchOperationAsync(
        IDocumentWriter<TestDocument> client, BatchOperation operation,
        TestDocument? inputDoc1, TestDocument? inputDoc2,
        string? id1 = null, string? id2 = null,
        TestDocument? outputDoc1 = null, TestDocument? outputDoc2 = null,
        string? partition = null)
    {
        partition ??= outputDoc1?.User;
        partition ??= inputDoc1?.User;

        if (operation != BatchOperation.Delete)
        {
            outputDoc1 ??= inputDoc1;
            outputDoc2 ??= inputDoc2;

            Assert.NotNull(outputDoc1);
            Assert.NotNull(outputDoc2);

            // that used as partition key and must be equal for batch operation
            Assert.Equal(partition, outputDoc1?.User);
            Assert.Equal(partition, outputDoc2?.User);
        }

        IReadOnlyList<BatchItem<TestDocument>> batch = new List<BatchItem<TestDocument>>
        {
            new(operation, inputDoc1, id1),
            new(operation, inputDoc2, id2),
        };

        var response =
            await client.ExecuteTransactionalBatchAsync(
                new() { PartitionKey = new[] { partition }, ContentResponseOnWrite = true },
                batch, CancellationToken.None);

        Assert.True(response.Succeeded, $"{response.Status}");

        response.Item!.Should().HaveCount(2);
        response.Item![0].Succeeded.Should().BeTrue();
        response.Item![1].Succeeded.Should().BeTrue();

        outputDoc1.Should().BeEquivalentTo(response.Item![0].Item);
        outputDoc2.Should().BeEquivalentTo(response.Item![1].Item);
    }

    private static Task TestReadNotFound(IDocumentReader<TestDocument> client, TestDocument document)
    {
        return client.ReadDocumentAsync(
                new() { PartitionKey = document.GetPartitionKey() },
                document.Id!,
                CancellationToken.None)
            .TestReturnStatus(null, HttpStatusCode.NotFound, false);
    }

    private static Task TestDeleteNotFound(IDocumentWriter<TestDocument> client, TestDocument document)
    {
        return client
            .DeleteDocumentAsync(
                new() { PartitionKey = document.GetPartitionKey() },
                document.Id!,
                CancellationToken.None)
            .TestReturnStatus(false, HttpStatusCode.NotFound, false);
    }

    private static async Task TestInsertException<T>(IDocumentWriter<TestDocument> client, TestDocument document, string message)
        where T : DatabaseException
    {
        var exception = await Assert.ThrowsAsync<T>(() =>
            client.InsertOrUpdateDocumentAsync(document.GetOptions(),
                document.Id!, t => document.GetDocument(), CancellationToken.None));
        exception.Message.Should().BeEquivalentTo(message);
    }

    private static Task TestUpsertSuccess(IDocumentWriter<TestDocument> client, TestDocument document)
    {
        return client.UpsertDocumentAsync(document.GetOptions(), CancellationToken.None)
            .TestReturnStatus(document);
    }

    private async Task TestFetchSuccess(
        IDocumentReader<TestDocument> client,
        TestDocument[] documents,
        QueryRequestOptions<TestDocument>? options,
        Func<IQueryable<TestDocument>, IQueryable<TestDocument>>? condition = null)
    {
        options ??= _request;

        await client.FetchDocumentsAsync(
            options,
            condition,
            CancellationToken.None)
            .TestReturnStatus(documents, count: documents.Length);
    }

    private async Task TestFetchSuccess(
        IDocumentReader<TestDocument> client,
        TestDocument[] documents)
    {
        await client.FetchDocumentsAsync<TestDocument>(
            _request,
            null,
            CancellationToken.None)
            .TestReturnStatus(documents, count: documents.Length);
    }

    private async Task TestQuerySuccess(
        IDocumentReader<TestDocument> client,
        TestDocument[] documents,
        QueryRequestOptions<TestDocument>? options = null)
    {
        options ??= _request;

        await client.QueryDocumentsAsync(
            options,
            new Query("SELECT * FROM c"),
            CancellationToken.None)
            .TestReturnStatus(documents, count: documents.Length);
    }

    private async Task TestQuerySuccess(
        IDocumentReader<TestDocument> client,
        TestDocument[] documents,
        Query? query = null)
    {
        Query notNullQuery = query ?? new Query("SELECT * FROM c", new Dictionary<string, string>());
        await client.QueryDocumentsAsync(_request, notNullQuery, CancellationToken.None)
            .TestReturnStatus(documents);

        await client.QueryDocumentsAsync(
            _request,
            new Query("SELECT * FROM c WHERE 1 <> 1", new Dictionary<string, string>()),
            CancellationToken.None)
            .TestReturnStatus(new TestDocument[0], count: 0);
    }

#pragma warning disable SA1204

    private static Task TestCreateSuccess(IDocumentWriter<TestDocument> client, TestDocument document)
    {
        return client.CreateDocumentAsync(
                document.GetOptions(),
                CancellationToken.None)
            .TestReturnStatus(document, HttpStatusCode.Created);
    }

    private static Task TestReplaceSuccess(IDocumentWriter<TestDocument> client, TestDocument document, string oldId)
    {
        return client.ReplaceDocumentAsync(
                document.GetOptions(),
                oldId,
                CancellationToken.None)
            .TestReturnStatus(document, HttpStatusCode.OK, true);
    }

    private Task TestCount(IDocumentReader<TestDocument> client, int count)
    {
        return client
            .CountDocumentsAsync(_request, null, CancellationToken.None)
            .TestReturnStatus(count);
    }

    private static async Task TestInsertOrUpdateSuccess(IDocumentWriter<TestDocument> client,
        TestDocument document, string oldId, string oldPartition, HttpStatusCode code,
        Func<TestDocument, TestDocument>? conflictResolution = null)
    {
        conflictResolution ??= t => document;

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
                        () => client.InsertOrUpdateDocumentAsync(
                            document.GetOptions(false),
                            oldId,
                            t => t,
                            CancellationToken.None));
        exception.Message.Should().Be("Partition key is null or empty in request.");

        await client.InsertOrUpdateDocumentAsync(
                document.GetOptions(oldPartition),
                oldId,
                conflictResolution,
                CancellationToken.None)
            .TestReturnStatus(document, code);
    }

    private async Task TestDeleteSuccess(IDocumentWriter<TestDocument> client, TestDocument document)
    {
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => client.DeleteDocumentAsync(
                _request,
                document.Id!,
                CancellationToken.None));
        exception.Message.Should().Be("Partition key is null or empty in request.");

        await client.DeleteDocumentAsync(
                document.GetOptions(),
                document.Id!,
                CancellationToken.None)
            .TestReturnStatus(true, HttpStatusCode.NoContent);
    }

    private static Task TestReplaceFailed(IDocumentWriter<TestDocument> client,
        TestDocument document, string notExistingId,
        HttpStatusCode codeToExpect = HttpStatusCode.NotFound)
    {
        return client
            .ReplaceDocumentAsync(document.GetOptions(), notExistingId, CancellationToken.None)
            .TestFailure(codeToExpect);
    }

    private static Task TestCreateFailed(IDocumentWriter<TestDocument> client,
        TestDocument document, HttpStatusCode codeToExpect = HttpStatusCode.Conflict)
    {
        return client
            .CreateDocumentAsync(document.GetOptions(), CancellationToken.None)
            .TestFailure(codeToExpect);
    }

    private static async Task TestReadSuccess(IDocumentReader<TestDocument> client, TestDocument document, bool matches = true)
    {
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
                        () => client.ReadDocumentAsync(
                            new() { ContentResponseOnWrite = true },
                            document.Id!,
                            CancellationToken.None));
        exception.Message.Should().Be("Partition key is null or empty in request.");

        IDatabaseResponse<TestDocument> readResult = await client
            .ReadDocumentAsync(
                document.GetOptions(),
                document.Id!,
                CancellationToken.None);

        Assert.True(readResult.Succeeded);
        if (matches)
        {
            readResult.Item.Should().BeEquivalentTo(document);
        }
        else
        {
            readResult.Item.Should().NotBeEquivalentTo(document);
        }
    }

    private async Task TestPatchSuccess(IDocumentWriter<TestDocument> client,
        TestDocument document, string message, string? filter = null)
    {
        PatchOperation[] patchOperations = new[] { PatchOperation.Replace("/message", message) };

        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => client.PatchDocumentAsync(
                _request,
                document.Id!,
                patchOperations,
                filter,
                CancellationToken.None));
        exception.Message.Should().Be("Partition key is null or empty in request.");

        IDatabaseResponse<TestDocument> patchResult = await client
            .PatchDocumentAsync(
                document.GetOptions(),
                document.Id!,
                patchOperations,
                filter,
                CancellationToken.None);

        Assert.True(patchResult.Succeeded);
        patchResult.Item!.Message.Should().BeEquivalentTo(message);
    }

    private static async Task TestPatchFail(IDocumentWriter<TestDocument> client,
        TestDocument document, string message, string? filter = null)
    {
        PatchOperation[] patchOperations = new[] { PatchOperation.Replace("/message", message) };

        var exception = await Assert.ThrowsAsync<DatabaseServerException>(
            () => client.PatchDocumentAsync(
                document.GetOptions(),
                document.Id!,
                patchOperations,
                filter,
                CancellationToken.None));
        exception.Message.Should().ContainAll("PreconditionFailed (412)", "One of the specified pre-condition is not met");
    }

    private async Task<IDocumentDatabase> CreateContainerForOptionsTest(DatabaseOptions databaseOptions,
        TableOptions options, int? throughput = null, bool deleteAfter = false)
    {
        options.Throughput = new Throughput(throughput);
        var client = TestCosmosAdapter.CreateCosmosAdapter(databaseOptions);
        await client.ConnectAsync(true, CancellationToken.None);

        await using TestDisposableResources<TestDocument> cleanup = new(deleteAfter ? client : null);

        IDatabaseResponse<TableOptions> result = await client.CreateTableAsync(options, _request, CancellationToken.None);
        Assert.True(result.Succeeded);

        await ReadContainerAndValidateOptions(options, client, null);

        return client;
    }

    private async Task ReadContainerAndValidateOptions(TableOptions options, IDocumentDatabase database, string? region)
    {
        IDatabaseResponse<TableOptions> createResult = await database.ReadTableSettingsAsync(
            options, new RequestOptions<TestDocument> { Region = region }, CancellationToken.None);
        Assert.True(createResult.Succeeded);

        Assert.Equal(options.TableName, createResult.Item?.TableName);
        Assert.Equal(options.TimeToLive, createResult.Item?.TimeToLive);
        Assert.Equal(options.PartitionIdPath, createResult.Item?.PartitionIdPath);
        Assert.Equal(options.IsRegional, createResult.Item?.IsRegional);
        Assert.Equal(region, createResult.RequestInfo.Region);

        IDatabaseResponse<TableOptions> conflictResult = await database.CreateTableAsync(options, _request, CancellationToken.None);
        Assert.True(conflictResult.HasStatus(HttpStatusCode.Conflict));
        Assert.False(conflictResult.Succeeded);
    }
}
