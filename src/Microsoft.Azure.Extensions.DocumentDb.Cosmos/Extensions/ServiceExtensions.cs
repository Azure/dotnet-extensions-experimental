// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Provides extensions for dependency injection.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds an instance of <see cref="System.Cloud.DocumentDb.IDocumentDatabase{DatabaseOptions}"/> to <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// It requires <see cref="IOptions{DatabaseOptions}"/> to be configured.
    /// This method to be used only if a single database will be used in a scope of application.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns><see cref="IDatabaseBuilder"/> for defining database.</returns>
    public static IDatabaseBuilder GetCosmosDatabaseBuilder(this IServiceCollection services)
        => new DatabaseBuilder(services);

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicallyAccessedMembers]")]
    internal static T Validate<T>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        this IOptions<T> options)
        where T : class, new()
    {
        options = Throw.IfNull(options);
        var value = Throw.IfNull(options.Value);

        Validator.ValidateObject(value, new ValidationContext(value, null, null));

        return value;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicallyAccessedMembers]")]
    internal static T Validate<T>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        this IOptionsMonitor<T> options,
        string context)
        where T : class, new()
    {
        context = Throw.IfNull(context);
        options = Throw.IfNull(options);
        var value = Throw.IfNull(options.Get(context));

        Validator.ValidateObject(value, new ValidationContext(value, null, null));

        return value;
    }
}
