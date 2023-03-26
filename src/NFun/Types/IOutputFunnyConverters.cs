using System;
using System.Collections.Generic;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types;

public interface IOutputFunnyConverter {
    public Type ClrType { get; }
    public FunnyType FunnyType { get; }
    public object ToClrObject(object funObject);
}

public class DynamicTypeOutputFunnyConverter : IOutputFunnyConverter {
    private readonly FunnyConverter _behaviour;
    public static DynamicTypeOutputFunnyConverter AnyConverter { get; } = new(typeof(object), FunnyConverter.RealIsDouble);

    public DynamicTypeOutputFunnyConverter(Type clrType, FunnyConverter typeBehaviour) {
        _behaviour = typeBehaviour;
        ClrType = clrType;
    }
    public Type ClrType { get; }
    public FunnyType FunnyType => FunnyType.Any;

    public object ToClrObject(object funObject) =>
        funObject switch {
            TextFunnyArray txt => txt.ToString(),
            IFunnyArray funArray =>
                _behaviour
                    .GetOutputConverterFor(FunnyType.ArrayOf(funArray.ElementType))
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
    public FunnyType FunnyType => FunnyType.Real;
    public object ToClrObject(object funObject) => (float)(Decimal)funObject;
}

public class DecimalToDoubleTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(float);
    public FunnyType FunnyType => FunnyType.Real;
    public object ToClrObject(object funObject) => (double)(Decimal)funObject;
}

public class DoubleToFloatTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(float);
    public FunnyType FunnyType => FunnyType.Real;
    public object ToClrObject(object funObject) => (float)(double)funObject;
}

public class DoubleToDecimalTypeOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(Decimal);
    public FunnyType FunnyType => FunnyType.Real;

    public object ToClrObject(object funObject) => new Decimal((double)funObject);
}

public class StringOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    public FunnyType FunnyType => FunnyType.Text;
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

internal class StructOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly OutputProperty[] _properties;

    public StructOutputFunnyConverter(
        Type clrType,
        OutputProperty[] properties,
        int writePropertiesCount) {
        ClrType = clrType;
        _properties = properties;
        var fieldTypes = new (string, FunnyType)[writePropertiesCount];
        for (int i = 0; i < writePropertiesCount; i++)
        {
            var property = properties[i];
            fieldTypes[i] = (property.PropertyName,  property.Converter.FunnyType);
        }
        // output allows default values
        FunnyType = FunnyType.StructOf(isFrozen: true, allowDefaultValues: true, fieldTypes);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var clrObj = Activator.CreateInstance(ClrType);
        var str = funObject as FunnyStruct;
        if (FunnyType.StructTypeSpecification.AllowDefaultValues)
        {
            foreach (var property in _properties)
            {
                if (str.TryGetValue(property.PropertyName, out var funValue))
                    property.SetValueToTargetProperty(funValue, clrObj);
            }
        }
        else
        {
            foreach (var property in _properties)
            {
                var funValue = str.GetValue(property.PropertyName);
                property.SetValueToTargetProperty(funValue, clrObj);
            }
        }

        return clrObj;
    }
}

public class StructToDictionaryOutputFunnyConverter : IOutputFunnyConverter {
    private readonly FunnyConverter _funnyConverter;
    public StructToDictionaryOutputFunnyConverter(FunnyConverter funnyConverter, FunnyType type) {
        _funnyConverter = funnyConverter;
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
            var converter = _funnyConverter.GetOutputConverterFor(property.Value);
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
        FunnyType.StructOf(new StructTypeSpecification(
            capacity: 0,
            isFrozen:false,
            allowDefaultValues:false));

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
