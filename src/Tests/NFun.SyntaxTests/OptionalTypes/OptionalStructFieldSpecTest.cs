namespace NFun.SyntaxTests.OptionalTypes;

using TestTools;
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

    // ───────────────────────────────────────────────────────────────
    // Review N1 — `??` over an always-none field must type NON-optional so the
    //   result participates in arithmetic. The Pull Apply(CS, None) cell used to
    //   set IsOptional on the negative-skolem U of `??` unconditionally; TIC then
    //   typed the coalesce Int32? and rejected the addition with FU761 (the
    //   builder-level Optional-strip ran too late to help and is now removed).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void CoalesceOverAlwaysNoneField_UsableInArithmetic() {
        "type t = {x: int?}; a = if(true) t{x=none} else none; out = (a?.x ?? -1) + 1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 0);
    }

    // ───────────────────────────────────────────────────────────────
    // MR2Bug1 — 2D array literal of struct with optional field, mixed
    //   asymmetric none/value across sub-arrays, FAILS when the outer
    //   variable carries the matching type annotation. Without the
    //   annotation the same expression infers exactly the declared type.
    //
    //   Works:  out = [[{v=none}], [{v=2}]]              → {v:Int32?}[][]
    //   Works:  out:int?[][] = [[none], [2]]             → Int32?[][]
    //   Fails:  out:{v:int?}[][] = [[{v=none}], [{v=2}]] → FU761 "Seems like expression `2` cannot be used here"
    //
    //   Symptom is TIC failing to push the annotated `int?` element
    //   constraint through the 2-level array nesting onto a struct field
    //   when one sub-array is none-only and another is value-only.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR2Bug1_AnnotatedNestedOptionalStructArray_ParseError() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("out:{v:int?}[][] = [[{v=none}], [{v=2}]]"));
    }

    [Test]
    public void MR2Bug1_AnnotatedNestedOptionalStructArray_Variant3D() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("out:{v:int?}[][][] = [[[{v=none}]], [[{v=2}]]]"));
    }

    [Test]
    public void MR2Bug1_AnnotatedNestedOptionalStructArray_MultipleFields() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("out:{v:int?,w:int?}[][] = [[{v=none,w=1}], [{v=2,w=none}]]"));
    }
}
