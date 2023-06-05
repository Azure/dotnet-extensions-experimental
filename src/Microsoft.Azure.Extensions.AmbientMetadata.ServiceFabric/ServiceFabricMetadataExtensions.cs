// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Fabric;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric;

/// <summary>
/// Extensions for Service Fabric Metadata types.
/// </summary>
public static class ServiceFabricMetadataExtensions
{
    private const string DefaultSectionName = "clustermetadata:servicefabric";

    /// <summary>
    /// Registers configuration provider for Service Fabric applications and binds a model object onto the configuration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="serviceContext">Service fabric context for the service.</param>
    /// <param name="sectionName">Section name. Default set to "clustermetadata:servicefabric".</param>
    /// <returns>The input host builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceFabricMetadata))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHostBuilder UseServiceFabricMetadata(
        this IHostBuilder builder,
        ServiceContext serviceContext,
        string sectionName = DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(serviceContext);
        _ = Throw.IfNull(sectionName);

        _ = builder
           .ConfigureHostConfiguration(builder =>
               builder.AddServiceFabricMetadata(serviceContext, sectionName))
           .ConfigureServices((hostBuilderContext, serviceCollection) =>
               serviceCollection.AddServiceFabricMetadata(hostBuilderContext.Configuration.GetSection(sectionName)));

        return builder;
    }

    /// <summary>
    /// Registers configuration provider for Service Fabric applications.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="serviceContext">Service fabric context for the service.</param>
    /// <param name="sectionName">Section name. Default set to "clustermetadata:servicefabric".</param>
    /// <returns>The input configuration builder for call chaining.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
    public static IConfigurationBuilder AddServiceFabricMetadata(
        this IConfigurationBuilder builder,
        ServiceContext serviceContext,
        string sectionName = DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(serviceContext);
        _ = Throw.IfNull(sectionName);

        return builder.Add(new ServiceFabricMetadataSource(serviceContext, sectionName));
    }

    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ServiceFabricMetadata))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddServiceFabricMetadata(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services
            .AddValidatedOptions<ServiceFabricMetadata, ServiceFabricMetadataValidator>()
            .Bind(section);

        return services;
    }

    /// <summary>
    /// Adds an instance of <see cref="ServiceFabricMetadata"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">The delegate to configure <see cref="ServiceFabricMetadata"/> with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">One of the arguments is null.</exception>
    public static IServiceCollection AddServiceFabricMetadata(this IServiceCollection services, Action<ServiceFabricMetadata> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services
            .AddValidatedOptions<ServiceFabricMetadata, ServiceFabricMetadataValidator>()
            .Configure(configure);

        return services;
    }
}
