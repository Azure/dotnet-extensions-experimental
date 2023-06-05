// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench.TestSamples;

internal sealed class LoggingDecoration :
    IOnAfterCosmosDecorator<string>,
    IOnBeforeCosmosDecorator<string>,
    IOnFinallyCosmosDecorator<string>
{
    private readonly ILogger _logger;

    public LoggingDecoration(ILogger logger)
    {
        _logger = logger;
    }

    public void OnAfter<T>(string context, T result)
    {
        TestLog.LogNoParameters(_logger);
    }

    public void OnBefore(string context)
    {
        TestLog.LogNoParameters(_logger);
    }

    public void OnFinally(string context)
    {
        TestLog.LogNoParameters(_logger);
    }
}
