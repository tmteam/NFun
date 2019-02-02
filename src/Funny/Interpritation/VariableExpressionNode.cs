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

        public HashSet<string> usedInOutputs = new HashSet<string>();
        
        public void AddEquatationName(string val)
        {
            val = val.ToLower();
            if (!usedInOutputs.Contains(val))
                usedInOutputs.Add(val);
        }
        
        public double Calc() => _value;
    }
}