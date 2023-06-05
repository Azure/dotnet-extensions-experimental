// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// The interface for call decorator chaining.
/// </summary>
/// <typeparam name="TContext">The type of context.</typeparam>
internal interface ICallDecorationPipeline<TContext>
{
    /// <summary>
    /// The method to execute provided function on preconstructed pipeline.
    /// </summary>
    /// <typeparam name="T">The type of output wrapped into <see cref="Task{T}"/>.</typeparam>
    /// <param name="functionToCall">The function to execute.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="exceptionHandler">The exception handler.
    /// Used only when specific exception instructed to be skipped.</param>
    /// <param name="cancellationToken">The cancelation token.</param>
    /// <returns>A <see cref="Task{T}"/> wrapping a result of the function call.</returns>
    Task<T> DoCallAsync<T>(
        Func<TContext, Func<Exception, T>, CancellationToken, Task<T>> functionToCall,
        TContext context,
        Func<Exception, T> exceptionHandler,
        CancellationToken cancellationToken);
}
