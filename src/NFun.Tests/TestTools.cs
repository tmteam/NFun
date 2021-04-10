﻿using System;
using System.Collections;
using System.Linq;
using System.Text.Json;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace NFun.Tests
{
    public static class TestTools
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
                foreach (var variable in vars)
                {
                    Console.WriteLine("Passing: " + variable);
                    AssertHas(result, variable, delta);
                }
            });
        }
        public static void AssertReturns(this CalculationResult result, params VarVal[] vars) 
            => AssertReturns(result, 0, vars);

        public static void AssertOutEquals(this CalculationResult result, object val)
            => AssertReturns(result, 0, VarVal.New("out", val));

        public static void AssertOutEquals(this CalculationResult result, double delta, object val)
            => AssertReturns(result, delta, VarVal.New("out", val));
        public static CalculationResult AssertHas(this CalculationResult result, VarVal variable, double delta = 0)
        {
            var res = result.Results.FirstOrDefault(r => r.Name == variable.Name);
            Assert.IsFalse(res.IsEmpty, $"Variable \"{variable.Name}\" not found");
            if (variable.Value is IFunArray funArray)
            {
                if (res.Value is IFunArray resFunArray)
                {
                    Assert.IsTrue(TypeHelper.AreEquivalent(funArray, resFunArray),
                            $"Var \"{variable}\" expected: {ToStringSmart(variable.Value)}, but was: {ToStringSmart(res.Value)}");
                    return result;
                }
            }
            Assert.AreEqual(variable.Type, res.Type, $"Variable \"{variable.Name}\" has wrong type");
            
            if (variable.Type == VarType.Real)
                Assert.AreEqual((double) variable.Value, (double)res.Value, delta,
                    $"Var \"{variable}\" expected: {variable.Value}, but was: {res.Value}");
            else
                Assert.AreEqual(variable.Value, res.Value,
                    $"Var \"{variable}\" expected: {ToStringSmart(variable.Value)}, but was: {ToStringSmart(res.Value)}");
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

    }
}
