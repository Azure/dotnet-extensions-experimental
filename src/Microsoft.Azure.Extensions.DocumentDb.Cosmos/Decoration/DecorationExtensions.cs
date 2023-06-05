// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Decoration;

internal static class DecorationExtensions
{
    internal static T[] SelectDecorators<T, TContext>(this IReadOnlyList<ICosmosDecorator<TContext>> decorators)
        where T : ICosmosDecorator<TContext>
    {
        if (decorators.Count == 0)
        {
            return Array.Empty<T>();
        }

        var resultList = new List<T>();
        for (int i = 0; i < decorators.Count; i++)
        {
            if (decorators[i] is T decorator)
            {
                resultList.Add(decorator);
            }
        }

        return resultList.ToArray();
    }

    /// <summary>
    /// Makes call decoration pipeline incorporating all decorator types.
    /// </summary>
    /// <typeparam name="TContext">Type of context.</typeparam>
    /// <param name="decorators">Decorators list.</param>
    /// <returns>The call decoration pipeline.</returns>
    internal static ICallDecorationPipeline<TContext> MakeCallDecorationPipeline<TContext>(this IReadOnlyList<ICosmosDecorator<TContext>> decorators)
    {
        BaseCallDecoration<TContext> baseOnCallDecorator = new BaseCallDecoration<TContext>(decorators);

        IOnCallCosmosDecorator<TContext>[] callDecorators = decorators.SelectDecorators<IOnCallCosmosDecorator<TContext>, TContext>();

        return callDecorators.MakeCallDecorationPipeline(baseOnCallDecorator);
    }

    internal static ICallDecorationPipeline<TContext> MakeCallDecorationPipeline<TContext>(this ICosmosDecorator<TContext> decorator)
    {
        return new[] { decorator }
            .MakeCallDecorationPipeline();
    }

    private static ICallDecorationPipeline<TContext> MakeCallDecorationPipeline<TContext>(
        this IOnCallCosmosDecorator<TContext>[] decorators,
        ICallDecorationPipeline<TContext> currentPipeline,
        int decoratorIndex = 0)
    {
        if (decoratorIndex >= decorators.Length)
        {
            return currentPipeline;
        }

        ICallDecorationPipeline<TContext> core = new CallDecorationPipeline<TContext>(decorators[decoratorIndex], currentPipeline);

        return decorators.MakeCallDecorationPipeline(core, decoratorIndex + 1);
    }
}
