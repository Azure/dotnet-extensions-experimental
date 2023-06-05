// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench.TestSamples;

internal sealed class PlainImplementation
{
    private int _counter;

    public Task<int> GetEvenAsync()
    {
        return ExternalCallAsync();
    }

    public Task<int> ExternalCallAsync()
    {
        int result = _counter += 2;
        return Task.FromResult(result);
    }
}
