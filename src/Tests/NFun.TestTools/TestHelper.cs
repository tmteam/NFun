﻿using System;
using System.Collections;
using System.Collections.Generic;
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
        RealClrType realClrType = RealClrType.IsDouble)
        => Funny.Hardcore.WithDialect(ifExpressionSyntax, integerPreferredType, realClrType).Build(expr);

    public static FunnyRuntime Build(this string expr) => Funny.Hardcore.Build(expr);
    
    public static void AssertRuntimes(this string expression) 
        => expression.Build().AssertRuntimes(_ => { });
    
    public static void AssertRuntimes(this string expression, Action<FunnyRuntime> action) 
        => expression.Build().AssertRuntimes(action);

    public static void AssertRuntimes(this FunnyRuntime origin, Action<FunnyRuntime> action)
    {
        var runtime = origin;
        Console.WriteLine("Origin");
        action(runtime);
        var clone1 = runtime.Clone();
        var clone2 = runtime.Clone();
        Console.WriteLine("Origin Clone 1");
        action(clone1);
        Console.WriteLine("Origin Clone 2");
        action(clone2);
        var clone3 = runtime.Clone();
        Console.WriteLine("Origin Clone 3");
        action(clone3);
        var grandClone = clone3.Clone();
        Console.WriteLine("Origin Grandclone");
        action(grandClone);
    }
    
    public static void AssertStringTemplates(this StringTemplateCalculator origin, Action<StringTemplateCalculator> action)
    {
        var runtime = origin;
        Console.WriteLine("Origin");
        action(runtime);
        var clone1 = runtime.Clone();
        var clone2 = runtime.Clone();
        Console.WriteLine("Origin Clone 1");
        action(clone1);
        Console.WriteLine("Origin Clone 2");
        action(clone2);
        var clone3 = runtime.Clone();
        Console.WriteLine("Origin Clone 3");
        action(clone3);
        var grandClone = clone3.Clone();
        Console.WriteLine("Origin Grandclone");
        action(grandClone);
    }
    
    public static void AssertAnonymousOut(this CalculationResult result, object expected) =>
        AssertReturns(result, Parser.AnonymousEquationId, expected);

    public static void AssertAnonymousOut(this string expr, object expected) =>
        expr.Calc().AssertReturns(Parser.AnonymousEquationId, expected);

    public static void AssertReturns(this string expr, object expected) => expr.Calc().AssertReturns(expected);

    public static void AssertReturns(this string expr, params (string id, object val)[] values) =>
        expr.Calc().AssertReturns(values);

    public static void AssertReturns(this string expr, string id, object val) =>
        AssertReturns(expr, new[] { (id, val) });

    public static void AssertReturns(this CalculationResult result, object expected) {
        Assert.AreEqual(
            1, result.Count,
            $"Many output variables found: {string.Join(",", result.ResultNames)}");
        AssertResultHas(result, (result.ResultNames.First(), expected));
    }

    public static void AssertReturns(this CalculationResult result, string id, object expected)
        => AssertReturns(result, (id, expected));


    public static void AssertReturns(this CalculationResult result, params (string id, object val)[] values) =>
        Assert.Multiple(
            () => {
                AssertResultHas(result, values);
                Assert.AreEqual(
                    values.Length, result.Count,
                    $"output variables mismatch: {string.Join(",", result.ResultNames)}");
            });

    public static CalculationResult AssertResultHas(this string expr, string id, object val) =>
        expr.Calc().AssertResultHas((id, val));
    
    public static void AssertResultHas(this string expr, params (string id, object val)[] values) =>
        expr.Calc().AssertResultHas(values);


    public static CalculationResult AssertResultHas(this CalculationResult result, string id, object val)
        => AssertResultHas(result, (id, val));

    public static FunnyRuntime AssertContains(this FunnyRuntime result, string id, FunnyType type) {
        var value = result[id];
        Assert.IsNotNull(value, $"Variable \"{id}\" not found");
        Assert.AreEqual(type, value.Type, $"Variable \"{id}\" has wrong type");
        return result;
    }

    public static void AssertResultIs(this CalculationResult result, params (string id, Type type)[] types) {
        if (result.Count != types.Length)
            Assert.Fail($"Unexpected outputs. Expected {types.Length} but was {result.Count}");
        foreach (var value in types)
        {
            var resultValue = result.Get(value.id);
            Assert.IsNotNull(resultValue, $"Output variable \"{value.id}\" not found");
            Assert.AreEqual(
                value.type, resultValue.GetType(),
                $"Output variable \"{value.id}\" has wrong clr type {resultValue.GetType()}");
        }
    }

    public static void AssertResultIs(this CalculationResult result, Type type) {
        var res = result.Results.FirstOrDefault();
        Assert.IsNotNull(res, "no results found");
        AssertResultIs(result, (res.Item1, type));
    }

    public static CalculationResult AssertResultHas(this CalculationResult result, params (string id, object val)[] values) {
        foreach (var value in values)
        {
            var resultValue = result.Get(value.id);
            Assert.IsNotNull(resultValue, $"Output variable \"{value.id}\" not found");
            if (resultValue is IReadOnlyDictionary<string, object> @struct)
            {
                var converted = result.Converter.GetInputConverterFor(value.val.GetType()).ToFunObject(value.val);
                Assert.AreEqual(converted, @struct);
                return result;
            }

            Assert.AreEqual(
                value.val.GetType(), resultValue.GetType(),
                $"Variable \"{value.id}\" has wrong type. Actual type is: {resultValue.GetType()} of value {ToStringSmart(resultValue)}");

            if (!AreSame(value.val, resultValue))
                Assert.Fail(
                    $"Var \"{value.id}\" expected: {ToStringSmart(value.val)}, but was: {ToStringSmart(resultValue)}\r\n" +
                    $"clr expected: {JsonSerializer.Serialize(value.val)}, clr actual: {JsonSerializer.Serialize(resultValue)}");
        }

        return result;
    }

    public static void AssertInputsCount(this FunnyRuntime runtime, int count, string message = "") =>
        Assert.AreEqual(count, runtime.Variables.Count(v => !v.IsOutput), message);

    public static void AssertOutputsCount(this FunnyRuntime runtime, int count) =>
        Assert.AreEqual(count, runtime.Variables.Count(v => v.IsOutput));

    public static string ToStringSmart(this object v) =>
        v switch {
            char[] c       => new string(c),
            string         => v.ToString(),
            IPAddress      => v.ToString(),
            IEnumerable en => "[" + string.Join(",", en.Cast<object>().Select(ToStringSmart)) + "]",
            _              => v.GetType().IsClass ? JsonSerializer.Serialize(v) : v.ToString()
        };

    public static bool AreSame(object a, object b) {
        if (a == null || b == null)
            return false;
        if (a.GetType() != b.GetType())
            return false;
        if (a is bool || a is byte || a is sbyte || a is short || a is ushort || a is int || a is uint || a is ulong || a is long)
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


    public static void AssertObviousFailsOnRuntime(this string expression) {
        var runtime = expression.Build();
        try
        {
            var res = runtime.Calc();
            Assert.Fail($"{nameof(FunnyRuntimeException)} was not thrown. Calculation results: {res}\r\n");
        }
        catch (FunnyRuntimeException e)
        {
            Assert.Pass($"Expected error: {e.Message} of type {e.GetType().Name}\r\n");
        }
        catch (Exception e)
        {
            Assert.Fail($"{e.GetType().Name}('{e.Message}') was thrown instead of {nameof(FunnyRuntimeException)}\r\n");
        }
    }

    public static void AssertObviousFailsOnParse(this string expression, 
        IfExpressionSetup ifExpressionSyntax = IfExpressionSetup.IfIfElse,
        IntegerPreferredType integerPreferredType = IntegerPreferredType.I32,
        RealClrType realClrType = RealClrType.IsDouble) {
        TraceLog.IsEnabled = true;
        try
        {
            var runtime = Funny.Hardcore
                               .WithDialect(ifExpressionSyntax, integerPreferredType, realClrType)
                               .Build(expression);
            if (runtime.Variables.Any(v => !v.IsOutput))
            {
                Assert.Fail($"Expression parsed without any errors");
                return;
            }

            try
            {
                var result = runtime.Calc();
                Assert.Fail($"Const expression succesfully executed. Result: {result}");
            }
            catch (Exception e)
            {
                Assert.Fail($"Const expression succesfully build. Execution failed with error: {e}");
            }
        }
        catch (FunnyParseException ex)
        {
            if(ex.Interval.Finish<ex.Interval.Start)
                Assert.Pass($"Start interval is less then finish interval: {ex}");
            Assert.Pass($"Fun parse error: {ex}");
        }
    }

    
    public static void AssertObviousFailsOnApiUsage(Action action) {
        TraceLog.IsEnabled = true;
        try
        {
            action();
            Assert.Fail($"Expression executed without any errors");
        }
        catch (FunnyInvalidUsageException ex)
        {
            Assert.Pass($"Funny api usage error: {ex}");
            return;
        }
    }
    
    public static void AssertObviousFailsOnParse(Action action) {
        TraceLog.IsEnabled = true;
        try
        {
            action();
            Assert.Fail($"Expression parsed without any errors");
        }
        catch (FunnyParseException ex)
        {
            Assert.Pass($"Fun parse error: {ex}");
            return;
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