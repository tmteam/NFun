using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun;
using NFun.BuiltInFunctions;
using NFun.Interpritation;
using NFun.Runtime;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.TryTicTests.SyntaxTests
{
    public static class TestTools
    {
        public static FunRuntime Build(string equation)
        {
            var flow = NFun.Tokenization.Tokenizer.ToFlow(equation);
            var tree = NFun.SyntaxParsing.Parser.Parse(flow);
            tree.ComeOver(new SetNodeNumberVisitor(0));


            var functions = new FunctionDictionary();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functions.Add(predefinedFunction);
            foreach (var predefinedFunction in BaseFunctions.GenericFunctions)
                functions.Add(predefinedFunction);

            return RuntimeBuilder.Build(tree, functions);
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
        {
            AssertReturns(result, 0, vars);
        }
        public static void AssertHas(this CalculationResult result, VarVal variable, double delta = 0)
        {
            var res = result.Results.FirstOrDefault(r => r.Name == variable.Name);
            Assert.IsNotNull(res, $"Variable \"{variable.Name}\" not found");
            Assert.AreEqual(variable.Type, res.Type, $"Variable \"{variable.Name}\" has wrong type");

            if (variable.Type == VarType.Real)
                Assert.AreEqual(variable.Value.To<double>(), res.Value.To<double>(), delta,
                    $"Var \"{variable}\" expected: {variable.Value}, but was: {res.Value}");
            else
                Assert.AreEqual(variable.Value, res.Value,
                    $"Var \"{variable}\" expected: {ToStringSmart(variable.Value)}, but was: {ToStringSmart(res.Value)}");
        }

        private static string ToStringSmart(object v)
        {
            if (v is string)
                return v.ToString();
            if (v is char[] c)
                return new string(c);
            if (v is IEnumerable en)
            {
                return "{" + string.Join(",", en.Cast<object>().Select(ToStringSmart)) + "}";
            }

            return v.ToString();
        }

    }
}
