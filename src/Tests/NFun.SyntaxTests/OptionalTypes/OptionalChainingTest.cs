namespace NFun.SyntaxTests.OptionalTypes;

using System;
using TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalChainingTest {
    [Test]
    public void OptionalChaining_StructField_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_StructField_None() {
        var result = "x:{name:text}? = none\r y = x?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_IntField_HasValue() =>
        "x:{age:int}? = {age = 25}\r y = x?.age"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 25);


    [Test]
    public void OptionalChaining_IntField_None() {
        var result = "x:{age:int}? = none\r y = x?.age"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_RealField_HasValue() =>
        "x:{n:real}? = {n = 3.14}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 3.14);


    [Test]
    public void OptionalChaining_BoolField_HasValue() =>
        "x:{flag:bool}? = {flag = true}\r y = x?.flag"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", true);


    // --- Short-circuit chain (nested non-nullable fields) ---

    [Test]
    public void OptionalChaining_NestedNonNullableField() =>
        "x:{profile:{name:text}}? = {profile = {name = 'Bob'}}\r y = x?.profile.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "Bob");


    [Test]
    public void OptionalChaining_NestedNonNullableField_None() {
        var result = "x:{profile:{name:text}}? = none\r y = x?.profile.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalChaining_DoubleChain_HasValue() =>
        "x:{a:{b:int}?}? = {a = {b = 42}}\r y = x?.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalChaining_DoubleChain_OuterNone() {
        var result = "x:{a:{b:int}?}? = none\r y = x?.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_DoubleChain_InnerNone() {
        var result = "x:{a:{b:int}?} = {a = none}\r y = x.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalChaining_ResultType_IsOptional() =>
        Assert.DoesNotThrow(() => "x:{name:text}? = {name = 'hi'}\r y:text? = x?.name".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));


    [Test]
    public void OptionalChaining_IntResult_IsOptional() =>
        Assert.DoesNotThrow(() => "x:{age:int}? = {age = 25}\r y:int? = x?.age".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));

    [Test]
    public void OptionalChaining_WithCoalesce_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name ?? 'default'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_WithCoalesce_None() =>
        "x:{name:text}? = none\r y = x?.name ?? 'default'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "default");


    [Test]
    public void OptionalChaining_IntWithCoalesce_HasValue() =>
        "x:{age:int}? = {age = 25}\r y = x?.age ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 25);


    [Test]
    public void OptionalChaining_IntWithCoalesce_None() =>
        "x:{age:int}? = none\r y = x?.age ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);


    [Test]
    public void OptionalChaining_NestedWithCoalesce() =>
        "x:{profile:{name:text}}? = none\r y = x?.profile.name ?? 'nobody'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "nobody");


    [Test]
    public void OptionalChaining_WithForceUnwrap_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_ArrayField_HasValue() =>
        "x:{items:int[]}? = {items = [10,20]}\r y = x?.items"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", new[] { 10, 20 });

    [Test]
    public void OptionalChaining_ArrayField_None() {
        var result = "x:{items:int[]}? = none\r y = x?.items"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ByteField_HasValue() =>
        "x:{n:byte}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", (byte)1);


    [Test]
    public void OptionalChaining_Int16Field_HasValue() =>
        "x:{n:int16}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", (Int16)1);


    [Test]
    public void OptionalChaining_Int64Field_HasValue() =>
        "x:{n:int64}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", (Int64)1);


    [Test]
    public void OptionalChaining_Uint16Field_HasValue() =>
        "x:{n:uint16}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", (UInt16)1);


    [Test]
    public void OptionalChaining_Uint32Field_HasValue() =>
        "x:{n:uint32}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", (UInt32)1);


    [Test]
    public void OptionalChaining_Uint64Field_HasValue() =>
        "x:{n:uint64}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", (UInt64)1);


    [Test]
    public void OptionalChaining_CharField_HasValue() =>
        "x:{c:char}? = {c = /'z'}\r y = x?.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 'z');

    [Test]
    public void OptionalChaining_ByteField_None() {
        var result =
            "x:{n:byte}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Int16Field_None() {
        var result =
            "x:{n:int16}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Int64Field_None() {
        var result =
            "x:{n:int64}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint16Field_None() {
        var result =
            "x:{n:uint16}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint32Field_None() {
        var result =
            "x:{n:uint32}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint64Field_None() {
        var result =
            "x:{n:uint64}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_RealField_None() {
        var result =
            "x:{n:real}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_TextField_None() {
        var result =
            "x:{t:text}? = none\r y = x?.t".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_BoolField_None() {
        var result =
            "x:{b:bool}? = none\r y = x?.b".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_CharField_None() {
        var result =
            "x:{c:char}? = none\r y = x?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_HasValue() =>
        "x:{a:{b:{c:int}?}?}? = {a = {b = {c = 99}}}\r y = x?.a?.b?.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 99);


    [Test]
    public void OptionalChaining_ThreeLevels_Level1None() {
        var result =
            "x:{a:{b:{c:int}?}?}? = none\r y = x?.a?.b?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_Level2None() {
        var result =
            "x:{a:{b:{c:int}?}?} = {a = none}\r y = x.a?.b?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_Level3None() {
        var result =
            "x:{a:{b:{c:int}?}} = {a = {b = none}}\r y = x.a.b?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_MultipleFields_AccessEach() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_MultipleFields_AccessAge() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.age"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 30);


    [Test]
    public void OptionalChaining_MultipleFields_AccessBool() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.active"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", true);


    [Test]
    public void OptionalChaining_ArrayField_Count() =>
        "x:{items:int[]}? = {items = [1,2,3]}\r y = x?.items"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", new[] { 1, 2, 3 });


    [TestCase("x:{n:byte}? = {n = 5}\r y = x?.n ?? 0", (byte)5)]
    [TestCase("x:{n:int16}? = {n = 5}\r y = x?.n ?? 0", (Int16)5)]
    [TestCase("x:{n:int64}? = {n = 5}\r y = x?.n ?? 0", (Int64)5)]
    [TestCase("x:{n:real}? = {n = 1.5}\r y = x?.n ?? 0.0", 1.5)]
    [TestCase("x:{t:text}? = {t = 'hi'}\r y = x?.t ?? 'bye'", "hi")]
    [TestCase("x:{b:bool}? = {b = true}\r y = x?.b ?? false", true)]
    public void OptionalChaining_WithCoalesce_EachType_HasValue(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", expected);


    [TestCase("x:{n:byte}? = none\r y = x?.n ?? 0", (byte)0)]
    [TestCase("x:{n:int16}? = none\r y = x?.n ?? 0", (Int16)0)]
    [TestCase("x:{n:int64}? = none\r y = x?.n ?? 0", (Int64)0)]
    [TestCase("x:{n:real}? = none\r y = x?.n ?? 0.0", 0.0)]
    [TestCase("x:{t:text}? = none\r y = x?.t ?? 'bye'", "bye")]
    [TestCase("x:{b:bool}? = none\r y = x?.b ?? false", false)]
    public void OptionalChaining_WithCoalesce_EachType_None(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", expected);


    // ?. on struct receivers is permissive (treated as `.` field access) — that
    // shape is tested in BugHunt300 (Section_E). ?. on non-struct types like
    // Int/Text is still an error.
    [TestCase("x:int\r y = x?.name")]
    [TestCase("x:text\r y = x?.count")]
    public void OptionalChaining_NonOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Enabled);


    [Test]
    public void OptionalChaining_StructWithOptionalField_FieldHasValue() =>
        "s:{n:int?} = {n = 42}\r y = s.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalChaining_StructWithOptionalField_FieldIsNone() {
        var result =
            "s:{n:int?} = {n = none}\r y = s.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_OptionalStructWithOptionalField() =>
        "s:{n:int?}? = {n = 42}\r y = s?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalStruct_ChainAccess() =>
        "x:{a:int}? = {a = 1}\r y = x?.a"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 1);


    [Test]
    public void OptionalStruct_ChainAccess_None() {
        var result =
            "x:{a:int}? = none\r y = x?.a"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalStructOptionalField_ChainCoalesce() =>
        "s:{n:int?}? = {n = 42}\r y = s?.n ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalStructOptionalField_OuterNone() =>
        "s:{n:int?}? = none\r y = s?.n ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Stress_NestedOptionalChaining() =>
        Assert.DoesNotThrow(() =>
            "x:{a:{b:{c:int?}?}?}? = {a = {b = {c = 42}}}\r y = x?.a?.b?.c".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));


    [Test]
    public void Combo_ChainingCoalesceUnwrap_HasValue() =>
        "s:{inner:{n:int?}?}? = {inner = {n = 42}}\r y = (s?.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneOuter() =>
        "s:{inner:{n:int?}?}? = none\r y = (s?.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneInner() =>
        "s:{inner:{n:int?}?} = {inner = none}\r y = (s.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneLeaf() =>
        "s:{inner:{n:int?}?} = {inner = {n = none}}\r y = (s.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingThenCoalesce_MultiField() =>
        "s:{name:text; age:int}? = {name = 'Alice'; age = 30}\r y = s?.name ?? 'unknown'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void Combo_ChainingThenCoalesce_MultiField_None() =>
        "s:{name:text; age:int}? = none\r y = s?.name ?? 'unknown'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", "unknown");


    [Test]
    public void Combo_ChainingIntThenCoalesce() =>
        "s:{age:int}? = {age = 25}\r y = s?.age ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 25);


    [Test]
    public void Combo_ChainingIntThenCoalesce_None() =>
        "s:{age:int}? = none\r y = s?.age ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", -1);


    [Test]
    public void Combo_DoubleChainingThenCoalesce() =>
        "s:{a:{b:int}?}? = {a = {b = 99}}\r y = s?.a?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 99);


    [Test]
    public void Combo_DoubleChainingThenCoalesce_OuterNone() =>
        "s:{a:{b:int}?}? = none\r y = s?.a?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_DoubleChainingThenCoalesce_InnerNone() =>
        "s:{a:{b:int}?} = {a = none}\r y = s.a?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainCoalesceUnwrap_AllThree() =>
        "s:{n:int?}? = {n = 10}\r y = (s?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 10);


    [Test]
    public void Combo_ChainUnwrapArithmetic() =>
        "s:{n:int}? = {n = 5}\r y = s!.n + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 6);


    [Test]
    public void Combo_ChainUnwrapArithmetic_None_RuntimeError() =>
        "s:{n:int}? = none\r y = s!.n + 1"
            .AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.Enabled);


    [Test]
    public void Combo_ChainingCoalesceArithmetic() =>
        "s:{n:int}? = {n = 5}\r y = (s?.n ?? 0) + 10"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 15);


    [Test]
    public void Combo_ChainingCoalesceArithmetic_None() =>
        "s:{n:int}? = none\r y = (s?.n ?? 0) + 10"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 10);


    [Test]
    public void Combo_ChainingCoalesceMultiply() =>
        "s:{n:int}? = {n = 3}\r y = (s?.n ?? 1) * 2"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 6);


    [Test]
    public void Combo_DeepNestAllOperators() =>
        @"root:{child:{leaf:int?}?}? = {child = {leaf = 7}}
          fallback:int? = 100
          y = (root?.child?.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 8);


    [Test]
    public void Combo_DeepNestAllOperators_NoneRoot() =>
        @"root:{child:{leaf:int?}?}? = none
          fallback:int? = 100
          y = (root?.child?.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_DeepNestAllOperators_NoneChild() =>
        @"root:{child:{leaf:int?}?} = {child = none}
          fallback:int? = 100
          y = (root.child?.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_DeepNestAllOperators_NoneLeaf() =>
        @"root:{child:{leaf:int?}} = {child = {leaf = none}}
          fallback:int? = 100
          y = (root.child.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_MultiFieldChainCoalesce() =>
        @"s:{name:text; age:int}? = {name = 'Bob'; age = 30}
          y = (s?.name ?? 'unknown')
          z = (s?.age ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas(("y", (object)"Bob"), ("z", (object)30));


    [Test]
    public void Combo_MultiFieldChainCoalesce_None() =>
        @"s:{name:text; age:int}? = none
          y = (s?.name ?? 'unknown')
          z = (s?.age ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas(("y", (object)"unknown"), ("z", (object)0));


    [Test]
    public void Combo_TripleOperator_ChainCoalesceFallbackUnwrap() =>
        @"s:{n:int}? = {n = 42}
          fallback:int? = 99
          y = s?.n ?? fallback!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 42);


    [Test]
    public void Combo_TripleOperator_ChainNone_CoalesceFallbackUnwrap() =>
        @"s:{n:int}? = none
          fallback:int? = 99
          y = s?.n ?? fallback!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", 99);


    [Test]
    public void Combo_TripleOperator_ChainNone_FallbackNone_RuntimeError() =>
        @"s:{n:int}? = none
          fallback:int? = none
          y = s?.n ?? fallback!"
            .AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.Enabled);

    // ═══════════════════════════════════════════════════════════════
    // Double anonymous optional safe access
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DoubleAnonymousOptionalSafeAccess() {
        "inner = if(true) {b = 42} else none; x = if(true) {a = inner} else none; out = x?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 42);
    }

    // ═══════════════════════════════════════════════════════════════
    // Safe call chain on none — .method() after ?.method() is also safe
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeCallChainOnNone_RuntimeOk() {
        "arr:int[]? = none; out = arr?.sort().reverse() ?? []"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", new int[0]);
    }

    // ═══════════════════════════════════════════════════════════════
    // Deep safe access chain with method
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DeepSafeAccessChainMethod() {
        "x = if(true) {a={b='hello'}} else none; out = x?.a.b.reverse() ?? ''"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", "olleh");
    }

    // ═══════════════════════════════════════════════════════════════
    // Safe access with HOF preserves struct fields
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void SafeAccessHOF_PreservesStructFields() {
        var r = "data:{users:{name:text, score:int}[]}? = {users = [{name='Alice', score=90}, {name='Bob', score=85}]}; y = data?.users.filter(rule it.score > 87)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        var arr = (object[])r.Get("y");
        Assert.AreEqual(1, arr.Length);
        var s = (System.Collections.Generic.IReadOnlyDictionary<string, object>)arr[0];
        Assert.IsTrue(s.ContainsKey("name"), "name field lost in ?. + filter chain");
    }

    // ───────────────────────────────────────────────────────────────
    // MBug4 — Filter-then-first on struct-typed array, with a
    //   `T?` return-type annotation and an inferred-parameter lambda,
    //   fails with FU710 "Unable to cast (T)->Bool to (T?)->Bool".
    //   The implicit `T → T?` lift at the return position is incorrectly
    //   back-propagating into the filter predicate's parameter type.
    //   Workaround: annotate the lambda parameter, OR drop the `?` from
    //   return type, OR use map/first without filter.
    //   Specific to struct element types — primitive and array elements
    //   work fine. Idiomatic findFirstMatching pattern broken.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MBug4_FilterFirstStructOptionalReturn_BackPropagatesOptional() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build(
                "type p = {v:int}\r" +
                "f(arr:p[]):p? = arr.filter(rule it.v > 0).first()\r" +
                "out = f([p{v=-1}, p{v=2}])?.v ?? -99");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug4 (CRITICAL) — Stack overflow in TIC Destruction when
    //   `?.` is used on an anonymous-typed optional struct that has a
    //   function-valued field:
    //     a:{f:rule(int)->int}? = {f = rule it+1}
    //     y = a?.f(5)   # CRASH — process exit code 134
    //
    //   Trace: alternating Destruction(StateStruct,StateStruct) →
    //   Destruction(StateFun,StateFun) → repeat — cyclic constraint.
    //   Works with NAMED type (`type p={f:rule(int)->int}; a:p? = ...`).
    //   Works with `!.` instead of `?.`. Specific to ?. + anon struct
    //   type + function field combination.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug4_SafeAccessAnonOptStructFnField_StackOverflow() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("a:{f:rule(int)->int}? = {f = rule it+1}\ry = a?.f(5)"));
    }

    [Test]
    public void MR5Bug4_SafeAccessAnonNonOptStructFnField_StackOverflow() {
        // The bug also reproduced WITHOUT Optional — `?.` on a non-opt anon struct with fn field.
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("a:{f:rule(int)->int} = {f = rule it+1}\ry = a?.f(5)"));
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug5 — `a = {b=1}; y = a?.b` widens `a`'s inferred type to
    //   `{b:Int32}?` (Optional) instead of keeping the concrete
    //   non-optional shape. Spec allows `?.` on non-optional receivers
    //   as no-op; the receiver type should stay non-optional.
    //
    //   Asymmetric with `?[`: `a = [1,2,3]; y = a?[0]` keeps `a:Int32[]`
    //   (non-optional). Practical impact: confusing typeof/printing.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug5_SafeAccessOnNonOptInferred_ReceiverStaysNonOpt() {
        // Bug: `a = {b=1}; y = a?.b` widened `a`'s inferred type to {b:int}? (Optional),
        // silently infecting subsequent regular `.field` access on `a`. The cascade
        // assertion is the strongest signal: if `a` is widened to opt(struct), regular
        // `.c` is rejected with FU755. The fix makes `a` stay {b:int,c:int} so all three
        // succeed end-to-end.
        "a = {b=1, c=2}\ry = a?.b\rz = a.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas(("y", (object)1), ("z", (object)2));
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug5 BOUNDARY PROBES — different contexts of `?.`. Hypothesis
    // (per professor) is that PushConstraintsFunctions.cs:224-240 uses
    // `!IsSolved` to discriminate "inferred abstract struct" vs
    // "concrete literal struct" — but should use `IsOpen` instead.
    //
    //   Literal `{b=1}` → closed struct (IsOpen=false), already shape-rigid.
    //   `?.field`-introduced shape → open struct (IsOpen=true).
    //
    // These probes lock down behavior across contexts so any future fix
    // doesn't regress. All marked [Ignore] until fix lands.
    // ───────────────────────────────────────────────────────────────

    // Probe 1a: cascade ?. then regular . over 3 fields.
    //   Pre-fix: FU755 at `z = a.c` — `a` widened to Opt, regular `.c` rejected.
    //   Post-fix: a stays {b,c,d}; y:int?=1, z:int=2, w:int=3.
    [Test]
    public void MR5Bug5b_CascadeSafeThenRegular_ThreeFields() {
        var rt = "a = {b=1, c=2, d=3}\ry = a?.b\rz = a.c\rw = a.d"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas(("y", 1), ("z", 2), ("w", 3));
    }

    // Probe 2: chained `?.` after another `?.` on inferred nested struct.
    //   Pre-fix: outermost `a` widened to {b:{c:int}}? — regular `.b` fails FU755.
    //   Post-fix: a stays {b:{c:int}} non-opt; both ?.-chain and regular `.b` work.
    [Test]
    public void MR5Bug5b_ChainedSafeAccess_OuterStaysNonOpt() {
        "a = {b={c=1}}\ry = a?.b?.c\rz = a.b.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas(("y", 1), ("z", 1));
    }

    // Probe 3: ?. on annotated optional receiver. This is the canonical
    // ?. usage and MUST KEEP WORKING after the fix.
    [Test]
    public void MR5Bug5b_SafeAccessOnAnnotatedOpt_StillWorks() {
        var rt = "a:{b:int}? = {b=1}\ry = a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", 1);
    }

    // Probe 4: ?. on if-else-inferred opt receiver. This is the natural
    // way to acquire an optional value without annotation; MUST KEEP WORKING.
    [Test]
    public void MR5Bug5b_SafeAccessOnIfElseInferredOpt_StillWorks() {
        var rt = "a = if(true) {b=1} else none\ry = a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", 1);
    }

    // Probe 5: `map(rule it?.b)` over array of inferred non-opt struct.
    // Current: arr stays {b:int}[], y becomes int?[] (lambda param `it`
    // is not widened to opt; ?. always wraps result in opt). This is the
    // scenario PushConstraintsFunctions.cs:224 was originally written for.
    // The fix must NOT regress this.
    [Test]
    public void MR5Bug5b_MapWithSafeAccess_LambdaParamNotWidened() {
        var rt = "arr = [{b=1}, {b=2}]\ry = arr.map(rule it?.b)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", new int?[] { 1, 2 });
    }

    // Probe 5b: map with truly opt elements (annotated array). MUST KEEP WORKING.
    // Asserting only via DoesNotThrow + type — AssertResultHas does not handle
    // null-bearing arrays (test infra NRE in ToStringSmart).
    [Test]
    public void MR5Bug5b_MapWithSafeAccess_OptElements() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("arr:{b:int}?[] = [{b=1}, {b=2}, none]\ry = arr.map(rule it?.b)")
                .Run());
    }

    // Probe 6: F-bounded recursive named type (GH126-style). Currently
    // works on master. The fix MUST NOT regress this.
    [Test]
    public void MR5Bug5b_FBoundedRecursiveNamedType_StillWorks() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build(
                "type n = {v:int = 0, next:n? = none}\r" +
                "loop(x, acc) = if(x==none) acc else loop(x?.next, n{next=acc})\r" +
                "out = loop(n{}, n{})");
        Assert.DoesNotThrow(() => rt.Run());
    }

    // Probe 7: user-defined function with `?.` on parameter. The parameter
    // type and return type should be principled regardless of fix.
    // Current: works — y:int?=42 with input of multi-field literal.
    // Post-fix: must continue to work.
    [Test]
    public void MR5Bug5b_UserFnWithSafeAccess_StillWorks() {
        var rt = "f(x) = x?.value\ry = f({value=42, other=1})"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", 42);
    }

    // Probe 8 (control): NAMED-type cascade — already works because named
    // type pins the receiver shape. The bug is specific to anon inferred
    // structs. Provides "good half" of the discriminator for the fix.
    [Test]
    public void MR5Bug5b_CascadeOnNamedType_AlreadyWorks() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={b:int,c:int}\ra:p = p{b=1,c=2}\ry = a?.b\rz = a.c");
        Assert.DoesNotThrow(() => rt.Run());
    }
}
