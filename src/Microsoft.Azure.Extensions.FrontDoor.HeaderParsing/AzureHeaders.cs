// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.HeaderParsing;

namespace Microsoft.Azure.Extensions.FrontDoor.HeaderParsing;

/// <summary>
/// Azure header setups.
/// </summary>
public static class AzureHeaders
{
    /// <summary>
    /// Gets AfdClientIp header setup.
    /// </summary>
    public static HeaderSetup<IPAddress> ClientIp => new("X-FD-ClientIP", IPAddressParser.Instance);

    /// <summary>
    /// Gets AfdSocketIp header setup.
    /// </summary>
    public static HeaderSetup<IPAddress> SocketIp => new("X-FD-SocketIP", IPAddressParser.Instance);

    /// <summary>
    /// Gets AfdEdgeEnvironment header setup.
    /// </summary>
    public static HeaderSetup<string> EdgeEnvironment => new("X-FD-EdgeEnvironment", StringParser.Instance, cacheable: true);

    /// <summary>
    /// Gets AfdRouteKey header setup.
    /// </summary>
    public static HeaderSetup<string> RouteKey => new("X-FD-RouteKey", StringParser.Instance);

    /// <summary>
    /// Gets AfdRouteKeyApplicationEndpointList header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<string>> RouteKeyApplicationEndpointList => new("X-FD-RouteKeyApplicationEndpointList", RouteKeyApplicationEndpointListParser.Instance);
}
