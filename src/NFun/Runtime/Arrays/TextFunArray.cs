using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NFun.Types;

namespace NFun.Runtime.Arrays
{
    public class TextFunArray : IFunArray, IComparable
    {
        public static readonly TextFunArray Empty = new("");

        private readonly string _text;
        public TextFunArray(string text) => _text = text;
        public IEnumerator<object> GetEnumerator() => new FunCharEnumerator(_text.GetEnumerator());

        private class FunCharEnumerator : IEnumerator<object>
        {
            private readonly CharEnumerator _enumerator;
            public FunCharEnumerator(CharEnumerator enumerator)
            {
                _enumerator = enumerator;
            }
            public bool MoveNext() => _enumerator.MoveNext();
            public void Reset() => _enumerator.Reset();
            public object Current => _enumerator.Current;
            object IEnumerator.Current => Current;
            public void Dispose() => _enumerator.MoveNext();
        }

        IEnumerator IEnumerable.GetEnumerator() => _text.GetEnumerator();

        public IFunArray Slice(int? startIndex, int? endIndex, int? step)
        {
            if (endIndex == int.MaxValue)
                endIndex = null;

            if (startIndex > _text.Length - 1)
                return Empty;

            if (step.HasValue)
            {
                var sb = new StringBuilder((int)Math.Ceiling((double)_text.Length / step.Value));
             

                for (int i = startIndex??0; i <= (endIndex ??(_text.Length-1)); i+=step.Value)
                {
                    sb.Append(_text[i]);
                }
                return new TextFunArray(sb.ToString());
            }

            string str;
            if (!startIndex.HasValue)
            {
                if (!endIndex.HasValue)
                    return this;

                str = _text[..endIndex.Value];
            }
            else
            {
                int len;
                if (endIndex.HasValue)
                    len = endIndex.Value+1 - startIndex.Value;
                else
                    len = _text.Length  - startIndex.Value;
                if (startIndex.Value + len > _text.Length)
                    len = _text.Length - startIndex.Value;
                str = _text.Substring(startIndex.Value, len);
            }
            return new TextFunArray(str);
        }

        public object GetElementOrNull(int index)
        {
            if (index < 0)
                return null;
            if (_text.Length > index)
                return _text[index];
            else
                return null;
        }
        
        public IEnumerable<T> As<T>()
        {
            if (typeof(T) == typeof(char))
                return _text as IEnumerable<T>;
            throw new InvalidCastException($"Cannot cast Text to {typeof(T).Name}[]");
        }

        public Array ClrArray => _text.ToCharArray();
        public string ToText() => _text;
        public FunnyType ElementType => FunnyType.Char;
        public int Count => _text.Length;
        public override string ToString() => _text;
        public int CompareTo(object obj)
        {
            if (obj is TextFunArray t2)
                return string.Compare(_text, t2._text, StringComparison.Ordinal);
            return 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is TextFunArray t)
                return t._text == _text;
            return false;
        }
    }
}