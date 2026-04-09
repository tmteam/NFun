using System;
using System.Collections.Generic;

namespace NFun.Types;

/// <summary>
/// Registry that maps named type names to their field definitions.
/// Used by TIC to resolve named struct types during type inference setup.
/// </summary>
internal interface INamedTypeFieldRegistry {
    bool TryGetFields(string typeName, out (string name, FunnyType type)[] fields);
}

internal class NamedTypeFieldRegistry : INamedTypeFieldRegistry {
    private readonly Dictionary<string, (string name, FunnyType type)[]> _types
        = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string typeName, (string name, FunnyType type)[] fields) =>
        _types[typeName] = fields;

    public bool TryGetFields(string typeName, out (string name, FunnyType type)[] fields) =>
        _types.TryGetValue(typeName, out fields);
}
