using System;
using NFun.Runtime;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class VariableExpressionNode: IExpressionNode
    {
        private readonly VariableSource _source;
        public string Name { get; }
        public Interval Interval { get; }
        public VariableExpressionNode(VariableSource source, Interval interval)
        {
            _source = source;
            Interval = interval;
        }

        public VarType Type => _source.Type;

        public bool IsOutput { get; set; } = false;
        
        public object Calc() => _source.Value;
        private static int _count = 0;
        private readonly int _uid = _count++;
        public override string ToString() => $"{Name}: {_source.Value} uid: {_uid}";
    }
    public class VariableExpressionNode__Old : IExpressionNode
    {
        public string Name { get; }
        public Interval Interval { get; }
        public VariableExpressionNode__Old(string name, VarType type, Interval interval)
        {
            Type = type;
            Interval = interval;
            Name = name;
        }
        
        private object _value;
        public void SetValue(object value) => _value = value;

        public VarType Type { get; private set; } 

        public bool IsOutput { get; set; } = false;
        
        public object Calc() => _value;
        private static int _count = 0;
        private readonly int _uid = _count++;
        
        public override string ToString() => $"{Name}: {_value} uid: {_uid}";

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