// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Net;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public class ExceptionsTests
{
    private const string TestMessage = "message";

    private const HttpStatusCode TestStatus = HttpStatusCode.Accepted;
    private const HttpStatusCode DefaultStatus = HttpStatusCode.InternalServerError;

    private readonly TimeSpan _defaultTime = TimeSpan.Zero;

    [Fact]
    public void TestStorageRetryableExceptionThrow()
    {
        // retryable exception
        DatabaseRetryableException retryableException = Assert.Throws<DatabaseRetryableException>(
            () => CosmosThrow.DatabaseRetryableException(TestMessage));
        VerifyRetryableException(retryableException, TestMessage, null, DefaultStatus, _defaultTime);
    }

    [Fact]
    public void TestStorageServerExceptionThrow()
    {
        // client exceptions
        DatabaseServerException serverException = Assert.Throws<DatabaseServerException>(
            (Action)(() => throw new DatabaseServerException(TestMessage, (int)TestStatus, 0, default)));
        VerifyException(serverException, TestMessage, null, TestStatus);

        serverException = Assert.Throws<DatabaseServerException>(
            () => CosmosThrow.UnexpectedResult("select", TestStatus, default));
        VerifyException(serverException, $"select operation failed with http code [{TestStatus}].", null, TestStatus);

        serverException = Assert.Throws<DatabaseServerException>(
            () => CosmosThrow.UnexpectedResult("select", null, default));
        VerifyException(serverException,
            $"select operation failed with http code [{HttpStatusCode.InternalServerError}].",
            null, HttpStatusCode.InternalServerError);
    }

    private static void VerifyException(DatabaseException exception,
        string? testMessage, Exception? testException, HttpStatusCode? httpStatusCode)
    {
        Assert.Equal(testMessage ?? $"Exception of type '{exception.GetType().FullName}' was thrown.", exception.Message);
        Assert.Equal(testException, exception.InnerException);
        Assert.Equal((int?)httpStatusCode, exception.StatusCode);
        Assert.Equal(default, exception.RequestInfo);
    }

    private static void VerifyRetryableException(DatabaseRetryableException exception,
        string? testMessage, Exception? testException, HttpStatusCode httpStatusCode,
        TimeSpan testTime)
    {
        Assert.Equal(testMessage ?? $"Exception of type '{exception.GetType().FullName}' was thrown.", exception.Message);
        Assert.Equal(testException, exception.InnerException);
        Assert.Equal((int?)httpStatusCode, exception.StatusCode);
        Assert.Equal(testTime, exception.RetryAfter);
        Assert.Equal(default, exception.RequestInfo);
    }
}
