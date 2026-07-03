using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime.Lists;

/// <summary>
/// Default implementation of lang-mode <c>list&lt;T&gt;</c>. Wraps
/// <see cref="List{T}"/> as untyped <c>List&lt;object&gt;</c> — element-type
/// is carried separately as <see cref="ElementType"/>.
///
/// Equality is by value (element-wise) so that two list&lt;int&gt; values
/// with the same elements compare equal — consistent with
/// <see cref="NFun.Runtime.Arrays.ImmutableFunnyArray"/>.
///
/// Stage 2.2 exposes the read surface only; the underlying <see cref="_items"/>
/// is mutable so Stage 3 can wire <c>add/remove/clear</c> without changing this
/// type's identity.
/// </summary>
public class MutableFunnyList : IFunnyList {
    public FunnyType ElementType { get; }
    private readonly List<object> _items;
    private int? _hash;

    public MutableFunnyList(FunnyType elementType) {
        ElementType = elementType;
        _items = new List<object>();
    }

    public MutableFunnyList(FunnyType elementType, IEnumerable<object> items) {
        ElementType = elementType;
        _items = new List<object>(items);
    }

    public int Count => _items.Count;

    public IEnumerable<T> As<T>() => _items.Cast<T>();

    public object GetElementOrNull(int index) =>
        index < 0 || index >= _items.Count ? null : _items[index];

    /// <summary>Appends <paramref name="item"/> to the end. Invalidates the hash cache.</summary>
    public void Add(object item) {
        _items.Add(item);
        _hash = null;
    }

    /// <summary>Appends every element of <paramref name="items"/>. Invalidates the hash cache.</summary>
    public void AddAll(IEnumerable<object> items) {
        _items.AddRange(items);
        _hash = null;
    }

    /// <summary>
    /// Removes the first occurrence of <paramref name="item"/>. Returns true if
    /// found and removed, false otherwise. Element equality uses
    /// <see cref="object.Equals(object)"/> on the boxed CLR value (matches
    /// how <see cref="MutableFunnyList.Equals(object)"/> compares element-wise).
    /// </summary>
    public bool Remove(object item) {
        int idx = _items.FindIndex(e => Equals(e, item));
        if (idx < 0) return false;
        _items.RemoveAt(idx);
        _hash = null;
        return true;
    }

    /// <summary>
    /// Removes the element at <paramref name="index"/> and returns it.
    /// Out-of-range returns null (the caller-side <c>removeAt</c> built-in
    /// wraps null as the lang-mode optional <c>none</c>).
    /// </summary>
    public object RemoveAt(int index) {
        if (index < 0 || index >= _items.Count) return null;
        var removed = _items[index];
        _items.RemoveAt(index);
        _hash = null;
        return removed;
    }

    /// <summary>
    /// Removes the last element and returns it. Empty list returns null
    /// (lang-mode <c>none</c> at the optional wrapper).
    /// </summary>
    public object RemoveLast() {
        if (_items.Count == 0) return null;
        var removed = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        _hash = null;
        return removed;
    }

    /// <summary>Drops every element. Invalidates the hash cache.</summary>
    public void Clear() {
        if (_items.Count == 0) return;
        _items.Clear();
        _hash = null;
    }

    /// <summary>
    /// Replaces the element at <paramref name="index"/> with
    /// <paramref name="value"/>. Returns true on success, false when
    /// out-of-range. The caller-side <c>a[i] = v</c> wiring surfaces an
    /// out-of-range hit as a runtime exception (per spec §Indexed write).
    /// </summary>
    public bool SetAt(int index, object value) {
        if (index < 0 || index >= _items.Count) return false;
        _items[index] = value;
        _hash = null;
        return true;
    }

    public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public override bool Equals(object obj) => obj is MutableFunnyList other && Equals(other);

    private bool Equals(MutableFunnyList other) {
        if (Count != other.Count) return false;
        for (int i = 0; i < _items.Count; i++) {
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
        // Nullable cache — explicit "not computed" sentinel. Earlier `int _hash`
        // with `_hash != 0` treated a legitimately-zero hash (empty list:
        // `0 * 397 = 0`) as "not yet cached" and recomputed on every call.
        // Stage 3 mutators must reset to null on add/remove/clear/[i]=v.
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
            var sb = new System.Text.StringBuilder(_items.Count);
            foreach (var c in _items) sb.Append((char)c);
            return sb.ToString();
        }
        return "[" + string.Join(",", _items) + "]";
    }
}
