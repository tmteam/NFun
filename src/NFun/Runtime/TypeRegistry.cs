using System;
using System.Collections.Generic;
using NFun.Types;

namespace NFun.Runtime;

/// <summary>
/// Immutable registry of named type definitions.
/// Accessible from FunnyRuntime for type introspection.
/// </summary>
public class TypeRegistry {
    public static readonly TypeRegistry Empty = new(new Dictionary<string, NamedTypeInfo>());

    private readonly Dictionary<string, NamedTypeInfo> _types;

    internal TypeRegistry(Dictionary<string, NamedTypeInfo> types) =>
        _types = types;

    public NamedTypeInfo this[string name] =>
        _types.TryGetValue(name, out var info)
            ? info
            : throw new KeyNotFoundException($"Named type '{name}' is not defined");

    public bool TryGetType(string name, out NamedTypeInfo info) =>
        _types.TryGetValue(name, out info);

    public IEnumerable<string> TypeNames => _types.Keys;
    public int Count => _types.Count;
}

/// <summary>
/// Information about a named type definition.
/// </summary>
public class NamedTypeInfo {
    public string Name { get; }

    /// <summary>True for type aliases (type age = int). False for struct types.</summary>
    public bool IsAlias { get; }

    /// <summary>For aliases: the resolved target type. For structs: the struct type itself.</summary>
    public FunnyType Type { get; }

    /// <summary>For struct types: field definitions. Null for aliases.</summary>
    public IReadOnlyList<NamedTypeFieldInfo> Fields { get; }

    internal NamedTypeInfo(string name, FunnyType type) {
        Name = name;
        IsAlias = true;
        Type = type;
    }

    internal NamedTypeInfo(string name, FunnyType type, IReadOnlyList<NamedTypeFieldInfo> fields) {
        Name = name;
        IsAlias = false;
        Type = type;
        Fields = fields;
    }
}

/// <summary>
/// Information about a field in a named struct type.
/// </summary>
public class NamedTypeFieldInfo {
    public string Name { get; }
    public FunnyType Type { get; }
    public bool HasDefault { get; }

    internal NamedTypeFieldInfo(string name, FunnyType type, bool hasDefault) {
        Name = name;
        Type = type;
        HasDefault = hasDefault;
    }
}
