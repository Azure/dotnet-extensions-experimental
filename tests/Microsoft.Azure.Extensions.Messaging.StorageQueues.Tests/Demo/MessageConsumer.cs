// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.Messaging;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Extensions.Messaging.StorageQueues.Tests.Demo;

internal class MessageConsumer : BaseMessageConsumer
{
    public MessageConsumer(IMessageSource messageSource, IMessageDelegate messageDelegate, ILogger logger)
        : base(messageSource, messageDelegate, logger)
    {
    }
}
