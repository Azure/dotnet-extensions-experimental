// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.DocumentDb;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench;

[GcServer(true)]
[MinColumn]
[MaxColumn]
[MemoryDiagnoser]
public sealed class ResultStructBenchmark
{
    private readonly string _response = "123";
    private readonly int _recurcy = 4;

    [Benchmark]
    public async Task<string?> ClassTestAsync()
    {
        var answer = await DoTest(
                () => new DatabaseResponseClass<string>(item: _response), _recurcy)
            .ConfigureAwait(false);

        return answer.Item;
    }

    [Benchmark(Baseline = true)]
    public async Task<string?> StructTestAsync()
    {
        var answer = await DoTest(
                () => new DatabaseResponseStruct<string>(item: _response), _recurcy)
            .ConfigureAwait(false);

        return answer.Item;
    }

    private static async Task<T> DoTest<T>(Func<T> preparedResult, int recurcy)
    {
        if (recurcy > 0)
        {
            return await DoTest(preparedResult, recurcy - 1).ConfigureAwait(false);
        }

        return preparedResult.Invoke();
    }

    /// <summary>
    /// That is the same as <see cref="DatabaseResponse{T}"/> but class.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    private sealed class DatabaseResponseClass<T>
        where T : notnull
    {
        public DatabaseResponseClass(
            in RequestInfo requestInfo = default,
            int statusCode = 0,
            T? item = default,
            string? itemVersion = null,
            string? continuationToken = null,
            bool succeeded = true)
        {
            RequestInfo = requestInfo;
            Status = statusCode;
            Item = item;
            ItemVersion = itemVersion;
            ContinuationToken = continuationToken;
            Succeeded = succeeded;
        }

        public int Status { get; }
        public T? Item { get; }
        public RequestInfo RequestInfo { get; }
        public string? ItemVersion { get; }
        public bool Succeeded { get; }
        public string? ContinuationToken { get; }
    }

    /// <summary>
    /// That is the same as <see cref="DatabaseResponse{T}"/> but struct.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    private readonly struct DatabaseResponseStruct<T>
        where T : notnull
    {
        public DatabaseResponseStruct(
            in RequestInfo requestInfo = default,
            int statusCode = 0,
            T? item = default,
            string? itemVersion = null,
            string? continuationToken = null,
            bool succeeded = true)
        {
            RequestInfo = requestInfo;
            Status = statusCode;
            Item = item;
            ItemVersion = itemVersion;
            ContinuationToken = continuationToken;
            Succeeded = succeeded;
        }

        public int Status { get; }
        public T? Item { get; }
        public RequestInfo RequestInfo { get; }
        public string? ItemVersion { get; }
        public bool Succeeded { get; }
        public string? ContinuationToken { get; }
    }
}
