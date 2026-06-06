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
            // Stage C — lang-mode collections render their elements via GetFunText
            // recursively so nested text values (IFunnyArray with Char element) print
            // as joined strings instead of `[c,h,a,r,s]`.
            Runtime.Lists.IFunnyEnumerable funEnum => FormatFunEnumerable(funEnum),
            bool b               => b ? "true" : "false",
            double dbl           => dbl.ToString(CultureInfo.InvariantCulture),
            Decimal dec          => dec.ToString(CultureInfo.InvariantCulture),
            _                    => obj.ToString()
        };

    private static string FormatFunEnumerable(Runtime.Lists.IFunnyEnumerable e) {
        // Text-shape: any IFunnyEnumerable with Char element renders as joined
        // string. Mirrors IFunnyArray.ToText() for lang-mode collections.
        if (e.ElementType.BaseType == BaseFunnyType.Char) {
            var sb = new System.Text.StringBuilder(e.Count);
            foreach (var item in e) sb.Append((char)item);
            return sb.ToString();
        }
        var parts = new List<string>(e.Count);
        foreach (var item in e)
            parts.Add(GetFunText(item));
        return "[" + string.Join(",", parts) + "]";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasPublicGetter(this PropertyInfo property) =>
        property.CanRead && property.GetMethod.Attributes.HasFlag(MethodAttributes.Public);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasPublicSetter(this PropertyInfo property) =>
        property.CanWrite && property.SetMethod.Attributes.HasFlag(MethodAttributes.Public);

    public static bool AreEqual(object left, object right) {
        // Container equality (Stage 0 hierarchy: `List ⊆ Array`). Treat all the
        // collection families (ee IFunnyArray + lang IFunnyEnumerable subclasses)
        // as a single equivalence class so `[1,2,3] == list(1,2,3) == fixedArray(1,2,3)`
        // and assertions across them succeed by value.
        bool leftIsCollection = left is IFunnyArray || left is Runtime.Lists.IFunnyEnumerable;
        bool rightIsCollection = right is IFunnyArray || right is Runtime.Lists.IFunnyEnumerable;
        if (leftIsCollection || rightIsCollection)
            return leftIsCollection && rightIsCollection
                && CollectionsAreEquivalent(left, right);
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

    /// <summary>
    /// Value equality for any pair of collection runtime types. List / Array /
    /// FixedArray compare positionally; Set + Map compare as multisets / by-key
    /// (order-independent). Cross-kind (set vs list) is always false — different
    /// semantics, so equal values cannot coexist.
    /// </summary>
    private static bool CollectionsAreEquivalent(object a, object b) {
        int countA = GetCollectionCount(a);
        int countB = GetCollectionCount(b);
        if (countA != countB) return false;
        if (countA == 0) return true;
        bool aIsSet = a is Runtime.Lists.IFunnyMutableSet;
        bool bIsSet = b is Runtime.Lists.IFunnyMutableSet;
        if (aIsSet != bIsSet) return false;
        if (aIsSet) {
            // Set vs Set — element-set equality, order-independent. Each side
            // hashes independently; cheapest direction: walk the smaller side,
            // check membership in the larger.
            var setB = (Runtime.Lists.IFunnyMutableSet)b;
            foreach (var item in (Runtime.Lists.IFunnyMutableSet)a)
                if (!setB.Contains(item)) return false;
            return true;
        }
        bool aIsMap = a is Runtime.Lists.IFunnyMap;
        bool bIsMap = b is Runtime.Lists.IFunnyMap;
        if (aIsMap != bIsMap) return false;
        if (aIsMap) {
            // Map vs Map — order-independent: for each (k, v) in a, look up k in b
            // and require equal v. Matches MutableFunnyMap.Equals() semantics so
            // operator `==` and method `.Equals()` agree.
            var mapB = (Runtime.Lists.IFunnyMap)b;
            foreach (var entry in (Runtime.Lists.IFunnyMap)a) {
                var pair = (NFun.Runtime.FunnyStruct)entry;
                var k = pair.GetValue("key");
                var v = pair.GetValue("value");
                if (!mapB.ContainsKey(k)) return false;
                if (!AreEqual(v, mapB.GetOrNull(k))) return false;
            }
            return true;
        }
        // Positional iteration via the IEnumerable<object> view for ee-mode
        // IFunnyArray + lang-mode List / MutableArray / FixedArray.
        using var enA = ((System.Collections.Generic.IEnumerable<object>)a).GetEnumerator();
        using var enB = ((System.Collections.Generic.IEnumerable<object>)b).GetEnumerator();
        while (enA.MoveNext() && enB.MoveNext()) {
            if (!AreEqual(enA.Current, enB.Current))
                return false;
        }
        return true;
    }

    private static int GetCollectionCount(object o) => o switch {
        IFunnyArray arr => arr.Count,
        Runtime.Lists.IFunnyEnumerable e => e.Count,
        _ => 0
    };
}