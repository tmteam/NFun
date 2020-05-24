using System.Collections.Generic;
using NFun.Types;

namespace NFun.Interpritation
{
    public interface IConstantList
    {
        bool TryGetConstant(string id, out VarVal constant);
    }

    public class EmptyConstantList : IConstantList
    {
        public bool TryGetConstant(string id, out VarVal constant)
        {
            constant = default;
            return false;
        }
    }
    public class ConstantList: IConstantList
    {
        public ConstantList()
        {
            _dictionary = new Dictionary<string, VarVal>();
        }

        public ConstantList(int capacity)
        {
            _dictionary = new Dictionary<string, VarVal>(capacity);
        }

        readonly Dictionary<string, VarVal> _dictionary;
        public void AddConstant(VarVal constant)
        {
            _dictionary.Add(constant.Name, constant);
        }
        public bool TryGetConstant(string id, out VarVal constant)
        {
            return _dictionary.TryGetValue(id, out constant);
        }
    }
}