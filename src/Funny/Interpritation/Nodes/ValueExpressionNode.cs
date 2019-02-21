using System.Collections.Generic;
using Funny.Runtime;

namespace Funny.Interpritation.Nodes
{
    public class ValueExpressionNode: IExpressionNode
    {
        private readonly object _value;

        public ValueExpressionNode(string value)
        {
            Type = VarType.TextType;
            _value = value;
        }
        public ValueExpressionNode(bool value)
        {
            Type = VarType.BoolType;
            _value = value;
        }
        public ValueExpressionNode(int value)
        {
            Type = VarType.IntType;
            _value = value;
        }
        public ValueExpressionNode(double value)
        {
            Type = VarType.RealType;
            _value = value;
        }

        public VarType Type { get; }

        public object Calc() => _value;
    }
}