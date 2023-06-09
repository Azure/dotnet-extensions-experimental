// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Consumers;

internal class SingleMessageConsumer : MessageConsumer
{
    public SingleMessageConsumer(IMessageSource source, IReadOnlyList<IMessageMiddleware> messageMiddlewares, MessageDelegate messageDelegate, ILogger logger)
        : base(source, messageMiddlewares, messageDelegate, logger)
    {
    }

    /// <inheritdoc/>
    public override ValueTask ExecuteAsync(CancellationToken cancellationToken) => ProcessingStepAsync(CancellationToken.None);

    /// <inheritdoc/>
    protected override ValueTask HandleMessageProcessingCompletionAsync(MessageContext context) => default;

    /// <inheritdoc/>
    protected override ValueTask HandleMessageProcessingFailureAsync(MessageContext context, Exception exception) => default;

    protected override ValueTask ProcessingStepAsync(CancellationToken cancellationToken) => FetchAndProcessMessageAsync(cancellationToken);
}
