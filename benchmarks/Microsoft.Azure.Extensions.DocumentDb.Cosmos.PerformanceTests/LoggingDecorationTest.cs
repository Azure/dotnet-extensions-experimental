// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Extensions.Document.Cosmos.Bench.TestSamples;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench;

[GcServer(true)]
[MinColumn]
[MaxColumn]
[MemoryDiagnoser]
public class LoggingDecorationTest
{
    private readonly PlainImplementation _plainImplementation;
    private readonly PlainLoggingImplementation _plainLoggingImplementation;
    private readonly DecoratedLoggingImplementation _decoratedLoggingImplementation;

    public LoggingDecorationTest()
    {
        ILogger logger = NullLogger.Instance;

        _plainImplementation = new PlainImplementation();
        _plainLoggingImplementation = new PlainLoggingImplementation(logger);
        _decoratedLoggingImplementation = new DecoratedLoggingImplementation(logger);
    }

    [Benchmark]
    public async Task<int> PlainTestAsync()
    {
        return await _plainImplementation.GetEvenAsync()
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<int> PlainLoggingTestAsync()
    {
        return await _plainLoggingImplementation.GetEvenAsync()
            .ConfigureAwait(false);
    }

    [Benchmark]
    public async Task<int> DecoratedLoggingTestAsync()
    {
        return await _decoratedLoggingImplementation.GetEvenAsync(default)
            .ConfigureAwait(false);
    }
}
