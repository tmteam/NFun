using System.Collections.Generic;

namespace Funny.Interpritation
{
    public class ValueExpressionNode: IExpressionNode
    {
        private readonly double _value;

        public ValueExpressionNode(double value)
        {
            _value = value;
        }

        public IEnumerable<IExpressionNode> Children {
            get { yield break;}
        }
        public double Calc() => _value;
    }
}