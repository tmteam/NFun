using System;
using System.Collections.Generic;
using System.Reflection;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    /// <summary>
    /// Converts CLR type and value into NFun type and value
    /// </summary>
    public interface IinputFunnyConverter {
        public FunnyType FunnyType { get; }
        public  object ToFunObject(object clrObject);
    }
    
    
    public class StructTypeInputFunnyConverter : IinputFunnyConverter
    {
        private readonly (string, IinputFunnyConverter, PropertyInfo)[] _propertiesConverters;
        private readonly int _readPropertiesCount;

        internal StructTypeInputFunnyConverter((string, IinputFunnyConverter, PropertyInfo)[] propertiesConverters,
            int readPropertiesCount)
        {
            _propertiesConverters = propertiesConverters;
            _readPropertiesCount = readPropertiesCount;
            (string, FunnyType)[] fieldTypes = new (string, FunnyType)[_readPropertiesCount];
            for (int i = 0; i < readPropertiesCount; i++)
            {
                var (name, converter, _) = propertiesConverters[i];
                fieldTypes[i] = (name, converter.FunnyType);
            }
            FunnyType = Types.FunnyType.StructOf(fieldTypes);
        }

        public FunnyType FunnyType { get; }

        public object ToFunObject(object clrObject)
        {
            var values = new Dictionary<string, object>(_propertiesConverters.Length);
            for (var i = 0; i < _readPropertiesCount; i++)
            {
                var (key, inputFunnyConverter, propertyInfo) = _propertiesConverters[i];
                values.Add(key, inputFunnyConverter.ToFunObject(propertyInfo.GetValue(clrObject)));
            }

            return new FunnyStruct(values);
        }
    }
    public class ClrArrayInputTypeFunnyConverter : IinputFunnyConverter
    {
        private readonly IinputFunnyConverter _elementConverter;
        public ClrArrayInputTypeFunnyConverter(IinputFunnyConverter elementConverter)
        {
            FunnyType = Types.FunnyType.ArrayOf(elementConverter.FunnyType);
            _elementConverter = elementConverter;
        }
        public FunnyType FunnyType { get; }

        public object ToFunObject(object clrObject)
        {
            var array = clrObject as Array;
            var funnyObjects = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                var val = array.GetValue(i);
                funnyObjects[i] = _elementConverter.ToFunObject(val);
            }
            return new ImmutableFunArray(funnyObjects, FunnyType.ArrayTypeSpecification.FunnyType);
        }
    }

    public class DynamicTypeInputFunnyConverter : IinputFunnyConverter
    {
        public FunnyType FunnyType => FunnyType.Any;
        public object ToFunObject(object clrObject)
        {
            var converter = FunnyTypeConverters.GetInputConverter(clrObject.GetType());
            return converter.ToFunObject(clrObject);
        }
    }

    public class PrimitiveTypeInputFunnyConverter : IinputFunnyConverter {
        public FunnyType FunnyType { get; }
        public PrimitiveTypeInputFunnyConverter(FunnyType funnyType) => FunnyType = funnyType;
        public object ToFunObject(object clrObject) => clrObject;
    }
    
    public class StringTypeInputFunnyConverter: IinputFunnyConverter {
        public FunnyType FunnyType { get; }
        public StringTypeInputFunnyConverter() => FunnyType = FunnyType.Text;
        public object ToFunObject(object clrObject) 
            => clrObject==null
                ?TextFunArray.Empty
                :new TextFunArray(clrObject.ToString());
    }
}