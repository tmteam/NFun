using System.Collections.Generic;

namespace Funny.Interpritation
{
    public class VariableExpressionNode : IExpressionNode
    {
        public string Name { get; }

        public VariableExpressionNode(string name)
        {
            Name = name;
        }

        private double _value;
        public void SetValue(double value) => _value = value;
        public IEnumerable<IExpressionNode> Children {
            get { yield break;}
        }
        public double Calc() => _value;
    }
}