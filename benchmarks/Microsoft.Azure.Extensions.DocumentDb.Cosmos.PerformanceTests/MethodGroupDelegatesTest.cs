// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Bench;

#pragma warning disable R9A034 // Optimize method group use to avoid allocations

[MemoryDiagnoser]
public class MethodGroupDelegatesTest
{
    private readonly Func<int> _intDelegateMethod;

    public MethodGroupDelegatesTest()
    {
        _intDelegateMethod = Producer<int>;
    }

    // When a method group used directly like Producer<int>(), no overhead
    // When it passed as a parameter, even if resolved directly to a specific T (etc. int)
    // Seems it is not actually resolves to a Func<int> on compile time,
    // but doing it in run time, creates a new delegate that for some reason not cached for new same type calls.
    // So we are seeing both heap memory allocations and cpu overhead
    [Benchmark]
    public static int TestIntDelegateFunc() => InvokeDelegate(Producer<int>);

    // In a case we know actual type, prestoring to a specific delegate avoid the issue,
    // since the resolution work not repeated and no memory allocated even for first delegate.
    // This is useful when we using a generic tool reducing scope to our specific scenario.
    // But I would expect compiler/intepreter to handle even generic case calling `InvokeDelegate(Producer)` with no type specification
    // I wonder would it be fixed in C# 6 https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/lambda-improvements
    // Where lambdas with attributes introduced
    // So, that in worst case we would be able to prestore generic one to `[T]Func<T>` kind of delegate.
    // Hopefully this would work as fast as Func<int> for a specific type.
    [Benchmark(Baseline = true)]
    public int TestIntFunc() => InvokeDelegate(_intDelegateMethod);

    private static T? Producer<T>()
    {
        return default;
    }

    private static T InvokeDelegate<T>(Func<T> delegateFunc) => delegateFunc();
}
