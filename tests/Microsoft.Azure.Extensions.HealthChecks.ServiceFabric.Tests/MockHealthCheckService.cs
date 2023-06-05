// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric.Tests;

internal class MockHealthCheckService : HealthCheckService
{
    private readonly Task<HealthReport> _healthyReport = CreateHealthReport(HealthStatus.Healthy);
    private readonly Task<HealthReport> _unhealthyReport = CreateHealthReport(HealthStatus.Unhealthy);
    public bool IsHealthy = true;

    public override Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate, CancellationToken cancellationToken = default)
    {
        return IsHealthy ? _healthyReport : _unhealthyReport;
    }

    private static Task<HealthReport> CreateHealthReport(HealthStatus healthStatus)
    {
        HealthReportEntry entry = new HealthReportEntry(healthStatus, null, TimeSpan.Zero, null, null);
        var healthStatusRecords = new Dictionary<string, HealthReportEntry> { { "id", entry } };
        return Task.FromResult(new HealthReport(healthStatusRecords, TimeSpan.Zero));
    }
}
