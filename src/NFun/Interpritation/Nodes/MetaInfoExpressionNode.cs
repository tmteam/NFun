using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class MetaInfoExpressionNode : IExpressionNode
    {
        private readonly VariableDictionary _dictionary;
        private readonly string _id;

        public MetaInfoExpressionNode(VariableDictionary dictionary, string id, Interval interval)
        {
            _dictionary = dictionary;
            _id = id;
            Interval = interval;
        }

        public Interval Interval { get; }
        public VarType Type => VarType.Anything;
        public object Calc() => _dictionary.GetSourceOrNull(_id);
    }
}