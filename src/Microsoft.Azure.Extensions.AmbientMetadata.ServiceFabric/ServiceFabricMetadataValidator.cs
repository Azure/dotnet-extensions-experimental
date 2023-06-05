// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;

namespace Microsoft.Azure.Extensions.AmbientMetadata.ServiceFabric;

[OptionsValidator]
internal sealed partial class ServiceFabricMetadataValidator : IValidateOptions<ServiceFabricMetadata>
{
}
