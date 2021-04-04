using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    /// <summary>
    /// Converts CLR type and value into NFun type and value
    /// </summary>
    public abstract class InputFunnyConverter
    {

        public static InputFunnyConverter GetConverter(Type clrType) => GetConverter(clrType, 0);
 
        private static InputFunnyConverter GetConverter(Type clrType, int reqDeepthCheck)
        {
            if (reqDeepthCheck > 100)
                throw new ArgumentException("Too nested input object");
            
            if (clrType == typeof(string))
                return new StringTypesInputFunnyConverter();

            if (clrType.IsArray)
            {
                var elementType = clrType.GetElementType();
                var elementConverter = GetConverter(elementType, reqDeepthCheck++);
                
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
                (string, InputFunnyConverter, PropertyInfo)[] propertiesConverters =
                    new (string, InputFunnyConverter, PropertyInfo)[properties.Length];
                int readPropertiesCount = 0;
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    if(!property.CanRead)
                        continue;
                    if(!property.GetMethod.Attributes.HasFlag(MethodAttributes.Public))
                        continue;
                    var  propertyConverter =GetConverter(property.PropertyType, reqDeepthCheck++);
                    propertiesConverters[readPropertiesCount] =
                        new (property.Name.ToLower(), propertyConverter, property);
                    readPropertiesCount++;
                }
                    
                return new StructTypeInputFunnyConverter(propertiesConverters,readPropertiesCount);
            }
            return new PrimitiveTypeInputFunnyConverter(VarType.Anything);
        }
      
        public VarType FunnyType { get; protected set; }
        
        public abstract object ToFunObject(object clrObject);
    }
    
    
    public class StructTypeInputFunnyConverter : InputFunnyConverter
    {
        private readonly (string, InputFunnyConverter, PropertyInfo)[] _propertiesConverters;
        private readonly int _readPropertiesCount;

        internal StructTypeInputFunnyConverter((string, InputFunnyConverter, PropertyInfo)[] propertiesConverters,
            int readPropertiesCount)
        {
            _propertiesConverters = propertiesConverters;
            _readPropertiesCount = readPropertiesCount;
            (string, VarType)[] fieldTypes = new (string, VarType)[_readPropertiesCount];
            for (int i = 0; i < readPropertiesCount; i++)
            {
                var converter = propertiesConverters[i];
                fieldTypes[i] = (converter.Item1, converter.Item2.FunnyType);
            }
            FunnyType = VarType.StructOf(fieldTypes);
        }

        public override object ToFunObject(object clrObject)
        {
            var values = new Dictionary<string, object>(_propertiesConverters.Length);
            for (var i = 0; i < _readPropertiesCount; i++)
            {
                var propertiesConverter = _propertiesConverters[i];
                values.Add(propertiesConverter.Item1,
                    propertiesConverter.Item2.ToFunObject(propertiesConverter.Item3.GetValue(clrObject)));
            }

            return new FunnyStruct(values);
        }
    }
    public class ClrArrayInputTypeFunnyConverter : InputFunnyConverter
    {
        private readonly InputFunnyConverter _elementConverter;
        public ClrArrayInputTypeFunnyConverter(InputFunnyConverter elementConverter)
        {
            FunnyType = VarType.ArrayOf(elementConverter.FunnyType);
            _elementConverter = elementConverter;
        }
        
        public override object ToFunObject(object clrObject)
        {
            var array = clrObject as Array;
            object[] funnyObjects = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                var val = array.GetValue(i);
                funnyObjects[i] = _elementConverter.ToFunObject(val);
            }
            return new ImmutableFunArray(funnyObjects, FunnyType.ArrayTypeSpecification.VarType);
        }
    }
    
    
    public class PrimitiveTypeInputFunnyConverter : InputFunnyConverter {
        public PrimitiveTypeInputFunnyConverter(VarType funnyType) => FunnyType = funnyType;
        public override object ToFunObject(object clrObject) => clrObject;
    }
    
    public class StringTypesInputFunnyConverter: InputFunnyConverter {
        public StringTypesInputFunnyConverter() => FunnyType = VarType.Text;
        public override object ToFunObject(object clrObject) => new TextFunArray(clrObject.ToString());
    }
}