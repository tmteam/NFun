using System;
using System.Collections;
using System.Linq;
using NFun;
using NFun.Interpritation;
using NFun.Jet;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    public static class TestTools
    {
        /// <summary>
        /// Assert that base runtime has no inputs.
        /// Assert output list.
        /// Calculate.
        /// Assert Results.
        /// Repeat fot jet compiled version
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="expectedOutputs"></param>
        public static void AssertBuildJetAndCalculateConstant(this FunRuntime runtime, params VarVal[] expectedOutputs)
            => AssertBuildJetAndCalculate(runtime, new VarVal[0], expectedOutputs);

        public static void AssertBuildJetAndCalculate(this FunRuntime runtime, VarVal[] inputs, params VarVal[] expectedOutputs)
        {
            runtime.Calculate(inputs).AssertReturns(expectedOutputs);

            var jet = runtime.ToJet();
            
            var jetRuntime = JetDeserializer.Deserialize(jet, new FunctionsDictionary(BaseFunctions.Functions));
            jetRuntime.Calculate(inputs).AssertReturns(expectedOutputs);

        }
        public static void AssertReturns(this CalculationResult result, double delta, params VarVal[] vars)
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
        public static void AssertReturns(this CalculationResult result,  params VarVal[] vars)
        {
            AssertReturns(result,0, vars);
        }
        public static void AssertHas(this CalculationResult result, VarVal variable, double delta = 0)
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