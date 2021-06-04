using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    [TestFixture]
    public class IfThenElseTest
    {
        [Test]
        public void Nested2IfThenElseParsing() =>
            @"y =
	            if (true) 
		            if (true) 1
		            else 4 	
	            else 5
            ".Build();

        [Test]
        public void Nested3IfThenElseParsing() =>
            @"y =
	            if (true) 
		            if (true)
		 	            if (true) 2
		 	            else 3
		            else 4 	
	            else 5
            ".Build();

        [Test]
        public void Nested4IfThenElseParsing() =>
            @"y =
	            if (true) 
		            if (true) 
		 	            if (true)
                            if (true) 3		 	
                            else 4
                        else 5
		            else 6 	
	            else 7
            ".Build();

        [TestCase(1,0,0,1)]
        [TestCase(2,1,0,2)]
        [TestCase(2,2,0,3)]
        [TestCase(2,9,0,4)]
        [TestCase(3,1,1,5)]
        [TestCase(3,1,9,6)]
        [TestCase(3,2,0,7)]
        [TestCase(3,9,0,8)]
        [TestCase(9,4,0,9)]
        [TestCase(9,9,0,10)]
        public void NestedIfThenElse(double x1, double x2, double x3, double expected) =>
            @"
                y = if (x1 == 1) 1 
                    if (x1 == 2)
                        if (x2 == 1) 2
                        if (x2 == 2) 3
                        else 4
                    if (x1 == 3) 
                        if (x2 == 1) 
                            if (x3 ==1) 5
                            else 6
                        if (x2 == 2) 7
                        else 8 	
                    else if (x2 == 4) 9 
                         else 10"
                .Calc(("x1",Convert.ToDouble(x1)), ("x2",Convert.ToDouble(x2)), ("x3",Convert.ToDouble(x3)))
                .AssertReturns("y",expected);

        [TestCase("y = if (1<2 )10 else -10", 10)]
        [TestCase("y = if (1>2 )-10 else 10", 10)]
        [TestCase("y = if (2>1 )10 else -10", 10)]
        [TestCase("y = if (2>1 )10\r else -10", 10)]
        [TestCase("y = if (2==1)10\r else -10", -10)]
        [TestCase("y = if (2<1 )10 \r if (2>1)  -10 else 0", -10)]
        [TestCase("y = if (1>2 )10\r if (1<2) -10\r else 0", -10)]
        public void ConstantIntEquation(string expr, double expected) 
            => expr.Calc().AssertReturns(expected);

        [TestCase("y = if (1<2 ) true else false", true)]
        [TestCase("y = if (true) true else false", true)]
        [TestCase("y = if (true) true \r if (false) false else true", true)]
        public void ConstantBoolEquation(string expr, bool expected) 
            => expr.Calc().AssertReturns(expected);

        [TestCase(@"
if (x == 0) 'zero'
else 'positive' ", 2, "positive")]
        [TestCase(@"
if (x == 0) [0.0]
if (x == 1) [0.0,1.0]
if (x == 2) [0.0,1.0,2.0]
if (x == 3) [0.0,1.0,2.0,3.0]
else [0.0,0.0,0.0] ", 2, new[] {0.0, 1.0, 2.0})]
        [TestCase(@"
if (x==0) ['0']
if (x==1) ['0','1']
if (x==2) ['0','1','2']
if (x==3) ['0','1','2','3']
else ['0','0','0'] ", 2, new[] {"0", "1", "2"})]

        [TestCase(@"
if (x == 0) 'zero'
if (x == 1) 'one'
if (x == 2) 'two'
else 'not supported' ", 2, "two")]
        [TestCase("if (x==1) [1,2,3] else []", 1, new[] {1.0, 2, 3})]
        [TestCase("if (x==1) [1,2,3] else []", 0, new double[0])]
        public void SingleVariableEquatation(string expression, int x, object expected)
            => Funny.Hardcore
                .WithApriori<int>("x")
                .Build(expression)
                .Calc("x", x)
                .AssertOut(expected);

        [TestCase("y = if (1<2 )10 else -10.0", 10.0)]
        [TestCase("y = if (1>2 )-10.0 else 10", 10.0)]
        [TestCase("y = if (2>1 )10.0 else -10.0", 10.0)]
        [TestCase("y = if (2>1 )10.0\r else -10.0", 10.0)]
        [TestCase("y = if (2==1)10.0\r else -10", -10.0)]
        [TestCase("y = if (2<1 )10.0\r if (2>1) -10.0 else 0", -10.0)]
        [TestCase("y = if (1>2 )10.0\r if (1<2)-10.0\r else 0.0", -10.0)]
        public void ConstantRealEquation(string expr, double expected) 
            => expr.Calc().AssertReturns(expected);

        [Test]
        public void IfElseAsExpression() =>
            @"i:int  = 42 * if (x>0) x else -1
                arri = [if(x>0) x else -x, if(x<0) -1 else 1 ]"
                .Calc("x",10)
                .AssertResultHas(("i", 420),("arri", new[]{10,1}));

        [TestCase("y = if (3) else 4")]
        [TestCase("y = if 1 3")]
        [TestCase("y = if true then 3")]
        [TestCase("y = if 1>0  3")]
        [TestCase("y = if (1>0) 3 else")]
        [TestCase("y = if (1>0) else 4")]
        [TestCase("y = if (1>0) 2 if 3 else 4")]
        [TestCase("y = if (1>0) 3 5")]
        [TestCase("y = if (1>0) 5")]
        [TestCase("y = if else 3")]
        [TestCase("y = if (1>0) 3 if 2>0 then 2")]
        [TestCase("y = if (1>0) if 2>0 then 2 else 3")]
        [TestCase("y = then 3")]
        [TestCase("y = if 3")]
        [TestCase("y = if else 3")]
        [TestCase("y = else then 3")]
        [TestCase("y = if (2>1)  3 else true")]
        [TestCase("y = if (2>1)  3 if 2<1 then true else 1")]
        [TestCase("y = if (2>1)  false if 2<1 then true else 1")]
        public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();
    }
}