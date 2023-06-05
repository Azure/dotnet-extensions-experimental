// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// The interface for decorating call exception processing logic.
/// </summary>
/// <typeparam name="TContext">The type of context will be provided.</typeparam>
public interface IOnExceptionCosmosDecorator<TContext> : ICosmosDecorator<TContext>
{
    /// <summary>
    /// The method will be invoked if exception received during the call.
    /// </summary>
    /// <remarks>
    /// The method can throw a new exception, it will be used then instead of incoming one.
    /// Returning true, the method tells calee that exception can be ignored.
    /// It is up to callee how to generate the value.
    /// </remarks>
    /// <param name="context">The context.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>true if the exception can be ignored, false otherwise.</returns>
    bool OnException(TContext context, Exception exception);
}
