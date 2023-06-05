// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Extensions.Resilience.FaultInjection;

/// <summary>
/// Factory class for <see cref="FaultInjector"/> creation to be used by Azure Chaos Studio IPFI agent.
/// </summary>
/// <remarks>
/// This class should implement interface Microsoft.Azure.Chaos.Agent.IPFI.FaultInjector.Contracts.Factory.IFaultInjectorFactory eventually.
/// </remarks>
public class FaultInjectorFactory
{
    /// <summary>
    /// Creates an instance of <see cref="FaultInjector"/>.
    /// </summary>
    /// <returns>An instance of <see cref="FaultInjector"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Need to implement interface Microsoft.Azure.Chaos.Agent.IPFI.FaultInjector.Contracts.Factory.IFaultInjectorFactory eventually.")]
    public FaultInjector CreateFaultInjector()
    {
        return new FaultInjector();
    }
}
