// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Extensions.Enrichment.ServiceFabric;

/// <summary>
/// Constants used for enrichment dimensions.
/// </summary>
// Avoid changing const values in this class by all means. Such a breaking change would break customer's monitoring.
public static class ServiceFabricEnricherDimensions
{
    /// <summary>
    /// Service Fabric application name.
    /// </summary>
    public const string Application = "env_sf_application";

    /// <summary>
    /// Service Fabric application type name.
    /// </summary>
    public const string ApplicationType = "env_sf_applicationType";

    /// <summary>
    /// Service Fabric node name.
    /// </summary>
    public const string Node = "env_sf_node";

    /// <summary>
    /// Service Fabric node type name.
    /// </summary>
    public const string NodeType = "env_sf_nodeType";

    /// <summary>
    /// Service Fabric partition ID.
    /// </summary>
    public const string PartitionId = "env_sf_partitionId";

    /// <summary>
    /// Service Fabric replica or instance ID.
    /// </summary>
    public const string ReplicaOrInstanceId = "env_sf_replicaOrInstanceId";

    /// <summary>
    /// Service Fabric service name.
    /// </summary>
    public const string Service = "env_sf_service";

    /// <summary>
    /// Service Fabric service type name.
    /// </summary>
    public const string ServiceType = "env_sf_serviceType";

    /// <summary>
    /// Azure Geography.
    /// </summary>
    public const string Geo = "env_azure_geo";

    /// <summary>
    /// Azure Region.
    /// </summary>
    public const string Region = "env_azure_region";

    /// <summary>
    /// Azure Cloud.
    /// </summary>
    public const string Cloud = "env_azure_cloud";

    /// <summary>
    /// Gets a list of all dimension names.
    /// </summary>
    /// <returns>A read-only <see cref="IReadOnlyList{String}"/> of all dimension names.</returns>
    public static IReadOnlyList<string> DimensionNames { get; } =
        Array.AsReadOnly(new[]
        {
            Application,
            ApplicationType,
            Cloud,
            Geo,
            Node,
            NodeType,
            PartitionId,
            Region,
            ReplicaOrInstanceId,
            Service,
            ServiceType
        });
}
