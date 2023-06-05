// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric;

namespace Microsoft.Azure.Extensions.Enrichment.ServiceFabric;

/// <summary>
/// Options for the Service Fabric enricher.
/// </summary>
public class ServiceFabricLogEnricherOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.ApplicationName"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool Application { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.ApplicationTypeName"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool ApplicationType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.Cloud"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool Cloud { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.Geo"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool Geo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.NodeName"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool Node { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.NodeType"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool NodeType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.PartitionId"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool PartitionId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.Region"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool Region { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.ReplicaOrInstanceId"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool ReplicaOrInstanceId { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.ServiceName"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="true"/>.
    /// </remarks>
    public bool Service { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ServiceFabricMetadata.ServiceTypeName"/> is used for telemetry enrichment.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="false"/>.
    /// </remarks>
    public bool ServiceType { get; set; }
}
