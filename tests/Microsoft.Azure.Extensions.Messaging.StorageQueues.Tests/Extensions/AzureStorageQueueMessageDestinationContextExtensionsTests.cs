// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data;
using Xunit;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Extensions;

/// <summary>
/// Tests for <see cref="AzureStorageQueueMessageDestinationContextExtensions"/>.
/// </summary>
public class AzureStorageQueueMessageDestinationContextExtensionsTests
{
    private static MessageContext CreateContext() => new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);

    [Fact]
    public void TryGetAzureStorageQueueWriteOptions_ShouldReturnFalse_WhenNotSet()
    {
        MessageContext context = CreateContext();
        Assert.False(context.TryGetAzureStorageQueueWriteOptions(out _));
    }

    [Theory]
    [InlineData(10, 20)]
    public void TryGetAzureStorageQueueWriteOptions_ShouldReturnTrue_WhenSet(int visibilityTimeoutInSeconds, int ttlInSeconds)
    {
        var visibilityTimeout = TimeSpan.FromSeconds(visibilityTimeoutInSeconds);
        var ttl = TimeSpan.FromSeconds(ttlInSeconds);

        MessageContext context = CreateContext();
        context.SetAzureStorageQueueWriteOptions(new AzureStorageQueueWriteOptions(visibilityTimeout, ttl));

        Assert.True(context.TryGetAzureStorageQueueWriteOptions(out AzureStorageQueueWriteOptions? azureStorageQueueWriteOptions));
        Assert.True(azureStorageQueueWriteOptions.HasValue);
        Assert.Equal(visibilityTimeout, azureStorageQueueWriteOptions.Value.VisibilityTimeout);
        Assert.Equal(ttl, azureStorageQueueWriteOptions.Value.TimeToLive);
    }
}
