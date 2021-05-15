using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpritation
{
    public interface IConstantList
    {
        bool TryGetConstant(string id, out VarVal constant);
        IConstantList CloneWith(params VarVal[] values);
    }

    public class EmptyConstantList : IConstantList
    {
        public static EmptyConstantList Instance = new ();
        public bool TryGetConstant(string id, out VarVal constant)
        {
            constant = default;
            return false;
        }

        public IConstantList CloneWith(params VarVal[] values)
        {
            ConstantList list = new ConstantList(values.Length);
            foreach (var val in values) 
                list.AddConstant(val);
            return list;
        }
    }

    public class ConstantList: IConstantList
    {
        public ConstantList()
        {
            _dictionary = new Dictionary<string, VarVal>();
        }
        private ConstantList( Dictionary<string, VarVal> dictionary)
        {
            _dictionary = dictionary;
        }
        public ConstantList(int capacity)
        {
            _dictionary = new Dictionary<string, VarVal>(capacity);
        }

        readonly Dictionary<string, VarVal> _dictionary;
        public void AddConstant(VarVal constant) => _dictionary.Add(constant.Name, constant);
        public bool TryGetConstant(string id, out VarVal constant) => _dictionary.TryGetValue(id, out constant);

        public IConstantList CloneWith(params VarVal[] values)
        {
            var clone = new Dictionary<string, VarVal>(_dictionary);
            foreach (var val in values) 
                clone.Add(val.Name, val);
            return new ConstantList(clone);
        }
    }
}