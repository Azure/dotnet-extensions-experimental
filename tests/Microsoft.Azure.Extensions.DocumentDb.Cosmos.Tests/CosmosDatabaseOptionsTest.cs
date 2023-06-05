// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;
public class CosmosDatabaseOptionsTest
{
    [Fact]
    public void TestDefaults()
    {
        var config = TestCosmosAdapter.CreateDatabaseOptions(nameof(TestDefaults));

        Assert.True(config.EnableGatewayMode);
        Assert.True(config.EnablePrivatePortPool);
        Assert.True(config.EnableTcpEndpointRediscovery);

        config.EnableGatewayMode = false;
        config.EnablePrivatePortPool = false;
        config.EnableTcpEndpointRediscovery = false;

        CosmosDatabaseConfiguration configuration = new(config);

        Assert.False(configuration.EnableGatewayMode);
        Assert.False(configuration.EnablePrivatePortPool);
        Assert.False(configuration.EnableTcpEndpointRediscovery);

        configuration = new(new DatabaseOptions
        {
            DatabaseName = config.DatabaseName,
            PrimaryKey = config.PrimaryKey,
            Endpoint = config.Endpoint,
        });

        Assert.True(configuration.EnableGatewayMode);
        Assert.True(configuration.EnablePrivatePortPool);
        Assert.True(configuration.EnableTcpEndpointRediscovery);
    }
}
