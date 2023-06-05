// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Internal;
using Moq;
using Xunit;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Extensions;

/// <summary>
/// Tests for <see cref="AzureStorageQueueMessageSourceContextExtensions"/>.
/// </summary>
public class AzureStorageQueueMessageSourceContextExtensionsTests
{
    private static MessageContext CreateContext()
    {
        var features = new FeatureCollection();
        var context = new MessageContext(features);

        return context;
    }

    private static MessageContext CreateContext(IFeatureCollection sourceFeatures)
    {
        var features = new FeatureCollection();
        var context = new MessageContext(features);
        context.SetMessageSourceFeatures(sourceFeatures);

        return context;
    }

    [Fact]
    public void TryGetMessageProcessingState_ShouldReturnFalse_WhenNotSet()
    {
        MessageContext context = CreateContext();
        Assert.False(context.TryGetAzureStorageQueueMessageProcessingState(out AzureStorageQueueMessageProcessingState messageProcessingState));
    }

    [Fact]
    public void TryGetMessageProcessingState_ShouldReturnTrueWithTheExpectedState_WhenSet()
    {
        foreach (AzureStorageQueueMessageProcessingState setProcessingState in (AzureStorageQueueMessageProcessingState[])Enum.GetValues(typeof(AzureStorageQueueMessageProcessingState)))
        {
            MessageContext context = CreateContext();
            context.SetAzureStorageQueueMessageProcessingState(setProcessingState);

            Assert.True(context.TryGetAzureStorageQueueMessageProcessingState(out AzureStorageQueueMessageProcessingState retrievedProcessingState));
            Assert.Equal(setProcessingState, retrievedProcessingState);
        }
    }

    [Fact]
    public void TryGetQueueClient_ShouldReturnFalse_WheNotSet()
    {
        MessageContext context = CreateContext(new FeatureCollection());
        Assert.False(context.TryGetAzureStorageQueueClient(out QueueClient? queueClient));
        Assert.Null(queueClient);
    }

    [Fact]
    public void TryGetQueueClient_ShouldReturnTrueWithTheExpectedQueueClient_WhenSet()
    {
        MessageContext context = CreateContext();
        var setQueueClient = new Mock<QueueClient>().Object;
        context.SetAzureStorageQueueClient(setQueueClient);

        Assert.True(context.TryGetAzureStorageQueueClient(out QueueClient? retrievedQueueClient));
        Assert.NotNull(retrievedQueueClient);
        Assert.Equal(setQueueClient, retrievedQueueClient);
    }

    [Fact]
    public void TryGetQueueClient_ShouldReturnFalseWithNullData_WhenFeatureThrowsException()
    {
        var exception = new InvalidOperationException();
        var mockSourceFeatures = new Mock<IFeatureCollection>();
        mockSourceFeatures.Setup(x => x.Get<QueueClient>()).Throws(exception);

        MessageContext context = CreateContext(mockSourceFeatures.Object);
        Assert.False(context.TryGetAzureStorageQueueClient(out QueueClient? queueClient));
        Assert.Null(queueClient);

        mockSourceFeatures.Verify(x => x.Get<QueueClient>(), Times.Once);
    }

    [Fact]
    public void TryGetQueueMessage_ShouldReturnFalseWithNullData_WhenNotSet()
    {
        MessageContext context = CreateContext(new FeatureCollection());
        Assert.False(context.TryGetAzureStorageQueueMessage(out QueueMessage? queueMessage));
        Assert.Null(queueMessage);
    }

    [Fact]
    public void TryGetQueueMessage_ShouldReturnFalseWithNullData_WhenFeatureThrowsException()
    {
        var exception = new InvalidOperationException();
        var mockSourceFeatures = new Mock<IFeatureCollection>();
        mockSourceFeatures.Setup(x => x.Get<QueueMessage>()).Throws(exception);

        MessageContext context = CreateContext(mockSourceFeatures.Object);
        Assert.False(context.TryGetAzureStorageQueueMessage(out QueueMessage? queueMessage));
        Assert.Null(queueMessage);

        mockSourceFeatures.Verify(x => x.Get<QueueMessage>(), Times.Once);
    }

    [Fact]
    public void TryGetQueueMessage_ShouldReturnTrueWithData_WhenSet()
    {
        MessageContext context = CreateContext();
        var setQueueMessage = QueuesModelFactory.QueueMessage("mockMessageId", "mockPopReceipt", "mockMessageText", 0);
        context.SetAzureStorageQueueMessage(setQueueMessage);

        Assert.True(context.TryGetAzureStorageQueueMessage(out QueueMessage? retrievedQueueMessage));
        Assert.Equal(setQueueMessage, retrievedQueueMessage);
    }

    [Fact]
    public void TryGetQueueSource_ShouldReturnFalseWithNullData_WhenNotSet()
    {
        MessageContext context = CreateContext(new FeatureCollection());
        Assert.False(context.TryGetAzureStorageQueueSource(out IAzureStorageQueueSource? queueSource));
        Assert.Null(queueSource);
    }

    [Fact]
    public void TryGetQueueSource_ShouldReturnFalseWithNullData_WhenFeatureThrowsException()
    {
        var exception = new InvalidOperationException();
        var mockSourceFeatures = new Mock<IFeatureCollection>();
        mockSourceFeatures.Setup(x => x.Get<IAzureStorageQueueSource>()).Throws(exception);

        MessageContext context = CreateContext(mockSourceFeatures.Object);
        Assert.False(context.TryGetAzureStorageQueueSource(out IAzureStorageQueueSource? queueSource));
        Assert.Null(queueSource);

        mockSourceFeatures.Verify(x => x.Get<IAzureStorageQueueSource>(), Times.Once);
    }

    [Fact]
    public void TryGetQueueSource_ShouldReturnTrueWithData_WhenSet()
    {
        MessageContext context = CreateContext();
        var mockQueueSource = new Mock<IAzureStorageQueueSource>();
        context.SetAzureStorageQueueSource(mockQueueSource.Object);

        Assert.True(context.TryGetAzureStorageQueueSource(out IAzureStorageQueueSource? retrievedQueueMessage));
        Assert.Equal(mockQueueSource.Object, retrievedQueueMessage);
    }
}
