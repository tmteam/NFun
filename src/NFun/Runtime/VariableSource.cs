using System;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Types;

namespace NFun.Runtime
{
    public class VariableSource
    {
        public readonly VarAttribute[] Attributes;
        public readonly string Name;

        public VariableSource(string name, VarType type, VarAttribute[] attributes = null)
        {
            Attributes = attributes??new VarAttribute[0];
            Name = name;
            Type = type;
            IsOutput = false;
        }
    
        public bool IsOutput { get; set; }
        public VarType Type { get; }
        public object Value { get; set; }

        public void SetConvertedValue(object valueValue)
        {
            //Unboxing special value
            if (valueValue is IFunConvertable c)
            {
                Value = c;
                return;
            }
            
            switch (Type.BaseType)
            {
                case BaseVarType.Bool:
                    Value = Convert.ToBoolean(valueValue);
                    break;
                case BaseVarType.Int32:
                    Value = Convert.ToInt32(valueValue);
                    break;
                case BaseVarType.Int64:
                    Value = Convert.ToInt64(valueValue);
                    break;
                case BaseVarType.Real:
                    Value = Convert.ToDouble(valueValue);
                    break;
                case BaseVarType.Text:
                    Value = valueValue?.ToString() ?? "";
                    break;
                case BaseVarType.ArrayOf:
                case BaseVarType.Any:
                    Value = valueValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}