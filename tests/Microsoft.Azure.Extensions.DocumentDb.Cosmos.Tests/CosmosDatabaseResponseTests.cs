// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Net;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public class CosmosDatabaseResponseTests
{
    private static readonly RequestInfo _testRequest = new("region", "table", 6, new Uri("http://localhost/"));

    [Fact]
    public void TestGetters()
    {
        CosmosDatabaseResponse<int> testResponse = MakeResponse();

        Assert.Equal(5, testResponse.Item);
        Assert.Equal("continuation token", testResponse.ContinuationToken);
        Assert.Equal("etag", testResponse.ItemVersion);
        Assert.True(testResponse.Succeeded);
        Assert.Equal((int)HttpStatusCode.OK, testResponse.Status);
        testResponse.RequestInfo.Should().Be(_testRequest);
        Assert.Equal("object", testResponse.RawResponse);

        testResponse = MakeResponse(HttpStatusCode.MultipleChoices);
        Assert.False(testResponse.Succeeded);
        Assert.Equal((int)HttpStatusCode.MultipleChoices, testResponse.Status);
        testResponse.RequestInfo.Should().Be(_testRequest);
        Assert.Equal("object", testResponse.RawResponse);

        testResponse = MakeResponse(HttpStatusCode.Continue);
        Assert.False(testResponse.Succeeded);
        Assert.Equal((int)HttpStatusCode.Continue, testResponse.Status);
        Assert.Equal("region", testResponse.RequestInfo.Region);
        Assert.Equal("table", testResponse.RequestInfo.TableName);
        Assert.Equal(6, testResponse.RequestInfo.Cost);
        Assert.Equal("http://localhost/", testResponse.RequestInfo.Endpoint?.ToString());
        testResponse.RequestInfo.Should().Be(_testRequest);
        Assert.Equal("object", testResponse.RawResponse);
    }

    private static CosmosDatabaseResponse<int> MakeResponse(
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new(
            _testRequest,
            statusCode,
            5,
            "etag",
            "continuation token",
            null,
            "object");
    }
}
