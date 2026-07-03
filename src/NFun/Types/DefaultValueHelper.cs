using System;
using System.Collections.Generic;
using System.Net;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types; 

internal  static class DefaultValueHelper {
    static readonly Dictionary<BaseFunnyType, object> PrimitiveTypeMap = new() {
        // `any` is semantically equivalent to `any?` in NFun — any-typed slots can
        // hold none. `default(any)` returns FunnyNone.Instance, matching the rule
        // for Optional types. Previously: `new object()`, exposing a raw CLR
        // System.Object instance through the API. (MR9Bug1.)
        { BaseFunnyType.Any,    FunnyNone.Instance },
        { BaseFunnyType.Bool,   default(bool) },
        { BaseFunnyType.Char,   default(char) },
        { BaseFunnyType.Ip,     new IPAddress(new byte[]{0,0,0,0}) },
        { BaseFunnyType.Real,   default(double) },
        { BaseFunnyType.Float32, default(float) },
        { BaseFunnyType.Int8,   default(sbyte) },
        { BaseFunnyType.Int16,  default(Int16) },
        { BaseFunnyType.Int32,  default(Int32) },
        { BaseFunnyType.Int64,  default(Int64) },
        { BaseFunnyType.UInt8,  default(byte) },
        { BaseFunnyType.UInt16, default(UInt16) },
        { BaseFunnyType.UInt32, default(UInt32) },
        { BaseFunnyType.UInt64, default(UInt64) }
    };

    public static object GetDefaultFunnyValue(this FunnyType type, INamedTypeFieldRegistry namedTypes = null) {
        if (type.BaseType == BaseFunnyType.Custom)
            return type.CustomTypeDefinition.DefaultValue;
        if (type.IsPrimitive)
            return PrimitiveTypeMap[type.BaseType];

        if (type.BaseType == BaseFunnyType.ArrayOf)
        {
            if (type.ArrayTypeSpecification.FunnyType.BaseType == BaseFunnyType.Char)
                return TextFunnyArray.Empty;
            return new ImmutableFunnyArray(type.ArrayTypeSpecification.FunnyType);
        }

        // Stage C — Concretest(FixedArray)=FixedArray means types resolve to fixedArray<T>
        // including ee-mode contexts. `default(fixedArray<T>)` is an empty FixedFunnyArray.
        if (type.BaseType == BaseFunnyType.FixedArray)
            return new NFun.Runtime.Lists.FixedFunnyArray(
                type.FixedArrayTypeSpecification.FunnyType, Array.Empty<object>());

        // Symmetric defaults for the other lang-mode collection kinds.
        if (type.BaseType == BaseFunnyType.List)
            return new NFun.Runtime.Lists.MutableFunnyList(
                type.ListTypeSpecification.FunnyType, Array.Empty<object>());
        if (type.BaseType == BaseFunnyType.MutableArray)
            return new NFun.Runtime.Lists.MutableFunnyArray(
                type.MutableArrayTypeSpecification.FunnyType, Array.Empty<object>());
        if (type.BaseType == BaseFunnyType.Set)
            return new NFun.Runtime.Lists.MutableFunnySet(type.SetTypeSpecification.FunnyType);
        if (type.BaseType == BaseFunnyType.Map)
            return new NFun.Runtime.Lists.MutableFunnyMap(
                type.MapTypeSpecification.KeyType,
                type.MapTypeSpecification.ValueType);

        if (type.BaseType == BaseFunnyType.Struct)
        {
            var structValue = new FunnyStruct.FieldsDictionary(type.StructTypeSpecification.Count);

            foreach (var (fieldName, fieldType) in type.StructTypeSpecification)
                structValue.Add(fieldName, fieldType.GetDefaultFunnyValue(namedTypes));

            return new FunnyStruct(structValue);
        }

        // NamedStruct: look up its fields in the registry and build a FunnyStruct
        // with each field defaulted recursively (BugHunt-stmt #70). TicTypesConverter
        // preserves NamedStruct identity for fields of an enclosing named struct
        // (b.p stays as NamedStructOf(p) instead of inlining p's shape) — that's
        // semantically correct for runtime fit-checking, but means the default-value
        // walk also has to know the named type's shape.
        if (type.BaseType == BaseFunnyType.NamedStruct
            && namedTypes != null
            && namedTypes.TryGetFields(type.NamedStructTypeName, out var fields))
        {
            var sv = new FunnyStruct.FieldsDictionary(fields.Length);
            foreach (var (fieldName, fieldType) in fields)
                sv.Add(fieldName, fieldType.GetDefaultFunnyValue(namedTypes));
            return new FunnyStruct(sv);
        }

        if (type.BaseType == BaseFunnyType.Fun)
            return new DefaultHiOrderFunction(type.FunTypeSpecification.Output, type.FunTypeSpecification.Output);

        if (type.BaseType == BaseFunnyType.Optional)
            return FunnyNone.Instance;

        throw new NotSupportedException($"Type {type} has no default value");
    }

    class DefaultHiOrderFunction : FunctionWithManyArguments {
        private readonly object _returnValue;

        public DefaultHiOrderFunction(FunnyType returnType, params FunnyType[] argTypes) : base("default", returnType, argTypes) => _returnValue = returnType.GetDefaultFunnyValue();
        public override object Calc(object[] args) => _returnValue;
    }
}