using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.SyntaxParsing;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.TestTools
{
    public static class TestHelper
    {
        public static void OLD_AssertReturns(this CalculationResult result, double delta, params VarVal[] vars)
        {
            Assert.AreEqual(vars.Length, result.Results.Length, $"output variables mismatch: {string.Join(",", result.Results)}");
            Assert.Multiple(() =>
            {
                foreach (var expected in vars)
                {
                    Console.WriteLine("Passing: " + expected);
                    OLD_AssertHas(result, expected, delta);
                }
            });
        }
        public static CalculationResult Calc(this string expr, string id, object val) => Funny.Hardcore.Build(expr).Calc((id,val));

        public static CalculationResult Calc(this string expr, params (string id, object val)[] values) => Funny.Hardcore.Build(expr).Calc(values);
        public static FunRuntime Build(this string expr) => Funny.Hardcore.Build(expr);

        public static void AssertOut(this CalculationResult result, object expected) => AssertReturns(result, Parser.AnonymousEquationId, expected);
        public static void AssertOut(this string expr, object expected) => expr.Calc().AssertReturns(Parser.AnonymousEquationId, expected);
        public static void AssertReturns(this string expr, object expected) => expr.Calc().AssertReturns(expected);
        public static void AssertReturns(this string expr,params (string id, object val)[] values) => expr.Calc().AssertReturns(values);
        public static void AssertReturns(this string expr,string id, object val) => AssertReturns(expr,new[]{(id,val)});

        public static void AssertReturns(this CalculationResult result, object expected)
        {
            Assert.AreEqual(1, result.Results.Length,
                $"Many output variables found: {string.Join(",", result.Results)}");
            AssertResultHas(result, (result.Results[0].Name, expected));
        }

        public static void AssertReturns(this CalculationResult result, string id, object expected)
            => AssertReturns(result, (id, expected));
        public static void AssertReturns(this CalculationResult result, params (string id, object val)[] values) =>
            Assert.Multiple(() =>
            {
                AssertResultHas(result, values);
                Assert.AreEqual(values.Length, result.Results.Length,
                    $"output variables mismatch: {string.Join(",", result.Results)}");
            });

            public static void AssertResultHas(this string expr, string id, object val) => expr.Calc().AssertResultHas((id, val));
            public static void AssertResultHas(this string expr, params(string id, object val)[] values) => expr.Calc().AssertResultHas(values);
            

            public static void AssertResultHas(this CalculationResult result, string id, object val)
                => AssertResultHas(result, (id, val));

        public static void AssertResultHas(this CalculationResult result, params (string id, object val)[] values)
        {
            foreach (var value in values)
            {
                var resultValue = result.GetClr(value.id);
                Assert.IsNotNull(resultValue, $"Output variable \"{value.id}\" not found");
                Assert.AreEqual(value.val.GetType(), resultValue.GetType(),
                    $"Variable \"{value.id}\" has wrong type. Actual Funny type is: {result.Get(value.id).Type}");
                if (!AreSame(value.val, resultValue))
                    Assert.Fail(
                        $"Var \"{value.id}\" expected: {ToStringSmart(value.val)}, but was: {ToStringSmart(resultValue)}\r\n" +
                        $"clr expected: { JsonSerializer.Serialize(value.val)}, clr actual: {JsonSerializer.Serialize(resultValue)}");
            }
        }


        public static void OLD_AssertReturns(this CalculationResult result, params VarVal[] vars) 
            => OLD_AssertReturns(result, 0, vars);

        public static void AssertOutEquals(this CalculationResult result, object val)
            => OLD_AssertReturns(result, 0, VarVal.New("out", val));

        public static void OLD_AssertOutEquals(this CalculationResult result, double delta, object val)
            => OLD_AssertReturns(result, delta, VarVal.New("out", val));
        public static CalculationResult OLD_AssertHas(this CalculationResult result, VarVal expected, double delta = 0)
        {
            var res = result.Results.FirstOrDefault(r => r.Name == expected.Name);
            Assert.IsFalse(res.IsEmpty, $"Variable \"{expected.Name}\" not found");
            if (expected.Value is IFunArray funArray)
            {
                if (res.Value is IFunArray resFunArray)
                {
                    Assert.IsTrue(TypeHelper.AreEquivalent(funArray, resFunArray),
                            $"Var \"{expected}\" expected: {ToStringSmart(expected.Value)}, but was: {ToStringSmart(res.Value)}");
                    return result;
                }
            }
            Assert.AreEqual(expected.Type, res.Type, $"Variable \"{expected.Name}\" has wrong type");
            
            if (expected.Type == VarType.Real)
                Assert.AreEqual((double) expected.Value, (double)res.Value, delta,
                    $"Var \"{expected}\" expected: {expected.Value}, but was: {res.Value}");
            else
                Assert.AreEqual(expected.Value, res.Value,
                    $"Var \"{expected}\" expected: {ToStringSmart(expected.Value)}, but was: {ToStringSmart(res.Value)}");
            return result;
        }

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
                var arrayOfA =arra.Cast<object>().ToArray();
                var arrayOfB =arrb.Cast<object>().ToArray();
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
            if(a is Delegate)
                throw new NotSupportedException($"type {a.GetType().Name} is not supported");
            
            var ajson = JsonSerializer.Serialize(a);
            var bjson = JsonSerializer.Serialize(b);
            return ajson.Equals(bjson);
        }


        public static void AssertObviousFailsOnRuntime(this string expression)
        {
            var runtime = expression.Build();
            Assert.Catch<FunRuntimeException>(() => runtime.Calculate());
        }

        public static void AssertObviousFailsOnParse(this string expression)
        {
            TraceLog.IsEnabled = true;
            try
            {
                var runtime = Funny.Hardcore.Build(expression);
                if (runtime.Inputs.Length > 0)
                {
                    Assert.Fail($"Expression parsed without any errors");
                    return;
                }
                try
                {
                    var result = runtime.Calculate();
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
