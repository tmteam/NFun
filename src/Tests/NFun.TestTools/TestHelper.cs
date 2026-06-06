using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Text.Json;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Types;

namespace NFun.TestTools;

public static class TestHelper {
    public static CalculationResult Calc(this FunnyRuntime runtime, string id, object clrValue) =>
        runtime.Calc((id, clrValue));

    public static CalculationResult Calc(this string expr, string id, object val) =>
        Funny.Hardcore.Build(expr).Calc((id, val));

    public static CalculationResult Calc(this FunnyRuntime runtime, params (string id, object clrValue)[] values) {
        foreach (var (id, clrValue) in values)
        {
            runtime[id].Value = clrValue;
        }

        runtime.Run();
        var vals = runtime.Variables.Where(v => v.IsOutput)
            .Select(v => new VariableTypeAndValue(v.Name, v.FunnyValue, v.Type))
            .ToArray();
        return new CalculationResult(vals, runtime.Converter);
    }

    public static CalculationResult CalcWithDialect(
        this string expr,
        IfExpressionSetup ifExpressionSyntax = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble,
        IntegerOverflow integerOverflow = IntegerOverflow.Checked,
        AllowUserFunctions allowUserFunctions = AllowUserFunctions.AllowAll,
        OptionalTypesSupport optionalTypesSupport = OptionalTypesSupport.Disabled,
        AllowNewlineInStrings allowNewlineInStrings = AllowNewlineInStrings.Allow,
        NamedTypesSupport namedTypesSupport = NamedTypesSupport.Disabled,
        ExtensionFunctionsSeparation extensionFunctionsSeparation = ExtensionFunctionsSeparation.Disabled,
        params (string id, object clrValue)[] values) =>
        Funny.Hardcore.WithDialect(
            ifExpressionSyntax,
            integerPreferredType,
            realClrType,
            integerOverflow,
            allowUserFunctions,
            optionalTypesSupport,
            allowNewlineInStrings,
            namedTypesSupport,
            extensionFunctionsSeparation: extensionFunctionsSeparation).Build(expr).Calc(values);

    public static CalculationResult Calc(this string expr, params (string id, object val)[] values) =>
        Funny.Hardcore.Build(expr).Calc(values);

    public static CalculationResult CalcWithNamedTypes(this string expr, params (string id, object val)[] values) =>
        Funny.Hardcore.WithDialect(namedTypesSupport: NamedTypesSupport.Enabled).Build(expr).Calc(values);

    public static FunnyRuntime BuildWithNamedTypes(this string expr) =>
        Funny.Hardcore.WithDialect(namedTypesSupport: NamedTypesSupport.Enabled).Build(expr);

    public static object CalcAnonymousOut(this string expr, params (string id, object val)[] values) =>
        Calc(expr, values).Get(Parser.AnonymousEquationId);

    public static FunnyRuntime BuildWithDialect(this string expr,
        IfExpressionSetup ifExpressionSyntax = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble,
        IntegerOverflow integerOverflow = IntegerOverflow.Checked,
        AllowUserFunctions allowUserFunctions = AllowUserFunctions.AllowAll,
        OptionalTypesSupport optionalTypesSupport = OptionalTypesSupport.Disabled,
        AllowNewlineInStrings allowNewlineInStrings = AllowNewlineInStrings.Allow
        )
        => Funny.Hardcore.WithDialect(
            ifExpressionSyntax,
            integerPreferredType,
            realClrType,
            integerOverflow,
            allowUserFunctions,
            optionalTypesSupport,
            allowNewlineInStrings).Build(expr);

    public static FunnyRuntime Build(this string expr) => Funny.Hardcore.Build(expr);


    public static string ToStringSmart(this object v) =>
        v switch {
            char[] c => new string(c),
            string => v.ToString(),
            IPAddress => v.ToString(),
            IEnumerable en => "[" + string.Join(",", en.Cast<object>().Select(ToStringSmart)) + "]",
            _ => v.GetType().IsClass ? JsonSerializer.Serialize(v) : v.ToString()
        };

    public static bool AreSame(object a, object b) {
        if (a == null || b == null)
            return false;

        // Sequence-like containers compare by elements regardless of concrete
        // CLR type: Array vs List<T>, MutableFunnyList vs Array, IFunnyArray vs
        // List<int>, etc. Lets lang-mode list-returning expressions pass tests
        // that assert against a CLR int[] expected value. Excludes strings and
        // IDictionary (handled elsewhere as their own types).
        bool aIsSeq = a is IEnumerable && a is not string && a is not IDictionary;
        bool bIsSeq = b is IEnumerable && b is not string && b is not IDictionary;
        if (aIsSeq && bIsSeq)
        {
            // Set semantics: iteration order is unspecified, so positional comparison
            // is unsound. Compare as multisets (one-to-one element pairing).
            bool aIsSet = a is NFun.Runtime.Lists.IFunnyMutableSet
                          || a is System.Collections.Generic.HashSet<object>
                          || IsClrHashSet(a);
            bool bIsSet = b is NFun.Runtime.Lists.IFunnyMutableSet
                          || b is System.Collections.Generic.HashSet<object>
                          || IsClrHashSet(b);
            if (aIsSet || bIsSet)
                return SeqMultisetEqual(a, b);

            var seqA = ((IEnumerable)a).Cast<object>().ToArray();
            var seqB = ((IEnumerable)b).Cast<object>().ToArray();
            if (seqA.Length != seqB.Length)
                return false;
            for (int i = 0; i < seqA.Length; i++)
                if (!AreSame(seqA[i], seqB[i]))
                    return false;
            return true;
        }
        // One sequence and one scalar → not equal.
        if (aIsSeq != bIsSeq)
            return false;

        if (a.GetType() != b.GetType())
            return false;
        if (a is bool || a is byte || a is sbyte || a is short || a is ushort || a is int || a is uint || a is ulong ||
            a is long)
            return a.Equals(b);

        switch (a)
        {
            case string astr:
                return astr.Equals((string)b);
            case double resultD:
                var expectedD = (double)b;
                if (double.IsNaN(resultD) && double.IsNaN(expectedD)) return true;
                return Math.Abs(resultD - expectedD) < 0.01;
            case decimal resultDec:
                return resultDec.Equals((decimal)b);
            case IPAddress ipAddress:
                return ipAddress.Equals((IPAddress)b);
            case Delegate:
                throw new NotSupportedException($"type {a.GetType().Name} is not supported");
            default:
            {
                var ajson = JsonSerializer.Serialize(a);
                var bjson = JsonSerializer.Serialize(b);
                return ajson.Equals(bjson);
            }
        }
    }

    private static bool IsClrHashSet(object o) {
        var t = o.GetType();
        return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.HashSet<>);
    }

    private static bool SeqMultisetEqual(object a, object b) {
        // O(n²) — fine for tests, simple and stable. For each element on side A,
        // find a matching unconsumed element on side B (by AreSame).
        var listA = ((IEnumerable)a).Cast<object>().ToList();
        var listB = ((IEnumerable)b).Cast<object>().ToList();
        if (listA.Count != listB.Count) return false;
        var matched = new bool[listB.Count];
        foreach (var itemA in listA)
        {
            bool found = false;
            for (int j = 0; j < listB.Count; j++)
            {
                if (matched[j]) continue;
                if (AreSame(itemA, listB[j]))
                {
                    matched[j] = true;
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }

    /// <summary>
    /// Strict variant of <see cref="AreSame"/>: requires same container kind
    /// (list vs array vs fixedArray vs set vs CLR T[]). Use in tests that pin
    /// container kind as part of the assertion — e.g. "this LINQ function
    /// returns fixedArray, not list".
    /// </summary>
    public static bool AreSameStrict(object a, object b) {
        if (a == null || b == null) return false;
        if (!SameContainerKind(a, b)) return false;
        return AreSame(a, b);
    }

    private static bool SameContainerKind(object a, object b) {
        // Identical CLR type → trivially same kind.
        if (a.GetType() == b.GetType()) return true;
        // Collapse by NFun runtime container interface.
        int kindA = ClassifyKind(a);
        int kindB = ClassifyKind(b);
        return kindA == kindB && kindA != 0;
    }

    private static int ClassifyKind(object o) => o switch {
        NFun.Runtime.Lists.IFunnyMutableSet     => 1,
        NFun.Runtime.Lists.IFunnyList           => 2,
        NFun.Runtime.Lists.IFunnyMutableArray   => 3,
        NFun.Runtime.Lists.IFunnyFixedArray     => 4,
        Runtime.Arrays.IFunnyArray              => 5,
        _ => 0
    };
}

/// <summary>
/// Name type and value of concrete variable
/// </summary>
readonly struct VariableTypeAndValue {
    public readonly string Name;
    public readonly object Value;
    public readonly FunnyType Type;

    public VariableTypeAndValue(string name, object value, FunnyType type) {
        Name = name;
        Value = value;
        Type = type;
    }

    public override string ToString() => $"{Name}:{Type} = {Value}";
}
