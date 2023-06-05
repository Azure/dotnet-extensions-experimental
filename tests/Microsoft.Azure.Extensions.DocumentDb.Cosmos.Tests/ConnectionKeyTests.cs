// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

[Collection(DocumentCosmosTestConstants.TestCollectionName)]
public class ConnectionKeyTests
{
    [Fact]
    public void ConnectionKeyTest()
    {
        ConnectionKey key1 = new ConnectionKey("url1", "db");
        ConnectionKey key2 = new ConnectionKey("url2", "db");

        Assert.Equal(key1, key1);
        Assert.NotEqual(key1, key2);
        Assert.NotEqual((ConnectionKey?)null, key1);
        Assert.NotEqual(key1.GetHashCode(), key2.GetHashCode());
        Assert.True(key1.Equals((object)key1));
        Assert.False(key1.Equals((object)key2));
        Assert.False(key1.Equals("123"));

        Assert.True(key1 != key2);
        Assert.False(key1 == key2);
#pragma warning disable CS8073, CS1718 // need to test those exact matches
        Assert.True(key1 != null);
        Assert.False(key1 == null);

        Assert.False(key1 != key1);
        Assert.True(key1 == key1);
#pragma warning restore CS8073, CS1718
    }
}
