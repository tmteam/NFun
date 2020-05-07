using System;
using System.Collections.Generic;
using System.Text;
using NFun;
using NUnit.Framework;

namespace Funny.Tests
{
    class AnonymEquationTest
    {
        [TestCase("1",1.0)]
        [TestCase("true", true)]
        [TestCase("(1+2)",3.0)]
        [TestCase("f(x)= x; (f(42))", 42.0)]
        [TestCase("f(x)= (x); (f(42))", 42.0)]
        [TestCase("f()= (2); (1)", 1.0)]

        public void ConstantEquation(string script, object expected) 
            => FunBuilder.BuildDefault(script).Calculate().AssertOutEquals(expected);
    }
}
