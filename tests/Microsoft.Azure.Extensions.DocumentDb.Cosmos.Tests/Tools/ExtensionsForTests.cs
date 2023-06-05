// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

public static class ExtensionsForTests
{
    internal static async Task DeleteDatabaseAsync(this BaseCosmosClient client, CancellationToken cancellationToken)
    {
        var database = (IDocumentDatabase)client.Database;
        var response = await database.DeleteDatabaseAsync(cancellationToken);
        response.Succeeded.Should().BeTrue();
        ((HttpStatusCode)response.Status).Should().Be(HttpStatusCode.OK);
        response.Item.Should().BeTrue();
    }
}
