// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Extensions.Telemetry;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Xunit;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

public class HttpClientLoggingExtensionsTest
{
    [Fact]
    public async Task AddHttpClientLogging_WithDownstreamDependencyMetadataManager_FetchesCorrectMetadata()
    {
        const string RequestPath = "https://fake.documents.azure.com/dbs/123db/colls/randomcoll111";

        await using var sp = new ServiceCollection()
            .AddFakeLogging()
            .AddFakeRedaction()
            .AddHttpClient()
            .AddDefaultHttpClientLogging()
            .AddAzureCosmosDBDownstreamDependencyMetadata()
            .BlockRemoteCall()
            .BuildServiceProvider();

        using var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("testClient");
        using var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(RequestPath),
        };

        var requestContext = sp.GetRequiredService<IOutgoingRequestContext>();
        _ = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

        var collector = sp.GetFakeLogCollector();
        var logRecord = collector.GetSnapshot().Single(logRecord => logRecord.Category == "Microsoft.Extensions.Http.Telemetry.Logging.Internal.HttpLoggingHandler");
        var state = logRecord.State as List<KeyValuePair<string, string>>;
        state!.Single(kvp => kvp.Key == "httpPath").Value.Should().Be("dbs/REDACTED/colls/REDACTED");
    }
}

