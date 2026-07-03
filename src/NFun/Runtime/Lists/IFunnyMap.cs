using NFun.Types;

namespace NFun.Runtime.Lists;

/// <summary>
/// Lang-mode <c>map&lt;K, V&gt;</c> contract — unordered hash-based key→value
/// mapping with O(1) membership testing. Iteration yields
/// <see cref="NFun.Runtime.FunnyStruct"/> instances with <c>key</c> and
/// <c>value</c> fields, so a map satisfies
/// <see cref="IFunnyEnumerable"/> with element-type
/// <c>{key:K, value:V}</c>.
///
/// Stage 5 / Map.1: value-equality is by content (same key set + same value
/// per key, order-independent).
/// </summary>
public interface IFunnyMap : IFunnyEnumerable {
    /// <summary>Key type as observed at construction.</summary>
    FunnyType KeyType { get; }

    /// <summary>Value type as observed at construction.</summary>
    FunnyType ValueType { get; }

    /// <summary>True when <paramref name="key"/> is a member.</summary>
    bool ContainsKey(object key);

    /// <summary>Value for <paramref name="key"/>, or null when absent.</summary>
    object GetOrNull(object key);

    /// <summary>
    /// Sets or inserts <paramref name="key"/> → <paramref name="value"/>.
    /// Returns true when the key was new.
    /// </summary>
    bool Set(object key, object value);

    /// <summary>Removes <paramref name="key"/>. Returns true when present.</summary>
    bool Remove(object key);

    /// <summary>Drops every entry.</summary>
    void Clear();
}
