using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NFun.Types;

namespace NFun.Runtime.Lists;

/// <summary>
/// Default implementation of lang-mode <c>set&lt;T&gt;</c>. Wraps a
/// <see cref="HashSet{T}"/> as untyped <c>HashSet&lt;object&gt;</c> —
/// element-type is carried separately as <see cref="ElementType"/>.
///
/// Equality is by value: two <c>set&lt;T&gt;</c> values with the same
/// cardinality and the same element set compare equal (order-independent).
/// </summary>
public class MutableFunnySet : IFunnyMutableSet {
    public FunnyType ElementType { get; }
    private readonly HashSet<object> _items;
    private int? _hash;

    public MutableFunnySet(FunnyType elementType) {
        ElementType = elementType;
        _items = new HashSet<object>();
    }

    public MutableFunnySet(FunnyType elementType, IEnumerable<object> items) {
        ElementType = elementType;
        _items = new HashSet<object>(items);
    }

    public int Count => _items.Count;

    public bool Contains(object value) => _items.Contains(value);

    public bool Add(object value) {
        if (!_items.Add(value)) return false;
        _hash = null;
        return true;
    }

    public bool Remove(object value) {
        if (!_items.Remove(value)) return false;
        _hash = null;
        return true;
    }

    public void Clear() {
        if (_items.Count == 0) return;
        _items.Clear();
        _hash = null;
    }

    public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public override bool Equals(object obj) => obj is MutableFunnySet other && Equals(other);

    private bool Equals(MutableFunnySet other) =>
        Count == other.Count && _items.SetEquals(other._items);

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode() {
        if (_hash.HasValue) return _hash.Value;
        unchecked {
            // Order-independent hash — XOR per element, so the same elements in
            // a different insertion order give the same hash. Mutators reset
            // _hash to null so a later Add/Remove rebuilds.
            int h = Count * 397;
            foreach (var v in _items)
                h ^= v?.GetHashCode() ?? 0;
            _hash = h;
            return h;
        }
    }

    public override string ToString() {
        var parts = new List<string>(_items.Count);
        foreach (var v in _items) parts.Add(v?.ToString() ?? "null");
        return "{" + string.Join(",", parts) + "}";
    }
}
