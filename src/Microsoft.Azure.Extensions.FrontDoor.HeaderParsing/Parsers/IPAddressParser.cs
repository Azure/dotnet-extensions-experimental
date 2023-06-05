// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.HeaderParsing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.Extensions.FrontDoor.HeaderParsing;

internal sealed class IPAddressParser : HeaderParser<IPAddress>
{
    public static IPAddressParser Instance { get; } = new();

    public override bool TryParse(StringValues values, [NotNullWhen(true)] out IPAddress? result, [NotNullWhen(false)] out string? error)
    {
        if (values.Count != 1 || !IPAddress.TryParse(values[0], out result))
        {
            error = "Unable to parse IP address value.";
            result = default;
            return false;
        }

        error = default;
        return true;
    }
}
