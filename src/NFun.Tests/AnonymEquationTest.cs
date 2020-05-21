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
            => FunBuilder.Build(script).Calculate().AssertOutEquals(expected);

        [TestCase("1", 1.0)]
        [TestCase("0x1", 1)]
        [TestCase("true", true)]
        [TestCase("2*0x3", 6)]
        [TestCase("true == false", false)]
        [TestCase("true==true==1",false)]
        [TestCase("8==8==1", false)]
        [TestCase("true==true==true", true)]
        [TestCase("8==8==8", false)]
        [TestCase("[0,0,1]==[0,false,1]", false)]
        [TestCase("[false,0,1]==[0,false,1]", false)]
        [TestCase("0 == 0 == 8",false)]
        [TestCase("8 == 1 == 0",false)]
        [TestCase("true == 1",false)]
        [TestCase("if (2<3) true else false", true)]
        [TestCase("y(x) = x*2 \r y(3.0) * y(4.0)", 48.0)]
        [TestCase("y(x) = x \r y(3.0)", 3.0)]
        [TestCase("y(x) = x*2 \r y(3.0)  \r z(j) = j*j", 6.0)]
        public void AnonymousExpressionConstantEquatation(string expr, object expected)
            => FunBuilder.Build(expr).Calculate().AssertOutEquals(expected);
    }


}
