// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Azure.Extensions.Telemetry;

internal sealed class AzureSearchMetadata : IDownstreamDependencyMetadata
{
    private static readonly ISet<string> _uniqueHostNameSuffixes = new HashSet<string>
    {
        ".search.windows.net"
    };

    private static readonly ISet<RequestMetadata> _requestMetadataSet = new HashSet<RequestMetadata>
    {
        // Index operations
        new ("POST", "/indexes", "CreateIndex"),
        new ("PUT", "/indexes/{index}", "UpdateIndex"),
        new ("GET", "/indexes", "ListIndexes"),
        new ("GET", "/indexes/{index}", "GetIndex"),
        new ("DELETE", "/indexes/{index}", "DeleteIndex"),
        new ("GET", "/indexes/{index}/stats", "GetIndexStatistics"),
        new ("POST", "/indexes/{index}/analyze", "AnalyzeText"),

        // Document operations
        new ("POST", "/indexes/{index}/docs/index", "AddUpdateDeleteDocuments"),
        new ("POST", "/indexes/{index}/docs/search.index", "AddUpdateDeleteDocuments"),
        new ("GET", "/indexes/{index}/docs", "GetSearchDocuments"),
        new ("POST", "/indexes/{index}/docs/search", "PostSearchDocuments"),
        new ("GET", "/indexes/{index}/docs/suggest", "GetSuggestions"),
        new ("POST", "/indexes/{index}/docs/suggest", "PostSuggestions"),
        new ("GET", "/indexes/{index}/docs/autocomplete", "GetAutocomplete"),
        new ("POST", "/indexes/{index}/docs/autocomplete", "PostAutocomplete"),
        new ("GET", "/indexes/{index}/docs/{doc}", "LookupDocument"),
        new ("GET", "/indexes/{index}/docs/$count", "CountDocuments"),

        // Indexer operations
        new ("POST", "/datasources", "CreateDataSource"),
        new ("POST", "/indexers", "CreateIndexer"),
        new ("DELETE", "/datasources/{datasource}", "DeleteDataSource"),
        new ("DELETE", "/indexers/{indexer}", "DeleteIndexer"),
        new ("GET", "/datasources/{datasource}", "GetDataSource"),
        new ("GET", "/indexers/{indexer}", "GetIndexer"),
        new ("GET", "/indexers/{indexer}/status", "GetIndexerStatus"),
        new ("GET", "/datasources", "ListDataSources"),
        new ("GET", "/indexers", "ListIndexers"),
        new ("POST", "/indexers/{indexer}/reset", "ResetIndexer"),
        new ("POST", "/indexers/{indexer}/run", "RunIndexer"),
        new ("PUT", "/datasources/{datasource}", "UpdateDataSource"),
        new ("PUT", "/indexers/{indexer}", "UpdateIndexer"),

        // Service operations
        new ("GET", "/servicestats", "GetServiceStatistics"),

        // Skillset operations
        new ("POST", "/skillsets/{skill}", "CreateSkillset"),
        new ("DELETE", "/skillsets/{skill}", "DeleteSkillset"),
        new ("GET", "/skillsets/{skill}", "GetSkillset"),
        new ("GET", "/skillsets", "ListSkillsets"),
        new ("PUT", "/skillsets/{skill}", "UpdateSkillset"),

        // Synonym operations
        new ("POST", "/synonymmaps", "CreateSynonymMap"),
        new ("PUT", "/synonymmaps/{synmap}", "UpdateSynonymMap"),
        new ("GET", "/synonymmaps", "ListSynonymMaps"),
        new ("GET", "/synonymmaps/{synmap}", "GetSynonymMap"),
        new ("DELETE", "/synonymmaps/{synmap}", "DeleteSynonymMap"),
    };

    public string DependencyName => "AzureSearch";

    public ISet<string> UniqueHostNameSuffixes => _uniqueHostNameSuffixes;

    public ISet<RequestMetadata> RequestMetadata => _requestMetadataSet;
}
