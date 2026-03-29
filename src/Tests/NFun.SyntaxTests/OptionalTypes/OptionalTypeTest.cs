namespace NFun.SyntaxTests.OptionalTypes;

using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalTypeTest {

    [Test]
    public void NoneLiteral_Standalone() =>
        Assert.DoesNotThrow(() => "y = none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void NoneLiteral_BothBranchesNone() =>
        Assert.DoesNotThrow(() => "y = if(true) none else none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

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
        Assert.DoesNotThrow(() => expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));

    [Test]
    public void OptionalInt_AssignNone_ReturnsNull() {
        var result = "y:int? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalReal_AssignNone_ReturnsNull() {
        var result = "y:real? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalText_AssignNone_ReturnsNull() {
        var result = "y:text? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalBool_AssignNone_ReturnsNull() {
        var result = "y:bool? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalChar_AssignNone_ReturnsNull() {
        var result = "y:char? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalByte_AssignNone_ReturnsNull() {
        var result = "y:byte? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalInt16_AssignNone_ReturnsNull() {
        var result = "y:int16? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalInt64_AssignNone_ReturnsNull() {
        var result = "y:int64? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalUint16_AssignNone_ReturnsNull() {
        var result = "y:uint16? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalUint32_AssignNone_ReturnsNull() {
        var result = "y:uint32? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalUint64_AssignNone_ReturnsNull() {
        var result = "y:uint64? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void OptionalAny_AssignNone_ReturnsNull() {
        var result = "y:any? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("y:char? = /'a'", 'a')]
    [TestCase("y:char? = /'z'", 'z')]
    [TestCase("y:char? = /'0'", '0')]
    public void OptionalChar_AssignValue(string expr, char expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("y:any? = 42", 42)]
    [TestCase("y:any? = 'hello'", "hello")]
    [TestCase("y:any? = true", true)]
    [TestCase("y:any? = 1.5", 1.5)]
    public void OptionalAny_AssignValue(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("y:int32? = 0xFF", (Int32)255)]
    [TestCase("y:int64? = 0xFF", (Int64)255)]
    [TestCase("y:real? = 42", 42.0)]
    [TestCase("y:int64? = 42", (Int64)42)]
    public void OptionalAnnotation_ImplicitUpcast(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [Test]
    public void ArrayOfOptionalInts_WithValues() =>
        "y:int?[] = [1, 2, 3]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", new int?[] { 1, 2, 3 });


    [Test]
    public void ArrayOfOptionalInts_WithNone() =>
        Assert.DoesNotThrow(() => "y:int?[] = [1, none, 3]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalReals_WithNone() =>
        Assert.DoesNotThrow(() => "y:real?[] = [1.0, none, 3.0]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalTexts_WithNone() =>
        Assert.DoesNotThrow(() => "y:text?[] = ['hello', none]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalBools_WithNone() =>
        Assert.DoesNotThrow(() => "y:bool?[] = [true, none, false]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalInts_NoneIsNull() {
        var result = "y:int?[] = [1, none, 3]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var arr = (int?[])result.Get("y");
        Assert.AreEqual(1, arr[0]);
        Assert.IsNull(arr[1], "none should be null, not 0");
        Assert.AreEqual(3, arr[2]);
    }


    [Test]
    public void ArrayOfOptionalBools_NoneIsNull() {
        var result = "y:bool?[] = [true, none, false]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var arr = (bool?[])result.Get("y");
        Assert.AreEqual(true, arr[0]);
        Assert.IsNull(arr[1], "none should be null, not false");
        Assert.AreEqual(false, arr[2]);
    }


    [Test]
    public void OptionalArrayOfInts_WithValue() =>
        "y:int[]? = [1, 2, 3]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", new[] { 1, 2, 3 });


    [Test]
    public void OptionalArrayOfInts_WithNone() {
        var result = "y:int[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalArrayOfReals_WithNone() {
        var result = "y:real[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalArrayOfTexts_WithNone() {
        var result = "y:text[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalArrayOfOptionalInts_WithValue() =>
        Assert.DoesNotThrow(() => "y:int?[]? = [1, none, 3]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalArrayOfOptionalInts_WithNone() {
        var result = "y:int?[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void MultipleOptionalVars_BothNone() =>
        Assert.DoesNotThrow(() => "a:int? = none\r b:int? = none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void MultipleOptionalVars_MixedNoneValue() =>
        Assert.DoesNotThrow(() => "a:int? = 42\r b:int? = none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void MultipleOptionalVars_BothValues() =>
        "a:int? = 1\r b:int? = 2\r y = a! + b!".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 3);


    [TestCase("a:int? = 42\r b:int? = a", 42)]
    [TestCase("a:real? = 1.5\r b:real? = a", 1.5)]
    [TestCase("a:text? = 'hi'\r b:text? = a", "hi")]
    [TestCase("a:bool? = true\r b:bool? = a", true)]
    public void OptionalAssignedFromOptional(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("b", expected);


    [Test]
    public void OptionalAssignedFromOptional_None() {
        var result = "a:int? = none\r b:int? = a".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("b"));
    }


    [Test]
    public void MultipleOutputs_OptionalAndNonOptional() =>
        "y:int? = 42\r z:int = 10".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns(("y", (object)42), ("z", (object)10));


    [Test]
    public void ArrayOfOptionalChars_WithNone() =>
        Assert.DoesNotThrow(() => "y:char?[] = [/'a', none, /'c']".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalChars_WithValues() =>
        Assert.DoesNotThrow(() => "y:char?[] = [/'x', /'y']".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalBools_WithValues() =>
        Assert.DoesNotThrow(() => "y:bool?[] = [true, false]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalArrayOfReals_WithValue() =>
        "y:real[]? = [1.0, 2.0, 3.0]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", new[] { 1.0, 2.0, 3.0 });


    [Test]
    public void ArrayOfOptionalTexts_WithValues() =>
        Assert.DoesNotThrow(() => "y:text?[] = ['hello', 'world']".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [TestCase("y:int? = 0xFF", (Int32)255)]
    [TestCase("y:int? = 0b1010", (Int32)10)]
    [TestCase("y:byte? = 0xFF", (byte)255)]
    [TestCase("y:int64? = 0xFFFF", (Int64)0xFFFF)]
    public void OptionalAnnotation_HexBinaryLiterals(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


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
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("y = none ?? /'a'", 'a')]
    public void CoalesceOperator_NoneWithDefaultChar(string expr, char expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: new[]{("x", (object)input)}).AssertResultHas("y", expected);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("y = none ?? none ?? 42", 42)]
    [TestCase("y = none ?? none ?? none ?? 1", 1)]
    [TestCase("y = none ?? 10 ?? 20", 10)]
    [TestCase("a:int? = none\r b:int? = none\r y = a ?? b ?? 0", 0)]
    [TestCase("a:int? = none\r b:int? = 5\r y = a ?? b ?? 0", 5)]
    [TestCase("a:int? = 3\r b:int? = 5\r y = a ?? b ?? 0", 3)]
    [TestCase("a:int? = none\r b:int? = none\r c:int? = 7\r y = a ?? b ?? c ?? 0", 7)]
    public void CoalesceOperator_Chain(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:int? = none\r y = x ?? (1 + 2)", 3)]
    [TestCase("x:int? = none\r y = x ?? (10 * 2)", 20)]
    [TestCase("x:int? = 5\r y = x ?? (1 + 2)", 5)]
    [TestCase("x:real? = none\r y = x ?? (1.0 + 2.5)", 3.5)]
    public void CoalesceOperator_WithExpressionDefault(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [Test]
    public void CoalesceOperator_OptionalArrayWithDefault() =>
        "x:int[]? = none\r y = x ?? [1,2,3]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", new[] { 1, 2, 3 });


    [Test]
    public void CoalesceOperator_OptionalArrayHasValue() =>
        "x:int[]? = [4,5]\r y = x ?? [1,2,3]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", new[] { 4, 5 });


    [Test]
    public void CoalesceOperator_ResultIsNotOptional() {
        var runtime = "x:int? = none\r y = x ?? 0".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        runtime.Calc().AssertResultHas("y", 0);
    }


    [TestCase("x:int? = none\r y = x ?? 1.5", 1.5)]
    [TestCase("x:int? = 3\r y = x ?? 1.5", 3.0)]
    [TestCase("x:byte? = none\r y = x ?? 256", 256)]
    public void CoalesceOperator_LcaWidening(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase((byte)10, "x:byte?\r y = x ?? 0", (byte)10)]
    [TestCase((Int16)10, "x:int16?\r y = x ?? 0", (Int16)10)]
    [TestCase((Int32)10, "x:int32?\r y = x ?? 0", (Int32)10)]
    [TestCase((Int64)10, "x:int64?\r y = x ?? 0", (Int64)10)]
    [TestCase((UInt16)10, "x:uint16?\r y = x ?? 0", (UInt16)10)]
    [TestCase((UInt32)10, "x:uint32?\r y = x ?? 0", (UInt32)10)]
    [TestCase((UInt64)10, "x:uint64?\r y = x ?? 0", (UInt64)10)]
    [TestCase(3.14, "x:real?\r y = x ?? 0.0", 3.14)]
    public void CoalesceOperator_EachNumericType_HasValue(object input, string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: new[]{("x", (object)input)}).AssertResultHas("y", expected);


    [Test]
    public void CoalesceOperator_ArrayElement_HasValue() =>
        "arr:int?[] = [10, none, 30]\r y = arr[0] ?? -1".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 10);


    [Test]
    public void CoalesceOperator_ArrayElement_None() =>
        "arr:int?[] = [10, none, 30]\r y = arr[1] ?? -1".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", -1);


    [TestCase("f():int? = none\r y = f() ?? 42", 42)]
    [TestCase("f():int? = 10\r y = f() ?? 42", 10)]
    [TestCase("f():text? = none\r y = f() ?? 'default'", "default")]
    [TestCase("f():text? = 'hello'\r y = f() ?? 'default'", "hello")]
    [TestCase("f():bool? = none\r y = f() ?? true", true)]
    [TestCase("f():real? = none\r y = f() ?? 3.14", 3.14)]
    public void CoalesceOperator_FunctionResult(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:byte? = none\r y = x ?? 1000", 1000)]
    [TestCase("x:int16? = none\r y = x ?? 1.5", 1.5)]
    [TestCase("x:byte? = 10\r y = x ?? 1.5", 10.0)]
    [TestCase("x:int16? = 5\r y = x ?? 100000", 5)]
    public void CoalesceOperator_LcaMoreCases(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:text? = none\r y = x ?? ''", "")]
    [TestCase("x:text? = 'abc'\r y = x ?? ''", "abc")]
    [TestCase("x:text? = none\r y = x ?? 'a'", "a")]
    public void CoalesceOperator_TextPreserved(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:bool? = none\r y = x ?? true", true)]
    [TestCase("x:bool? = none\r y = x ?? false", false)]
    [TestCase("x:bool? = true\r y = x ?? false", true)]
    [TestCase("x:bool? = false\r y = x ?? true", false)]
    public void CoalesceOperator_BoolVariants(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:int? = 5\r y = (x ?? 0) + 10", 15)]
    [TestCase("x:int? = none\r y = (x ?? 0) + 10", 10)]
    [TestCase("x:int? = 3\r y = (x ?? 1) * (x ?? 1)", 9)]
    [TestCase("x:int? = none\r y = (x ?? 2) * (x ?? 3)", 6)]
    [TestCase("x:real? = 2.0\r y = (x ?? 0.0) / 2.0", 1.0)]
    public void CoalesceOperator_ResultInExpression(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [Test]
    public void CoalesceOperator_OptionalTextArray() =>
        Assert.DoesNotThrow(() => "x:text[]? = none\r y = x ?? ['default']".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void CoalesceOperator_OptionalRealArray() =>
        "x:real[]? = none\r y = x ?? [1.0, 2.0]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", new[] { 1.0, 2.0 });


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    [TestCase("y:char? = /'a'\r z = y!", 'a')]
    public void ForceUnwrap_Char_HasValue(string expr, char expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    [TestCase("y:byte? = 1\r z = y!", (byte)1)]
    [TestCase("y:int16? = 1\r z = y!", (Int16)1)]
    [TestCase("y:int32? = 1\r z = y!", (Int32)1)]
    [TestCase("y:int64? = 1\r z = y!", (Int64)1)]
    [TestCase("y:uint16? = 1\r z = y!", (UInt16)1)]
    [TestCase("y:uint32? = 1\r z = y!", (UInt32)1)]
    [TestCase("y:uint64? = 1\r z = y!", (UInt64)1)]
    public void ForceUnwrap_EachNumericType(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


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
        expr.AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [TestCase("x:int? = 5\r y = x! + 1", 6)]
    [TestCase("x:int? = 5\r y = x! - 1", 4)]
    [TestCase("x:int? = 5\r y = x! * 2", 10)]
    [TestCase("x:real? = 10.0\r y = x! / 2.0", 5.0)]
    [TestCase("x:int? = 7\r y = x! % 3", 1)]
    [TestCase("x:int? = 2\r y = x! ** 3", 8)]
    public void ForceUnwrap_InArithmeticExpr(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [Test]
    public void ForceUnwrap_ResultIsNonOptional() =>
        Assert.DoesNotThrow(() => "x:int? = 42\r y:int = x!".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ForceUnwrap_OptionalArray_IndexAccess() =>
        "x:int[]? = [10,20,30]\r y = x![0]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 10);


    [Test]
    public void ForceUnwrap_OptionalArray_None_RuntimeError() =>
        "x:int[]? = none\r y = x![0]".AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [TestCase("a:int? = 3\r b:int? = 4\r y = a! + b!", 7)]
    [TestCase("a:int? = 3\r b:int? = 4\r y = a! * b!", 12)]
    [TestCase("a:real? = 2.0\r b:real? = 3.0\r y = a! + b!", 5.0)]
    [TestCase("a:real? = 2.0\r b:real? = 3.0\r y = a! * b!", 6.0)]
    public void ForceUnwrap_TwoUnwrapsInExpr(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("f():int? = 42\r y = f()!", 42)]
    [TestCase("f():real? = 1.5\r y = f()!", 1.5)]
    [TestCase("f():text? = 'hi'\r y = f()!", "hi")]
    [TestCase("f():bool? = true\r y = f()!", true)]
    public void ForceUnwrap_FunctionResult(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("f():int? = none\r y = f()!")]
    [TestCase("f():text? = none\r y = f()!")]
    public void ForceUnwrap_FunctionResult_None_RuntimeError(string expr) =>
        expr.AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [Test]
    public void ForceUnwrap_OptionalArray_Count() =>
        "x:int[]? = [1,2,3]\r y = x!.count()".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 3);


    [Test]
    public void ForceUnwrap_OptionalArray_Map() =>
        "x:int[]? = [1,2,3]\r y = x!.map(rule it * 2)".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", new[] { 2, 4, 6 });


    [Test]
    public void ForceUnwrap_OptionalText_Count() =>
        "x:text? = 'abc'\r y = x!.count()".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 3);


    [TestCase("x:int? = 5\r y = x! > 3", true)]
    [TestCase("x:int? = 1\r y = x! > 3", false)]
    [TestCase("x:int? = 5\r y = x! == 5", true)]
    [TestCase("x:int? = 5\r y = x! != 5", false)]
    [TestCase("x:int? = 5\r y = x! >= 5", true)]
    [TestCase("x:int? = 5\r y = x! <= 5", true)]
    public void ForceUnwrap_InComparison(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


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


    [TestCase("y = if(true) 42 else none", 42)]
    [TestCase("y = if(true) 1 else none", 1)]
    [TestCase("y = if(true) 0 else none", 0)]
    [TestCase("y = if(true) -1 else none", -1)]
    [TestCase("y = if(true) 1.5 else none", 1.5)]
    [TestCase("y = if(true) 'hello' else none", "hello")]
    [TestCase("y = if(true) true else none", true)]
    public void IfElse_ValueElseNone_TrueBranch(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("y = if(true) /'a' else none", 'a')]
    public void IfElse_CharElseNone_TrueBranch(string expr, char expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);

    [Test]
    public void IfElse_IntElseNone_FalseBranch() {
        var result = "y = if(false) 42 else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void IfElse_RealElseNone_FalseBranch() {
        var result = "y = if(false) 1.5 else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void IfElse_TextElseNone_FalseBranch() {
        var result = "y = if(false) 'hi' else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void IfElse_BoolElseNone_FalseBranch() {
        var result = "y = if(false) true else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void IfElse_CharElseNone_FalseBranch() {
        var result = "y = if(false) /'a' else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [TestCase("y = if(false) none else 42", 42)]
    [TestCase("y = if(false) none else 1.5", 1.5)]
    [TestCase("y = if(false) none else 'hello'", "hello")]
    [TestCase("y = if(false) none else true", true)]
    [TestCase("y = if(false) none else false", false)]
    public void IfElse_NoneElseValue_FalseBranch(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [Test]
    public void IfElse_NoneElseValue_TrueBranch() {
        var result = "y = if(true) none else 42".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }

    [Test]
    public void IfElse_BothNone() {
        var result = "y = if(true) none else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [TestCase("x:bool\r y = if(x) 0xFF else none", true, 255)]
    [TestCase("x:bool\r y = if(x) 0xFF else none", false, null)]
    public void IfElse_ByteElseNone(string expr, object input, object expected) {
        if (expected == null) {
            var result = expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: new[]{("x", (object)input)});
            Assert.IsNull(result.Get("y"));
        } else {
            expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: new[]{("x", (object)input)}).AssertResultHas("y", expected);
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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("x:int? = 42\r y = if(true) x else 1.0", 42.0)]
    [TestCase("x:int? = 42\r z:int? = 10\r y = if(true) x else z", 42)]
    [TestCase("x:int? = 42\r z:real? = 1.5\r y = if(true) x else z", 42.0)]
    public void IfElse_LcaWithOptionals(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [Test]
    public void IfElse_IntOptionalElseReal_IsRealOptional() =>
        Assert.DoesNotThrow(() => "x:int? = 42\r y:real? = if(true) x else 1.0".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void IfElse_IntOptionalElseIntOptional_IsIntOptional() =>
        Assert.DoesNotThrow(() => "x:int? = 42\r z:int? = 10\r y:int? = if(true) x else z".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void IfElse_IntOptionalElseRealOptional_IsRealOptional() =>
        Assert.DoesNotThrow(() =>
            "x:int? = 42\r z:real? = 1.5\r y:real? = if(true) x else z".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void IfElse_IntElseRealOptional_IsRealOptional() =>
        Assert.DoesNotThrow(() =>
            "z:real? = 1.5\r y:real? = if(true) 42 else z".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void IfElse_ArrayElseNone_TrueBranch() =>
        "y = if(true) [1,2] else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", new[] { 1, 2 });


    [Test]
    public void IfElse_ArrayElseNone_FalseBranch() {
        var result = "y = if(false) [1,2] else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void IfElse_NoneElseArray() =>
        "y = if(false) none else [1,2]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", new[] { 1, 2 });


    [Test]
    public void IfElse_StructElseNone_TrueBranch() =>
        Assert.DoesNotThrow(() => "y = if(true) {a = 1} else none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void IfElse_StructElseNone_FalseBranch() {
        var result = "y = if(false) {a = 1} else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [TestCase("y = if(true) if(true) 42 else none else none", 42)]
    [TestCase("y = if(true) 42 else if(true) 0 else none", 42)]
    [TestCase("y = if(false) 42 else if(true) 0 else none", 0)]
    public void IfElse_Nested_WithNone(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [Test]
    public void IfElse_Nested_AllNone() {
        var result = "y = if(false) 42 else if(false) 0 else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void IfElse_Nested_InnerBothNone() {
        var result = "y = if(true) if(false) 42 else none else none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled, values: new[]{("x", (object)input)}).AssertResultHas("y", expected);


    [TestCase("a:int? = 1\r b:int? = 2\r y = if(true) a else b", 1)]
    [TestCase("a:int? = 1\r b:int? = 2\r y = if(false) a else b", 2)]
    [TestCase("a:int? = none\r b:int? = 2\r y = if(true) a else b", null)]
    [TestCase("a:int? = 1\r b:int? = none\r y = if(false) a else b", null)]
    public void IfElse_BothOptionalVariables(string expr, object expected) {
        if (expected == null) {
            var result = expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
            Assert.IsNull(result.Get("y"));
        } else {
            expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);
        }
    }


    [TestCase("flag:bool? = true\r y = if(flag!) 1 else 0", 1)]
    [TestCase("flag:bool? = false\r y = if(flag!) 1 else 0", 0)]
    [TestCase("flag:bool? = true\r y = if(flag ?? false) 1 else 0", 1)]
    [TestCase("flag:bool? = none\r y = if(flag ?? false) 1 else 0", 0)]
    public void IfElse_OptionalBoolCondition(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("y:int? = if(true) 42 else 0", 42)]
    [TestCase("y:int? = if(false) 42 else 0", 0)]
    [TestCase("y:real? = if(true) 1.5 else 2.5", 1.5)]
    [TestCase("y:text? = if(true) 'a' else 'b'", "a")]
    public void IfElse_NonOptionalBranches_AssignedToOptional(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [Test]
    public void IfElse_ArrayOfOptionals_InTrueBranch() =>
        Assert.DoesNotThrow(() => "y = if(true) [1, none, 3] else [4, 5, 6]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void IfElse_NoneArray_InFalseBranch() =>
        Assert.DoesNotThrow(() => "y = if(true) [1, 2] else none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    [TestCase("y:int? = 42\r z = y != none", true)]
    [TestCase("y:int? = none\r z = y != none", false)]
    [TestCase("y:real? = 1.5\r z = y != none", true)]
    [TestCase("y:real? = none\r z = y != none", false)]
    [TestCase("y:text? = 'hi'\r z = y != none", true)]
    [TestCase("y:text? = none\r z = y != none", false)]
    [TestCase("y:bool? = true\r z = y != none", true)]
    [TestCase("y:bool? = none\r z = y != none", false)]
    public void NotEqualsNone(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    [TestCase("y = none == none", true)]
    [TestCase("y = none != none", false)]
    public void NoneComparedToNone(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("x:int? = 42\r y = if(x != none) x! else 0", 42)]
    [TestCase("x:int? = none\r y = if(x != none) x! else 0", 0)]
    [TestCase("x:int? = 42\r y = if(x == none) 0 else x!", 42)]
    [TestCase("x:int? = none\r y = if(x == none) 0 else x!", 0)]
    public void EqualsNone_InCondition(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:text? = 'hi'\r y = if(x != none) x! else 'default'", "hi")]
    [TestCase("x:text? = none\r y = if(x != none) x! else 'default'", "default")]
    public void EqualsNone_TextInCondition(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("y = 42 == none", false)]
    [TestCase("y = 'hi' == none", false)]
    [TestCase("y = true == none", false)]
    [TestCase("y = 42 != none", true)]
    [TestCase("y = 'hi' != none", true)]
    [TestCase("y = 1.5 == none", false)]
    [TestCase("y = 1.5 != none", true)]
    public void NonOptional_ComparedToNone(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("x:int? = 42\r y:int? = none\r z = (x == none) or (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x == none) or (y == none)", false)]
    [TestCase("x:int? = none\r y:int? = none\r z = (x == none) and (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = none\r z = (x == none) and (y == none)", false)]
    public void EqualsNone_InBooleanExpr(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    [TestCase("x:real? = 1.5\r z = x != none", true)]
    [TestCase("x:real? = none\r z = x != none", false)]
    [TestCase("x:byte? = 1\r z = x != none", true)]
    [TestCase("x:byte? = none\r z = x != none", false)]
    [TestCase("x:int64? = 1\r z = x != none", true)]
    [TestCase("x:int64? = none\r z = x != none", false)]
    public void NotEqualsNone_MoreTypes(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    [TestCase("f(x:int?):int = x ?? 0\r y = f(42)", 42)]
    [TestCase("f(x:int?):int = x ?? 0\r y = f(none)", 0)]
    [TestCase("f(x:int?):int = x ?? 99\r y = f(10)", 10)]
    [TestCase("f(x:int?):int = x ?? 99\r y = f(none)", 99)]
    public void UserFunc_OptionalArg_CoalesceBody(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("f(x:text?):text = x ?? 'empty'\r y = f('hello')", "hello")]
    [TestCase("f(x:text?):text = x ?? 'empty'\r y = f(none)", "empty")]
    public void UserFunc_OptionalTextArg(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("f(x:bool?):bool = x ?? false\r y = f(true)", true)]
    [TestCase("f(x:bool?):bool = x ?? false\r y = f(none)", false)]
    public void UserFunc_OptionalBoolArg(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("f(x:real?):real = x ?? 0.0\r y = f(1.5)", 1.5)]
    [TestCase("f(x:real?):real = x ?? 0.0\r y = f(none)", 0.0)]
    public void UserFunc_OptionalRealArg(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5)", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5) ?? 0", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? 0", 0)]
    public void UserFunc_ReturnsOptional(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [Test]
    public void UserFunc_ReturnsOptional_NoneResult() {
        var result = "f(x:int):int? = if(x > 0) x else none\r y = f(-1)".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    // --- User function optional chained with ?? ---

    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5) ?? f(3) ?? 0", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? f(3) ?? 0", 3)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? f(-2) ?? 0", 0)]
    public void UserFunc_ReturnsOptional_ChainedCoalesce(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Generic function with optional ---

    [TestCase("f(x) = x ?? 0\r y = f(none)", 0.0)]
    [TestCase("f(x) = x ?? 'default'\r y = f(none)", "default")]
    public void GenericFunc_WithCoalesce(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Optional in anonymous functions (rule) ---

    [Test]
    public void AnonymousFunc_CoalesceInBody() =>
        Assert.DoesNotThrow(() => "y = [1,2,3].map(rule it ?? 0)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- Optional in pipe ---

    [TestCase("f(x:int?):int = x ?? 0\r x:int? = 42\r y = x.f()", 42)]
    [TestCase("f(x:int?):int = x ?? 0\r x:int? = none\r y = x.f()", 0)]
    public void OptionalInPipe(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    // --- Multiple optional parameters ---

    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(1, 2)", 3)]
    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(1, none)", 1)]
    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(none, 2)", 2)]
    [TestCase("f(a:int?, b:int?):int = (a ?? 0) + (b ?? 0)\r y = f(none, none)", 0)]
    public void UserFunc_MultipleOptionalArgs(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Function returning optional in if-else ---

    [TestCase("f(x:int):int? = if(x > 0) x else none\r g(x:int):int = f(x) ?? -1\r y = g(5)", 5)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r g(x:int):int = f(x) ?? -1\r y = g(-1)", -1)]
    public void UserFunc_OptionalComposition(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Recursive function returning optional ---

    [Test]
    public void UserFunc_Recursive_ReturnsOptional() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x == 0) none else f(x - 1)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- map/filter/fold with optional ---

    [Test]
    public void MapProducingOptional() =>
        Assert.DoesNotThrow(() =>
            "y = [1, -2, 3].map(rule if(it > 0) it else none)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void FilterByOptional_Coalesce() =>
        "y = [1, -2, 3].filter(rule it > 0)".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", new[] { 1, 3 });


    [Test]
    public void MapThenCoalesce() =>
        Assert.DoesNotThrow(() =>
            "y = [1, -2, 3].map(rule if(it > 0) it else none).map(rule it ?? 0)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- Optional in pipe: different types ---

    [TestCase("f(x:real?):real = x ?? 0.0\r p:real? = 3.14\r y = p.f()", 3.14)]
    [TestCase("f(x:real?):real = x ?? 0.0\r p:real? = none\r y = p.f()", 0.0)]
    [TestCase("f(x:text?):text = x ?? 'none'\r p:text? = 'hi'\r y = p.f()", "hi")]
    [TestCase("f(x:text?):text = x ?? 'none'\r p:text? = none\r y = p.f()", "none")]
    [TestCase("f(x:bool?):bool = x ?? false\r p:bool? = true\r y = p.f()", true)]
    [TestCase("f(x:bool?):bool = x ?? false\r p:bool? = none\r y = p.f()", false)]
    public void OptionalInPipe_MoreTypes(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    // --- Function with optional → pipe → arithmetic ---

    [TestCase("f(x:int?):int = x ?? 0\r p:int? = 5\r y = p.f() + 10", 15)]
    [TestCase("f(x:int?):int = x ?? 0\r p:int? = none\r y = p.f() + 10", 10)]
    public void OptionalInPipe_ThenArithmetic(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    // --- Higher-order: function taking optional function ---

    [Test]
    public void FuncReturningOptional_UsedInMap() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x > 0) x else none\r y = [1,-2,3].map(f)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- Function optional result → struct field ---

    [Test]
    public void FuncOptionalResult_AssignedToStructField() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x > 0) x else none\r y = {n = f(5)}".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- Function with all types of optional args ---

    [TestCase("f(a:byte?):byte = a ?? 0\r y = f(none)", (byte)0)]
    [TestCase("f(a:int16?):int16 = a ?? 0\r y = f(none)", (Int16)0)]
    [TestCase("f(a:int64?):int64 = a ?? 0\r y = f(none)", (Int64)0)]
    [TestCase("f(a:uint16?):uint16 = a ?? 0\r y = f(none)", (UInt16)0)]
    [TestCase("f(a:uint32?):uint32 = a ?? 0\r y = f(none)", (UInt32)0)]
    [TestCase("f(a:uint64?):uint64 = a ?? 0\r y = f(none)", (UInt64)0)]
    public void UserFunc_EachTypeOptionalArg(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Nested function calls with optional ---

    [TestCase("f(x:int?):int = x ?? 0\r g(x:int):int? = if(x > 0) x else none\r y = f(g(5))", 5)]
    [TestCase("f(x:int?):int = x ?? 0\r g(x:int):int? = if(x > 0) x else none\r y = f(g(-1))", 0)]
    public void NestedFuncCalls_WithOptional(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Anonymous function returning optional ---

    [Test]
    public void AnonymousFunc_ReturningOptional() =>
        Assert.DoesNotThrow(() =>
            "[1, -2, 3].map(rule if(it > 0) it else none)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- Function accepting optional array ---

    [Test]
    public void UserFunc_OptionalArrayArg() =>
        Assert.DoesNotThrow(() =>
            "f(x:int[]?):int = (x ?? [0])[0]\r y = f(none)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void UserFunc_OptionalArrayArg_HasValue() =>
        "f(x:int[]?):int = (x ?? [0])[0]\r y = f([42, 1])".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", 42);


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
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


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
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


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
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Boolean operators reject T? ---

    [TestCase("x:bool?\r y = x and true")]
    [TestCase("x:bool?\r y = x or true")]
    [TestCase("x:bool?\r y = x xor true")]
    [TestCase("x:bool?\r y = not x")]
    [TestCase("x:bool?\r y = true and x")]
    [TestCase("x:bool?\r y = true or x")]
    [TestCase("x:bool?\r y = true xor x")]
    public void BooleanOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


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
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Built-in functions reject T? ---

    [TestCase("x:int?\r y = abs(x)")]
    [TestCase("x:int?\r y = max(x, 1)")]
    [TestCase("x:int?\r y = min(x, 1)")]
    [TestCase("x:real?\r y = abs(x)")]
    [TestCase("x:real?\r y = round(x)")]
    [TestCase("x:real?\r y = sqrt(x)")]
    [TestCase("x:int?\r y = [1,2,3].map(rule it + x)")]
    [TestCase("x:int?\r y = [1,2,3].filter(rule it > x)")]
    public void BuiltInFuncOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- String operations reject T? ---

    [TestCase("x:text?\r y = x.count()")]
    [TestCase("x:text?\r y = x[0]")]
    [TestCase("x:text?\r y = x.reverse()")]
    [TestCase("x:text?\r y = x.trim()")]
    [TestCase("x:text?\r y = x.concat('!')")]
    [TestCase("x:text?\r y = x.split(' ')")]
    public void StringOpsOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


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
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Arithmetic with two optionals ---

    [TestCase("a:int?\r b:int?\r y = a + b")]
    [TestCase("a:int?\r b:int?\r y = a - b")]
    [TestCase("a:int?\r b:int?\r y = a * b")]
    [TestCase("a:real?\r b:real?\r y = a + b")]
    [TestCase("a:real?\r b:real?\r y = a * b")]
    public void ArithmeticTwoOptionals_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Comparison with two optionals (except == !=) ---

    [TestCase("a:int?\r b:int?\r y = a > b")]
    [TestCase("a:int?\r b:int?\r y = a < b")]
    [TestCase("a:int?\r b:int?\r y = a >= b")]
    [TestCase("a:int?\r b:int?\r y = a <= b")]
    [TestCase("a:real?\r b:real?\r y = a > b")]
    public void ComparisonTwoOptionals_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Unary operations on optional ---

    [TestCase("x:int?\r y = -x")]
    [TestCase("x:int64?\r y = -x")]
    [TestCase("x:real?\r y = -x")]
    [TestCase("x:int?\r y = ~x")]
    [TestCase("x:int64?\r y = ~x")]
    [TestCase("x:byte?\r y = ~x")]
    [TestCase("x:bool?\r y = not x")]
    public void UnaryOnOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Assigning T? to incompatible types ---

    [TestCase("x:int?\r y:real = x")]
    [TestCase("x:byte?\r y:int = x")]
    [TestCase("x:int?\r y:int64 = x")]
    public void AssignOptionalToWiderNonOptional_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Optional in range ---

    [TestCase("x:int?\r y = [1..x]")]
    [TestCase("x:int?\r y = [x..10]")]
    [TestCase("x:int?\r y = [1..10 step x]")]
    public void OptionalInRange_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [Test]
    public void ArrayLiteral_IntAndNone() =>
        Assert.DoesNotThrow(() => "[1, none, 3]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayLiteral_RealAndNone() =>
        Assert.DoesNotThrow(() => "[1.0, none]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayLiteral_TextAndNone() =>
        Assert.DoesNotThrow(() => "['hello', none]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayLiteral_BoolAndNone() =>
        Assert.DoesNotThrow(() => "[true, none, false]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayLiteral_AllNone() =>
        Assert.DoesNotThrow(() => "[none, none]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayLiteral_IntAndNone_TypedResult() =>
        Assert.DoesNotThrow(() => "y:int?[] = [1, none, 3]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayLiteral_RealAndNone_TypedResult() =>
        Assert.DoesNotThrow(() => "y:real?[] = [1.0, none, 3.0]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- Optional array: element access ---

    [TestCase("x:int[]? = [10, 20, 30]\r y = x![0]", 10)]
    [TestCase("x:int[]? = [10, 20, 30]\r y = x![1]", 20)]
    [TestCase("x:int[]? = [10, 20, 30]\r y = x![2]", 30)]
    public void OptionalArray_ForceUnwrap_IndexAccess(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    // --- Array of optionals: element access ---

    [Test]
    public void ArrayOfOptionals_ElementAccess_HasValue() =>
        Assert.DoesNotThrow(() => "x:int?[] = [1, none, 3]\r y = x[0]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionals_ElementAccess_IsOptional() =>
        Assert.DoesNotThrow(() => "x:int?[] = [1, none, 3]\r y:int? = x[0]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionals_ElementAccess_NoneElement() {
        var result = "x:int?[] = [1, none, 3]\r y = x[1]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    // --- Mixed arrays ---

    [Test]
    public void OptionalArray_Count() =>
        Assert.DoesNotThrow(() => "x:int[]? = [1,2,3]\r y = x!.count()".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalArray_Map() =>
        Assert.DoesNotThrow(() => "x:int[]? = [1,2,3]\r y = x!.map(rule it * 2)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- T?[]? combinations ---

    [Test]
    public void OptionalArrayOfOptionals_Builds() =>
        Assert.DoesNotThrow(() => "y:int?[]? = [1, none, 3]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalArrayOfOptionals_None() {
        var result = "y:int?[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    // --- Array of optional — each type ---

    [Test]
    public void ArrayOfOptionalReals_Builds() =>
        Assert.DoesNotThrow(() => "y:real?[] = [1.0, none, 3.0]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalInt64_Builds() =>
        Assert.DoesNotThrow(() => "y:int64?[] = [1, none]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalBytes_Builds() =>
        Assert.DoesNotThrow(() => "y:byte?[] = [1, none]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void ArrayOfOptionalUint32_Builds() =>
        Assert.DoesNotThrow(() => "y:uint32?[] = [1, none]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    // --- Optional array — each type ---

    [Test]
    public void OptionalRealArray_None() {
        var result = "y:real[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalInt64Array_None() {
        var result = "y:int64[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalByteArray_None() {
        var result = "y:byte[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalBoolArray_None() {
        var result = "y:bool[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void OptionalCharArray_None() {
        var result = "y:char[]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    // --- Optional array with unwrap and operations ---

    [Test]
    public void OptionalIntArray_UnwrapThenSum() =>
        "x:int[]? = [1, 2, 3]\r y = x!.sum()".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 6);


    [Test]
    public void OptionalIntArray_UnwrapThenFilter() =>
        "x:int[]? = [1, 2, 3]\r y = x!.filter(rule it > 1)".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", new[] { 2, 3 });


    [Test]
    public void OptionalIntArray_UnwrapThenReverse() =>
        "x:int[]? = [1, 2, 3]\r y = x!.reverse()".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", new[] { 3, 2, 1 });


    // --- Nested arrays with optional ---

    [Test]
    public void NestedOptionalArrays() =>
        Assert.DoesNotThrow(() => "y:int[]?[] = [[1,2], none, [3]]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalNestedArray() =>
        Assert.DoesNotThrow(() => "y:int[][]? = [[1], [2, 3]]".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void OptionalNestedArray_None() {
        var result = "y:int[][]? = none".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [TestCase("y:char? = /'a'", 'a')]
    public void TypeMatrix_CharAnnotation_Value(string expr, char expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:char? = /'a'\r y = x!", 'a')]
    public void TypeMatrix_CharForceUnwrap(string expr, char expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);

    [TestCase("y:real? = 42", 42.0)]
    [TestCase("y:int64? = 42", (Int64)42)]
    [TestCase("y:real? = 0xFF", 255.0)]
    public void Optional_ImplicitCast(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Optional + comparison chains → not supported ---

    [TestCase("x:int?\r y = 1 < x < 10")]
    [TestCase("x:int?\r y = 0 <= x <= 100")]
    public void Optional_ComparisonChain_FailsOnParse(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    // --- Optional + pipe forward ---

    [TestCase("f(x:int?):int = x ?? 0\r y = none.f()", 0)]
    public void Optional_PipeForward_NoneValue(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Optional + string interpolation ---

    [TestCase("x:int? = 42\r y = 'val: {x}'", "val: 42")]
    [TestCase("x:int? = none\r y = 'val: {x}'", "val: none")]
    [TestCase("x:text? = 'hi'\r y = 'val: {x}'", "val: hi")]
    [TestCase("x:text? = none\r y = 'val: {x}'", "val: none")]
    public void Optional_InStringInterpolation(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    // --- Optional + toText ---

    [TestCase("x:int? = 42\r y = toText(x!)", "42")]
    [TestCase("x:real? = 1.5\r y = toText(x!)", "1.5")]
    public void Optional_ToText_AfterUnwrap(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    // --- toText on optional works (opt(T) <: any, toText accepts any) ---

    [TestCase("x:int?\r y = toText(x)", "none")]
    public void Optional_ToText_DirectlyOnOptional(string expr, string expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Optional + default ---

    [Test]
    public void Optional_Default_ReturnsNone() {
        var result = "y:int? = default".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void Optional_RealDefault_ReturnsNone() {
        var result = "y:real? = default".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    [Test]
    public void Optional_TextDefault_ReturnsNone() {
        var result = "y:text? = default".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNull(result.Get("y"));
    }


    // --- Complex / stress expressions ---

    [TestCase("a:int? = 5\r b:int? = none\r y = (a ?? 0) + (b ?? 10)", 15)]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? 1) + (b ?? 2)", 3)]
    [TestCase("a:int? = 3\r b:int? = 4\r y = (a ?? 0) * (b ?? 0)", 12)]
    public void Stress_ComplexOptionalExpressions(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [Test]
    public void Stress_NestedCoalesce_WithFunctions() =>
        "f(x:int):int? = if(x > 0) x else none\r y = f(-1) ?? f(-2) ?? f(3) ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", 3);


    [Test]
    public void Stress_OptionalInArrayMap() =>
        Assert.DoesNotThrow(() =>
            "f(x:int):int? = if(x > 0) x else none\r y = [1,-2,3].map(f)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void Stress_IfElseChainWithOptional() =>
        "x:int? = 5\r y = if(x != none) x! * 2 else -1".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 10);


    [Test]
    public void Stress_IfElseChainWithOptional_None() =>
        "x:int? = none\r y = if(x != none) x! * 2 else -1".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", -1);


    [Test]
    public void Stress_CoalesceInIfCondition() =>
        "x:int? = none\r y = if((x ?? 0) > 0) 'positive' else 'non-positive'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", "non-positive");


    [Test]
    public void Stress_CoalesceInIfCondition_HasValue() =>
        "x:int? = 5\r y = if((x ?? 0) > 0) 'positive' else 'non-positive'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", "positive");


    [Test]
    public void Stress_OptionalArrayFilter() =>
        Assert.DoesNotThrow(() =>
            "y = [1,2,3].map(rule if(it > 1) it else none)".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [Test]
    public void Stress_MultipleOutputsWithOptional() =>
        Assert.DoesNotThrow(() =>
            "a:int? = 42\r b:int? = none\r y = a ?? 0\r z = b ?? -1".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [TestCase("x:int? = 42\r y = x ?? 0\r z = y + 1", 43)]
    public void Stress_OptionalThenArithmetic(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    // --- Optional + array indexing after coalesce ---

    [Test]
    public void Integration_CoalesceThenIndex() =>
        "x:int[]? = [10, 20]\r y = (x ?? [0, 0])[1]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 20);


    [Test]
    public void Integration_CoalesceThenIndex_None() =>
        "x:int[]? = none\r y = (x ?? [0, 0])[1]".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 0);


    // --- Optional + count after unwrap ---

    [Test]
    public void Integration_UnwrapThenCount() =>
        "x:text? = 'hello'\r y = x!.count()".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 5);


    // --- Optional in multi-equation ---

    [Test]
    public void Integration_MultiEquation_OptionalAndRegular() =>
        "a:int? = 42\r b = 10\r y = (a ?? 0) + b".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 52);


    [Test]
    public void Integration_MultiEquation_TwoOptionals() =>
        "a:int? = 5\r b:int? = 3\r y = (a ?? 0) + (b ?? 0)".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 8);


    // --- Optional with negative values ---

    [TestCase("y:int? = -42", -42)]
    [TestCase("y:int? = -1", -1)]
    [TestCase("y:real? = -1.5", -1.5)]
    public void Optional_NegativeValues(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Optional + zero values ---

    [TestCase("y:int? = 0", 0)]
    [TestCase("y:real? = 0.0", 0.0)]
    [TestCase("y:byte? = 0", (byte)0)]
    public void Optional_ZeroValues(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Optional + large values ---

    [TestCase("y:int64? = 9223372036854775807", Int64.MaxValue)]
    [TestCase("y:uint64? = 18446744073709551615", UInt64.MaxValue)]
    public void Optional_LargeValues(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    // --- Optional + toText on unwrapped ---

    [TestCase("x:int? = 42\r y = '{x!}'", "42")]
    [TestCase("x:bool? = true\r y = '{x!}'", "True")]
    public void Optional_InterpolationWithUnwrap(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);



    // Note: ?? does not short-circuit (both args evaluated eagerly),
    // so (a ?? b!) panics when b is none even if a has value.
    [TestCase("a:int? = 5\r b:int? = none\r y = (a ?? b!)")]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? b!)")]
    public void Combo_CoalesceAndUnwrap_UnwrapNone_RuntimeError(string expr) =>
        expr.AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    [TestCase("a:int? = none\r b:int? = 7\r y = a ?? b!", 7)]
    [TestCase("x:int? = 42\r y = (x ?? 0)!", 42)]
    public void Combo_CoalesceAndUnwrap(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [Test]
    public void Combo_CoalesceResultUnwrap_None_RuntimeError() =>
        "a:int? = none\r b:int? = none\r y = (a ?? b)!".AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


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
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:int? = 5\r y = x! + x!", 10)]
    [TestCase("x:int? = 3\r y = x! * x!", 9)]
    [TestCase("x:int? = 10\r y = x! - 3", 7)]
    [TestCase("x:int? = 10\r y = x! / 2", 5.0)]
    [TestCase("x:int? = 7\r y = x! % 3", 1)]
    [TestCase("x:real? = 2.5\r y = x! + 1.5", 4.0)]
    [TestCase("x:real? = 4.0\r y = x! * x!", 16.0)]
    public void Combo_UnwrapThenArithmetic(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("a:int? = none\r b:int? = none\r c:int? = 42\r y = a ?? (b ?? (c ?? 0))", 42)]
    [TestCase("a:int? = none\r b:int? = 7\r c:int? = 42\r y = a ?? (b ?? (c ?? 0))", 7)]
    [TestCase("a:int? = 1\r b:int? = 7\r c:int? = 42\r y = a ?? (b ?? (c ?? 0))", 1)]
    [TestCase("a:int? = none\r b:int? = none\r c:int? = none\r y = a ?? (b ?? (c ?? 0))", 0)]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? b) ?? 0", 0)]
    [TestCase("a:int? = none\r b:int? = 5\r y = (a ?? b) ?? 0", 5)]
    [TestCase("a:int? = 3\r b:int? = 5\r y = (a ?? b) ?? 0", 3)]
    public void Combo_NestedParenthesizedCoalesce(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    // --- Complex boolean expressions with optionals ---

    [TestCase("x:int? = 42\r y:int? = none\r z = (x != none) and (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x != none) and (y == none)", false)]
    [TestCase("x:int? = none\r y:int? = none\r z = (x == none) and (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x != none) and (y != none)", true)]
    [TestCase("x:int? = none\r y:int? = 10\r z = (x == none) or (y == none)", true)]
    [TestCase("x:int? = 42\r y:int? = 10\r z = (x == none) or (y == none)", false)]
    public void Combo_BooleanWithNoneChecks(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("z", expected);


    [TestCase("x:int? = 42\r y = if(x != none) x! + 10 else -1", 52)]
    [TestCase("x:int? = none\r y = if(x != none) x! + 10 else -1", -1)]
    [TestCase("x:int? = 5\r y = if(x != none) x! * x! else 0", 25)]
    [TestCase("x:int? = none\r y = if(x != none) x! * x! else 0", 0)]
    [TestCase("x:real? = 2.5\r y = if(x != none) x! * 2.0 else 0.0", 5.0)]
    [TestCase("x:real? = none\r y = if(x != none) x! * 2.0 else 0.0", 0.0)]
    public void Combo_ConditionalUnwrapWithArithmetic(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("a:int? = 1\r b:int? = 2\r c:int? = 3\r y = a! + b! + c!", 6)]
    [TestCase("a:int? = 10\r b:int? = none\r y = a! + (b ?? 5)", 15)]
    [TestCase("a:int? = none\r b:int? = none\r y = (a ?? 1) + (b ?? 2)", 3)]
    [TestCase("a:int? = 3\r b:int? = none\r y = (a ?? 0) * 2 + (b ?? 1)", 7)]
    public void Combo_MultipleOptionalsInExpr(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = (f(5) ?? 0) + (f(-1) ?? 10)", 15)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = (f(3) ?? 0) * (f(4) ?? 1)", 12)]
    [TestCase("f(x:int):int? = if(x > 0) x else none\r y = f(5)! + f(3)!", 8)]
    public void Combo_FunctionResultCoalesceArithmetic(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [Test]
    public void Combo_FunctionResultUnwrap_RuntimeError() =>
        "f(x:int):int? = if(x > 0) x else none\r y = f(-1)!"
            .AssertObviousFailsOnRuntime(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);


    [Test]
    public void Combo_UnwrapArrayThenIndex() =>
        "x:int[]? = [10,20,30]\r y = x![1] + 5".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", 25);


    [TestCase("x:int? = 42\r y = (if(true) x else none) ?? 0", 42)]
    [TestCase("x:int? = none\r y = (if(true) x else none) ?? 0", 0)]
    [TestCase("x:int? = 42\r y = (if(false) x else none) ?? 0", 0)]
    [TestCase("y = (if(true) 42 else none) ?? 0", 42)]
    [TestCase("y = (if(false) 42 else none) ?? 0", 0)]
    [TestCase("y = (if(true) none else 42) ?? 99", 99)]
    public void Combo_IfElseWrappedInCoalesce(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);



    [TestCase("f(x:int?):int? = x\r g(x:int?):int = x ?? 0\r y = g(f(42))", 42)]
    [TestCase("f(x:int?):int? = x\r g(x:int?):int = x ?? 0\r y = g(f(none))", 0)]
    [TestCase("f(x:int):int? = if(x>0) x else none\r g(x:int?):int = x ?? -1\r y = g(f(5))", 5)]
    [TestCase("f(x:int):int? = if(x>0) x else none\r g(x:int?):int = x ?? -1\r y = g(f(-1))", -1)]
    public void Combo_ChainedFunctionCalls(string expr, object expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertReturns("y", expected);


    [Test]
    public void Combo_PipeUnwrapArithmetic() =>
        Assert.DoesNotThrow(() =>
            "f(x:int?):int = x ?? 0\r x:int? = 42\r y = x.f() + 1".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));


    [TestCase("x:int? = 5\r y = (x ?? 0) > 3", true)]
    [TestCase("x:int? = 1\r y = (x ?? 0) > 3", false)]
    [TestCase("x:int? = none\r y = (x ?? 0) > 3", false)]
    [TestCase("x:int? = 5\r y = (x ?? 0) == 5", true)]
    [TestCase("x:int? = none\r y = (x ?? 0) == 0", true)]
    [TestCase("x:int? = 5\r y = (x ?? 0) >= 5", true)]
    [TestCase("x:int? = 5\r y = (x ?? 0) < 10", true)]
    public void Combo_CoalesceThenComparison(string expr, bool expected) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled).AssertResultHas("y", expected);


    [TestCase("x:int?\r y = (x) + 1")]
    [TestCase("x:int?\r y = -(x)")]
    [TestCase("x:int?\r a:int?\r y = x + a")]
    [TestCase("x:int?\r y = x * x")]
    [TestCase("x:int?\r y = x > x")]
    [TestCase("x:bool?\r y = x and x")]
    [TestCase("x:int?\r y = x & x")]
    public void Combo_Negative_OptionalInArithmetic(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

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
    public void InferredOptionalArray_WithCoalesce_XShouldBeOptional() {
        var result = "x = [1, none, 3]\r y = x.map(rule it ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        var yArr = (int[])result.Get("y");
        Assert.AreEqual(new[] { 1, 0, 3 }, yArr);
        Assert.DoesNotThrow(
            () => result.Get("x"),
            "Reading x throws InvalidCastException because TIC inferred Int32[] instead of Int32?[]");
    }

    [Test]
    public void InferredOptionalArray_ElementCoalesce_XShouldBeOptional() {
        var result = "x = [1, none, 3]\r y = x[0] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(1, result.Get("y"));
        Assert.DoesNotThrow(
            () => result.Get("x"),
            "Reading x throws InvalidCastException because TIC inferred Int32[] instead of Int32?[]");
    }

    [Test]
    public void AnnotatedOptionalArray_MapWithCoalesce_Works() =>
        "x:int?[] = [1, none, 3]\r y = x.map(rule it ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 0, 3 });

    [Test]
    public void InlineOptionalArray_MapWithCoalesce_Works() =>
        "y = [1, none, 3].map(rule it ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 0, 3 });

    [Test]
    public void TypedOptionalArray_TwoNoneLiterals_ShouldCompile() {
        Assert.DoesNotThrow(
            () => "x:int?[] = [1, none, none]"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled),
            "FU775 parse error on typed array with 2 none literals");
    }

    [Test]
    public void TypedOptionalArray_ThreeNoneLiterals_ShouldCompile() {
        Assert.DoesNotThrow(
            () => "x:int?[] = [none, 1, none, 2, none]"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled),
            "FU775 parse error on typed array with 3 none literals");
    }

    [TestCase("x:int?[] = [1, none]", Description = "1 none works")]
    [TestCase("x:int?[] = [none, 1]", Description = "1 none at start works")]
    [TestCase("x:int?[] = [none, none]", Description = "only nones works")]
    public void TypedOptionalArray_SingleOrAllNone_Works(string expr) {
        Assert.DoesNotThrow(
            () => expr.BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }

    [Test]
    public void UntypedArray_MultipleNones_Works() {
        var result = "y = [1, none, none]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.IsNotNull(result.Get("y"));
    }

    // ── Nested arrays with none ─────────────────────────────────────

    [Test]
    public void NestedArrayWithNone_Inline() {
        Assert.DoesNotThrow(
            () => "y = [[1,none],[none,2]]"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
                .Calc());
    }

    [Test]
    public void NestedArrayWithNone_Typed() {
        Assert.DoesNotThrow(
            () => "y:int?[][] = [[1,none],[none,2]]"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled));
    }
}
