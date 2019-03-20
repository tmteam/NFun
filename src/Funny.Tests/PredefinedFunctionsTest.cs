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
        [TestCase("y = count([1,2,3])",3)]
        [TestCase("y = count([])",0)]
        [TestCase("y = count([1.0,2.0,3.0])",3)]
        [TestCase("y = count([[1,2],[3,4]])",2)]
        [TestCase("y = avg([1,2,3])",2.0)]
        [TestCase("y = avg([1.0,2.0,6.0])",3.0)]
        [TestCase("y = sum([1,2,3])",6)]
        [TestCase("y = sum([1.0,2.5,6.0])",9.5)]
        [TestCase("y = max([1.0,10.5,6.0])",10.5)]
        [TestCase("y = max([1,-10,0])",1)]
        [TestCase("y = max(1.0,3.4)",3.4)]
        [TestCase("y = max(4,3)",4)]
        [TestCase("y = min([1.0,10.5,6.0])",1.0)]
        [TestCase("y = min([1,-10,0])",-10)]
        [TestCase("y = min(1.0,3.4)",1.0)]
        [TestCase("y = min(4,3)",3)]
        [TestCase("y = median([1.0,10.5,6.0])",6.0)]
        [TestCase("y = median([1,-10,0])",0)]        
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
        [TestCase("y= max([])")]
        [TestCase("y= max(['a','b'])")]
        [TestCase("y= max('a','b')")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
        
    }
}