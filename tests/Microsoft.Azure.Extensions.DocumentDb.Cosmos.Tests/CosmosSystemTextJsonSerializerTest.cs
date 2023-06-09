// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text.Json;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public class CosmosSystemTextJsonSerializerTest
{
    [Fact]
    public void SerializerTests()
    {
        CosmosSystemTextJsonSerializer serializer = new(null);
        CosmosSystemTextJsonSerializer serializer2 = new(new JsonSerializerOptions());

        string longString = "long text";

        Stream stream = serializer.ToStream(longString);
        Assert.Equal(longString, serializer.FromStream<string>(stream));

        stream = serializer.ToStream(longString);
        stream = serializer.FromStream<Stream>(stream);
        Assert.Equal(longString, serializer.FromStream<string>(stream));

        Assert.Null(serializer2.FromStream<string>(Stream.Null));
        Assert.Null(serializer2.FromStream<string>(null!));
    }
}
