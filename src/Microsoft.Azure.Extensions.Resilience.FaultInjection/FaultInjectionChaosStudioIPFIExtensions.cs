// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection;

/// <summary>
/// Provides extension methods for enabling Azure Chaos Studio IPFI agent as fault configuration provider for R9.FaultInjection chaos policies.
/// </summary>
public static class FaultInjectionChaosStudioIPFIExtensions
{
    /// <summary>
    /// Registers an implementation of <see cref="IFaultInjectionOptionsProvider"/> that will be used by
    /// <see cref="FaultInjector"/> for adding fault configurations.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection InitializeAzureChaosStudioFaultInjection(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddSingleton<IFaultInjectionOptionsProvider>(ACSFaultInjectionOptionsProvider.Instance);
        return services;
    }
}
