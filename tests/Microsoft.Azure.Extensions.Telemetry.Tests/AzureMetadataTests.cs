// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Telemetry.Test;

#pragma warning disable CA1505
#pragma warning disable S125
public class AzureMetadataTests
{
    /*
    See https://github.com/Azure/dotnet-extensions-experimental/issues/22

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
    */
}
#pragma warning restore S125
#pragma warning restore CA1505
