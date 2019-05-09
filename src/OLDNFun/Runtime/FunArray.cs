using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NFun.Types;

namespace NFun.Runtime
{
    public interface IFunArray: IEnumerable<object>
    {
        int Count { get; }
        IFunArray Slice(int? startIndex, int? endIndex, int? step);
        object GetElementOrNull(int index);
        bool IsEquivalent(IFunArray array);
        IEnumerable<T> As<T>();

    }
    
    public class FunArray: IFunArray
    {
        private readonly Array _values;
        public static FunArray Empty => new FunArray(new object[0]);

        public IEnumerable<T> As<T>()
        {
            foreach (var value in _values)
            {
                yield return (T) value;
            }
        }

        public static FunArray By(IEnumerable<object> values)
        {
            return new FunArray(values.ToArray());
        }
        public FunArray(Array values)
        {
            _values = values;
            Count = _values.Length;
        }
        public FunArray(params FunArray[] values)
        {
            _values = values;
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
                return Empty;
            
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
                    newArr[index] = _values.GetValue(i);
            }
            return new FunArray(newArr);
        }

        public object GetElementOrNull(int index)
        {
            if (index >= Count)
                return null;
            return _values.GetValue(index);
        }

        public bool IsEquivalent(IFunArray array)
        {
            if (Count != array.Count)
                return false;
            if (Count == 0)
                return true;
            
            if (GetElementOrNull(0) is IFunArray)
            {
                for (int i = 0; i < Count; i++)
                {
                    var foreign = array.GetElementOrNull(i) ;
                    var origin = GetElementOrNull(i) ;
                    if (foreign is IFunArray f)
                    {
                        if (origin is IFunArray o)
                        {
                            if (!f.IsEquivalent(o))
                                return false;
                        }
                        else return false;
                    }
                    else if (!foreign.Equals(origin))
                        return false;
                }
                return true;
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!TypeHelper.AreEqual(GetElementOrNull(i), array.GetElementOrNull(i)))
                        return false;
                }
                return true;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is FunArray f)
            {
                return Equals(f);
            }
            return false;
        }

        protected bool Equals(FunArray other)
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

        
        private int _hash = 0;
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
        {
            return "Arr["+String.Join(",", _values.Cast<object>())+"]" ;
        }
    }
}