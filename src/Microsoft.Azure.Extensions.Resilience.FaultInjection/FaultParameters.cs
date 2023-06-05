// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection;

internal sealed class FaultParameters
{
    public string? ExceptionKey { get; set; }

    public string? StatusCode { get; set; }

    public string? HttpContentKey { get; set; }

    public string? Latency { get; set; }
}
