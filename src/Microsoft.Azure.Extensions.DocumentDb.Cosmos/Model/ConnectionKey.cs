// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Azure.Extensions.Document.Cosmos;

/// <summary>
/// The key used in dictionary to represent a connection.
/// </summary>
internal readonly struct ConnectionKey : IEquatable<ConnectionKey>
{
    private readonly string _endpoint;
    private readonly string _database;
    private readonly int _hash;

    public ConnectionKey(string endpoint, string database)
    {
        _endpoint = endpoint;
        _database = database;
        _hash = (_endpoint, _database).GetHashCode();
    }

    public override int GetHashCode()
    {
        return _hash;
    }

    public static bool operator ==(ConnectionKey left, ConnectionKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ConnectionKey left, ConnectionKey right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is ConnectionKey key)
        {
            return Equals(key);
        }

        return false;
    }

    public bool Equals(ConnectionKey other)
    {
        return _endpoint == other._endpoint && _database == other._database;
    }
}
