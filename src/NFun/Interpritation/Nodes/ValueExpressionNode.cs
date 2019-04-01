using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class ValueExpressionNode: IExpressionNode
    {
        private readonly object _value;

        public ValueExpressionNode(string value)
        {
            Type = VarType.Text;
            _value = value;
        }
        public ValueExpressionNode(bool value)
        {
            Type = VarType.Bool;
            _value = value;
        }
        public ValueExpressionNode(int value)
        {
            Type = VarType.Int;
            _value = value;
        }
        public ValueExpressionNode(double value)
        {
            Type = VarType.Real;
            _value = value;
        }

        public VarType Type { get; }

        public object Calc() => _value;
    }
}