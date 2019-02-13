using System.Collections.Generic;
using Funny.Runtime;

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

        public VarType Type => VarType.NumberType;

        public HashSet<int> usedInOutputs = new HashSet<int>();
        public bool IsOutput { get; set; } = false;
        public void AddEquatationNum(int num)
        {
            if (!usedInOutputs.Contains(num))
                usedInOutputs.Add(num);
        }
        
        public object Calc() => _value;
        private static int _count = 0;
        private readonly int uid = _count++;
        public override string ToString()
        {
            return Name+": "+ _value+" uid: "+uid;
        }
    }
}