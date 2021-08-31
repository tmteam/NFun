using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NFun.Exceptions;
using NFun.ParseErrors;

namespace NFun.Types
{
    public static class FunnyTypeConverters
    {
        public static object ConvertInputOrThrow(object clrValue, FunnyType resultFunnyType)
        {
            var clrFromType = clrValue.GetType();
            if (resultFunnyType.BaseType.GetClrType() == clrFromType)
                return clrValue;

            var converter = GetInputConverter(resultFunnyType, clrFromType, 0);
            if (converter.FunnyType == resultFunnyType)
                return converter.ToFunObject(clrValue);

            //Special slow convertation
            return resultFunnyType.BaseType switch
            {
                BaseFunnyType.Any => converter.ToFunObject(clrValue),
                BaseFunnyType.Bool => Convert.ToBoolean(clrValue),
                BaseFunnyType.Int16 => Convert.ToInt16(clrValue),
                BaseFunnyType.Int32 => Convert.ToInt32(clrValue),
                BaseFunnyType.Int64 => Convert.ToInt64(clrValue),
                BaseFunnyType.UInt8 => Convert.ToByte(clrValue),
                BaseFunnyType.UInt16 => Convert.ToUInt16(clrValue),
                BaseFunnyType.UInt32 => Convert.ToUInt32(clrValue),
                BaseFunnyType.UInt64 => Convert.ToUInt64(clrValue),
                BaseFunnyType.Real => Convert.ToDouble(clrValue),
                BaseFunnyType.Char => clrValue.ToString(),
                _ => converter.ToFunObject(clrValue)
            };
        }


        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static IInputFunnyConverter GetInputConverter(FunnyType funnyType, Type clrTypeOrNull,
            int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested input object");

            if (funnyType.IsPrimitive)
            {
                if (clrTypeOrNull != null &&
                    PrimitiveInputConvertersByType.TryGetValue(clrTypeOrNull, out var byTypeConverter))
                    return byTypeConverter;

                if (PrimitiveInputConvertersByName.TryGetValue(funnyType.BaseType, out var converter))
                    return converter;
            }

            if (funnyType.IsText)
                return new StringTypeInputFunnyConverter();

            if (funnyType.BaseType == BaseFunnyType.ArrayOf)
            {
                var elementType = clrTypeOrNull?.GetElementType();
                var elementConverter = GetInputConverter(funnyType.ArrayTypeSpecification.FunnyType, elementType,
                    reqDeepthCheck++);
                return new ClrArrayInputTypeFunnyConverter(elementConverter);
            }

            if (funnyType.BaseType != BaseFunnyType.Struct)
                return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);

            if (clrTypeOrNull == null || typeof(Dictionary<string, object>).IsAssignableFrom(clrTypeOrNull))
            {
                var dictionaryFields = new (string, IInputFunnyConverter)[funnyType.StructTypeSpecification.Count];
                var i = 0;
                foreach (var field in funnyType.StructTypeSpecification)
                {
                    dictionaryFields[i] = new(field.Key, GetInputConverter(field.Value, null, reqDeepthCheck++));
                    i++;
                }

                return new DynamicStructTypeInputFunnyConverter(dictionaryFields);
            }

            var properties = clrTypeOrNull.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                (string, IInputFunnyConverter, PropertyInfo)[] propertiesConverters =
                    new (string, IInputFunnyConverter, PropertyInfo)[properties.Length];
                int readPropertiesCount = 0;
                foreach (var property in properties)
                {
                    if (!property.CanBeUsedAsFunnyInputProperty())
                        continue;
                    if (!funnyType.StructTypeSpecification.TryGetValue(property.Name, out var fieldDef))
                        continue;
                    var propertyConverter = GetInputConverter(fieldDef, property.PropertyType, reqDeepthCheck++);
                    propertiesConverters[readPropertiesCount] =
                        new ValueTuple<string, IInputFunnyConverter, PropertyInfo>(property.Name.ToLower(),
                            propertyConverter, property);
                    readPropertiesCount++;
                }

                return new StructTypeInputFunnyConverter(propertiesConverters, readPropertiesCount, funnyType);
            }

            return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);
        }

        public static IInputFunnyConverter GetInputConverter(Type clrType) => GetInputConverter(clrType, 0);

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static IInputFunnyConverter GetInputConverter(Type clrType, int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested input object");

            if (clrType == typeof(string))
                return new StringTypeInputFunnyConverter();

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                // ReSharper disable once RedundantAssignment
                var elementConverter = GetInputConverter(elementType, reqDeepthCheck++);

                return new ClrArrayInputTypeFunnyConverter(elementConverter);
            }

            if (PrimitiveInputConvertersByType.TryGetValue(clrType, out var converter))
                return converter;

            var properties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                (string, IInputFunnyConverter, PropertyInfo)[] propertiesConverters =
                    new (string, IInputFunnyConverter, PropertyInfo)[properties.Length];
                int readPropertiesCount = 0;
                foreach (var property in properties)
                {
                    if (!property.CanBeUsedAsFunnyInputProperty())
                        continue;
                    var propertyConverter = GetInputConverter(property.PropertyType, reqDeepthCheck++);
                    propertiesConverters[readPropertiesCount] =
                        new ValueTuple<string, IInputFunnyConverter, PropertyInfo>(property.Name.ToLower(),
                            propertyConverter, property);
                    readPropertiesCount++;
                }

                return new StructTypeInputFunnyConverter(propertiesConverters, readPropertiesCount);
            }

            return new PrimitiveTypeInputFunnyConverter(FunnyType.Any);
        }

        public static IOutputFunnyConverter GetOutputConverter(Type clrType) => GetOutputConverter(clrType, 0);

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static IOutputFunnyConverter GetOutputConverter(Type clrType, int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested output object");

            if (clrType == typeof(string))
                return new StringOutputFunnyConverter();

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                // ReSharper disable once RedundantAssignment
                var elementConverter = GetOutputConverter(elementType, reqDeepthCheck++);

                return new ClrArrayOutputFunnyConverter(clrType, elementConverter);
            }

            if (PrimitiveOutputConvertersByType.TryGetValue(clrType, out var converter))
                return converter;

            var properties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                if (clrType.GetConstructor(Type.EmptyTypes) == null)
                    throw FunnyInvalidUsageException.OutputTypeConstainsNoParameterlessCtor(clrType);
                var propertiesConverters =
                    new (string, IOutputFunnyConverter, PropertyInfo)[properties.Length];
                int readPropertiesCount = 0;
                foreach (var property in properties)
                {
                    if(!property.CanBeUsedAsFunnyOutputProperty())
                        continue;
                    var propertyConverter = GetOutputConverter(property.PropertyType, reqDeepthCheck++);

                    propertiesConverters[readPropertiesCount] =
                        new ValueTuple<string, IOutputFunnyConverter, PropertyInfo>(property.Name.ToLower(),
                            propertyConverter, property);
                    readPropertiesCount++;
                }

                return new StructOutputFunnyConverter(clrType, propertiesConverters, readPropertiesCount);
            }

            return new DynamicTypeOutputFunnyConverter(clrType);
        }

        public static IOutputFunnyConverter GetOutputConverter(FunnyType funnyType)
        {
            if (PrimitiveOutputConvertersByName.TryGetValue(funnyType.BaseType, out var converter))
                return converter;
            switch (funnyType.BaseType)
            {
                case BaseFunnyType.Any: return new DynamicTypeOutputFunnyConverter(typeof(object));
                case BaseFunnyType.ArrayOf:
                {
                    if (funnyType.IsText)
                        return new StringOutputFunnyConverter();
                    var elementConverter = GetOutputConverter(funnyType.ArrayTypeSpecification.FunnyType);
                    var arrayType = elementConverter.ClrType.MakeArrayType();
                    return new ClrArrayOutputFunnyConverter(arrayType, elementConverter);
                }
                // If output type is struct, but clr type is unknown (for ex in case of hardcore calc)
                // convert funny struct to an IDictionary<string,object>
                case BaseFunnyType.Struct:
                    return new StructToDictionaryOutputFunnyConverter(funnyType);
                default:
                    throw ErrorFactory.TypeCannotBeUsedAsOutputNfunType(funnyType);
            }
        }

        #region predefined converters

        private static readonly IReadOnlyDictionary<BaseFunnyType, IInputFunnyConverter> PrimitiveInputConvertersByName
            = new Dictionary<BaseFunnyType, IInputFunnyConverter>()
            {
                { BaseFunnyType.Bool, new PrimitiveTypeInputFunnyConverter(FunnyType.Bool) },
                { BaseFunnyType.Char, new PrimitiveTypeInputFunnyConverter(FunnyType.Char) },
                { BaseFunnyType.UInt8, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt8) },
                { BaseFunnyType.UInt16, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt16) },
                { BaseFunnyType.UInt32, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt32) },
                { BaseFunnyType.UInt64, new PrimitiveTypeInputFunnyConverter(FunnyType.UInt64) },
                { BaseFunnyType.Int16, new PrimitiveTypeInputFunnyConverter(FunnyType.Int16) },
                { BaseFunnyType.Int32, new PrimitiveTypeInputFunnyConverter(FunnyType.Int32) },
                { BaseFunnyType.Int64, new PrimitiveTypeInputFunnyConverter(FunnyType.Int64) },
                { BaseFunnyType.Real, new PrimitiveTypeInputFunnyConverter(FunnyType.Real) },
            };

        private static readonly IReadOnlyDictionary<Type, IInputFunnyConverter> PrimitiveInputConvertersByType
            = new Dictionary<Type, IInputFunnyConverter>()
            {
                { typeof(bool), new PrimitiveTypeInputFunnyConverter(FunnyType.Bool) },
                { typeof(Char), new PrimitiveTypeInputFunnyConverter(FunnyType.Char) },
                { typeof(byte), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt8) },
                { typeof(UInt16), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt16) },
                { typeof(UInt32), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt32) },
                { typeof(UInt64), new PrimitiveTypeInputFunnyConverter(FunnyType.UInt64) },
                { typeof(Int16), new PrimitiveTypeInputFunnyConverter(FunnyType.Int16) },
                { typeof(Int32), new PrimitiveTypeInputFunnyConverter(FunnyType.Int32) },
                { typeof(Int64), new PrimitiveTypeInputFunnyConverter(FunnyType.Int64) },
                { typeof(double), new PrimitiveTypeInputFunnyConverter(FunnyType.Real) },
            };

        private static readonly IReadOnlyDictionary<BaseFunnyType, IOutputFunnyConverter>
            PrimitiveOutputConvertersByName
                = new Dictionary<BaseFunnyType, IOutputFunnyConverter>()
                {
                    { BaseFunnyType.Bool, new PrimitiveTypeOutputFunnyConverter(FunnyType.Bool, typeof(bool)) },
                    { BaseFunnyType.Char, new PrimitiveTypeOutputFunnyConverter(FunnyType.Char, typeof(Char)) },
                    { BaseFunnyType.UInt8, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt8, typeof(byte)) },
                    { BaseFunnyType.UInt16, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt16, typeof(UInt16)) },
                    { BaseFunnyType.UInt32, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt32, typeof(UInt32)) },
                    { BaseFunnyType.UInt64, new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt64, typeof(UInt64)) },
                    { BaseFunnyType.Int16, new PrimitiveTypeOutputFunnyConverter(FunnyType.Int16, typeof(Int16)) },
                    { BaseFunnyType.Int32, new PrimitiveTypeOutputFunnyConverter(FunnyType.Int32, typeof(Int32)) },
                    { BaseFunnyType.Int64, new PrimitiveTypeOutputFunnyConverter(FunnyType.Int64, typeof(Int64)) },
                    { BaseFunnyType.Real, new PrimitiveTypeOutputFunnyConverter(FunnyType.Real, typeof(double)) },
                };

        private static readonly IReadOnlyDictionary<Type, IOutputFunnyConverter> PrimitiveOutputConvertersByType
            = new Dictionary<Type, IOutputFunnyConverter>()
            {
                { typeof(bool), new PrimitiveTypeOutputFunnyConverter(FunnyType.Bool, typeof(bool)) },
                { typeof(Char), new PrimitiveTypeOutputFunnyConverter(FunnyType.Char, typeof(Char)) },
                { typeof(byte), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt8, typeof(byte)) },
                { typeof(UInt16), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt16, typeof(UInt16)) },
                { typeof(UInt32), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt32, typeof(UInt32)) },
                { typeof(UInt64), new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt64, typeof(UInt64)) },
                { typeof(Int16), new PrimitiveTypeOutputFunnyConverter(FunnyType.Int16, typeof(Int16)) },
                { typeof(Int32), new PrimitiveTypeOutputFunnyConverter(FunnyType.Int32, typeof(Int32)) },
                { typeof(Int64), new PrimitiveTypeOutputFunnyConverter(FunnyType.Int64, typeof(Int64)) },
                { typeof(double), new PrimitiveTypeOutputFunnyConverter(FunnyType.Real, typeof(double)) },
            };

        #endregion
    }
}