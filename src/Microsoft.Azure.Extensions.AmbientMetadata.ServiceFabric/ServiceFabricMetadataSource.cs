// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Fabric;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

namespace Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric;

/// <summary>
/// Provides virtual configuration source for information in Service Fabric SDK.
/// </summary>
internal sealed class ServiceFabricMetadataSource : IConfigurationSource
{
    private readonly ServiceContext _serviceContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFabricMetadataSource"/> class.
    /// </summary>
    /// <param name="serviceContext">Service fabric context for the service.</param>
    /// <param name="sectionName">Section name in configuration.</param>
    public ServiceFabricMetadataSource(ServiceContext serviceContext, string sectionName)
    {
        _serviceContext = Throw.IfNull(serviceContext);

        SectionName = sectionName;
    }

    /// <summary>
    /// Gets configuration section name.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Builds <see cref="IConfigurationProvider"/> from <see cref="ServiceContext"/>.
    /// </summary>
    /// <param name="builder">Used to build the application configuration.</param>
    /// <returns>The configuration provider.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var provider = new MemoryConfigurationProvider(new MemoryConfigurationSource())
        {
            { $"{SectionName}:servicename", _serviceContext.ServiceName.ToString() },
            { $"{SectionName}:servicetypename", _serviceContext.ServiceTypeName },
            { $"{SectionName}:replicaorinstanceid", _serviceContext.ReplicaOrInstanceId.ToInvariantString() },
            { $"{SectionName}:partitionid", _serviceContext.PartitionId.ToString() },
            { $"{SectionName}:applicationname", _serviceContext.CodePackageActivationContext.ApplicationName },
            { $"{SectionName}:applicationtypename", _serviceContext.CodePackageActivationContext.ApplicationTypeName },
            { $"{SectionName}:nodename", _serviceContext.NodeContext.NodeName },
            { $"{SectionName}:nodetype", _serviceContext.NodeContext.NodeType }
        };

        return provider;
    }
}
