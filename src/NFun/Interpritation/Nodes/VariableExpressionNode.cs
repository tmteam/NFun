using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class VariableExpressionNode: IExpressionNode
    {
        private readonly VariableSource _source;
        public Interval Interval { get; }
        public VariableExpressionNode(VariableSource source, Interval interval)
        {
            _source = source;
            Interval = interval;
        }

        public VarType Type => _source.Type;
        
        public object Calc() => _source.Value;
        private static int _count = 0;
        private readonly int _uid = _count++;
        public override string ToString() => $"{_source.Name}: {_source.Value} uid: {_uid}";
    }
}