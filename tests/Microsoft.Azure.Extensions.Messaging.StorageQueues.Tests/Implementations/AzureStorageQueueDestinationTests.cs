// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data;
using Moq;
using Xunit;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Implementations;

/// <summary>
/// Tests for <see cref="AzureStorageQueueDestination"/>.
/// </summary>
public class AzureStorageQueueDestinationTests
{
    private static MessageContext CreateContext() => new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);

    [Theory]
    [InlineData("abc", 10, 20)]
    public async Task MessageWrite_ShouldUseMethodLevelTimeSpans_WhenItIsNotNull(string message, int visibilityTimeoutInSeconds, int timeToLiveInSeconds)
    {
        SendReceipt sendReceipt = QueuesModelFactory.SendReceipt("msgId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "popReceipt", DateTimeOffset.UtcNow);
        var mockQueueClient = new Mock<QueueClient>();
        mockQueueClient.Setup(x => x.SendMessageAsync(It.IsAny<BinaryData>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(sendReceipt, Mock.Of<Response>()));

        var messageDestination = new AzureStorageQueueDestination(mockQueueClient.Object, new AzureStorageQueueWriteOptions(TimeSpan.MaxValue, TimeSpan.MaxValue));

        var visibilityTimeout = TimeSpan.FromSeconds(visibilityTimeoutInSeconds);
        var timeToLive = TimeSpan.FromSeconds(timeToLiveInSeconds);

        MessageContext context = CreateContext();
        context.SetDestinationPayload(Encoding.UTF8.GetBytes(message));
        context.SetAzureStorageQueueWriteOptions(new AzureStorageQueueWriteOptions(visibilityTimeout, timeToLive));

        await messageDestination.WriteAsync(context);
        mockQueueClient.Verify(x => x.SendMessageAsync(It.IsAny<BinaryData>(), visibilityTimeout, timeToLive, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("abc", 10, 20, false)]
    [InlineData("abc", 10, 20, true)]
    public async Task MessageWrite_ShouldUseClassLevelTimeSpans_WhenMethodLevelIsNull(string message, int visibilityTimeoutInSeconds, int timeToLiveInSeconds, bool setWriteOptionsAtMethodLevel)
    {
        SendReceipt sendReceipt = QueuesModelFactory.SendReceipt("msgId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "popReceipt", DateTimeOffset.UtcNow);
        var mockQueueClient = new Mock<QueueClient>();
        mockQueueClient.Setup(x => x.SendMessageAsync(It.IsAny<BinaryData>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(sendReceipt, Mock.Of<Response>()));

        var visibilityTimeout = TimeSpan.FromSeconds(visibilityTimeoutInSeconds);
        var timeToLive = TimeSpan.FromSeconds(timeToLiveInSeconds);
        var messageDestination = new AzureStorageQueueDestination(mockQueueClient.Object, new AzureStorageQueueWriteOptions(visibilityTimeout, timeToLive));

        MessageContext context = CreateContext();
        context.SetDestinationPayload(Encoding.UTF8.GetBytes(message));

        if (setWriteOptionsAtMethodLevel)
        {
            context.SetAzureStorageQueueWriteOptions(new AzureStorageQueueWriteOptions(null, null));
        }

        await messageDestination.WriteAsync(context);
        mockQueueClient.Verify(x => x.SendMessageAsync(It.IsAny<BinaryData>(), visibilityTimeout, timeToLive, It.IsAny<CancellationToken>()), Times.Once);
    }
}
