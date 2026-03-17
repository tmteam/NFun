namespace NFun.SyntaxTests.OptionalTypes;

using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalBugsTest {

    [Test]
    public void OptionalStructArray_Index_ShouldWork() {
        var result = "x = [{a=1}, none]\r y = x[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNotNull(result.Get("y"));
    }

    [Test]
    public void OptionalStructArray_Map_ShouldWork() {
        "x = [{a=1}, none, {a=3}]\r y = x.map(rule it?.a ?? 0)"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
    }

    [Test]
    public void FoldOnOptionalArray_WithArithmetic_GivesTypeError() {
        Assert.Throws<FunnyParseException>(
            () => "y = [1,none,3].fold(rule it1 + (it2 ?? 0))"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test]
    public void FoldOnOptionalArray_WithCoalesce_Works() {
        "y = [1,none,3].map(rule it ?? 0).fold(rule it1 + it2)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 4);
    }

    [Test]
    public void OptionalIntArray_NoneDisplaysAsNull() {
        var result = "x:int?[] = [1, none, 3]\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var arr = (int?[])result.Get("y");
        Assert.AreEqual(1, arr[0]);
        Assert.IsNull(arr[1]);
        Assert.AreEqual(3, arr[2]);
    }

    [Test]
    public void OptionalBoolArray_NoneDisplaysAsNull() {
        var result = "x:bool?[] = [true, none, false]\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var arr = (bool?[])result.Get("y");
        Assert.AreEqual(true, arr[0]);
        Assert.IsNull(arr[1]);
        Assert.AreEqual(false, arr[2]);
    }

    [Test]
    public void ChainedCoalesce_WithSafeAccess_HasValue() =>
        "x:{a:int, b:int}? = {a=1, b=2}\r y = x?.a ?? x?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas(("y", 1));

    [Test]
    public void ChainedCoalesce_WithSafeAccess_None() =>
        "x:{a:int, b:int}? = none\r y = x?.a ?? x?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);

    [Test]
    public void StructInIfElse_NestedStruct_PreservesFields() {
        var result = "z1 = {b=1}\r x = if(true) {a = z1} else none\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNotNull(result.Get("y"));
    }

    [Test]
    public void StructInIfElse_NestedStruct_NoneCase() {
        var result = "z1 = {b=1}\r x = if(false) {a = z1} else none\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }
}
