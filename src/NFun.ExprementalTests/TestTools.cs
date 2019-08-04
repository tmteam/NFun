using System;
using System.Collections;
using System.Linq;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ExprementalTests
{
    public static class TestTools
    {
        public static void AssertReturns(this CalculationResult result, double delta, params Var[] vars)
        {
            Assert.AreEqual(vars.Length, result.Results.Length,$"output variables mismatch: {string.Join(",", result.Results)}");
            Assert.Multiple(() =>
            {
                foreach (var variable in vars)
                {
                    Console.WriteLine("Passing: "+variable);
                    AssertHas(result, variable, delta);
                }
            });
        }
        public static void AssertReturns(this CalculationResult result,  params Var[] vars)
        {
            AssertReturns(result,0, vars);
        }
        public static void AssertHas(this CalculationResult result, Var variable, double delta = 0)
        {
            var res = result.Results.FirstOrDefault(r => r.Name == variable.Name);
            Assert.IsNotNull(res, $"Variable \"{variable.Name}\" not found");
            Assert.AreEqual(variable.Type, res.Type,  $"Variable \"{variable.Name}\" has wrong type");
            
            if(variable.Type== VarType.Real)
                Assert.AreEqual (variable.Value.To<double>(), res.Value.To<double>(), delta, 
                    $"Var \"{variable}\" expected: {variable.Value}, but was: {res.Value}");
            else
                Assert.AreEqual(variable.Value, res.Value, 
                    $"Var \"{variable}\" expected: {ToStringSmart(variable.Value)}, but was: {ToStringSmart(res.Value)}");
        }

        private static string ToStringSmart(object v)
        {
            if (v is string)
                return v.ToString();
            if (v is IEnumerable en)
            {
                return "{" + string.Join(",", en.Cast<object>().Select(ToStringSmart)) + "}";
            }

            return v.ToString();
        }
        
    }
}