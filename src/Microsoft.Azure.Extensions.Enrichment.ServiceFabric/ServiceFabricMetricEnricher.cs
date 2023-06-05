// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

namespace Microsoft.Azure.Extensions.Enrichment.ServiceFabric;

internal sealed class ServiceFabricMetricEnricher : IMetricEnricher
{
    private readonly KeyValuePair<string, string>[] _props;

    public ServiceFabricMetricEnricher(IOptions<ServiceFabricMetricEnricherOptions> options, IOptions<ServiceFabricMetadata> metadata)
    {
        var enricherOptions = Throw.IfMemberNull(options, options.Value);
        var clusterMetadata = Throw.IfMemberNull(metadata, metadata.Value);

        _props = Initialize(enricherOptions, clusterMetadata);
    }

    public void Enrich(IEnrichmentPropertyBag enrichmentPropertyBag) => enrichmentPropertyBag.Add(_props);

    private static KeyValuePair<string, string>[] Initialize(ServiceFabricMetricEnricherOptions enricherOptions, ServiceFabricMetadata clusterMetadata)
    {
        var l = new List<KeyValuePair<string, string>>();

        if (enricherOptions.Application)
        {
            l.Add(new(ServiceFabricEnricherDimensions.Application, clusterMetadata.ApplicationName));
        }

        if (enricherOptions.ApplicationType && clusterMetadata.ApplicationTypeName != null)
        {
            l.Add(new(ServiceFabricEnricherDimensions.ApplicationType, clusterMetadata.ApplicationTypeName));
        }

        if (enricherOptions.Node)
        {
            l.Add(new(ServiceFabricEnricherDimensions.Node, clusterMetadata.NodeName));
        }

        if (enricherOptions.ReplicaOrInstanceId)
        {
            l.Add(new(ServiceFabricEnricherDimensions.ReplicaOrInstanceId, clusterMetadata.ReplicaOrInstanceId.ToInvariantString()));
        }

        if (enricherOptions.Service)
        {
            l.Add(new(ServiceFabricEnricherDimensions.Service, clusterMetadata.ServiceName));
        }

        if (enricherOptions.ServiceType && clusterMetadata.ServiceTypeName != null)
        {
            l.Add(new(ServiceFabricEnricherDimensions.ServiceType, clusterMetadata.ServiceTypeName));
        }

        if (enricherOptions.Geo)
        {
            l.Add(new(ServiceFabricEnricherDimensions.Geo, clusterMetadata.Geo));
        }

        if (enricherOptions.Region)
        {
            l.Add(new(ServiceFabricEnricherDimensions.Region, clusterMetadata.Region));
        }

        if (enricherOptions.Cloud)
        {
            l.Add(new(ServiceFabricEnricherDimensions.Cloud, clusterMetadata.Cloud));
        }

        return l.ToArray();
    }
}
