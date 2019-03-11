using System;
using System.Collections.Generic;
using Funny.Interpritation.Nodes;
using Funny.Runtime;

namespace Funny.Interpritation
{
    public class VariableExpressionNode : IExpressionNode
    {
        public string Name { get; }

        public VariableExpressionNode(string name, VarType type = VarType.RealType)
        {
            Type = type;
            Name = name;
        }
        
        private object _value;
        public void SetValue(object value) => _value = value;
        
        public IEnumerable<IExpressionNode> Children {
            get { yield break;}
        }

        public VarType Type { get; private set; } 

        public bool IsOutput { get; set; } = false;
        
        public object Calc() => _value;
        private static int _count = 0;
        private readonly int uid = _count++;
        
        public override string ToString() => $"{Name}: {_value} uid: {uid}";

        public void SetType(VarType expressionType)
        {
            Type = expressionType;
            _value = null;
        }

        public void SetConvertedValue(object valueValue)
        {
            switch (Type)
            {
                case VarType.BoolType:
                    _value = Convert.ToBoolean(valueValue);
                    break;
                case VarType.IntType:
                    _value = Convert.ToInt32(valueValue);
                    break;
                case VarType.RealType:
                    _value = Convert.ToDouble(valueValue);
                    break;
                case VarType.TextType:
                    _value = valueValue?.ToString()??"";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}