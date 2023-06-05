// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Microsoft.Azure.Extensions.FrontDoor.HeaderParsing.Test;

public class AzureHeadersTests
{
    [Fact]
    public void ClientIp_header_has_correct_setup()
    {
        Assert.Equal("X-FD-ClientIP", AzureHeaders.ClientIp.HeaderName);
        Assert.NotNull(AzureHeaders.ClientIp.ParserInstance);
        Assert.Null(AzureHeaders.ClientIp.ParserType);
    }

    [Fact]
    public void SocketIp_header_has_correct_setup()
    {
        Assert.Equal("X-FD-SocketIP", AzureHeaders.SocketIp.HeaderName);
        Assert.NotNull(AzureHeaders.SocketIp.ParserInstance);
        Assert.Null(AzureHeaders.SocketIp.ParserType);
    }

    [Fact]
    public void EdgeEnvironment_header_has_correct_setup()
    {
        Assert.Equal("X-FD-EdgeEnvironment", AzureHeaders.EdgeEnvironment.HeaderName);
        Assert.NotNull(AzureHeaders.EdgeEnvironment.ParserInstance);
        Assert.Null(AzureHeaders.EdgeEnvironment.ParserType);
    }

    [Fact]
    public void RouteKey_header_has_correct_setup()
    {
        Assert.Equal("X-FD-RouteKey", AzureHeaders.RouteKey.HeaderName);
        Assert.NotNull(AzureHeaders.RouteKey.ParserInstance);
        Assert.Null(AzureHeaders.RouteKey.ParserType);
    }

    [Fact]
    public void RouteKeyApplicationEndpointList_header_has_correct_setup()
    {
        Assert.Equal("X-FD-RouteKeyApplicationEndpointList", AzureHeaders.RouteKeyApplicationEndpointList.HeaderName);
        Assert.NotNull(AzureHeaders.RouteKeyApplicationEndpointList.ParserInstance);
        Assert.Null(AzureHeaders.RouteKeyApplicationEndpointList.ParserType);
    }
}
