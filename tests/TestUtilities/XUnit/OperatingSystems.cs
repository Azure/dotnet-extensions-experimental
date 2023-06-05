// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Borrowed from https://github.com/dotnet/aspnetcore/blob/95ed45c67/src/Testing/src/xunit/

using System;

namespace Microsoft.TestUtilities;

[Flags]
public enum OperatingSystems
{
    Linux = 1,
    MacOSX = 2,
    Windows = 4,
}
