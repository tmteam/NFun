using System.Collections.Generic;
using Funny.Interpritation.Nodes;
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
        
        private object _value;
        public void SetValue(object value) => _value = value;
        
        public IEnumerable<IExpressionNode> Children {
            get { yield break;}
        }

        public VarType Type { get; private set; } = VarType.NumberType;

        public bool IsOutput { get; set; } = false;
        
        public object Calc() => _value;
        private static int _count = 0;
        private readonly int uid = _count++;
        
        public override string ToString()
        {
            return Name+": "+ _value+" uid: "+uid;
        }

        public void SetType(VarType expressionType)
        {
            Type = expressionType;
            _value = null;
        }
    }
}