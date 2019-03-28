using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Types;

namespace Funny.Runtime
{
    public interface IFunArray: IEnumerable<object>
    {
        VarType ElementType { get; }
        int Count { get; }
        IFunArray Slice(int? startIndex, int? endIndex, int? step);
        object GetElementOrNull(int index);
        bool IsEquivalent(IFunArray array);
    }
    
    public class FunArray: IFunArray
    {
        private readonly object[] _values;
        public static FunArray Empty(VarType elementType) => new FunArray(new object[0], elementType);
        public FunArray(object[] values, VarType elementType)
        {
            _values = values;
            ElementType = elementType;
            Count = _values.Length;
        }
        public FunArray( VarType elementType, params FunArray[] values)
        {
            _values = values;
            ElementType = elementType;
            Count = _values.Length;
        }

        public IEnumerator<object> GetEnumerator()
        {
            foreach (var t in _values)
                yield return t;
        }

        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        public VarType ElementType { get; }
        public int Count { get; }
        public IFunArray Slice(int? startIndex, int? endIndex, int? step)
        {
            if (Count == 0)
                return this;
            
            var start = startIndex ?? 0;
            if(start > Count-1) 
                return Empty(ElementType);
            
            var end = Count - 1;
            if(endIndex.HasValue)
                end = endIndex.Value >= Count ? Count - 1 : endIndex.Value;
            object[] newArr;
            if (step == null || step == 1)
            {
                var size = end - start + 1;
                newArr = new object[size];
                Array.Copy(_values, start, newArr,  0, size);
            }
            else
            {    
                var size = (int)Math.Floor((end - start) / (double)step)+1;
                newArr = new object[size];
                for (int i = start, index = 0; i <= end; i+= step.Value, index++)
                    newArr[index] = _values[i];
            }
            return new FunArray(newArr, ElementType);
        }

        public object GetElementOrNull(int index)
        {
            if (index >= Count)
                return null;
            return _values[index];
        }

        public bool IsEquivalent(IFunArray array)
        {
            if (Count != array.Count)
                return false;
            if (ElementType != array.ElementType)
                return false;
            if (ElementType.BaseType == BaseVarType.ArrayOf)
            {
                for (int i = 0; i < Count; i++)
                {
                    if(!((IFunArray) array.GetElementOrNull(i)).IsEquivalent((IFunArray) _values[i]))
                        return false;
                }
                return true;
            }
            else
                return array.SequenceEqual(this);

        }
    }
}