// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// Implements chaining of <see cref="IOnCallCosmosDecorator{TContext}"/> calls.
/// </summary>
/// <typeparam name="TContext">The decoration context type.</typeparam>
internal readonly struct CallDecorationPipeline<TContext> : ICallDecorationPipeline<TContext>
{
    private readonly IOnCallCosmosDecorator<TContext> _call;
    private readonly ICallDecorationPipeline<TContext> _pipeline;

    public CallDecorationPipeline(IOnCallCosmosDecorator<TContext> call, ICallDecorationPipeline<TContext> pipeline)
    {
        _call = call;
        _pipeline = pipeline;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Applies predefined <see cref="IOnCallCosmosDecorator{TContext}"/> on the predefined
    /// <see cref="ICallDecorationPipeline{TContext}"/> to execute given function code in given context.
    /// </remarks>
    public Task<T> DoCallAsync<T>(
        Func<TContext, Func<Exception, T>, CancellationToken, Task<T>> functionToCall,
        TContext context,
        Func<Exception, T> exceptionHandler,
        CancellationToken cancellationToken)
    {
#pragma warning disable R9A034 // Optimize method group use to avoid allocations
        return _call.OnCallAsync(_pipeline.DoCallAsync, functionToCall, context, exceptionHandler, cancellationToken);
#pragma warning restore R9A034 // Optimize method group use to avoid allocations
    }
}
