// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

namespace Microsoft.Azure.Extensions.Enrichment.ServiceFabric;

internal sealed class ServiceFabricTraceEnricher : ITraceEnricher
{
    private readonly string? _application;
    private readonly string? _node;
    private readonly string? _partitionId;
    private readonly string? _replicaOrInstanceId;
    private readonly string? _service;
    private readonly string? _geo;
    private readonly string? _region;
    private readonly string? _cloud;

    public ServiceFabricTraceEnricher(IOptions<ServiceFabricTraceEnricherOptions> options, IOptions<ServiceFabricMetadata> metadata)
    {
        var enricherOptions = Throw.IfMemberNull(options, options.Value);
        var clusterMetadata = Throw.IfMemberNull(metadata, metadata.Value);

        if (enricherOptions.Application)
        {
            _application = clusterMetadata.ApplicationName;
        }

        if (enricherOptions.Node)
        {
            _node = clusterMetadata.NodeName;
        }

        if (enricherOptions.PartitionId && clusterMetadata.PartitionId != Guid.Empty)
        {
            _partitionId = clusterMetadata.PartitionId.ToString();
        }

        if (enricherOptions.ReplicaOrInstanceId)
        {
            _replicaOrInstanceId = clusterMetadata.ReplicaOrInstanceId.ToInvariantString();
        }

        if (enricherOptions.Service)
        {
            _service = clusterMetadata.ServiceName;
        }

        if (enricherOptions.Geo)
        {
            _geo = clusterMetadata.Geo;
        }

        if (enricherOptions.Region)
        {
            _region = clusterMetadata.Region;
        }

        if (enricherOptions.Cloud)
        {
            _cloud = clusterMetadata.Cloud;
        }
    }

    public void Enrich(Activity activity)
    {
        if (_application != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.Application, _application);
        }

        if (_node != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.Node, _node);
        }

        if (_partitionId != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.PartitionId, _partitionId);
        }

        if (_replicaOrInstanceId != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.ReplicaOrInstanceId, _replicaOrInstanceId);
        }

        if (_service != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.Service, _service);
        }

        if (_geo != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.Geo, _geo);
        }

        if (_region != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.Region, _region);
        }

        if (_cloud != null)
        {
            _ = activity.AddTag(ServiceFabricEnricherDimensions.Cloud, _cloud);
        }
    }

    public void EnrichOnActivityStart(Activity activity)
    {
        // nothing
    }
}
