using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using NFun.Runtime.Arrays;

namespace NFun.Types; 

internal static class TypeHelper {
    
    internal static string GetFunSignature<T>(string name, T returnType, IEnumerable<T> arguments)
        => name + "(" + string.Join(",", arguments) + "):" + returnType;

    internal static string GetFunSignature<T>(T returnType, IEnumerable<T> arguments)
        => "(" + string.Join(",", arguments) + "):" + returnType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string GetFunText(object obj) =>
        obj switch {
            IFunnyArray funArray => funArray.ToText(),
            double dbl           => dbl.ToString(CultureInfo.InvariantCulture),
            Decimal dec          => dec.ToString(CultureInfo.InvariantCulture),
            _                    => obj.ToString()
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasPublicGetter(this PropertyInfo property) =>
        property.CanRead && property.GetMethod.Attributes.HasFlag(MethodAttributes.Public);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasPublicSetter(this PropertyInfo property) =>
        property.CanWrite && property.SetMethod.Attributes.HasFlag(MethodAttributes.Public);

    public static bool AreEqual(object left, object right) {
        if (left is IFunnyArray le)
            return right is IFunnyArray re && AreEquivalent(le, re);
        else
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