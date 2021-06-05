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
        public static object ConvertInput(object clrValue) =>
            GetInputConverter(clrValue.GetType()).ToFunObject(clrValue);
        
        public static IinputFunnyConverter GetInputConverter(FunnyType funnyType)
            => GetInputConverter(funnyType, 0);

        private static IinputFunnyConverter GetInputConverter(FunnyType funnyType, int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested input object");
            
            switch (funnyType.BaseType)
            {
                case BaseFunnyType.Char:
                case BaseFunnyType.Bool:
                case BaseFunnyType.UInt8:
                case BaseFunnyType.UInt16:
                case BaseFunnyType.UInt32:
                case BaseFunnyType.UInt64:
                case BaseFunnyType.Int16:
                case BaseFunnyType.Int32:
                case BaseFunnyType.Int64:
                case BaseFunnyType.Real:
                    return new PrimitiveTypeInputFunnyConverter(funnyType);
                case BaseFunnyType.ArrayOf:
                    if (funnyType.IsText)
                        return new StringTypeInputFunnyConverter();
                    var elementConverter = GetInputConverter(funnyType.ArrayTypeSpecification.FunnyType, reqDeepthCheck+1);
                    return new ClrArrayInputTypeFunnyConverter(elementConverter);
                case BaseFunnyType.Any:
                    return new DynamicTypeInputFunnyConverter();
                case BaseFunnyType.Empty:
                case BaseFunnyType.Fun:
                case BaseFunnyType.Generic:
                case BaseFunnyType.Struct:
                default:
                    throw new NotSupportedException($"type {funnyType} is not supported for input convertion");
            }
        }


        public static IinputFunnyConverter GetInputConverter(Type clrType) => GetInputConverter(clrType, 0);
 
        private static IinputFunnyConverter GetInputConverter(Type clrType, int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested input object");
            
            if (clrType == typeof(string))
                return new StringTypeInputFunnyConverter();

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                var elementConverter = GetInputConverter(elementType, reqDeepthCheck++);
                
                return new ClrArrayInputTypeFunnyConverter(elementConverter);
            }
           
            if (clrType == typeof(byte)) 
                return new PrimitiveTypeInputFunnyConverter(FunnyType.UInt8);
            if (clrType == typeof(UInt16)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.UInt16);
            if (clrType == typeof(UInt32)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.UInt32);
            if (clrType == typeof(UInt64)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.UInt64);
            if (clrType == typeof(Int16)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.Int16);
            if (clrType == typeof(Int32)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.Int32);
            if (clrType == typeof(Int64)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.Int64);
            if (clrType == typeof(Double)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.Real);
            if (clrType == typeof(Char)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.Char);
            if (clrType == typeof(bool)) 
                return  new PrimitiveTypeInputFunnyConverter(FunnyType.Bool);
            
            var properties =  clrType.GetProperties( BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                (string, IinputFunnyConverter, PropertyInfo)[] propertiesConverters =
                    new (string, IinputFunnyConverter, PropertyInfo)[properties.Length];
                int readPropertiesCount = 0;
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    if(!property.CanRead)
                        continue;
                    if(!property.GetMethod.Attributes.HasFlag(MethodAttributes.Public))
                        continue;
                    var  propertyConverter =GetInputConverter(property.PropertyType, reqDeepthCheck++);
                    propertiesConverters[readPropertiesCount] =
                        new (property.Name.ToLower(), propertyConverter, property);
                    readPropertiesCount++;
                }
                    
                return new StructTypeInputFunnyConverter(propertiesConverters,readPropertiesCount);
            }
            return new PrimitiveTypeInputFunnyConverter(FunnyType.Anything);
        }


        public static IOutputFunnyConverter GetOutputConverter(Type clrType) => GetOutputConverter(clrType, 0);

        private static IOutputFunnyConverter GetOutputConverter(Type clrType, int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested output object");
            
            if (clrType == typeof(string))
                return new StringOutputFunnyConverter();

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                var elementConverter = GetOutputConverter(elementType, reqDeepthCheck++);
                
                return new ClrArrayOutputFunnyConverter(clrType, elementConverter);
            }
           
            if (clrType == typeof(byte))    return  new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt8, clrType);
            if (clrType == typeof(UInt16))  return  new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt16, clrType);
            if (clrType == typeof(UInt32))  return  new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt32, clrType);
            if (clrType == typeof(UInt64))  return  new PrimitiveTypeOutputFunnyConverter(FunnyType.UInt64,clrType);
            if (clrType == typeof(Int16))   return  new PrimitiveTypeOutputFunnyConverter(FunnyType.Int16,clrType);
            if (clrType == typeof(Int32))   return  new PrimitiveTypeOutputFunnyConverter(FunnyType.Int32,clrType);
            if (clrType == typeof(Int64))   return  new PrimitiveTypeOutputFunnyConverter(FunnyType.Int64,clrType);
            if (clrType == typeof(Double))  return  new PrimitiveTypeOutputFunnyConverter(FunnyType.Real,clrType);
            if (clrType == typeof(Char))    return  new PrimitiveTypeOutputFunnyConverter(FunnyType.Char,clrType);
            if (clrType == typeof(bool))    return  new PrimitiveTypeOutputFunnyConverter(FunnyType.Bool,clrType);
            
            var properties =  clrType.GetProperties( BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                if (clrType.GetConstructor(Type.EmptyTypes) == null)
                    throw FunInvalidUsageException.OutputTypeConstainsNoParameterlessCtor(clrType);
                var propertiesConverters =
                    new (string, IOutputFunnyConverter, PropertyInfo)[properties.Length];
                int readPropertiesCount = 0;
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    if(!property.CanWrite)
                        continue;
                    if(!property.GetMethod.Attributes.HasFlag(MethodAttributes.Public))
                        continue;
                    var  propertyConverter =GetOutputConverter(property.PropertyType, reqDeepthCheck++);
                    
                    propertiesConverters[readPropertiesCount] =
                        new (property.Name.ToLower(), propertyConverter, property);
                    readPropertiesCount++;
                }
                return new StructOutputFunnyConverter(clrType, propertiesConverters,readPropertiesCount);
            }
            return new DynamicTypeOutputFunnyConverter(clrType);
        }

        public static IOutputFunnyConverter GetOutputConverter(FunnyType funnyType)
        {
            switch (funnyType.BaseType)
            {
                case BaseFunnyType.Bool:   return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(bool));
                case BaseFunnyType.Char:   return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(char));
                case BaseFunnyType.UInt8:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(byte));
                case BaseFunnyType.UInt16: return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(ushort));
                case BaseFunnyType.UInt32: return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(uint));
                case BaseFunnyType.UInt64: return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(ulong));
                case BaseFunnyType.Int16:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(short));
                case BaseFunnyType.Int32:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(int));
                case BaseFunnyType.Int64:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(long));
                case BaseFunnyType.Real:   return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(double));
                case BaseFunnyType.Any:    return new DynamicTypeOutputFunnyConverter(typeof(object));
                case BaseFunnyType.ArrayOf:
                {
                    if (funnyType.IsText)
                        return new StringOutputFunnyConverter();
                    var elementConverter = GetOutputConverter(funnyType.ArrayTypeSpecification.FunnyType);
                    var arrayType =elementConverter.ClrType.MakeArrayType();
                    return new ClrArrayOutputFunnyConverter(arrayType, elementConverter);
                }
                // If output type is struct, but clr type is unknown (for ex in case of hardcore calc)
                // return funny struct as IReadOnlyDictionary<string,object> interface
                case BaseFunnyType.Struct: return new PrimitiveTypeOutputFunnyConverter(funnyType,
                    typeof(IReadOnlyDictionary<string,object>));
                default:
                    throw ErrorFactory.TypeCannotBeUsedAsOutputNfunType(funnyType);
            }
        }
    }
}