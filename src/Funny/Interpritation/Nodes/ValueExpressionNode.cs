using System.Collections.Generic;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public class ValueExpressionNode: IExpressionNode
    {
        private readonly object _value;

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
            Type = VarType.NumberType;
            _value = value;
        }

        public VarType Type { get; }

        public IEnumerable<IExpressionNode> Children {
            get { yield break;}
        }
        public object Calc() => _value;
    }
}