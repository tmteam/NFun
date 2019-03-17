using System;
using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class PredefinedFunctionsTest
    {
        [TestCase("y = abs(1)",1.0)]
        [TestCase("y = abs(-1)",1.0)]
        [TestCase("y = add(1,2)",3.0)]
        [TestCase("y = add(add(1,2),add(3,4))",10.0)]
        [TestCase("y = abs(1-4)",3.0)]
        [TestCase("y = 15 - add(abs(1-4), 7)",5.0)]
        [TestCase("y = pi()",Math.PI)]
        [TestCase("y = e()",Math.E)]
        [TestCase("y = length([1,2,3])",3)]
        [TestCase("y = length([])",0)]
        [TestCase("y = length([1.0,2.0,3.0])",3)]
        [TestCase("y = length([[1,2],[3,4]])",2)]
        [TestCase("y = avg([1,2,3])",2.0)]
        [TestCase("y = avg([1.0,2.0,6.0])",3.0)]
        public void ConstantEquationWithPredefinedFunction(string expr, object expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        
        [TestCase("y = abs(x)",1,1)]
        [TestCase("y = abs(-x)",-1,1)]
        [TestCase("y = add(x,2)",1,3)]
        [TestCase("y = add(1,x)",2,3)]
        [TestCase("y = add(add(x,x),add(x,x))",1,4)]
        [TestCase("y = abs(x-4)",1,3)]
        
        public void EquationWithPredefinedFunction(string expr, double arg, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate(Var.New("x", arg))
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        [TestCase("y = pi(")]
        [TestCase("y = pi(1)")]
        [TestCase("y = abs(")]
        [TestCase("y = abs)")]
        [TestCase("y = abs()")]
        [TestCase("y = abs(1,)")]
        [TestCase("y = abs(1,,2)")]
        [TestCase("y = abs(,,2)")]
        [TestCase("y = abs(,,)")]
        [TestCase("y = abs(2,)")]
        [TestCase("y = abs(1,2)")]
        [TestCase("y = abs(1 2)")]
        [TestCase("y = add(")]
        [TestCase("y = add()")]
        [TestCase("y = add(1)")]
        [TestCase("y = add 1")]
        [TestCase("y = add(1,2,3)")]
        [TestCase("y = avg(['1','2','3'])")]

        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
        
    }
}