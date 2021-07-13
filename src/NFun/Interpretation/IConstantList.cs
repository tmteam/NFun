using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpretation
{
    public interface IConstantList
    {
        bool TryGetConstant(string id, out VarVal constant);
        IConstantList CloneWith(params (string id, object value)[] values);
    }

    public class EmptyConstantList : IConstantList
    {
        public static readonly EmptyConstantList Instance = new();

        public bool TryGetConstant(string id, out VarVal constant)
        {
            constant = default;
            return false;
        }

        public IConstantList CloneWith((string id, object value)[] values)
            => new ConstantList(values);
    }

    public class ConstantList : IConstantList
    {
        public ConstantList()
        {
            _dictionary = new Dictionary<string, VarVal>();
        }

        private ConstantList(Dictionary<string, VarVal> dictionary)
        {
            _dictionary = dictionary;
        }

        internal ConstantList((string id, object value)[] items)
        {
            _dictionary = new Dictionary<string, VarVal>(items.Length);
            foreach (var item in items)
            {
                var converter = FunnyTypeConverters.GetInputConverter(item.value.GetType());
                _dictionary.Add(item.id, new VarVal(item.id, converter.ToFunObject(item.value), converter.FunnyType));
            }
        }

        readonly Dictionary<string, VarVal> _dictionary;

        public void AddConstant(string id, object val)
        {
            //constants are readonly so we need to use input converter
            var converter = FunnyTypeConverters.GetInputConverter(val.GetType());
            _dictionary.Add(id, new VarVal(id, converter.ToFunObject(val), converter.FunnyType));
        }

        public bool TryGetConstant(string id, out VarVal constant) => _dictionary.TryGetValue(id, out constant);

        public IConstantList CloneWith(params (string id, object value)[] items)
        {
            var clone = new Dictionary<string, VarVal>(_dictionary);

            foreach (var item in items)
            {
                var converter = FunnyTypeConverters.GetInputConverter(item.value.GetType());
                clone.Add(item.id, new VarVal(item.id, converter.ToFunObject(item.value), converter.FunnyType));
            }

            return new ConstantList(clone);
        }
    }
}