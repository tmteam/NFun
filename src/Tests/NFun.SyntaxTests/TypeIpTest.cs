using System.Net;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class TypeIpTest {
    [TestCase("out:ip = 127.0.0.1", "127.0.0.1")]
    [TestCase("out = 127.0.0.1", "127.0.0.1")]
    [TestCase("out:ip = default", "0.0.0.0")]
    [TestCase("out:any = 0.0.0.12", "0.0.0.12")]
    public void ConstantEquation_ResultIsIp(string expr, string expectedIp) =>
        expr.AssertResultHas("out", IPAddress.Parse(expectedIp));

    [TestCase("127.0.0.1.toText()", "127.0.0.1")]
    [TestCase("127.0.0.1 == 127.0.0.1", true)]
    [TestCase("127.0.0.1 == default", false)]
    [TestCase("0.0.0.0 == default", true)]
    [TestCase("0.0.0.1 == default", false)]
    [TestCase("default == 0.0.0.0", true)]
    [TestCase("127.0.0.1 != 127.0.0.1", false)]
    [TestCase("127.0.0.2 != default", true)]
    [TestCase("127.0.0.1 != default", true)]
    [TestCase("0.0.0.0 != default", false)]
    [TestCase("0.0.0.1 != default", true)]
    [TestCase("default != 0.0.0.0", false)]
    [TestCase("x:ip = default; out = x.toText()", "0.0.0.0")]
    [TestCase("x:any = 253.12.3.7; y:any = 253.12.3.7; out = x == y", true)]
    [TestCase("x:any = 253.12.3.7; y:any = 253.12.3.8; out = x == y", false)]
    [TestCase("x:any = 253.12.3.7; y:ip = 254.12.3.7; out = x == y", false)]
    [TestCase("x:ip = 253.12.3.7; y:any = 253.12.3.7; out = x == y", true)]
    [TestCase("[12, 127.0.0.1][0] == 12", true)]
    [TestCase("[12.0, 127.0.0.1][0] == 12.0", true)]
    [TestCase("[127.0.0.1,12.0][0] == 127.0.0.1", true)]
    [TestCase("[12, 127.0.0.1][0] == 127.0.0.1", false)]
    [TestCase("[12.0, 127.0.0.1][0] == 127.0.0.1", false)]
    [TestCase("[ 127.0.0.1,12.0][0] == 12", false)]
    [TestCase("x:any[] = [127.0.0.1, 12.0]; out = x[0] == 127.0.0.1", true)]
    [TestCase("x = [127.0.0.1, 12.0]; out = x[0] == 12", false)]
    public void ConstantEquation_2(string expr, object expected) => expr.AssertResultHas("out", expected);

    [TestCase("1.2.3.x> default")]
    [TestCase("400.x.x.x")]
    [TestCase("256.0.0.1")]
    [TestCase("0.256.0.1")]
    [TestCase("0.0.256.1")]
    [TestCase("0.0.0.256")]
    [TestCase("0.0.350.0")]
    [TestCase("0x100.0.0.1")]
    [TestCase("0.0x100.0.1")]
    [TestCase("0.0.0x100.1")]
    [TestCase("0.0.0.0x100")]
    [TestCase("127.0.0.1.0")]
    [TestCase("127,0.0.1")]
    [TestCase("127.0,0.1")]
    [TestCase("127.0.0,1")]
    [TestCase("127:0.0.1")]
    [TestCase("127.0:0.1")]
    [TestCase("127.0.0:1")]
    [TestCase("0.400.0.0")]
    [TestCase("0.0.400.0")]
    [TestCase("0.0.0.400")]
    [TestCase("x:byte[] = 0.0.0.400")]
    [TestCase("x:int = 0.0.0.400")]
    [TestCase("-0.0.0.0")]
    [TestCase("+0.0.0.0")]
    [TestCase("-(0.0.0.0)")]
    [TestCase("+(0.0.0.0)")]
    [TestCase("0.0.0.0-")]
    [TestCase("0.0.0.0+")]
    [TestCase(".0.0.0.0")]
    [TestCase("0.0.0.0.")]
    [TestCase("0.0.0.")]
    [TestCase("0.0.0..0")]
    [TestCase("0.0.0")]
    public void ObviousFails(string expr) =>
        expr.AssertObviousFailsOnParse();
}
