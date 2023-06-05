// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// The interface for decorating before call logic.
/// </summary>
/// <typeparam name="TContext">The type of context will be provided.</typeparam>
public interface IOnCallCosmosDecorator<TContext> : ICosmosDecorator<TContext>
{
    /// <summary>
    /// The method will be invoked for making decorated async call.
    /// </summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="callToBeDecorated">The call to be decorated.</param>
    /// <param name="functionParameter">The function to be passed to decorated call.
    /// This parameter added to propagate the end method through decoration pipeline.
    /// It allows the decoration pipeline to be preconstructed and executed without extra memory allocations.
    /// </param>
    /// <param name="context">The operation context. Should be provided to the decorated call, can be used by decorator as well.</param>
    /// <param name="exceptionHandler">The exception handler.
    /// Used only when specific exception instructed to be skipped.</param>
    /// <param name="cancelationToken">The cancellation token.
    /// Should be provided to the decorated call and to be respected by the decoration implementation for request cancelation.</param>
    /// <returns><see cref="Task{TResult}"/> wrapping the result.</returns>
    Task<T> OnCallAsync<T>(
        Func<Func<TContext, Func<Exception, T>, CancellationToken, Task<T>>, TContext, Func<Exception, T>, CancellationToken, Task<T>> callToBeDecorated,
        Func<TContext, Func<Exception, T>, CancellationToken, Task<T>> functionParameter,
        TContext context,
        Func<Exception, T> exceptionHandler,
        CancellationToken cancelationToken);
}
