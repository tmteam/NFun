using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class ValueExpressionNode: IExpressionNode
    {
        private readonly object _value;

        public ValueExpressionNode(string value, Interval interval)
        {
            Type = VarType.Text;
            _value = value;
            Interval = interval;
        }
        public ValueExpressionNode(bool value, Interval interval)
        {
            Type = VarType.Bool;
            _value = value;
            Interval = interval;
        }
        public ValueExpressionNode(int value, Interval interval)
        {
            Type = VarType.Int;
            _value = value;
            Interval = interval;
        }
        public ValueExpressionNode(double value, Interval interval)
        {
            Type = VarType.Real;
            _value = value;
            Interval = interval;
        }

        public VarType Type { get; }
        public Interval Interval { get; }
        public object Calc() => _value;
    }
}