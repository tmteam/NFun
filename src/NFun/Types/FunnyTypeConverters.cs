using System;
using System.Linq;
using System.Reflection;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Runtime;

namespace NFun.Types
{
    public static class FunnyTypeConverters
    {
        public static object ConvertInput(object clrValue) =>
            GetInputConverter(clrValue.GetType()).ToFunObject(clrValue);
        
        public static IinputFunnyConverter GetInputConverter(VarType funnyType)
            => GetInputConverter(funnyType, 0);

        private static IinputFunnyConverter GetInputConverter(VarType funnyType, int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested input object");
            
            switch (funnyType.BaseType)
            {
                case BaseVarType.Char:
                case BaseVarType.Bool:
                case BaseVarType.UInt8:
                case BaseVarType.UInt16:
                case BaseVarType.UInt32:
                case BaseVarType.UInt64:
                case BaseVarType.Int16:
                case BaseVarType.Int32:
                case BaseVarType.Int64:
                case BaseVarType.Real:
                    return new PrimitiveTypeInputFunnyConverter(funnyType);
                case BaseVarType.ArrayOf:
                    if (funnyType.IsText)
                        return new StringTypeInputFunnyConverter();
                    var elementConverter = GetInputConverter(funnyType.ArrayTypeSpecification.VarType, reqDeepthCheck+1);
                    return new ClrArrayInputTypeFunnyConverter(elementConverter);
                case BaseVarType.Any:
                    return new DynamicTypeInputFunnyConverter();
                case BaseVarType.Empty:
                case BaseVarType.Fun:
                case BaseVarType.Generic:
                case BaseVarType.Struct:
                    
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
                return new PrimitiveTypeInputFunnyConverter(VarType.UInt8);
            if (clrType == typeof(UInt16)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.UInt16);
            if (clrType == typeof(UInt32)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.UInt32);
            if (clrType == typeof(UInt64)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.UInt64);
            if (clrType == typeof(Int16)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.Int16);
            if (clrType == typeof(Int32)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.Int32);
            if (clrType == typeof(Int64)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.Int64);
            if (clrType == typeof(Double)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.Real);
            if (clrType == typeof(Char)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.Char);
            if (clrType == typeof(bool)) 
                return  new PrimitiveTypeInputFunnyConverter(VarType.Bool);
            
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
            return new PrimitiveTypeInputFunnyConverter(VarType.Anything);
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
           
            if (clrType == typeof(byte))    return  new PrimitiveTypeOutputFunnyConverter(VarType.UInt8, clrType);
            if (clrType == typeof(UInt16))  return  new PrimitiveTypeOutputFunnyConverter(VarType.UInt16, clrType);
            if (clrType == typeof(UInt32))  return  new PrimitiveTypeOutputFunnyConverter(VarType.UInt32, clrType);
            if (clrType == typeof(UInt64))  return  new PrimitiveTypeOutputFunnyConverter(VarType.UInt64,clrType);
            if (clrType == typeof(Int16))   return  new PrimitiveTypeOutputFunnyConverter(VarType.Int16,clrType);
            if (clrType == typeof(Int32))   return  new PrimitiveTypeOutputFunnyConverter(VarType.Int32,clrType);
            if (clrType == typeof(Int64))   return  new PrimitiveTypeOutputFunnyConverter(VarType.Int64,clrType);
            if (clrType == typeof(Double))  return  new PrimitiveTypeOutputFunnyConverter(VarType.Real,clrType);
            if (clrType == typeof(Char))    return  new PrimitiveTypeOutputFunnyConverter(VarType.Char,clrType);
            if (clrType == typeof(bool))    return  new PrimitiveTypeOutputFunnyConverter(VarType.Bool,clrType);
            
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

        public static IOutputFunnyConverter GetOutputConverter(VarType funnyType)
        {
            switch (funnyType.BaseType)
            {
                case BaseVarType.Bool:   return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(bool));
                case BaseVarType.Char:   return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(char));
                case BaseVarType.UInt8:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(byte));
                case BaseVarType.UInt16: return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(ushort));
                case BaseVarType.UInt32: return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(uint));
                case BaseVarType.UInt64: return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(ulong));
                case BaseVarType.Int16:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(short));
                case BaseVarType.Int32:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(int));
                case BaseVarType.Int64:  return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(long));
                case BaseVarType.Real:   return new PrimitiveTypeOutputFunnyConverter(funnyType, typeof(double));
                case BaseVarType.Any:    return new DynamicTypeOutputFunnyConverter(typeof(object));
                case BaseVarType.ArrayOf:
                {
                    if (funnyType.IsText)
                        return new StringOutputFunnyConverter();
                    var elementConverter = GetOutputConverter(funnyType.ArrayTypeSpecification.VarType);
                    var arrayType =elementConverter.ClrType.MakeArrayType();
                    return new ClrArrayOutputFunnyConverter(arrayType, elementConverter);
                }
                // If output type is struct, but clr type is unknown (for ex in case of hardcore calc)
                // return funny struct as IReadonlyFunnyStruct interface
                // It looks like dictionary - and it is best we can do for a client
                case BaseVarType.Struct: return new PrimitiveTypeOutputFunnyConverter(funnyType,
                    typeof(IReadonlyFunnyStruct));
                default:
                    throw ErrorFactory.TypeCannotBeUsedAsOutputNfunType(funnyType);
            }
        }
    }
}