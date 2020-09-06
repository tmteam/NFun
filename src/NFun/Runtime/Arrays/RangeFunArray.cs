using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Runtime.Arrays
{
    public class RangeFunArray : IFunArray
    {
        private readonly int _start;
        private readonly int _count;

        public RangeFunArray(int start, int count)
        {
            _start = start;
            _count = count;
        }
        public IEnumerator<object> GetEnumerator() => new RangeEnumerator(_start, _count, 1);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _count;

        public IFunArray Slice(int? startIndex, int? endIndex, int? step) =>
            ArrayTools.SliceToImmutable(ClrArray, startIndex, endIndex, step);

        public object GetElementOrNull(int index)
        {
            if (index > _count)
                return null;
            return _start + index;
        }

        public bool IsEquivalent(IFunArray array)
        {
            if (_count != array.Count)
                return false;
            for (int i = 0; i < _count; i++)
            {
                if (!array.GetElementOrNull(i).Equals(_start + i))
                    return false;
            }
            return true;
        }

        public IEnumerable<T> As<T>() => this.Cast<T>();

        private int[] _clrArray;
        public Array ClrArray
        {
            get
            {
                if (_clrArray == null)
                {
                    _clrArray = new int[_count];
                    for (int i = 0; i < _count; i++)
                    {
                        _clrArray[i] = _start + i;
                    }
                }
                return _clrArray;
            }
        }
    }
}