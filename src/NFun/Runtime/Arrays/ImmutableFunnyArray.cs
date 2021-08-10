using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime.Arrays {
    public class ImmutableFunnyArray: IFunnyArray {
        public FunnyType ElementType { get; }
        private int _hash = 0;
      
        public ImmutableFunnyArray(bool[] values):this(values, FunnyType.Bool) {}
        public ImmutableFunnyArray(byte[] values):this(values, FunnyType.UInt8) {}
        public ImmutableFunnyArray(ushort[] values):this(values, FunnyType.UInt16) {}
        public ImmutableFunnyArray(uint[] values):this(values, FunnyType.UInt32) {}
        public ImmutableFunnyArray(ulong[] values):this(values, FunnyType.UInt64) {}
        public ImmutableFunnyArray(short[] values):this(values, FunnyType.Int16) {}
        public ImmutableFunnyArray(int[] values):this(values, FunnyType.Int32) {}
        public ImmutableFunnyArray(long[] values):this(values, FunnyType.Int64) {}
        public ImmutableFunnyArray(double[] values) : this(values, FunnyType.Real) { }

        public ImmutableFunnyArray(Array values, FunnyType elementType)
        {
            ElementType = elementType;
            ClrArray = values;
            Count = ClrArray.Length;
        }
        
        public ImmutableFunnyArray(FunnyType elementType, params ImmutableFunnyArray[] values)
        {
            ClrArray = values;
            Count = ClrArray.Length;
            ElementType = elementType;
        }
        
        public int Count { get; }
        public IEnumerable<T> As<T>() => ClrArray.Cast<T>();

        public Array ClrArray { get; private set; }

        public string ToText()
        {
            if (ClrArray is char[] str)
                return new string(str);
            if (ElementType == FunnyType.Char)
            {
                var newArray = new char[ClrArray.Length];
                ClrArray.CopyTo(newArray,0);
                ClrArray = newArray;
                return new string(newArray);
            }

            return FunnyArrayTools.JoinElementsToFunString(ClrArray);
        }

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var t in ClrArray)
                yield return t;
        }

        IEnumerator IEnumerable.GetEnumerator() => ClrArray.GetEnumerator();

        public IFunnyArray Slice(int? startIndex, int? endIndex, int? step) =>
            FunnyArrayTools.SliceToImmutable(ClrArray, ElementType, startIndex, endIndex, step);

        public object GetElementOrNull(int index) => index >= ClrArray.Length ? null : ClrArray.GetValue(index);

        public bool IsEquivalent(IFunnyArray array) => TypeHelper.AreEquivalent(this, array);

        public override bool Equals(object obj)
        {
            if (obj is ImmutableFunnyArray f)
            {
                return Equals(f);
            }
            return false;
        }

        private bool Equals(ImmutableFunnyArray other)
        {
            if (Count != other.Count)
                return false;
            for (int i = 0; i < ClrArray.Length; i++)
            {
                if (!ClrArray.GetValue(i).Equals(other.ClrArray.GetValue(i)))
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
                foreach (var value in ClrArray)
                {
                    _hash = (_hash * 397) ^ value.GetHashCode();
                }
                return _hash;
            }
        }

        public override string ToString() 
            => $"Arr[{string.Join(",", ClrArray.Cast<object>())}]";
    }
}