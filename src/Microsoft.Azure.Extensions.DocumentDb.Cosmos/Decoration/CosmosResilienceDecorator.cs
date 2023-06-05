// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// <see cref="IOnCallCosmosDecorator{TContext}"/> for wrapping a call by <see cref="IAsyncPolicy"/>.
/// </summary>
internal sealed class CosmosResilienceDecorator
    : IOnCallCosmosDecorator<DecoratedCosmosContext>
{
    private readonly IAsyncPolicy _asyncPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosResilienceDecorator"/> class.
    /// </summary>
    /// <param name="asyncPolicy">The async policy to be used by decorator.</param>
    public CosmosResilienceDecorator(IAsyncPolicy asyncPolicy)
    {
        _asyncPolicy = asyncPolicy;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Implements <see cref="IOnCallCosmosDecorator{TContext}.OnCallAsync{T}"/> by wrapping a call by <see cref="IAsyncPolicy"/>.
    /// </remarks>
    public Task<T> OnCallAsync<T>(Func<Func<DecoratedCosmosContext, Func<Exception, T>, CancellationToken, Task<T>>,
        DecoratedCosmosContext, Func<Exception, T>, CancellationToken, Task<T>> callToBeDecorated,
        Func<DecoratedCosmosContext, Func<Exception, T>, CancellationToken, Task<T>> functionParameter,
        DecoratedCosmosContext context,
        Func<Exception, T> exceptionHandler,
        CancellationToken cancelationToken)
    {
        return _asyncPolicy.ExecuteAsync(() => callToBeDecorated(functionParameter, context, exceptionHandler, cancelationToken));
    }
}
