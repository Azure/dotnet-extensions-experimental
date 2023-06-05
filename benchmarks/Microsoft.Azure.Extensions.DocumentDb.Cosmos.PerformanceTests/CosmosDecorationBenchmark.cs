// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench;

#pragma warning disable R9A034 // Optimize method group use to avoid allocations

[GcServer(true)]
[MinColumn]
[MaxColumn]
[MemoryDiagnoser]
public class CosmosDecorationBenchmark
{
    [SuppressMessage("Minor Code Smell", "S3459:Unassigned members should be removed",
        Justification = "Default value is assigned and what is wanted.")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression",
        Justification = "IDE is not aware of above warning.")]
    private CancellationToken CancellationToken { get; }

    // This should to be an even number
    [Params(TEN)]
    public uint Iterations { get; set; }

    private const int TEN = 10;
    private const int TWO = 2;
#pragma warning disable CS8618 // Initialized in target test.
    private ICallDecorationPipeline<TestContext> _decorators;
#pragma warning restore CS8618

    [Benchmark]
    public async Task DecorationTestAsync()
    {
        TestPerformanceDecorator additionalJobsToDo = new();
        TestContext context = new TestContext();
        _decorators = additionalJobsToDo.MakeCallDecorationPipeline();

        await DoTest(TestDecorationImplementation, additionalJobsToDo, context)
            .ConfigureAwait(false);
    }

    [Benchmark(Baseline = true)]
    public async Task PlainTestAsync()
    {
        TestPerformanceDecorator additionalJobsToDo = new();
        TestContext context = new TestContext();

        await DoTest(TestPlainImplementation, additionalJobsToDo, context)
            .ConfigureAwait(false);
    }

    private async Task DoTest(Func<TestPerformanceDecorator, TestContext, Func<Exception, int>,
        Func<TestContext, Func<Exception, int>, CancellationToken, Task<int>>, Task<int>> implementation,
        TestPerformanceDecorator additionalJobsToDo, TestContext context)
    {
        for (uint i = 0; i < Iterations; i += 1)
        {
            await implementation(additionalJobsToDo, context, _ => 0,
                (_, _, _) => i % TWO == 1 ? Task.FromResult((int)i) : throw new TestException($"{i}"))
                .ConfigureAwait(false);
        }
    }

    private Task<int> TestPlainImplementation(TestPerformanceDecorator additionalJobsToDo,
        TestContext context, Func<Exception, int> handler,
        Func<TestContext, Func<Exception, int>, CancellationToken, Task<int>> mainJob)
    {
        return additionalJobsToDo.OnCallAsync(async (mainJob, context, handler, cancellationToken) =>
        {
            try
            {
                additionalJobsToDo.OnBefore(context);
                int result = await mainJob(context, handler, cancellationToken)
                    .ConfigureAwait(false);
                additionalJobsToDo.OnAfter(context, result);

                return result;
            }
            catch (TestException ex)
            {
                if (additionalJobsToDo.OnException(context, ex))
                {
                    return handler(ex);
                }

                throw;
            }
            finally
            {
                additionalJobsToDo.OnFinally(context);
            }
        }, mainJob, context, handler, CancellationToken);
    }

    private async Task<int> TestDecorationImplementation(TestPerformanceDecorator additionalJobsToDo, TestContext context, Func<Exception, int> handler,
        Func<TestContext, Func<Exception, int>, CancellationToken, Task<int>> mainJob)
    {
        return await _decorators.DoCallAsync(mainJob, context, handler, CancellationToken)
            .ConfigureAwait(false);
    }

    public class TestException : Exception
    {
        public TestException(string text)
            : base(text)
        {
        }
    }

    public class TestContext
    {
        public long Counter;
    }

    public sealed class TestPerformanceDecorator :
        IOnAfterCosmosDecorator<TestContext>,
        IOnBeforeCosmosDecorator<TestContext>,
        IOnCallCosmosDecorator<TestContext>,
        IOnExceptionCosmosDecorator<TestContext>,
        IOnFinallyCosmosDecorator<TestContext>
    {
        public void OnAfter<T>(TestContext context, T result)
        {
            context.Counter += result is int count ? count : 1;
        }

        public void OnBefore(TestContext context)
        {
            context.Counter += (int)Math.Log10(context.Counter);
        }

        public Task<T> OnCallAsync<T>(
            Func<Func<TestContext, Func<Exception, T>, CancellationToken, Task<T>>, TestContext, Func<Exception, T>, CancellationToken, Task<T>> callToBeDecorated,
            Func<TestContext, Func<Exception, T>, CancellationToken, Task<T>> functionParameter,
            TestContext context, Func<Exception, T> exceptionHandler, CancellationToken cancelationToken)
        {
            context.Counter += (int)Math.Log10(context.Counter);
            return callToBeDecorated(functionParameter, context, exceptionHandler, cancelationToken);
        }

        public bool OnException(TestContext context, Exception exception)
        {
            context.Counter += exception.Message.Length;
            return true;
        }

        public void OnFinally(TestContext context)
        {
            context.Counter += (int)Math.Log10(context.Counter);
        }
    }
}
