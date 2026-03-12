using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class OptionalStructFieldSpecTest {


    // ═══════════════════════════════════════════════════════════════
    // Step 2: ?? operator (~120 tests)
    // ═══════════════════════════════════════════════════════════════


    // --- ?? with struct field ---

    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void CoalesceOperator_StructField_HasValue() =>
        "s = {n:int? = 42}\r y = s.n ?? 0".AssertResultHas("y", 42);


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void CoalesceOperator_StructField_None() =>
        "s = {n:int? = none}\r y = s.n ?? 0".AssertResultHas("y", 0);


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void CoalesceOperator_StructRealField_None() =>
        "s = {r:real? = none}\r y = s.r ?? 1.0".AssertResultHas("y", 1.0);


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void CoalesceOperator_StructTextField_None() =>
        "s = {t:text? = none}\r y = s.t ?? 'fallback'".AssertResultHas("y", "fallback");


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void CoalesceOperator_StructTextField_HasValue() =>
        "s = {t:text? = 'hi'}\r y = s.t ?? 'fallback'".AssertResultHas("y", "hi");


    // ═══════════════════════════════════════════════════════════════
    // Step 9: Arrays and structs with optional (~80 tests)
    // ═══════════════════════════════════════════════════════════════


    // --- Struct with optional fields ---

    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void StructWithOptionalField_HasValue() =>
        "y = {a:int? = 42; b = 'hi'}".AssertResultHas("y",
            new { a = 42, b = "hi" });


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void StructWithOptionalField_NoneValue() =>
        Assert.DoesNotThrow(() => "y = {a:int? = none; b = 'hi'}".Build());


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void StructWithOptionalField_Access() =>
        Assert.DoesNotThrow(() => "s = {val:int? = 42}\r y = s.val".Build());


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void StructWithOptionalField_AccessIsOptional() =>
        Assert.DoesNotThrow(() => "s = {val:int? = 42}\r y:int? = s.val".Build());


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
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void NestedOptionalStruct_HasValue() =>
        Assert.DoesNotThrow(() =>
            "y = {inner:{val:int?}? = {val = 42}}".Build());


    // --- Struct with multiple optional fields ---

    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void StructMultipleOptionalFields_AllNone() =>
        Assert.DoesNotThrow(() =>
            "y = {a:int? = none; b:text? = none; c:bool? = none}".Build());


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void StructMultipleOptionalFields_SomeNone() =>
        Assert.DoesNotThrow(() =>
            "y = {a:int? = 42; b:text? = none; c:bool? = true}".Build());


    [Test]
    public void OptionalStructOptionalField_InnerNone() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 0".AssertResultHas("y", 0);


    // --- Array of structs with optional fields ---

    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void ArrayOfStructsWithOptionalField() =>
        Assert.DoesNotThrow(() =>
            "y = [{n:int? = 1}, {n:int? = none}, {n:int? = 3}]".Build());


    // ═══════════════════════════════════════════════════════════════
    // Step 11: Integration with existing features (~50 tests)
    // ═══════════════════════════════════════════════════════════════


    [Test]
    public void Stress_OptionalStructFieldCoalesce() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 99".AssertResultHas("y", 99);


    [Test]
    public void Stress_OptionalStructFieldCoalesce_HasValue() =>
        "s:{n:int?} = {n = 42}\r y = s.n ?? 99".AssertResultHas("y", 42);
}
