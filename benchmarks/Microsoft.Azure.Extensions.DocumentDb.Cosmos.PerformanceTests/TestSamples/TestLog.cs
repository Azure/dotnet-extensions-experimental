// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench.TestSamples;

internal static partial class TestLog
{
    private const int TWO = 2;

    [LogMethod(0, LogLevel.Information, "'{method}' - '{methodStage}' called.")]
    public static partial void LogStage(ILogger logger, string method, string methodStage);

    [LogMethod(1, LogLevel.Information, "'{method}' - '{methodStage}' called, result is: `{result}`.")]
    public static partial void LogStageResult(ILogger logger, string method, string methodStage, object result);

    [LogMethod(TWO, LogLevel.Information, "Log no parameters called.")]
    public static partial void LogNoParameters(ILogger logger);
}
