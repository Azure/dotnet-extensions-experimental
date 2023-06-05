// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Borrowed from https://github.com/dotnet/aspnetcore/blob/95ed45c67/src/Testing/src/xunit/

namespace Microsoft.TestUtilities;

public interface ITestCondition
{
    bool IsMet { get; }

    string SkipReason { get; }
}
