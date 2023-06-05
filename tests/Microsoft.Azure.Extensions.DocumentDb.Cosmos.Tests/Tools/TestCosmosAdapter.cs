// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Collections.Generic;
using Microsoft.Azure.Extensions.Cosmos.DocumentStorage;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Microsoft.Azure.Extensions.Document.Cosmos.Model;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Xunit;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

public static class TestCosmosAdapter
{
    private readonly struct EmptyDecorator : ICosmosDecorator<DecoratedCosmosContext>
    {
        // Empty by design
    }

    public const string TestRegion = "TestRegion";

    // The key used to log in into emulator
    // It is the same in all installations
    private const string CosmosDbEmulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private static readonly string _endpoint = "https://localhost:8081?testName=";

    internal static CosmosDocumentDatabase<DatabaseBuilder> CreateCosmosAdapter(
        DatabaseOptions options,
        bool enableResilience = true,
        ITableLocator? locator = null,
        ICosmosEncryptionProvider? cosmosEncryption = null,
        IAsyncPolicy? policy = null,
        bool createDatabase = false)
    {
        IServiceCollection services = new ServiceCollection();

        var databaseBuilder = (DatabaseBuilder)services.GetCosmosDatabaseBuilder();

        if (enableResilience)
        {
            databaseBuilder.EnableResilience(_ => policy ?? Policy
                .Handle<DatabaseRetryableException>()
                .Or<DatabaseServerException>()
                .Or<DatabaseException>()
                .RetryAsync(20));
        }

        if (createDatabase)
        {
            Assert.False(databaseBuilder.CreateMissingDatabases);
            databaseBuilder.CreateDatabaseIfNotExists();
            Assert.True(databaseBuilder.CreateMissingDatabases);
        }

        if (locator is not null)
        {
            databaseBuilder.EnableTableLocator(_ => locator);
        }

        if (cosmosEncryption is not null)
        {
            databaseBuilder.EnableEncryption(_ => cosmosEncryption);
        }

        _ = databaseBuilder
            .Configure(_ => options)
            .AddDecorator(_ => default(EmptyDecorator))
            .BuildDatabase();

        IDocumentDatabase adapter = services.BuildServiceProvider().GetRequiredService<IDocumentDatabase>();

        Assert.IsType<CosmosDocumentDatabase<DatabaseBuilder>>(adapter);
        return (CosmosDocumentDatabase<DatabaseBuilder>)adapter;
    }

    internal static CosmosDocumentDatabase<DatabaseBuilder> CreateCosmosAdapter(
        string testName,
        bool enableResilience = true,
        ITableLocator? locator = null,
        ICosmosEncryptionProvider? cosmosEncryption = null)
    {
        return CreateCosmosAdapter(CreateDatabaseOptions(testName), enableResilience, locator, cosmosEncryption);
    }

    public static CosmosDatabaseOptions CreateDatabaseOptions(string testName, bool enableAdditionalOptions = true)
    {
        // While testing different platforms, same tests should coexist.
        long timeSuffix = DateTime.UtcNow.Ticks;
        string databaseName = $"{testName}-Database-{timeSuffix}";
        Uri uri = new Uri(_endpoint + databaseName);

        return new()
        {
            DatabaseName = databaseName,
            IdleTcpConnectionTimeout = TimeSpan.FromMinutes(10),
            PrimaryKey = CosmosDbEmulatorKey,
            Endpoint = uri,
            RegionalDatabaseOptions = new Dictionary<string, RegionalDatabaseOptions>
            {
                {
                    TestRegion,
                    new()
                    {
                        DatabaseName = $"{TestRegion}-{databaseName}",
                        Endpoint = new Uri($"{uri}?region={TestRegion}"),
                        FailoverRegions = new List<string>(),
                    }
                },
            },
            EnableGatewayMode = enableAdditionalOptions,
            EnablePrivatePortPool = enableAdditionalOptions,
            EnableTcpEndpointRediscovery = enableAdditionalOptions,
        };
    }

    public static TableOptions GetContainerOptions(string testName, string? region = null)
        => TestCosmosClient.GetContainerOptions<TableOptions>(testName, region);

    public static DatabaseOptions CreateGenericDatabaseOptions(string testName)
    {
        CosmosDatabaseOptions options = CreateDatabaseOptions(testName);

        return new()
        {
            DatabaseName = options.DatabaseName,
            IdleTcpConnectionTimeout = options.IdleTcpConnectionTimeout,
            PrimaryKey = options.PrimaryKey,
            Endpoint = options.Endpoint,
            RegionalDatabaseOptions = options.RegionalDatabaseOptions,
        };
    }
}
