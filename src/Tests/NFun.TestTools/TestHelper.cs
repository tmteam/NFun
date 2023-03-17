using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Text.Json;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

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


    public static CalculationResult Calc(this string expr, params (string id, object val)[] values) =>
        Funny.Hardcore.Build(expr).Calc(values);

    public static object CalcAnonymousOut(this string expr, params (string id, object val)[] values) =>
        Calc(expr, values).Get(Parser.AnonymousEquationId);

    public static FunnyRuntime BuildWithDialect(this string expr,
        IfExpressionSetup ifExpressionSyntax = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble,
        IntegerOverflow integerOverflow = IntegerOverflow.Checked,
        AllowUserFunctions allowUserFunctions = AllowUserFunctions.AllowAll)
        => Funny.Hardcore.WithDialect(ifExpressionSyntax, integerPreferredType, realClrType, integerOverflow,
            allowUserFunctions).Build(expr);

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
                return Math.Abs(resultD - (double)b) < 0.01;
            case decimal resultDec:
                return resultDec.Equals((decimal)b);
            case IPAddress ipAddress:
                return ipAddress.Equals((IPAddress)b);
            case Array arra:
            {
                var arrb = (Array)b;
                var arrayOfA = arra.Cast<object>().ToArray();
                var arrayOfB = arrb.Cast<object>().ToArray();
                if (arrayOfA.Length != arrayOfB.Length)
                    return false;
                for (int i = 0; i < arrayOfA.Length; i++)
                {
                    if (!AreSame(arrayOfA[i], arrayOfB[i]))
                        return false;
                }

                return true;
            }
            case IEnumerable:
                throw new NotSupportedException($"type {a.GetType().Name} is not supported");
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
