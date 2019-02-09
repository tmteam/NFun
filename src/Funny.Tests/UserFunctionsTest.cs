using System;
using Funny.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class UserFunctionsTest
    {
        [TestCase("mult(a,b) = a*b\r y = mult(3,4)+1",13)]
        public void ConstantEquatationWithPredefinedFunction(string expr, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }
    }
}