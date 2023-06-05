// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric;

/// <summary>
/// Opens the provided Service Fabric listener if the service is healthy and closes it otherwise.
/// </summary>
internal sealed class ServiceFabricHealthCheckService : BackgroundService
{
    internal TimeProvider TimeProvider = TimeProvider.System;

    private readonly HealthCheckService _healthCheckService;
    private readonly ICommunicationListener _listener;
    private readonly ServiceFabricHealthCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFabricHealthCheckService"/> class.
    /// </summary>
    /// <param name="healthCheckService">The HealthCheckService.</param>
    /// <param name="listener">The <see cref="ICommunicationListener"/> to use.</param>
    /// <param name="options">The options.</param>
    public ServiceFabricHealthCheckService(HealthCheckService healthCheckService, ICommunicationListener listener, IOptions<ServiceFabricHealthCheckOptions> options)
    {
        _healthCheckService = healthCheckService;
        _listener = listener;
        _options = Throw.IfMemberNull(options, options.Value);
    }

    /// <summary>
    /// Executes the health checks in the <see cref="HealthCheckService"/> and opens the registered Service Fabric listener if the service is healthy and closes it otherwise.
    /// </summary>
    /// <param name="stoppingToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Task.</returns>
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var report = await _healthCheckService.CheckHealthAsync(_options.PublishingPredicate, stoppingToken).ConfigureAwait(false);
            if (report.Status == HealthStatus.Healthy)
            {
                _ = await _listener.OpenAsync(stoppingToken).ConfigureAwait(false);
            }
            else
            {
                await _listener.CloseAsync(stoppingToken).ConfigureAwait(false);
            }

            await TimeProvider.Delay(_options.Period, stoppingToken).ConfigureAwait(false);
        }
    }
}
