// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The implementation of builder for Cosmos Database accessors.
/// </summary>
internal sealed class TableAccessorBuilder : ITableAccessorBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services => Configurer.DatabaseBuilder.ServiceCollection;

    /// <inheritdoc/>
    public IDatabaseBuilder DatabaseBuilder => Configurer.DatabaseBuilder;

    /// <inheritdoc/>
    public ITableConfigurer TableConfigurer => Configurer;

    internal TableConfigurer Configurer { get; }
    internal Func<IServiceProvider, TableOptions> OptionsGetter { get; }

    public TableAccessorBuilder(TableConfigurer configurer, Func<IServiceProvider, TableOptions> optionsGetter)
    {
        Configurer = configurer;
        OptionsGetter = optionsGetter;
    }

    /// <inheritdoc/>
    public ITableAccessorBuilder AddReader<TDocument>()
        where TDocument : notnull
    {
        _ = Services.AddSingleton(
            provider => Configurer
                .DatabaseGetter
                .Invoke(provider)
                .GetDocumentReader<TDocument>(OptionsGetter.Invoke(provider)));

        return this;
    }

    /// <inheritdoc/>
    public ITableAccessorBuilder AddWriter<TDocument>()
        where TDocument : notnull
    {
        _ = Services.AddSingleton(
            provider => Configurer
                .DatabaseGetter
                .Invoke(provider)
                .GetDocumentWriter<TDocument>(OptionsGetter.Invoke(provider)));

        return this;
    }
}
