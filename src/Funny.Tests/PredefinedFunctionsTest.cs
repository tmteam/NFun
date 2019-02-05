using System;
using Funny.Interpritation;
using Funny.Runtime;
using Funny.Tokenization;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class PredefinedFunctionsTest
    {
        [TestCase("y = abs(1)",1)]
        [TestCase("y = abs(-1)",1)]
        [TestCase("y = add(1,2)",3)]
        [TestCase("y = add(add(1,2),add(3,4))",10)]
        [TestCase("y = abs(1-4)",3)]
        [TestCase("y = 15 - add(abs(1-4), 7)",5)]
        [TestCase("y = pi()",Math.PI)]
        [TestCase("y = e()",Math.E)]

        public void ConstantEquatationWithPredefinedFunction(string expr, double expected)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        
        [TestCase("y = abs(x)",1,1)]
        [TestCase("y = abs(-x)",-1,1)]
        [TestCase("y = add(x,2)",1,3)]
        [TestCase("y = add(1,x)",2,3)]
        [TestCase("y = add(add(x,x),add(x,x))",1,4)]
        [TestCase("y = abs(x-4)",1,3)]
        
        public void EquatationWithPredefinedFunction(string expr, double arg, double expected)
        {
            var runtime = Interpriter.BuildOrThrow(expr);
            runtime.Calculate(Var.New("x", arg))
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        [TestCase("y = pi(")]
        [TestCase("y = pi(1)")]
        [TestCase("y = abs(")]
        [TestCase("y = abs)")]
        [TestCase("y = abs()")]
        [TestCase("y = abs(1,2)")]
        [TestCase("y = abs(1 2)")]
        [TestCase("y = add(")]
        [TestCase("y = add()")]
        [TestCase("y = add(1)")]
        [TestCase("y = add 1")]
        [TestCase("y = add(1,2,3)")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpriter.BuildOrThrow(expr));
        
    }
}