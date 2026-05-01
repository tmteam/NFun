namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalStructFieldSpecTest {

    [Test]
    public void OptionalStruct_WithValue() =>
        Assert.DoesNotThrow(() =>
            "x:{a:int}? = {a = 1}"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));


    [Test]
    public void OptionalStruct_WithNone() {
        var result = "x:{a:int}? = none\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalStructOptionalField_InnerNone() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);

    [Test]
    public void Stress_OptionalStructFieldCoalesce() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 99);

    [Test]
    public void Stress_OptionalStructFieldCoalesce_HasValue() =>
        "s:{n:int?} = {n = 42}\r y = s.n ?? 99"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);

    [Test]
    public void StructInIfElse_NestedStruct_PreservesFields() {
        var result = "z1 = {b=1}\r x = if(true) {a = z1} else none\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNotNull(result.Get("y"));
    }

    [Test]
    public void StructInIfElse_NestedStruct_NoneCase() {
        var result = "z1 = {b=1}\r x = if(false) {a = z1} else none\r y = x"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    // ═══════════════════════════════════════════════════════════════
    // if-else struct with swapped none fields
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IfElseStruct_SwappedNoneFields_Addition() {
        var r = "x = if(true) {a=1, b=none} else {a=none, b=2}\r y = x.a ?? 0\r z = x.b ?? 0\r out = y + z"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void IfElseStruct_SwappedNoneFields_FalseBranch() {
        var r = "x = if(false) {a=1, b=none} else {a=none, b=2}\r y = x.a ?? 0\r z = x.b ?? 0\r out = y + z"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(2, r.Get("out"));
    }

    [Test]
    public void IfElseStruct_SwappedNoneFields_ExplicitIntType() {
        var r = "x:{a:int?, b:int?} = if(true) {a=1, b=none} else {a=none, b=2}\r out = (x.a ?? 0) + (x.b ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(1, r.Get("out"));
    }

    [Test]
    public void IfElseStruct_BothFieldsPresent_NoNone() {
        var r = "x = if(true) {a=1, b=2} else {a=3, b=4}\r out = x.a + x.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(3, r.Get("out"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Optional struct with none optional field
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IfElseOptionalStructNoneField() {
        "type t = {x: int?}; a = if(true) t{x=none} else none; out = a?.x ?? -1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", -1);
    }
}
