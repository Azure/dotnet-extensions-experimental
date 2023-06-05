// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// <see cref="IOnExceptionCosmosDecorator{TContext}"/> for handling <see cref="CosmosException"/>.
/// </summary>
internal sealed class CosmosExceptionHandlingDecorator : IOnExceptionCosmosDecorator<DecoratedCosmosContext>
{
#if !NETCOREAPP3_1_OR_GREATER
    // HttpStatusCode.TooManyRequests is not available in some .NET versions
    private const HttpStatusCode TooManyRequestsCode = (HttpStatusCode)429;
#endif
    private const int StaleSessionErrorCode = 1002;
    private const HttpStatusCode TransientErrorCode = (HttpStatusCode)449;

    /// <inheritdoc/>
    /// <remarks>
    /// Handles <see cref="CosmosException"/> by wrapping it by a specific type of <see cref="DatabaseException"/>.
    /// </remarks>
    public bool OnException(DecoratedCosmosContext context, Exception exception)
    {
        if (exception is not CosmosException cosmosException)
        {
            return false;
        }

        RequestInfo request = context.GetRequest(cosmosException.RequestCharge);

        switch (cosmosException.StatusCode)
        {
            case HttpStatusCode.BadRequest:
            case HttpStatusCode.Unauthorized:
            case HttpStatusCode.Forbidden:
            case HttpStatusCode.RequestEntityTooLarge:
            case HttpStatusCode.PreconditionFailed:
                throw new DatabaseServerException(
                    exception.Message,
                    exception,
                    (int)cosmosException.StatusCode,
                    cosmosException.SubStatusCode,
                    request);

            // https://docs.microsoft.com/en-us/azure/cosmos-db/sql/troubleshoot-dot-net-sdk?tabs=diagnostics-v3#retry-logic-
#if NETCOREAPP3_1_OR_GREATER
            case HttpStatusCode.TooManyRequests:
#else
            case TooManyRequestsCode:
#endif
            case HttpStatusCode.RequestTimeout:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.Gone:
            case TransientErrorCode:
                throw new DatabaseRetryableException(
                    exception.Message,
                    exception,
                    (int)cosmosException.StatusCode,
                    cosmosException.SubStatusCode,
                    cosmosException.RetryAfter,
                    request);

            case HttpStatusCode.NotFound:
                // This case will happen on session consistency level and read region session is stale,
                // should retry after read region is in sync.
                if (cosmosException.SubStatusCode == StaleSessionErrorCode)
                {
                    throw new DatabaseRetryableException(
                        exception.Message,
                        exception,
                        (int)cosmosException.StatusCode,
                        cosmosException.SubStatusCode,
                        cosmosException.RetryAfter,
                        request);
                }

                break;

            case HttpStatusCode.Conflict:
                // This code is expected behavior on writes. Return an unsuccessful result instead of exception.
                break;

            default:
                throw new DatabaseException(
                    exception.Message,
                    exception,
                    (int)cosmosException.StatusCode,
                    cosmosException.SubStatusCode,
                    request);
        }

        return true;
    }
}
