using System;
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
        [TestCase("y:int=[1,2,3][2]", 3)]
        [TestCase("y:int=[[1,2,3],[4,5,6],[7,8,9]][2][1]", 8)]
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
        [TestCase("myGet(x,i) = x[i];  y=[1,2,3].myGet(1)",2)]
        [TestCase("getHalf(x) = x[round(x.count()/2) : ];  y=[1,2,3,4].getHalf()", new int[]{3,4})]
        [TestCase("fake(a) = a\r y = 'test'.fake().fake().fake()", "test")]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,2,true)", 1)]
        [TestCase("y=[1,2,3].map((i)->1)", new[] { 1,1,1 })]
        [TestCase("y=[1,2,3].map((i)->i*2)", new[]{2,4,6})]
        [TestCase("myMap(i):int=i*2;  y=[1,2,3].map(myMap)", new[] { 2,4,6 })]
        [TestCase("y=[1,2,3].reduce((i,j)-> i+j)", 6)]
        [TestCase("y=[1,2,3].reduce(max)", 3)]

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



        [TestCase("y:int=x",10, 10)]
        [TestCase("y=1/x",2, 0.5)]
        [TestCase("y=if(x>1) [1+2,2+3]; if (false) [1-4,3]; else [0]",2, new[] { 3, 5 })]
        [TestCase("y:int[]=x", new[] { 1, 2, 3 }, new[] { 1, 2, 3 })]
        [TestCase("y=['1','{x}','3']",2, new[] { "1", "2", "3" })]
        [TestCase("y:int=[1,2,3][x]",2, 3)]
        [TestCase("y:int=[[1,2,3],[4,5,6],[7,8,9]][2][x]",1, 8)]
        [TestCase("y=x +1/2", 42, 42.5)]
        [TestCase("y='{x}'","test",  "test")]
        [TestCase("y='1+{x}= {x+1}'",1, "1+1= 2")]
        [TestCase("myInc(x):int = x+1;  y=myInc(x)",41, 42)]
        [TestCase("mult2(x):int = x*2;  myInc(x):int = x+1;  y=x.myInc().mult2()",20, 42)]
        [TestCase("mult2(x):int = mySum(x,x);  mySum(x,y):int = x+y;  y=x.mult2().mult2().mySum(2)",10, 42)]
        [TestCase("mySum(x,y):int = x+y; mult2(x):int = mySum(x,x);    y=x.mult2().mult2().mySum(2)",10, 42)]
        [TestCase("fact(x):int = if(x==0) 1 else fact(x-1)*x; y=x.fact()", 4, 4 * 3 * 2 * 1)]
        [TestCase("myMult(x,y):int = x*y;  fact(x):int = if(x==0) 1 else fact(x-1).myMult(x); y=x.fact()", 4, 4 * 3 * 2 * 1)]
        [TestCase("fact(x):int = if(x==0) 1 else fact(x-1).myMult(x); myMult(x,y):int = x*y; y=x.fact()",4, 4 * 3 * 2 * 1)]
        [TestCase("myGet(x,i) = x[i];  y=[1,2,3].myGet(x)",1, 2)]
        [TestCase("getHalf(x) = x[round(x.count()/2) : ];  y=[1,2,3,x].getHalf()",4, new int[] { 3, 4 })]
        [TestCase("fake(a) = a; x:text; y = x.fake().fake().fake()", "test", "test")]
        [TestCase("choise(a,b,takefirst) = if(takefirst) a else b\r y = choise(1,x,true)",2, 1)]
        [TestCase("x:int; y=[1,2,3].map((i)->x)",1, new[] { 1, 1, 1 })]
        [TestCase("x:int; y=[1,2,3].map((i)->i*x)",2, new[] { 2, 4, 6 })]
        [TestCase("myMap(i):int=i*2;  y=[1,2,x].map(myMap)",3, new[] { 2, 4, 6 })]
        [TestCase("y=[1,2,3].reduce((i,j)-> i+j+x)",1, 8)]
        [TestCase("x:int[]; y=x.reduce(max)",new[]{1,2,3}, 3)]

        public void OriginAnJettedCalculateSameWithSingleVariable(string expression,object variable, object expectedY)
        {
            var runtime = FunBuilder.BuildDefault(expression);
            runtime.Calculate(VarVal.New("x",variable)).AssertReturns(VarVal.New("y", expectedY));

            var jetBuilder = new JetSerializerVisitor();
            runtime.ApplyEntry(jetBuilder);
            var jet = jetBuilder.GetResult().ToString();
            Console.WriteLine("Jet: " + jet);
            var functionsDictionary = new FunctionsDictionary();
            foreach (var predefinedFunction in BaseFunctions.ConcreteFunctions)
                functionsDictionary.Add(predefinedFunction);
            foreach (var genericFunctionBase in BaseFunctions.GenericFunctions)
                functionsDictionary.Add(genericFunctionBase);

            var jetRuntime = JetDeserializer.Deserialize(jet, functionsDictionary);
            jetRuntime.Calculate(VarVal.New("x", variable)).AssertReturns(VarVal.New("y", expectedY));
        }
    }
}
