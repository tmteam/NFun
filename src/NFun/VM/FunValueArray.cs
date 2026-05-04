using System;
using System.Collections;
using System.Collections.Generic;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.VM;

/// <summary>
/// VM-native array backed by FunValue[]. Zero boxing for VM operations.
/// Implements IFunnyArray for compatibility with tree-walker functions.
/// </summary>
internal sealed class FunValueArray : IFunnyArray {
    private readonly FunValue[] _elements;
    private readonly FunnyType _elementType;

    public FunValueArray(FunValue[] elements, FunnyType elementType) {
        _elements = elements;
        _elementType = elementType;
    }

    // ── VM fast path (zero boxing) ──
    internal FunValue GetDirect(int i) => _elements[i];
    internal FunValue[] RawElements => _elements;

    // ── IFunnyArray implementation (boxes on demand for tree-walker compat) ──
    public int Count => _elements.Length;
    public FunnyType ElementType => _elementType;

    public object GetElementOrNull(int index) {
        if (index < 0 || index >= _elements.Length) return null;
        return _elements[index].Box(_elementType);
    }

    public IEnumerator<object> GetEnumerator() {
        for (int i = 0; i < _elements.Length; i++)
            yield return _elements[i].Box(_elementType);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public Array ClrArray {
        get {
            // Materialize boxed array on demand
            var arr = Array.CreateInstance(TypeToClr(_elementType.BaseType), _elements.Length);
            for (int i = 0; i < _elements.Length; i++)
                arr.SetValue(_elements[i].Box(_elementType), i);
            return arr;
        }
    }

    public IEnumerable<T> As<T>() {
        for (int i = 0; i < _elements.Length; i++)
            yield return (T)_elements[i].Box(_elementType);
    }

    public IFunnyArray Slice(int? startIndex, int? endIndex, int? step) {
        // Delegate to ImmutableFunnyArray for complex slicing
        var boxed = new object[_elements.Length];
        for (int i = 0; i < _elements.Length; i++)
            boxed[i] = _elements[i].Box(_elementType);
        return new ImmutableFunnyArray(boxed, _elementType).Slice(startIndex, endIndex, step);
    }

    private static Type TypeToClr(BaseFunnyType bt) => bt switch {
        BaseFunnyType.Int32 => typeof(int),
        BaseFunnyType.Int64 => typeof(long),
        BaseFunnyType.Real => typeof(double),
        BaseFunnyType.Bool => typeof(bool),
        BaseFunnyType.Char => typeof(char),
        BaseFunnyType.UInt8 => typeof(byte),
        BaseFunnyType.UInt16 => typeof(ushort),
        BaseFunnyType.UInt32 => typeof(uint),
        BaseFunnyType.UInt64 => typeof(ulong),
        BaseFunnyType.Int16 => typeof(short),
        _ => typeof(object),
    };

    public string ToText() {
        var sb = new System.Text.StringBuilder("[");
        for (int i = 0; i < _elements.Length; i++) {
            if (i > 0) sb.Append(", ");
            sb.Append(_elements[i].Box(_elementType));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
