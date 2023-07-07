// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Extensions.HealthChecks.ServiceFabric;

[OptionsValidator]
internal sealed partial class ServiceFabricHealthCheckOptionsValidator : IValidateOptions<ServiceFabricHealthCheckOptions>
{
}
