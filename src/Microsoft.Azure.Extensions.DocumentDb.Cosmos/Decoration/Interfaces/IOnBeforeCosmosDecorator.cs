// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// The interface for decorating before call logic.
/// </summary>
/// <typeparam name="TContext">The type of context will be provided.</typeparam>
public interface IOnBeforeCosmosDecorator<TContext> : ICosmosDecorator<TContext>
{
    /// <summary>
    /// The method will be invoked before decorated call.
    /// </summary>
    /// <param name="context">The operation context.</param>
    void OnBefore(TContext context);
}
