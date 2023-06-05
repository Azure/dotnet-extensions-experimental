// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HeaderParsing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Extensions.FrontDoor.HeaderParsing;

internal sealed class StringParser : HeaderParser<string>
{
    public static StringParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out string? result, [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1 || string.IsNullOrEmpty(values[0]))
        {
            error = "There should be exactly one, non-empty header value.";
            result = default;
            return false;
        }

        error = default;
        result = values[0]!;
        return true;
    }
}
