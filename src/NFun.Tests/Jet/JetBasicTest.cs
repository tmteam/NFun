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
      //  [TestCase("y:int=[1,2,3][2]", 3)]
        [TestCase("y=42 +1/2", 42.5)]
        [TestCase("y='test'", "test")]
        [TestCase("y='my name is \\'vasa\\''", "my name is 'vasa'")]
        [TestCase("y='1+1= {1+1}'", "1+1= 2")]
        [TestCase("myInc(x):int = x+1;  y=myInc(41)",42)]
        [TestCase("mult2(x):int = x*2;  myInc(x):int = x+1;  y=20.myInc().mult2()", 42)]
        [TestCase("mult2(x):int = mySum(x,x);  mySum(x,y):int = x+y;  y=10.mult2().mult2().mySum(2)", 42)]
        [TestCase("mySum(x,y):int = x+y; mult2(x):int = mySum(x,x);    y=10.mult2().mult2().mySum(2)", 42)]
        [TestCase("fact(x):int = if(x==0) 1 else fact(x-1)*x; y=4.fact()", 4*3*2*1)]
        [TestCase("myMult(x,y):int = x*y;  fact(x):int = if(x==0) 1 else fact(x-1).myMult(x); y=4.fact()", 4 * 3 * 2 * 1)]
        [TestCase("fact(x):int = if(x==0) 1 else fact(x-1).myMult(x); myMult(x,y):int = x*y; y=4.fact()", 4 * 3 * 2 * 1)]

        public void OriginAnJettedCalculateSameConstants(string expression, object expectedY)
        {
            var runtime = FunBuilder.BuildDefault(expression);
            runtime.Calculate().AssertReturns(VarVal.New("y",expectedY));

            var jetBuilder = new JetSerializerVisitor();
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
