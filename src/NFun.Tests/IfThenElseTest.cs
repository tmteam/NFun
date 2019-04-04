using System;
using System.Linq;
using NFun;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class IfThenElseTest
    {
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
        public void NestedIfThenElse(double x1, double x2, double x3, int expected)
        {
            var expr = @"
                y = if x1 == 1 then 1 
                    if x1 == 2 then 
                        if x2 == 1 then 2
                        if x2 == 2 then 3
                        else 4
                    if x1 == 3 then 
                        if x2 == 1 then 
                            if x3 ==1 then 5
                            else 6
                        if x2 == 2 then 7
                        else 8 	
                    else if x2 == 4 then 9 
                         else 10";
            var runtime = FunBuilder.BuildDefault(expr);
               
            runtime.Calculate(
                    Var.New("x1",Convert.ToDouble(x1)),
                    Var.New("x2",Convert.ToDouble(x2)), 
                    Var.New("x3",Convert.ToDouble(x3)))
                .AssertReturns(Var.New("y", expected));
        }
        
        [TestCase("y = if 1<2 then 10 else -10", 10)]
        [TestCase("y = if 1>2 then -10 else 10", 10)]
        [TestCase("y = if 2>1 then 10 else -10", 10)]
        [TestCase("y = if 2>1 then 10\r else -10", 10)]
        [TestCase("y = if 2==1 then 10\r else -10", -10)]
        [TestCase("y = if 2<1 then 10 if 2>1 then -10 else 0", -10)]
        [TestCase("y = if 1>2 then 10\r if 1<2 then -10\r else 0", -10)]
        public void ConstantIntEquation(string expr, int expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }
        [TestCase("y = if 1<2 then true else false", true)]
        [TestCase("y = if true then true else false", true)]
        [TestCase("y = if true then true if false then false else true", true)]
        public void ConstantBoolEquation(string expr, bool expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }

       
        [TestCase(@"
if x == 0 then 'zero'
else 'positive' ", 2, "positive")]
        [TestCase(@"
if x == 0 then [0]
if x == 1 then [0,1]
if x == 2 then [0,1,2]
if x == 3 then [0,1,2,3]
else [0,0,0] ", 2, new[]{0,1,2})]
        [TestCase(@"
if x == 0 then ['0']
if x == 1 then ['0','1']
if x == 2 then ['0','1','2']
if x == 3 then ['0','1','2','3']
else ['0','0','0'] ", 2, new[]{"0","1","2"})]
        
        [TestCase(@"
if x == 0 then 'zero'
if x == 1 then 'one'
if x == 2 then 'two'
else 'not supported' ", 2, "two")]
        public void SingleVariableEquatation(string expression, int x, object expected)
        {
            FunBuilder.BuildDefault(expression).Calculate(Var.New("x", x))
                .AssertReturns(Var.New("out", expected));
        }
        
        [TestCase("y = if 1<2 then 10 else -10.0", 10.0)]
        [TestCase("y = if 1>2 then -10.0 else 10", 10.0)]
        [TestCase("y = if 2>1 then 10.0 else -10.0", 10.0)]
        [TestCase("y = if 2>1 then 10.0\r else -10.0", 10.0)]
        [TestCase("y = if 2==1 then 10.0\r else -10", -10.0)]
        [TestCase("y = if 2<1 then 10.0 if 2>1 then -10.0 else 0", -10.0)]
        [TestCase("y = if 1>2 then 10.0\r if 1<2 then -10.0\r else 0.0", -10.0)]
        public void ConstantRealEquation(string expr, double expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }
        
        [TestCase("y = if then 3 else 4")]
        [TestCase("y = if then 3")]
        [TestCase("y = if 1>0 then 3")]
        [TestCase("y = if (1>0) 3")]
        [TestCase("y = if (1>0) then 3 else")]
        [TestCase("y = if (1>0) then else 4")]
        [TestCase("y = if (1>0) then 2 if then 3 else 4")]
        [TestCase("y = if (1>0) then 3 then 5")]
        [TestCase("y = if (1>0) then then 5")]
        [TestCase("y = if else 3")]
        [TestCase("y = if 1>0 then 3 if 2>0 then 2")]
        [TestCase("y = if 1>0 then if 2>0 then 2 else 3")]
        [TestCase("y = then 3")]
        [TestCase("y = else then 3")]
        public void ObviouslyFailsOnParsing(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
        
        [TestCase("y = if 2>1 then 3 else true")]
        [TestCase("y = if 2>1 then 3 if 2<1 then true else 1")]
        [TestCase("y = if 2>1 then false if 2<1 then true else 1")]
        public void ObviouslyFailsOnOuputCast(string expr) =>
            Assert.Throws<OutpuCastFunParseException>(
                ()=> FunBuilder.BuildDefault(expr));
        
        
    }
}