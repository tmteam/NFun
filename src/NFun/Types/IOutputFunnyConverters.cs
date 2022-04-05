using System;
using System.Collections.Generic;
using System.Reflection;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types {

public interface IOutputFunnyConverter {
    public Type ClrType { get; }
    public FunnyType FunnyType { get; }
    public object ToClrObject(object funObject);
}

public class DynamicTypeOutputFunnyConverter : IOutputFunnyConverter {
    private readonly TypeBehaviour _behaviour;
    public static DynamicTypeOutputFunnyConverter AnyConverter { get; } = new(typeof(object), TypeBehaviour.RealIsDoubleWithIntOverflow);

    public DynamicTypeOutputFunnyConverter(Type clrType, TypeBehaviour _behaviour) {
        this._behaviour = _behaviour;
        ClrType = clrType;
    }
    public Type ClrType { get; }
    public FunnyType FunnyType => FunnyType.Any;

    public object ToClrObject(object funObject) =>
        funObject switch {
            TextFunnyArray txt => txt.ToString(),
            IFunnyArray funArray =>
                TypeBehaviourExtensions
                    .GetOutputConverterFor(_behaviour, FunnyType.ArrayOf(funArray.ElementType))
                    .ToClrObject(funObject),
            FunnyStruct str => DynamicStructToDictionaryOutputFunnyConverter.Instance.ToClrObject(str),
            _               => funObject
        };
}

public class PrimitiveTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    public FunnyType FunnyType { get; }

    public PrimitiveTypeOutputFunnyConverter(FunnyType funnyType, Type clrType) {
        FunnyType = funnyType;
        ClrType = clrType;
    }

    public object ToClrObject(object funObject) => funObject;
}

public class DecimalToFloatTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(float);
    public FunnyType FunnyType { get; } = FunnyType.Real;
    public object ToClrObject(object funObject) => (float)(Decimal)funObject;
}

public class DecimalToDoubleTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(float);
    public FunnyType FunnyType { get; } = FunnyType.Real;
    public object ToClrObject(object funObject) => (double)(Decimal)funObject;
}

public class DoubleToFloatTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(float);
    public FunnyType FunnyType { get; } = FunnyType.Real;
    public object ToClrObject(object funObject) => (float)(double)funObject;
}


public class DoubleToDecimalTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(Decimal);
    public FunnyType FunnyType { get; } = FunnyType.Real;

    public object ToClrObject(object funObject) => new Decimal((double)funObject);
}

public class StringOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    public FunnyType FunnyType { get; } = FunnyType.Text;
    public StringOutputFunnyConverter() => ClrType = typeof(string);
    public object ToClrObject(object funObject) => ((IFunnyArray)funObject).ToText();
}

public class ClrArrayOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly IOutputFunnyConverter _elementConverter;

    public ClrArrayOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter) {
        ClrType = clrType;
        _elementConverter = elementConverter;
        FunnyType = FunnyType.ArrayOf(elementConverter.FunnyType);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var funArray = funObject as IFunnyArray;
        var clrArray = Array.CreateInstance(ClrType.GetElementType(), funArray.Count);
        for (int i = 0; i < funArray.Count; i++)
        {
            var item = _elementConverter.ToClrObject(funArray.GetElementOrNull(i));
            clrArray.SetValue(item, i);
        }

        return clrArray;
    }
}

public class StructOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly (string, IOutputFunnyConverter, PropertyInfo)[] _propertiesConverters;

    public StructOutputFunnyConverter(
        Type clrType,
        (string, IOutputFunnyConverter, PropertyInfo)[] propertiesConverters,
        int writePropertiesCount) {
        ClrType = clrType;
        _propertiesConverters = propertiesConverters;
        var fieldTypes = new (string, FunnyType)[writePropertiesCount];
        for (int i = 0; i < writePropertiesCount; i++)
        {
            var (name, outputFunnyConverter, _) = propertiesConverters[i];
            fieldTypes[i] = (name, outputFunnyConverter.FunnyType);
        }

        FunnyType = FunnyType.StructOf(fieldTypes);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var clrObj = Activator.CreateInstance(ClrType);
        var str = funObject as FunnyStruct;
        foreach (var property in _propertiesConverters)
        {
            var (name, converter, info) = property;

            var funValue = str.GetValue(name);
            var clrValue = converter.ToClrObject(funValue);
            info.SetValue(clrObj, clrValue);
        }

        return clrObj;
    }
}

public class StructToDictionaryOutputFunnyConverter : IOutputFunnyConverter {
    private readonly TypeBehaviour _typeBehaviour;
    public StructToDictionaryOutputFunnyConverter(TypeBehaviour typeBehaviour,
        FunnyType type) {
        _typeBehaviour = typeBehaviour;
        FunnyType = type;
    }

    public Type ClrType { get; } = typeof(Dictionary<string, object>);
    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var str = funObject as FunnyStruct;
        var result = new Dictionary<string, object>();
        if (str == null)
            return result;
        foreach (var property in FunnyType.StructTypeSpecification)
        {
            if (!str.TryGetValue(property.Key, out var fieldValue))
                continue;
            var converter = TypeBehaviourExtensions.GetOutputConverterFor(_typeBehaviour, property.Value);
            result.Add(property.Key, converter.ToClrObject(fieldValue));
        }

        return result;
    }
}

public class DynamicStructToDictionaryOutputFunnyConverter : IOutputFunnyConverter {
    public static DynamicStructToDictionaryOutputFunnyConverter Instance { get; } = new();

    private DynamicStructToDictionaryOutputFunnyConverter() { }

    public Type ClrType { get; } = typeof(Dictionary<string, object>);

    public FunnyType FunnyType { get; } =
        FunnyType.StructOf(new Dictionary<string, FunnyType>(0, FunnyType.StructKeyComparer));

    public object ToClrObject(object funObject) {
        var str = funObject as FunnyStruct;
        var result = new Dictionary<string, object>();
        if (str == null)
            return result;
        foreach (var property in str)
        {
            var clrObject = DynamicTypeOutputFunnyConverter.AnyConverter.ToClrObject(property.Value);
            result.Add(property.Key, clrObject);
        }

        return result;
    }
}

}