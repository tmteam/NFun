using System;
using System.Reflection;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    public interface IOutputFunnyConverter
    {
        public VarType FunnyType { get; }
        public object ToClrObject(object funObject);
    }

    public class PrimitiveTypeOutputFunnyConverter:IOutputFunnyConverter {
        public VarType FunnyType { get;  }
        public PrimitiveTypeOutputFunnyConverter(VarType funnyType) => FunnyType = funnyType;
        public object ToClrObject(object funObject) => funObject;
    }

    public class StringOutputFunnyConverter : IOutputFunnyConverter {
        public VarType FunnyType { get; } = VarType.Text;
        public object ToClrObject(object funObject) => (funObject as IFunArray).ToText();
    }

    public class ClrArrayOutputFunnyConverter : IOutputFunnyConverter
    {
        private readonly Type _clrType;
        private readonly IOutputFunnyConverter _elementConverter;

        public ClrArrayOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter)
        {
            _clrType = clrType;
            _elementConverter = elementConverter;
            FunnyType = VarType.ArrayOf(elementConverter.FunnyType);
        }
        public VarType FunnyType { get; }
        public object ToClrObject(object funObject)
        {
            var funArray = (funObject as IFunArray);
            var clrArray = Array.CreateInstance(_clrType.GetElementType(), funArray.Count);
            for (int i = 0; i < funArray.Count; i++)
            {
                var item = _elementConverter.ToClrObject(funArray.GetElementOrNull(i));
                clrArray.SetValue(item,i);
            }
            return clrArray;

        }
    }
    public class StructOutputFunnyConverter: IOutputFunnyConverter
    {
        private readonly Type _clrType;
        private readonly (string, IOutputFunnyConverter, PropertyInfo)[] _propertiesConverters;
        private readonly int _writePropertiesCount;

        public StructOutputFunnyConverter(
            Type clrType,
            (string, IOutputFunnyConverter, PropertyInfo)[] propertiesConverters,
            int writePropertiesCount)
        {
            _clrType = clrType;
            _propertiesConverters = propertiesConverters;
            _writePropertiesCount = writePropertiesCount;
            (string, VarType)[] fieldTypes = new (string, VarType)[_writePropertiesCount];
            for (int i = 0; i < writePropertiesCount; i++)
            {
                var converter = propertiesConverters[i];
                fieldTypes[i] = (converter.Item1, converter.Item2.FunnyType);
            }
            FunnyType = VarType.StructOf(fieldTypes);
        }
        public VarType FunnyType { get; }
        public object ToClrObject(object funObject)
        {
            var clrObj = Activator.CreateInstance(_clrType);
            var str = funObject as FunnyStruct;
            foreach (var property in _propertiesConverters)
            {
                var (name, converter, info) = property;
                
                var funValue = str.GetValue(name);
                var clrValue = converter.ToClrObject(funValue);
                info.SetValue(clrObj,clrValue);
            }
            return clrObj;
        }
    }
}