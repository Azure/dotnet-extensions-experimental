// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection;

/// <summary>
/// This class is used by Azure Chaos Studio IPFI agent to add and remove <see cref="FaultInjectionOptions"/> to <see cref="IFaultInjectionOptionsProvider"/>.
/// </summary>
/// <remarks>
/// This class should implement interface Microsoft.Azure.Chaos.Agent.IPFI.FaultInjector.Contracts.Factory.IFaultInjector eventually.
/// </remarks>
public class FaultInjector
{
    private readonly ACSFaultInjectionOptionsProvider _optionsProvider;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FaultInjector"/> class.
    /// </summary>
    public FaultInjector(ILogger? logger = null)
    {
        _optionsProvider = ACSFaultInjectionOptionsProvider.Instance;
        _logger = logger;
    }

    /// <summary>
    /// Translate provided parameters to <see cref="FaultInjectionOptions"/> then add it to <see cref="IFaultInjectionOptionsProvider"/>.
    /// </summary>
    /// <param name="chaosPolicyOptionsGroupName">The chaos policy options group name.</param>
    /// <param name="faultType">The fault type.</param>
    /// <param name="faultValue">The fault value.</param>
    /// <param name="faultInjectionRate">The fault injection rate.</param>
    /// <returns>
    /// The fault Id that is associated with the added <see cref="FaultInjectionOptions"/> if successful;
    /// return null if the fault is not added successfully.
    /// </returns>
    public Guid? AddFault(string chaosPolicyOptionsGroupName, string faultType, string faultValue, double faultInjectionRate)
    {
        _ = Throw.IfNull(chaosPolicyOptionsGroupName);
        _ = Throw.IfNull(faultType);
        _ = Throw.IfNull(faultValue);
        _ = Throw.IfOutOfRange(faultInjectionRate, 0.0, 1.0);

        var faultId = Guid.NewGuid();
        var faultInjectionOptions = new FaultInjectionOptions();
        var optionsGroup = new ChaosPolicyOptionsGroup();

        if (!Enum.TryParse(faultType, true, out IPFIFaultType faultTypeEnum))
        {
            // Return null to indicate that fault is not added successfully
            return null;
        }

        TryParseFaultValue(faultValue, out var faultParamJson);
        switch (faultTypeEnum)
        {
            case IPFIFaultType.Exception:
            {
                optionsGroup.ExceptionPolicyOptions = new ExceptionPolicyOptions
                {
                    Enabled = true,
                    FaultInjectionRate = faultInjectionRate,
                    ExceptionKey = faultParamJson?.ExceptionKey ?? faultValue,
                };

                break;
            }

            case IPFIFaultType.HttpStatusCode:
            {
                var statusCodeString = faultParamJson?.StatusCode ?? faultValue;
                var httpContentKey = faultParamJson?.HttpContentKey;

                if (Enum.TryParse(statusCodeString, out HttpStatusCode statusCode))
                {
                    optionsGroup.HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
                    {
                        Enabled = true,
                        FaultInjectionRate = faultInjectionRate,
                        StatusCode = statusCode,
                        HttpContentKey = httpContentKey
                    };
                }

                break;
            }

            case IPFIFaultType.Latency:
            {
                var latency = faultParamJson?.Latency ?? faultValue;

                if (TimeSpan.TryParse(latency, CultureInfo.InvariantCulture, out var timeSpan))
                {
                    optionsGroup.LatencyPolicyOptions = new LatencyPolicyOptions
                    {
                        Enabled = true,
                        FaultInjectionRate = faultInjectionRate,
                        Latency = timeSpan,
                    };
                }

                break;
            }
        }

        faultInjectionOptions.ChaosPolicyOptionsGroups.Add(chaosPolicyOptionsGroupName, optionsGroup);
        _ = _optionsProvider.SetFaultInjectionOptions(faultId, faultInjectionOptions);

        return faultId;
    }

    /// <summary>
    /// Removes the <see cref="FaultInjectionOptions"/> associated with the fault Id.
    /// </summary>
    /// <param name="id">The fault id.</param>
    /// <returns>True if the <see cref="FaultInjectionOptions"/> is removed successfully; false otherwise.</returns>
    public bool RemoveFault(Guid id)
    {
        return _optionsProvider.RemoveFaultInjectionOptions(id);
    }

    private void TryParseFaultValue(string faultValue, [NotNullWhen(true)] out FaultParameters? jsonObj)
    {
        try
        {
#if NET6_0_OR_GREATER
            jsonObj = JsonSerializer.Deserialize(faultValue, FaultParametersJsonContext.Default.FaultParameters);
#else
            jsonObj = JsonSerializer.Deserialize<FaultParameters>(faultValue);
#endif
        }
        catch (JsonException)
        {
#pragma warning disable R9A000 // FaultInjector class is not instantiated through the DI pattern
            _logger?.LogWarning(
                $"Failed to parse fault value to a json object. The fault value will be used as is.");
#pragma warning restore R9A000

            jsonObj = null;
        }
    }
}
