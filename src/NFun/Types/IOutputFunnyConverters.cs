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
            FunnyNone       => null,
            TextFunnyArray txt => txt.ToString(),
            IFunnyArray funArray =>
                _behaviour
                    .GetOutputConverterFor(FunnyType.ArrayOf(funArray.ElementType))
                    .ToClrObject(funObject),
            // Lang-mode containers: rebuild the funny type from the runtime value's
            // element type and reuse the per-type converters. Without these arms the
            // default case leaked RAW FixedFunnyArray/MutableFunnyList/… into the
            // untyped Api output (`ids.map(rule it.toText())` returned a funny array
            // of char-arrays instead of string[]).
            NFun.Runtime.Lists.MutableFunnyMap map =>
                _behaviour
                    .GetOutputConverterFor(FunnyType.MapOf(map.KeyType, map.ValueType))
                    .ToClrObject(funObject),
            NFun.Runtime.Lists.IFunnyFixedArray fixedArr =>
                _behaviour
                    .GetOutputConverterFor(FunnyType.FixedArrayOf(fixedArr.ElementType))
                    .ToClrObject(funObject),
            NFun.Runtime.Lists.IFunnyList list =>
                _behaviour
                    .GetOutputConverterFor(FunnyType.ListOf(list.ElementType))
                    .ToClrObject(funObject),
            NFun.Runtime.Lists.IFunnyMutableArray mutArr =>
                _behaviour
                    .GetOutputConverterFor(FunnyType.MutableArrayOf(mutArr.ElementType))
                    .ToClrObject(funObject),
            NFun.Runtime.Lists.IFunnyMutableSet set =>
                _behaviour
                    .GetOutputConverterFor(FunnyType.SetOf(set.ElementType))
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

/// <summary>
/// CLR-output converter for lang-mode <c>fixedArray&lt;char&gt;</c> — the
/// text shape produced by constructive LINQ on text (e.g. `s.reverse()`).
/// Returns a <see cref="string"/>.
/// </summary>
public class FixedArrayCharToStringOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    public FunnyType FunnyType => FunnyType.FixedArrayOf(FunnyType.Char);
    public FixedArrayCharToStringOutputFunnyConverter() => ClrType = typeof(string);
    public object ToClrObject(object funObject) {
        var enumerable = (NFun.Runtime.Lists.IFunnyEnumerable)funObject;
        var sb = new System.Text.StringBuilder(enumerable.Count);
        foreach (var c in enumerable) sb.Append((char)c);
        return sb.ToString();
    }
}

/// <summary>
/// CLR-output converter for lang-mode <c>list&lt;T&gt;</c>.
/// Takes an <see cref="NFun.Runtime.Lists.IFunnyList"/> produced by the runtime
/// and returns a <c>System.Collections.Generic.List&lt;T&gt;</c>.
/// </summary>
public class ClrListOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly IOutputFunnyConverter _elementConverter;
    private readonly Type _elementClrType;

    public ClrListOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter) {
        ClrType = clrType;
        _elementConverter = elementConverter;
        _elementClrType = elementConverter.ClrType;
        FunnyType = FunnyType.ListOf(elementConverter.FunnyType);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var funList = (NFun.Runtime.Lists.IFunnyList)funObject;
        // Build the closed System.Collections.Generic.List<T> via reflection so
        // the converter is element-type-agnostic (mirrors ClrArrayOutputFunnyConverter).
        var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(_elementClrType);
        var clrList = (System.Collections.IList)Activator.CreateInstance(listType, funList.Count);
        for (int i = 0; i < funList.Count; i++) {
            var item = _elementConverter.ToClrObject(funList.GetElementOrNull(i));
            clrList.Add(item);
        }
        return clrList;
    }
}

/// <summary>
/// CLR-output converter for lang-mode <c>fixedArray&lt;T&gt;</c>. Takes an
/// <see cref="NFun.Runtime.Lists.IFunnyFixedArray"/> and returns a CLR <c>T[]</c>.
/// </summary>
public class ClrFixedArrayOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly IOutputFunnyConverter _elementConverter;

    public ClrFixedArrayOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter) {
        ClrType = clrType;
        _elementConverter = elementConverter;
        FunnyType = FunnyType.FixedArrayOf(elementConverter.FunnyType);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var funArr = (NFun.Runtime.Lists.IFunnyFixedArray)funObject;
        var clrArray = Array.CreateInstance(ClrType.GetElementType(), funArr.Count);
        for (int i = 0; i < funArr.Count; i++) {
            var item = _elementConverter.ToClrObject(funArr.GetElementOrNull(i));
            clrArray.SetValue(item, i);
        }
        return clrArray;
    }
}

/// <summary>
/// CLR-output converter for lang-mode <c>array&lt;T&gt;</c>. Takes an
/// <see cref="NFun.Runtime.Lists.IFunnyMutableArray"/> and returns a
/// CLR <c>T[]</c>.
/// </summary>
public class ClrMutableArrayOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly IOutputFunnyConverter _elementConverter;

    public ClrMutableArrayOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter) {
        ClrType = clrType;
        _elementConverter = elementConverter;
        FunnyType = FunnyType.MutableArrayOf(elementConverter.FunnyType);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var funArr = (NFun.Runtime.Lists.IFunnyMutableArray)funObject;
        var clrArray = Array.CreateInstance(ClrType.GetElementType(), funArr.Count);
        for (int i = 0; i < funArr.Count; i++) {
            var item = _elementConverter.ToClrObject(funArr.GetElementOrNull(i));
            clrArray.SetValue(item, i);
        }
        return clrArray;
    }
}

/// <summary>
/// CLR-output converter for lang-mode <c>map&lt;K, V&gt;</c>. Takes an
/// <see cref="NFun.Runtime.Lists.IFunnyMap"/> and returns a CLR
/// <c>Dictionary&lt;K, V&gt;</c>.
/// </summary>
public class ClrDictionaryOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly IOutputFunnyConverter _keyConverter;
    private readonly IOutputFunnyConverter _valueConverter;

    public ClrDictionaryOutputFunnyConverter(Type clrType,
        IOutputFunnyConverter keyConverter, IOutputFunnyConverter valueConverter) {
        ClrType = clrType;
        _keyConverter = keyConverter;
        _valueConverter = valueConverter;
        FunnyType = FunnyType.MapOf(keyConverter.FunnyType, valueConverter.FunnyType);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var funMap = (NFun.Runtime.Lists.IFunnyMap)funObject;
        var clrDict = (System.Collections.IDictionary)Activator.CreateInstance(ClrType);
        // Iterate via IFunnyMap directly through its underlying dictionary —
        // GetEnumerator yields FunnyStruct pairs (Enumerable interop), which is
        // not what we want here. Walk via Count + IFunnyMap-specific API would
        // require an entries property; for now go through the enumerable
        // pair-struct form and unpack each.
        foreach (var pair in (System.Collections.Generic.IEnumerable<object>)funMap) {
            var s = (NFun.Runtime.FunnyStruct)pair;
            var k = _keyConverter.ToClrObject(s.GetValue("key"));
            var v = _valueConverter.ToClrObject(s.GetValue("value"));
            clrDict[k] = v;
        }
        return clrDict;
    }
}

/// <summary>
/// CLR-output converter for lang-mode <c>set&lt;T&gt;</c>. Takes an
/// <see cref="NFun.Runtime.Lists.IFunnyMutableSet"/> and returns a CLR
/// <c>HashSet&lt;T&gt;</c>.
/// </summary>
public class ClrHashSetOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; }
    private readonly IOutputFunnyConverter _elementConverter;

    public ClrHashSetOutputFunnyConverter(Type clrType, IOutputFunnyConverter elementConverter) {
        ClrType = clrType;
        _elementConverter = elementConverter;
        FunnyType = FunnyType.SetOf(elementConverter.FunnyType);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var funSet = (NFun.Runtime.Lists.IFunnyMutableSet)funObject;
        var clrSet = (System.Collections.IEnumerable)Activator.CreateInstance(ClrType);
        var addMethod = ClrType.GetMethod("Add", new[] { _elementConverter.ClrType });
        var addArgs = new object[1];
        foreach (var item in funSet) {
            addArgs[0] = _elementConverter.ToClrObject(item);
            addMethod.Invoke(clrSet, addArgs);
        }
        return clrSet;
    }
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
            try {
                clrArray.SetValue(item, i);
            }
            catch (InvalidCastException ex) {
                // Raw .NET InvalidCastException leaks an implementation detail.
                // Happens when TIC inferred an array-of-T but the runtime produced
                // an array-of-something-else — e.g. a recursive fn whose return-type
                // LCA has no finite fixed point (`fun f(x): if x==0: return [0];
                // return [f(x-1)]` — `T = [T]` has no solution; TIC collapses to
                // `Int32[]` but values are nested arrays). Surface as a clean
                // FunnyRuntimeException (BugHunt-stmt #68).
                throw new Exceptions.FunnyRuntimeException(
                    $"Cannot store value of type '{item?.GetType().Name ?? "null"}' in array of '{ClrType.GetElementType()?.Name}'. "
                    + "This usually means the function's recursive return-type has no concrete solution — "
                    + "annotate the return type explicitly or use a named recursive type.", ex);
            }
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

        FunnyType = FunnyType.StructOf(isFrozen: true, fieldTypes);
    }

    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) {
        var clrObj = Activator.CreateInstance(ClrType);
        var str = funObject as FunnyStruct;
        foreach (var property in _properties)
        {
            var funValue = str.GetValue(property.PropertyName);
            property.SetValueToTargetProperty(funValue, clrObj);
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

public class OptionalOutputFunnyConverter : IOutputFunnyConverter {
    private readonly IOutputFunnyConverter _elementConverter;

    public OptionalOutputFunnyConverter(IOutputFunnyConverter elementConverter) {
        _elementConverter = elementConverter;
        FunnyType = FunnyType.OptionalOf(elementConverter.FunnyType);
        // Value types (int, bool, etc.) can't represent null — use Nullable<T>
        // This ensures arrays like int?[] become Nullable<int>[] which can hold null for FunnyNone
        ClrType = elementConverter.ClrType.IsValueType
            ? typeof(Nullable<>).MakeGenericType(elementConverter.ClrType)
            : elementConverter.ClrType;
    }

    public Type ClrType { get; }
    public FunnyType FunnyType { get; }

    public object ToClrObject(object funObject) =>
        funObject is FunnyNone ? null : _elementConverter.ToClrObject(funObject);
}

public class NoneOutputFunnyConverter : IOutputFunnyConverter {
    public Type ClrType { get; } = typeof(object);
    public FunnyType FunnyType => FunnyType.None;
    public object ToClrObject(object funObject) => null;
}

/// <summary>
/// Output converter for function-typed variables.
/// Function values are IConcreteFunction at runtime — pass through as-is.
/// </summary>
public class FunOutputFunnyConverter : IOutputFunnyConverter {
    public FunOutputFunnyConverter(FunnyType funnyType) => FunnyType = funnyType;
    public Type ClrType { get; } = typeof(object);
    public FunnyType FunnyType { get; }
    public object ToClrObject(object funObject) => funObject;
}

public class DynamicStructToDictionaryOutputFunnyConverter : IOutputFunnyConverter {
    public static DynamicStructToDictionaryOutputFunnyConverter Instance { get; } = new();

    private DynamicStructToDictionaryOutputFunnyConverter() { }

    public Type ClrType { get; } = typeof(Dictionary<string, object>);

    public FunnyType FunnyType { get; } =
        FunnyType.StructOf(new StructTypeSpecification(0, isFrozen:false));

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
