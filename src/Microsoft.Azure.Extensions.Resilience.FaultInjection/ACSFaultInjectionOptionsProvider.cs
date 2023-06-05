// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Resilience.FaultInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection;

/// <summary>
/// Special implementation of <see cref="IFaultInjectionOptionsProvider"/> to be used by <see cref="FaultInjector"/> to inject/remove fault options.
/// This is expected to be called by multiple threads.
/// </summary>
internal sealed class ACSFaultInjectionOptionsProvider : IFaultInjectionOptionsProvider
{
    private static readonly Lazy<ACSFaultInjectionOptionsProvider> _instance = new(() => new ACSFaultInjectionOptionsProvider());
    private readonly ConcurrentDictionary<Guid, FaultInjectionOptions> _faultInjectionOptionsDictionary = new();

    private ACSFaultInjectionOptionsProvider()
    {
    }

    public static ACSFaultInjectionOptionsProvider Instance => _instance.Value;

    public bool TryGetChaosPolicyOptionsGroup(string optionsGroupName, [NotNullWhen(true)] out ChaosPolicyOptionsGroup? optionsGroup)
    {
        _ = Throw.IfNull(optionsGroupName);

        optionsGroup = null;
        foreach (var entry in _faultInjectionOptionsDictionary)
        {
            if (entry.Value.ChaosPolicyOptionsGroups.TryGetValue(optionsGroupName, out optionsGroup))
            {
                // Return first one found
                return true;
            }
        }

        return false;
    }

    public bool SetFaultInjectionOptions(Guid faultId, FaultInjectionOptions options)
    {
        return _faultInjectionOptionsDictionary.TryAdd(faultId, options);
    }

    public bool RemoveFaultInjectionOptions(Guid faultId)
    {
        return _faultInjectionOptionsDictionary.TryRemove(faultId, out _);
    }
}
