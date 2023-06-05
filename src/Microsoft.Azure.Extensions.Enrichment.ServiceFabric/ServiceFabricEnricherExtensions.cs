// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry.Trace;

namespace Microsoft.Azure.Extensions.Enrichment.ServiceFabric;

/// <summary>
/// Extension methods for setting up Service Fabric enrichers in an <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceFabricEnricherExtensions
{
    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricLogEnricher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ServiceFabricLogEnricher"/> to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceFabricLogEnricher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services
            .AddLogEnricher<ServiceFabricLogEnricher>()
            .Configure<ServiceFabricLogEnricherOptions>(static _ => { });
    }

    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricLogEnricher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ServiceFabricLogEnricher"/> to.</param>
    /// <param name="configure">The <see cref="ServiceFabricLogEnricherOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceFabricLogEnricher(this IServiceCollection services, Action<ServiceFabricLogEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .AddLogEnricher<ServiceFabricLogEnricher>()
            .Configure(configure);
    }

    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricLogEnricher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ServiceFabricLogEnricher"/> to.</param>
    /// <param name="section">
    ///     The <see cref="IConfigurationSection"/> to use for configuring <see cref="ServiceFabricLogEnricherOptions"/> in the <see cref="ServiceFabricLogEnricher"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceFabricLogEnricherOptions))]
    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddServiceFabricLogEnricher(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .AddLogEnricher<ServiceFabricLogEnricher>()
            .Configure<ServiceFabricLogEnricherOptions>(section);
    }

    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricMetricEnricher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ServiceFabricMetricEnricher"/> to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceFabricMetricEnricher(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        return services
            .AddMetricEnricher<ServiceFabricMetricEnricher>()
            .Configure<ServiceFabricMetricEnricherOptions>(static _ => { });
    }

    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricMetricEnricher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ServiceFabricMetricEnricher"/> to.</param>
    /// <param name="configure">The <see cref="ServiceFabricMetricEnricherOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    public static IServiceCollection AddServiceFabricMetricEnricher(this IServiceCollection services, Action<ServiceFabricMetricEnricherOptions> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        return services
            .AddMetricEnricher<ServiceFabricMetricEnricher>()
            .Configure(configure);
    }

    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricMetricEnricher"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the <see cref="ServiceFabricMetricEnricher"/> to.</param>
    /// <param name="section">
    ///     The <see cref="IConfigurationSection"/> to use for configuring <see cref="ServiceFabricMetricEnricherOptions"/> in the <see cref="ServiceFabricMetricEnricher"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceFabricMetricEnricherOptions))]
    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddServiceFabricMetricEnricher(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        return services
            .AddMetricEnricher<ServiceFabricMetricEnricher>()
            .Configure<ServiceFabricMetricEnricherOptions>(section);
    }

    /// <summary>
    /// Adds an instance of Service Fabric trace enricher to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the Service Fabric trace enricher to.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddServiceFabricTraceEnricher(this TracerProviderBuilder builder)
    {
        _ = Throw.IfNull(builder);

        _ = builder.AddTraceEnricher<ServiceFabricTraceEnricher>();
        _ = builder.ConfigureServices(services => services.Configure<ServiceFabricTraceEnricherOptions>(static _ => { }));

        return builder;
    }

    /// <summary>
    /// Adds an instance of Service Fabric trace enricher to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the Service Fabric trace enricher to.</param>
    /// <param name="configure">The <see cref="ServiceFabricTraceEnricherOptions"/> configuration delegate.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    public static TracerProviderBuilder AddServiceFabricTraceEnricher(this TracerProviderBuilder builder, Action<ServiceFabricTraceEnricherOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.AddTraceEnricher<ServiceFabricTraceEnricher>();
        _ = builder.ConfigureServices(services => services.Configure(configure));

        return builder;
    }

    /// <summary>
    /// Adds an instance of Service Fabric trace enricher to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to add the Service Fabric trace enricher to.</param>
    /// <param name="section">The <see cref="IConfigurationSection"/> to use for configuring <see cref="ServiceFabricTraceEnricherOptions"/> in the Service Fabric trace enricher.</param>
    /// <returns>The <see cref="TracerProviderBuilder"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceFabricTraceEnricherOptions))]
    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = "Addressed with [DynamicDependency]")]
    public static TracerProviderBuilder AddServiceFabricTraceEnricher(this TracerProviderBuilder builder, IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.AddTraceEnricher<ServiceFabricTraceEnricher>();
        _ = builder.ConfigureServices(services => services.Configure<ServiceFabricTraceEnricherOptions>(section));

        return builder;
    }
}
