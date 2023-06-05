// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public static class TestHelpers
{
    public static async Task TestReturnStatus<T>(
        this Task<IDatabaseResponse<T>> task,
        T? itemToExpect,
        HttpStatusCode codeToExpect = HttpStatusCode.OK,
        bool statusToExpect = true,
        int? count = null)
        where T : notnull
    {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        IDatabaseResponse<T> response = await task.ConfigureAwait(false);
#pragma warning restore VSTHRD003

        Assert.True(response.HasStatus(codeToExpect));
        Assert.Equal(statusToExpect, response.Succeeded);
        response.Item.Should().BeEquivalentTo(itemToExpect);
        response.ItemCount.Should().Be(count ?? (itemToExpect != null! ? 1 : 0));
        Assert.True(response.RequestInfo.Cost > 0.99);
    }

    public static async Task TestException<T>(
        this Task<IDatabaseResponse<T>> task,
        HttpStatusCode codeToExpect)
        where T : notnull
    {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        DatabaseException exception = await Assert.ThrowsAsync<DatabaseException>(() => task).ConfigureAwait(false);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks

        Assert.Equal((int)codeToExpect, exception.StatusCode);
    }

    public static Task TestFailure<T>(
        this Task<IDatabaseResponse<T>> task,
        HttpStatusCode codeToExpect)
        where T : class
    {
        return codeToExpect == HttpStatusCode.NotFound || codeToExpect == HttpStatusCode.Conflict
            ? task.TestReturnStatus(null, codeToExpect, false)
            : task.TestException(codeToExpect);
    }
}
