namespace NFun.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Runtime;
using Runtime.Arrays;
using Types;

public class Serializer {
    public static string Serialize(object value) {
        var clrType = value.GetType();
        var converter = FunnyConverter.RealIsDouble.GetInputConverterFor(clrType);
        var funObj = converter.ToFunObject(value);
        var serializer = GetSerializer(converter.FunnyType);
        var builder = new StringBuilder();

        serializer.Serialize(funObj, builder, new FunnyFormatterSettings("    "), 0);
        return builder.ToString();
    }

    private static IFunnySerializer GetSerializer(FunnyType type) {
        switch (type.BaseType)
        {
            case BaseFunnyType.Char:
            case BaseFunnyType.UInt8:
            case BaseFunnyType.UInt16:
            case BaseFunnyType.UInt32:
            case BaseFunnyType.UInt64:
            case BaseFunnyType.Int16:
            case BaseFunnyType.Int32:
            case BaseFunnyType.Int64:
            case BaseFunnyType.Real:
            case BaseFunnyType.Any:
                return new PrimitiveFunnySerializer();
            case BaseFunnyType.Bool:
                return new BoolFunnySerializer();
            case BaseFunnyType.Ip:
                return new IpFunnySerializer();
            case BaseFunnyType.ArrayOf:
                if (type.IsText)
                    return new TextFunnySerializer();
                var elementTypeSerializer = GetSerializer(type.ArrayTypeSpecification.FunnyType);
                return new ArrayFunnySerializer(elementTypeSerializer);
            case BaseFunnyType.Struct:
                return new StructFunnySerializer(
                    type.StructTypeSpecification.ToDictionary((kv) => kv.Key, (kv) => GetSerializer(kv.Value)));
            default:
                throw new ArgumentOutOfRangeException(type.BaseType.ToString());
        }
    }
}

public interface IFunnySerializer {
    void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel);
}

public class PrimitiveFunnySerializer : IFunnySerializer {
    public void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel)
        => builder.Append(value);
}

public class BoolFunnySerializer : IFunnySerializer {
    public void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel) {
        builder.Append(
            value.Equals(true) ? "true" : "false");
    }
}

public class PrimitiveStringSerializer : IFunnySerializer {
    public void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel)
        => builder.Append(value);
}

public class ArrayFunnySerializer : IFunnySerializer {
    private readonly IFunnySerializer _elementSerializer;

    public ArrayFunnySerializer(IFunnySerializer elementSerializer) {
        _elementSerializer = elementSerializer;
    }

    public void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel) {
        builder.Append('[');
        var array = (IFunnyArray)value;

        var items = new List<string>();
        var len = 0;
        var aSize = 0;
        var hasNewLine = false;
        foreach (var e in array)
        {
            var b = new StringBuilder();
            _elementSerializer.Serialize(e, b, settings, indentLevel + 1);
            var serialized = b.ToString();
            items.Add(serialized);
            len += serialized.Length;
            aSize++;
            if (!hasNewLine)
                hasNewLine = serialized.Contains('\n');
        }

        if (aSize == 0)
            builder.Append(']');
        else if (!hasNewLine && (aSize == 1 || len < settings.WrapSize))
        {
            var i = 0;
            foreach (var item in items)
            {
                i++;
                builder.Append(item);
                if (i < aSize)
                    builder.Append(", ");
            }

            builder.Append(']');
        }
        else
        {
            var i = 0;
            foreach (var item in items)
            {
                i++;
                builder.AppendLine(item, settings, indentLevel+1);
                if (i < aSize)
                    builder.Append(",");
            }

            builder.AppendLine("]", settings, indentLevel);
        }
    }
}

public class IpFunnySerializer : IFunnySerializer {
    public void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel) {
        var ip = (IPAddress)value;
        var bytes = ip.MapToIPv4().GetAddressBytes();
        builder.Append(bytes[0]);
        builder.Append('.');
        builder.Append(bytes[1]);
        builder.Append('.');
        builder.Append(bytes[2]);
        builder.Append('.');
        builder.Append(bytes[3]);
    }
}

public class TextFunnySerializer : IFunnySerializer {
    public void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel) {
        var str = (value as IFunnyArray)?.ToText() ?? value.ToString();
        str = str.Replace("'", "\\\'");
        builder.Append("'" + str + "'");
    }
}

public class StructFunnySerializer : IFunnySerializer {
    private readonly Dictionary<string, IFunnySerializer> _elementSerializers;

    public StructFunnySerializer(Dictionary<string, IFunnySerializer> elementSerializers) {
        _elementSerializers = elementSerializers;
    }

    public void Serialize(object value, StringBuilder builder, FunnyFormatterSettings settings, int indentLevel) {
        builder.Append('{');
        var structure = (FunnyStruct)value;

        var items = new List<(string, string)>();
        var len = 0;
        var hasNewLine = false;
        foreach (var (k, v) in structure)
        {
            var serializer = _elementSerializers[k];
            var b = new StringBuilder();
            serializer.Serialize(v, b, settings, indentLevel + 1);
            var serialized = b.ToString();
            items.Add((k, serialized));
            len += k.Length;
            len += serialized.Length;
            len += 3;
            if (!hasNewLine)
                hasNewLine = serialized.Contains('\n');
        }

        if (structure.Count == 0)
            builder.Append('}');
        else if (!hasNewLine && (structure.Count == 1 || len < settings.WrapSize))
        {
            var i = 0;
            foreach (var item in items)
            {
                i++;
                builder.Append(item.Item1 + "= " + item.Item2);
                if (i < structure.Count)
                    builder.Append(", ");
            }

            builder.Append('}');
        }
        else
        {
            foreach (var item in items)
                builder.AppendLine(item.Item1 + " = " + item.Item2, settings, indentLevel+1);
            builder.AppendLine("}", settings, indentLevel);
        }
    }
}

public static class SerializerHelper {
    public static void AppendLine(
        this StringBuilder builder,
        string value,
        FunnyFormatterSettings settings,
        int indentLevel) {
        builder.AppendLine();
        for (int i = 0; i < indentLevel; i++) builder.Append(settings.IndentType);
        builder.Append(value);
    }
}

public class FunnyFormatterSettings {
    public string IndentType { get; }
    internal int WrapSize { get; } = 20;

    public FunnyFormatterSettings(string indentType) {
        IndentType = indentType;
    }
}
