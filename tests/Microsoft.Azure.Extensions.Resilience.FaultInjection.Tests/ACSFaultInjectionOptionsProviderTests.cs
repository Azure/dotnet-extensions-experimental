// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Resilience.FaultInjection;
using Xunit;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection.Test;

public class ACSFaultInjectionOptionsProviderTests
{
    [Fact]
    public void ACSFaultInjectionOptionsProvider_GetInstance()
    {
        var optionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var optionsProvider2 = ACSFaultInjectionOptionsProvider.Instance;

        Assert.Equal(optionsProvider, optionsProvider2);
    }

    [Fact]
    public void SetFaultInjectionOptions_ShouldAddOptionsWithFaultId()
    {
        var optionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultId = Guid.NewGuid();
        var faultOptions = new FaultInjectionOptions
        {
            ChaosPolicyOptionsGroups = new Dictionary<string, ChaosPolicyOptionsGroup>
                {
                    { "OptionsGroup", new ChaosPolicyOptionsGroup() }
                }
        };

        optionsProvider.SetFaultInjectionOptions(faultId, faultOptions);
        var results = optionsProvider.TryGetChaosPolicyOptionsGroup("OptionsGroup", out var resultOptionsGroup);

        Assert.True(results);
        Assert.NotNull(resultOptionsGroup);
    }

    [Fact]
    public void TryGetChaosPolicyOptionsGroup_WithRandomOptionsGroupName_ShouldReturnNull()
    {
        var optionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var results = optionsProvider.TryGetChaosPolicyOptionsGroup("RandomGroup", out var resultOptionsGroup);

        Assert.False(results);
        Assert.Null(resultOptionsGroup);
    }

    [Fact]
    public void RemoveFaultInjectionOptions_FaultIdExists_ShouldReturnTrue()
    {
        var optionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultId = Guid.NewGuid();
        var faultOptions = new FaultInjectionOptions();

        optionsProvider.SetFaultInjectionOptions(faultId, faultOptions);
        var result = optionsProvider.RemoveFaultInjectionOptions(faultId);

        Assert.True(result);
    }

    [Fact]
    public void RemoveFaultInjectionOptions_FaultIdDoesNotExist_ShouldReturnFalse()
    {
        var optionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultId = Guid.NewGuid();
        var result = optionsProvider.RemoveFaultInjectionOptions(faultId);

        Assert.False(result);
    }
}
