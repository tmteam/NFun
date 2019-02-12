using System.Linq;
using Funny.Interpritation;
using Funny.Runtime;
using Funny.Tokenization;
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
        public void NestedIfThenElse(double x1, double x2, double x3, double expected)
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
            var runtime = Interpreter.BuildOrThrow(expr);
               
            runtime.Calculate(
                    Var.Number("x1",x1),
                    Var.Number("x2",x2), 
                    Var.Number("x3",x3))
                .AssertReturns(Var.Number("y", expected));
        }
        
        [TestCase("y = if 1<2 then 10 else -10", 10)]
        [TestCase("y = if 1>2 then -10 else 10", 10)]
        [TestCase("y = if 2>1 then 10 else -10", 10)]
        [TestCase("y = if 2>1 then 10\r else -10", 10)]
        [TestCase("y = if 2==1 then 10\r else -10", -10)]
        [TestCase("y = if 2<1 then 10 if 2>1 then -10 else 0", -10)]
        [TestCase("y = if 1>2 then 10\r if 1<2 then -10\r else 0", -10)]
        public void ConstantEquatation(string expr, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            var res = runtime.Calculate();
            Assert.AreEqual(1, res.Results.Length);
            Assert.AreEqual(expected, res.Results.First().Value);
        }
        
        [TestCase("y = if then 3")]
        [TestCase("y = if 1>0 then 3")]
        [TestCase("y = if (1>0) 3")]
        [TestCase("y = if (1>0) then 3 else")]
        [TestCase("y = if (1>0) then else 4")]
        [TestCase("y = if (1>0) then 2 if then 3 else 4")]
        [TestCase("y = if then 3 else 4")]
        [TestCase("y = if (1>0) then 3 then 5")]
        [TestCase("y = if (1>0) then then 5")]
        [TestCase("y = if else 3")]
        [TestCase("y = if 1>0 then 3 if 2>0 then 2")]
        [TestCase("y = if 1>0 then if 2>0 then 2 else 3")]
        [TestCase("y = then 3")]
        [TestCase("y = else then 3")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
    }
}