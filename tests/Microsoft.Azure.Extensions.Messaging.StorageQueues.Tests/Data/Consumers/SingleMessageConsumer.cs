// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Cloud.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Data.Consumers;

internal class SingleMessageConsumer : BaseMessageConsumer
{
    public SingleMessageConsumer(IMessageSource source, IMessageDelegate messageDelegate, ILogger logger)
        : base(source, messageDelegate, logger)
    {
    }

    /// <inheritdoc/>
    public override async ValueTask ExecuteAsync(CancellationToken cancellationToken)
    {
        await FetchAndProcessMessageAsync(cancellationToken);
    }

    /// <inheritdoc/>
    protected override ValueTask OnMessageProcessingCompletionAsync(MessageContext context) => default;

    /// <inheritdoc/>
    protected override ValueTask OnMessageProcessingFailureAsync(MessageContext context, Exception exception) => default;
}
