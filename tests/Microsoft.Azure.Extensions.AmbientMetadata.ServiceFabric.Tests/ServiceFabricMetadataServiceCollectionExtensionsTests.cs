// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric.Test;

public class ServiceFabricMetadataServiceCollectionExtensionsTests
{
    [Fact]
    public void ServiceFabricMetadataServiceCollectionExtensions_GivenAnyNullArgument_ThrowsArgumentNullException()
    {
        var serviceCollection = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddServiceFabricMetadata(config.GetSection(string.Empty)));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.AddServiceFabricMetadata((Action<ServiceFabricMetadata>)null!));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddServiceFabricMetadata(_ => { }));
        Assert.Throws<ArgumentNullException>(() => serviceCollection.AddServiceFabricMetadata((IConfigurationSection)null!));
    }

    [Fact]
    public void ServiceFabricMetadataServiceCollectionExtensions_GivenConfigurationSection_RegistersMetadataFromIt()
    {
        var serviceCollection = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.ServiceName)}"] = "Some service name",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.ServiceTypeName)}"] = "Some service type name",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.ApplicationName)}"] = "Some application name",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.ApplicationTypeName)}"] = "Some application type name",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.NodeName)}"] = "Some node name",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.NodeType)}"] = "Some node type",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.Cloud)}"] = "Some cloud",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.Geo)}"] = "Some geo",
                [$"{nameof(ServiceFabricMetadata)}:{nameof(ServiceFabricMetadata.Region)}"] = "Some region"
            })
            .Build();
        var configurationSection = config
            .GetSection(nameof(ServiceFabricMetadata));

        using var provider = serviceCollection
            .AddServiceFabricMetadata(configurationSection)
            .BuildServiceProvider();
        var metadata = provider
            .GetRequiredService<IOptions<ServiceFabricMetadata>>();

        Assert.NotNull(metadata?.Value);
    }

    [Fact]
    public void ServiceFabricMetadataServiceCollectionExtensions_GivenConfigurationDelegate_RegistersMetadataFromIt()
    {
        var serviceCollection = new ServiceCollection();

        using var provider = serviceCollection
            .AddServiceFabricMetadata(config =>
            {
                config.ServiceName = "Some service name";
                config.ServiceTypeName = "Some service type name";
                config.ApplicationName = "Some application name";
                config.ApplicationTypeName = "Some application type name";
                config.NodeName = "Some node name";
                config.NodeType = "Some node type";
                config.Cloud = "Some cloud";
                config.Geo = "Some geo";
                config.Region = "Some region";
            })
            .BuildServiceProvider();
        var metadata = provider
            .GetRequiredService<IOptions<ServiceFabricMetadata>>();

        Assert.NotNull(metadata?.Value);
    }
}
