using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class RuntimeErrorsTest {

    // ── Integer overflow / division by zero / negative repeat etc. ─────
    // All expressions parse cleanly but throw FunnyRuntimeException on Calc().

    [TestCase("y = 2147483647 + 1",              TestName = "Int32 overflow")]
    [TestCase("y = -2147483648 - 1",             TestName = "Int32 underflow")]
    [TestCase("y = 2 ** 32",                     TestName = "Int32 power overflow")]
    [TestCase("y:int64 = 9223372036854775807 + 1", TestName = "Int64 overflow")]
    [TestCase("y = 1 // 0",                      TestName = "Int division by zero")]
    [TestCase("y = 5 % 0",                       TestName = "Modulo by zero")]
    [TestCase("y:int[] = []\r z = y.last()",     TestName = "last() of empty array")]
    [TestCase("y:int[] = []\r z = y.median()",   TestName = "median() of empty array")]
    [TestCase("y:real[] = []\r z = y.avg()",     TestName = "avg() of empty array")]
    [TestCase("y:int[] = []\r z = y.max()",      TestName = "max() of empty array")]
    [TestCase("y:int[] = []\r z = y.min()",      TestName = "min() of empty array")]
    [TestCase("y = repeat(1, -1)",               TestName = "repeat with negative count")]
    [TestCase("y = [1,2,3,4,5][3:1]",            TestName = "backwards slice")]
    [TestCase("y:int = -2147483648\r out = -y",  TestName = "negate Int32.MinValue overflows")]
    public void RuntimeExceptionExpected(string expr) =>
        Assert.Throws<FunnyRuntimeException>(() => expr.Calc());
}
