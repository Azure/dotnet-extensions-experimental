// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.FaultInjection;
using Xunit;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection.Test;
public class FaultInjectionChaosStudioIPFIExtensionsTests
{
    [Fact]
    public void InitializeAzureChaosStudioFaultInjection_ShouldRegisterFaultInjectionOptionsProviderSingleton()
    {
        var services = new ServiceCollection();
        services.InitializeAzureChaosStudioFaultInjection();

        using var serviceProvider = services.BuildServiceProvider();

        var optionsProvider = serviceProvider.GetService<IFaultInjectionOptionsProvider>();
        Assert.IsAssignableFrom<IFaultInjectionOptionsProvider>(optionsProvider);

        Assert.Equal((ACSFaultInjectionOptionsProvider)optionsProvider!, ACSFaultInjectionOptionsProvider.Instance);
    }

    [Fact]
    public void InitializeAzureChaosStudioFaultInjection_NullService_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => ((ServiceCollection)null!).InitializeAzureChaosStudioFaultInjection());
    }
}
