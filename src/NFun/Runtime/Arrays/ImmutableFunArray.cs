using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime.Arrays
{
    public class ImmutableFunArray: IFunArray
    {
        public static ImmutableFunArray By(IEnumerable<object> values) 
            => new ImmutableFunArray(values.ToArray());

        private readonly Array _values;
        private int _hash = 0;
      
        public ImmutableFunArray(Array values)
        {
            _values = values;
            Count = _values.Length;
        }
        public ImmutableFunArray(params ImmutableFunArray[] values)
        {
            _values = values;
            Count = _values.Length;
        }
        
        public int Count { get; }
        public Array Values => _values;
        public IEnumerable<T> As<T>()
        {
            foreach (var value in _values)
            {
                yield return (T) value;
            }
        }

        public Array ClrArray => _values;

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var t in _values)
                yield return t;
        }

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public IFunArray Slice(int? startIndex, int? endIndex, int? step) =>
            ArrayTools.SliceToImmutable(_values, startIndex, endIndex, step);

        public object GetElementOrNull(int index) => index >= _values.Length ? null : _values.GetValue(index);

        public bool IsEquivalent(IFunArray array) => TypeHelper.AreEquivalent(this, array);

        public override bool Equals(object obj)
        {
            if (obj is ImmutableFunArray f)
            {
                return Equals(f);
            }
            return false;
        }

        private bool Equals(ImmutableFunArray other)
        {
            if (Count != other.Count)
                return false;
            for (int i = 0; i < _values.Length; i++)
            {
                if (!_values.GetValue(i).Equals(other._values.GetValue(i)))
                    return false;
            }
            return true;
        }
        
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            if (_hash != 0)
                return _hash;
            unchecked
            {
                _hash = Count * 397;
                foreach (var value in _values)
                {
                    _hash = (_hash * 397) ^ value.GetHashCode();
                }
                return _hash;
            }
        }

        public override string ToString() 
            => $"Arr[{string.Join(",", _values.Cast<object>())}]";
    }
}