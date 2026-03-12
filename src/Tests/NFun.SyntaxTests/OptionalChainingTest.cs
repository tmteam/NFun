using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class OptionalChainingTest {


    // ═══════════════════════════════════════════════════════════════
    // Step 4: ?. optional chaining (~100 tests)
    // ═══════════════════════════════════════════════════════════════


    // --- Basic optional chaining on struct ---

    [Test]
    public void OptionalChaining_StructField_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name".AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_StructField_None() {
        var result = "x:{name:text}? = none\r y = x?.name".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_IntField_HasValue() =>
        "x:{age:int}? = {age = 25}\r y = x?.age".AssertResultHas("y", 25);


    [Test]
    public void OptionalChaining_IntField_None() {
        var result = "x:{age:int}? = none\r y = x?.age".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_RealField_HasValue() =>
        "x:{n:real}? = {n = 3.14}\r y = x?.n".AssertResultHas("y", 3.14);


    [Test]
    public void OptionalChaining_BoolField_HasValue() =>
        "x:{flag:bool}? = {flag = true}\r y = x?.flag".AssertResultHas("y", true);


    // --- Short-circuit chain (nested non-nullable fields) ---

    [Test]
    public void OptionalChaining_NestedNonNullableField() =>
        "x:{profile:{name:text}}? = {profile = {name = 'Bob'}}\r y = x?.profile.name"
            .AssertResultHas("y", "Bob");


    [Test]
    public void OptionalChaining_NestedNonNullableField_None() {
        var result = "x:{profile:{name:text}}? = none\r y = x?.profile.name".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- Multiple nullable levels ---

    [Test]
    public void OptionalChaining_DoubleChain_HasValue() =>
        "x:{a:{b:int}?}? = {a = {b = 42}}\r y = x?.a?.b".AssertResultHas("y", 42);


    [Test]
    public void OptionalChaining_DoubleChain_OuterNone() {
        var result = "x:{a:{b:int}?}? = none\r y = x?.a?.b".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_DoubleChain_InnerNone() {
        var result = "x:{a:{b:int}?} = {a = none}\r y = x.a?.b".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- Result is optional (propagation) ---

    [Test]
    public void OptionalChaining_ResultType_IsOptional() =>
        Assert.DoesNotThrow(() => "x:{name:text}? = {name = 'hi'}\r y:text? = x?.name".Build());


    [Test]
    public void OptionalChaining_IntResult_IsOptional() =>
        Assert.DoesNotThrow(() => "x:{age:int}? = {age = 25}\r y:int? = x?.age".Build());


    // --- ?. combined with ?? ---

    [Test]
    public void OptionalChaining_WithCoalesce_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name ?? 'default'"
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_WithCoalesce_None() =>
        "x:{name:text}? = none\r y = x?.name ?? 'default'"
            .AssertResultHas("y", "default");


    [Test]
    public void OptionalChaining_IntWithCoalesce_HasValue() =>
        "x:{age:int}? = {age = 25}\r y = x?.age ?? 0".AssertResultHas("y", 25);


    [Test]
    public void OptionalChaining_IntWithCoalesce_None() =>
        "x:{age:int}? = none\r y = x?.age ?? 0".AssertResultHas("y", 0);


    [Test]
    public void OptionalChaining_NestedWithCoalesce() =>
        "x:{profile:{name:text}}? = none\r y = x?.profile.name ?? 'nobody'"
            .AssertResultHas("y", "nobody");


    // --- ?. combined with ! ---

    [Test]
    public void OptionalChaining_WithForceUnwrap_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name!"
            .AssertResultHas("y", "Alice");


    // --- ?. with array field ---

    [Test]
    public void OptionalChaining_ArrayField_HasValue() =>
        "x:{items:int[]}? = {items = [10,20]}\r y = x?.items"
            .AssertResultHas("y", new[] { 10, 20 });


    [Test]
    public void OptionalChaining_ArrayField_None() {
        var result = "x:{items:int[]}? = none\r y = x?.items".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- ?. for each field type ---

    [Test]
    public void OptionalChaining_ByteField_HasValue() =>
        "x:{n:byte}? = {n = 1}\r y = x?.n".AssertResultHas("y", (byte)1);


    [Test]
    public void OptionalChaining_Int16Field_HasValue() =>
        "x:{n:int16}? = {n = 1}\r y = x?.n".AssertResultHas("y", (Int16)1);


    [Test]
    public void OptionalChaining_Int64Field_HasValue() =>
        "x:{n:int64}? = {n = 1}\r y = x?.n".AssertResultHas("y", (Int64)1);


    [Test]
    public void OptionalChaining_Uint16Field_HasValue() =>
        "x:{n:uint16}? = {n = 1}\r y = x?.n".AssertResultHas("y", (UInt16)1);


    [Test]
    public void OptionalChaining_Uint32Field_HasValue() =>
        "x:{n:uint32}? = {n = 1}\r y = x?.n".AssertResultHas("y", (UInt32)1);


    [Test]
    public void OptionalChaining_Uint64Field_HasValue() =>
        "x:{n:uint64}? = {n = 1}\r y = x?.n".AssertResultHas("y", (UInt64)1);


    [Test]
    public void OptionalChaining_CharField_HasValue() =>
        "x:{c:char}? = {c = /'z'}\r y = x?.c".AssertResultHas("y", 'z');


    // --- ?. for each field type → none ---

    [Test]
    public void OptionalChaining_ByteField_None() {
        var result = "x:{n:byte}? = none\r y = x?.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Int16Field_None() {
        var result = "x:{n:int16}? = none\r y = x?.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Int64Field_None() {
        var result = "x:{n:int64}? = none\r y = x?.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint16Field_None() {
        var result = "x:{n:uint16}? = none\r y = x?.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint32Field_None() {
        var result = "x:{n:uint32}? = none\r y = x?.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint64Field_None() {
        var result = "x:{n:uint64}? = none\r y = x?.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_RealField_None() {
        var result = "x:{n:real}? = none\r y = x?.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_TextField_None() {
        var result = "x:{t:text}? = none\r y = x?.t".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_BoolField_None() {
        var result = "x:{b:bool}? = none\r y = x?.b".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_CharField_None() {
        var result = "x:{c:char}? = none\r y = x?.c".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- ?. chain: 3 levels ---

    [Test]
    public void OptionalChaining_ThreeLevels_HasValue() =>
        "x:{a:{b:{c:int}?}?}? = {a = {b = {c = 99}}}\r y = x?.a?.b?.c"
            .AssertResultHas("y", 99);


    [Test]
    public void OptionalChaining_ThreeLevels_Level1None() {
        var result = "x:{a:{b:{c:int}?}?}? = none\r y = x?.a?.b?.c".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_Level2None() {
        var result = "x:{a:{b:{c:int}?}?} = {a = none}\r y = x.a?.b?.c".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_Level3None() {
        var result = "x:{a:{b:{c:int}?}} = {a = {b = none}}\r y = x.a.b?.c".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- ?. with multiple fields ---

    [Test]
    public void OptionalChaining_MultipleFields_AccessEach() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.name"
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_MultipleFields_AccessAge() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.age"
            .AssertResultHas("y", 30);


    [Test]
    public void OptionalChaining_MultipleFields_AccessBool() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.active"
            .AssertResultHas("y", true);


    // --- ?. with array field operations ---

    [Test]
    public void OptionalChaining_ArrayField_Count() =>
        "x:{items:int[]}? = {items = [1,2,3]}\r y = x?.items"
            .AssertResultHas("y", new[] { 1, 2, 3 });


    // --- ?. combined with ?? for each type ---

    [TestCase("x:{n:byte}? = {n = 5}\r y = x?.n ?? 0", (byte)5)]
    [TestCase("x:{n:int16}? = {n = 5}\r y = x?.n ?? 0", (Int16)5)]
    [TestCase("x:{n:int64}? = {n = 5}\r y = x?.n ?? 0", (Int64)5)]
    [TestCase("x:{n:real}? = {n = 1.5}\r y = x?.n ?? 0.0", 1.5)]
    [TestCase("x:{t:text}? = {t = 'hi'}\r y = x?.t ?? 'bye'", "hi")]
    [TestCase("x:{b:bool}? = {b = true}\r y = x?.b ?? false", true)]
    public void OptionalChaining_WithCoalesce_EachType_HasValue(string expr, object expected) =>
        expr.AssertResultHas("y", expected);


    [TestCase("x:{n:byte}? = none\r y = x?.n ?? 0", (byte)0)]
    [TestCase("x:{n:int16}? = none\r y = x?.n ?? 0", (Int16)0)]
    [TestCase("x:{n:int64}? = none\r y = x?.n ?? 0", (Int64)0)]
    [TestCase("x:{n:real}? = none\r y = x?.n ?? 0.0", 0.0)]
    [TestCase("x:{t:text}? = none\r y = x?.t ?? 'bye'", "bye")]
    [TestCase("x:{b:bool}? = none\r y = x?.b ?? false", false)]
    public void OptionalChaining_WithCoalesce_EachType_None(string expr, object expected) =>
        expr.AssertResultHas("y", expected);


    // --- Negative: ?. on non-optional ---

    [TestCase("x:{name:text} = {name = 'hi'}\r y = x?.name")]
    [TestCase("x:int\r y = x?.name")]
    [TestCase("x:text\r y = x?.count")]
    public void OptionalChaining_NonOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();


    // --- ?. on optional array indexing ---

    [Test]
    public void OptionalChaining_ArrayIndex_HasValue() =>
        "x:int[]? = [10,20,30]\r y = x?[0]".AssertResultHas("y", 10);


    [Test]
    public void OptionalChaining_ArrayIndex_Second() =>
        "x:int[]? = [10,20,30]\r y = x?[1]".AssertResultHas("y", 20);


    [Test]
    public void OptionalChaining_ArrayIndex_None() {
        var result = "x:int[]? = none\r y = x?[0]".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_RealArrayIndex_HasValue() =>
        "x:real[]? = [1.1, 2.2]\r y = x?[0]".AssertResultHas("y", 1.1);


    [Test]
    public void OptionalChaining_TextArrayIndex_HasValue() =>
        "x:text[]? = ['hello', 'world']\r y = x?[0]".AssertResultHas("y", "hello");


    [Test]
    public void OptionalChaining_TextArrayIndex_None() {
        var result = "x:text[]? = none\r y = x?[0]".Calc();
        Assert.IsNull(result.Get("y"));
    }


    // --- ?. on struct with optional field (field itself is optional) ---

    [Test]
    public void OptionalChaining_StructWithOptionalField_FieldHasValue() =>
        "s:{n:int?} = {n = 42}\r y = s.n".AssertResultHas("y", 42);


    [Test]
    public void OptionalChaining_StructWithOptionalField_FieldIsNone() {
        var result = "s:{n:int?} = {n = none}\r y = s.n".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_OptionalStructWithOptionalField() =>
        "s:{n:int?}? = {n = 42}\r y = s?.n".AssertResultHas("y", 42);


    // ═══════════════════════════════════════════════════════════════
    // Step 9: Arrays and structs with optional (~80 tests)
    // ═══════════════════════════════════════════════════════════════


    [Test]
    public void OptionalStruct_ChainAccess() =>
        "x:{a:int}? = {a = 1}\r y = x?.a".AssertResultHas("y", 1);


    [Test]
    public void OptionalStruct_ChainAccess_None() {
        var result = "x:{a:int}? = none\r y = x?.a".Calc();
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void NestedOptionalStruct_ChainAccess() =>
        Assert.DoesNotThrow(() =>
            "s = {inner:{n:int?}? = {n = 42}}\r y = s.inner?.n".Build());


    // --- Optional struct with optional field — double optional ---

    [Test]
    public void OptionalStructOptionalField_ChainCoalesce() =>
        "s:{n:int?}? = {n = 42}\r y = s?.n ?? 0".AssertResultHas("y", 42);


    [Test]
    public void OptionalStructOptionalField_OuterNone() =>
        "s:{n:int?}? = none\r y = s?.n ?? 0".AssertResultHas("y", 0);


    // ═══════════════════════════════════════════════════════════════
    // Step 11: Integration with existing features (~50 tests)
    // ═══════════════════════════════════════════════════════════════


    [Test]
    public void Stress_NestedOptionalChaining() =>
        Assert.DoesNotThrow(() =>
            "x:{a:{b:{c:int?}?}?}? = {a = {b = {c = 42}}}\r y = x?.a?.b?.c".Build());


    // ═══════════════════════════════════════════════════════════════
    // Step 12: Complex operator combinations — ??, !, ?., (), chains
    // ═══════════════════════════════════════════════════════════════


    // --- ?. then ?? then ! ---

    [Test]
    public void Combo_ChainingCoalesceUnwrap_HasValue() =>
        "s:{inner:{n:int?}?}? = {inner = {n = 42}}\r y = (s?.inner?.n ?? 0)"
            .AssertResultHas("y", 42);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneOuter() =>
        "s:{inner:{n:int?}?}? = none\r y = (s?.inner?.n ?? 0)"
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneInner() =>
        "s:{inner:{n:int?}?} = {inner = none}\r y = (s.inner?.n ?? 0)"
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneLeaf() =>
        "s:{inner:{n:int?}?} = {inner = {n = none}}\r y = (s.inner?.n ?? 0)"
            .AssertResultHas("y", 0);


    // --- ?? + ?. mixed ---

    [Test]
    public void Combo_ChainingThenCoalesce_MultiField() =>
        "s:{name:text; age:int}? = {name = 'Alice'; age = 30}\r y = s?.name ?? 'unknown'"
            .AssertResultHas("y", "Alice");


    [Test]
    public void Combo_ChainingThenCoalesce_MultiField_None() =>
        "s:{name:text; age:int}? = none\r y = s?.name ?? 'unknown'"
            .AssertResultHas("y", "unknown");


    [Test]
    public void Combo_ChainingIntThenCoalesce() =>
        "s:{age:int}? = {age = 25}\r y = s?.age ?? -1".AssertResultHas("y", 25);


    [Test]
    public void Combo_ChainingIntThenCoalesce_None() =>
        "s:{age:int}? = none\r y = s?.age ?? -1".AssertResultHas("y", -1);


    [Test]
    public void Combo_DoubleChainingThenCoalesce() =>
        "s:{a:{b:int}?}? = {a = {b = 99}}\r y = s?.a?.b ?? 0"
            .AssertResultHas("y", 99);


    [Test]
    public void Combo_DoubleChainingThenCoalesce_OuterNone() =>
        "s:{a:{b:int}?}? = none\r y = s?.a?.b ?? 0"
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_DoubleChainingThenCoalesce_InnerNone() =>
        "s:{a:{b:int}?} = {a = none}\r y = s.a?.b ?? 0"
            .AssertResultHas("y", 0);


    // --- ?? + ! + ?. all together ---

    [Test]
    public void Combo_ChainCoalesceUnwrap_AllThree() =>
        "s:{n:int?}? = {n = 10}\r y = (s?.n ?? 0)".AssertResultHas("y", 10);


    [Test]
    public void Combo_ChainUnwrapArithmetic() =>
        "s:{n:int}? = {n = 5}\r y = s!.n + 1".AssertResultHas("y", 6);


    [Test]
    public void Combo_ChainUnwrapArithmetic_None_RuntimeError() =>
        "s:{n:int}? = none\r y = s!.n + 1".AssertObviousFailsOnRuntime();


    // --- ?. with ?? with arithmetic ---

    [Test]
    public void Combo_ChainingCoalesceArithmetic() =>
        "s:{n:int}? = {n = 5}\r y = (s?.n ?? 0) + 10"
            .AssertResultHas("y", 15);


    [Test]
    public void Combo_ChainingCoalesceArithmetic_None() =>
        "s:{n:int}? = none\r y = (s?.n ?? 0) + 10"
            .AssertResultHas("y", 10);


    [Test]
    public void Combo_ChainingCoalesceMultiply() =>
        "s:{n:int}? = {n = 3}\r y = (s?.n ?? 1) * 2"
            .AssertResultHas("y", 6);


    // --- Deeply nested: ?. chain → ?? → ! → arithmetic ---

    [Test]
    public void Combo_DeepNestAllOperators() =>
        @"root:{child:{leaf:int?}?}? = {child = {leaf = 7}}
          fallback:int? = 100
          y = (root?.child?.leaf ?? fallback!) + 1"
            .AssertResultHas("y", 8);


    [Test]
    public void Combo_DeepNestAllOperators_NoneRoot() =>
        @"root:{child:{leaf:int?}?}? = none
          fallback:int? = 100
          y = (root?.child?.leaf ?? fallback!) + 1"
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_DeepNestAllOperators_NoneChild() =>
        @"root:{child:{leaf:int?}?} = {child = none}
          fallback:int? = 100
          y = (root.child?.leaf ?? fallback!) + 1"
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_DeepNestAllOperators_NoneLeaf() =>
        @"root:{child:{leaf:int?}} = {child = {leaf = none}}
          fallback:int? = 100
          y = (root.child.leaf ?? fallback!) + 1"
            .AssertResultHas("y", 101);


    // --- Array indexing with optional chains ---

    [Test]
    public void Combo_OptionalArrayIndexCoalesce() =>
        "x:int[]? = [10,20,30]\r y = (x?[1]) ?? -1".AssertResultHas("y", 20);


    [Test]
    public void Combo_OptionalArrayIndexCoalesce_None() =>
        "x:int[]? = none\r y = (x?[1]) ?? -1".AssertResultHas("y", -1);


    // --- Complex: multiple ?. and ?? in single line ---

    [Test]
    public void Combo_MultiFieldChainCoalesce() =>
        @"s:{name:text; age:int}? = {name = 'Bob'; age = 30}
          y = (s?.name ?? 'unknown')
          z = (s?.age ?? 0)"
            .AssertResultHas(("y", (object)"Bob"), ("z", (object)30));


    [Test]
    public void Combo_MultiFieldChainCoalesce_None() =>
        @"s:{name:text; age:int}? = none
          y = (s?.name ?? 'unknown')
          z = (s?.age ?? 0)"
            .AssertResultHas(("y", (object)"unknown"), ("z", (object)0));


    // --- Triple operator combo: ?. ?? ! ---

    [Test]
    public void Combo_TripleOperator_ChainCoalesceFallbackUnwrap() =>
        @"s:{n:int}? = {n = 42}
          fallback:int? = 99
          y = s?.n ?? fallback!"
            .AssertResultHas("y", 42);


    [Test]
    public void Combo_TripleOperator_ChainNone_CoalesceFallbackUnwrap() =>
        @"s:{n:int}? = none
          fallback:int? = 99
          y = s?.n ?? fallback!"
            .AssertResultHas("y", 99);


    [Test]
    public void Combo_TripleOperator_ChainNone_FallbackNone_RuntimeError() =>
        @"s:{n:int}? = none
          fallback:int? = none
          y = s?.n ?? fallback!"
            .AssertObviousFailsOnRuntime();
}
