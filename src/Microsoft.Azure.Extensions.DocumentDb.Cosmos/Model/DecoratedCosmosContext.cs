// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Cloud.DocumentDb;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// Defines a context for Cosmos client decorations.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
    Justification = "The struct instances are not to be compared or used as a hash key.")]
public readonly struct DecoratedCosmosContext
{
    /// <summary>
    /// Gets a request options.
    /// </summary>
    public RequestOptions RequestOptions { get; }

    /// <summary>
    /// Gets a table options.
    /// </summary>
    public TableOptions? TableOptions { get; }

    /// <summary>
    /// Gets get an operation name.
    /// </summary>
    public string OperationName { get; }

    /// <summary>
    /// Gets an operation item.
    /// </summary>
    public object? Item { get; }

    /// <summary>
    /// Gets Item as a specific type.
    /// </summary>
    /// <remarks>
    /// The method throws <see cref="System.ArgumentNullException"/> if item is null or not T.
    /// </remarks>
    /// <typeparam name="T">The requested type.</typeparam>
    /// <returns>Gets item as T type.</returns>
    [SuppressMessage("Minor Code Smell", "S4049:Properties should be preferred", Justification = "A property wouldn't work in this case.")]
    public T GetItemOf<T>()
        where T : notnull
    {
        return (T)InternalThrows.IfNull(Item, "Item is null");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DecoratedCosmosContext"/> struct.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="requestOptions">The request options.</param>
    /// <param name="tableOptions">The table options.</param>
    /// <param name="item">The item operation targets.</param>
    public DecoratedCosmosContext(string operationName, RequestOptions requestOptions, TableOptions? tableOptions, object? item)
    {
        OperationName = operationName;
        RequestOptions = requestOptions;
        TableOptions = tableOptions;
        Item = item;
    }

    internal RequestInfo GetRequest(double? cost)
        => new(RequestOptions.Region, TableOptions?.TableName, cost);
}
