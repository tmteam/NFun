using System;
using System.Reflection;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types
{
    public interface IOutputFunnyConverter
    {
        public Type ClrType { get; }
        public FunnyType FunnyType { get; }
        public object ToClrObject(object funObject);
    }

    public class DynamicTypeOutputFunnyConverter : IOutputFunnyConverter
    {
        public DynamicTypeOutputFunnyConverter(Type clrType) => ClrType = clrType;

        public Type ClrType { get; }
        public FunnyType FunnyType { get; } = FunnyType.Any;
        public object ToClrObject(object funObject)
        {
            if (funObject is IFunArray funArray)
            {
                if (funObject is TextFunArray txt)
                    return txt.ToString();
                
                var outConverter = FunnyTypeConverters.GetOutputConverter(FunnyType.ArrayOf(funArray.ElementType));
                return outConverter.ToClrObject(funObject);
            }

            if (funObject is FunnyStruct str)
                return str.ToString();
            
            return funObject;
        }
    }
    public class PrimitiveTypeOutputFunnyConverter:IOutputFunnyConverter {
        public Type ClrType { get; }
        public FunnyType FunnyType { get;  }
        public PrimitiveTypeOutputFunnyConverter(FunnyType funnyType, Type clrType)
        {
            FunnyType = funnyType;
            ClrType = clrType;
        }

        public object ToClrObject(object funObject) => funObject;
    }

    public class StringOutputFunnyConverter : IOutputFunnyConverter {
        public Type ClrType { get; }
        public FunnyType FunnyType { get; } = FunnyType.Text;
        public StringOutputFunnyConverter() => ClrType = typeof(string);
        public object ToClrObject(object funObject) => ((IFunArray) funObject).ToText();
    }

    public class ClrArrayOutputFunnyConverter : IOutputFunnyConverter
    {
        public Type ClrType { get; }
        private readonly IOutputFunnyConverter _elementConverter;
        public ClrArrayOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter)
        {
            ClrType = clrType;
            _elementConverter = elementConverter;
            FunnyType = Types.FunnyType.ArrayOf(elementConverter.FunnyType);
        }
        public FunnyType FunnyType { get; }
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

        public StructOutputFunnyConverter(
            Type clrType,
            (string, IOutputFunnyConverter, PropertyInfo)[] propertiesConverters,
            int writePropertiesCount)
        {
            ClrType = clrType;
            _propertiesConverters = propertiesConverters;
            var fieldTypes = new (string, FunnyType)[writePropertiesCount];
            for (int i = 0; i < writePropertiesCount; i++)
            {
                var (name, outputFunnyConverter, _) = propertiesConverters[i];
                fieldTypes[i] = (name, outputFunnyConverter.FunnyType);
            }
            FunnyType = Types.FunnyType.StructOf(fieldTypes);
        }
        public FunnyType FunnyType { get; }
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