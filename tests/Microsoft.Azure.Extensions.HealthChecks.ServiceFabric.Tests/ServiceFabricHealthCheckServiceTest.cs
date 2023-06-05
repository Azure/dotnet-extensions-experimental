// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Moq;
using Xunit;
using MSOptions = Microsoft.Extensions.Options;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric.Tests;

public class ServiceFabricHealthCheckServiceTest
{
    [Fact]
    public async Task ExecuteAsync_CheckListenerOpenAndCloseAfterHealthStatusEvents()
    {
        var listener = CreateMockListener();
        var healthCheckService = new MockHealthCheckService();

        var options = new ServiceFabricHealthCheckOptions();
        var timeProvider = new FakeTimeProvider(default);
        using var fabricHealthCheckService = new ServiceFabricHealthCheckService(healthCheckService, listener.Object, MSOptions.Options.Create(options))
        {
            TimeProvider = timeProvider
        };

        listener.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
        listener.Verify(x => x.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);

        healthCheckService.IsHealthy = true;
        await fabricHealthCheckService.StartAsync(default);
        listener.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Once);
        listener.Verify(x => x.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);

        healthCheckService.IsHealthy = true;
        await fabricHealthCheckService.StartAsync(default);
        listener.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        listener.Verify(x => x.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);

        healthCheckService.IsHealthy = false;
        await fabricHealthCheckService.StartAsync(default);
        listener.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        listener.Verify(x => x.CloseAsync(It.IsAny<CancellationToken>()), Times.Once);

        healthCheckService.IsHealthy = false;
        await fabricHealthCheckService.StartAsync(default);
        listener.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        listener.Verify(x => x.CloseAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

        healthCheckService.IsHealthy = true;
        await fabricHealthCheckService.StartAsync(default);
        listener.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        listener.Verify(x => x.CloseAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

#if NET5_0_OR_GREATER
    [Fact]
    public async Task ExecuteAsync_Does_Nothing_On_Cancellation()
    {
        var listener = CreateMockListener();
        var healthCheckService = new MockHealthCheckService();

        var options = new ServiceFabricHealthCheckOptions();
        var timeProvider = new FakeTimeProvider(default);
        using var fabricHealthCheckService = new ServiceFabricHealthCheckService(healthCheckService, listener.Object, MSOptions.Options.Create(options))
        {
            TimeProvider = timeProvider
        };

        using var source = new CancellationTokenSource();

        source.Cancel();
        await fabricHealthCheckService.StartAsync(source.Token);

        listener.Verify(x => x.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
        listener.Verify(x => x.CloseAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
#endif

    [Fact]
    public void Ctor_ThrowsWhenNull()
    {
        var listener = CreateMockListener();
        var healthCheckService = new MockHealthCheckService();
        var options = new ServiceFabricHealthCheckOptions();

        Assert.Throws<ArgumentException>(() => new ServiceFabricHealthCheckService(healthCheckService, listener.Object, MSOptions.Options.Create<ServiceFabricHealthCheckOptions>(null!)));
    }

    private static Mock<ICommunicationListener> CreateMockListener()
    {
        var listener = new Mock<ICommunicationListener>(MockBehavior.Strict);
        listener.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("Opened"));
        listener.Setup(x => x.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return listener;
    }
}
