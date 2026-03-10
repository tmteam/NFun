using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
[Ignore("Struct field type specification ({name:type}) not yet supported")]
public class OptionalStructFieldSpecTest {


    // ═══════════════════════════════════════════════════════════════
    // Step 2: ?? operator (~120 tests)
    // ═══════════════════════════════════════════════════════════════


    // --- ?? with struct field ---

    [Test]
    public void CoalesceOperator_StructField_HasValue() =>
        "s = {n:int? = 42}\r y = s.n ?? 0".AssertResultHas("y", 42);


    [Test]
    public void CoalesceOperator_StructField_None() =>
        "s = {n:int? = none}\r y = s.n ?? 0".AssertResultHas("y", 0);


    [Test]
    public void CoalesceOperator_StructRealField_None() =>
        "s = {r:real? = none}\r y = s.r ?? 1.0".AssertResultHas("y", 1.0);


    [Test]
    public void CoalesceOperator_StructTextField_None() =>
        "s = {t:text? = none}\r y = s.t ?? 'fallback'".AssertResultHas("y", "fallback");


    [Test]
    public void CoalesceOperator_StructTextField_HasValue() =>
        "s = {t:text? = 'hi'}\r y = s.t ?? 'fallback'".AssertResultHas("y", "hi");


    // ═══════════════════════════════════════════════════════════════
    // Step 9: Arrays and structs with optional (~80 tests)
    // ═══════════════════════════════════════════════════════════════


    // --- Struct with optional fields ---

    [Test]
    public void StructWithOptionalField_HasValue() =>
        "y = {a:int? = 42; b = 'hi'}".AssertResultHas("y",
            new { a = 42, b = "hi" });


    [Test]
    public void StructWithOptionalField_NoneValue() =>
        Assert.DoesNotThrow(() => "y = {a:int? = none; b = 'hi'}".Build());


    [Test]
    public void StructWithOptionalField_Access() =>
        Assert.DoesNotThrow(() => "s = {num:int? = 42}\r y = s.num".Build());


    [Test]
    public void StructWithOptionalField_AccessIsOptional() =>
        Assert.DoesNotThrow(() => "s = {num:int? = 42}\r y:int? = s.num".Build());


    // --- Optional struct ---

    [Test]
    public void OptionalStruct_WithValue() =>
        Assert.DoesNotThrow(() => "x:{a:int}? = {a = 1}".Build());


    [Test]
    public void OptionalStruct_WithNone() {
        var result = "x:{a:int}? = none\r y = x".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- Nested optional in struct ---

    [Test]
    public void NestedOptionalStruct_HasValue() =>
        Assert.DoesNotThrow(() =>
            "y = {inner:{num:int?}? = {num = 42}}".Build());


    // --- Struct with multiple optional fields ---

    [Test]
    public void StructMultipleOptionalFields_AllNone() =>
        Assert.DoesNotThrow(() =>
            "y = {a:int? = none; b:text? = none; c:bool? = none}".Build());


    [Test]
    public void StructMultipleOptionalFields_SomeNone() =>
        Assert.DoesNotThrow(() =>
            "y = {a:int? = 42; b:text? = none; c:bool? = true}".Build());


    [Test]
    public void OptionalStructOptionalField_InnerNone() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 0".AssertResultHas("y", 0);


    // --- Array of structs with optional fields ---

    [Test]
    public void ArrayOfStructsWithOptionalField() =>
        Assert.DoesNotThrow(() =>
            "y = [{n:int? = 1}, {n:int? = none}, {n:int? = 3}]".Build());


    // ═══════════════════════════════════════════════════════════════
    // Step 11: Integration with existing features (~50 tests)
    // ═══════════════════════════════════════════════════════════════


    [Test]
    public void Stress_OptionalStructFieldCoalesce() =>
        "s:{num:int?} = {num = none}\r y = s.num ?? 99".AssertResultHas("y", 99);


    [Test]
    public void Stress_OptionalStructFieldCoalesce_HasValue() =>
        "s:{num:int?} = {num = 42}\r y = s.num ?? 99".AssertResultHas("y", 42);
}
