using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class ConstantExpressionNode: IExpressionNode
    {
        private readonly object _value;

        public ConstantExpressionNode(object objVal, VarType type, Interval interval)
        {
            _value = objVal;
            Interval = interval;
            Type = type;
        }

        public VarType Type { get; }
        public Interval Interval { get; }
        public object Calc() => _value;
        public void Apply(IExpressionNodeVisitor visitor) 
            => visitor.Visit(this,_value);
    }
}