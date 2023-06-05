// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Xunit;
using RequestOptions = System.Cloud.DocumentDb.RequestOptions;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

[Collection(DocumentCosmosTestConstants.TestCollectionName)]
public class CosmosClientAdapterBuilderTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public async Task BuildTest(bool enableOptions, bool rediscovery, bool addGatewayAddress)
    {
        const string TestName = nameof(BuildTest);

        IDocumentDatabase adapter = TestCosmosAdapter.CreateCosmosAdapter(TestName);

        Assert.NotNull(adapter);
        Assert.IsType<CosmosDocumentDatabase<DatabaseBuilder>>(adapter);

        var options = TestCosmosAdapter.CreateDatabaseOptions(TestName, enableOptions);
        options.EnableTcpEndpointRediscovery = rediscovery;

        bool enabledGateway = enableOptions && addGatewayAddress;

        if (addGatewayAddress)
        {
            options.Endpoint = new Uri($"{options.Endpoint!.OriginalString}?a={CosmosDocumentDatabase<Type>.CosmosGatewayAddress}");
        }

        var client = await TestCosmosClient.CreateAndVerifyClientAsync(
            options, TestCosmosClient.GetContainerOptions(TestName));
        await using TestDisposableResources<TestDocument> cleanup = new(client);

        TableOptions tableOptions = TestCosmosClient.GetContainerOptions(nameof(LocatorTestAsync));

        var container = await client.Database
            .GetContainerAsync(new TableInfo(tableOptions), new RequestOptions<int>(), default);

        var clientOptions = container.Database.Database.Client.ClientOptions;

        PortReuseMode portReuseMode = enableOptions ? PortReuseMode.PrivatePortPool : PortReuseMode.ReuseUnicastPort;

        clientOptions.EnableTcpConnectionEndpointRediscovery.Should().Be(enabledGateway || rediscovery);
        clientOptions.PortReuseMode.Should().Be(enabledGateway ? null : portReuseMode);
        clientOptions.ConnectionMode.Should().Be(enabledGateway ? ConnectionMode.Gateway : ConnectionMode.Direct);
    }

    internal class ContainerLocatorTest : ITableLocator
    {
        public const string NewContainerName = "new container name";

        public int Count { get; private set; }

        private static TableInfo? Modify(TableInfo options)
        {
            return new(options, NewContainerName);
        }

        public TableInfo? LocateTable(in TableInfo options, RequestOptions request)
        {
            Count += 1;
            return Count switch
            {
                2 => null,
                3 => Modify(options),
                _ => options,
            };
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LocatorTestAsync(bool addLocator)
    {
        ContainerLocatorTest? locator = addLocator ? new() : null;
        var adapter = TestCosmosAdapter.CreateCosmosAdapter(nameof(LocatorTestAsync), locator: locator);
        adapter.TableLocator.Should().Be(locator);

        await adapter.ConnectAsync(true, default);

        await using TestDisposableResources<TestDocument> cleanup = new(adapter);

        TableOptions options = TestCosmosClient.GetContainerOptions(nameof(LocatorTestAsync));
        options.IsLocatorRequired = true;
        string name = options.TableName;

        if (locator == null)
        {
            Assert.Throws<DatabaseClientException>(
                () => (BaseCosmosDocumentClient<string>)adapter.GetDocumentReader<string>(options))
                .Message.Should().Contain("Table locator is required for the table");
            options.IsLocatorRequired = false;
        }

        var client = (BaseCosmosDocumentClient<string>)adapter.GetDocumentReader<string>(options);
        client.Table.Should().BeEquivalentTo(options);

        var container = await client.Database.GetContainerAsync(client.Table, new(), default);
        container.Options.TableName.Should().BeEquivalentTo(name);

        container = await client.Database.GetContainerAsync(client.Table, new(), default);
        container.Options.TableName.Should().BeEquivalentTo(name);

        container = await client.Database.GetContainerAsync(client.Table, new(), default);
        container.Options.TableName.Should().BeEquivalentTo(addLocator ? ContainerLocatorTest.NewContainerName : name);

        locator?.Count.Should().Be(3);
    }
}
