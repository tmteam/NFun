using System;
using System.Collections;
using System.Linq;
using System.Text.Json;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.TestTools
{
    public static class TestHelper
    {
        
        public static void AssertVarCalc(
            string inputName,
            object inputValue,
            string outputName,
            string expression, object expected)
        {
            var runtime = FunBuilder.Build(expression);
            var res = runtime.Calculate(VarVal.New(inputName, inputValue));
            res.AssertHas(VarVal.New(outputName, expected));
        }
        public static void AssertConstantCalc(string varName,string expression, object expected)
        {
            var runtime = FunBuilder.Build(expression);
            var res = runtime.Calculate();
            if(expected is double)
                res.AssertHas(VarVal.New(varName, expected), delta:0.0001);
            else
                res.AssertHas(VarVal.New(varName, expected));
        }
        public static void AssertReturns(this CalculationResult result, double delta, params VarVal[] vars)
        {
            Assert.AreEqual(vars.Length, result.Results.Length, $"output variables mismatch: {string.Join(",", result.Results)}");
            Assert.Multiple(() =>
            {
                foreach (var expected in vars)
                {
                    Console.WriteLine("Passing: " + expected);
                    AssertHas(result, expected, delta);
                }
            });
        }
        public static void AssertReturns(this CalculationResult result, params VarVal[] vars) 
            => AssertReturns(result, 0, vars);

        public static void AssertOutEquals(this CalculationResult result, object val)
            => AssertReturns(result, 0, VarVal.New("out", val));

        public static void AssertOutEquals(this CalculationResult result, double delta, object val)
            => AssertReturns(result, delta, VarVal.New("out", val));
        public static CalculationResult AssertHas(this CalculationResult result, VarVal expected, double delta = 0)
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
            if (v is IEnumerable en)
                return "{" + string.Join(",", en.Cast<object>().Select(ToStringSmart)) + "}";

            return v.ToString();
        }
        
        public static bool AreSame(object a, object b)
        {
            if (a == null || b == null)
                return false;
            if (a.GetType() != b.GetType())
                return false;
            var ajson = JsonSerializer.Serialize(a);
            var bjson = JsonSerializer.Serialize(b);
            Console.WriteLine($"Comparing object. \r\norigin: \r\n{ajson}\r\nexpected: \r\n{bjson}");
            return ajson.Equals(bjson);
        }

        public static void AssertObviousFailsOnParse(string expression)
        {
            TraceLog.IsEnabled = true;
            try
            {
                var runtime = FunBuilder.Build(expression);
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
    }
}
