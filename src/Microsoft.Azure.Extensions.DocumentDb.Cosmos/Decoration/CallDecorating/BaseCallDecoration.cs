// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

/// <summary>
/// Implements decoration logic of base call, using provided decorations list.
/// </summary>
/// <typeparam name="TContext">The type of decoration context.</typeparam>
internal readonly struct BaseCallDecoration<TContext> : ICallDecorationPipeline<TContext>
{
    private readonly IOnBeforeCosmosDecorator<TContext>[] _beforeDecorators;
    private readonly IOnAfterCosmosDecorator<TContext>[] _afterDecorators;
    private readonly IOnExceptionCosmosDecorator<TContext>[] _exceptionDecorators;
    private readonly IOnFinallyCosmosDecorator<TContext>[] _finallyDecorators;

    internal BaseCallDecoration(IReadOnlyList<ICosmosDecorator<TContext>> decorators)
    {
        _beforeDecorators = decorators.SelectDecorators<IOnBeforeCosmosDecorator<TContext>, TContext>();
        _afterDecorators = decorators.SelectDecorators<IOnAfterCosmosDecorator<TContext>, TContext>();
        _exceptionDecorators = decorators.SelectDecorators<IOnExceptionCosmosDecorator<TContext>, TContext>();
        _finallyDecorators = decorators.SelectDecorators<IOnFinallyCosmosDecorator<TContext>, TContext>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Implements `try` `catch` `finally` pattern to decorate given function by predefined decorators set of all types except on call.
    /// </remarks>
    public async Task<T> DoCallAsync<T>(
        Func<TContext, Func<Exception, T>, CancellationToken, Task<T>> functionToCall,
        TContext context,
        Func<Exception, T> exceptionHandler,
        CancellationToken cancellationToken)
    {
        try
        {
            // Inlined directly, since MethodImplOptions.AggressiveInlining not inlined them.
            // It was optimized in latest C# 10, but this code should work well for all versions.
            for (int index = 0, length = _beforeDecorators.Length; index < length; ++index)
            {
                _beforeDecorators[index].OnBefore(context);
            }

            T result = await functionToCall(context, exceptionHandler, cancellationToken)
                .ConfigureAwait(false);

            for (int index = 0, length = _afterDecorators.Length; index < length; ++index)
            {
                _afterDecorators[index].OnAfter(context, result);
            }

            return result;
        }
        catch (Exception exception)
        {
            bool shouldSkipException = false;
            Exception modifiedException = exception;

            for (int index = 0, length = _exceptionDecorators.Length; index < length; ++index)
            {
                try
                {
                    bool canSkip = _exceptionDecorators[index].OnException(context, modifiedException);
                    shouldSkipException = shouldSkipException || canSkip;
                }
#pragma warning disable CA1031 // It is a general exception processor, so catch general one.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    // Catch exception, to let other exception decorators like telemetry finish their work.
                    modifiedException = ex;
                }
            }

            if (shouldSkipException)
            {
                return exceptionHandler(modifiedException);
            }

            throw modifiedException;
        }
        finally
        {
            for (int index = 0, length = _finallyDecorators.Length; index < length; ++index)
            {
                _finallyDecorators[index].OnFinally(context);
            }
        }
    }
}
