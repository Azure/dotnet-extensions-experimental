// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Extensions.Document.Cosmos.Test;

internal class TestCosmosResponse<T> : ItemResponse<T>
{
    public static ItemResponse<T> Empty { get; } = new TestCosmosResponse<T>();

    public override T Resource { get; }
    public override Headers Headers => throw new NotSupportedException();
    public override HttpStatusCode StatusCode { get; }
    public override double RequestCharge { get; } = 1;
    public override string ActivityId => throw new NotSupportedException();
    public override string ETag { get; }
    public override CosmosDiagnostics Diagnostics => throw new NotSupportedException();

    internal TestCosmosResponse()
    {
        Resource = default!;
        ETag = default!;
    }

    internal TestCosmosResponse(T? item, HttpStatusCode code = HttpStatusCode.OK)
    {
        Resource = item!;
        StatusCode = code;
        ETag = default!;
    }

    internal static ItemResponse<T> Create(T? item, HttpStatusCode code = HttpStatusCode.OK)
    {
        return new TestCosmosResponse<T>(item, code);
    }
}
