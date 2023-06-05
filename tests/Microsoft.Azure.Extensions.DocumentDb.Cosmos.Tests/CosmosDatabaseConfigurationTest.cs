// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using FluentAssertions;
using Microsoft.Azure.Extensions.Document.Cosmos.Model;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public class CosmosDatabaseConfigurationTest
{
    [Fact]
    public void ConfigurationTest()
    {
        CosmosDatabaseOptions options = new()
        {
            PrimaryKey = "pk"
        };

        options.EnableGatewayMode.Should().BeTrue();
        options.EnablePrivatePortPool.Should().BeTrue();
        options.EnableTcpEndpointRediscovery.Should().BeTrue();

        var exception = Assert.Throws<InvalidDataException>(() => new CosmosDatabaseConfiguration(options));
        exception.Message.Should().Be("DatabaseName field is null or empty.");

        options.DatabaseName = "123";

        var exception2 = Assert.Throws<InvalidDataException>(() => new CosmosDatabaseConfiguration(options));
        exception2.Message.Should().Contain("Endpoint field is null or empty.");

        var exception3 = Assert.Throws<InvalidDataException>(() => new CosmosDatabaseConfiguration(null!));
        exception3.Message.Should().Contain("DatabaseName field is null or empty.");

        options.RegionalDatabaseOptions["test"] = null!;
        exception = Assert.Throws<InvalidDataException>(() => CosmosDatabaseConfiguration.GetRegionalConfigurations(options));
        exception.Message.Should().ContainAll("Region [test] is not configured.");
    }
}
