// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// The parent interface for all decorators.
/// </summary>
/// <typeparam name="TContext">The type of context will be provided.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S4023:Interfaces should not be empty",
    Justification = "Declaring this to be able to pass array of different decorators.")]
public interface ICosmosDecorator<TContext>
{
}
