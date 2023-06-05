// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Telemetry;

/// <summary>
/// Extensions for telemetry utilities.
/// </summary>
public static class TelemetryAzureExtensions
{
    /// <summary>
    /// Adds dependency metadata for CosmosDB dependency.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <returns><see cref="IServiceCollection"/> object for chaining.</returns>
    [Experimental]
    public static IServiceCollection AddAzureCosmosDBDownstreamDependencyMetadata(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        return services.AddDownstreamDependencyMetadata<AzureCosmosDBMetadata>();
    }

    /// <summary>
    /// Adds dependency metadata for Azure Search dependency.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <returns><see cref="IServiceCollection"/> object for chaining.</returns>
    [Experimental]
    public static IServiceCollection AddAzureSearchDownstreamDependencyMetadata(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        return services.AddDownstreamDependencyMetadata<AzureSearchMetadata>();
    }
}
