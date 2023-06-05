// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HeaderParsing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Extensions.FrontDoor.HeaderParsing;

internal sealed class RouteKeyApplicationEndpointListParser : HeaderParser<IReadOnlyList<string>>
{
    public static RouteKeyApplicationEndpointListParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out IReadOnlyList<string>? result, [NotNullWhen(false)] out string? error)
    {
        var list = new List<string>();
        foreach (var value in values)
        {
#pragma warning disable R9A043 // Use 'Microsoft.R9.Extensions.Text.StringSplitExtensions.TrySplit' for improved performance
            list.AddRange(value!.Split(',', StringSplitOptions.RemoveEmptyEntries));
#pragma warning restore R9A043 // Use 'Microsoft.R9.Extensions.Text.StringSplitExtensions.TrySplit' for improved performance
        }

        result = list;
        error = null;
        return true;
    }
}
