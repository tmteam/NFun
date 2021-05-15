using System;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public class VariableSource
    {
        internal object InternalFunnyValue;

        public static VariableSource CreateWithStrictTypeLabel( string name, 
            VarType type, 
            Interval typeSpecificationInterval,
            bool isOutput,
            VarAttribute[] attributes = null)
            => new (name, type, typeSpecificationInterval, isOutput, attributes);

        public static VariableSource CreateWithoutStrictTypeLabel(
            string name, VarType type, bool isOutput,  VarAttribute[] attributes = null)
            => new(name, type, isOutput, attributes);
        
        private VariableSource(
            string name, 
            VarType type, 
            Interval? typeSpecificationIntervalOrNull, 
            bool isOutput,
            VarAttribute[] attributes = null)
        {
            IsOutput = isOutput;
            InternalFunnyValue = type.GetDefaultValueOrNull();
            IsStrictTyped = true;
            TypeSpecificationIntervalOrNull = typeSpecificationIntervalOrNull;
            Attributes = attributes ?? new VarAttribute[0];
            Name = name;
            Type = type;
        }
        
        private VariableSource(string name, VarType type,bool isOutput,  VarAttribute[] attributes = null)
        {
            IsOutput = isOutput;
            InternalFunnyValue = type.GetDefaultValueOrNull();
            IsStrictTyped = false;
            Attributes = attributes??new VarAttribute[0];
            Name = name;
            Type = type;
        }
        public bool IsStrictTyped { get; }
        public VarAttribute[] Attributes { get; }
        public string Name { get; }
        internal Interval? TypeSpecificationIntervalOrNull { get; }
        public bool IsOutput { get; }
        public VarType Type { get; }

        public object FunnyValue
        {
            get => InternalFunnyValue;
            set => InternalFunnyValue = value;
        }

        public VariableSource Fork() => new(Name, Type, TypeSpecificationIntervalOrNull, IsOutput, Attributes);

        public void SetClrValue(object valueValue)
        {
            if (Type.BaseType.GetClrType() == valueValue.GetType())
            {
                FunnyValue = valueValue;
                return;
            }
            switch (Type.BaseType)
            {
                case BaseVarType.ArrayOf:
                case BaseVarType.Struct:
                case BaseVarType.Any:
                    FunnyValue = valueValue;
                    break;
                case BaseVarType.Bool:
                    FunnyValue = Convert.ToBoolean(valueValue);
                    break;
                case BaseVarType.Int16:
                    FunnyValue = Convert.ToInt16(valueValue);
                    break;
                case BaseVarType.Int32:
                    FunnyValue = Convert.ToInt32(valueValue);
                    break;
                case BaseVarType.Int64:
                    FunnyValue = Convert.ToInt64(valueValue);
                    break;
                case BaseVarType.UInt8:
                    FunnyValue = Convert.ToByte(valueValue);
                    break;
                case BaseVarType.UInt16:
                    FunnyValue = Convert.ToUInt16(valueValue);
                    break;
                case BaseVarType.UInt32:
                    FunnyValue = Convert.ToUInt32(valueValue);
                    break;
                case BaseVarType.UInt64:
                    FunnyValue = Convert.ToUInt64(valueValue);
                    break;
                case BaseVarType.Real:
                    FunnyValue = Convert.ToDouble(valueValue);
                    break;
                case BaseVarType.Char:
                    FunnyValue = valueValue?.ToString() ?? "";
                    break;
                default:
                    throw new NotSupportedException($"type '{Type.BaseType}' is not supported as primitive type");
            }
        }
        
        
    }
}