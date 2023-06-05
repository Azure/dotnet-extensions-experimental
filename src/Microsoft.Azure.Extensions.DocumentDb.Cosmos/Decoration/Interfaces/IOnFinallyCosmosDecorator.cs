// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// The interface for decorating after call finally logic.
/// </summary>
/// <typeparam name="TContext">The type of context will be provided.</typeparam>
public interface IOnFinallyCosmosDecorator<TContext> : ICosmosDecorator<TContext>
{
    /// <summary>
    /// The method will be called after method call and all other decorations.
    /// </summary>
    /// <remarks>
    /// Use this method to release decorator used resources.
    /// </remarks>
    /// <param name="context">The context.</param>
    void OnFinally(TContext context);
}
