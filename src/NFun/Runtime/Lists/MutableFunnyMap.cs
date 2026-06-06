using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NFun.Types;

namespace NFun.Runtime.Lists;

/// <summary>
/// Default implementation of lang-mode <c>map&lt;K, V&gt;</c>. Wraps a
/// <see cref="Dictionary{TKey,TValue}"/> as untyped
/// <c>Dictionary&lt;object, object&gt;</c> — key + value types carried as
/// separate metadata.
///
/// Iteration yields <see cref="FunnyStruct"/> instances with <c>key</c> and
/// <c>value</c> fields, so the map participates in
/// <see cref="IFunnyEnumerable"/> consumers (count, etc.) without needing a
/// separate code path. Order is unspecified (hash-based).
///
/// Equality is by value: two maps with the same key set and same value per
/// key compare equal regardless of insertion order.
/// </summary>
public class MutableFunnyMap : IFunnyMap {
    public FunnyType KeyType { get; }
    public FunnyType ValueType { get; }
    private readonly Dictionary<object, object> _items;
    private int? _hash;

    /// <summary>Element type for the IFunnyEnumerable contract — the
    /// <c>{key, value}</c> pair struct.</summary>
    public FunnyType ElementType { get; }

    public MutableFunnyMap(FunnyType keyType, FunnyType valueType) {
        KeyType = keyType;
        ValueType = valueType;
        ElementType = FunnyType.StructOf(("key", keyType), ("value", valueType));
        _items = new Dictionary<object, object>();
    }

    public MutableFunnyMap(FunnyType keyType, FunnyType valueType,
        IEnumerable<(object key, object value)> items)
        : this(keyType, valueType) {
        foreach (var (k, v) in items) _items[k] = v;
    }

    public int Count => _items.Count;

    public bool ContainsKey(object key) => _items.ContainsKey(key);

    public object GetOrNull(object key) =>
        _items.TryGetValue(key, out var v) ? v : null;

    public bool Set(object key, object value) {
        bool isNew = !_items.ContainsKey(key);
        _items[key] = value;
        _hash = null;
        return isNew;
    }

    public bool Remove(object key) {
        if (!_items.Remove(key)) return false;
        _hash = null;
        return true;
    }

    public void Clear() {
        if (_items.Count == 0) return;
        _items.Clear();
        _hash = null;
    }

    /// <summary>
    /// Iterate as a sequence of <c>{key, value}</c> FunnyStruct values — lets
    /// `count`, `for kv in m: ...`, and other Enumerable consumers work
    /// without per-map code paths.
    /// </summary>
    public IEnumerator<object> GetEnumerator() {
        foreach (var kv in _items)
            yield return FunnyStruct.Create(("key", kv.Key), ("value", kv.Value));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override bool Equals(object obj) => obj is MutableFunnyMap other && Equals(other);

    private bool Equals(MutableFunnyMap other) {
        if (Count != other.Count) return false;
        foreach (var kv in _items) {
            if (!other._items.TryGetValue(kv.Key, out var otherVal))
                return false;
            if (kv.Value == null) {
                if (otherVal != null) return false;
                continue;
            }
            if (!kv.Value.Equals(otherVal)) return false;
        }
        return true;
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode() {
        if (_hash.HasValue) return _hash.Value;
        unchecked {
            // Order-independent: XOR (k.hash * 397) ^ v.hash across entries.
            int h = Count * 397;
            foreach (var kv in _items) {
                int keyH = kv.Key?.GetHashCode() ?? 0;
                int valH = kv.Value?.GetHashCode() ?? 0;
                h ^= (keyH * 397) ^ valH;
            }
            _hash = h;
            return h;
        }
    }

    public override string ToString() {
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        bool first = true;
        foreach (var kv in _items) {
            if (!first) sb.Append(",");
            first = false;
            sb.Append(kv.Key).Append("->").Append(kv.Value);
        }
        sb.Append("}");
        return sb.ToString();
    }
}
