// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER

using System.Globalization;

namespace System;

/// <summary>
/// Provides functionality to format the value of an object into a string representation.
/// </summary>
public interface ISpanFormattable : IFormattable
{
    /// <summary>
    /// Tries to format the value of the current instance using the specified format.
    /// </summary>
    /// <param name="destination">Destination buffer that receives the formatted text.</param>
    /// <param name="charsWritten">Receives the number of characters written into the destination buffer.</param>
    /// <param name="format">The format to use. If this is empty, then formatting uses the default format defined for the type.</param>
    /// <param name="provider">The provider to use to format the value, or <see langword="null" /> to use <see cref="CultureInfo.CurrentCulture" />.</param>
    /// <returns><see langword="true"/> if the destination buffer was large enough, <see langword="false"/> otherwise.</returns>
    bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider);
}

#endif
