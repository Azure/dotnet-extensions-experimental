// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Borrowed from https://github.com/dotnet/aspnetcore/blob/95ed45c67/src/Testing/src/xunit/

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.TestUtilities;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Microsoft.TestUtilities." + nameof(ConditionalFactDiscoverer), "Microsoft.TestUtilities")]
public class ConditionalFactAttribute : FactAttribute
{
}
