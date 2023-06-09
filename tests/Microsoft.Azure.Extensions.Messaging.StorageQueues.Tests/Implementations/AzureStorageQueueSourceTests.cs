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
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;
using Moq;
using Xunit;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Implementations;

/// <summary>
/// Tests for <see cref="AzureStorageQueueSource"/>.
/// </summary>
public class AzureStorageQueueSourceTests
{
    private static MessageContext CreateContext(QueueClient queueClient, QueueMessage queueMessage)
        => new AzureStorageQueueMessageContext(queueClient, queueMessage, new FeatureCollection(), ReadOnlyMemory<byte>.Empty);

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public async Task MessageRead_ShouldUseVisibilityTimeout_WhenSet(int visibilityTimeoutInMinutes)
    {
        DateTimeOffset? nextVisibleOn = visibilityTimeoutInMinutes < 0 ? null : DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromMinutes(visibilityTimeoutInMinutes));
        QueueMessage queueMessage = QueuesModelFactory.QueueMessage("msgId", "popReceipt", "message", 0, nextVisibleOn);

        var mockQueueClient = new Mock<QueueClient>();
        mockQueueClient.Setup(x => x.ReceiveMessageAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(Response.FromValue(queueMessage, Mock.Of<Response>()));

        var visibilityTimeout = TimeSpan.FromSeconds(visibilityTimeoutInMinutes);
        var features = new FeatureCollection();

        var messageSource = new AzureStorageQueueSource(mockQueueClient.Object, new AzureStorageQueueReadOptions(visibilityTimeout), () => features);
        _ = await messageSource.ReadAsync(CancellationToken.None);

        mockQueueClient.Verify(x => x.ReceiveMessageAsync(visibilityTimeout, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("msgId", "popReceipt", "message", 1)]
    public async Task MessageDelete_ShouldCallUnderlyingQueueClientDeleteMessage(string messageId, string popReceipt, string message, int dequeueCount)
    {
        QueueMessage queueMessage = QueuesModelFactory.QueueMessage(messageId, popReceipt, message, dequeueCount);
        var mockQueueClient = new Mock<QueueClient>();

        MessageContext messageContext = CreateContext(mockQueueClient.Object, queueMessage);
        messageContext.SetAzureStorageQueueMessage(queueMessage);
        messageContext.SetAzureStorageQueueClient(mockQueueClient.Object);

        var features = new FeatureCollection();
        var messageSource = new AzureStorageQueueSource(mockQueueClient.Object, default, () => features);
        await messageSource.DeleteAsync(messageContext, CancellationToken.None);

        mockQueueClient.Verify(x => x.DeleteMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("msgId", "popReceipt", "message", 1, 5)]
    public async Task MessagePostpone_ShouldCallUnderlyingQueueClientUpdateMessage(string messageId, string popReceipt, string message, int dequeueCount, int newVisibilityTimeoutInSeconds)
    {
        QueueMessage queueMessage = QueuesModelFactory.QueueMessage(messageId, popReceipt, BinaryData.FromString(message), dequeueCount);
        var mockQueueClient = new Mock<QueueClient>();
        var features = new FeatureCollection();

        var messageSource = new AzureStorageQueueSource(mockQueueClient.Object, default, () => features);
        TimeSpan newVisibilityTimeout = TimeSpan.FromSeconds(newVisibilityTimeoutInSeconds);

        MessageContext messageContext = CreateContext(mockQueueClient.Object, queueMessage);
        messageContext.SetAzureStorageQueueMessage(queueMessage);
        messageContext.SetAzureStorageQueueClient(mockQueueClient.Object);

        await messageSource.PostponeAsync(messageContext, newVisibilityTimeout, CancellationToken.None);
        mockQueueClient.Verify(x => x.UpdateMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, queueMessage.Body, newVisibilityTimeout, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("msgId-1", "popReceipt-1", "message-1", 1, 5)]
    [InlineData("msgId-2", "popReceipt-2", "message-2", 2, 10)]
    public async Task MessageUpdateVisibilityTime_ShouldCallUnderlyingQueueClientUpdateMessage(string messageId,
                                                                                               string popReceipt,
                                                                                               string message,
                                                                                               int dequeueCount,
                                                                                               int newVisibilityTimeoutInSeconds)
    {
        QueueMessage queueMessage = QueuesModelFactory.QueueMessage(messageId, popReceipt, BinaryData.FromString(message), dequeueCount);
        var mockQueueClient = new Mock<QueueClient>();
        var features = new FeatureCollection();

        var messageSource = new AzureStorageQueueSource(mockQueueClient.Object, default, () => features);
        TimeSpan newVisibilityTimeout = TimeSpan.FromSeconds(newVisibilityTimeoutInSeconds);

        MessageContext messageContext = CreateContext(mockQueueClient.Object, queueMessage);
        messageContext.SetAzureStorageQueueMessage(queueMessage);
        messageContext.SetAzureStorageQueueClient(mockQueueClient.Object);

        await messageSource.UpdateVisibilityTimeoutAsync(messageContext, newVisibilityTimeout, CancellationToken.None);
        mockQueueClient.Verify(x => x.UpdateMessageAsync(queueMessage.MessageId, queueMessage.PopReceipt, queueMessage.Body, newVisibilityTimeout, It.IsAny<CancellationToken>()), Times.Once);
    }
}
