// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The interface for defining table accessors.
/// </summary>
public interface ITableAccessorBuilder
{
    /// <summary>
    /// Adds an instance of <see cref="IDocumentReader{TDocument}"/> to <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a table schema.
    /// Request results will be mapped to instance of this type.
    /// </typeparam>
    /// <returns><see cref="ITableAccessorBuilder"/> for chaining calls.</returns>
    ITableAccessorBuilder AddReader<TDocument>()
        where TDocument : notnull;

    /// <summary>
    /// Adds an instance of <see cref="IDocumentWriter{TDocument}"/> to <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TDocument">
    /// The document entity type to be used as a table schema.
    /// Request results will be mapped to instance of this type.
    /// </typeparam>
    /// <returns><see cref="ITableAccessorBuilder"/> for chaining calls.</returns>
    ITableAccessorBuilder AddWriter<TDocument>()
        where TDocument : notnull;

    /// <summary>
    /// Gets the instance of <see cref="IServiceCollection"/> to continue adding services.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the link to the database builder to continue building databases.
    /// </summary>
    IDatabaseBuilder DatabaseBuilder { get; }

    /// <summary>
    /// Gets the link to the table configurer to continue defining table accessors.
    /// </summary>
    ITableConfigurer TableConfigurer { get; }
}
