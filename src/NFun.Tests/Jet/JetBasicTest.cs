using System;
using System.Collections.Generic;
using System.Text;
using NFun;
using NFun.Interpritation;
using NFun.Jet;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.Jet
{
    public class JetBasicTest
    {
        [TestCase("y:int=10", 10)]
        [TestCase("y=1/2", 0.5)]
        [TestCase("y=if(2>1) [1+2,2+3]; if (false) [1-4,3]; else [0]", new[]{3,5})]
        [TestCase("y:int[]=[1,2,3]", new[] { 1, 2,3 })]
        [TestCase("y=['1','2','3']", new[] { "1", "2", "3" })]

        [TestCase("y=42 +1/2", 42.5)]
        [TestCase("y='test'", "test")]
        [TestCase("y='my name is \\'vasa\\''", "my name is 'vasa'")]
        [TestCase("y='1+1= {1+1}'", "1+1= 2")]
        public void OriginAnJettedCalculateSameConstants(string expression, object expectedY)
        {
            var runtime = FunBuilder.BuildDefault(expression);
            runtime.Calculate().AssertReturns(VarVal.New("y",expectedY));

            var jetBuilder = new JetBuilderVisitor();
            runtime.ApplyEntry(jetBuilder);
            var jet = jetBuilder.GetResult().ToString();
            Console.WriteLine("Jet: "+ jet);
            var functionsDictionary = new FunctionsDictionary();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functionsDictionary.Add(predefinedFunction);
            foreach (var genericFunctionBase in BaseFunctions.GenericFunctions)
                functionsDictionary.Add(genericFunctionBase);

            var jetRuntime  =  JetDeserializer.Deserialize(jet, functionsDictionary);
            jetRuntime.Calculate().AssertReturns(VarVal.New("y", expectedY));

        }

    }
}
