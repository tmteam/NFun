namespace NFun.SyntaxTests.TypeNarrowing;
using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class QuickCompactTest {
    [Test]
    public void Basic_Compact() {
        var r = "arr:int?[] = [1, none, 3, none, 5]\r y = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[]{1, 3, 5}, r.Get("y"));
    }
    [Test]
    public void Compact_ThenMap() {
        var r = "arr:int?[] = [1, none, 3]\r y = arr.filterNotNull().map(rule it + 1)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new[]{2, 4}, r.Get("y"));
    }
    [Test]
    public void Compact_AllNone() {
        var r = "arr:int?[] = [none, none]\r y = arr.filterNotNull()"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(new int[0], r.Get("y"));
    }
}
