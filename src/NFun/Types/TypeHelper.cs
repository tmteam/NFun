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
            bool b               => b ? "true" : "false",
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
        // Asymmetric guard: left non-array AND right array → unequal. Without this, a
        // numeric/char value silently passed where the TIC inferred Char[] (e.g.
        // `rule it == 'a'` over a Char[]) would be ToText-coerced into a 1-char text
        // and compared as arrays, giving the wrong `true`. Equality compares actual
        // runtime values; cross-family casts have no place here. (Bug CC.)
        if (right is IFunnyArray)
            return false;
        // Numeric cross-type promotion: `1 == 1L`, `1 == 1.0`, `1:u8 == 1:i32`.
        // Doubles need IEEE 754 NaN semantics (NaN != NaN); other numerics widen
        // through double. Precision loss above 2^53 is the documented edge case for
        // int64 cross-equality but is rare in practice.
        if (IsNumeric(left) && IsNumeric(right)) {
            if (left is double dl && right is double dr) return dl == dr;
            return Convert.ToDouble(left, CultureInfo.InvariantCulture)
                == Convert.ToDouble(right, CultureInfo.InvariantCulture);
        }
        return left.GetType() == right.GetType() && left.Equals(right);
    }

    private static bool IsNumeric(object o) =>
        o is sbyte or byte or short or ushort or int or uint or long or ulong or float or double;

    public static T[] ToArrayOf<T>(this IFunnyArray a) => a.As<T>().ToArray(a.Count);
    
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