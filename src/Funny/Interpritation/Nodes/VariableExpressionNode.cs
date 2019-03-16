using System;
using System.Collections.Generic;

namespace Funny.Interpritation.Nodes
{
    public class VariableExpressionNode : IExpressionNode
    {
        public string Name { get; }

        public VariableExpressionNode(string name, VarType type)
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
            switch (Type.BaseType)
            {
                case PrimitiveVarType.BoolType:
                    _value = Convert.ToBoolean(valueValue);
                    break;
                case PrimitiveVarType.IntType:
                    _value = Convert.ToInt32(valueValue);
                    break;
                case PrimitiveVarType.RealType:
                    _value = Convert.ToDouble(valueValue);
                    break;
                case PrimitiveVarType.TextType:
                    _value = valueValue?.ToString()??"";
                    break;
                case PrimitiveVarType.Array:
                    _value = (double[]) valueValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}