// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using ServiceFabric.Mocks;
using Xunit;

namespace Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric.Test;

public class AcceptanceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("R9:ClusterMetadata:ServiceFabric")]
    [InlineData(null)]
    public async Task UseServiceFabricMetadata_CreatesPopulatesAndRegistersOptions(string? sectionName)
    {
        var context = MockStatelessServiceContextFactory.Default;
        await RunAsync(context,  // <-- Model object looks sane.
            i =>
            {
                Assert.Equal("fabric:/MockApp", i.ApplicationName);
                Assert.Equal("MockAppType", i.ApplicationTypeName);
                Assert.Equal("Node0", i.NodeName);
                Assert.Equal("NodeType1", i.NodeType);
                Assert.IsType<long>(i.ReplicaOrInstanceId);
                Assert.Equal(context.ReplicaOrInstanceId, i.ReplicaOrInstanceId);
                Assert.Equal(context.PartitionId, i.PartitionId);
                Assert.Equal("fabric:/MockApp/MockStatefulService", i.ServiceName);
                Assert.Equal("MockServiceType", i.ServiceTypeName);

                return Task.CompletedTask;
            },
            sectionName);
    }

    [Fact]
    public void AddServiceFabricMetadata_CreatesPopulatesAndRegistersOptions()
    {
        var context = MockStatelessServiceContextFactory.Default;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["clustermetadata:servicefabric:cloud"] = "Some cloud",
                ["clustermetadata:servicefabric:geo"] = "Some geo",
                ["clustermetadata:servicefabric:region"] = "Some region"
            })
            .AddServiceFabricMetadata(context)
            .Build();
        var configSection = config.GetSection("clustermetadata:servicefabric");
        var metadata = new ServiceCollection()
            .AddServiceFabricMetadata(configSection)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ServiceFabricMetadata>>()
            .Value;
        Assert.NotNull(metadata.Cloud);
        Assert.NotNull(metadata.Region);
        Assert.NotNull(metadata.Geo);
        Assert.NotNull(metadata.ServiceName);
        Assert.NotNull(metadata.ApplicationName);
        Assert.NotNull(metadata.NodeName);
    }

    private static async Task RunAsync(ServiceContext context, Func<ServiceFabricMetadata, Task> func, string? sectionName)
    {
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices((_, services) => services.AddServiceFabricMetadata(metadata =>
            {
                metadata.Cloud = "public";
                metadata.Region = "westus";
                metadata.Geo = "united states";
            }))
            .UseServiceFabricMetadata(context, sectionName ?? "clustermetadata:servicefabric")
            .StartAsync();

        await func(host.Services.GetRequiredService<IOptions<ServiceFabricMetadata>>().Value); // <-- Our feature has registered model object.
        await host.StopAsync();
    }
}
