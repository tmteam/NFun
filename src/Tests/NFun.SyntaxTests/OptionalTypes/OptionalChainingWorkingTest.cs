namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalChainingWorkingTest {
    [Test]
    public void SafeFieldAccess_Text_HasValue() =>
        "x = if(true) {name = 'Alice'} else none\r y = x?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Alice");

    [Test]
    public void SafeFieldAccess_Text_None() {
        var result = "x = if(false) {name = 'Alice'} else none\r y = x?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void SafeFieldAccess_TextWithCoalesce_HasValue() =>
        "x = if(true) {name = 'Alice'} else none\r y = x?.name ?? 'default'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void SafeFieldAccess_TextWithCoalesce_None() =>
        "x = if(false) {name = 'Alice'} else none\r y = x?.name ?? 'default'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "default");


    [Test]
    public void SafeFieldAccess_IntWithCoalesce_HasValue() =>
        "x = if(true) {age = 25} else none\r y:int = x?.age ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 25);


    [Test]
    public void SafeFieldAccess_IntWithCoalesce_None() =>
        "x = if(false) {age = 25} else none\r y:int = x?.age ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void SafeFieldAccess_BoolWithCoalesce_HasValue() =>
        "x = if(true) {flag = true} else none\r y = x?.flag ?? false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", true);


    [Test]
    public void SafeFieldAccess_BoolWithCoalesce_None() =>
        "x = if(false) {flag = true} else none\r y = x?.flag ?? false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", false);


    [TestCase("x:int\r y = x?.name")]
    [TestCase("x:text\r y = x?.count")]
    public void SafeFieldAccess_OnNonStruct_Fails(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [Test]
    public void SafeFieldAccess_OnNonOptionalStruct_Fails() =>
        "x = {name = 'hi'}\r y = x?.name"
            .AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    [Test]
    public void SafeFieldAccess_OptionalIntField_HasValue() {
        var result = "x:int? = 5\r s = if(true) {v = x} else none\r y = s?.v"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(5, result.Get("y"));
    }

    [Test]
    public void SafeFieldAccess_OptionalIntField_None() {
        var result = "x:int? = 5\r s = if(false) {v = x} else none\r y = s?.v"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void SafeFieldAccess_OptionalArrayField_HasValue() {
        var result = "x:int[]? = [1,2]\r s = if(true) {arr = x} else none\r y = s?.arr"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNotNull(result.Get("y"));
    }

    [Test]
    public void SafeFieldAccess_OptionalArrayField_None() {
        var result = "x:int[]? = [1,2]\r s = if(false) {arr = x} else none\r y = s?.arr"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void SafeFieldAccess_ChainedTwoLevels_HasValue() =>
        "x = if(true) {a = {b = 42}} else none\r y = x?.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);

    [Test]
    public void SafeFieldAccess_ChainedTwoLevels_None() {
        var result = "x = if(false) {a = {b = 42}} else none\r y = x?.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void ChainedCoalesce_WithSafeFieldAccess() =>
        "x = if(true) {a=1, b=2} else none\r y:int = x?.a ?? x?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 1);

    [Test]
    public void ChainedCoalesce_WithSafeFieldAccess_None() =>
        "x = if(false) {a=1, b=2} else none\r y:int = x?.a ?? x?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);
}
