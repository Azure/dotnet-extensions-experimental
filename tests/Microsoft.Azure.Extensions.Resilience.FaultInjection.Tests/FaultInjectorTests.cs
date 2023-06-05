// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection.Test;

public class FaultInjectorTests
{
    [Fact]
    public void AddFault_NullAndIvalidArguments_ShouldThrow()
    {
        var faultInjector = new FaultInjector();
        Assert.Throws<ArgumentNullException>(() => faultInjector.AddFault(null!, "Exception", "TestException", 1.0));
        Assert.Throws<ArgumentNullException>(() => faultInjector.AddFault("TestOptionsGroup", null!, "TestException", 1.0));
        Assert.Throws<ArgumentNullException>(() => faultInjector.AddFault("TestOptionsGroup", "Exception", null!, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => faultInjector.AddFault("TestOptionsGroup", "Exception", "TestException", -0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => faultInjector.AddFault("TestOptionsGroup", "Exception", "TestException", 1.5));
    }

    [Fact]
    public void AddFault_RemoveFault_ShouldRemoveAssociatedOptionsFromOptionsProvider_Exception()
    {
        var faultOptionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultInjector = new FaultInjector();

        var faultId = faultInjector.AddFault("TestOptionsGroup", "Exception", "TestException", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup", out var results);
        Assert.NotNull(results);
        Assert.NotNull(results!.ExceptionPolicyOptions);
        Assert.True(results!.ExceptionPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup", out results);
        Assert.Null(results);

        // test case sensitive
        faultId = faultInjector.AddFault("TestOptionsGroup", "exception", "TestException", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup", out results);
        Assert.NotNull(results);
        Assert.NotNull(results!.ExceptionPolicyOptions);
        Assert.True(results!.ExceptionPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup", out results);
        Assert.Null(results);
    }

    [Fact(Skip = "Flaky test, see https://github.com/dotnet/r9/issues/95")]
    public void AddFault_RemoveFault_JToken_ShouldRemoveAssociatedOptionsFromOptionsProvider_Exception()
    {
        var faultOptionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultInjector = new FaultInjector();

        var faultId = faultInjector.AddFault("TestOptionsGroup2", "Exception", "{\"ExceptionKey\": \"TestException\"}", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup2", out var results);
        Assert.NotNull(results);
        Assert.NotNull(results!.ExceptionPolicyOptions);
        Assert.True(results!.ExceptionPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup2", out results);
        Assert.Null(results);
    }

    [Fact]
    public void AddFault_RemoveFault_ShouldRemoveAssociatedOptionsFromOptionsProvider_HttpStatusCode()
    {
        var faultOptionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultInjector = new FaultInjector();
        var faultId = faultInjector.AddFault("TestOptionsGroup3", "HttpStatusCode", "503", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup3", out var results);
        Assert.NotNull(results);
        Assert.NotNull(results!.HttpResponseInjectionPolicyOptions);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, results!.HttpResponseInjectionPolicyOptions!.StatusCode);
        Assert.True(results!.HttpResponseInjectionPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup3", out results);
        Assert.Null(results);

        // test case sensitive
        faultId = faultInjector.AddFault("TestOptionsGroup3", "httpStatusCode", "503", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup3", out results);
        Assert.NotNull(results);
        Assert.NotNull(results!.HttpResponseInjectionPolicyOptions);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, results!.HttpResponseInjectionPolicyOptions!.StatusCode);
        Assert.True(results!.HttpResponseInjectionPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup3", out results);
        Assert.Null(results);
    }

    [Fact(Skip = "Flaky test, see https://github.com/dotnet/r9/issues/95")]
    public void AddFault_RemoveFault_JToken_ShouldRemoveAssociatedOptionsFromOptionsProvider_HttpStatusCode()
    {
        var faultOptionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultInjector = new FaultInjector();
        var faultId = faultInjector.AddFault("TestOptionsGroup4", "HttpStatusCode", "{\"StatusCode\": \"503\"}", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup4", out var results);
        Assert.NotNull(results);
        Assert.NotNull(results!.HttpResponseInjectionPolicyOptions);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, results!.HttpResponseInjectionPolicyOptions!.StatusCode);
        Assert.True(results!.HttpResponseInjectionPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup4", out results);
        Assert.Null(results);

        // Http Content case
        var faultValueObj = new
        {
            StatusCode = "200",
            HttpContentKey = "TestKey",
        };
        var testFaultValue = JToken.FromObject(faultValueObj);
        faultId = faultInjector.AddFault("TestOptionsGroup4", "HttpStatusCode", testFaultValue.ToString(), 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup4", out results);
        Assert.NotNull(results);
        Assert.NotNull(results!.HttpResponseInjectionPolicyOptions);
        Assert.Equal(HttpStatusCode.OK, results!.HttpResponseInjectionPolicyOptions!.StatusCode);
        Assert.True(results!.HttpResponseInjectionPolicyOptions!.Enabled);
        Assert.Equal("TestKey", results!.HttpResponseInjectionPolicyOptions!.HttpContentKey);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup4", out results);
        Assert.Null(results);
    }

    [Fact]
    public void AddFault_RemoveFault_ShouldRemoveAssociatedOptionsFromOptionsProvider_Latency()
    {
        var faultOptionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultInjector = new FaultInjector();
        var faultId = faultInjector.AddFault("TestOptionsGroup5", "Latency", "00:00:40", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup5", out var results);
        Assert.NotNull(results);
        Assert.NotNull(results!.LatencyPolicyOptions);
        Assert.True(results!.LatencyPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup5", out results);
        Assert.Null(results);

        // test case sensitive
        faultId = faultInjector.AddFault("TestOptionsGroup5", "latency", "00:00:40", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup5", out results);
        Assert.NotNull(results);
        Assert.NotNull(results!.LatencyPolicyOptions);
        Assert.True(results!.LatencyPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup5", out results);
        Assert.Null(results);
    }

    [Fact(Skip = "Flaky test, see https://github.com/dotnet/r9/issues/95")]
    public void AddFault_RemoveFault_JToken_ShouldRemoveAssociatedOptionsFromOptionsProvider_Latency()
    {
        var faultOptionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultInjector = new FaultInjector();
        var faultId = faultInjector.AddFault("TestOptionsGroup6", "Latency", "{\"Latency\": \"00:00:40\"}", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup6", out var results);
        Assert.NotNull(results);
        Assert.NotNull(results!.LatencyPolicyOptions);
        Assert.True(results!.LatencyPolicyOptions!.Enabled);

        faultInjector.RemoveFault(faultId ?? Guid.Empty);
        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup6", out results);
        Assert.Null(results);
    }

    [Fact]
    public void AddFault_InvalidFaultType_ShouldReturnNull()
    {
        var faultOptionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        var faultInjector = new FaultInjector();
        var faultId = faultInjector.AddFault("TestOptionsGrou7", "ABC", "TestException", 1.0);

        faultOptionsProvider.TryGetChaosPolicyOptionsGroup("TestOptionsGroup7", out var results);
        Assert.Null(results);
    }

    [Fact]
    public void RemoveFault_faultIdNotFound_ShouldReturnFalse()
    {
        var faultInjector = new FaultInjector();
        var result = faultInjector.RemoveFault(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public void NonJsonStringParameter_ShouldLogWarning()
    {
        var logger = new FakeLogger();
        var faultInjector = new FaultInjector(logger);

        var faultId = faultInjector.AddFault("TestOptionsGroup", "Exception", "TestException", 1.0);
        faultInjector.RemoveFault(faultId ?? Guid.Empty);

        Assert.Equal("Failed to parse fault value to a json object. The fault value will be used as is.", logger.LatestRecord.Message);
    }
}
