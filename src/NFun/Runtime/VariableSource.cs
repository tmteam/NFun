using System;
using NFun.Parsing;
using NFun.Types;

namespace NFun.Runtime
{
    public class VariableSource
    {
        public readonly string Name;

        public VariableSource(VariableInfo info)
        {
            Name = info.Id;
            Type = info.Type;
            IsOutput = false;
        }
        public VariableSource(string name, VarType type)
        {
            Name = name;
            Type = type;
            IsOutput = false;
        }
        public VariableSource(string name)
        {
            Name = name;
            IsOutput = false;
        }
        public bool IsOutput { get; set; }
        public VarType Type { get; set; }
        public object Value { get; set; }
        
        public void SetConvertedValue(object valueValue)
        {
            switch (Type.BaseType)
            {
                case BaseVarType.Bool:
                    Value = Convert.ToBoolean(valueValue);
                    break;
                case BaseVarType.Int:
                    Value = Convert.ToInt32(valueValue);
                    break;
                case BaseVarType.Real:
                    Value = Convert.ToDouble(valueValue);
                    break;
                case BaseVarType.Text:
                    Value = valueValue?.ToString()??"";
                    break;
                case BaseVarType.ArrayOf:
                case BaseVarType.Any:
                    Value = valueValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetType(VarType type)
        {
            Type = type;
        }
    }
}