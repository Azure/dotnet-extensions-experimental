// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Microsoft.Extensions.Http.Telemetry.Logging.Test;

internal static class HelperExtensions
{
    public static IServiceCollection BlockRemoteCall(this IServiceCollection services)
    {
        return services
            .AddTransient<NoRemoteCallHandler>()
            .ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
                    builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<NoRemoteCallHandler>());
                });
            });
    }
}

