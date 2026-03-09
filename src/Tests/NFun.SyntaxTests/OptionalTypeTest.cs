using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class OptionalTypeTest {

    // ═══════════════════════════════════════════════════════════════
    // Step 1: none literal + T? annotations (~100 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- none literal ---

    [Test]
    public void NoneLiteral_Standalone() =>
        Assert.DoesNotThrow(() => "y = none".Build());

    [Test]
    public void NoneLiteral_BothBranchesNone() =>
        Assert.DoesNotThrow(() => "y = if(true) none else none".Build());

    // --- T? = none (builds without error) ---

    [TestCase("y:byte? = none")]
    [TestCase("y:int16? = none")]
    [TestCase("y:int32? = none")]
    [TestCase("y:int? = none")]
    [TestCase("y:int64? = none")]
    [TestCase("y:uint16? = none")]
    [TestCase("y:uint32? = none")]
    [TestCase("y:uint64? = none")]
    [TestCase("y:real? = none")]
    [TestCase("y:text? = none")]
    [TestCase("y:char? = none")]
    [TestCase("y:bool? = none")]
    [TestCase("y:any? = none")]
    public void OptionalAnnotation_AssignNone_Builds(string expr) =>
        Assert.DoesNotThrow(() => expr.Build());

    [Test]
    public void OptionalInt_AssignNone_ReturnsNull() {
        var result = "y:int? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalReal_AssignNone_ReturnsNull() {
        var result = "y:real? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalText_AssignNone_ReturnsNull() {
        var result = "y:text? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalBool_AssignNone_ReturnsNull() {
        var result = "y:bool? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalChar_AssignNone_ReturnsNull() {
        var result = "y:char? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalByte_AssignNone_ReturnsNull() {
        var result = "y:byte? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalInt16_AssignNone_ReturnsNull() {
        var result = "y:int16? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalInt64_AssignNone_ReturnsNull() {
        var result = "y:int64? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalUint16_AssignNone_ReturnsNull() {
        var result = "y:uint16? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalUint32_AssignNone_ReturnsNull() {
        var result = "y:uint32? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalUint64_AssignNone_ReturnsNull() {
        var result = "y:uint64? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalAny_AssignNone_ReturnsNull() {
        var result = "y:any? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- T? = value (implicit T → T?) ---

    [TestCase("y:byte? = 42", (byte)42)]
    [TestCase("y:int16? = 42", (Int16)42)]
    [TestCase("y:int32? = 42", (Int32)42)]
    [TestCase("y:int? = 42", (Int32)42)]
    [TestCase("y:int64? = 42", (Int64)42)]
    [TestCase("y:uint16? = 42", (UInt16)42)]
    [TestCase("y:uint32? = 42", (UInt32)42)]
    [TestCase("y:uint64? = 42", (UInt64)42)]
    [TestCase("y:real? = 1.5", 1.5)]
    [TestCase("y:real? = 42.0", 42.0)]
    [TestCase("y:text? = 'hello'", "hello")]
    [TestCase("y:text? = ''", "")]
    [TestCase("y:bool? = true", true)]
    [TestCase("y:bool? = false", false)]
    public void OptionalAnnotation_AssignValue(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("y:char? = /'a'", 'a')]
    [TestCase("y:char? = /'z'", 'z')]
    [TestCase("y:char? = /'0'", '0')]
    public void OptionalChar_AssignValue(string expr, char expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("y:any? = 42", 42)]
    [TestCase("y:any? = 'hello'", "hello")]
    [TestCase("y:any? = true", true)]
    [TestCase("y:any? = 1.5", 1.5)]
    public void OptionalAny_AssignValue(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- T? implicit upcast from integer ---

    [TestCase("y:int32? = 0xFF", (Int32)255)]
    [TestCase("y:int64? = 0xFF", (Int64)255)]
    [TestCase("y:real? = 42", 42.0)]
    [TestCase("y:int64? = 42", (Int64)42)]
    public void OptionalAnnotation_ImplicitUpcast(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Array of optionals: T?[] ---

    [Test]
    public void ArrayOfOptionalInts_WithValues() =>
        "y:int?[] = [1, 2, 3]".AssertReturns("y", new[] { 1, 2, 3 });

    [Test]
    public void ArrayOfOptionalInts_WithNone() =>
        Assert.DoesNotThrow(() => "y:int?[] = [1, none, 3]".Build());

    [Test]
    public void ArrayOfOptionalReals_WithNone() =>
        Assert.DoesNotThrow(() => "y:real?[] = [1.0, none, 3.0]".Build());

    [Test]
    public void ArrayOfOptionalTexts_WithNone() =>
        Assert.DoesNotThrow(() => "y:text?[] = ['hello', none]".Build());

    [Test]
    public void ArrayOfOptionalBools_WithNone() =>
        Assert.DoesNotThrow(() => "y:bool?[] = [true, none, false]".Build());

    // --- Optional array: T[]? ---

    [Test]
    public void OptionalArrayOfInts_WithValue() =>
        "y:int[]? = [1, 2, 3]".AssertReturns("y", new[] { 1, 2, 3 });

    [Test]
    public void OptionalArrayOfInts_WithNone() {
        var result = "y:int[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalArrayOfReals_WithNone() {
        var result = "y:real[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalArrayOfTexts_WithNone() {
        var result = "y:text[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- T?[]? (optional array of optionals) ---

    [Test]
    public void OptionalArrayOfOptionalInts_WithValue() =>
        Assert.DoesNotThrow(() => "y:int?[]? = [1, none, 3]".Build());

    [Test]
    public void OptionalArrayOfOptionalInts_WithNone() {
        var result = "y:int?[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Multiple T? variables ---

    [Test]
    public void MultipleOptionalVars_BothNone() =>
        Assert.DoesNotThrow(() => "a:int? = none\r b:int? = none".Build());

    [Test]
    public void MultipleOptionalVars_MixedNoneValue() =>
        Assert.DoesNotThrow(() => "a:int? = 42\r b:int? = none".Build());

    [Test]
    public void MultipleOptionalVars_BothValues() =>
        "a:int? = 1\r b:int? = 2\r y = a! + b!".AssertResultHas("y", 3);

    // --- T? assigned from another T? ---

    [TestCase("a:int? = 42\r b:int? = a", 42)]
    [TestCase("a:real? = 1.5\r b:real? = a", 1.5)]
    [TestCase("a:text? = 'hi'\r b:text? = a", "hi")]
    [TestCase("a:bool? = true\r b:bool? = a", true)]
    public void OptionalAssignedFromOptional(string expr, object expected) =>
        expr.AssertResultHas("b", expected);

    [Test]
    public void OptionalAssignedFromOptional_None() {
        var result = "a:int? = none\r b:int? = a".Calc();
        Assert.IsNull(result.Get("b"));
    }

    // --- T? in multiple output equations ---

    [Test]
    public void MultipleOutputs_OptionalAndNonOptional() =>
        "y:int? = 42\r z:int = 10".AssertReturns(("y", (object)42), ("z", (object)10));

    // --- char?[] ---

    [Test]
    public void ArrayOfOptionalChars_WithNone() =>
        Assert.DoesNotThrow(() => "y:char?[] = [/'a', none, /'c']".Build());

    [Test]
    public void ArrayOfOptionalChars_WithValues() =>
        Assert.DoesNotThrow(() => "y:char?[] = [/'x', /'y']".Build());

    // --- bool?[] ---

    [Test]
    public void ArrayOfOptionalBools_WithValues() =>
        Assert.DoesNotThrow(() => "y:bool?[] = [true, false]".Build());

    // --- real?[] ---

    [Test]
    public void OptionalArrayOfReals_WithValue() =>
        "y:real[]? = [1.0, 2.0, 3.0]".AssertReturns("y", new[] { 1.0, 2.0, 3.0 });

    // --- text?[] ---

    [Test]
    public void ArrayOfOptionalTexts_WithValues() =>
        Assert.DoesNotThrow(() => "y:text?[] = ['hello', 'world']".Build());

    // --- T? with hex/binary literals ---

    [TestCase("y:int? = 0xFF", (Int32)255)]
    [TestCase("y:int? = 0b1010", (Int32)10)]
    [TestCase("y:byte? = 0xFF", (byte)255)]
    [TestCase("y:int64? = 0xFFFF", (Int64)0xFFFF)]
    public void OptionalAnnotation_HexBinaryLiterals(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Negative: T = none (type error) ---

    [TestCase("y:int = none")]
    [TestCase("y:int32 = none")]
    [TestCase("y:int64 = none")]
    [TestCase("y:int16 = none")]
    [TestCase("y:byte = none")]
    [TestCase("y:uint16 = none")]
    [TestCase("y:uint32 = none")]
    [TestCase("y:uint64 = none")]
    [TestCase("y:real = none")]
    [TestCase("y:text = none")]
    [TestCase("y:char = none")]
    [TestCase("y:bool = none")]
    public void NonOptionalType_AssignNone_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // ═══════════════════════════════════════════════════════════════
    // Step 2: ?? operator (~120 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- Basic ?? with none literal ---

    [TestCase("y = none ?? 0", 0)]
    [TestCase("y = none ?? 1", 1)]
    [TestCase("y = none ?? 42", 42)]
    [TestCase("y = none ?? -1", -1)]
    [TestCase("y = none ?? true", true)]
    [TestCase("y = none ?? false", false)]
    [TestCase("y = none ?? 'hello'", "hello")]
    [TestCase("y = none ?? ''", "")]
    [TestCase("y = none ?? 1.5", 1.5)]
    [TestCase("y = none ?? 0.0", 0.0)]
    public void CoalesceOperator_NoneWithDefault(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("y = none ?? /'a'", 'a')]
    public void CoalesceOperator_NoneWithDefaultChar(string expr, char expected) =>
        expr.AssertReturns("y", expected);

    // --- ?? with typed none ---

    [TestCase("y:byte = none ?? 0", (byte)0)]
    [TestCase("y:int16 = none ?? 0", (Int16)0)]
    [TestCase("y:int32 = none ?? 0", (Int32)0)]
    [TestCase("y:int = none ?? 0", (Int32)0)]
    [TestCase("y:int64 = none ?? 0", (Int64)0)]
    [TestCase("y:uint16 = none ?? 0", (UInt16)0)]
    [TestCase("y:uint32 = none ?? 0", (UInt32)0)]
    [TestCase("y:uint64 = none ?? 0", (UInt64)0)]
    [TestCase("y:real = none ?? 0.0", 0.0)]
    [TestCase("y:text = none ?? 'default'", "default")]
    [TestCase("y:bool = none ?? false", false)]
    public void CoalesceOperator_TypedResult(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- ?? with optional variable (value present) ---

    [TestCase((byte)5, "x:byte?\r y = x ?? 0", (byte)5)]
    [TestCase((Int16)5, "x:int16?\r y = x ?? 0", (Int16)5)]
    [TestCase((Int32)5, "x:int?\r y = x ?? 0", (Int32)5)]
    [TestCase((Int64)5, "x:int64?\r y = x ?? 0", (Int64)5)]
    [TestCase((UInt16)5, "x:uint16?\r y = x ?? 0", (UInt16)5)]
    [TestCase((UInt32)5, "x:uint32?\r y = x ?? 0", (UInt32)5)]
    [TestCase((UInt64)5, "x:uint64?\r y = x ?? 0", (UInt64)5)]
    [TestCase(2.5, "x:real?\r y = x ?? 0.0", 2.5)]
    [TestCase("hi", "x:text?\r y = x ?? 'default'", "hi")]
    [TestCase(true, "x:bool?\r y = x ?? false", true)]
    public void CoalesceOperator_VariableHasValue(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertResultHas("y", expected);

    // --- ?? with optional variable assigned in expression ---

    [TestCase("x:int? = 42\r y = x ?? 0", 42)]
    [TestCase("x:int? = none\r y = x ?? 0", 0)]
    [TestCase("x:real? = 1.5\r y = x ?? 0.0", 1.5)]
    [TestCase("x:real? = none\r y = x ?? 0.0", 0.0)]
    [TestCase("x:text? = 'hi'\r y = x ?? 'default'", "hi")]
    [TestCase("x:text? = none\r y = x ?? 'default'", "default")]
    [TestCase("x:bool? = true\r y = x ?? false", true)]
    [TestCase("x:bool? = none\r y = x ?? false", false)]
    [TestCase("x:int? = 42\r y = x ?? 99", 42)]
    [TestCase("x:int? = none\r y = x ?? 99", 99)]
    public void CoalesceOperator_InlineOptionalVariable(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? chains ---

    [TestCase("y = none ?? none ?? 42", 42)]
    [TestCase("y = none ?? none ?? none ?? 1", 1)]
    [TestCase("y = none ?? 10 ?? 20", 10)]
    [TestCase("a:int? = none\r b:int? = none\r y = a ?? b ?? 0", 0)]
    [TestCase("a:int? = none\r b:int? = 5\r y = a ?? b ?? 0", 5)]
    [TestCase("a:int? = 3\r b:int? = 5\r y = a ?? b ?? 0", 3)]
    [TestCase("a:int? = none\r b:int? = none\r c:int? = 7\r y = a ?? b ?? c ?? 0", 7)]
    public void CoalesceOperator_Chain(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? with subexpressions ---

    [TestCase("x:int? = none\r y = x ?? (1 + 2)", 3)]
    [TestCase("x:int? = none\r y = x ?? (10 * 2)", 20)]
    [TestCase("x:int? = 5\r y = x ?? (1 + 2)", 5)]
    [TestCase("x:real? = none\r y = x ?? (1.0 + 2.5)", 3.5)]
    public void CoalesceOperator_WithExpressionDefault(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? with array default ---

    [Test]
    public void CoalesceOperator_OptionalArrayWithDefault() =>
        "x:int[]? = none\r y = x ?? [1,2,3]".AssertResultHas("y", new[] { 1, 2, 3 });

    [Test]
    public void CoalesceOperator_OptionalArrayHasValue() =>
        "x:int[]? = [4,5]\r y = x ?? [1,2,3]".AssertResultHas("y", new[] { 4, 5 });

    // --- ?? result type is non-optional ---

    [Test]
    public void CoalesceOperator_ResultIsNotOptional() {
        var runtime = "x:int? = none\r y = x ?? 0".Build();
        // y should be int, not int?
        Assert.DoesNotThrow(() => runtime.Calc());
    }

    // --- ?? LCA: int? ?? real → real ---

    [TestCase("x:int? = none\r y = x ?? 1.5", 1.5)]
    [TestCase("x:int? = 3\r y = x ?? 1.5", 3.0)]
    [TestCase("x:byte? = none\r y = x ?? 256", 256)]
    public void CoalesceOperator_LcaWidening(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? with each numeric type via Calc (value present → returns value) ---

    [TestCase((byte)10, "x:byte?\r y = x ?? 0", (byte)10)]
    [TestCase((Int16)10, "x:int16?\r y = x ?? 0", (Int16)10)]
    [TestCase((Int32)10, "x:int32?\r y = x ?? 0", (Int32)10)]
    [TestCase((Int64)10, "x:int64?\r y = x ?? 0", (Int64)10)]
    [TestCase((UInt16)10, "x:uint16?\r y = x ?? 0", (UInt16)10)]
    [TestCase((UInt32)10, "x:uint32?\r y = x ?? 0", (UInt32)10)]
    [TestCase((UInt64)10, "x:uint64?\r y = x ?? 0", (UInt64)10)]
    [TestCase(3.14, "x:real?\r y = x ?? 0.0", 3.14)]
    public void CoalesceOperator_EachNumericType_HasValue(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertResultHas("y", expected);

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

    // --- ?? with array element ---

    [Test]
    public void CoalesceOperator_ArrayElement_HasValue() =>
        "arr:int?[] = [10, none, 30]\r y = arr[0] ?? -1".AssertResultHas("y", 10);

    [Test]
    public void CoalesceOperator_ArrayElement_None() =>
        "arr:int?[] = [10, none, 30]\r y = arr[1] ?? -1".AssertResultHas("y", -1);

    // --- ?? with function result ---

    [TestCase("f():int? = none\r y = f() ?? 42", 42)]
    [TestCase("f():int? = 10\r y = f() ?? 42", 10)]
    [TestCase("f():text? = none\r y = f() ?? 'default'", "default")]
    [TestCase("f():text? = 'hello'\r y = f() ?? 'default'", "hello")]
    [TestCase("f():bool? = none\r y = f() ?? true", true)]
    [TestCase("f():real? = none\r y = f() ?? 3.14", 3.14)]
    public void CoalesceOperator_FunctionResult(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? LCA more cases ---

    [TestCase("x:byte? = none\r y = x ?? 1000", 1000)]
    [TestCase("x:int16? = none\r y = x ?? 1.5", 1.5)]
    [TestCase("x:byte? = 10\r y = x ?? 1.5", 10.0)]
    [TestCase("x:int16? = 5\r y = x ?? 100000", (Int64)5)]
    public void CoalesceOperator_LcaMoreCases(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? preserving text type ---

    [TestCase("x:text? = none\r y = x ?? ''", "")]
    [TestCase("x:text? = 'abc'\r y = x ?? ''", "abc")]
    [TestCase("x:text? = none\r y = x ?? 'a'", "a")]
    public void CoalesceOperator_TextPreserved(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? with boolean ---

    [TestCase("x:bool? = none\r y = x ?? true", true)]
    [TestCase("x:bool? = none\r y = x ?? false", false)]
    [TestCase("x:bool? = true\r y = x ?? false", true)]
    [TestCase("x:bool? = false\r y = x ?? true", false)]
    public void CoalesceOperator_BoolVariants(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? result used in further expressions ---

    [TestCase("x:int? = 5\r y = (x ?? 0) + 10", 15)]
    [TestCase("x:int? = none\r y = (x ?? 0) + 10", 10)]
    [TestCase("x:int? = 3\r y = (x ?? 1) * (x ?? 1)", 9)]
    [TestCase("x:int? = none\r y = (x ?? 2) * (x ?? 3)", 6)]
    [TestCase("x:real? = 2.0\r y = (x ?? 0.0) / 2.0", 1.0)]
    public void CoalesceOperator_ResultInExpression(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? optional array operations ---

    [Test]
    public void CoalesceOperator_OptionalTextArray() =>
        Assert.DoesNotThrow(() => "x:text[]? = none\r y = x ?? ['default']".Build());

    [Test]
    public void CoalesceOperator_OptionalRealArray() =>
        "x:real[]? = none\r y = x ?? [1.0, 2.0]".AssertResultHas("y", new[] { 1.0, 2.0 });

    // --- Negative: ?? on non-optional ---

    [TestCase("y = 42 ?? 0")]
    [TestCase("y = 'hello' ?? 'default'")]
    [TestCase("y = true ?? false")]
    [TestCase("y = 1.5 ?? 0.0")]
    [TestCase("y = [1,2] ?? [3]")]
    [TestCase("x:int\r y = x ?? 0")]
    [TestCase("x:real\r y = x ?? 0.0")]
    [TestCase("x:text\r y = x ?? 'default'")]
    [TestCase("x:bool\r y = x ?? false")]
    public void CoalesceOperator_NonOptionalLeft_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // ═══════════════════════════════════════════════════════════════
    // Step 3: ! operator (force unwrap) (~60 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- Basic unwrap with value ---

    [TestCase("y:int? = 42\r z = y!", 42)]
    [TestCase("y:int? = 0\r z = y!", 0)]
    [TestCase("y:int? = -1\r z = y!", -1)]
    [TestCase("y:real? = 1.5\r z = y!", 1.5)]
    [TestCase("y:real? = 0.0\r z = y!", 0.0)]
    [TestCase("y:text? = 'hi'\r z = y!", "hi")]
    [TestCase("y:text? = ''\r z = y!", "")]
    [TestCase("y:bool? = true\r z = y!", true)]
    [TestCase("y:bool? = false\r z = y!", false)]
    public void ForceUnwrap_HasValue(string expr, object expected) =>
        expr.AssertResultHas("z", expected);

    [TestCase("y:char? = /'a'\r z = y!", 'a')]
    public void ForceUnwrap_Char_HasValue(string expr, char expected) =>
        expr.AssertResultHas("z", expected);

    // --- Unwrap for each numeric type ---

    [TestCase("y:byte? = 1\r z = y!", (byte)1)]
    [TestCase("y:int16? = 1\r z = y!", (Int16)1)]
    [TestCase("y:int32? = 1\r z = y!", (Int32)1)]
    [TestCase("y:int64? = 1\r z = y!", (Int64)1)]
    [TestCase("y:uint16? = 1\r z = y!", (UInt16)1)]
    [TestCase("y:uint32? = 1\r z = y!", (UInt32)1)]
    [TestCase("y:uint64? = 1\r z = y!", (UInt64)1)]
    public void ForceUnwrap_EachNumericType(string expr, object expected) =>
        expr.AssertResultHas("z", expected);

    // --- Runtime panic on none ---

    [TestCase("y:int? = none\r z = y!")]
    [TestCase("y:real? = none\r z = y!")]
    [TestCase("y:text? = none\r z = y!")]
    [TestCase("y:bool? = none\r z = y!")]
    [TestCase("y:char? = none\r z = y!")]
    [TestCase("y:byte? = none\r z = y!")]
    [TestCase("y:int16? = none\r z = y!")]
    [TestCase("y:int32? = none\r z = y!")]
    [TestCase("y:int64? = none\r z = y!")]
    [TestCase("y:uint16? = none\r z = y!")]
    [TestCase("y:uint32? = none\r z = y!")]
    [TestCase("y:uint64? = none\r z = y!")]
    public void ForceUnwrap_None_RuntimeError(string expr) =>
        expr.AssertObviousFailsOnRuntime();

    // --- Unwrap in expressions ---

    [TestCase("x:int? = 5\r y = x! + 1", 6)]
    [TestCase("x:int? = 5\r y = x! - 1", 4)]
    [TestCase("x:int? = 5\r y = x! * 2", 10)]
    [TestCase("x:real? = 10.0\r y = x! / 2.0", 5.0)]
    [TestCase("x:int? = 7\r y = x! % 3", 1)]
    [TestCase("x:int? = 2\r y = x! ** 3", 8)]
    public void ForceUnwrap_InArithmeticExpr(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Unwrap result type is T (not T?) ---

    [Test]
    public void ForceUnwrap_ResultIsNonOptional() =>
        Assert.DoesNotThrow(() => "x:int? = 42\r y:int = x!".Build());

    // --- Unwrap optional array ---

    [Test]
    public void ForceUnwrap_OptionalArray_IndexAccess() =>
        "x:int[]? = [10,20,30]\r y = x![0]".AssertResultHas("y", 10);

    [Test]
    public void ForceUnwrap_OptionalArray_None_RuntimeError() =>
        "x:int[]? = none\r y = x![0]".AssertObviousFailsOnRuntime();

    // --- Unwrap in chain: a! + b! ---

    [TestCase("a:int? = 3\r b:int? = 4\r y = a! + b!", 7)]
    [TestCase("a:int? = 3\r b:int? = 4\r y = a! * b!", 12)]
    [TestCase("a:real? = 2.0\r b:real? = 3.0\r y = a! + b!", 5.0)]
    [TestCase("a:real? = 2.0\r b:real? = 3.0\r y = a! * b!", 6.0)]
    public void ForceUnwrap_TwoUnwrapsInExpr(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Unwrap function result ---

    [TestCase("f():int? = 42\r y = f()!", 42)]
    [TestCase("f():real? = 1.5\r y = f()!", 1.5)]
    [TestCase("f():text? = 'hi'\r y = f()!", "hi")]
    [TestCase("f():bool? = true\r y = f()!", true)]
    public void ForceUnwrap_FunctionResult(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("f():int? = none\r y = f()!")]
    [TestCase("f():text? = none\r y = f()!")]
    public void ForceUnwrap_FunctionResult_None_RuntimeError(string expr) =>
        expr.AssertObviousFailsOnRuntime();

    // --- Unwrap then method call ---

    [Test]
    public void ForceUnwrap_OptionalArray_Count() =>
        "x:int[]? = [1,2,3]\r y = x!.count()".AssertResultHas("y", 3);

    [Test]
    public void ForceUnwrap_OptionalArray_Map() =>
        "x:int[]? = [1,2,3]\r y = x!.map(rule it * 2)".AssertResultHas("y", new[] { 2, 4, 6 });

    [Test]
    public void ForceUnwrap_OptionalText_Count() =>
        "x:text? = 'abc'\r y = x!.count()".AssertResultHas("y", 3);

    // --- Unwrap with comparison ---

    [TestCase("x:int? = 5\r y = x! > 3", true)]
    [TestCase("x:int? = 1\r y = x! > 3", false)]
    [TestCase("x:int? = 5\r y = x! == 5", true)]
    [TestCase("x:int? = 5\r y = x! != 5", false)]
    [TestCase("x:int? = 5\r y = x! >= 5", true)]
    [TestCase("x:int? = 5\r y = x! <= 5", true)]
    public void ForceUnwrap_InComparison(string expr, bool expected) =>
        expr.AssertResultHas("y", expected);

    // --- Negative: unwrap non-optional ---

    [TestCase("y = 42!")]
    [TestCase("y = 'hello'!")]
    [TestCase("y = true!")]
    [TestCase("y = 1.5!")]
    [TestCase("x:int = 5\r y = x!")]
    [TestCase("x:real = 1.5\r y = x!")]
    [TestCase("x:text = 'hi'\r y = x!")]
    [TestCase("x:bool = true\r y = x!")]
    [TestCase("y = [1,2,3]!")]
    public void ForceUnwrap_NonOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

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
        "x:{num:real}? = {num = 3.14}\r y = x?.num".AssertResultHas("y", 3.14);

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
    // Step 5: if-else with none + LCA (~100 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- Basic: if(cond) value else none → T? ---

    [TestCase("y = if(true) 42 else none", 42)]
    [TestCase("y = if(true) 1 else none", 1)]
    [TestCase("y = if(true) 0 else none", 0)]
    [TestCase("y = if(true) -1 else none", -1)]
    [TestCase("y = if(true) 1.5 else none", 1.5)]
    [TestCase("y = if(true) 'hello' else none", "hello")]
    [TestCase("y = if(true) true else none", true)]
    public void IfElse_ValueElseNone_TrueBranch(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("y = if(true) /'a' else none", 'a')]
    public void IfElse_CharElseNone_TrueBranch(string expr, char expected) =>
        expr.AssertReturns("y", expected);

    // --- if(false) value else none → none ---

    [Test]
    public void IfElse_IntElseNone_FalseBranch() {
        var result = "y = if(false) 42 else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void IfElse_RealElseNone_FalseBranch() {
        var result = "y = if(false) 1.5 else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void IfElse_TextElseNone_FalseBranch() {
        var result = "y = if(false) 'hi' else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void IfElse_BoolElseNone_FalseBranch() {
        var result = "y = if(false) true else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void IfElse_CharElseNone_FalseBranch() {
        var result = "y = if(false) /'a' else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- if(cond) none else value ---

    [TestCase("y = if(false) none else 42", 42)]
    [TestCase("y = if(false) none else 1.5", 1.5)]
    [TestCase("y = if(false) none else 'hello'", "hello")]
    [TestCase("y = if(false) none else true", true)]
    [TestCase("y = if(false) none else false", false)]
    public void IfElse_NoneElseValue_FalseBranch(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [Test]
    public void IfElse_NoneElseValue_TrueBranch() {
        var result = "y = if(true) none else 42".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- if(cond) none else none → none ---

    [Test]
    public void IfElse_BothNone() {
        var result = "y = if(true) none else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Each type: if(flag) T else none → T? ---

    [TestCase("x:bool\r y = if(x) 0xFF else none", true, (byte)0xFF)]
    [TestCase("x:bool\r y = if(x) 0xFF else none", false, null)]
    public void IfElse_ByteElseNone(string expr, object input, object expected) {
        if (expected == null) {
            var result = expr.Calc("x", input);
            Assert.IsNull(result.Get("y"));
        } else {
            expr.Calc("x", input).AssertResultHas("y", expected);
        }
    }

    [TestCase("y:byte? = if(true) 1 else none", (byte)1)]
    [TestCase("y:int16? = if(true) 1 else none", (Int16)1)]
    [TestCase("y:int32? = if(true) 1 else none", (Int32)1)]
    [TestCase("y:int? = if(true) 1 else none", (Int32)1)]
    [TestCase("y:int64? = if(true) 1 else none", (Int64)1)]
    [TestCase("y:uint16? = if(true) 1 else none", (UInt16)1)]
    [TestCase("y:uint32? = if(true) 1 else none", (UInt32)1)]
    [TestCase("y:uint64? = if(true) 1 else none", (UInt64)1)]
    [TestCase("y:real? = if(true) 1.0 else none", 1.0)]
    [TestCase("y:text? = if(true) 'hi' else none", "hi")]
    [TestCase("y:bool? = if(true) true else none", true)]
    public void IfElse_TypedOptional_TrueBranch(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- LCA with optionals in if-else ---

    [TestCase("x:int? = 42\r y = if(true) x else 1.0", 42.0)]
    [TestCase("x:int? = 42\r z:int? = 10\r y = if(true) x else z", 42)]
    [TestCase("x:int? = 42\r z:real? = 1.5\r y = if(true) x else z", 42.0)]
    public void IfElse_LcaWithOptionals(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    [Test]
    public void IfElse_IntOptionalElseReal_IsRealOptional() =>
        Assert.DoesNotThrow(() => "x:int? = 42\r y:real? = if(true) x else 1.0".Build());

    [Test]
    public void IfElse_IntOptionalElseIntOptional_IsIntOptional() =>
        Assert.DoesNotThrow(() => "x:int? = 42\r z:int? = 10\r y:int? = if(true) x else z".Build());

    [Test]
    public void IfElse_IntOptionalElseRealOptional_IsRealOptional() =>
        Assert.DoesNotThrow(() =>
            "x:int? = 42\r z:real? = 1.5\r y:real? = if(true) x else z".Build());

    [Test]
    public void IfElse_IntElseRealOptional_IsRealOptional() =>
        Assert.DoesNotThrow(() =>
            "z:real? = 1.5\r y:real? = if(true) 42 else z".Build());

    // --- Arrays in if-else with none ---

    [Test]
    public void IfElse_ArrayElseNone_TrueBranch() =>
        "y = if(true) [1,2] else none".AssertReturns("y", new[] { 1, 2 });

    [Test]
    public void IfElse_ArrayElseNone_FalseBranch() {
        var result = "y = if(false) [1,2] else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void IfElse_NoneElseArray() =>
        "y = if(false) none else [1,2]".AssertReturns("y", new[] { 1, 2 });

    // --- Structs in if-else with none ---

    [Test]
    public void IfElse_StructElseNone_TrueBranch() =>
        Assert.DoesNotThrow(() => "y = if(true) {a = 1} else none".Build());

    [Test]
    public void IfElse_StructElseNone_FalseBranch() {
        var result = "y = if(false) {a = 1} else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Nested if with none ---

    [TestCase("y = if(true) if(true) 42 else none else none", 42)]
    [TestCase("y = if(true) 42 else if(true) 0 else none", 42)]
    [TestCase("y = if(false) 42 else if(true) 0 else none", 0)]
    public void IfElse_Nested_WithNone(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [Test]
    public void IfElse_Nested_AllNone() {
        var result = "y = if(false) 42 else if(false) 0 else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void IfElse_Nested_InnerBothNone() {
        var result = "y = if(true) if(false) 42 else none else none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Each type: if(flag) none else T --- (through Calc)

    [TestCase(false, "x:bool\r y:int16? = if(x) none else 1", (Int16)1)]
    [TestCase(false, "x:bool\r y:int32? = if(x) none else 1", (Int32)1)]
    [TestCase(false, "x:bool\r y:int64? = if(x) none else 1", (Int64)1)]
    [TestCase(false, "x:bool\r y:uint16? = if(x) none else 1", (UInt16)1)]
    [TestCase(false, "x:bool\r y:uint32? = if(x) none else 1", (UInt32)1)]
    [TestCase(false, "x:bool\r y:uint64? = if(x) none else 1", (UInt64)1)]
    [TestCase(false, "x:bool\r y:byte? = if(x) none else 1", (byte)1)]
    [TestCase(false, "x:bool\r y:real? = if(x) none else 1.0", 1.0)]
    [TestCase(false, "x:bool\r y:text? = if(x) none else 'hi'", "hi")]
    [TestCase(false, "x:bool\r y:bool? = if(x) none else true", true)]
    public void IfElse_EachType_NoneElseValue_ViaCalc(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertResultHas("y", expected);

    // --- if-else with optional variable on both sides ---

    [TestCase("a:int? = 1\r b:int? = 2\r y = if(true) a else b", 1)]
    [TestCase("a:int? = 1\r b:int? = 2\r y = if(false) a else b", 2)]
    [TestCase("a:int? = none\r b:int? = 2\r y = if(true) a else b", null)]
    [TestCase("a:int? = 1\r b:int? = none\r y = if(false) a else b", null)]
    public void IfElse_BothOptionalVariables(string expr, object expected) {
        if (expected == null) {
            var result = expr.Calc();
            Assert.IsNull(result.Get("y"));
        } else {
            expr.AssertResultHas("y", expected);
        }
    }

    // --- if-else: T? in condition (unwrap needed) ---

    [TestCase("flag:bool? = true\r y = if(flag!) 1 else 0", 1)]
    [TestCase("flag:bool? = false\r y = if(flag!) 1 else 0", 0)]
    [TestCase("flag:bool? = true\r y = if(flag ?? false) 1 else 0", 1)]
    [TestCase("flag:bool? = none\r y = if(flag ?? false) 1 else 0", 0)]
    public void IfElse_OptionalBoolCondition(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- if-else: result assigned to T? ---

    [TestCase("y:int? = if(true) 42 else 0", 42)]
    [TestCase("y:int? = if(false) 42 else 0", 0)]
    [TestCase("y:real? = if(true) 1.5 else 2.5", 1.5)]
    [TestCase("y:text? = if(true) 'a' else 'b'", "a")]
    public void IfElse_NonOptionalBranches_AssignedToOptional(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- if-else array of optionals with none ---

    [Test]
    public void IfElse_ArrayOfOptionals_InTrueBranch() =>
        Assert.DoesNotThrow(() => "y = if(true) [1, none, 3] else [4, 5, 6]".Build());

    [Test]
    public void IfElse_NoneArray_InFalseBranch() =>
        Assert.DoesNotThrow(() => "y = if(true) [1, 2] else none".Build());

    // ═══════════════════════════════════════════════════════════════
    // Step 6: == none / != none (~50 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- x == none ---

    [TestCase("y:int? = 42\r z = y == none", false)]
    [TestCase("y:int? = none\r z = y == none", true)]
    [TestCase("y:real? = 1.5\r z = y == none", false)]
    [TestCase("y:real? = none\r z = y == none", true)]
    [TestCase("y:text? = 'hi'\r z = y == none", false)]
    [TestCase("y:text? = none\r z = y == none", true)]
    [TestCase("y:bool? = true\r z = y == none", false)]
    [TestCase("y:bool? = none\r z = y == none", true)]
    [TestCase("y:byte? = 1\r z = y == none", false)]
    [TestCase("y:byte? = none\r z = y == none", true)]
    [TestCase("y:int16? = 1\r z = y == none", false)]
    [TestCase("y:int16? = none\r z = y == none", true)]
    [TestCase("y:int64? = 1\r z = y == none", false)]
    [TestCase("y:int64? = none\r z = y == none", true)]
    [TestCase("y:uint16? = 1\r z = y == none", false)]
    [TestCase("y:uint16? = none\r z = y == none", true)]
    [TestCase("y:uint32? = 1\r z = y == none", false)]
    [TestCase("y:uint32? = none\r z = y == none", true)]
    [TestCase("y:uint64? = 1\r z = y == none", false)]
    [TestCase("y:uint64? = none\r z = y == none", true)]
    public void EqualsNone(string expr, bool expected) =>
        expr.AssertResultHas("z", expected);

    // --- x != none ---

    [TestCase("y:int? = 42\r z = y != none", true)]
    [TestCase("y:int? = none\r z = y != none", false)]
    [TestCase("y:real? = 1.5\r z = y != none", true)]
    [TestCase("y:real? = none\r z = y != none", false)]
    [TestCase("y:text? = 'hi'\r z = y != none", true)]
    [TestCase("y:text? = none\r z = y != none", false)]
    [TestCase("y:bool? = true\r z = y != none", true)]
    [TestCase("y:bool? = none\r z = y != none", false)]
    public void NotEqualsNone(string expr, bool expected) =>
        expr.AssertResultHas("z", expected);

    // --- none == none / none != none ---

    [TestCase("y = none == none", true)]
    [TestCase("y = none != none", false)]
    public void NoneComparedToNone(string expr, bool expected) =>
        expr.AssertReturns("y", expected);

    // --- == none in conditions ---

    [TestCase("x:int? = 42\r y = if(x != none) x! else 0", 42)]
    [TestCase("x:int? = none\r y = if(x != none) x! else 0", 0)]
    [TestCase("x:int? = 42\r y = if(x == none) 0 else x!", 42)]
    [TestCase("x:int? = none\r y = if(x == none) 0 else x!", 0)]
    public void EqualsNone_InCondition(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    [TestCase("x:text? = 'hi'\r y = if(x != none) x! else 'default'", "hi")]
    [TestCase("x:text? = none\r y = if(x != none) x! else 'default'", "default")]
    public void EqualsNone_TextInCondition(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Comparing non-optional with none ---

    [TestCase("y = 42 == none", false)]
    [TestCase("y = 'hi' == none", false)]
    [TestCase("y = true == none", false)]
    [TestCase("y = 42 != none", true)]
    [TestCase("y = 'hi' != none", true)]
    [TestCase("y = 1.5 == none", false)]
    [TestCase("y = 1.5 != none", true)]
    public void NonOptional_ComparedToNone(string expr, bool expected) =>
        expr.AssertReturns("y", expected);

    // --- == none used in boolean expressions ---

    [TestCase("x:int? = 42\r y:int? = none\r z = (x == none) or (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x == none) or (y == none)", false)]
    [TestCase("x:int? = none\r y:int? = none\r z = (x == none) and (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = none\r z = (x == none) and (y == none)", false)]
    public void EqualsNone_InBooleanExpr(string expr, bool expected) =>
        expr.AssertResultHas("z", expected);

    // --- != none for more types ---

    [TestCase("x:real? = 1.5\r z = x != none", true)]
    [TestCase("x:real? = none\r z = x != none", false)]
    [TestCase("x:byte? = 1\r z = x != none", true)]
    [TestCase("x:byte? = none\r z = x != none", false)]
    [TestCase("x:int64? = 1\r z = x != none", true)]
    [TestCase("x:int64? = none\r z = x != none", false)]
    public void NotEqualsNone_MoreTypes(string expr, bool expected) =>
        expr.AssertResultHas("z", expected);

    // ═══════════════════════════════════════════════════════════════
    // Step 7: Functions + optional (~100 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- User function with optional argument ---

    [TestCase("f(x:int?):int = x ?? 0\r y = f(42)", 42)]
    [TestCase("f(x:int?):int = x ?? 0\r y = f(none)", 0)]
    [TestCase("f(x:int?):int = x ?? 99\r y = f(10)", 10)]
    [TestCase("f(x:int?):int = x ?? 99\r y = f(none)", 99)]
    public void UserFunc_OptionalArg_CoalesceBody(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("f(x:text?):text = x ?? 'empty'\r y = f('hello')", "hello")]
    [TestCase("f(x:text?):text = x ?? 'empty'\r y = f(none)", "empty")]
    public void UserFunc_OptionalTextArg(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("f(x:bool?):bool = x ?? false\r y = f(true)", true)]
    [TestCase("f(x:bool?):bool = x ?? false\r y = f(none)", false)]
    public void UserFunc_OptionalBoolArg(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("f(x:real?):real = x ?? 0.0\r y = f(1.5)", 1.5)]
    [TestCase("f(x:real?):real = x ?? 0.0\r y = f(none)", 0.0)]
    public void UserFunc_OptionalRealArg(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- User function returning optional ---

    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5)", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5) ?? 0", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? 0", 0)]
    public void UserFunc_ReturnsOptional(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [Test]
    public void UserFunc_ReturnsOptional_NoneResult() {
        var result = "f(x:int):int? = if(x > 0) x else none\r y = f(-1)".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- User function optional chained with ?? ---

    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5) ?? f(3) ?? 0", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? f(3) ?? 0", 3)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? f(-2) ?? 0", 0)]
    public void UserFunc_ReturnsOptional_ChainedCoalesce(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Generic function with optional ---

    [TestCase("f(x) = x ?? 0\r y = f(none)", 0)]
    [TestCase("f(x) = x ?? 'default'\r y = f(none)", "default")]
    public void GenericFunc_WithCoalesce(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Optional in anonymous functions (rule) ---

    [Test]
    public void AnonymousFunc_CoalesceInBody() =>
        Assert.DoesNotThrow(() => "y = [1,2,3].map(rule it ?? 0)".Build());

    // --- Optional in pipe ---

    [TestCase("f(x:int?):int = x ?? 0\r x:int? = 42\r y = x.f()", 42)]
    [TestCase("f(x:int?):int = x ?? 0\r x:int? = none\r y = x.f()", 0)]
    public void OptionalInPipe(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Multiple optional parameters ---

    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(1, 2)", 3)]
    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(1, none)", 1)]
    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(none, 2)", 2)]
    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(none, none)", 0)]
    public void UserFunc_MultipleOptionalArgs(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Function returning optional in if-else ---

    [TestCase("f(x:int):int? = if(x > 0) x else none\r g(x:int):int = f(x) ?? -1\r y = g(5)", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r g(x:int):int = f(x) ?? -1\r y = g(-1)", -1)]
    public void UserFunc_OptionalComposition(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Recursive function returning optional ---

    [Test]
    public void UserFunc_Recursive_ReturnsOptional() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x == 0) none else f(x - 1)".Build());

    // --- map/filter/fold with optional ---

    [Test]
    public void MapProducingOptional() =>
        Assert.DoesNotThrow(() =>
            "y = [1, -2, 3].map(rule if(it > 0) it else none)".Build());

    [Test]
    public void FilterByOptional_Coalesce() =>
        "y = [1, -2, 3].filter(rule it > 0)".AssertReturns("y", new[] { 1, 3 });

    [Test]
    public void MapThenCoalesce() =>
        Assert.DoesNotThrow(() =>
            "y = [1, -2, 3].map(rule if(it > 0) it else none).map(rule it ?? 0)".Build());

    // --- Optional in pipe: different types ---

    [TestCase("f(x:real?):real = x ?? 0.0\r p:real? = 3.14\r y = p.f()", 3.14)]
    [TestCase("f(x:real?):real = x ?? 0.0\r p:real? = none\r y = p.f()", 0.0)]
    [TestCase("f(x:text?):text = x ?? 'none'\r p:text? = 'hi'\r y = p.f()", "hi")]
    [TestCase("f(x:text?):text = x ?? 'none'\r p:text? = none\r y = p.f()", "none")]
    [TestCase("f(x:bool?):bool = x ?? false\r p:bool? = true\r y = p.f()", true)]
    [TestCase("f(x:bool?):bool = x ?? false\r p:bool? = none\r y = p.f()", false)]
    public void OptionalInPipe_MoreTypes(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Function with optional → pipe → arithmetic ---

    [TestCase("f(x:int?):int = x ?? 0\r p:int? = 5\r y = p.f() + 10", 15)]
    [TestCase("f(x:int?):int = x ?? 0\r p:int? = none\r y = p.f() + 10", 10)]
    public void OptionalInPipe_ThenArithmetic(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Higher-order: function taking optional function ---

    [Test]
    public void FuncReturningOptional_UsedInMap() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x > 0) x else none\r y = [1,-2,3].map(f)".Build());

    // --- Function optional result → struct field ---

    [Test]
    public void FuncOptionalResult_AssignedToStructField() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x > 0) x else none\r y = {n = f(5)}".Build());

    // --- Function with all types of optional args ---

    [TestCase("f(a:byte?):byte = a ?? 0\r y = f(none)", (byte)0)]
    [TestCase("f(a:int16?):int16 = a ?? 0\r y = f(none)", (Int16)0)]
    [TestCase("f(a:int64?):int64 = a ?? 0\r y = f(none)", (Int64)0)]
    [TestCase("f(a:uint16?):uint16 = a ?? 0\r y = f(none)", (UInt16)0)]
    [TestCase("f(a:uint32?):uint32 = a ?? 0\r y = f(none)", (UInt32)0)]
    [TestCase("f(a:uint64?):uint64 = a ?? 0\r y = f(none)", (UInt64)0)]
    public void UserFunc_EachTypeOptionalArg(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Nested function calls with optional ---

    [TestCase("f(x:int?):int = x ?? 0\r g(x:int):int? = if(x > 0) x else none\r y = f(g(5))", 5)]
    [TestCase("f(x:int?):int = x ?? 0\r g(x:int):int? = if(x > 0) x else none\r y = f(g(-1))", 0)]
    public void NestedFuncCalls_WithOptional(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Anonymous function returning optional ---

    [Test]
    public void AnonymousFunc_ReturningOptional() =>
        Assert.DoesNotThrow(() =>
            "[1, -2, 3].map(rule if(it > 0) it else none)".Build());

    // --- Function accepting optional array ---

    [Test]
    public void UserFunc_OptionalArrayArg() =>
        Assert.DoesNotThrow(() =>
            "f(x:int[]?):int = (x ?? [0])[0]\r y = f(none)".Build());

    [Test]
    public void UserFunc_OptionalArrayArg_HasValue() =>
        "f(x:int[]?):int = (x ?? [0])[0]\r y = f([42, 1])".AssertReturns("y", 42);

    // ═══════════════════════════════════════════════════════════════
    // Step 8: Negatives — operators reject T? (~150 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- Arithmetic operators reject T? ---

    [TestCase("x:int?\r y = x + 1")]
    [TestCase("x:int?\r y = x - 1")]
    [TestCase("x:int?\r y = x * 2")]
    [TestCase("x:int?\r y = x / 2")]
    [TestCase("x:int?\r y = x % 2")]
    [TestCase("x:int?\r y = x ** 2")]
    [TestCase("x:int?\r y = x // 2")]
    [TestCase("x:int?\r y = 1 + x")]
    [TestCase("x:int?\r y = 1 - x")]
    [TestCase("x:int?\r y = 2 * x")]
    [TestCase("x:int?\r y = 2 / x")]
    [TestCase("x:int?\r y = 2 % x")]
    [TestCase("x:int?\r y = 2 ** x")]
    [TestCase("x:int?\r y = -x")]
    [TestCase("x:real?\r y = x + 1.0")]
    [TestCase("x:real?\r y = x - 1.0")]
    [TestCase("x:real?\r y = x * 2.0")]
    [TestCase("x:real?\r y = x / 2.0")]
    [TestCase("x:real?\r y = -x")]
    [TestCase("x:int64?\r y = x + 1")]
    [TestCase("x:int64?\r y = x - 1")]
    [TestCase("x:int64?\r y = x * 2")]
    [TestCase("x:byte?\r y = x + 1")]
    [TestCase("x:int16?\r y = x + 1")]
    [TestCase("x:uint16?\r y = x + 1")]
    [TestCase("x:uint32?\r y = x + 1")]
    [TestCase("x:uint64?\r y = x + 1")]
    public void ArithmeticOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Comparison operators (except == !=) reject T? ---

    [TestCase("x:int?\r y = x > 1")]
    [TestCase("x:int?\r y = x < 1")]
    [TestCase("x:int?\r y = x >= 1")]
    [TestCase("x:int?\r y = x <= 1")]
    [TestCase("x:int?\r y = 1 > x")]
    [TestCase("x:int?\r y = 1 < x")]
    [TestCase("x:int?\r y = 1 >= x")]
    [TestCase("x:int?\r y = 1 <= x")]
    [TestCase("x:real?\r y = x > 1.0")]
    [TestCase("x:real?\r y = x < 1.0")]
    [TestCase("x:real?\r y = x >= 1.0")]
    [TestCase("x:real?\r y = x <= 1.0")]
    [TestCase("x:int64?\r y = x > 1")]
    [TestCase("x:byte?\r y = x > 1")]
    [TestCase("x:int16?\r y = x > 1")]
    [TestCase("x:uint16?\r y = x > 1")]
    [TestCase("x:uint32?\r y = x > 1")]
    [TestCase("x:uint64?\r y = x > 1")]
    public void ComparisonOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Bitwise operators reject T? ---

    [TestCase("x:int?\r y = x & 1")]
    [TestCase("x:int?\r y = x | 1")]
    [TestCase("x:int?\r y = x ^ 1")]
    [TestCase("x:int?\r y = x << 1")]
    [TestCase("x:int?\r y = x >> 1")]
    [TestCase("x:int?\r y = ~x")]
    [TestCase("x:int?\r y = 1 & x")]
    [TestCase("x:int?\r y = 1 | x")]
    [TestCase("x:int?\r y = 1 ^ x")]
    [TestCase("x:int64?\r y = x & 1")]
    [TestCase("x:byte?\r y = x & 1")]
    [TestCase("x:uint32?\r y = x & 1")]
    public void BitwiseOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Boolean operators reject T? ---

    [TestCase("x:bool?\r y = x and true")]
    [TestCase("x:bool?\r y = x or true")]
    [TestCase("x:bool?\r y = x xor true")]
    [TestCase("x:bool?\r y = not x")]
    [TestCase("x:bool?\r y = true and x")]
    [TestCase("x:bool?\r y = true or x")]
    [TestCase("x:bool?\r y = true xor x")]
    public void BooleanOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Assigning T? to T ---

    [TestCase("x:int?\r y:int = x")]
    [TestCase("x:real?\r y:real = x")]
    [TestCase("x:text?\r y:text = x")]
    [TestCase("x:bool?\r y:bool = x")]
    [TestCase("x:char?\r y:char = x")]
    [TestCase("x:byte?\r y:byte = x")]
    [TestCase("x:int16?\r y:int16 = x")]
    [TestCase("x:int32?\r y:int32 = x")]
    [TestCase("x:int64?\r y:int64 = x")]
    [TestCase("x:uint16?\r y:uint16 = x")]
    [TestCase("x:uint32?\r y:uint32 = x")]
    [TestCase("x:uint64?\r y:uint64 = x")]
    public void AssignOptionalToNonOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Built-in functions reject T? ---

    [TestCase("x:int?\r y = abs(x)")]
    [TestCase("x:int?\r y = max(x, 1)")]
    [TestCase("x:int?\r y = min(x, 1)")]
    [TestCase("x:real?\r y = abs(x)")]
    [TestCase("x:real?\r y = round(x)")]
    [TestCase("x:real?\r y = sqrt(x)")]
    [TestCase("x:int?\r y = toText(x)")]
    [TestCase("x:int?\r y = [1,2,3].map(rule it + x)")]
    [TestCase("x:int?\r y = [1,2,3].filter(rule it > x)")]
    public void BuiltInFuncOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- String operations reject T? ---

    [TestCase("x:text?\r y = x.count()")]
    [TestCase("x:text?\r y = x[0]")]
    [TestCase("x:text?\r y = x.reverse()")]
    [TestCase("x:text?\r y = x.trim()")]
    [TestCase("x:text?\r y = x.concat('!')")]
    [TestCase("x:text?\r y = x.split(' ')")]
    public void StringOpsOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Array operations reject T? ---

    [TestCase("x:int?[]\r y = x[0] + 1")]
    [TestCase("x:int[]?\r y = x.count()")]
    [TestCase("x:int[]?\r y = x[0]")]
    [TestCase("x:int[]?\r y = x.map(rule it * 2)")]
    [TestCase("x:int[]?\r y = x.filter(rule it > 0)")]
    [TestCase("x:int[]?\r y = x.reverse()")]
    [TestCase("x:int[]?\r y = x.sum()")]
    [TestCase("x:real[]?\r y = x.sum()")]
    [TestCase("x:int[]?\r y = x.concat([1])")]
    public void ArrayOpsOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Arithmetic with two optionals ---

    [TestCase("a:int?\r b:int?\r y = a + b")]
    [TestCase("a:int?\r b:int?\r y = a - b")]
    [TestCase("a:int?\r b:int?\r y = a * b")]
    [TestCase("a:real?\r b:real?\r y = a + b")]
    [TestCase("a:real?\r b:real?\r y = a * b")]
    public void ArithmeticTwoOptionals_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Comparison with two optionals (except == !=) ---

    [TestCase("a:int?\r b:int?\r y = a > b")]
    [TestCase("a:int?\r b:int?\r y = a < b")]
    [TestCase("a:int?\r b:int?\r y = a >= b")]
    [TestCase("a:int?\r b:int?\r y = a <= b")]
    [TestCase("a:real?\r b:real?\r y = a > b")]
    public void ComparisonTwoOptionals_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Unary operations on optional ---

    [TestCase("x:int?\r y = -x")]
    [TestCase("x:int64?\r y = -x")]
    [TestCase("x:real?\r y = -x")]
    [TestCase("x:int?\r y = ~x")]
    [TestCase("x:int64?\r y = ~x")]
    [TestCase("x:byte?\r y = ~x")]
    [TestCase("x:bool?\r y = not x")]
    public void UnaryOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Assigning T? to incompatible types ---

    [TestCase("x:int?\r y:real = x")]
    [TestCase("x:byte?\r y:int = x")]
    [TestCase("x:int?\r y:int64 = x")]
    public void AssignOptionalToWiderNonOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Optional in range ---

    [TestCase("x:int?\r y = [1..x]")]
    [TestCase("x:int?\r y = [x..10]")]
    [TestCase("x:int?\r y = [1..10 step x]")]
    public void OptionalInRange_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // ═══════════════════════════════════════════════════════════════
    // Step 9: Arrays and structs with optional (~80 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- Arrays with none elements → T?[] ---

    [Test]
    public void ArrayLiteral_IntAndNone() =>
        Assert.DoesNotThrow(() => "[1, none, 3]".Build());

    [Test]
    public void ArrayLiteral_RealAndNone() =>
        Assert.DoesNotThrow(() => "[1.0, none]".Build());

    [Test]
    public void ArrayLiteral_TextAndNone() =>
        Assert.DoesNotThrow(() => "['hello', none]".Build());

    [Test]
    public void ArrayLiteral_BoolAndNone() =>
        Assert.DoesNotThrow(() => "[true, none, false]".Build());

    [Test]
    public void ArrayLiteral_AllNone() =>
        Assert.DoesNotThrow(() => "[none, none]".Build());

    [Test]
    public void ArrayLiteral_IntAndNone_TypedResult() =>
        Assert.DoesNotThrow(() => "y:int?[] = [1, none, 3]".Build());

    [Test]
    public void ArrayLiteral_RealAndNone_TypedResult() =>
        Assert.DoesNotThrow(() => "y:real?[] = [1.0, none, 3.0]".Build());

    // --- Optional array: element access ---

    [TestCase("x:int[]? = [10, 20, 30]\r y = x![0]", 10)]
    [TestCase("x:int[]? = [10, 20, 30]\r y = x![1]", 20)]
    [TestCase("x:int[]? = [10, 20, 30]\r y = x![2]", 30)]
    public void OptionalArray_ForceUnwrap_IndexAccess(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Array of optionals: element access ---

    [Test]
    public void ArrayOfOptionals_ElementAccess_HasValue() =>
        Assert.DoesNotThrow(() => "x:int?[] = [1, none, 3]\r y = x[0]".Build());

    [Test]
    public void ArrayOfOptionals_ElementAccess_IsOptional() =>
        Assert.DoesNotThrow(() => "x:int?[] = [1, none, 3]\r y:int? = x[0]".Build());

    [Test]
    public void ArrayOfOptionals_ElementAccess_NoneElement() {
        var result = "x:int?[] = [1, none, 3]\r y = x[1]".Calc();
        Assert.IsNull(result.Get("y"));
    }

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

    [Test]
    public void OptionalStruct_ChainAccess() =>
        "x:{a:int}? = {a = 1}\r y = x?.a".AssertResultHas("y", 1);

    [Test]
    public void OptionalStruct_ChainAccess_None() {
        var result = "x:{a:int}? = none\r y = x?.a".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Nested optional in struct ---

    [Test]
    public void NestedOptionalStruct_HasValue() =>
        Assert.DoesNotThrow(() =>
            "y = {inner:{num:int?}? = {num = 42}}".Build());

    [Test]
    public void NestedOptionalStruct_ChainAccess() =>
        Assert.DoesNotThrow(() =>
            "s = {inner:{num:int?}? = {num = 42}}\r y = s.inner?.num".Build());

    // --- Mixed arrays ---

    [Test]
    public void OptionalArray_Count() =>
        Assert.DoesNotThrow(() => "x:int[]? = [1,2,3]\r y = x!.count()".Build());

    [Test]
    public void OptionalArray_Map() =>
        Assert.DoesNotThrow(() => "x:int[]? = [1,2,3]\r y = x!.map(rule it * 2)".Build());

    // --- T?[]? combinations ---

    [Test]
    public void OptionalArrayOfOptionals_Builds() =>
        Assert.DoesNotThrow(() => "y:int?[]? = [1, none, 3]".Build());

    [Test]
    public void OptionalArrayOfOptionals_None() {
        var result = "y:int?[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Array of optional — each type ---

    [Test]
    public void ArrayOfOptionalReals_Builds() =>
        Assert.DoesNotThrow(() => "y:real?[] = [1.0, none, 3.0]".Build());

    [Test]
    public void ArrayOfOptionalInt64_Builds() =>
        Assert.DoesNotThrow(() => "y:int64?[] = [1, none]".Build());

    [Test]
    public void ArrayOfOptionalBytes_Builds() =>
        Assert.DoesNotThrow(() => "y:byte?[] = [1, none]".Build());

    [Test]
    public void ArrayOfOptionalUint32_Builds() =>
        Assert.DoesNotThrow(() => "y:uint32?[] = [1, none]".Build());

    // --- Optional array — each type ---

    [Test]
    public void OptionalRealArray_None() {
        var result = "y:real[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalInt64Array_None() {
        var result = "y:int64[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalByteArray_None() {
        var result = "y:byte[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalBoolArray_None() {
        var result = "y:bool[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalCharArray_None() {
        var result = "y:char[]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Optional array with unwrap and operations ---

    [Test]
    public void OptionalIntArray_UnwrapThenSum() =>
        "x:int[]? = [1, 2, 3]\r y = x!.sum()".AssertResultHas("y", 6);

    [Test]
    public void OptionalIntArray_UnwrapThenFilter() =>
        "x:int[]? = [1, 2, 3]\r y = x!.filter(rule it > 1)".AssertResultHas("y", new[] { 2, 3 });

    [Test]
    public void OptionalIntArray_UnwrapThenReverse() =>
        "x:int[]? = [1, 2, 3]\r y = x!.reverse()".AssertResultHas("y", new[] { 3, 2, 1 });

    // --- Struct with multiple optional fields ---

    [Test]
    public void StructMultipleOptionalFields_AllNone() =>
        Assert.DoesNotThrow(() =>
            "y = {a:int? = none; b:text? = none; c:bool? = none}".Build());

    [Test]
    public void StructMultipleOptionalFields_SomeNone() =>
        Assert.DoesNotThrow(() =>
            "y = {a:int? = 42; b:text? = none; c:bool? = true}".Build());

    // --- Optional struct with optional field — double optional ---

    [Test]
    public void OptionalStructOptionalField_ChainCoalesce() =>
        "s:{n:int?}? = {n = 42}\r y = s?.n ?? 0".AssertResultHas("y", 42);

    [Test]
    public void OptionalStructOptionalField_OuterNone() =>
        "s:{n:int?}? = none\r y = s?.n ?? 0".AssertResultHas("y", 0);

    [Test]
    public void OptionalStructOptionalField_InnerNone() =>
        "s:{n:int?} = {n = none}\r y = s.n ?? 0".AssertResultHas("y", 0);

    // --- Array of structs with optional fields ---

    [Test]
    public void ArrayOfStructsWithOptionalField() =>
        Assert.DoesNotThrow(() =>
            "y = [{n:int? = 1}, {n:int? = none}, {n:int? = 3}]".Build());

    // --- Nested arrays with optional ---

    [Test]
    public void NestedOptionalArrays() =>
        Assert.DoesNotThrow(() => "y:int[]?[] = [[1,2], none, [3]]".Build());

    [Test]
    public void OptionalNestedArray() =>
        Assert.DoesNotThrow(() => "y:int[][]? = [[1], [2, 3]]".Build());

    [Test]
    public void OptionalNestedArray_None() {
        var result = "y:int[][]? = none".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // ═══════════════════════════════════════════════════════════════
    // Step 10: Type matrix — coverage for all types (~50 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- T? annotation works for every type ---

    [TestCase("y:byte? = 1", (byte)1)]
    [TestCase("y:int16? = 1", (Int16)1)]
    [TestCase("y:int32? = 1", (Int32)1)]
    [TestCase("y:int? = 1", (Int32)1)]
    [TestCase("y:int64? = 1", (Int64)1)]
    [TestCase("y:uint16? = 1", (UInt16)1)]
    [TestCase("y:uint32? = 1", (UInt32)1)]
    [TestCase("y:uint64? = 1", (UInt64)1)]
    [TestCase("y:real? = 1.0", 1.0)]
    [TestCase("y:text? = 'a'", "a")]
    [TestCase("y:bool? = true", true)]
    public void TypeMatrix_Annotation_Value(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [TestCase("y:char? = /'a'", 'a')]
    public void TypeMatrix_CharAnnotation_Value(string expr, char expected) =>
        expr.AssertReturns("y", expected);

    // --- none ?? T_default returns default for every type ---

    [TestCase("y:byte = none ?? 0", (byte)0)]
    [TestCase("y:int16 = none ?? 0", (Int16)0)]
    [TestCase("y:int32 = none ?? 0", (Int32)0)]
    [TestCase("y:int = none ?? 0", (Int32)0)]
    [TestCase("y:int64 = none ?? 0", (Int64)0)]
    [TestCase("y:uint16 = none ?? 0", (UInt16)0)]
    [TestCase("y:uint32 = none ?? 0", (UInt32)0)]
    [TestCase("y:uint64 = none ?? 0", (UInt64)0)]
    [TestCase("y:real = none ?? 0.0", 0.0)]
    [TestCase("y:text = none ?? ''", "")]
    [TestCase("y:bool = none ?? false", false)]
    public void TypeMatrix_CoalesceDefault(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- T_value! unwrap works for every type ---

    [TestCase("x:byte? = 1\r y = x!", (byte)1)]
    [TestCase("x:int16? = 1\r y = x!", (Int16)1)]
    [TestCase("x:int32? = 1\r y = x!", (Int32)1)]
    [TestCase("x:int? = 1\r y = x!", (Int32)1)]
    [TestCase("x:int64? = 1\r y = x!", (Int64)1)]
    [TestCase("x:uint16? = 1\r y = x!", (UInt16)1)]
    [TestCase("x:uint32? = 1\r y = x!", (UInt32)1)]
    [TestCase("x:uint64? = 1\r y = x!", (UInt64)1)]
    [TestCase("x:real? = 1.0\r y = x!", 1.0)]
    [TestCase("x:text? = 'a'\r y = x!", "a")]
    [TestCase("x:bool? = true\r y = x!", true)]
    public void TypeMatrix_ForceUnwrap(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    [TestCase("x:char? = /'a'\r y = x!", 'a')]
    public void TypeMatrix_CharForceUnwrap(string expr, char expected) =>
        expr.AssertResultHas("y", expected);

    // --- x == none for every type ---

    [TestCase("x:byte? = 1\r y = x == none", false)]
    [TestCase("x:byte? = none\r y = x == none", true)]
    [TestCase("x:int16? = 1\r y = x == none", false)]
    [TestCase("x:int16? = none\r y = x == none", true)]
    [TestCase("x:int32? = 1\r y = x == none", false)]
    [TestCase("x:int32? = none\r y = x == none", true)]
    [TestCase("x:int64? = 1\r y = x == none", false)]
    [TestCase("x:int64? = none\r y = x == none", true)]
    [TestCase("x:uint16? = 1\r y = x == none", false)]
    [TestCase("x:uint16? = none\r y = x == none", true)]
    [TestCase("x:uint32? = 1\r y = x == none", false)]
    [TestCase("x:uint32? = none\r y = x == none", true)]
    [TestCase("x:uint64? = 1\r y = x == none", false)]
    [TestCase("x:uint64? = none\r y = x == none", true)]
    [TestCase("x:real? = 1.0\r y = x == none", false)]
    [TestCase("x:real? = none\r y = x == none", true)]
    [TestCase("x:text? = 'a'\r y = x == none", false)]
    [TestCase("x:text? = none\r y = x == none", true)]
    [TestCase("x:bool? = true\r y = x == none", false)]
    [TestCase("x:bool? = none\r y = x == none", true)]
    [TestCase("x:char? = /'a'\r y = x == none", false)]
    [TestCase("x:char? = none\r y = x == none", true)]
    public void TypeMatrix_EqualsNone(string expr, bool expected) =>
        expr.AssertResultHas("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Step 11: Integration with existing features (~50 tests)
    // ═══════════════════════════════════════════════════════════════

    // --- Optional + implicit cast ---

    [TestCase("y:real? = 42", 42.0)]
    [TestCase("y:int64? = 42", (Int64)42)]
    [TestCase("y:real? = 0xFF", 255.0)]
    public void Optional_ImplicitCast(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Optional + comparison chains → not supported ---

    [TestCase("x:int?\r y = 1 < x < 10")]
    [TestCase("x:int?\r y = 0 <= x <= 100")]
    public void Optional_ComparisonChain_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Optional + pipe forward ---

    [TestCase("f(x:int?):int = x ?? 0\r y = none.f()", 0)]
    public void Optional_PipeForward_NoneValue(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Optional + string interpolation ---

    [TestCase("x:int? = 42\r y = 'val: {x}'", "val: 42")]
    [TestCase("x:int? = none\r y = 'val: {x}'", "val: none")]
    [TestCase("x:text? = 'hi'\r y = 'val: {x}'", "val: hi")]
    [TestCase("x:text? = none\r y = 'val: {x}'", "val: none")]
    public void Optional_InStringInterpolation(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Optional + toText ---

    [TestCase("x:int? = 42\r y = toText(x!)", "42")]
    [TestCase("x:real? = 1.5\r y = toText(x!)", "1.5")]
    public void Optional_ToText_AfterUnwrap(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- toText on optional directly should fail ---

    [TestCase("x:int?\r y = toText(x)")]
    [TestCase("x:real?\r y = toText(x)")]
    public void Optional_ToText_DirectlyOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse();

    // --- Optional + default ---

    [Test]
    public void Optional_Default_ReturnsNone() {
        var result = "y:int? = default".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void Optional_RealDefault_ReturnsNone() {
        var result = "y:real? = default".Calc();
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void Optional_TextDefault_ReturnsNone() {
        var result = "y:text? = default".Calc();
        Assert.IsNull(result.Get("y"));
    }

    // --- Complex / stress expressions ---

    [TestCase("a:int? = 5\r b:int? = none\r y = (a ?? 0) + (b ?? 10)", 15)]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? 1) + (b ?? 2)", 3)]
    [TestCase("a:int? = 3\r b:int? = 4\r y = (a ?? 0) * (b ?? 0)", 12)]
    public void Stress_ComplexOptionalExpressions(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    [Test]
    public void Stress_NestedOptionalChaining() =>
        Assert.DoesNotThrow(() =>
            "x:{a:{b:{c:int?}?}?}? = {a = {b = {c = 42}}}\r y = x?.a?.b?.c".Build());

    [Test]
    public void Stress_NestedCoalesce_WithFunctions() =>
        "f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? f(-2) ?? f(3) ?? 0"
            .AssertReturns("y", 3);

    [Test]
    public void Stress_OptionalInArrayMap() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x > 0) x else none\r y = [1,-2,3].map(f)".Build());

    [Test]
    public void Stress_IfElseChainWithOptional() =>
        "x:int? = 5\r y = if(x != none) x! * 2 else -1".AssertResultHas("y", 10);

    [Test]
    public void Stress_IfElseChainWithOptional_None() =>
        "x:int? = none\r y = if(x != none) x! * 2 else -1".AssertResultHas("y", -1);

    [Test]
    public void Stress_CoalesceInIfCondition() =>
        "x:int? = none\r y = if((x ?? 0) > 0) 'positive' else 'non-positive'"
            .AssertResultHas("y", "non-positive");

    [Test]
    public void Stress_CoalesceInIfCondition_HasValue() =>
        "x:int? = 5\r y = if((x ?? 0) > 0) 'positive' else 'non-positive'"
            .AssertResultHas("y", "positive");

    [Test]
    public void Stress_OptionalArrayFilter() =>
        Assert.DoesNotThrow(() =>
            "y = [1,2,3].map(rule if(it > 1) it else none)".Build());

    [Test]
    public void Stress_MultipleOutputsWithOptional() =>
        Assert.DoesNotThrow(() =>
            "a:int? = 42\r b:int? = none\r y = a ?? 0\r z = b ?? -1".Build());

    [TestCase("x:int? = 42\r y = x ?? 0\r z = y + 1", 43)]
    public void Stress_OptionalThenArithmetic(string expr, object expected) =>
        expr.AssertResultHas("z", expected);

    [Test]
    public void Stress_OptionalStructFieldCoalesce() =>
        "s:{num:int?} = {num = none}\r y = s.num ?? 99".AssertResultHas("y", 99);

    [Test]
    public void Stress_OptionalStructFieldCoalesce_HasValue() =>
        "s:{num:int?} = {num = 42}\r y = s.num ?? 99".AssertResultHas("y", 42);

    // --- Optional + array indexing after coalesce ---

    [Test]
    public void Integration_CoalesceThenIndex() =>
        "x:int[]? = [10, 20]\r y = (x ?? [0, 0])[1]".AssertResultHas("y", 20);

    [Test]
    public void Integration_CoalesceThenIndex_None() =>
        "x:int[]? = none\r y = (x ?? [0, 0])[1]".AssertResultHas("y", 0);

    // --- Optional + count after unwrap ---

    [Test]
    public void Integration_UnwrapThenCount() =>
        "x:text? = 'hello'\r y = x!.count()".AssertResultHas("y", 5);

    // --- Optional in multi-equation ---

    [Test]
    public void Integration_MultiEquation_OptionalAndRegular() =>
        "a:int? = 42\r b = 10\r y = (a ?? 0) + b".AssertResultHas("y", 52);

    [Test]
    public void Integration_MultiEquation_TwoOptionals() =>
        "a:int? = 5\r b:int? = 3\r y = (a ?? 0) + (b ?? 0)".AssertResultHas("y", 8);

    // --- Optional with negative values ---

    [TestCase("y:int? = -42", -42)]
    [TestCase("y:int? = -1", -1)]
    [TestCase("y:real? = -1.5", -1.5)]
    public void Optional_NegativeValues(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Optional + zero values ---

    [TestCase("y:int? = 0", 0)]
    [TestCase("y:real? = 0.0", 0.0)]
    [TestCase("y:byte? = 0", (byte)0)]
    public void Optional_ZeroValues(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Optional + large values ---

    [TestCase("y:int64? = 9223372036854775807", Int64.MaxValue)]
    [TestCase("y:uint64? = 18446744073709551615", UInt64.MaxValue)]
    public void Optional_LargeValues(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Optional + toText on unwrapped ---

    [TestCase("x:int? = 42\r y = '{x!}'", "42")]
    [TestCase("x:bool? = true\r y = '{x!}'", "True")]
    public void Optional_InterpolationWithUnwrap(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════════
    // Step 12: Complex operator combinations — ??, !, ?., (), chains
    // ═══════════════════════════════════════════════════════════════

    // --- ?? with ! in same expression ---

    [TestCase("a:int? = 5\r b:int? = none\r y = (a ?? b!)", 5)]
    [TestCase("a:int? = none\r b:int? = 7\r y = a ?? b!", 7)]
    [TestCase("x:int? = 42\r y = (x ?? 0)!", 42)]
    public void Combo_CoalesceAndUnwrap(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    [Test]
    public void Combo_CoalesceResultUnwrap_None_RuntimeError() =>
        "a:int? = none\r b:int? = none\r y = (a ?? b)!".AssertObviousFailsOnRuntime();

    // --- ?. then ?? then ! ---

    [Test]
    public void Combo_ChainingCoalesceUnwrap_HasValue() =>
        "s:{inner:{num:int?}?}? = {inner = {num = 42}}\r y = (s?.inner?.num ?? 0)"
            .AssertResultHas("y", 42);

    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneOuter() =>
        "s:{inner:{num:int?}?}? = none\r y = (s?.inner?.num ?? 0)"
            .AssertResultHas("y", 0);

    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneInner() =>
        "s:{inner:{num:int?}?} = {inner = none}\r y = (s.inner?.num ?? 0)"
            .AssertResultHas("y", 0);

    [Test]
    public void Combo_ChainingCoalesceUnwrap_NoneLeaf() =>
        "s:{inner:{num:int?}?} = {inner = {num = none}}\r y = (s.inner?.num ?? 0)"
            .AssertResultHas("y", 0);

    // --- Parenthesized ?? expressions ---

    [TestCase("a:int? = none\r y = (a ?? 1) + (a ?? 2)", 3)]
    [TestCase("a:int? = 5\r y = (a ?? 1) + (a ?? 2)", 10)]
    [TestCase("a:int? = 3\r b:int? = 4\r y = (a ?? 0) + (b ?? 0)", 7)]
    [TestCase("a:int? = none\r b:int? = 4\r y = (a ?? 0) + (b ?? 0)", 4)]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? 0) + (b ?? 0)", 0)]
    [TestCase("a:int? = 2\r b:int? = 3\r y = (a ?? 0) * (b ?? 1)", 6)]
    [TestCase("a:int? = none\r b:int? = 3\r y = (a ?? 1) * (b ?? 1)", 3)]
    [TestCase("a:real? = 2.0\r b:real? = 3.0\r y = (a ?? 0.0) + (b ?? 0.0)", 5.0)]
    [TestCase("a:real? = none\r b:real? = 1.5\r y = (a ?? 0.0) + (b ?? 0.0)", 1.5)]
    public void Combo_ParenthesizedCoalesceInArithmetic(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ! then arithmetic ---

    [TestCase("x:int? = 5\r y = x! + x!", 10)]
    [TestCase("x:int? = 3\r y = x! * x!", 9)]
    [TestCase("x:int? = 10\r y = x! - 3", 7)]
    [TestCase("x:int? = 10\r y = x! / 2", 5.0)]
    [TestCase("x:int? = 7\r y = x! % 3", 1)]
    [TestCase("x:real? = 2.5\r y = x! + 1.5", 4.0)]
    [TestCase("x:real? = 4.0\r y = x! * x!", 16.0)]
    public void Combo_UnwrapThenArithmetic(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Nested ?? with parentheses ---

    [TestCase("a:int? = none\r b:int? = none\r c:int? = 42\r y = a ?? (b ?? (c ?? 0))", 42)]
    [TestCase("a:int? = none\r b:int? = 7\r c:int? = 42\r y = a ?? (b ?? (c ?? 0))", 7)]
    [TestCase("a:int? = 1\r b:int? = 7\r c:int? = 42\r y = a ?? (b ?? (c ?? 0))", 1)]
    [TestCase("a:int? = none\r b:int? = none\r c:int? = none\r y = a ?? (b ?? (c ?? 0))", 0)]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? b) ?? 0", 0)]
    [TestCase("a:int? = none\r b:int? = 5\r y = (a ?? b) ?? 0", 5)]
    [TestCase("a:int? = 3\r b:int? = 5\r y = (a ?? b) ?? 0", 3)]
    public void Combo_NestedParenthesizedCoalesce(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

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
        "s:{num:int?}? = {num = 10}\r y = (s?.num ?? 0)".AssertResultHas("y", 10);

    [Test]
    public void Combo_ChainUnwrapArithmetic() =>
        "s:{num:int}? = {num = 5}\r y = s!.num + 1".AssertResultHas("y", 6);

    [Test]
    public void Combo_ChainUnwrapArithmetic_None_RuntimeError() =>
        "s:{num:int}? = none\r y = s!.num + 1".AssertObviousFailsOnRuntime();

    // --- Complex boolean expressions with optionals ---

    [TestCase("x:int? = 42\r y:int? = none\r z = (x != none) and (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x != none) and (y == none)", false)]
    [TestCase("x:int? = none\r y:int? = none\r z = (x == none) and (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x != none) and (y != none)", true)]
    [TestCase("x:int? = none\r y:int? = 10\r z = (x == none) or (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x == none) or (y == none)", false)]
    public void Combo_BooleanWithNoneChecks(string expr, bool expected) =>
        expr.AssertResultHas("z", expected);

    // --- Conditional unwrap: if(x != none) x! else ... ---

    [TestCase("x:int? = 42\r y = if(x != none) x! + 10 else -1", 52)]
    [TestCase("x:int? = none\r y = if(x != none) x! + 10 else -1", -1)]
    [TestCase("x:int? = 5\r y = if(x != none) x! * x! else 0", 25)]
    [TestCase("x:int? = none\r y = if(x != none) x! * x! else 0", 0)]
    [TestCase("x:real? = 2.5\r y = if(x != none) x! * 2.0 else 0.0", 5.0)]
    [TestCase("x:real? = none\r y = if(x != none) x! * 2.0 else 0.0", 0.0)]
    public void Combo_ConditionalUnwrapWithArithmetic(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Multiple optionals in single expression ---

    [TestCase("a:int? = 1\r b:int? = 2\r c:int? = 3\r y = a! + b! + c!", 6)]
    [TestCase("a:int? = 10\r b:int? = none\r y = a! + (b ?? 5)", 15)]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? 1) + (b ?? 2)", 3)]
    [TestCase("a:int? = 3\r b:int? = none\r y = (a ?? 0) * 2 + (b ?? 1)", 7)]
    public void Combo_MultipleOptionalsInExpr(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- ?? with function call results ---

    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = (f(5) ?? 0) + (f(-1) ?? 10)", 15)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = (f(3) ?? 0) * (f(4) ?? 1)", 12)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5)! + f(3)!", 8)]
    public void Combo_FunctionResultCoalesceArithmetic(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    [Test]
    public void Combo_FunctionResultUnwrap_RuntimeError() =>
        "f(x:int):int? = if(x > 0) x else none\r y = f(-1)!"
            .AssertObviousFailsOnRuntime();

    // --- ?. with ?? with arithmetic ---

    [Test]
    public void Combo_ChainingCoalesceArithmetic() =>
        "s:{num:int}? = {num = 5}\r y = (s?.num ?? 0) + 10"
            .AssertResultHas("y", 15);

    [Test]
    public void Combo_ChainingCoalesceArithmetic_None() =>
        "s:{num:int}? = none\r y = (s?.num ?? 0) + 10"
            .AssertResultHas("y", 10);

    [Test]
    public void Combo_ChainingCoalesceMultiply() =>
        "s:{num:int}? = {num = 3}\r y = (s?.num ?? 1) * 2"
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

    [Test]
    public void Combo_UnwrapArrayThenIndex() =>
        "x:int[]? = [10,20,30]\r y = x![1] + 5".AssertResultHas("y", 25);

    // --- ?? with if-else containing none ---

    [TestCase("x:int? = 42\r y = (if(true) x else none) ?? 0", 42)]
    [TestCase("x:int? = none\r y = (if(true) x else none) ?? 0", 0)]
    [TestCase("x:int? = 42\r y = (if(false) x else none) ?? 0", 0)]
    [TestCase("y = (if(true) 42 else none) ?? 0", 42)]
    [TestCase("y = (if(false) 42 else none) ?? 0", 0)]
    [TestCase("y = (if(true) none else 42) ?? 99", 99)]
    public void Combo_IfElseWrappedInCoalesce(string expr, object expected) =>
        expr.AssertResultHas("y", expected);

    // --- Chained function calls with optionals ---

    [TestCase("f(x:int?):int? = x\r g(x:int?):int = x ?? 0\r y = g(f(42))", 42)]
    [TestCase("f(x:int?):int? = x\r g(x:int?):int = x ?? 0\r y = g(f(none))", 0)]
    [TestCase("f(x:int):int? = if(x>0) x else none\r g(x:int?):int = x ?? -1\r y = g(f(5))", 5)]
    [TestCase("f(x:int):int? = if(x>0) x else none\r g(x:int?):int = x ?? -1\r y = g(f(-1))", -1)]
    public void Combo_ChainedFunctionCalls(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // --- Pipe with optional unwrap ---

    [Test]
    public void Combo_PipeUnwrapArithmetic() =>
        Assert.DoesNotThrow(() =>
            "f(x:int?):int = x ?? 0\r x:int? = 42\r y = x.f() + 1".Build());

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

    // --- ?? with comparisons after coalesce ---

    [TestCase("x:int? = 5\r y = (x ?? 0) > 3", true)]
    [TestCase("x:int? = 1\r y = (x ?? 0) > 3", false)]
    [TestCase("x:int? = none\r y = (x ?? 0) > 3", false)]
    [TestCase("x:int? = 5\r y = (x ?? 0) == 5", true)]
    [TestCase("x:int? = none\r y = (x ?? 0) == 0", true)]
    [TestCase("x:int? = 5\r y = (x ?? 0) >= 5", true)]
    [TestCase("x:int? = 5\r y = (x ?? 0) < 10", true)]
    public void Combo_CoalesceThenComparison(string expr, bool expected) =>
        expr.AssertResultHas("y", expected);

    // --- Triple operator combo: ?. ?? ! ---

    [Test]
    public void Combo_TripleOperator_ChainCoalesceFallbackUnwrap() =>
        @"s:{num:int}? = {num = 42}
          fallback:int? = 99
          y = s?.num ?? fallback!"
            .AssertResultHas("y", 42);

    [Test]
    public void Combo_TripleOperator_ChainNone_CoalesceFallbackUnwrap() =>
        @"s:{num:int}? = none
          fallback:int? = 99
          y = s?.num ?? fallback!"
            .AssertResultHas("y", 99);

    [Test]
    public void Combo_TripleOperator_ChainNone_FallbackNone_RuntimeError() =>
        @"s:{num:int}? = none
          fallback:int? = none
          y = s?.num ?? fallback!"
            .AssertObviousFailsOnRuntime();

    // --- Negative combos: optional in wrong places ---

    [TestCase("x:int?\r y = (x) + 1")]
    [TestCase("x:int?\r y = -(x)")]
    [TestCase("x:int?\r a:int?\r y = x + a")]
    [TestCase("x:int?\r y = x * x")]
    [TestCase("x:int?\r y = x > x")]
    [TestCase("x:bool?\r y = x and x")]
    [TestCase("x:int?\r y = x & x")]
    public void Combo_Negative_OptionalInArithmetic(string expr) =>
        expr.AssertObviousFailsOnParse();
}
