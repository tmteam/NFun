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
        public VarType FunnyType { get; }
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
            (string, VarType)[] fieldTypes = new (string, VarType)[_readPropertiesCount];
            for (int i = 0; i < readPropertiesCount; i++)
            {
                var (name, converter, _) = propertiesConverters[i];
                fieldTypes[i] = (name, converter.FunnyType);
            }
            FunnyType = VarType.StructOf(fieldTypes);
        }

        public VarType FunnyType { get; }

        public object ToFunObject(object clrObject)
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
    public class ClrArrayInputTypeFunnyConverter : IinputFunnyConverter
    {
        private readonly IinputFunnyConverter _elementConverter;
        public ClrArrayInputTypeFunnyConverter(IinputFunnyConverter elementConverter)
        {
            FunnyType = VarType.ArrayOf(elementConverter.FunnyType);
            _elementConverter = elementConverter;
        }
        public VarType FunnyType { get; }

        public object ToFunObject(object clrObject)
        {
            var array = clrObject as Array;
            var funnyObjects = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                var val = array.GetValue(i);
                funnyObjects[i] = _elementConverter.ToFunObject(val);
            }
            return new ImmutableFunArray(funnyObjects, FunnyType.ArrayTypeSpecification.VarType);
        }
    }

    public class DynamicTypeInputFunnyConverter : IinputFunnyConverter
    {
        public VarType FunnyType => VarType.Anything;
        public object ToFunObject(object clrObject)
        {
            var converter = FunnyTypeConverters.GetInputConverter(clrObject.GetType());
            return converter.ToFunObject(clrObject);
        }
    }

    public class PrimitiveTypeInputFunnyConverter : IinputFunnyConverter {
        public VarType FunnyType { get; }
        public PrimitiveTypeInputFunnyConverter(VarType funnyType) => FunnyType = funnyType;
        public object ToFunObject(object clrObject) => clrObject;
    }
    
    public class StringTypeInputFunnyConverter: IinputFunnyConverter {
        public VarType FunnyType { get; }
        public StringTypeInputFunnyConverter() => FunnyType = VarType.Text;
        public object ToFunObject(object clrObject) 
            => clrObject==null
                ?TextFunArray.Empty
                :new TextFunArray(clrObject.ToString());
    }
}