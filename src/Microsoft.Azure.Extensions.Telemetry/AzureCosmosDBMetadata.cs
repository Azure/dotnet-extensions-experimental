// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Azure.Extensions.Telemetry;

internal sealed class AzureCosmosDBMetadata : IDownstreamDependencyMetadata
{
    private static readonly ISet<string> _uniqueHostNameSuffixes = new HashSet<string>
    {
        ".documents.azure.com",
        ".documents.chinacloudapi.cn",
        ".documents.cloudapi.de",
        ".documents.usgovcloudapi.net"
    };

    private static readonly ISet<RequestMetadata> _requestMetadataSet = new HashSet<RequestMetadata>
    {
        // Azure CosmosDB documented REST URIs.
        // https://docs.microsoft.com/en-us/rest/api/cosmos-db/cosmosdb-resource-uri-syntax-for-rest

        // Database operations
        new ("POST", "/dbs", "CreateDatabase"),
        new ("GET", "/dbs", "ListDatabases"),
        new ("GET", "/dbs/{db}", "GetDatabase"),
        new ("DELETE", "/dbs/{db}", "DeleteDatabase"),

        // Collection operations
        new ("POST", "/dbs/{db}/colls", "CreateCollection"),
        new ("GET", "/dbs/{db}/colls", "ListCollections"),
        new ("GET", "/dbs/{db}/colls/{coll}", "GetCollection"),
        new ("DELETE", "/dbs/{db}/colls/{coll}", "DeleteCollection"),
        new ("PUT", "/dbs/{db}/colls/{coll}", "ReplaceCollection"),

        // Document operations
        new ("POST", "/dbs/{db}/colls/{coll}/docs", "CreateOrQueryDocument"),
        new ("GET", "/dbs/{db}/colls/{coll}/docs", "ListDocuments"),
        new ("GET", "/dbs/{db}/colls/{coll}/docs/{doc}", "GetDocument"),
        new ("PUT", "/dbs/{db}/colls/{coll}/docs/{doc}", "ReplaceDocument"),
        new ("DELETE", "/dbs/{db}/colls/{coll}/docs/{doc}", "DeleteDocument"),

        // Attachment operations
        new ("POST", "/dbs/{db}/colls/{coll}/docs/{doc}/attachments", "CreateAttachment"),
        new ("GET", "/dbs/{db}/colls/{coll}/docs/{doc}/attachments", "ListAttachments"),
        new ("GET", "/dbs/{db}/colls/{coll}/docs/{doc}/attachments/{attch}", "GetAttachment"),
        new ("PUT", "/dbs/{db}/colls/{coll}/docs/{doc}/attachments/{attch}", "ReplaceAttachment"),
        new ("DELETE", "/dbs/{db}/colls/{coll}/docs/{doc}/attachments/{attch}", "DeleteAttachment"),

        // Stored procedure operations
        new ("POST", "/dbs/{db}/colls/{coll}/sprocs", "CreateStoredProcedure"),
        new ("GET", "/dbs/{db}/colls/{coll}/sprocs", "ListStoredProcedures"),
        new ("PUT", "/dbs/{db}/colls/{coll}/sprocs/{sproc}", "ReplaceStoredProcedure"),
        new ("DELETE", "/dbs/{db}/colls/{coll}/sprocs/{sproc}", "DeleteStoredProcedure"),
        new ("POST", "/dbs/{db}/colls/{coll}/sprocs/{sproc}", "ExecuteStoredProcedure"),

        // User defined function operations
        new ("POST", "/dbs/{db}/colls/{coll}/udfs", "CreateUDF"),
        new ("GET", "/dbs/{db}/colls/{coll}/udfs", "ListUDFs"),
        new ("PUT", "/dbs/{db}/colls/{coll}/udfs/{udf}", "ReplaceUDF"),
        new ("DELETE", "/dbs/{db}/colls/{coll}/udfs/{udf}", "DeleteUDF"),

        // Trigger operations
        new ("POST", "/dbs/{db}/colls/{coll}/triggers", "CreateTrigger"),
        new ("GET", "/dbs/{db}/colls/{coll}/triggers", "ListTriggers"),
        new ("PUT", "/dbs/{db}/colls/{coll}/triggers/{trigger}", "ReplaceTrigger"),
        new ("DELETE", "/dbs/{db}/colls/{coll}/triggers/{trigger}", "DeleteTrigger"),

        // User operations
        new ("POST", "/dbs/{db}/users", "CreateUser"),
        new ("GET", "/dbs/{db}/users", "ListUsers"),
        new ("GET", "/dbs/{db}/users/{user}", "GetUser"),
        new ("PUT", "/dbs/{db}/users/{user}", "ReplaceUser"),
        new ("DELETE", "/dbs/{db}/users/{user}", "DeleteUser"),

        // Permission operations
        new ("POST", "/dbs/{db}/users/{user}/permissions", "CreatePermission"),
        new ("GET", "/dbs/{db}/users/{user}/permissions", "ListPermissions"),
        new ("GET", "/dbs/{db}/users/{user}/permissions/{perm}", "GetPermission"),
        new ("PUT", "/dbs/{db}/users/{user}/permissions/{perm}", "ReplacePermission"),
        new ("DELETE", "/dbs/{db}/users/{user}/permissions/{perm}", "DeletePermission"),

        // Offer operations
        new ("POST", "/offers", "QueryOffers"),
        new ("GET", "/offers", "ListOffers"),
        new ("GET", "/offers/{offer}", "GetOffer"),
        new ("PUT", "/offers/{offer}", "ReplaceOffer"),
    };

    public string DependencyName => "AzureCosmosDB";

    public ISet<string> UniqueHostNameSuffixes => _uniqueHostNameSuffixes;

    public ISet<RequestMetadata> RequestMetadata => _requestMetadataSet;
}
