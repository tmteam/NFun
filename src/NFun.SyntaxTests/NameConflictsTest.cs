using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    public class NameConflictsTest
    {
        [TestCase("concat = 1+2.0","concat",3.0)]
        [TestCase("min = 1+2","min",3)]
        [TestCase("max = 1+0x2","max",3)]
        public void OutputNameOverloadsBuiltinFunctionName(string expr, string output, object expected) => 
            expr.AssertResultHas(output,expected);

        [TestCase("concat = 1+2 \r y = concat*3 ","y", 9)]
        [TestCase("count = 1+2 \r y = count*3 \r ","y", 9)]
        public void OverloadOutputUsesInOtherEquation(string expr, string output, object expected) 
            => expr.AssertResultHas(output,expected);
        
        [TestCase("min",3, "min:int \r y = min*3 ","y",9)]
        [TestCase("max",3, "max:int \r max*3 ","out",9)]
        public void InputNameOverloadsBuiltinFunctionName(string iname, object ival, string expr, string oname, object oval) 
            => expr.Calc(iname, ival).AssertResultHas(oname,oval);
        
        [TestCase("foo(x) = x +1\r y=foo*3 ")]
        [TestCase("\r y=foo*3 \r foo(x) = x +1")]
        [TestCase("\r y=Foo*3 \r foo(x) = x +1")]
        [TestCase("foo(x) = x +1\r foo*3 ")]
        [TestCase("foo(x) = x +1\r Foo*3 ")]
        [TestCase("foo(x) = x +1\r foo = 1+2")]
        [TestCase("foo(x) = x +1\r foo = 1+2 \r y = foo*3 ")]
        [TestCase("foo(x) = x +1\r foo = 1+2 \ry = foo*3  ")]
        [TestCase("foo(x) = x +1\r foo = 1+2 \ry = foo*3 \r  ")]
        public void ObviousFails(string expr) => expr.AssertObviousFailsOnParse();
    }
}