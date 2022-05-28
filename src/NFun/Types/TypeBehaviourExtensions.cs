using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Runtime.Arrays;

namespace NFun.Types {

public static class TypeBehaviourExtensions {
    
    internal static object GetDefaultValueOrNullFor(this TypeBehaviour typeBehaviour, FunnyType type) {
        var defaultValue = typeBehaviour.GetDefaultPrimitiveValueOrNull(type.BaseType);
        if (defaultValue != null)
            return defaultValue;
        
        if (type.ArrayTypeSpecification == null)
            return null;
            
        var arr = type.ArrayTypeSpecification;
        if (arr.FunnyType.BaseType == BaseFunnyType.Char)
            return TextFunnyArray.Empty;
        return new ImmutableFunnyArray(Array.Empty<object>(), arr.FunnyType);
    }
    public static object ConvertInputOrThrow(this TypeBehaviour typeBehaviour, object clrValue, FunnyType resultFunnyType) {
        var clrFromType = clrValue.GetType();
        if (typeBehaviour.GetClrTypeFor(resultFunnyType.BaseType) == clrFromType)
            return clrValue;

        var converter = GetInputConverterFor(typeBehaviour, resultFunnyType, clrFromType);
        if (converter.FunnyType == resultFunnyType)
            return converter.ToFunObject(clrValue);

        //Special slow convertation
        return resultFunnyType.BaseType switch {
                   BaseFunnyType.Any                                        => converter.ToFunObject(clrValue),
                   BaseFunnyType.Bool                                       => Convert.ToBoolean(clrValue),
                   BaseFunnyType.Int16                                      => Convert.ToInt16(clrValue),
                   BaseFunnyType.Int32                                      => Convert.ToInt32(clrValue),
                   BaseFunnyType.Int64                                      => Convert.ToInt64(clrValue),
                   BaseFunnyType.UInt8                                      => Convert.ToByte(clrValue),
                   BaseFunnyType.UInt16                                     => Convert.ToUInt16(clrValue),
                   BaseFunnyType.UInt32                                     => Convert.ToUInt32(clrValue),
                   BaseFunnyType.UInt64                                     => Convert.ToUInt64(clrValue),
                   BaseFunnyType.Real when  typeBehaviour.DoubleIsReal => Convert.ToDouble(clrValue),
                   BaseFunnyType.Real                                       => Convert.ToDecimal(clrValue),
                   BaseFunnyType.Char                                       => clrValue.ToString(),
                   _                                                        => converter.ToFunObject(clrValue)
               };
    }

    public static IInputFunnyConverter GetInputConverterFor(this TypeBehaviour typeBehaviour, FunnyType funnyType, Type clrTypeOrNull) =>
        GetInputConverterFor(typeBehaviour, funnyType, clrTypeOrNull, 0);

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    private static IInputFunnyConverter GetInputConverterFor(
        TypeBehaviour typeBehaviour, FunnyType funnyType, Type clrTypeOrNull,
        int reqDeepthCheck) {
        if (reqDeepthCheck > 100)
            throw new ArgumentException("Too nested input object");

        if (funnyType.IsPrimitive)
        {
            if (clrTypeOrNull != null)
            {
                var byTypeConverter = typeBehaviour.GetPrimitiveInputConverterOrNull(clrTypeOrNull);
                if (byTypeConverter != null)
                    return byTypeConverter;
            }

            var converter = typeBehaviour.GetPrimitiveInputConverterOrNull(funnyType);
            if (converter != null) return converter;
        }

        if (funnyType.IsText)
            return new StringTypeInputFunnyConverter();

        if (funnyType.BaseType == BaseFunnyType.ArrayOf)
        {
            var elementType = clrTypeOrNull?.GetElementType();
            var elementConverter = GetInputConverterFor(typeBehaviour,
                // ReSharper disable once RedundantAssignment
                funnyType.ArrayTypeSpecification.FunnyType, elementType, reqDeepthCheck++);
            return new ClrArrayInputTypeFunnyConverter(elementConverter);
        }

        if (funnyType.BaseType != BaseFunnyType.Struct)
            return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);

        if (clrTypeOrNull == null || typeof(Dictionary<string, object>).IsAssignableFrom(clrTypeOrNull))
            return GetDynamicStructInputFunnyConverter(typeBehaviour, funnyType, reqDeepthCheck++);

        var properties = clrTypeOrNull.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (properties.Any())
        {
            var propertiesConverters = new (string, IInputFunnyConverter, PropertyInfo)[properties.Length];
            int readPropertiesCount = 0;
            foreach (var property in properties)
            {
                if (!property.HasPublicGetter())
                    continue;
                if (!funnyType.StructTypeSpecification.TryGetValue(property.Name, out var fieldDef))
                    continue;
                var propertyConverter = GetInputConverterFor(typeBehaviour, fieldDef, property.PropertyType, reqDeepthCheck++);
                propertiesConverters[readPropertiesCount] =
                    new ValueTuple<string, IInputFunnyConverter, PropertyInfo>(
                        property.Name.ToLower(),
                        propertyConverter, property);
                readPropertiesCount++;
            }

            return new StructTypeInputFunnyConverter(propertiesConverters, readPropertiesCount, funnyType);
        }

        return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);
    }

    public static IInputFunnyConverter GetInputConverterFor(this TypeBehaviour typeBehaviour, Type clrType) => GetInputConverterFor(typeBehaviour, clrType, 0);

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    private static IInputFunnyConverter GetInputConverterFor(TypeBehaviour typeBehaviour, Type clrType, int reqDeepthCheck) {
        if (reqDeepthCheck > 100)
            throw new ArgumentException("Too nested input object");

        if (clrType == typeof(string))
            return new StringTypeInputFunnyConverter();

        if (clrType.IsArray)
        {
            var elementType = clrType.GetElementType();
            // ReSharper disable once RedundantAssignment
            var elementConverter = GetInputConverterFor(typeBehaviour, elementType, reqDeepthCheck++);

            return new ClrArrayInputTypeFunnyConverter(elementConverter);
        }

        var converter = typeBehaviour.GetPrimitiveInputConverterOrNull(clrType);
        if (converter!=null)
            return converter;

        var properties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (properties.Any())
        {
            (string, IInputFunnyConverter, PropertyInfo)[] propertiesConverters =
                new (string, IInputFunnyConverter, PropertyInfo)[properties.Length];
            int readPropertiesCount = 0;
            foreach (var property in properties)
            {
                if (!property.HasPublicGetter())
                    continue;
                var propertyConverter = GetInputConverterFor(typeBehaviour, property.PropertyType, reqDeepthCheck++);
                propertiesConverters[readPropertiesCount] =
                    new ValueTuple<string, IInputFunnyConverter, PropertyInfo>(
                        property.Name.ToLower(),
                        propertyConverter, property);
                readPropertiesCount++;
            }

            return new StructTypeInputFunnyConverter(propertiesConverters, readPropertiesCount);
        }

        return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);
    }

    public static IOutputFunnyConverter GetOutputConverterFor(this TypeBehaviour typeBehaviour, Type clrType) => GetOutputConverterFor(typeBehaviour, clrType, 0);

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    private static IOutputFunnyConverter GetOutputConverterFor(TypeBehaviour typeBehaviour, Type clrType, int reqDeepthCheck) {
        if (reqDeepthCheck > 100)
            throw new ArgumentException("Too nested output object");

        if (clrType == typeof(string))
            return new StringOutputFunnyConverter();

        if (clrType.IsArray)
        {
            var elementType = clrType.GetElementType();
            // ReSharper disable once RedundantAssignment
            var elementConverter = GetOutputConverterFor(typeBehaviour, elementType, reqDeepthCheck++);

            return new ClrArrayOutputFunnyConverter(clrType, elementConverter);
        }

        var converter = typeBehaviour.GetPrimitiveOutputConverterOrNull(clrType);
        if (converter!=null) return converter;

        if (clrType == typeof(Dictionary<string, object>))
            return DynamicStructToDictionaryOutputFunnyConverter.Instance;
        if (clrType == typeof(IDictionary<string, object>))
            return DynamicStructToDictionaryOutputFunnyConverter.Instance;

        var properties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        if (clrType.GetConstructor(Type.EmptyTypes) == null)
            throw FunnyInvalidUsageException.OutputTypeConstainsNoParameterlessCtor(clrType);
        if (properties.Any())
        {
            var propertiesConverters =
                new (string, IOutputFunnyConverter, PropertyInfo)[properties.Length];
            int readPropertiesCount = 0;
            foreach (var property in properties)
            {
                if (!property.HasPublicSetter())
                    continue;
                var propertyConverter = GetOutputConverterFor(typeBehaviour, property.PropertyType, reqDeepthCheck++);

                propertiesConverters[readPropertiesCount] =
                    new ValueTuple<string, IOutputFunnyConverter, PropertyInfo>(
                        property.Name.ToLower(),
                        propertyConverter, property);
                readPropertiesCount++;
            }

            return new StructOutputFunnyConverter(clrType, propertiesConverters, readPropertiesCount);
        }

        return new DynamicTypeOutputFunnyConverter(clrType,typeBehaviour);
    }

    public static IOutputFunnyConverter GetOutputConverterFor(this TypeBehaviour typeBehaviour, FunnyType funnyType) {

        var converter =  typeBehaviour.GetPrimitiveOutputConverterOrNull(funnyType);
        if (converter!=null)
            return converter;
        
        switch (funnyType.BaseType)
        {
            case BaseFunnyType.ArrayOf:
            {
                if (funnyType.IsText)
                    return new StringOutputFunnyConverter();
                var elementConverter = GetOutputConverterFor(typeBehaviour, funnyType.ArrayTypeSpecification.FunnyType);
                var arrayType = elementConverter.ClrType.MakeArrayType();
                return new ClrArrayOutputFunnyConverter(arrayType, elementConverter);
            }
            // If output type is struct, but clr type is unknown (for ex in case of hardcore calc)
            // convert funny struct to an IDictionary<string,object>
            case BaseFunnyType.Struct:
                return new StructToDictionaryOutputFunnyConverter(typeBehaviour, funnyType);
            default:
                throw Errors.TypeCannotBeUsedAsOutputNfunType(funnyType);
        }
    }
    
    private static DynamicStructTypeInputFunnyConverter GetDynamicStructInputFunnyConverter(
        TypeBehaviour typeBehaviour, FunnyType funnyType,
        int reqDeepthCheck) {
        var dictionaryFields = new (string, IInputFunnyConverter)[funnyType.StructTypeSpecification.Count];
        var i = 0;
        foreach (var field in funnyType.StructTypeSpecification)
        {
            dictionaryFields[i] = new(field.Key, GetInputConverterFor(typeBehaviour, field.Value, null, reqDeepthCheck++));
            i++;
        }
        
        return new DynamicStructTypeInputFunnyConverter(dictionaryFields, funnyType);
    }
}

}