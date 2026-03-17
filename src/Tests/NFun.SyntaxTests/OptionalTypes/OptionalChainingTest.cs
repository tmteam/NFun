namespace NFun.SyntaxTests.OptionalTypes;

using System;
using TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalChainingTest {
    [Test]
    public void OptionalChaining_StructField_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_StructField_None() {
        var result = "x:{name:text}? = none\r y = x?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_IntField_HasValue() =>
        "x:{age:int}? = {age = 25}\r y = x?.age"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 25);


    [Test]
    public void OptionalChaining_IntField_None() {
        var result = "x:{age:int}? = none\r y = x?.age"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_RealField_HasValue() =>
        "x:{n:real}? = {n = 3.14}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 3.14);


    [Test]
    public void OptionalChaining_BoolField_HasValue() =>
        "x:{flag:bool}? = {flag = true}\r y = x?.flag"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", true);


    // --- Short-circuit chain (nested non-nullable fields) ---

    [Test]
    public void OptionalChaining_NestedNonNullableField() =>
        "x:{profile:{name:text}}? = {profile = {name = 'Bob'}}\r y = x?.profile.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Bob");


    [Test]
    public void OptionalChaining_NestedNonNullableField_None() {
        var result = "x:{profile:{name:text}}? = none\r y = x?.profile.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalChaining_DoubleChain_HasValue() =>
        "x:{a:{b:int}?}? = {a = {b = 42}}\r y = x?.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalChaining_DoubleChain_OuterNone() {
        var result = "x:{a:{b:int}?}? = none\r y = x?.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_DoubleChain_InnerNone() {
        var result = "x:{a:{b:int}?} = {a = none}\r y = x.a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalChaining_ResultType_IsOptional() =>
        Assert.DoesNotThrow(() => "x:{name:text}? = {name = 'hi'}\r y:text? = x?.name".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalChaining_IntResult_IsOptional() =>
        Assert.DoesNotThrow(() => "x:{age:int}? = {age = 25}\r y:int? = x?.age".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void OptionalChaining_WithCoalesce_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name ?? 'default'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_WithCoalesce_None() =>
        "x:{name:text}? = none\r y = x?.name ?? 'default'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "default");


    [Test]
    public void OptionalChaining_IntWithCoalesce_HasValue() =>
        "x:{age:int}? = {age = 25}\r y = x?.age ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 25);


    [Test]
    public void OptionalChaining_IntWithCoalesce_None() =>
        "x:{age:int}? = none\r y = x?.age ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void OptionalChaining_NestedWithCoalesce() =>
        "x:{profile:{name:text}}? = none\r y = x?.profile.name ?? 'nobody'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "nobody");


    [Test]
    public void OptionalChaining_WithForceUnwrap_HasValue() =>
        "x:{name:text}? = {name = 'Alice'}\r y = x?.name!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_ArrayField_HasValue() =>
        "x:{items:int[]}? = {items = [10,20]}\r y = x?.items"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 10, 20 });

    [Test]
    public void OptionalChaining_ArrayField_None() {
        var result = "x:{items:int[]}? = none\r y = x?.items"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ByteField_HasValue() =>
        "x:{n:byte}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", (byte)1);


    [Test]
    public void OptionalChaining_Int16Field_HasValue() =>
        "x:{n:int16}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", (Int16)1);


    [Test]
    public void OptionalChaining_Int64Field_HasValue() =>
        "x:{n:int64}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", (Int64)1);


    [Test]
    public void OptionalChaining_Uint16Field_HasValue() =>
        "x:{n:uint16}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", (UInt16)1);


    [Test]
    public void OptionalChaining_Uint32Field_HasValue() =>
        "x:{n:uint32}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", (UInt32)1);


    [Test]
    public void OptionalChaining_Uint64Field_HasValue() =>
        "x:{n:uint64}? = {n = 1}\r y = x?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", (UInt64)1);


    [Test]
    public void OptionalChaining_CharField_HasValue() =>
        "x:{c:char}? = {c = /'z'}\r y = x?.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 'z');

    [Test]
    public void OptionalChaining_ByteField_None() {
        var result =
            "x:{n:byte}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Int16Field_None() {
        var result =
            "x:{n:int16}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Int64Field_None() {
        var result =
            "x:{n:int64}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint16Field_None() {
        var result =
            "x:{n:uint16}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint32Field_None() {
        var result =
            "x:{n:uint32}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_Uint64Field_None() {
        var result =
            "x:{n:uint64}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_RealField_None() {
        var result =
            "x:{n:real}? = none\r y = x?.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_TextField_None() {
        var result =
            "x:{t:text}? = none\r y = x?.t".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_BoolField_None() {
        var result =
            "x:{b:bool}? = none\r y = x?.b".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_CharField_None() {
        var result =
            "x:{c:char}? = none\r y = x?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_HasValue() =>
        "x:{a:{b:{c:int}?}?}? = {a = {b = {c = 99}}}\r y = x?.a?.b?.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 99);


    [Test]
    public void OptionalChaining_ThreeLevels_Level1None() {
        var result =
            "x:{a:{b:{c:int}?}?}? = none\r y = x?.a?.b?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_Level2None() {
        var result =
            "x:{a:{b:{c:int}?}?} = {a = none}\r y = x.a?.b?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_ThreeLevels_Level3None() {
        var result =
            "x:{a:{b:{c:int}?}} = {a = {b = none}}\r y = x.a.b?.c".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_MultipleFields_AccessEach() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.name"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void OptionalChaining_MultipleFields_AccessAge() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.age"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 30);


    [Test]
    public void OptionalChaining_MultipleFields_AccessBool() =>
        "s:{name:text; age:int; active:bool}? = {name = 'Alice'; age = 30; active = true}\r y = s?.active"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", true);


    [Test]
    public void OptionalChaining_ArrayField_Count() =>
        "x:{items:int[]}? = {items = [1,2,3]}\r y = x?.items"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 2, 3 });


    [TestCase("x:{n:byte}? = {n = 5}\r y = x?.n ?? 0", (byte)5)]
    [TestCase("x:{n:int16}? = {n = 5}\r y = x?.n ?? 0", (Int16)5)]
    [TestCase("x:{n:int64}? = {n = 5}\r y = x?.n ?? 0", (Int64)5)]
    [TestCase("x:{n:real}? = {n = 1.5}\r y = x?.n ?? 0.0", 1.5)]
    [TestCase("x:{t:text}? = {t = 'hi'}\r y = x?.t ?? 'bye'", "hi")]
    [TestCase("x:{b:bool}? = {b = true}\r y = x?.b ?? false", true)]
    public void OptionalChaining_WithCoalesce_EachType_HasValue(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", expected);


    [TestCase("x:{n:byte}? = none\r y = x?.n ?? 0", (byte)0)]
    [TestCase("x:{n:int16}? = none\r y = x?.n ?? 0", (Int16)0)]
    [TestCase("x:{n:int64}? = none\r y = x?.n ?? 0", (Int64)0)]
    [TestCase("x:{n:real}? = none\r y = x?.n ?? 0.0", 0.0)]
    [TestCase("x:{t:text}? = none\r y = x?.t ?? 'bye'", "bye")]
    [TestCase("x:{b:bool}? = none\r y = x?.b ?? false", false)]
    public void OptionalChaining_WithCoalesce_EachType_None(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", expected);


    [TestCase("x:{name:text} = {name = 'hi'}\r y = x?.name")]
    [TestCase("x:int\r y = x?.name")]
    [TestCase("x:text\r y = x?.count")]
    public void OptionalChaining_NonOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [Test]
    public void OptionalChaining_StructWithOptionalField_FieldHasValue() =>
        "s:{n:int?} = {n = 42}\r y = s.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalChaining_StructWithOptionalField_FieldIsNone() {
        var result =
            "s:{n:int?} = {n = none}\r y = s.n".CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalChaining_OptionalStructWithOptionalField() =>
        "s:{n:int?}? = {n = 42}\r y = s?.n"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalStruct_ChainAccess() =>
        "x:{a:int}? = {a = 1}\r y = x?.a"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 1);


    [Test]
    public void OptionalStruct_ChainAccess_None() {
        var result =
            "x:{a:int}? = none\r y = x?.a"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    [Ignore("Requires {name:type = value} struct literal field spec syntax")]
    public void NestedOptionalStruct_ChainAccess() =>
        Assert.DoesNotThrow(() =>
            "s = {inner:{n:int?}? = {n = 42}}\r y = s.inner?.n"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalStructOptionalField_ChainCoalesce() =>
        "s:{n:int?}? = {n = 42}\r y = s?.n ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);


    [Test]
    public void OptionalStructOptionalField_OuterNone() =>
        "s:{n:int?}? = none\r y = s?.n ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Stress_NestedOptionalChaining() =>
        Assert.DoesNotThrow(() =>
            "x:{a:{b:{c:int?}?}?}? = {a = {b = {c = 42}}}\r y = x?.a?.b?.c".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void Combo_ChainingCoalesceUnwrap_HasValue() =>
        "s:{inner:{n:int?}?}? = {inner = {n = 42}}\r y = (s?.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneOuter() =>
        "s:{inner:{n:int?}?}? = none\r y = (s?.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneInner() =>
        "s:{inner:{n:int?}?} = {inner = none}\r y = (s.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneLeaf() =>
        "s:{inner:{n:int?}?} = {inner = {n = none}}\r y = (s.inner?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainingThenCoalesce_MultiField() =>
        "s:{name:text; age:int}? = {name = 'Alice'; age = 30}\r y = s?.name ?? 'unknown'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "Alice");


    [Test]
    public void Combo_ChainingThenCoalesce_MultiField_None() =>
        "s:{name:text; age:int}? = none\r y = s?.name ?? 'unknown'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", "unknown");


    [Test]
    public void Combo_ChainingIntThenCoalesce() =>
        "s:{age:int}? = {age = 25}\r y = s?.age ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 25);


    [Test]
    public void Combo_ChainingIntThenCoalesce_None() =>
        "s:{age:int}? = none\r y = s?.age ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", -1);


    [Test]
    public void Combo_DoubleChainingThenCoalesce() =>
        "s:{a:{b:int}?}? = {a = {b = 99}}\r y = s?.a?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 99);


    [Test]
    public void Combo_DoubleChainingThenCoalesce_OuterNone() =>
        "s:{a:{b:int}?}? = none\r y = s?.a?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_DoubleChainingThenCoalesce_InnerNone() =>
        "s:{a:{b:int}?} = {a = none}\r y = s.a?.b ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 0);


    [Test]
    public void Combo_ChainCoalesceUnwrap_AllThree() =>
        "s:{n:int?}? = {n = 10}\r y = (s?.n ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 10);


    [Test]
    public void Combo_ChainUnwrapArithmetic() =>
        "s:{n:int}? = {n = 5}\r y = s!.n + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 6);


    [Test]
    public void Combo_ChainUnwrapArithmetic_None_RuntimeError() =>
        "s:{n:int}? = none\r y = s!.n + 1"
            .AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [Test]
    public void Combo_ChainingCoalesceArithmetic() =>
        "s:{n:int}? = {n = 5}\r y = (s?.n ?? 0) + 10"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 15);


    [Test]
    public void Combo_ChainingCoalesceArithmetic_None() =>
        "s:{n:int}? = none\r y = (s?.n ?? 0) + 10"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 10);


    [Test]
    public void Combo_ChainingCoalesceMultiply() =>
        "s:{n:int}? = {n = 3}\r y = (s?.n ?? 1) * 2"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 6);


    [Test]
    public void Combo_DeepNestAllOperators() =>
        @"root:{child:{leaf:int?}?}? = {child = {leaf = 7}}
          fallback:int? = 100
          y = (root?.child?.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 8);


    [Test]
    public void Combo_DeepNestAllOperators_NoneRoot() =>
        @"root:{child:{leaf:int?}?}? = none
          fallback:int? = 100
          y = (root?.child?.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_DeepNestAllOperators_NoneChild() =>
        @"root:{child:{leaf:int?}?} = {child = none}
          fallback:int? = 100
          y = (root.child?.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_DeepNestAllOperators_NoneLeaf() =>
        @"root:{child:{leaf:int?}} = {child = {leaf = none}}
          fallback:int? = 100
          y = (root.child.leaf ?? fallback!) + 1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 101);


    [Test]
    public void Combo_MultiFieldChainCoalesce() =>
        @"s:{name:text; age:int}? = {name = 'Bob'; age = 30}
          y = (s?.name ?? 'unknown')
          z = (s?.age ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas(("y", (object)"Bob"), ("z", (object)30));


    [Test]
    public void Combo_MultiFieldChainCoalesce_None() =>
        @"s:{name:text; age:int}? = none
          y = (s?.name ?? 'unknown')
          z = (s?.age ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas(("y", (object)"unknown"), ("z", (object)0));


    [Test]
    public void Combo_TripleOperator_ChainCoalesceFallbackUnwrap() =>
        @"s:{n:int}? = {n = 42}
          fallback:int? = 99
          y = s?.n ?? fallback!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 42);


    [Test]
    public void Combo_TripleOperator_ChainNone_CoalesceFallbackUnwrap() =>
        @"s:{n:int}? = none
          fallback:int? = 99
          y = s?.n ?? fallback!"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", 99);


    [Test]
    public void Combo_TripleOperator_ChainNone_FallbackNone_RuntimeError() =>
        @"s:{n:int}? = none
          fallback:int? = none
          y = s?.n ?? fallback!"
            .AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
}
