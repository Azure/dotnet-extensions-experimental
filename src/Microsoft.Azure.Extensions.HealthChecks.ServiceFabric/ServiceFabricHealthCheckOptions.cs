// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric;

/// <summary>
/// Options for <see cref="ServiceFabricHealthCheckService"/>.
/// </summary>
public class ServiceFabricHealthCheckOptions
{
    /// <summary>
    /// Gets or sets a predicate that can be used to include health checks based on user-defined criteria.
    /// </summary>
    /// <remarks>
    /// Default set to a predicate that accepts all health checks.
    /// </remarks>
    public Func<HealthCheckRegistration, bool> PublishingPredicate { get; set; } = (_) => true;

    /// <summary>
    /// Gets or sets the period of the publisher execution.
    /// </summary>
    /// <remarks>
    /// Default set to 30 seconds.
    /// </remarks>
    [TimeSpan("00:00:05", "00:05:00")]
    public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(30);
}
