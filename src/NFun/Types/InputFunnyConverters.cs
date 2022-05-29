using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NFun.Exceptions;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;

namespace NFun.Types {

/// <summary>
/// Converts CLR type and value into NFun type and value
/// </summary>
public interface IInputFunnyConverter {
    public FunnyType FunnyType { get; }
    public object ToFunObject(object clrObject);
}

public class PrimitiveTypeInputFunnyConverter : IInputFunnyConverter {
    public FunnyType FunnyType { get; }
    public PrimitiveTypeInputFunnyConverter(FunnyType funnyType) => FunnyType = funnyType;
    public object ToFunObject(object clrObject) => clrObject;
}

public class StringTypeInputFunnyConverter : IInputFunnyConverter {
    public static readonly StringTypeInputFunnyConverter Instance = new();
    public FunnyType FunnyType { get; }
    private StringTypeInputFunnyConverter() => FunnyType = FunnyType.Text;

    public object ToFunObject(object clrObject)
        => clrObject == null
            ? TextFunnyArray.Empty
            : new TextFunnyArray(clrObject.ToString());
}

public class FloatToDoubleInputFunnyConverter : IInputFunnyConverter {
    public FunnyType FunnyType => FunnyType.Real;
    public object ToFunObject(object clrObject) =>  (double)(float)clrObject;
}

public class FloatToDecimalInputFunnyConverter : IInputFunnyConverter {
    public FunnyType FunnyType => FunnyType.Real;
    public object ToFunObject(object clrObject) =>  (Decimal)(float)clrObject;
}


public class DecimalToDoubleInputFunnyConverter : IInputFunnyConverter {
    public FunnyType FunnyType => FunnyType.Real;
    public object ToFunObject(object clrObject) =>  decimal.ToDouble((Decimal)clrObject);
}

public class DoubleToDecimalInputFunnyConverter : IInputFunnyConverter {
    public FunnyType FunnyType { get; } = FunnyType.Real;
    public object ToFunObject(object clrObject) =>  (Decimal)(double)clrObject;
}

public class StructTypeInputFunnyConverter : IInputFunnyConverter {
    private readonly InputProperty[] _properties;
    private readonly int _readPropertiesCount;

    internal StructTypeInputFunnyConverter(
        InputProperty[] properties,
        int readPropertiesCount, FunnyType? type = null) {
        _properties = properties;
        _readPropertiesCount = readPropertiesCount;

        if (type == null)
        {
            (string, FunnyType)[] fieldTypes = new (string, FunnyType)[_readPropertiesCount];
            for (int i = 0; i < readPropertiesCount; i++)
            {
                var property = properties[i];
                fieldTypes[i] = (property.PropertyName, property.Converter.FunnyType);
            }

            FunnyType = FunnyType.StructOf(fieldTypes);
        }
        else
        {
            FunnyType = type.Value;
        }
    }

    public FunnyType FunnyType { get; }

    public object ToFunObject(object clrObject) {
        var values = new FunnyStruct.FieldsDictionary(_properties.Length);
        if (clrObject == null)
            return FunnyType.GetDefaultFunnyValue();
        
        for (var i = 0; i < _readPropertiesCount; i++)
        {
            var property = _properties[i];
            values.Add(property.PropertyName, property.GetFunValue(clrObject));
        }

        return new FunnyStruct(values);
    }
}

public class DynamicStructTypeInputFunnyConverter : IInputFunnyConverter {
    private readonly (string, IInputFunnyConverter)[] _propertiesConverters;

    internal DynamicStructTypeInputFunnyConverter((string, IInputFunnyConverter)[] properties, FunnyType funnyType) {
        _propertiesConverters = properties;
        FunnyType = funnyType;
    }

    public FunnyType FunnyType { get; }

    public object ToFunObject(object clrObject) {
        if (clrObject is IDictionary<string, object> dic)
            return ConvertFromDictionary(dic);
        else
            return ConvertFromSomeClrObject(clrObject);
    }

    private object ConvertFromSomeClrObject(object clrObject) {
        if (clrObject == null)
            throw new InvalidCastException($"Expected: value for {FunnyType} but was null");

        //dynamic convertion is very slow but it is only on option
        var properties = clrObject.GetType().GetProperties();
        var values = new FunnyStruct.FieldsDictionary(_propertiesConverters.Length);

        foreach (var (name, converter) in _propertiesConverters)
        {
            var property = properties.FirstOrDefault(
                p =>
                    p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                    p.HasPublicGetter());
            if (property == null)
                throw new InvalidCastException(
                    $"Type {clrObject.GetType().Name} cannot be used for struct, as it contains no readable public property {name}");
            var value = property.GetValue(clrObject);
            values.Add(name, converter.ToFunObject(value));
        }

        return new FunnyStruct(values);
    }

    private object ConvertFromDictionary(IDictionary<string, object> dic) {
        var values = new FunnyStruct.FieldsDictionary(_propertiesConverters.Length);
        foreach (var (name, converter) in _propertiesConverters)
        {
            if (!dic.TryGetValue(name, out var value))
                throw new InvalidCastException($"field {name} is missing in input dictionary");
            if (value == null)
                throw new InvalidCastException($"field {name} is null");
            values.Add(name, converter.ToFunObject(value));
        }

        return new FunnyStruct(values);
    }
}

public class ClrArrayInputTypeFunnyConverter : IInputFunnyConverter {
    private readonly IInputFunnyConverter _elementConverter;

    public ClrArrayInputTypeFunnyConverter(IInputFunnyConverter elementConverter) {
        FunnyType = FunnyType.ArrayOf(elementConverter.FunnyType);
        _elementConverter = elementConverter;
    }

    public FunnyType FunnyType { get; }

    public object ToFunObject(object clrObject) {
        switch (clrObject)
        {
            case Array array:
            {
                var funnyObjects = new object[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    var val = array.GetValue(i);
                    funnyObjects[i] = _elementConverter.ToFunObject(val);
                }

                return new ImmutableFunnyArray(funnyObjects, FunnyType.ArrayTypeSpecification.FunnyType);
            }
            case string str:
                return new ImmutableFunnyArray(str.ToCharArray(), FunnyType.ArrayTypeSpecification.FunnyType);
            default:
                throw FunnyInvalidUsageException.InputTypeCannotBeConverted(clrObject.GetType(), FunnyType);
        }
    }
}

}