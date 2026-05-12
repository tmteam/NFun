namespace NFun.SyntaxTests.OptionalTypes;

using TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for the PushConstraints IsOptional workaround.
///
/// Bug pattern: When a ConstraintsState with IsOptional=true (i.e. the element of an optional)
/// gets a struct ancestor during Push, TransformToStructOrNull materializes it to a bare struct,
/// losing the Optional wrapper. This manifests in double ?. chains on inferred optional structs:
///   inner = if(cond) {b=42} else none
///   y = if(cond) {a = inner} else none
///   z = y?.a?.b   &lt;-- intermediate optional struct loses its Optional during Push
///
/// Most tests here use intermediate variables (inner, y) rather than nested if-else
/// inside struct literals, because NFun does not support if-else as struct field values.
/// </summary>
[TestFixture]
public class PushConstraintsIsOptionalTest {

    // ===== Double ?. chain (the original bug pattern) =====

    [Test]
    public void DoubleChain_InferredOptionalStruct_BothTrue_ReturnsValue() =>
        "inner = if(true) {b=42} else none\r y = if(true) {a = inner} else none\r z = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 42);

    [Test]
    public void DoubleChain_InferredOptionalStruct_OuterNone_ReturnsDefault() =>
        "inner = if(true) {b=42} else none\r y = if(false) {a = inner} else none\r z = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", -1);

    [Test]
    public void DoubleChain_InferredOptionalStruct_InnerNone_ReturnsDefault() =>
        "inner = if(false) {b=42} else none\r y = if(true) {a = inner} else none\r z = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", -1);

    [Test]
    public void TripleChain_InferredOptionalStruct_AllTrue_ReturnsValue() =>
        ("deep = if(true) {c=42} else none\r"
         + " mid = if(true) {b = deep} else none\r"
         + " y = if(true) {a = mid} else none\r"
         + " z = y?.a?.b?.c ?? -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 42);

    // ===== Optional struct with various field types =====

    [Test]
    public void DoubleChain_TextField_ReturnsValue() =>
        "inner = if(true) {name='hello'} else none\r y = if(true) {a = inner} else none\r z = y?.a?.name ?? ''"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", "hello");

    [Test]
    public void DoubleChain_BoolField_ReturnsValue() =>
        "inner = if(true) {flag=true} else none\r y = if(true) {a = inner} else none\r z = y?.a?.flag ?? false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", true);

    [Test]
    public void DoubleChain_RealField_ReturnsValue() =>
        "inner = if(true) {v=3.14} else none\r y = if(true) {a = inner} else none\r z = y?.a?.v ?? 0.0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 3.14);

    [Test]
    public void DoubleChain_ArrayField_ReturnsValue() =>
        "inner = if(true) {items=[1,2,3]} else none\r y = if(true) {a = inner} else none\r z = y?.a?.items ?? [0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", new[] { 1, 2, 3 });

    // ===== Optional struct from function return =====

    [Test]
    public void DoubleChain_FromFunctionReturn_ReturnsValue() =>
        ("f() = if(true) {v=42} else none\r"
         + " y = if(true) {inner = f()} else none\r"
         + " z = y?.inner?.v ?? -1")
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                allowUserFunctions: AllowUserFunctions.AllowAll)
            .AssertResultHas("z", 42);

    // ===== Optional struct with multiple fields =====

    [Test]
    public void DoubleChain_MultipleFields_SumBoth() =>
        ("inner = if(true) {x=1, y=2} else none\r"
         + " outer = if(true) {a = inner} else none\r"
         + " z = (outer?.a?.x ?? 0) + (outer?.a?.y ?? 0)")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 3);

    // ===== Safe access + method chain on nested optional =====

    [Test]
    public void DoubleChain_ArrayFieldCount_ReturnsValue() =>
        ("inner = if(true) {items=[1,2,3]} else none\r"
         + " y = if(true) {a = inner} else none\r"
         + " z = y?.a?.items.count() ?? 0")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 3);

    // ===== Interaction with type narrowing =====

    [Test]
    public void DoubleChain_WithManualNoneCheck_ReturnsValue() =>
        ("inner = if(true) {v=42} else none\r"
         + " y = if(true) {a = inner} else none\r"
         + " z:int = if(y != none and y.a != none) y.a.v else -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 42);

    // ===== Array of optional structs with optional fields =====

    [Test]
    public void DoubleChain_InArrayElement_ReturnsValue() =>
        ("inner = if(true) {v=1} else none\r"
         + " item = if(true) {a = inner} else none\r"
         + " arr = [item]\r"
         + " z = arr[0]?.a?.v ?? -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 1);

    // ===== Negative tests -- inner struct NOT optional, single ?. suffices =====

    [Test]
    public void SingleChain_InnerNotOptional_ReturnsValue() =>
        "y = if(true) {a = {b=42}} else none\r z = y?.a.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 42);

    [Test]
    public void NoOptional_PlainStruct_NoIsOptionalInvolved() =>
        "y = {a = {b = 42}}\r z = y.a.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 42);

    // ===== Edge cases =====

    [Test]
    public void DoubleChain_BothNone_ReturnsDefault() =>
        "inner = if(false) {b=42} else none\r y = if(false) {a = inner} else none\r z = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", -1);

    [Test]
    public void DoubleChain_TypedVariable_ReturnsValue() =>
        "y:{a:{b:int}?}? = {a = {b = 99}}\r z = y?.a?.b ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 99);

    [Test]
    public void DoubleChain_InIfElseResult_ReturnsValue() =>
        ("inner = if(true) {b=42} else none\r"
         + " y = if(true) {a = inner} else none\r"
         + " z:int = if(true) (y?.a?.b ?? 0) else -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 42);

    [Test]
    public void DoubleChain_CoalesceAtEachLevel_ReturnsValue() =>
        ("inner = if(true) {b=42} else none\r"
         + " y = if(true) {a = inner} else none\r"
         + " resolved = y?.a ?? {b = 0}\r"
         + " z = resolved.b")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 42);

    [Test]
    public void DoubleChain_ArrayOfOptionalStructs_FilterFirstValid() =>
        ("inner = if(true) {v=10} else none\r"
         + " item = if(true) {a = inner} else none\r"
         + " arr = [item, none]\r"
         + " z = arr[0]?.a?.v ?? -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("z", 10);
}
