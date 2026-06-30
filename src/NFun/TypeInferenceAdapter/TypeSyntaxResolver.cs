using System;
using System.Linq;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.TypeInferenceAdapter;

/// <summary>
/// Resolves TypeSyntax (syntactic type description) into FunnyType (semantic type).
/// This is where custom type names are looked up.
/// </summary>
public static class TypeSyntaxResolver {
    public static FunnyType Resolve(TypeSyntax syntax, ICustomTypeRegistry customTypes = null) =>
        syntax switch {
            TypeSyntax.EmptyType => FunnyType.Empty,
            TypeSyntax.Named n => ResolveNamed(n, customTypes),
            TypeSyntax.ArrayOf a => FunnyType.ArrayOf(Resolve(a.Element, customTypes)),
            TypeSyntax.OptionalOf o => FunnyType.OptionalOf(Resolve(o.Element, customTypes)),
            TypeSyntax.StructOf s => FunnyType.StructOf(
                s.IsFrozen,
                s.Fields.Select(f => (f.FieldName, Resolve(f.FieldType, customTypes))).ToArray()),
            TypeSyntax.FunOf f => FunnyType.FunOf(
                Resolve(f.ReturnType, customTypes),
                f.ArgTypes.Select(a => Resolve(a, customTypes)).ToArray()),
            _ => throw new ArgumentException($"Unknown TypeSyntax: {syntax}")
        };

    private static FunnyType ResolveNamed(TypeSyntax.Named named, ICustomTypeRegistry customTypes) =>
        TryResolvePrimitiveKeyword(named.Name, out var t)
            ? t
            : ResolveCustomOrThrow(named, customTypes);

    /// <summary>
    /// Maps a bare primitive-type keyword (int8, int16, real, float32, etc.)
    /// to its <see cref="FunnyType"/>. Returns false for user-defined / composite names.
    /// Case-insensitive; accepts all NFun aliases (byte ≡ uint8, int ≡ int32, sbyte ≡ int8, float64 ≡ real).
    /// </summary>
    public static bool TryResolvePrimitiveKeyword(string name, out FunnyType type) {
        type = name.ToLowerInvariant() switch {
            "int8" or "sbyte"  => FunnyType.Int8,
            "float32"          => FunnyType.Float32,
            "float64"          => FunnyType.Real,
            "int16"            => FunnyType.Int16,
            "int" or "int32"   => FunnyType.Int32,
            "int64"            => FunnyType.Int64,
            "byte" or "uint8"  => FunnyType.UInt8,
            "uint16"           => FunnyType.UInt16,
            "uint" or "uint32" => FunnyType.UInt32,
            "uint64"           => FunnyType.UInt64,
            "real"             => FunnyType.Real,
            "bool"             => FunnyType.Bool,
            "char"             => FunnyType.Char,
            "text"             => FunnyType.Text,
            "any"              => FunnyType.Any,
            "ip"               => FunnyType.Ip,
            _                  => FunnyType.Empty
        };
        return type.BaseType != BaseFunnyType.Empty;
    }

    private static FunnyType ResolveCustomOrThrow(TypeSyntax.Named named, ICustomTypeRegistry customTypes) {
        if (customTypes != null && customTypes.TryResolve(named.Name, out var customType))
            return customType;
        throw new Exceptions.FunnyParseException(
            406, $"Expected: type, but was '{named.Name}'", named.Interval.Start, named.Interval.Finish);
    }
}
