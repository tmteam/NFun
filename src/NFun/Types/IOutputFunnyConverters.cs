using System;
using System.Reflection;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    public interface IOutputFunnyConverter
    {
        public Type ClrType { get; }
        public VarType FunnyType { get; }
        public object ToClrObject(object funObject);
    }

    public class PrimitiveTypeOutputFunnyConverter:IOutputFunnyConverter {
        public Type ClrType { get; }
        public VarType FunnyType { get;  }
        public PrimitiveTypeOutputFunnyConverter(VarType funnyType, Type clrType)
        {
            FunnyType = funnyType;
            ClrType = clrType;
        }

        public object ToClrObject(object funObject) => funObject;
    }

    public class StringOutputFunnyConverter : IOutputFunnyConverter {
        public Type ClrType { get; }
        public VarType FunnyType { get; } = VarType.Text;
        public StringOutputFunnyConverter() => ClrType = typeof(string);
        public object ToClrObject(object funObject) => (funObject as IFunArray).ToText();
    }

    public class ClrArrayOutputFunnyConverter : IOutputFunnyConverter
    {
        public Type ClrType { get; }
        private readonly IOutputFunnyConverter _elementConverter;
        public ClrArrayOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter)
        {
            ClrType = clrType;
            _elementConverter = elementConverter;
            FunnyType = VarType.ArrayOf(elementConverter.FunnyType);
        }
        public VarType FunnyType { get; }
        public object ToClrObject(object funObject)
        {
            var funArray = (funObject as IFunArray);
            var clrArray = Array.CreateInstance(ClrType.GetElementType(), funArray.Count);
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
        public Type ClrType { get; }
        private readonly (string, IOutputFunnyConverter, PropertyInfo)[] _propertiesConverters;
        private readonly int _writePropertiesCount;
        public StructOutputFunnyConverter(
            Type clrType,
            (string, IOutputFunnyConverter, PropertyInfo)[] propertiesConverters,
            int writePropertiesCount)
        {
            ClrType = clrType;
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
            var clrObj = Activator.CreateInstance(ClrType);
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