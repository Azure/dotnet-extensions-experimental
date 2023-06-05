// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public sealed class TestDocument
{
    public const string PartitionKey = "user";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName(PartitionKey)]
    public string? User { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    public TestDocument GetDocument() => this;

    public TestDocument()
    {
    }

    public TestDocument(string id, string? user = "default user", string? message = "test message")
    {
        Id = id;
        User = user;
        Message = message;
    }

    public static TestDocument GetDefault()
    => new()
    {
        Id = "id",
        User = "default user",
        Message = "test message",
    };

    public IReadOnlyList<string?> GetPartitionKey()
        => new[] { User };

    public QueryRequestOptions<TestDocument> GetOptions(bool hasPK = true)
        => new()
        {
            Document = GetDocument(),
            ContentResponseOnWrite = true,
            PartitionKey = hasPK ? GetPartitionKey() : null
        };

    public QueryRequestOptions<TestDocument> GetOptions(string partition)
        => new()
        {
            Document = GetDocument(),
            ContentResponseOnWrite = true,
            PartitionKey = new[] { partition }
        };
}
