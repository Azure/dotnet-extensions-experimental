// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench.TestSamples;

internal sealed class PlainLoggingImplementation
{
    private ILogger Logger { get; }
    private int _counter;

    public PlainLoggingImplementation(ILogger logger)
    {
        Logger = logger;
    }

    public async Task<int> GetEvenAsync()
    {
        try
        {
            TestLog.LogNoParameters(Logger);

            int result = await ExternalCallAsync().ConfigureAwait(false);

            TestLog.LogNoParameters(Logger);

            return result;
        }
        finally
        {
            TestLog.LogNoParameters(Logger);
        }
    }

    public Task<int> ExternalCallAsync()
    {
        int result = _counter += 2;
        return Task.FromResult(result);
    }
}
