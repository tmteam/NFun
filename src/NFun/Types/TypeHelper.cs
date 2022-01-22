using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using NFun.Runtime.Arrays;

namespace NFun.Types {

internal static class TypeHelper {
    static TypeHelper() {
        FunToClrTypesMap = new[] {
            null,
            typeof(char),
            typeof(bool),
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
            typeof(short),
            typeof(int),
            typeof(long),
            typeof(double),
            null,
            null,
            null,
            typeof(object),
            null
        };

        DefaultPrimitiveValues = new[] {
            null,
            default(char),
            default(bool),
            default(byte),
            default(ushort),
            default(uint),
            default(ulong),
            default(short),
            default(int),
            default(long),
            default(double),
            null,
            null,
            null,
            new object(),
            null
        };
    }

    private static readonly object[] DefaultPrimitiveValues;
    private static readonly Type[] FunToClrTypesMap;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Type GetClrType(this BaseFunnyType funnyType) => FunToClrTypesMap[(int)funnyType];

    internal static string GetFunSignature<T>(string name, T returnType, IEnumerable<T> arguments)
        => name + "(" + string.Join(",", arguments) + "):" + returnType;

    internal static string GetFunSignature<T>(T returnType, IEnumerable<T> arguments)
        => "(" + string.Join(",", arguments) + "):" + returnType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string GetFunText(object obj) =>
        obj switch {
            IFunnyArray funArray => funArray.ToText(),
            double dbl           => dbl.ToString(CultureInfo.InvariantCulture),
            _                    => obj.ToString()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CanBeUsedAsFunnyInputProperty(this PropertyInfo property) =>
        property.CanRead && property.GetMethod.Attributes.HasFlag(MethodAttributes.Public);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CanBeUsedAsFunnyOutputProperty(this PropertyInfo property) =>
        property.CanWrite && property.SetMethod.Attributes.HasFlag(MethodAttributes.Public);

    internal static object GetDefaultValueOrNull(this FunnyType type) {
        var defaultValue = DefaultPrimitiveValues[(int)type.BaseType];
        if (defaultValue != null)
            return defaultValue;
        if (type.ArrayTypeSpecification == null)
            return null;

        var arr = type.ArrayTypeSpecification;
        if (arr.FunnyType.BaseType == BaseFunnyType.Char)
            return TextFunnyArray.Empty;
        return new ImmutableFunnyArray(Array.Empty<object>(), arr.FunnyType);
    }

    public static bool AreEqual(object left, object right) {
        if (left is IFunnyArray le)
        {
            return right is IFunnyArray re && AreEquivalent(le, re);
        }

        return left.GetType() == right.GetType() && left.Equals(right);
    }

    public static bool AreEquivalent(IFunnyArray a, IFunnyArray b) {
        if (a.Count != b.Count)
            return false;
        if (a.Count == 0)
            return true;
        for (int i = 0; i < a.Count; i++)
        {
            var elementA = a.GetElementOrNull(i);
            var elementB = b.GetElementOrNull(i);
            if (!AreEqual(elementA, elementB))
                return false;
        }

        return true;
    }
    
}

}