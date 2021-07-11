using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime.Arrays {
    public class ImmutableFunArray: IFunArray {
        public FunnyType ElementType { get; }
        private int _hash = 0;
      
        public ImmutableFunArray(bool[] values):this(values, FunnyType.Bool) {}
        public ImmutableFunArray(byte[] values):this(values, FunnyType.UInt8) {}
        public ImmutableFunArray(ushort[] values):this(values, FunnyType.UInt16) {}
        public ImmutableFunArray(uint[] values):this(values, FunnyType.UInt32) {}
        public ImmutableFunArray(ulong[] values):this(values, FunnyType.UInt64) {}
        public ImmutableFunArray(short[] values):this(values, FunnyType.Int16) {}
        public ImmutableFunArray(int[] values):this(values, FunnyType.Int32) {}
        public ImmutableFunArray(long[] values):this(values, FunnyType.Int64) {}
        public ImmutableFunArray(double[] values) : this(values, FunnyType.Real) { }

        public ImmutableFunArray(Array values, FunnyType elementType)
        {
            ElementType = elementType;
            ClrArray = values;
            Count = ClrArray.Length;
        }
        
        public ImmutableFunArray(FunnyType elementType, params ImmutableFunArray[] values)
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

            return ArrayTools.JoinElementsToFunString(ClrArray);
        }

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var t in ClrArray)
                yield return t;
        }

        IEnumerator IEnumerable.GetEnumerator() => ClrArray.GetEnumerator();

        public IFunArray Slice(int? startIndex, int? endIndex, int? step) =>
            ArrayTools.SliceToImmutable(ClrArray, ElementType, startIndex, endIndex, step);

        public object GetElementOrNull(int index) => index >= ClrArray.Length ? null : ClrArray.GetValue(index);

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