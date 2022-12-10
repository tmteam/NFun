using System;
using System.Globalization;
using System.Linq;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

// ReSharper disable StringLiteralTypo

namespace NFun.SyntaxTests.SyntaxDialect;

public class RealIsDecimalTest {
    [TestCase("[0.0,2.0,3.0].avg() == 5/3", true)]
    [TestCase("[1.0,2.0,3.0].any(rule(i)= i == 1.0)", true)]
    [TestCase("[1.0,2.0,3.0].any(rule(i)= i == 0.0)", false)]
    [TestCase("[1.0,2.0,3.0].all(rule(i)= i >0)", true)]
    [TestCase("[1.0,2.0,3.0].all(rule(i)= i >1.0)", false)]
    [TestCase("f(m:real[], p):bool = m.all(rule(i)= i>p) \r f([1.0,2.0,3.0],1.0)", false)]
    [TestCase("toText([1.1,2.2,3.3]) == '[1.1,2.2,3.3]'", true)]
    [TestCase("toText(0.2+0.3) == '0.5'", true)]
    [TestCase("0.2 > 0.3", false)]
    [TestCase("0.2 < 0.3", true)]
    [TestCase("0.2 == 0.3", false)]
    public void ConstToBoolCalc(string expr, bool expected)
        => expr
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc()
            .AssertAnonymousOut(expected);

    [TestCase("x== 2.0", 2.0, true)]
    [TestCase("if (x<3) true else false", 2.0, true)]
    public void SingleArgDecimalToBoolCalc(string expr, Decimal arg, Boolean expected)
        => expr
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc("x", arg)
            .AssertAnonymousOut(expected);

    [TestCase("x:real; x", 2.0, 2.0)]
    [TestCase("x:real; x*3", 2.0, 6.0)]
    [TestCase("y(x) = x*2 \r y(x) * y(4.0)", 3.0, 48.0)]
    [TestCase("x + 0.2", 0.3, 0.5)]
    [TestCase("x - 0.2", 0.3, 0.1)]
    [TestCase("-x", -0.3, 0.3)]
    [TestCase("-x", 0.3, -0.3)]
    [TestCase("x * 0.2", 0.3, 0.06)]
    [TestCase("x / 0.2", 0.3, 1.5)]
    [TestCase("x ** 2.0", 0.3, 0.09)]
    [TestCase("x % 0.2", 0.3, 0.1)]
    [TestCase("abs(x)", -0.3, 0.3)]
    [TestCase("sqrt(x)", 4.0, 2.0)]
    [TestCase("cos(x)", 0.0, 1.0)]
    [TestCase("sin(x)", 0.0, 0.0)]
    [TestCase("acos(x)", 1.0, 0.0)]
    [TestCase("asin(x)", 0.0, 0.0)]
    [TestCase("atan(x)", 0.0, 0.0)]
    [TestCase("tan(x)", 0.0, 0.0)]
    [TestCase("exp(x)", 0.0, 1.0)]
    [TestCase("log(x,10)", 1.0, 0.0)]
    [TestCase("log(x)", 1.0, 0.0)]
    [TestCase("log10(x)", 1.0, 0.0)]
    [TestCase("round(x,1)", 1.66666, 1.7)]
    [TestCase("round(x,2)", 1.222, 1.22)]
    [TestCase("round(x,0)", 1.66666, 2.0)]
    [TestCase("round(x, 0)", 1.2, 1.0)]
    [TestCase("x:real; min(x, 1)", 0.5, 0.5)]
    public void SingleArgDecimalCalc(string expr, Decimal arg, Decimal expected)
        => expr
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc("x", arg)
            .AssertAnonymousOut(expected);

    [TestCase("out:real[]   = range(0,5)", new[] { 0.0, 1, 2, 3, 4, 5 })]
    [TestCase("out:real[]   = range(-2.5,2.4)", new[] { -2.5, -1.5, -0.5, 0.5, 1.5 })]
    [TestCase("out:real[]   = range(-2.5,2.5)", new[] { -2.5, -1.5, -0.5, 0.5, 1.5, 2.5 })]
    [TestCase("out:real[]   = range(-2.5,2.6)", new[] { -2.5, -1.5, -0.5, 0.5, 1.5, 2.5 })]
    [TestCase("out:real[]   = range(-2.4,2.4)", new[] { -2.4, -1.4, -0.4, 0.6, 1.6 })]
    [TestCase("out:real[]   = range(-2.4,2.5)", new[] { -2.4, -1.4, -0.4, 0.6, 1.6 })]
    [TestCase("out:real[]   = range(-2.4,2.6)", new[] { -2.4, -1.4, -0.4, 0.6, 1.6, 2.6 })]
    [TestCase("out:real[]   = range(-2.4,2.7)", new[] { -2.4, -1.4, -0.4, 0.6, 1.6, 2.6 })]
    [TestCase("out:real[]   = range(-5,5)", new double[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:real[]   = range(5,0)", new[] { 5, 4, 3, 2, 1, 0.0 })]
    [TestCase("out:real[]   = range(5,-5)", new double[] { 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5 })]
    [TestCase("out:real[]   = range(2.5,-2.4)", new[] { 2.5, 1.5, 0.5, -0.5, -1.5 })]
    [TestCase("out:real[]   = range(2.5,-2.5)", new[] { 2.5, 1.5, 0.5, -0.5, -1.5, -2.5 })]
    [TestCase("out:real[]   = range(2.5,-2.6)", new[] { 2.5, 1.5, 0.5, -0.5, -1.5, -2.5 })]
    [TestCase("out:real[]   = range(2.4,-2.4)", new[] { 2.4, 1.4, 0.4, -0.6, -1.6 })]
    [TestCase("out:real[]   = range(2.4,-2.5)", new[] { 2.4, 1.4, 0.4, -0.6, -1.6 })]
    [TestCase("out:real[]   = range(2.4,-2.6)", new[] { 2.4, 1.4, 0.4, -0.6, -1.6, -2.6 })]
    [TestCase("out:real[]   = range(2.4,-2.7)", new[] { 2.4, 1.4, 0.4, -0.6, -1.6, -2.6 })]
    [TestCase("range(7,10.0)", new[] { 7.0, 8, 9, 10 })]
    [TestCase("range(7.0,10.0)", new[] { 7.0, 8, 9, 10 })]
    [TestCase("range(1,10,2.0)", new[] { 1.0, 3.0, 5.0, 7.0, 9.0 })]
    public void SingleArgDecimalCalc(string expr, double[] expected)
        => expr
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc()
            .AssertAnonymousOut(expected.Select(s => (decimal)s).ToArray());


    [TestCase("[1.0,2.0,3.0] . fold(rule(i,j)=i+j)", 6.0)]
    [TestCase("[1.0,2.0,3.0] . fold(rule(i:real,j)=i+j)", 6.0)]
    [TestCase("[1.0,2.0,3.0] . fold(rule(i,j:real):real=i+j)", 6.0)]
    [TestCase("[1.0,2.0,3.0] . fold(rule(i,j):real=i+j)", 6.0)]
    [TestCase("fold([1.0,2.0,3.0],rule(i,j)=i+j)", 6.0)]
    [TestCase("[1,2,3] . fold(rule(i:real, j:real)=i+j)", 6)]
    [TestCase("out:real = [-1,-2,0,1,2,3].filter(rule it>0).fold(rule(a:int,b)= a+b)", 6)]
    [TestCase("out:real = [-1,-2,0,1,2,3].filter((rule it>0)).fold(rule(i,j)= i+j)", 6)]
    [TestCase("out:real = [-1,-2,0,1,2,3].filter((rule it>0)).fold(rule(a,b)= a+b)", 6)]
    [TestCase("car3(g) = g(2);   out:real = car3(rule(x)=x-1)   ", 1)]
    [TestCase("car4(g) = g(2);   out:real = car4(rule it)   ", 2)]
    [TestCase("call5(f, x) = f(x); out:real = call5(rule(x)=x+1,  1)", 2)]
    [TestCase("call6(f, x) = f(x); out:real = call6(rule(x)=x+1.0, 1.0)", 2.0)]
    [TestCase("call7(f, x) = f(x); out:real = call7(rule(x:real)=x+1.0, 1.0)", 2.0)]
    [TestCase("call8(f) = rule(i)=f(i); out:real = call8(rule(x)=x+1)(2)", 3)]
    [TestCase("call9(f) = rule(i)=f(i); out:real = (rule(x)=x+1).call9()(2)", 3)]
    [TestCase("mult(x)= rule(y)=x*y;    out:real = mult(2)(3)", 6)]
    [TestCase("mult(x)= rule(y)=rule(z)=x*y*z;    mult(2.0)(3)(4)", 24.0)]
    [TestCase("(rule(x)=x+1)(3.0)", 4.0)]
    [TestCase("(rule it+1)(3.0)", 4.0)]
    [TestCase("(rule(a)=rule(b)=a+b)(3.0)(5.0)", 8.0)]
    [TestCase("sum([1.0,2.5,6.0])", 9.5)]
    [TestCase("[1.0..3.0].sum()", 6.0)]
    [TestCase("[3.5..1.5].sum()", 7.5)]
    [TestCase("[1.0..2.0 step 0.4].sum()", 4.2)]
    [TestCase("max([1.0,10.5,6.0])", 10.5)]
    [TestCase("max([1,-10,0.0])", 1.0)]
    [TestCase("max([1,0.0])", 1.0)]
    [TestCase("max(1.0,3.4)", 3.4)]
    [TestCase("median([1.0,10.5,6.0])", 6.0)]
    [TestCase("abs(1.0)", 1.0)]
    [TestCase("abs(-1.5)", 1.5)]
    [TestCase("15 - min(abs(1-4), 7.0)", 12.0)]
    [TestCase("sqrt(0)", 0.0)]
    [TestCase("sqrt(1.0)", 1.0)]
    [TestCase("sqrt(4.0)", 2.0)]
    [TestCase("cos(0)", 1.0)]
    [TestCase("sin(0)", 0.0)]
    [TestCase("acos(1)", 0.0)]
    [TestCase("asin(0)", 0.0)]
    [TestCase("atan(0)", 0.0)]
    [TestCase("tan(0)", 0.0)]
    [TestCase("exp(0)", 1.0)]
    [TestCase("log(1,10)", 0.0)]
    [TestCase("log(1)", 0.0)]
    [TestCase("log10(1)", 0.0)]
    [TestCase("round(1.66666,1)", 1.7)]
    [TestCase("round(1.222,2)", 1.22)]
    [TestCase("round(1.66666,0)", 2.0)]
    [TestCase("round(1.2,0)", 1.0)]
    [TestCase("min(0.5, 1)", 0.5)]
    [TestCase("avg([1.0,2.0,6.0])", 3.0)]
    [TestCase("sum([1.0,2,3])", 6.0)]
    [TestCase("sum([1.0,2.5,6.0])", 9.5)]
    [TestCase("max([1.0,10.5,6.0])", 10.5)]
    [TestCase("max([1,-10,0.0])", 1.0)]
    [TestCase("max([1,0.0])", 1.0)]
    [TestCase("max(1.0,3.4)", 3.4)]
    [TestCase("median([1.0,10.5,6.0])", 6.0)]
    [TestCase("0.09.sqrt()", 0.3)]
    [TestCase("f(a) = a; f(42.5)", 42.5)]
    [TestCase("f(a) = a+1; f(42.5)", 43.5)]
    public void ConstDecimalCalc(string expr, Decimal expected)
        => expr
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc().AssertAnonymousOut(expected);

    [Test]
    public void BigIntNumberParsedWell() =>
        "36893488147419103230.0"
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc().AssertAnonymousOut(new decimal(ulong.MaxValue) * 2);

    [Test]
    public void BigNegativeIntNumberParsedWell() =>
        "-36893488147419103230.0"
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc().AssertAnonymousOut(new decimal(ulong.MaxValue) * -2);

    [TestCase("0.0", "0.0")]
    [TestCase("-0.1", "-0.1")]
    [TestCase("-368.000000000001", "-368.000000000001")]
    [TestCase("0.000_000_000_000_000_000_000_0012345", "0.0000000000000000000000012345")]
    [TestCase("0.0000000000000000000000012345", "0.0000000000000000000000012345")]
    [TestCase("00001232364273645716283_76_187.01231", "123236427364571628376187.01231")]
    public void DecimalNumberParsedWell(string expr, string expected) =>
        expr
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc().AssertAnonymousOut(Parse(expected));

    [TestCase("0.0000000000000000000000012345")]
    [TestCase("-368.000000000001")]
    [TestCase("123236427364571628376187.01231")]
    public void ToTextTest(string literal) =>
        $"({literal}).toText()"
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc().AssertAnonymousOut(literal);

    [TestCase("[1.1,2.2,3.3]", "[1.1,2.2,3.3]")]
    [TestCase("0.5+0.6", "1.1")]
    public void ToTextComplexTest(string literal, string expectedText) =>
        $"({literal}).toText()"
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc().AssertAnonymousOut(expectedText);

    [Test]
    public void CalcSingleConstDecimal() {
        var dec = Funny
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .Calc<decimal>("36893488147419103230.0");
        Assert.AreEqual(dec, new decimal(ulong.MaxValue) * 2);
    }

    [Test]
    public void CalcSingleConstWithSetupDecimal() {
        var dec = Funny
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .WithConstant("ma", new decimal(ulong.MaxValue) * 2)
            .Calc<decimal>("ma-1");
        Assert.AreEqual(dec, new decimal(ulong.MaxValue) * 2 - 1);
    }

    [TestCase("1.1")]
    [TestCase("0.1")]
    [TestCase("-42.21")]
    [TestCase("0.00000000000001234567")]
    [TestCase("12345.666666666")]
    public void PreciseConstantTest(string number) => Assert.AreEqual(number,
        ((decimal)Funny
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .Calc(number))
        .ToString(CultureInfo.InvariantCulture));

    [Test]
    public void ConstDecimalArrayCalc_1() =>
        AssertConstDecimalArrayCalc("out:real[] = [11.0,20.0,1.0,2.0].filter(rule(i)= i>10)", (decimal)11.0,
            (decimal)20.0);

    [Test]
    public void ConstDecimalArrayCalc_2() =>
        AssertConstDecimalArrayCalc("out:real[] = map([1,2,3], rule(i:int):real  =i*i)", (decimal)1.0, 4, 9);

    [Test]
    public void ConstDecimalArrayCalc_3() =>
        AssertConstDecimalArrayCalc("out:real[] = [1.0,2.0,3.0] . map(rule(i)=i*i)", (decimal)1.0, 4, 9);

    [Test]
    public void ConstDecimalArrayCalc_4() =>
        AssertConstDecimalArrayCalc("out:real[] = [-7,-2,0,1,2,3].filter(rule it>0)", 1, 2, 3);

    [Test]
    public void ConstDecimalArrayCalc_5() =>
        AssertConstDecimalArrayCalc("out:real[] = [-1,-2,0,1,2,3].filter((rule it>0)).filter(rule(i)=i>2)", 3);

    [Test]
    public void ConstDecimalArrayCalc_6() => AssertConstDecimalArrayCalc(
        "out:real[] = [-1,-2,0,1,2,3].filter((rule it>0)).map(rule(i)=i*i).map(rule(i:int)=i*i)", 1, 16, 81);

    [Test]
    public void ConstDecimalArrayCalc_7() =>
        AssertConstDecimalArrayCalc(
            "out:real[] = [-1,-2,0,1,2,3].filter((rule it>0)).map(rule(i)=i*i).map(rule(i)=i*i)", 1, 16, 81);

    [Test]
    public void ConstDecimalArrayCalc_8() => AssertConstDecimalArrayCalc(
        "out:real[] = [-1,-2,0.0,1,2,3].filter((rule it>0)).map(rule(i)=i*i).map(rule(i)=i*i)", 1, 16, 81);

    [Test]
    public void ConstDecimalArrayCalc_9() =>
        AssertConstDecimalArrayCalc("out:real[] = [-1,-2,0,1,2,3].filter((rule it>0)).filter(rule(a)=a>2)", 3);

    [Test]
    public void ConstDecimalArrayCalc_10() =>
        AssertConstDecimalArrayCalc(
            "out:real[] = [-1,-2,0,1,2,3].filter((rule it>0)).map(rule(a)=a*a).map(rule(b)=b*b)", 1, 16, 81);

    [Test]
    public void ConstDecimalArrayCalc_11() => AssertConstDecimalArrayCalc(
        "out:real[] = [-1,-2,0,1,2,3].filter(rule it>0).map(rule(a)=a*a).map(rule(b:int)=b*b)", 1, 16, 81);

    private void AssertConstDecimalArrayCalc(string expr, params Decimal[] expected)
        => expr
            .BuildWithDialect(realClrType: RealClrType.IsDecimal)
            .Calc().AssertAnonymousOut(expected);

    private decimal Parse(string number) => decimal.Parse(number,
        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    [TestCase("0xFFFF_FFFFF_FFFF_FFFFF_FFFF_FFFFF_FFFF_FFFFF")]
    [TestCase("-36893488147419103230")]
    [TestCase("36893488147419103230")]
    public void ObviousFails(string expr) =>
        TestHelper.AssertObviousFailsOnParse(() =>
            expr.BuildWithDialect(realClrType: RealClrType.IsDecimal));
}
