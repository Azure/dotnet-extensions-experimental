// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Internal;
using Xunit;

namespace Microsoft.Azure.Extensions.Telemetry.Test;

#pragma warning disable CA1505
public class AzureMetadataTests
{
    private readonly IDownstreamDependencyMetadataManager _depMetadataManager;

    public AzureMetadataTests()
    {
        var sp = new ServiceCollection()
            .AddAzureSearchDownstreamDependencyMetadata()
            .AddAzureCosmosDBDownstreamDependencyMetadata()
            .BuildServiceProvider();
        _depMetadataManager = sp.GetRequiredService<IDownstreamDependencyMetadataManager>();
    }

    [Fact]
    public void GetRequestMetadata_WithValidUrl_ReturnsCorrectMetadata()
    {
        string[] filePaths = { "TestInput/AzureCosmosDB.txt", "TestInput/AzureSearch.txt" };

        string httpMethod;
        string urlString;
        string expectedRequestName;
        foreach (var filePath in filePaths)
        {
            string[] testEntries = File.ReadAllLines(filePath);
            foreach (var testEntry in testEntries)
            {
                var tokens = testEntry.Split(' ');
                httpMethod = tokens[0];
                urlString = tokens[1];
                expectedRequestName = tokens[2];
                using var requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod(method: httpMethod),
                    RequestUri = new Uri(uriString: urlString)
                };

                var requestMetadata = _depMetadataManager.GetRequestMetadata(requestMessage);
                Assert.NotNull(requestMetadata);
                Assert.Equal(expectedRequestName, requestMetadata!.RequestName);
            }
        }
    }
}
