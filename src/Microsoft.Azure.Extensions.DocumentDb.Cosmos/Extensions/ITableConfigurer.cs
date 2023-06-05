// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The interface allows to select a table for building accessors.
/// </summary>
public interface ITableConfigurer
{
    /// <summary>
    /// Instructs to create table if it is not exists.
    /// </summary>
    /// <returns>The <see cref="ITableAccessorBuilder"/> to create table accessors.</returns>
    ITableConfigurer CreateTableIfNotExists();

    /// <summary>
    /// Configures the cosmos table.
    /// </summary>
    /// <typeparam name="T">The table options type.</typeparam>
    /// <param name="context">The context where options defined.</param>
    /// <returns>The <see cref="ITableAccessorBuilder"/> to create table accessors.</returns>
    ITableAccessorBuilder ConfigureTable<T>(string? context)
        where T : TableOptions, new();

    /// <summary>
    /// Configures the cosmos table.
    /// </summary>
    /// <typeparam name="T">The table options type.</typeparam>
    /// <returns>The <see cref="ITableAccessorBuilder"/> to create table accessors.</returns>
    ITableAccessorBuilder ConfigureTable<T>()
        where T : TableOptions, new();

    /// <summary>
    /// Configures the cosmos table.
    /// </summary>
    /// <param name="optionsGetter">The options getter.</param>
    /// <returns>The <see cref="ITableAccessorBuilder"/> to define table accessors.</returns>
    ITableAccessorBuilder ConfigureTable(Func<IServiceProvider, TableOptions> optionsGetter);
}
