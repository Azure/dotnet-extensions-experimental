// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Polly;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The interface for Cosmos Database builder.
/// </summary>
public interface IDatabaseBuilder
{
    /// <summary>
    /// Instructs builder to enable encryption on client.
    /// </summary>
    /// <typeparam name="T">The <see cref="ICosmosEncryptionProvider"/> implementation.</typeparam>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder EnableEncryption<T>()
        where T : class, ICosmosEncryptionProvider;

    /// <summary>
    /// Instructs builder to enable encryption on client.
    /// </summary>
    /// <param name="encriptionGetter">The encryption provider getter.</param>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder EnableEncryption(Func<IServiceProvider, ICosmosEncryptionProvider> encriptionGetter);

    /// <summary>
    /// Instructs to use table locator.
    /// </summary>
    /// <remarks>
    /// This setting is required then and only then when any table in the database has enabled <see cref="TableOptions.IsLocatorRequired"/>.
    /// <seealso cref="ITableLocator"/>.
    /// </remarks>
    /// <typeparam name="T">The <see cref="ITableLocator"/> implementation.</typeparam>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder EnableTableLocator<T>()
        where T : class, ITableLocator;

    /// <summary>
    /// Instructs to use table locator.
    /// </summary>
    /// <remarks>
    /// This setting is required then and only then when any table in the database has enabled <see cref="TableOptions.IsLocatorRequired"/>.
    /// <seealso cref="ITableLocator"/>.
    /// </remarks>
    /// <param name="locatorGetter">The table locator getter.</param>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder EnableTableLocator(Func<IServiceProvider, ITableLocator> locatorGetter);

    /// <summary>
    /// Enables resilience for produced clients, based on provided policy.
    /// </summary>
    /// <param name="policyGetter">The async policy getter.</param>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder EnableResilience(Func<IServiceProvider, IAsyncPolicy> policyGetter);

    /// <summary>
    /// Adds a decorator for future clients.
    /// </summary>
    /// <typeparam name="T">The implementation of <see cref="ICosmosDecorator{DecoratedCosmosContext}"/>.</typeparam>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder AddDecorator<T>()
        where T : class, ICosmosDecorator<DecoratedCosmosContext>;

    /// <summary>
    /// Adds a decorator for future clients.
    /// </summary>
    /// <param name="decoratorGetter">The decorator getter.</param>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder AddDecorator(Func<IServiceProvider, ICosmosDecorator<DecoratedCosmosContext>> decoratorGetter);

    /// <summary>
    /// Configures database using an options getter.
    /// </summary>
    /// <param name="optionsGetter">The options getter.</param>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder Configure(Func<IServiceProvider, DatabaseOptions> optionsGetter);

    /// <summary>
    /// Configures database using <see cref="DatabaseOptions"/> from context.
    /// </summary>
    /// <param name="context">The name of context.</param>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder Configure(string? context);

    /// <summary>
    /// Configures database using <see cref="DatabaseOptions"/> from context.
    /// </summary>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder Configure();

    /// <summary>
    /// Configures database using <see cref="DatabaseOptions"/> from context.
    /// </summary>
    /// <typeparam name="T">The type of options to configure.</typeparam>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder Configure<T>()
        where T : DatabaseOptions, new();

    /// <summary>
    /// Instructs to create missing databases.
    /// </summary>
    /// <returns>Instance of <see cref="IDatabaseBuilder"/> to continue building.</returns>
    IDatabaseBuilder CreateDatabaseIfNotExists();

    /// <summary>
    /// Builds a document database using provided database options.
    /// </summary>
    /// <remarks>
    /// This method adds created database to the service collection.
    /// </remarks>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    /// <returns>The <see cref="ITableConfigurer"/> to define table accessors.</returns>
    ITableConfigurer BuildDatabase();

    /// <summary>
    /// Builds a context based document database using provided database options.
    /// </summary>
    /// <exception cref="DatabaseClientException">Thrown when an error occurred on a client side.
    /// For example on a bad request, permissions error or client timeout.</exception>
    /// <exception cref="DatabaseServerException">Thrown when an error occurred on a database server side,
    /// including internal server error.</exception>
    /// <exception cref="DatabaseRetryableException">Thrown when a request failed but can be retried.
    /// This includes throttling and server not available cases. </exception>
    /// <exception cref="DatabaseException">A generic exception thrown in all other not covered above cases.</exception>
    /// <returns>The <see cref="ITableConfigurer"/> to define table accessors.</returns>
    /// <typeparam name="TContext">The context type, indicating injection preferences.</typeparam>
    ITableConfigurer BuildDatabase<TContext>()
        where TContext : class;
}
