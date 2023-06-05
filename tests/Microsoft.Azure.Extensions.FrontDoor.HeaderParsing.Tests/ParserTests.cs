// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Azure.Extensions.FrontDoor.HeaderParsing.Test;

public class ParserTests
{
    public static IEnumerable<object?[]> GetStringPropertyTestData()
    {
        yield return new object?[] { new StringValues("SingleValue"), true, "SingleValue" };
        yield return new object?[] { StringValues.Empty, false, null };
        yield return new object?[] { new StringValues(new[] { "1", "2" }), false, null };
    }

    [Theory]
    [InlineData("NotAnIp")]
    [InlineData("")]
    public void TryParseIpAddressValue_ReturnsFalseForAnInvalidHeaderValue(string invalidHeaderValue)
    {
        var sv = new StringValues(invalidHeaderValue);
        var result = IPAddressParser.Instance.TryParse(sv, out var parsedIp, out var error);

        result.Should().Be(false);
        parsedIp.Should().BeNull();
        error.Should().Be("Unable to parse IP address value.");
    }

    [Fact]
    public void TryParseIpAddressValue_ReturnsFalseForAMultipleHeaderValues()
    {
        var sv = new StringValues(new[] { "1", "2" });
        var result = IPAddressParser.Instance.TryParse(sv, out var parsedIp, out var error);

        result.Should().Be(false);
        parsedIp.Should().BeNull();
        error.Should().Be("Unable to parse IP address value.");
    }

    [Fact]
    public void TryParseIpAddressValue_ParsesAnIpAddressSuccessfully()
    {
        var sv = new StringValues("2001:0db8:85a3:0000:0000:8a2e:0370:7334");
        var result = IPAddressParser.Instance.TryParse(sv, out var parsedIp, out var error);

        result.Should().Be(true);
        parsedIp!.AddressFamily.Should().Be(AddressFamily.InterNetworkV6);
        var expectedShortenedIpV6 = "2001:db8:85a3::8a2e:370:7334";
        parsedIp!.ToString().Should().Be(expectedShortenedIpV6);
        error.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(GetStringPropertyTestData))]
    public void TryParseStringValueTest(StringValues sv, bool expectedResult, string expectedParsedValue)
    {
        var result = StringParser.Instance.TryParse(sv, out var parsedValue, out _);

        result.Should().Be(expectedResult);
        parsedValue!.Should().Be(expectedParsedValue);
    }

    [Theory]
    [InlineData("", new string[0])]
    [InlineData("endpoint1.internal.contoso.com", new[] { "endpoint1.internal.contoso.com" })]
    [InlineData("endpoint1.internal.contoso.com,endpoint2.internal.contoso.com,endpoint3.internal.contoso.com",
        new[] { "endpoint1.internal.contoso.com", "endpoint2.internal.contoso.com", "endpoint3.internal.contoso.com" })]
    public void TryParseRouteKeyApplicationEndpointValuesTest(string headerValue, IEnumerable<string> expectedParsedValue)
    {
        var sv = new StringValues(headerValue);
        var result = RouteKeyApplicationEndpointListParser.Instance.TryParse(sv, out var parsedValue, out _);

        result.Should().Be(true);
        parsedValue!.Should().Equal(expectedParsedValue);
    }

    [Fact]
    public void TryParseRouteKeyApplicationEndpointValues_ReturnsTrueForMultipleHeaderValues()
    {
        var sv = new StringValues(new[] { "1", "2" });
        var result = RouteKeyApplicationEndpointListParser.Instance.TryParse(sv, out var l, out var error);

        result.Should().Be(true);
        l.Should().NotBeNull();
        error.Should().BeNull();
    }
}

