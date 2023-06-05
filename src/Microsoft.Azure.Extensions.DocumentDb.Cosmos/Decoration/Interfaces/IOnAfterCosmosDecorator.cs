// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// The interface for decorating after call logic.
/// </summary>
/// <typeparam name="TContext">The type of context will be provided.</typeparam>
public interface IOnAfterCosmosDecorator<TContext> : ICosmosDecorator<TContext>
{
    /// <summary>
    /// The method will be invoked after the call.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="context">The context.</param>
    /// <param name="result">The result.</param>
    void OnAfter<T>(TContext context, T result);
}
