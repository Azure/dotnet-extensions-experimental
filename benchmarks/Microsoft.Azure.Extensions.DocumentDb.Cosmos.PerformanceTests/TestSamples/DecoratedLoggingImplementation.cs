// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench.TestSamples;

internal sealed class DecoratedLoggingImplementation
{
    private readonly ICallDecorationPipeline<string> _decorators;
    private readonly PlainImplementation _classToDecorate;
    private readonly Func<string, Func<Exception, int>, CancellationToken, Task<int>> _funcReference;

    public DecoratedLoggingImplementation(ILogger logger)
    {
        _decorators = new LoggingDecoration(logger).MakeCallDecorationPipeline();

        _classToDecorate = new PlainImplementation();
        _funcReference = InternalGetEvenAsync;
    }

    public Task<int> GetEvenAsync(CancellationToken cancellationToken)
    {
        return _decorators.DoCallAsync(_funcReference, string.Empty, ex => 0, cancellationToken);
    }

    private Task<int> InternalGetEvenAsync(string context, Func<Exception, int> handler, CancellationToken _)
    {
        return _classToDecorate.GetEvenAsync();
    }
}
