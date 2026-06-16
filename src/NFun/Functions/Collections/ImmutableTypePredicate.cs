using NFun.Exceptions;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Functions.Collections;

/// <summary>
/// Predicate gate for Set/Map key/element types. Initial scope: primitives only
/// (bool, integer family, real, char, ipaddress) plus <c>text</c> (which is
/// <c>char[]</c> at the type level but runtime-backed by an immutable string).
///
/// <para>Recursive extension to <c>FixedArray&lt;T&gt;</c>, <c>Fun(...)</c>,
/// frozen-struct, <c>Optional&lt;T&gt;</c> is tracked in
/// <see href="https://github.com/tmteam/NFun/issues/129">issue #129</see>.
/// Until that ships, mutable composites and shallow-immutable composites alike
/// are rejected at the call site of <c>set(...)</c> / <c>__mkMap(...)</c>.</para>
/// </summary>
internal static class ImmutableTypePredicate {
    public static bool IsImmutable(FunnyType type) {
        if (IsImmutablePrimitive(type.BaseType)) return true;
        // text == char[] at the type layer but is backed by System.String at
        // runtime — semantically immutable, safe as a key.
        if (type.BaseType == BaseFunnyType.ArrayOf
            && type.ArrayTypeSpecification?.FunnyType.BaseType == BaseFunnyType.Char)
            return true;
        return false;
    }

    private static bool IsImmutablePrimitive(BaseFunnyType b) =>
        b is BaseFunnyType.Bool
            or BaseFunnyType.Char
            or BaseFunnyType.UInt8 or BaseFunnyType.UInt16
            or BaseFunnyType.UInt32 or BaseFunnyType.UInt64
            or BaseFunnyType.Int16 or BaseFunnyType.Int32 or BaseFunnyType.Int64
            or BaseFunnyType.Real
            or BaseFunnyType.Ip;

    public static void RequireImmutable(FunnyType type, string callerName, string role) {
        if (IsImmutable(type)) return;
        throw new FunnyParseException(
            580,
            $"{callerName}: {role} type `{type}` is not Immutable. " +
            "Initial scope accepts only primitives (bool, int*, real, char, text, ipaddress). " +
            "Extension to FixedArray<T>, Fun(...), and frozen-struct is tracked in issue #129.",
            new Interval(0, 0));
    }
}
