namespace NFun.TestTools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Exceptions;
using NUnit.Framework;
using Runtime;
using SyntaxParsing;
using Tic;
using Types;

public static class FunnyAssert {
    public static void AssertRuntimes(this string expression)
        => expression.Build().AssertRuntimes(_ => { });

    public static void AssertRuntimes(this string expression, Action<FunnyRuntime> action)
        => expression.Build().AssertRuntimes(action);

    public static void AssertRuntimes(this FunnyRuntime origin, Action<FunnyRuntime> action) {
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

    public static void AssertStringTemplates(this StringTemplateCalculator origin,
        Action<StringTemplateCalculator> action) {
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

    public static CalculationResult AssertResultHas(this CalculationResult result,
        params (string id, object val)[] values) {
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
                $"Variable \"{value.id}\" has wrong type. Actual type is: {resultValue.GetType()} of value {TestHelper.ToStringSmart(resultValue)}");

            if (!TestHelper.AreSame(value.val, resultValue))
                Assert.Fail(
                    $"Var \"{value.id}\" expected: {TestHelper.ToStringSmart(value.val)}, but was: {TestHelper.ToStringSmart(resultValue)}\r\n" +
                    $"clr expected: {JsonSerializer.Serialize(value.val)}, clr actual: {JsonSerializer.Serialize(resultValue)}");
        }

        return result;
    }

    public static void AssertInputsCount(this FunnyRuntime runtime, int count, string message = "") =>
        Assert.AreEqual(count, runtime.Variables.Count(v => !v.IsOutput), message);

    public static void AssertOutputsCount(this FunnyRuntime runtime, int count) =>
        Assert.AreEqual(count, runtime.Variables.Count(v => v.IsOutput));

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
            if (ex.Interval.Finish < ex.Interval.Start)
                Assert.Pass($"Start interval is less then finish interval: {ex}");
            Assert.Pass($"Fun parse error: {ex}");
        }
    }


    public static void ObviousFailsOnApiUsage(Action action) {
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

    public static void ObviousFailsOnParse(Action action) {
        TraceLog.IsEnabled = true;
        try
        {
            action();
            Assert.Fail($"Expression parsed without any errors");
        }
        catch (FunnyParseException ex)
        {
            Assert.Pass($"Fun parse error: {ex}");
        }
    }

    public static void AreSame(object expected, object actual, string message) =>
        Assert.IsTrue(TestHelper.AreSame(expected, actual),
            message + $"Expected: {expected.ToStringSmart()} \r\nActual: {actual.ToStringSmart()}");

    public static void AreSame(object expected, object actual) =>
        Assert.IsTrue(TestHelper.AreSame(expected, actual),
            $"Expected: {expected.ToStringSmart()} \r\nActual: {actual.ToStringSmart()}");
}
