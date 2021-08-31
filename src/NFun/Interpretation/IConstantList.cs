using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpretation
{
    internal interface IConstantList
    {
        bool TryGetConstant(string id, out ConstantValueAndType constant);
        IConstantList CloneWith(params (string id, object value)[] values);
    }

    internal class EmptyConstantList : IConstantList
    {
        public static readonly EmptyConstantList Instance = new();
        private EmptyConstantList()
        {
            
        }
        public bool TryGetConstant(string id, out ConstantValueAndType constant)
        {
            constant = default;
            return false;
        }

        public IConstantList CloneWith((string id, object value)[] values)
            => new ConstantList(values);
    }

    internal  class ConstantList : IConstantList
    {
        public ConstantList()
        {
            _dictionary = new Dictionary<string, ConstantValueAndType>();
        }

        private ConstantList(Dictionary<string, ConstantValueAndType> dictionary)
        {
            _dictionary = dictionary;
        }

        internal ConstantList((string id, object value)[] items)
        {
            _dictionary = new Dictionary<string, ConstantValueAndType>(items.Length);
            foreach (var (id, value) in items)
            {
                var converter = FunnyTypeConverters.GetInputConverter(value.GetType());
                _dictionary.Add(id, new ConstantValueAndType(converter.ToFunObject(value), converter.FunnyType));
            }
        }

        readonly Dictionary<string, ConstantValueAndType> _dictionary;

        public void AddConstant(string id, object val)
        {
            //constants are readonly so we need to use input converter
            var converter = FunnyTypeConverters.GetInputConverter(val.GetType());
            _dictionary.Add(id, new ConstantValueAndType(converter.ToFunObject(val), converter.FunnyType));
        }

        public bool TryGetConstant(string id, out ConstantValueAndType constant) => _dictionary.TryGetValue(id, out constant);

        public IConstantList CloneWith(params (string id, object value)[] items)
        {
            var clone = new Dictionary<string, ConstantValueAndType>(_dictionary);

            foreach (var (id, value) in items)
            {
                var converter = FunnyTypeConverters.GetInputConverter(value.GetType());
                clone.Add(id, new ConstantValueAndType(converter.ToFunObject(value), converter.FunnyType));
            }

            return new ConstantList(clone);
        }
    }
}