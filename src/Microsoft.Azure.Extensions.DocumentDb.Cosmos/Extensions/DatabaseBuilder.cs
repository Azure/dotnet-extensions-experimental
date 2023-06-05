// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The implementation of builder for Cosmos Database.
/// </summary>
internal sealed class DatabaseBuilder : IDatabaseBuilder
{
    private const string DatabaseNotConfiguredError = "The database is not configured. Please use Configure before.";

    // The operation is not frequent, so do not use Pool of Lists to allocate a new one.
    private readonly List<Func<IServiceProvider, ICosmosDecorator<DecoratedCosmosContext>>> _customDecorators = new();

    internal IServiceCollection ServiceCollection { get; }
    internal bool CreateMissingDatabases;

    private Func<IServiceProvider, ICosmosEncryptionProvider>? _cosmosEncryptionGetter;
    private Func<IServiceProvider, IAsyncPolicy>? _resiliencePolicyGetter;
    private Func<IServiceProvider, ITableLocator>? _tableLocatorGetter;
    private Func<IServiceProvider, DatabaseOptions>? _databaseOptionsGetter;

    internal DatabaseBuilder(IServiceCollection serviceCollection)
    {
        ServiceCollection = serviceCollection;
    }

    /// <inheritdoc/>
    public IDatabaseBuilder EnableEncryption<T>()
        where T : class, ICosmosEncryptionProvider
    {
        // If T allow simple injection, it will save customer a line.
        // If it is not, user may override, by using AddSingleton
        ServiceCollection.TryAddSingleton<T>();
        return EnableEncryption(provider => provider.GetRequiredService<T>());
    }

    /// <inheritdoc/>
    public IDatabaseBuilder EnableEncryption(Func<IServiceProvider, ICosmosEncryptionProvider> encriptionGetter)
    {
        _cosmosEncryptionGetter = Throw.IfNull(encriptionGetter);
        return this;
    }

    /// <inheritdoc/>
    public IDatabaseBuilder EnableTableLocator<T>()
        where T : class, ITableLocator
    {
        // If T allow simple injection, it will save customer a line.
        // If it is not, user may override, by using AddSingleton
        ServiceCollection.TryAddSingleton<T>();
        return EnableTableLocator(provider => provider.GetRequiredService<T>());
    }

    /// <inheritdoc/>
    public IDatabaseBuilder EnableTableLocator(Func<IServiceProvider, ITableLocator> locatorGetter)
    {
        _tableLocatorGetter = Throw.IfNull(locatorGetter);
        return this;
    }

    /// <inheritdoc/>
    public IDatabaseBuilder EnableResilience(Func<IServiceProvider, IAsyncPolicy> policyGetter)
    {
        _resiliencePolicyGetter = Throw.IfNull(policyGetter);
        return this;
    }

    /// <inheritdoc/>
    public IDatabaseBuilder AddDecorator<T>()
        where T : class, ICosmosDecorator<DecoratedCosmosContext>
    {
        // If T allow simple injection, it will save customer a line.
        // If it is not, user may override, by using AddSingleton
        ServiceCollection.TryAddSingleton<T>();
        return AddDecorator(provider => provider.GetRequiredService<T>());
    }

    /// <inheritdoc/>
    public IDatabaseBuilder AddDecorator(Func<IServiceProvider, ICosmosDecorator<DecoratedCosmosContext>> decoratorGetter)
    {
        _customDecorators.Add(Throw.IfNull(decoratorGetter));
        return this;
    }

    /// <inheritdoc/>
    public IDatabaseBuilder Configure(Func<IServiceProvider, DatabaseOptions> optionsGetter)
    {
        _databaseOptionsGetter = Throw.IfNull(optionsGetter);
        return this;
    }

    /// <inheritdoc/>
    public IDatabaseBuilder Configure(string? context)
    {
        if (context == null)
        {
            _databaseOptionsGetter = provider => provider
                .GetRequiredService<IOptions<DatabaseOptions>>()
                .Validate();
        }
        else
        {
            _databaseOptionsGetter = provider => provider
                .GetRequiredService<IOptionsMonitor<DatabaseOptions>>()
                .Validate(context);
        }

        return this;
    }

    /// <inheritdoc/>
    public IDatabaseBuilder Configure()
        => Configure((string?)null);

    /// <inheritdoc/>
    public IDatabaseBuilder Configure<T>()
        where T : DatabaseOptions, new()
    {
        _databaseOptionsGetter = provider => provider
            .GetRequiredService<IOptions<T>>()
            .Validate();
        return this;
    }

    /// <inheritdoc/>
    public ITableConfigurer BuildDatabase()
    {
        return BuildDatabase<DatabaseBuilder>();
    }

    /// <inheritdoc/>
    public ITableConfigurer BuildDatabase<TContext>()
        where TContext : class
    {
        _databaseOptionsGetter = InternalThrows.IfNull(_databaseOptionsGetter, DatabaseNotConfiguredError);

        // Developer note:
        // Below lambda should not be reused more than 1 time.
        Func<IServiceProvider, CosmosDocumentDatabase<TContext>> databaseProvider = provider =>
        {
            var options = _databaseOptionsGetter(provider);

            List<ICosmosDecorator<DecoratedCosmosContext>> clientDecorators = new()
            {
                // First decorator should be exception handling.
                // So that other decorators can benefit from right exceptions in OnException.
                // Exception handling is a part of contract, so it is not optional like most of others.
                new CosmosExceptionHandlingDecorator()
            };

            // Add all customer provided decorators.
            foreach (var decoratorGetter in _customDecorators)
            {
                clientDecorators.Add(decoratorGetter(provider));
            }

            if (_resiliencePolicyGetter != null)
            {
                // Add resilience to the end of all decorators.
                // Just in case a customer defines other OnCall decorators
                // Resilience should be the last in the chain,
                // so that retries will repeat the whole sequence of calls.
                clientDecorators.Add(new CosmosResilienceDecorator(_resiliencePolicyGetter(provider)));
            }

            ICosmosEncryptionProvider? encryptionProvider = _cosmosEncryptionGetter?.Invoke(provider);
            ITableLocator? tableLocator = _tableLocatorGetter?.Invoke(provider);

            return new CosmosDocumentDatabase<TContext>(options, encryptionProvider, clientDecorators, tableLocator);
        };

        Func<IServiceProvider, IDocumentDatabase> databaseGetter;

        if (typeof(TContext).IsAssignableFrom(typeof(DatabaseBuilder)))
        {
            _ = ServiceCollection.AddSingleton<IDocumentDatabase>(databaseProvider);
            databaseGetter = provider => provider.GetRequiredService<IDocumentDatabase>();
        }
        else
        {
            _ = ServiceCollection.AddSingleton<IDocumentDatabase<TContext>>(databaseProvider);
            databaseGetter = provider => provider.GetRequiredService<IDocumentDatabase<TContext>>();
        }

        _ = ServiceCollection
            .AddStartupInitialization()
            .AddInitializer((provider, cancelationToken)
                => databaseGetter(provider)
                .ConnectAsync(CreateMissingDatabases, cancelationToken));

        return new TableConfigurer(this, databaseGetter);
    }

    public IDatabaseBuilder CreateDatabaseIfNotExists()
    {
        CreateMissingDatabases = true;
        return this;
    }
}
