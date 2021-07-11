using System;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Runtime
{
    public interface IFunnyVar
    {
        string Name { get; }
        VarAttribute[] Attributes { get; }
        FunnyType Type { get; }

        /// <summary>
        /// internal representation of value
        /// </summary>
        object FunnyValue { get; }

        bool IsOutput { get; }

        /// <summary>
        /// Converts clr value with default input converter and setup it to variable
        /// </summary>
        /// <param name="value"></param>
        void SetClrValue(object value);

        /// <summary>
        /// Converts current funnyValue with default output converter
        /// </summary>
        object GetClrValue();
    }

    internal class VariableSource : IFunnyVar
    {
        internal object InternalFunnyValue;

        private readonly FunnyVarAccess _access;

        internal static VariableSource CreateWithStrictTypeLabel(
            string name,
            FunnyType type,
            Interval typeSpecificationIntervalOrNull,
            FunnyVarAccess access,
            VarAttribute[] attributes = null)
            => new(name, type, typeSpecificationIntervalOrNull, access, attributes);

        internal static VariableSource CreateWithoutStrictTypeLabel(
            string name, FunnyType type, FunnyVarAccess access, VarAttribute[] attributes = null)
            => new(name, type, access, attributes);

        private VariableSource(
            string name,
            FunnyType type,
            Interval typeSpecificationIntervalOrNull,
            FunnyVarAccess access,
            VarAttribute[] attributes = null)
        {
            _access = access;
            InternalFunnyValue = type.GetDefaultValueOrNull();
            TypeSpecificationIntervalOrNull = typeSpecificationIntervalOrNull;
            Attributes = attributes ?? Array.Empty<VarAttribute>();
            Name = name;
            Type = type;
        }

        public bool IsOutput => _access.HasFlag(FunnyVarAccess.Output);


        private VariableSource(string name, FunnyType type, FunnyVarAccess access, VarAttribute[] attributes = null)
        {
            _access = access;
            InternalFunnyValue = type.GetDefaultValueOrNull();
            Attributes = attributes ?? Array.Empty<VarAttribute>();
            Name = name;
            Type = type;
        }

        public VarAttribute[] Attributes { get; }
        public string Name { get; }
        internal Interval? TypeSpecificationIntervalOrNull { get; }
        public FunnyType Type { get; }

        public object FunnyValue => InternalFunnyValue;

        public void SetClrValue(object value)
        {
            if (Type.BaseType.GetClrType() == value.GetType())
            {
                InternalFunnyValue = value;
                return;
            }

            switch (Type.BaseType)
            {
                case BaseFunnyType.ArrayOf:
                case BaseFunnyType.Struct:
                case BaseFunnyType.Any:
                    InternalFunnyValue = value;
                    break;
                case BaseFunnyType.Bool:
                    InternalFunnyValue = Convert.ToBoolean(value);
                    break;
                case BaseFunnyType.Int16:
                    InternalFunnyValue = Convert.ToInt16(value);
                    break;
                case BaseFunnyType.Int32:
                    InternalFunnyValue = Convert.ToInt32(value);
                    break;
                case BaseFunnyType.Int64:
                    InternalFunnyValue = Convert.ToInt64(value);
                    break;
                case BaseFunnyType.UInt8:
                    InternalFunnyValue = Convert.ToByte(value);
                    break;
                case BaseFunnyType.UInt16:
                    InternalFunnyValue = Convert.ToUInt16(value);
                    break;
                case BaseFunnyType.UInt32:
                    InternalFunnyValue = Convert.ToUInt32(value);
                    break;
                case BaseFunnyType.UInt64:
                    InternalFunnyValue = Convert.ToUInt64(value);
                    break;
                case BaseFunnyType.Real:
                    InternalFunnyValue = Convert.ToDouble(value);
                    break;
                case BaseFunnyType.Char:
                    InternalFunnyValue = value?.ToString() ?? "";
                    break;
                default:
                    throw new NotSupportedException($"type '{Type.BaseType}' is not supported as primitive type");
            }
        }

        public object GetClrValue() => FunnyTypeConverters.GetOutputConverter(Type).ToClrObject(InternalFunnyValue);
    }

    internal enum FunnyVarAccess
    {
        NoInfo = 0,

        /// <summary>
        /// Funny variable is input, so can be modified from the outside before calculation
        /// </summary>
        Input = 1 << 0,

        /// <summary>
        /// Funny variable is output so it can be considered as the result of the calculation
        /// </summary>
        Output = 1 << 1,
    }
}