using System;
using NFun.Types;

namespace NFun.Interpritation.Nodes
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
                case BaseVarType.Bool:
                    _value = Convert.ToBoolean(valueValue);
                    break;
                case BaseVarType.Int:
                    _value = Convert.ToInt32(valueValue);
                    break;
                case BaseVarType.Real:
                    _value = Convert.ToDouble(valueValue);
                    break;
                case BaseVarType.Text:
                    _value = valueValue?.ToString()??"";
                    break;
                case BaseVarType.ArrayOf:
                case BaseVarType.Any:
                    _value = valueValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}