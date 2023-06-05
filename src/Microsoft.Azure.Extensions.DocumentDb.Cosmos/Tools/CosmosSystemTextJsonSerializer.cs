// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text.Json;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.IO;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Implementation of the default System.Text.Json based cosmos serializer.
/// </summary>
/// <remarks>
/// The implementation of Cosmos Serializers are internal in Cosmos SDK without a way of actual usage.
/// Examples:
/// - <see href="https://github.com/Azure/azure-cosmos-dotnet-v3/blob/v4/Microsoft.Azure.Cosmos/azuredata/Serializer/CosmosTextJsonSerializer.cs"/>.
/// - <see href="https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos.Samples/Usage/SystemTextJson/CosmosSystemTextJsonSerializer.cs"/>
/// The Cosmos SDK discussion on github: <see href="https://github.com/Azure/azure-cosmos-dotnet-v3/blob/v4/Microsoft.Azure.Cosmos/azuredata/Serializer/CosmosTextJsonSerializer.cs"/>.
/// This class to be replaced with Cosmos implementation once opened.
/// </remarks>
internal sealed class CosmosSystemTextJsonSerializer : CosmosSerializer
{
    private static readonly RecyclableMemoryStreamManager _memmoryStreamManager = new();
    private readonly JsonObjectSerializer _serializer;

    public CosmosSystemTextJsonSerializer(JsonSerializerOptions? jsonSerializerOptions)
    {
        _serializer = jsonSerializerOptions != null
            ? new JsonObjectSerializer(jsonSerializerOptions)
            : new JsonObjectSerializer();
    }

    public override T FromStream<T>(Stream stream)
    {
        // External interface CosmosSerializer does not enforce using nullable references.
        // Therefore for APIs expected to return null it declares not nullable return types.
        // But null could be returned out of interface by design.
#pragma warning disable CS8603, CS8600
        if (stream == null)
        {
            return default;
        }

        if (typeof(Stream).IsAssignableFrom(typeof(T)))
        {
            return (T)(object)stream;
        }

        using (stream)
        {
            if (stream.Length == 0)
            {
                return default;
            }

            return (T)_serializer.Deserialize(stream, typeof(T), default);
        }
#pragma warning restore CS8603, CS8600
    }

    public override Stream ToStream<T>(T input)
    {
        MemoryStream streamPayload = _memmoryStreamManager.GetStream();
        _serializer.Serialize(streamPayload, input, typeof(T), default);
        streamPayload.Position = 0;
        return streamPayload;
    }
}
