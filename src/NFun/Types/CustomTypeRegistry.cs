using System;
using System.Collections.Generic;

namespace NFun.Types;

/// <summary>
/// Registry of user-defined custom types.
/// </summary>
internal interface ICustomTypeRegistry {
    bool TryResolve(string name, out FunnyType type);
    ICustomTypeRegistry CloneWith(string name, FunnyType customType);
    bool IsEmpty { get; }
    bool TryResolveByClrType(Type clrType, out FunnyType funnyType);
}

internal class EmptyCustomTypeRegistry : ICustomTypeRegistry {
    public static readonly ICustomTypeRegistry Instance = new EmptyCustomTypeRegistry();
    private EmptyCustomTypeRegistry() { }

    public bool TryResolve(string name, out FunnyType type) {
        type = default;
        return false;
    }

    public ICustomTypeRegistry CloneWith(string name, FunnyType customType) =>
        new CustomTypeRegistry(name, customType);

    public bool IsEmpty => true;

    public bool TryResolveByClrType(Type clrType, out FunnyType funnyType) {
        funnyType = default;
        return false;
    }
}

internal class CustomTypeRegistry : ICustomTypeRegistry {

    private readonly Dictionary<string, FunnyType> _types;

    internal CustomTypeRegistry(string name, FunnyType customType) {
        _types = new Dictionary<string, FunnyType>(StringComparer.OrdinalIgnoreCase) {
            [name.ToLowerInvariant()] = customType
        };
    }

    private CustomTypeRegistry(Dictionary<string, FunnyType> types) => _types = types;

    public bool TryResolve(string name, out FunnyType type) =>
        _types.TryGetValue(name.ToLowerInvariant(), out type);

    public ICustomTypeRegistry CloneWith(string name, FunnyType customType) {
        var copy = new Dictionary<string, FunnyType>(_types, StringComparer.OrdinalIgnoreCase);
        copy[name.ToLowerInvariant()] = customType;
        return new CustomTypeRegistry(copy);
    }

    public bool IsEmpty => _types.Count == 0;

    public bool TryResolveByClrType(Type clrType, out FunnyType funnyType) {
        foreach (var ft in _types.Values)
        {
            if (ft.CustomTypeDefinition != null && ft.CustomTypeDefinition.DefaultValue.GetType() == clrType)
            {
                funnyType = ft;
                return true;
            }
        }
        funnyType = default;
        return false;
    }
}
