// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Collections.Generic;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;

public static class Program
{
    public static void Demo()
    {
        var builder = FakeHost.CreateBuilder();
        builder.ConfigureServices(services =>
        {
            ProducerQueue("ProducerQueue", services);
            QueueConsumer("QueueConsumer", services);
            QueueConsumerQueue("QueueConsumerQueue", services);
            QueueConsumerMultipleQueues("QueueConsumerMultipleQueues", services);
        });

        using var host = builder.Build();
        host.Run();
    }

    /// <summary>
    /// Producer --> Queue.
    /// </summary>
    private static void ProducerQueue(string pipelineName, IServiceCollection services)
    {
        _ = services.AddAsyncPipeline(pipelineName)
                    .AddNamedSingleton(sp => new Mock<QueueClient>().Object)
                    .ConfigureMessageDestination(sp =>
                    {
                        var queueClient = sp.GetRequiredService<INamedServiceProvider<QueueClient>>().GetRequiredService(pipelineName);
                        var writeOptions = new AzureStorageQueueWriteOptions(TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
                        return new AzureStorageQueueDestination(queueClient, writeOptions);
                    });
    }

    /// <summary>
    /// Queue --> Consumer.
    /// </summary>
    private static void QueueConsumer(string pipelineName, IServiceCollection services)
    {
        services.AddAsyncPipeline(pipelineName)
                .AddNamedSingleton<ILogger>(sp => new FakeLogger())
                .AddNamedSingleton(sp => new Mock<QueueClient>().Object)
                .ConfigureMessageSource(sp =>
                {
                    var queueClient = sp.GetRequiredService<INamedServiceProvider<QueueClient>>().GetRequiredService(pipelineName);
                    var readOptions = new AzureStorageQueueReadOptions(TimeSpan.FromSeconds(1));
                    return new AzureStorageQueueSource(queueClient, readOptions, () => new FeatureCollection());
                })
                .ConfigureTerminalMessageDelegate(_ => new ReadOnlyMessageDelegate())
                .ConfigureMessageConsumer(sp =>
                {
                    var messageSource = sp.GetRequiredService<INamedServiceProvider<IMessageSource>>().GetRequiredService(pipelineName);
                    var pipelineDelegate = sp.GetRequiredService<INamedServiceProvider<IMessageDelegate>>().GetRequiredService(pipelineName);
                    var logger = sp.GetRequiredService<INamedServiceProvider<ILogger>>().GetRequiredService(pipelineName);
                    return new MessageConsumer(messageSource, pipelineDelegate, logger);
                })
                .RunConsumerAsBackgroundService();
    }

    /// <summary>
    /// Source Queue --> Consumer --> Destination Queue.
    /// </summary>
    private static void QueueConsumerQueue(string pipelineName, IServiceCollection services)
    {
        string source = $"{pipelineName}-source";
        string destination = $"{pipelineName}-destination";

        services.AddAsyncPipeline(pipelineName)
                .AddNamedSingleton<ILogger>(sp => new FakeLogger())
                .AddNamedSingleton(source, sp => new Mock<QueueClient>().Object)
                .AddNamedSingleton(destination, sp => new Mock<QueueClient>().Object)
                .ConfigureMessageSource(sp =>
                {
                    var queueClient = sp.GetRequiredService<INamedServiceProvider<QueueClient>>().GetRequiredService(source);
                    var readOptions = new AzureStorageQueueReadOptions(TimeSpan.FromSeconds(1));
                    return new AzureStorageQueueSource(queueClient, readOptions, () => new FeatureCollection());
                })
                .ConfigureMessageDestination(sp =>
                {
                    var queueClient = sp.GetRequiredService<INamedServiceProvider<QueueClient>>().GetRequiredService(destination);
                    var writeOptions = new AzureStorageQueueWriteOptions(TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
                    return new AzureStorageQueueDestination(queueClient, writeOptions);
                })
                .ConfigureTerminalMessageDelegate(sp =>
                {
                    var messageDestination = sp.GetRequiredService<INamedServiceProvider<IMessageDestination>>().GetRequiredService(pipelineName);
                    return new SingleDestinationMessageDelegate(messageDestination);
                })
                .ConfigureMessageConsumer(sp =>
                {
                    var messageSource = sp.GetRequiredService<INamedServiceProvider<IMessageSource>>().GetRequiredService(pipelineName);
                    var pipelineDelegate = sp.GetRequiredService<INamedServiceProvider<IMessageDelegate>>().GetRequiredService(pipelineName);
                    var logger = sp.GetRequiredService<INamedServiceProvider<ILogger>>().GetRequiredService(pipelineName);
                    return new MessageConsumer(messageSource, pipelineDelegate, logger);
                })
                .RunConsumerAsBackgroundService();
    }

    /// <summary>
    /// Source Queue --> Consumer --> Queue1, Queue2.
    /// </summary>
    private static void QueueConsumerMultipleQueues(string pipelineName, IServiceCollection services)
    {
        string source = $"{pipelineName}-source";
        string destination1 = $"{pipelineName}-destination1";
        string destination2 = $"{pipelineName}-destination2";

        services.AddAsyncPipeline(pipelineName)
                .AddNamedSingleton<ILogger>(sp => new FakeLogger())
                .AddNamedSingleton(source, sp => new Mock<QueueClient>().Object)
                .AddNamedSingleton(destination1, sp => new Mock<QueueClient>().Object)
                .AddNamedSingleton(destination2, sp => new Mock<QueueClient>().Object)
                .ConfigureMessageSource(sp =>
                {
                    var queueClient = sp.GetRequiredService<INamedServiceProvider<QueueClient>>().GetRequiredService(source);
                    var readOptions = new AzureStorageQueueReadOptions(TimeSpan.FromSeconds(1));
                    return new AzureStorageQueueSource(queueClient, readOptions, () => new FeatureCollection());
                })
                .ConfigureMessageDestination(destination1, sp =>
                {
                    var queueClient = sp.GetRequiredService<INamedServiceProvider<QueueClient>>().GetRequiredService(destination1);
                    var writeOptions = new AzureStorageQueueWriteOptions(TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
                    return new AzureStorageQueueDestination(queueClient, writeOptions);
                })
                .ConfigureMessageDestination(destination2, sp =>
                {
                    var queueClient = sp.GetRequiredService<INamedServiceProvider<QueueClient>>().GetRequiredService(destination2);
                    var writeOptions = new AzureStorageQueueWriteOptions(TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
                    return new AzureStorageQueueDestination(queueClient, writeOptions);
                })
                .ConfigureTerminalMessageDelegate(sp =>
                {
                    var messageDestination1 = sp.GetRequiredService<INamedServiceProvider<IMessageDestination>>().GetRequiredService(destination1);
                    var messageDestination2 = sp.GetRequiredService<INamedServiceProvider<IMessageDestination>>().GetRequiredService(destination2);
                    return new MultipleDestinationMessageDelegate(new List<IMessageDestination> { messageDestination1, messageDestination2 });
                })
                .ConfigureMessageConsumer(sp =>
                {
                    var messageSource = sp.GetRequiredService<INamedServiceProvider<IMessageSource>>().GetRequiredService(pipelineName);
                    var pipelineDelegate = sp.GetRequiredService<INamedServiceProvider<IMessageDelegate>>().GetRequiredService(pipelineName);
                    var logger = sp.GetRequiredService<INamedServiceProvider<ILogger>>().GetRequiredService(pipelineName);
                    return new MessageConsumer(messageSource, pipelineDelegate, logger);
                })
                .RunConsumerAsBackgroundService();
    }
}
