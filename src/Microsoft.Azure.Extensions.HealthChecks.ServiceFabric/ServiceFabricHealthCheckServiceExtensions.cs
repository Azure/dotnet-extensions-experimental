// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric;

/// <summary>
/// Extension methods for adding <see cref="ServiceFabricHealthCheckService" /> to an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceFabricHealthCheckServiceExtensions
{
    /// <summary>
    /// Adds Service Fabric health check service publisher to the container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
    /// <param name="listener">The <see cref="ICommunicationListener"/> to use.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddServiceFabricHealthCheckPublisher(this IServiceCollection services, ICommunicationListener listener)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(listener);

        _ = services.AddHealthChecks();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ServiceFabricHealthCheckService>((serviceProvider) =>
                ActivatorUtilities.CreateInstance<ServiceFabricHealthCheckService>(serviceProvider, listener)));
        return services;
    }

    /// <summary>
    /// Adds Service Fabric health check service publisher to the container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
    /// <param name="listener">The <see cref="ICommunicationListener"/> to use.</param>
    /// <param name="options">Options to configure ServiceFabricHealthCheckService.</param>
    /// <returns>The same instance of the <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddServiceFabricHealthCheckPublisher(this IServiceCollection services, ICommunicationListener listener, Action<ServiceFabricHealthCheckOptions> options)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(listener);
        _ = Throw.IfNull(options);

        _ = services.AddHealthChecks();
        _ = services.Configure(options);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ServiceFabricHealthCheckService>((serviceProvider) =>
                ActivatorUtilities.CreateInstance<ServiceFabricHealthCheckService>(serviceProvider, listener)));
        return services;
    }
}
