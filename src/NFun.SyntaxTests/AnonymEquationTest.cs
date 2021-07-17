using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    class AnonymEquationTest
    {
        [TestCase("1", 1)]
        [TestCase("true", true)]
        [TestCase("(1+2)", 3)]
        [TestCase("f(x)= x; (f(42))", 42)]
        [TestCase("f(x)= x; (f(42.0))", 42.0)]
        [TestCase("f(x)= (x); (f(42))", 42)]
        [TestCase("f()= (2); (1)", 1)]
        public void ConstantEquation(string expr, object expected) => expr.AssertOut(expected);

        [TestCase("x:real\r x", 2.0, 2.0)]
        [TestCase("x== 2.0", 2.0, true)]
        [TestCase("x:real \rx*3", 2.0, 6.0)]
        [TestCase("x*3", 2, 6)]
        [TestCase("\rx*3", 2, 6)]
        [TestCase("if (x<3) true else false", 2.0, true)]
        [TestCase("y(x) = x*2 \r y(x) * y(4.0)", 3.0, 48.0)]
        public void AnonymousExpressionSingleVariableEquatation(string expr, object arg, object expected)
            => expr.Calc("x",arg).AssertOut(expected);

        [TestCase("1", 1)]
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
        [TestCase("if (false) true else false", false)]
        [TestCase("y(x) = x*2 \r y(3.0) * y(4.0)", 48.0)]
        [TestCase("y(x) = x \r y(3.0)", 3.0)]
        [TestCase("y(x) = x*2 \r y(3.0)  \r z(j) = j*j", 6.0)]
        public void AnonymousExpressionConstantEquatation(string expr, object expected)
            => expr.AssertOut(expected);
    }
}
