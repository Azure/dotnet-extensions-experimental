// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric.Tests;

public class ServiceFabricHealthCheckOptionsTest
{
    [Fact]
    public void ServiceFabricHealthCheckOptions_DefaultValues()
    {
        var options = new ServiceFabricHealthCheckOptions();

        Assert.True(options.PublishingPredicate(null!));

        var mockHealthCheck = new Mock<IHealthCheck>();
        Assert.True(options.PublishingPredicate(new HealthCheckRegistration("TestHealthCheckRegistration", mockHealthCheck.Object, HealthStatus.Unhealthy, Enumerable.Empty<string>())));
    }
}
