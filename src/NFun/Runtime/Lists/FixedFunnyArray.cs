using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime.Lists;

/// <summary>
/// Default implementation of lang-mode <c>fixedArray&lt;T&gt;</c>. Immutable
/// after construction: no <c>SetAt</c>, no <c>add</c>/<c>remove</c>. The
/// <c>fixedArray(...)</c> factory is the canonical entry point; an empty
/// <c>fixedArray&lt;T&gt;</c> is the default value for the slot.
///
/// Distinct from <see cref="NFun.Runtime.Arrays.ImmutableFunnyArray"/> (the
/// legacy ee-mode covariant array): fixedArray is INVARIANT in element
/// (matches lang-mode collection semantics).
/// </summary>
public class FixedFunnyArray : IFunnyFixedArray {
    public FunnyType ElementType { get; }
    private readonly object[] _items;
    private int _hash;

    public FixedFunnyArray(FunnyType elementType, object[] items) {
        ElementType = elementType;
        _items = items;
    }

    /// <summary>Constructs an empty fixedArray of the given element type.</summary>
    public FixedFunnyArray(FunnyType elementType) : this(elementType, System.Array.Empty<object>()) { }

    public int Count => _items.Length;

    public IEnumerable<T> As<T>() => _items.Cast<T>();

    public object GetElementOrNull(int index) =>
        index < 0 || index >= _items.Length ? null : _items[index];

    public IEnumerator<object> GetEnumerator() => ((IEnumerable<object>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public override bool Equals(object obj) => obj is FixedFunnyArray other && Equals(other);

    private bool Equals(FixedFunnyArray other) {
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

    public override int GetHashCode() {
        // Immutable container — cache once, reuse forever. `_hash == 0` is a
        // valid value (empty fixedArray); use a separate computed flag wired
        // through the cache initialisation pattern used by ImmutableFunnyArray.
        if (_hash != 0) return _hash;
        unchecked {
            int h = Count * 397;
            foreach (var v in _items)
                h = (h * 397) ^ (v?.GetHashCode() ?? 0);
            _hash = h == 0 ? 1 : h; // ensure non-zero sentinel
            return _hash;
        }
    }

    public override string ToString() {
        // Stage C — render fixedArray<char> as a joined string (matches text semantics).
        if (ElementType.BaseType == BaseFunnyType.Char) {
            var sb = new System.Text.StringBuilder(_items.Length);
            foreach (var c in _items) sb.Append((char)c);
            return sb.ToString();
        }
        return "[" + string.Join(",", _items) + "]";
    }
}
