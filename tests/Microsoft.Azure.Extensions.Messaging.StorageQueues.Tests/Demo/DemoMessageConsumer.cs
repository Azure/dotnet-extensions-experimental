// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data;

internal class DemoMessageConsumer : MessageConsumer
{
    public DemoMessageConsumer(IMessageSource messageSource, IReadOnlyList<IMessageMiddleware> messageMiddlewares, MessageDelegate messageDelegate, ILogger logger)
        : base(messageSource, messageMiddlewares, messageDelegate, logger)
    {
    }

    protected override ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception) => default;

    protected override ValueTask ProcessingStepAsync(CancellationToken cancellationToken) => FetchAndProcessMessageAsync(cancellationToken);
}
