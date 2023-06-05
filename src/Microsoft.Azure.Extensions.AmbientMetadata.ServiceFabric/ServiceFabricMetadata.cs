// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric;

/// <summary>
/// Cluster metadata for applications running in Service Fabric.
/// </summary>
public class ServiceFabricMetadata
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="string.Empty"/>.
    /// </remarks>
    [Required]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service type name.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="null"/>.
    /// </remarks>
    public string? ServiceTypeName { get; set; }

    /// <summary>
    /// Gets or sets the stateful service replica ID or the stateless service instance ID.
    /// </summary>
    /// <remarks>
    /// Default set to 0.
    /// </remarks>
    [Range(0, long.MaxValue)]
    public long ReplicaOrInstanceId { get; set; }

    /// <summary>
    /// Gets or sets the partition ID.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="Guid.Empty"/>.
    /// </remarks>
    public Guid PartitionId { get; set; }

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="string.Empty"/>.
    /// </remarks>
    [Required]
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application type name.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="null"/>.
    /// </remarks>
    public string? ApplicationTypeName { get; set; }

    /// <summary>
    /// Gets or sets the node name.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="string.Empty"/>.
    /// </remarks>
    [Required]
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node type.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="null"/>.
    /// </remarks>
    public string? NodeType { get; set; }

    /// <summary>
    /// Gets or sets the Azure Cloud the application is running in.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="string.Empty"/>.
    /// </remarks>
    public string Cloud { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure Geography the application is running in.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="string.Empty"/>.
    /// </remarks>
    public string Geo { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure Region the application is running in.
    /// </summary>
    /// <remarks>
    /// Default set to <see cref="string.Empty"/>.
    /// </remarks>
    public string Region { get; set; } = string.Empty;
}
