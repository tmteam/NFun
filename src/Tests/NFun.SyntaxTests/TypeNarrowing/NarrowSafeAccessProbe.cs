namespace NFun.SyntaxTests.TypeNarrowing;
using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class NarrowSafeAccessProbe {
    private static object Calc(string expr) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .Get("y");

    [Test]
    public void SafeFieldAccess_NotNone_NarrowsRoot() =>
        // a?.foo != none → a is non-none → a.boo works
        Assert.AreEqual(42, Calc(
            "a = if(true) {foo = true, boo = 42} else none\r y = if(a?.foo != none) a.boo else 0"));

    [Test]
    public void SafeFieldAccess_EqualTrue_NarrowsRoot() =>
        Assert.AreEqual(42, Calc(
            "a = if(true) {foo = true, boo = 42} else none\r y = if(a?.foo == true) a.boo else 0"));

    [Test]
    public void SafeFieldAccess_NoneInput_ReturnsElse() =>
        Assert.AreEqual(0, Calc(
            "a = if(false) {foo = true, boo = 42} else none\r y = if(a?.foo == true) a.boo else 0"));
}
