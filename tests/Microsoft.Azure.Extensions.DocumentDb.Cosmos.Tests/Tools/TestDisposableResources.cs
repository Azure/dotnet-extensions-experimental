// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public sealed class TestDisposableResources<TDocument> : IAsyncDisposable
    where TDocument : notnull
{
    private readonly BaseCosmosDocumentClient<TDocument>? _client;
    private readonly IDocumentDatabase? _adapter;

    private readonly bool _clientSet;
    private readonly bool _adapterSet;

    public TestDisposableResources(IDocumentReader<TDocument>? client)
    {
        _client = (BaseCosmosDocumentClient<TDocument>?)client;

        _clientSet = client != null;
        _adapterSet = false;
    }

    public TestDisposableResources(IDocumentDatabase? adapter)
    {
        _client = null;
        _adapter = adapter;

        _adapterSet = adapter != null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_clientSet)
        {
            await _client!.DeleteDatabaseAsync(CancellationToken.None);
        }
        else if (_adapterSet)
        {
            var response = await _adapter!.DeleteDatabaseAsync(CancellationToken.None);
            response.Succeeded.Should().BeTrue();
            ((HttpStatusCode)response.Status).Should().Be(HttpStatusCode.OK);
            response.Item.Should().BeTrue();
        }
    }
}
