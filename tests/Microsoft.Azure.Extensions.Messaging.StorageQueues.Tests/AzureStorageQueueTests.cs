// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Consumers;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Delegates;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Test;

/// <summary>
/// Tests for Queue initialization and processing using <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class AzureStorageQueueTests
{
    [Theory]
    [InlineData("pipeline-1", "msgId-1", "popReceipt-1", "msg-1", false)]
    [InlineData("pipeline-2", "msgId-2", "popReceipt-2", "msg-2", true)]
    public async Task AddNamedMessageProcessingPipelineBuilder_ShouldWorkCorrectly(string pipelineName, string messageId, string popReceipt, string message, bool writerThrowsException)
    {
        var mocks = new TestMocks(QueuesModelFactory.QueueMessage(messageId, popReceipt, message, 0), writerThrowsException);

        IHostBuilder hostBuilder = FakeHost.CreateBuilder(TestMocks.GetFakeHostOptions());
        hostBuilder.ConfigureServices(services =>
        {
            // Create a message consumer pipeline.
            services.AddAsyncPipeline(pipelineName)
                    .ConfigureMessageSource<IMessageSource>(_ => mocks.MessageSource)
                    .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockMessageDelegate1.Object))
                    .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDelegate2.Object, mocks.MessageWriter).InvokeAsync)
                    .ConfigureMessageConsumer(sp => new SingleMessageConsumer(sp.GetMessageSource(pipelineName),
                                                                              sp.GetMessageMiddlewares(pipelineName),
                                                                              sp.GetMessageDelegate(pipelineName),
                                                                              sp.GetRequiredService<ILogger>()))
                    .RunConsumerAsBackgroundService();
        });

        // Build the host.
        using IHost host = hostBuilder.Build();

        // Start and stop the host.
        await host.StartAsync();
        await host.StopAsync();

        // Verify Mocks
        mocks.VerifyMocksCount(1);
    }

    private class TestMocks
    {
        public static FakeHostOptions GetFakeHostOptions() => new()
        {
            StartUpTimeout = TimeSpan.FromMinutes(4),
            ShutDownTimeout = TimeSpan.FromMinutes(4),
            TimeToLive = TimeSpan.FromMinutes(10),
            FakeLogging = true,
        };

        public readonly Mock<MessageDelegate> MockMessageDelegate1 = new();
        public readonly Mock<MessageDelegate> MockMessageDelegate2 = new();

        public readonly AzureStorageQueueSource MessageSource;
        public readonly AzureStorageQueueDestination MessageWriter;
        private readonly Mock<QueueClient> _mockQueueClient = new();

        public TestMocks(QueueMessage message, bool writerThrowsException)
        {
            var sendReceipt = QueuesModelFactory.SendReceipt(message.MessageId,
                                                             message.InsertedOn.GetValueOrDefault(),
                                                             message.ExpiresOn.GetValueOrDefault(),
                                                             message.PopReceipt,
                                                             message.NextVisibleOn.GetValueOrDefault());
            _mockQueueClient.Setup(x => x.ReceiveMessageAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(message, Mock.Of<Response>()));

            MessageSource = new AzureStorageQueueSource(_mockQueueClient.Object, new AzureStorageQueueReadOptions(TimeSpan.Zero), () => new FeatureCollection());

            if (writerThrowsException)
            {
                _mockQueueClient.Setup(x => x.SendMessageAsync(It.IsAny<BinaryData>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                                .ThrowsAsync(new InvalidOperationException("Error while writing message to the queue."));
            }
            else
            {
                _mockQueueClient.Setup(x => x.SendMessageAsync(It.IsAny<BinaryData>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                                .ReturnsAsync(Response.FromValue(sendReceipt, Mock.Of<Response>()));
            }

            MessageWriter = new AzureStorageQueueDestination(_mockQueueClient.Object, new AzureStorageQueueWriteOptions(TimeSpan.Zero, TimeSpan.Zero));
        }

        public void VerifyMocksCount(int count)
        {
            _mockQueueClient.Verify(x => x.ReceiveMessageAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
            MockMessageDelegate1.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Exactly(count));
            MockMessageDelegate2.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Exactly(count));
            _mockQueueClient.Verify(x => x.SendMessageAsync(It.IsAny<BinaryData>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
        }
    }
}
