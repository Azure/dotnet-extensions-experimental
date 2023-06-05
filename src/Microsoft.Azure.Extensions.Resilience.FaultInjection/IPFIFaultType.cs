// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection;

internal enum IPFIFaultType
{
    Exception,
    HttpStatusCode,
    Latency,
}
