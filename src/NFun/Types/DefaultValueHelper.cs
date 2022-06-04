using System;
using System.Collections.Generic;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types; 

internal  static class DefaultValueHelper {
    static readonly Dictionary<BaseFunnyType, object> PrimitiveTypeMap = new() {
        { BaseFunnyType.Any, new object() },
        { BaseFunnyType.Bool, default(bool) },
        { BaseFunnyType.Char, default(char) },
        { BaseFunnyType.Real, default(double) },
        { BaseFunnyType.Int16, default(Int16) },
        { BaseFunnyType.Int32, default(Int32) },
        { BaseFunnyType.Int64, default(Int64) },
        { BaseFunnyType.UInt8, default(byte) },
        { BaseFunnyType.UInt16, default(UInt16) },
        { BaseFunnyType.UInt32, default(UInt32) },
        { BaseFunnyType.UInt64, default(UInt64) }
    };

    public static object GetDefaultFunnyValue(this FunnyType type) {
        if (type.IsPrimitive)
            return PrimitiveTypeMap[type.BaseType];
        
        if (type.BaseType == BaseFunnyType.ArrayOf)
        {
            if (type.ArrayTypeSpecification.FunnyType.BaseType == BaseFunnyType.Char)
                return TextFunnyArray.Empty;
            return new ImmutableFunnyArray(type.ArrayTypeSpecification.FunnyType);
        }

        if (type.BaseType == BaseFunnyType.Struct)
        {
            var structValue = new FunnyStruct.FieldsDictionary(type.StructTypeSpecification.Count);

            foreach (var (fieldName, fieldType) in type.StructTypeSpecification)
                structValue.Add(fieldName, fieldType.GetDefaultFunnyValue());

            return new FunnyStruct(structValue);
        }

        if (type.BaseType == BaseFunnyType.Fun)
            return new DefaultHiOrderFunction(type.FunTypeSpecification.Output, type.FunTypeSpecification.Output);

        throw new NotSupportedException($"Type {type} has no default value");
    }

    class DefaultHiOrderFunction : FunctionWithManyArguments {
        private readonly object _returnValue;

        public DefaultHiOrderFunction(FunnyType returnType, params FunnyType[] argTypes) : base("default", returnType, argTypes) {
            _returnValue = returnType.GetDefaultFunnyValue();
        }
        public override object Calc(object[] args) => _returnValue;
    }
}