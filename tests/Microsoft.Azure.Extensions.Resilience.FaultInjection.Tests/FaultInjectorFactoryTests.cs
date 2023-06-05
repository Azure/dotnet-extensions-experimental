// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection.Test;

public class FaultInjectorFactoryTests
{
    [Fact]
    public void CreateFaultInjector_ShouldReturnFaultInjector()
    {
        var factory = new FaultInjectorFactory();
        var result = factory.CreateFaultInjector();

        Assert.IsAssignableFrom<FaultInjector>(result);
    }
}
