using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime.Lists;

/// <summary>
/// Lang-mode <c>array&lt;T&gt;</c>: fixed-length mutable element. Backed by
/// <c>object[]</c>; size is pinned at construction (no <c>add</c>/<c>remove</c>).
///
/// Distinct from <c>MutableFunnyList</c> (growable) and the ee-mode
/// <see cref="NFun.Runtime.Arrays.ImmutableFunnyArray"/> (covariant, no <c>SetAt</c>).
/// Per Stage 0 hierarchy <c>array</c> sits above <c>list</c> in the lattice —
/// every <c>list</c> can flow into an <c>array</c> parameter via subtype edges,
/// but writing through an <c>array</c> handle doesn't grow it.
/// </summary>
public class MutableFunnyArray : IFunnyMutableArray {
    public FunnyType ElementType { get; }
    private readonly object[] _items;
    private int? _hash;

    public MutableFunnyArray(FunnyType elementType, int length) {
        ElementType = elementType;
        _items = new object[length];
    }

    public MutableFunnyArray(FunnyType elementType, object[] items) {
        ElementType = elementType;
        _items = items;
    }

    public int Count => _items.Length;

    public IEnumerable<T> As<T>() => _items.Cast<T>();

    public object GetElementOrNull(int index) =>
        index < 0 || index >= _items.Length ? null : _items[index];

    public bool SetAt(int index, object value) {
        if (index < 0 || index >= _items.Length) return false;
        _items[index] = value;
        _hash = null;
        return true;
    }

    public IEnumerator<object> GetEnumerator() => ((IEnumerable<object>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public override bool Equals(object obj) => obj is MutableFunnyArray other && Equals(other);

    private bool Equals(MutableFunnyArray other) {
        if (Count != other.Count) return false;
        for (int i = 0; i < _items.Length; i++) {
            var a = _items[i];
            var b = other._items[i];
            if (a == null) {
                if (b != null) return false;
                continue;
            }
            if (!a.Equals(b)) return false;
        }
        return true;
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode() {
        if (_hash.HasValue) return _hash.Value;
        unchecked {
            int h = Count * 397;
            foreach (var v in _items)
                h = (h * 397) ^ (v?.GetHashCode() ?? 0);
            _hash = h;
            return h;
        }
    }

    public override string ToString() {
        if (ElementType.BaseType == BaseFunnyType.Char) {
            var sb = new System.Text.StringBuilder(_items.Length);
            foreach (var c in _items) sb.Append((char)c);
            return sb.ToString();
        }
        return "[" + string.Join(",", _items) + "]";
    }
}
