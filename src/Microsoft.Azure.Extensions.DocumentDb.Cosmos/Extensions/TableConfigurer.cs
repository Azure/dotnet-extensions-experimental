// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Collections;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

internal sealed class TableConfigurer : ITableConfigurer
{
    internal DatabaseBuilder DatabaseBuilder { get; }
    internal Func<IServiceProvider, IDocumentDatabase> DatabaseGetter { get; }
    internal bool CreateIfNotExists;

    public TableConfigurer(DatabaseBuilder databaseBuilder, Func<IServiceProvider, IDocumentDatabase> databaseGetter)
    {
        DatabaseBuilder = databaseBuilder;
        DatabaseGetter = databaseGetter;
    }

    public ITableAccessorBuilder ConfigureTable<T>(string? context)
        where T : TableOptions, new()
    {
        Func<IServiceProvider, TableOptions> optionsGetter = context == null
            ? provider => provider.GetRequiredService<IOptions<T>>().Validate()
            : provider => provider.GetRequiredService<IOptionsMonitor<T>>().Validate(context);

        return ConfigureTable(optionsGetter);
    }

    public ITableAccessorBuilder ConfigureTable<T>()
        where T : TableOptions, new()
        => ConfigureTable<T>(null);

    public ITableAccessorBuilder ConfigureTable(Func<IServiceProvider, TableOptions> optionsGetter)
    {
        if (CreateIfNotExists)
        {
            _ = DatabaseBuilder.ServiceCollection
                .AddStartupInitialization()
                .AddInitializer(async (provider, cancellationToken) =>
                {
                    var database = DatabaseGetter(provider);
                    await database.ConnectAsync(true, cancellationToken).ConfigureAwait(false);

                    var tableOptions = optionsGetter(provider);

                    if (tableOptions.IsRegional)
                    {
                        IEnumerable<string> regions = Empty.ReadOnlyCollection<string>();
                        if (database is IInternalDatabase internalDatabase)
                        {
                            regions = internalDatabase.ConfiguredRegions;
                        }

                        regions = InternalThrows.IfNullOrEmptyEnumerable<IEnumerable<string>, string>(
                            regions, "No regions configured for regional table.");

                        foreach (string region in regions)
                        {
                            _ = await database.CreateTableAsync(tableOptions, new() { Region = region }, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        _ = await database.CreateTableAsync(tableOptions, new(), cancellationToken)
                            .ConfigureAwait(false);
                    }
                });
        }

        return new TableAccessorBuilder(this, optionsGetter);
    }

    public ITableConfigurer CreateTableIfNotExists()
    {
        CreateIfNotExists = true;
        return this;
    }
}
