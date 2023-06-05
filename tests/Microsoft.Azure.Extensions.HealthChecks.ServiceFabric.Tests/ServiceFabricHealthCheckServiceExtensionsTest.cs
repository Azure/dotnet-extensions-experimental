// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting.Testing;
#else
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Internal;
#endif
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Moq;
using Xunit;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric.Tests;

public class ServiceFabricHealthCheckServiceExtensionsTest
{
    [Fact]
    public void AddServiceFabricHealthCheckPublisherTest_WithoutAction()
    {
        var listener = CreateMockListener();

        using var host = CreateWebHost(listener.Object);

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is ServiceFabricHealthCheckService);

        Assert.Single(hostedServices);
    }

    [Fact]
    public void AddServiceFabricHealthCheckPublisherTest_WithAction()
    {
        var listener = CreateMockListener();

        using var host = CreateWebHostWithAction(listener.Object, o =>
        {
            o.PublishingPredicate = _ => false;
            o.Period = TimeSpan.FromSeconds(15);
        });

        var hostedServices = host.Services.GetServices<IHostedService>().Where(x => x is ServiceFabricHealthCheckService);

        Assert.Single(hostedServices);
    }

    private static Mock<ICommunicationListener> CreateMockListener()
    {
        var listener = new Mock<ICommunicationListener>(MockBehavior.Strict);
        listener.Setup(x => x.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("Opened"));
        listener.Setup(x => x.CloseAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return listener;
    }

#if NETCOREAPP3_1_OR_GREATER
    private static IHost CreateWebHost(ICommunicationListener listener)
    {
        return FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddServiceFabricHealthCheckPublisher(listener)))
            .Build();
    }

    private static IHost CreateWebHostWithAction(ICommunicationListener listener, Action<ServiceFabricHealthCheckOptions> options)
    {
        return FakeHost.CreateBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddServiceFabricHealthCheckPublisher(listener, options)))
            .Build();
    }
#else
    private static IWebHost CreateWebHost(ICommunicationListener listener)
    {
        return new WebHostBuilder()
            .ConfigureServices(services => services
                .AddRouting()
                .AddServiceFabricHealthCheckPublisher(listener))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(routes => { })
                .UseMvc())
            .Build();
    }

    private static IWebHost CreateWebHostWithAction(ICommunicationListener listener, Action<ServiceFabricHealthCheckOptions> options)
    {
        return new WebHostBuilder()
            .ConfigureServices(services => services
                .AddRouting()
                .AddServiceFabricHealthCheckPublisher(listener, options))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(routes => { })
                .UseMvc())
            .Build();
    }
#endif
}
