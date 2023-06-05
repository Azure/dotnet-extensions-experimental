// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET6_0_OR_GREATER
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection;

[JsonSerializable(typeof(FaultParameters))]
internal sealed partial class FaultParametersJsonContext : JsonSerializerContext
{
}

#endif
