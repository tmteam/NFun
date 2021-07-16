using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.TestTools
{
    public static class TestHelper
    {
        public static CalculationResult Calc(this FunnyRuntime runtime, string id, object clrValue) => runtime.Calc((id, clrValue));
        
        public static CalculationResult Calc(this string expr, string id, object val) =>
            Funny.Hardcore.Build(expr).Calc((id, val));

        public static CalculationResult Calc(this FunnyRuntime runtime, params (string id, object clrValue)[] values)
        {
            foreach (var (id, clrValue) in values)
            {
                runtime[id].Value= clrValue;
            }
            runtime.Run();
            var vals = runtime.Variables.Where(v => v.IsOutput).Select(v => new VarVal(v.Name, v.FunnyValue, v.Type))
                .ToArray();
            return new CalculationResult(vals);
        }


        public static CalculationResult Calc(this string expr, params (string id, object val)[] values) =>
            Funny.Hardcore.Build(expr).Calc(values);

        public static FunnyRuntime Build(this string expr) => Funny.Hardcore.Build(expr);

        public static void AssertOut(this CalculationResult result, object expected) =>
            AssertReturns(result, Parser.AnonymousEquationId, expected);

        public static void AssertOut(this string expr, object expected) =>
            expr.Calc().AssertReturns(Parser.AnonymousEquationId, expected);

        public static void AssertReturns(this string expr, object expected) => expr.Calc().AssertReturns(expected);

        public static void AssertReturns(this string expr, params (string id, object val)[] values) =>
            expr.Calc().AssertReturns(values);

        public static void AssertReturns(this string expr, string id, object val) =>
            AssertReturns(expr, new[] {(id, val)});

        public static void AssertReturns(this CalculationResult result, object expected)
        {
            Assert.AreEqual(1, result.Count,
                $"Many output variables found: {string.Join(",", result.Results.Select(r=>r.Item1))}");
            AssertResultHas(result, (result.Results.First().Item1, expected));
        }

        public static void AssertReturns(this CalculationResult result, string id, object expected)
            => AssertReturns(result, (id, expected));

        public static void AssertReturns(this CalculationResult result, params (string id, object val)[] values) =>
            Assert.Multiple(() =>
            {
                AssertResultHas(result, values);
                Assert.AreEqual(values.Length, result.Count,
                    $"output variables mismatch: {string.Join(",", result.Results.Select(r=>r.Item1))}");
            });

        public static void AssertResultHas(this string expr, string id, object val) =>
            expr.Calc().AssertResultHas((id, val));

        public static void AssertResultHas(this string expr, params (string id, object val)[] values) =>
            expr.Calc().AssertResultHas(values);


        public static void AssertResultHas(this CalculationResult result, string id, object val)
            => AssertResultHas(result, (id, val));

        public static void AssertResultIs(this CalculationResult result, params (string id, Type type)[] types)
        {
            if (result.Count != types.Length)
                Assert.Fail($"Unexpected outputs. Expected {types.Length} but was {result.Count}");
            foreach (var value in types)
            {
                var resultValue = result.Get(value.id);
                Assert.IsNotNull(resultValue, $"Output variable \"{value.id}\" not found");
                Assert.AreEqual(value.type, resultValue.GetType(),
                    $"Output variable \"{value.id}\" has wrong clr type {resultValue.GetType()}");
            }
        }

        public static void AssertResultIs(this CalculationResult result, Type type)
        {
            var res = result.Results.FirstOrDefault();
            Assert.IsNotNull(res, "no results found");
            AssertResultIs(result, (res.Item1, type));
        }

        public static void AssertResultHas(this CalculationResult result, params (string id, object val)[] values)
        {
            foreach (var value in values)
            {
                var resultValue = result.Get(value.id);
                Assert.IsNotNull(resultValue, $"Output variable \"{value.id}\" not found");
                if (resultValue is IReadOnlyDictionary<string,object> @struct)
                {
                    var converted = FunnyTypeConverters.GetInputConverter(value.val.GetType()).ToFunObject(value.val);
                    Assert.AreEqual(converted, @struct);
                    return;
                }

                Assert.AreEqual(value.val.GetType(), resultValue.GetType(),
                    $"Variable \"{value.id}\" has wrong type. Actual type is: {resultValue.GetType()}");

                if (!AreSame(value.val, resultValue))
                    Assert.Fail(
                        $"Var \"{value.id}\" expected: {ToStringSmart(value.val)}, but was: {ToStringSmart(resultValue)}\r\n" +
                        $"clr expected: {JsonSerializer.Serialize(value.val)}, clr actual: {JsonSerializer.Serialize(resultValue)}");
            }
        }

        public static void AssertOutputs(this FunnyRuntime runtime, IEnumerable<IFunnyVar> variables) => 
            CollectionAssert.AreEquivalent(variables, runtime.Variables.Where(v=>v.IsOutput));

        public static void AssertInputsCount(this FunnyRuntime runtime, int count, string message ="") => 
            Assert.AreEqual(count, runtime.Variables.Count(v=>!v.IsOutput), message);

        public static void AssertOutputsCount(this FunnyRuntime runtime, int count) => 
            Assert.AreEqual(count, runtime.Variables.Count(v=>v.IsOutput));

        private static string ToStringSmart(object v)
        {
            if (v is string)
                return v.ToString();
            if (v is char[] c)
                return new string(c);
            if (!v.GetType().IsClass)
                return v.ToString();
            if (v is IEnumerable en)
                return "[" + string.Join(",", en.Cast<object>().Select(ToStringSmart)) + "]";
            return JsonSerializer.Serialize(v);
        }

        public static bool AreSame(object a, object b)
        {
            if (a == null || b == null)
                return false;
            if (a.GetType() != b.GetType())
                return false;
            if (a is string astr)
            {
                var bstr = (string) b;
                return astr.Equals(bstr);
            }

            if (a is double resultD)
            {
                var expectedD = (double) b;
                return Math.Abs(resultD - expectedD) < 0.01;
            }

            if (a is Array arra)
            {
                var arrb = (Array) b;
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

            if (a is IEnumerable)
                throw new NotSupportedException($"type {a.GetType().Name} is not supported");
            if (a is IList)
                throw new NotSupportedException($"type {a.GetType().Name} is not supported");
            if (a is Delegate)
                throw new NotSupportedException($"type {a.GetType().Name} is not supported");

            var ajson = JsonSerializer.Serialize(a);
            var bjson = JsonSerializer.Serialize(b);
            return ajson.Equals(bjson);
        }


        public static void AssertObviousFailsOnRuntime(this string expression)
        {
            var runtime = expression.Build();
            Assert.Catch<FunRuntimeException>(() => runtime.Calc());
        }

        public static void AssertObviousFailsOnParse(this string expression)
        {
            TraceLog.IsEnabled = true;
            try
            {
                var runtime = Funny.Hardcore.Build(expression);
                if (runtime.Variables.Any(v=>!v.IsOutput))
                {
                    Assert.Fail($"Expression parsed without any errors");
                    return;
                }

                try
                {
                    var result = runtime.Calc();
                    Assert.Fail($"Const expression succesfully executed. Result: {result}");
                    return;
                }
                catch (Exception e)
                {
                    Assert.Fail($"Const expression succesfully build. Executrion failed with error: {e}");
                    return;
                }
            }
            catch (FunParseException ex)
            {
                Assert.Pass($"Fun parse error: {ex}");
                return;
            }
        }

        public static void AssertObviousFailsOnParse(Action action)
        {
            TraceLog.IsEnabled = true;
            try
            {
                action();
                Assert.Fail($"Expression parsed without any errors");
            }
            catch (FunParseException ex)
            {
                Assert.Pass($"Fun parse error: {ex}");
                return;
            }
        }
    }
}
