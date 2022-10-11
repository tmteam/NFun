using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NFun.Exceptions;
using NFun.ParseErrors;

namespace NFun.Types;

public class FunnyConverter {
    
    private readonly ConcurrentDictionary<Type, IOutputFunnyConverter> _outputTypeFromClr = new();
    private readonly ConcurrentDictionary<FunnyType, IOutputFunnyConverter> _outputTypeFromFunny = new();
    private readonly ConcurrentDictionary<(Type, FunnyType), IInputFunnyConverter> _inputTypeFromPair = new();
    private readonly ConcurrentDictionary<Type, IInputFunnyConverter> _inputTypeFromClr = new();
    
    public TypeBehaviour TypeBehaviour { get; }
    public static readonly FunnyConverter RealIsDouble = new(TypeBehaviour.RealIsDouble);
    public static readonly FunnyConverter RealIsDecimal = new(TypeBehaviour.RealIsDecimal);
    
    private FunnyConverter(TypeBehaviour typeBehaviour)
    {
        TypeBehaviour = typeBehaviour;
    }

    /// <summary>
    /// Test GAP
    /// </summary>
    public void ClearCaches()
    {
        _outputTypeFromClr.Clear();
        _outputTypeFromFunny.Clear();
        _inputTypeFromPair.Clear();
        _inputTypeFromClr.Clear();
    }

    /// <summary>
    /// Test GAP
    /// </summary>
    public int CacheSize =>
        _outputTypeFromClr.Count +
        _outputTypeFromFunny.Count +
        _inputTypeFromPair.Count +
        _inputTypeFromClr.Count;
    
    public object ConvertInputOrThrow(object clrValue, FunnyType resultFunnyType) {
        var clrFromType = clrValue.GetType();
        if (TypeBehaviour.GetClrTypeFor(resultFunnyType.BaseType) == clrFromType)
            return clrValue;

        var converter = GetInputConverterFor(clrFromType,resultFunnyType);
        if (converter.FunnyType == resultFunnyType)
            return converter.ToFunObject(clrValue);

        //Special slow convertation
        return resultFunnyType.BaseType switch {
                   BaseFunnyType.Any                                  => converter.ToFunObject(clrValue),
                   BaseFunnyType.Bool                                 => Convert.ToBoolean(clrValue),
                   BaseFunnyType.Int16                                => Convert.ToInt16(clrValue),
                   BaseFunnyType.Int32                                => Convert.ToInt32(clrValue),
                   BaseFunnyType.Int64                                => Convert.ToInt64(clrValue),
                   BaseFunnyType.UInt8                                => Convert.ToByte(clrValue),
                   BaseFunnyType.UInt16                               => Convert.ToUInt16(clrValue),
                   BaseFunnyType.UInt32                               => Convert.ToUInt32(clrValue),
                   BaseFunnyType.UInt64                               => Convert.ToUInt64(clrValue),
                   BaseFunnyType.Real when TypeBehaviour.DoubleIsReal => Convert.ToDouble(clrValue),
                   BaseFunnyType.Real                                 => Convert.ToDecimal(clrValue),
                   BaseFunnyType.Char                                 => clrValue.ToString(),
                   _                                                  => converter.ToFunObject(clrValue)
               };
    }

    public IOutputFunnyConverter GetOutputConverterFor(Type clrType) => GetOutputConverterReq(clrType, reqDeepthCheck: 0);
    
    private IOutputFunnyConverter GetOutputConverterReq(Type clrType, int reqDeepthCheck = 0)
    {
        if (reqDeepthCheck > 100)
            throw new ArgumentException("Too nested output object");
        // primitive types are not cached
        var primitiveOutputConverter = TypeBehaviour.GetPrimitiveOutputConverterOrNull(clrType);
        if (primitiveOutputConverter != null) 
            return primitiveOutputConverter;
        if (clrType == typeof(string))
            return new StringOutputFunnyConverter();
        
        // if it is not primitive - search in cache
        if (_outputTypeFromClr.Count <1000 && _outputTypeFromClr.TryGetValue(clrType, out var alreadyKnown))
            return alreadyKnown;
        
        // Create new converter and put it into cache
        var converter =  CreateOutputConverterFor(clrType, reqDeepthCheck);
        _outputTypeFromClr.TryAdd(clrType, converter);
        return converter;
        
        IOutputFunnyConverter CreateOutputConverterFor(Type clrType, int reqDeepthCheck) {

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                // ReSharper disable once RedundantAssignment
                var elementConverter = GetOutputConverterReq(elementType, reqDeepthCheck++);

                return new ClrArrayOutputFunnyConverter(clrType, elementConverter);
            }

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
                    new OutputProperty[properties.Length];
                int readPropertiesCount = 0;
                foreach (var property in properties)
                {
                    if (!property.HasPublicSetter())
                        continue;
                    var propertyConverter = GetOutputConverterReq(property.PropertyType, reqDeepthCheck++);

                    propertiesConverters[readPropertiesCount] =
                        new OutputProperty(
                            property.Name.ToLower(),
                            propertyConverter, property);
                    readPropertiesCount++;
                }
                return new StructOutputFunnyConverter(clrType, propertiesConverters, readPropertiesCount);
            }
            return new DynamicTypeOutputFunnyConverter(clrType, this);
        }
    }

    public IOutputFunnyConverter GetOutputConverterFor(FunnyType type)
    {
        var primitiveConverter = TypeBehaviour.GetPrimitiveOutputConverterOrNull(type);
        if (primitiveConverter != null)
            return primitiveConverter;
        
        if (_outputTypeFromFunny.Count<10000 && _outputTypeFromFunny.TryGetValue(type, out var alreadyKnown))
            return alreadyKnown;
        
        var converter = CreateOutputConverterFor(type);
        _outputTypeFromFunny.TryAdd(type, converter);
        return converter;
        
        IOutputFunnyConverter CreateOutputConverterFor(FunnyType funnyType) {

            switch (funnyType.BaseType)
            {
                case BaseFunnyType.ArrayOf:
                {
                    if (funnyType.IsText)
                        return new StringOutputFunnyConverter();
                    var elementConverter = GetOutputConverterFor(funnyType.ArrayTypeSpecification.FunnyType);
                    var arrayType = elementConverter.ClrType.MakeArrayType();
                    return new ClrArrayOutputFunnyConverter(arrayType, elementConverter);
                }
                // If output type is struct, but clr type is unknown (for ex in case of hardcore calc)
                // convert funny struct to an IDictionary<string,object>
                case BaseFunnyType.Struct:
                    return new StructToDictionaryOutputFunnyConverter(this, funnyType);
                default:
                    throw Errors.TypeCannotBeUsedAsOutputNfunType(funnyType);
            }
        }

    }
    
    public IInputFunnyConverter GetInputConverterFor(Type clrType, FunnyType funnyType) => GetInputConverterReq(clrType, funnyType, reqDeepthCheck: 0);
   
    private IInputFunnyConverter GetInputConverterReq(Type clrTypeOrNull, FunnyType funnyType, int reqDeepthCheck)
    {
        if (reqDeepthCheck > 100)
            throw new ArgumentException("Too nested input object");
        // Primitive types are not cached
        if (funnyType.IsPrimitive)
        {
            if (clrTypeOrNull != null)
            {
                var byTypeConverter = TypeBehaviour.GetPrimitiveInputConverterOrNull(clrTypeOrNull);
                if (byTypeConverter != null)
                    return byTypeConverter;
            }
            else
            {
                var byFunnyConverter = TypeBehaviour.GetPrimitiveInputConverterOrNull(funnyType);
                if (byFunnyConverter != null) 
                    return byFunnyConverter;
            }
        }
        
        // if it is not primitive - search in cache
        var pair = (clrTypeOrNull, funnyType);
        if (_inputTypeFromPair.Count<10000 && _inputTypeFromPair.TryGetValue(pair, out var alreadyKnown))
            return alreadyKnown;
        // Create new converter and put it into cache
        var converter = CreateInputConverterFor(funnyType, clrTypeOrNull, reqDeepthCheck);
        _inputTypeFromPair.TryAdd(pair, converter);
        return converter;

        IInputFunnyConverter CreateInputConverterFor(
            FunnyType funnyType,
            Type clrTypeOrNull,
            int reqDeepthCheck)
        {
            if (funnyType.IsText)
                return StringTypeInputFunnyConverter.Instance;

            if (funnyType.BaseType == BaseFunnyType.ArrayOf)
            {
                var elementType = clrTypeOrNull?.GetElementType();
                var elementConverter = GetInputConverterReq(
                    // ReSharper disable once RedundantAssignment
                    elementType, funnyType.ArrayTypeSpecification.FunnyType, reqDeepthCheck++);
                return new ClrArrayInputTypeFunnyConverter(elementConverter);
            }

            if (funnyType.BaseType != BaseFunnyType.Struct)
                return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);

            if (clrTypeOrNull == null || typeof(Dictionary<string, object>).IsAssignableFrom(clrTypeOrNull))
                return GetDynamicStructInputFunnyConverter(funnyType, reqDeepthCheck++);

            var properties = clrTypeOrNull.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                var propertiesConverters = new InputProperty[properties.Length];
                int readPropertiesCount = 0;
                foreach (var property in properties)
                {
                    if (!property.HasPublicGetter())
                        continue;
                    if (!funnyType.StructTypeSpecification.TryGetValue(property.Name, out var fieldDef))
                        continue;
                    var propertyConverter = GetInputConverterReq(property.PropertyType, fieldDef, reqDeepthCheck++);
                    propertiesConverters[readPropertiesCount] =
                        new InputProperty(
                            property.Name.ToLower(),
                            propertyConverter, property);
                    readPropertiesCount++;
                }

                return new StructTypeInputFunnyConverter(propertiesConverters, readPropertiesCount, funnyType);
            }

            return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);
        }
    }

    public IInputFunnyConverter GetInputConverterFor(Type clrType) => GetInputConverterReq(clrType, reqDeepthCheck: 0);

    private IInputFunnyConverter GetInputConverterReq(Type clrType, int reqDeepthCheck)
    {
        if (reqDeepthCheck > 100)
            throw new ArgumentException("Too nested input object");
        // Primitive converter are not cached
        var primitiveConverter = TypeBehaviour.GetPrimitiveInputConverterOrNull(clrType);
        if (primitiveConverter != null)
            return primitiveConverter;
        // if it is not primitive then search in the cache
        if (_inputTypeFromClr.Count < 10000 && _inputTypeFromClr.TryGetValue(clrType, out var alreadyKnown))
            return alreadyKnown;
        // Create new converter and put it into cache
        var converter =  CreateInputConverterFor(clrType, reqDeepthCheck);
        _inputTypeFromClr.TryAdd(clrType, converter);
        return converter;
        
        IInputFunnyConverter CreateInputConverterFor(Type clrType, int reqDeepthCheck) {
            if (clrType == typeof(string))
                return StringTypeInputFunnyConverter.Instance;

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                // ReSharper disable once RedundantAssignment
                var elementConverter = GetInputConverterReq(elementType, reqDeepthCheck++);

                return new ClrArrayInputTypeFunnyConverter(elementConverter);
            }

            var properties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                var propertiesConverters =
                    new InputProperty[properties.Length];
                int readPropertiesCount = 0;
                foreach (var property in properties)
                {
                    if (!property.HasPublicGetter())
                        continue;
                    var propertyConverter = GetInputConverterReq(property.PropertyType, reqDeepthCheck++);
                    propertiesConverters[readPropertiesCount] =
                        new InputProperty(
                            property.Name.ToLower(),
                            propertyConverter, property);
                    readPropertiesCount++;
                }

                return new StructTypeInputFunnyConverter(propertiesConverters, readPropertiesCount);
            }

            return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);
        }
    }

    private IInputFunnyConverter GetDynamicStructInputFunnyConverter(
        FunnyType funnyType,
        int reqDeepthCheck) {
        var dictionaryFields = new (string, IInputFunnyConverter)[funnyType.StructTypeSpecification.Count];
        var i = 0;
        foreach (var field in funnyType.StructTypeSpecification)
        {
            dictionaryFields[i] = new(field.Key, GetInputConverterReq(null, field.Value, reqDeepthCheck++));
            i++;
        }

        return new DynamicStructTypeInputFunnyConverter(dictionaryFields, funnyType);
    }
}