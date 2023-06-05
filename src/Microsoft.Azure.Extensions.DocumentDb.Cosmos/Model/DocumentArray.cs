// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

internal sealed class DocumentArray<T>
{
    [JsonPropertyName("Documents")]
    public T[]? Documents { get; set; }
}
