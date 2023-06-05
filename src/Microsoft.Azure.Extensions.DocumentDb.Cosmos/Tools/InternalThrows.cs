// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Defines static methods used to throw exceptions.
/// </summary>
internal static class InternalThrows
{
    /// <summary>
    /// Throws an <see cref="InvalidDataException"/>.
    /// if the specified string is <see langword="null"/> or whitespace.
    /// </summary>
    /// <param name="argument">String to be checked for <see langword="null"/> or whitespace.</param>
    /// <param name="error">The error message.</param>
    /// <returns>The original value of <paramref name="argument"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static string IfNullOrWhitespace([NotNull] string? argument, string error)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            InvalidDataException(error);
        }

#pragma warning disable CS8777 // Checked for null above.
        return argument!;
#pragma warning restore CS8777
    }

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/>.
    /// if the specified collection is <see langword="null"/> or empty.
    /// </summary>
    /// <param name="argument">Collection to be checked for <see langword="null"/> or empty.</param>
    /// <param name="error">The error message.</param>
    /// <returns>The original value of <paramref name="argument"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static TCollection IfNullOrEmpty<TCollection, TType>([NotNull] TCollection? argument, string error)
        where TCollection : IReadOnlyCollection<TType>
    {
        if (argument is null || argument.Count == 0)
        {
            InvalidDataException(error);
        }

        return argument;
    }

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/>.
    /// if the specified enumerable is <see langword="null"/> or empty.
    /// </summary>
    /// <param name="argument">Enumerable to be checked for <see langword="null"/> or empty.</param>
    /// <param name="error">The error message.</param>
    /// <returns>The original value of <paramref name="argument"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static TEnumerable IfNullOrEmptyEnumerable<TEnumerable, TType>([NotNull] TEnumerable? argument, string error)
        where TEnumerable : IEnumerable<TType>
    {
        if (argument is null || !argument.Any())
        {
            InvalidDataException(error);
        }

        return argument;
    }

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/> if the specified object is <see langword="null"/>.
    /// </summary>
    /// <param name="argument">Object to be checked for <see langword="null"/>.</param>
    /// <param name="error">The error message.</param>
    /// <returns>The original value of <paramref name="argument"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static T IfNull<T>([NotNull] T? argument, string error)
    {
        if (argument is null)
        {
            InvalidDataException(error);
        }

        return argument;
    }

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/>.
    /// </summary>
    /// <param name="message">The explanation message that caused the exception.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public static void InvalidDataException(string message)
        => throw new InvalidDataException(message);
}
