using System;
using System.Linq;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.TypeInferenceAdapter;

/// <summary>
/// Resolves TypeSyntax (syntactic type description) into FunnyType (semantic type).
/// This is where custom type names are looked up.
/// </summary>
internal static class TypeSyntaxResolver {
    internal static FunnyType Resolve(TypeSyntax syntax, ICustomTypeRegistry customTypes = null) =>
        syntax switch {
            TypeSyntax.EmptyType => FunnyType.Empty,
            TypeSyntax.Named n => ResolveNamed(n, customTypes),
            TypeSyntax.ArrayOf a => FunnyType.ArrayOf(Resolve(a.Element, customTypes)),
            TypeSyntax.OptionalOf o => FunnyType.OptionalOf(Resolve(o.Element, customTypes)),
            TypeSyntax.StructOf s => FunnyType.StructOf(
                s.IsFrozen,
                s.Fields.Select(f => (f.FieldName, Resolve(f.FieldType, customTypes))).ToArray()),
            _ => throw new ArgumentException($"Unknown TypeSyntax: {syntax}")
        };

    private static FunnyType ResolveNamed(TypeSyntax.Named named, ICustomTypeRegistry customTypes) {
        var name = named.Name;
        return name.ToLowerInvariant() switch {
            "int16"           => FunnyType.Int16,
            "int" or "int32"  => FunnyType.Int32,
            "int64"           => FunnyType.Int64,
            "byte" or "uint8" => FunnyType.UInt8,
            "uint16"          => FunnyType.UInt16,
            "uint" or "uint32" => FunnyType.UInt32,
            "uint64"          => FunnyType.UInt64,
            "real"            => FunnyType.Real,
            "bool"            => FunnyType.Bool,
            "char"            => FunnyType.Char,
            "text"            => FunnyType.Text,
            "any"             => FunnyType.Any,
            "ip"              => FunnyType.Ip,
            _ => ResolveCustomOrThrow(named, customTypes)
        };
    }

    private static FunnyType ResolveCustomOrThrow(TypeSyntax.Named named, ICustomTypeRegistry customTypes) {
        if (customTypes != null && customTypes.TryResolve(named.Name, out var customType))
            return customType;
        throw new NFun.Exceptions.FunnyParseException(
            406, $"Expected: type, but was '{named.Name}'", named.Interval.Start, named.Interval.Finish);
    }
}
