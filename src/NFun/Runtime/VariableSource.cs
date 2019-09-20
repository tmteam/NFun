using System;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public class VariableSource
    {
        public bool IsStrictTyped { get; }

        public readonly VarAttribute[] Attributes;
        public readonly string Name;

        public readonly Interval? TypeSpecificationIntervalOrNull;
        public static VariableSource CreateWithStrictTypeLabel( string name, 
            VarType type, 
            Interval typeSpecificationIntervalOrNull, 
            VarAttribute[] attributes = null)
            => new VariableSource(name, type, typeSpecificationIntervalOrNull, attributes);

        public static VariableSource CreateWithoutStrictTypeLabel(
            string name, VarType type,  VarAttribute[] attributes = null)
            => new VariableSource(name, type, attributes);
        
        private VariableSource(
            string name, 
            VarType type, 
            Interval typeSpecificationIntervalOrNull, 
            VarAttribute[] attributes = null)
        {
            IsStrictTyped = true;
            TypeSpecificationIntervalOrNull = typeSpecificationIntervalOrNull;
            Attributes = attributes ?? new VarAttribute[0];
            Name = name;
            Type = type;
            IsOutput = false;
        }
        public VariableSource(string name, VarType type,  VarAttribute[] attributes = null)
        {
            IsStrictTyped = false;
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
                case BaseVarType.Int16:
                    Value = Convert.ToInt16(valueValue);
                    break;
                case BaseVarType.Int32:
                    Value = Convert.ToInt32(valueValue);
                    break;
                case BaseVarType.Int64:
                    Value = Convert.ToInt64(valueValue);
                    break;
                case BaseVarType.UInt8:
                    Value = Convert.ToByte(valueValue);
                    break;
                case BaseVarType.UInt16:
                    Value = Convert.ToUInt16(valueValue);
                    break;
                case BaseVarType.UInt32:
                    Value = Convert.ToUInt32(valueValue);
                    break;
                case BaseVarType.UInt64:
                    Value = Convert.ToUInt64(valueValue);
                    break;
                case BaseVarType.Real:
                    Value = Convert.ToDouble(valueValue);
                    break;
                case BaseVarType.Char:
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