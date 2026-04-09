namespace NFun.SyntaxTests.OptionalTypes;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for TypeScript-style safe method chain propagation.
/// Once ?. is used, none propagates through the entire chain of .method() calls.
/// </summary>
[TestFixture]
public class SafeMethodChainTest {

    private static CalculationResult Calc(string expr) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);

    // ═══════════════════════════════════════════════════════════
    // Single ?.method() — basic
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void SingleMethod_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.count() ?? 0").AssertResultHas("out", 3);

    [Test]
    public void SingleMethod_None() =>
        Calc("arr:int[]? = none; out = arr?.count() ?? 0").AssertResultHas("out", 0);

    [Test]
    public void SingleMethod_Sort_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort() ?? []").AssertResultHas("out", new[] { 1, 2, 3 });

    [Test]
    public void SingleMethod_Sort_None() =>
        Calc("arr:int[]? = none; out = arr?.sort() ?? []").AssertResultHas("out", new int[0]);

    // ═══════════════════════════════════════════════════════════
    // Chained ?.method().method() — propagation
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Chain_SortReverse_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort().reverse() ?? []")
            .AssertResultHas("out", new[] { 3, 2, 1 });

    [Test]
    public void Chain_SortReverse_None() =>
        Calc("arr:int[]? = none; out = arr?.sort().reverse() ?? []")
            .AssertResultHas("out", new int[0]);

    [Test]
    public void Chain_SortReverseCount_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort().reverse().count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void Chain_SortReverseCount_None() =>
        Calc("arr:int[]? = none; out = arr?.sort().reverse().count() ?? 0")
            .AssertResultHas("out", 0);

    // ═══════════════════════════════════════════════════════════
    // Text chains
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TextChain_HasValue() =>
        Calc("s:text? = 'hello'; out = s?.reverse().count() ?? 0")
            .AssertResultHas("out", 5);

    [Test]
    public void TextChain_None() =>
        Calc("s:text? = none; out = s?.reverse().count() ?? 0")
            .AssertResultHas("out", 0);

    // ═══════════════════════════════════════════════════════════
    // ?.method() on struct field
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void StructField_SafeMethodChain() =>
        Calc("y = {items = if(true) [3,1,2] else none}; out = y.items?.sort().count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void StructField_SafeMethodChain_None() =>
        Calc("y = {items = if(false) [3,1,2] else none}; out = y.items?.sort().count() ?? 0")
            .AssertResultHas("out", 0);

    // ═══════════════════════════════════════════════════════════
    // Mixed: ?.field then .method()
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void FieldThenMethod_HasValue() =>
        Calc("x = if(true) {items = [1,2,3]} else none; out = x?.items.count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void FieldThenMethod_None() =>
        Calc("x = if(false) {items = [1,2,3]} else none; out = x?.items.count() ?? 0")
            .AssertResultHas("out", 0);
}
