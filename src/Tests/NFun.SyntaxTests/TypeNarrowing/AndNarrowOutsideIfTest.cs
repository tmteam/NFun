namespace NFun.SyntaxTests.TypeNarrowing;
using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class AndNarrowOutsideIfTest {
    [Test]
    public void BoolEquation_NarrowInAnd() {
        var r = "y:int? = 15\r x = y != none and y > 12"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(true, r.Get("x"));
    }
    [Test]
    public void BoolEquation_NarrowInAnd_None() {
        var r = "y:int? = none\r x = y != none and y > 12"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(false, r.Get("x"));
    }
    [Test]
    public void BoolEquation_NarrowInAnd_FailsCheck() {
        var r = "y:int? = 5\r x = y != none and y > 12"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(false, r.Get("x"));
    }
}
