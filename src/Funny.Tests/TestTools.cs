using System;
using System.Linq;
using Funny.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    public static class TestTools
    {
        public static void AssertReturns(this CalculationResult result, double delta, params Var[] vars)
        {
            Assert.AreEqual(vars.Length, result.Results.Length);
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
            
            if(variable.Type== VarType.NumberType)
                Assert.AreEqual ((double)variable.Value, (double)res.Value, delta, 
                    $"Var \"{variable}\" expected: {variable.Value}, but was: {res.Value}");
            else
                Assert.AreEqual(variable.Value, res.Value, 
                    $"Var \"{variable}\" expected: {variable.Value}, but was: {res.Value}");
        }
    }
}