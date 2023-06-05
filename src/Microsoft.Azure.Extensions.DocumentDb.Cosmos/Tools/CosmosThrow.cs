// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

internal static class CosmosThrow
{
    /// <summary>
    /// Throws a <see cref="DatabaseClientException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    internal static void DatabaseClientException(string message)
        => throw new DatabaseClientException(message);

    /// <summary>
    /// Throws a <see cref="DatabaseRetryableException"/>.
    /// </summary>
    /// <param name="message">The exception message.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    internal static void DatabaseRetryableException(string message)
        => throw new DatabaseRetryableException(message);

    /// <summary>
    /// Throws a new instance of the <see cref="DatabaseServerException"/> class.
    /// </summary>
    /// <param name="operationName">The failed operation name.</param>
    /// <param name="statusCode">The http status code.</param>
    /// <param name="requestInfo">The request info.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    [ExcludeFromCodeCoverage] // Closing } marked as not covered in CI build due to Throw never returns.
    internal static void UnexpectedResult(string operationName, HttpStatusCode? statusCode, RequestInfo requestInfo)
    {
        HttpStatusCode code = statusCode ?? HttpStatusCode.InternalServerError;
        throw new DatabaseServerException($"{operationName} operation failed with http code [{code}].", (int)code, 0, requestInfo);
    }
}
