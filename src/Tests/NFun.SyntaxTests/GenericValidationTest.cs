using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Tests for FU711 generic resolution validation.
/// WORKAROUND: When a generic function's type parameter T resolves to Any and T appears
/// at different structural depths in the function's input arguments (e.g., bare T and T[]),
/// the expression is rejected because the Any resolution is vacuous --
/// TIC merged structurally incompatible constraints instead of reporting a contradiction.
/// </summary>
[TestFixture]
public class GenericValidationTest {

    // ═══════════════════════════════════════════════════════════════
    // 1. `in` operator -- the original bug
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TextInText_ShouldError() {
        // 'h' is text (char[]), 'hello' is text (char[]).
        // in(T, T[]): T must be both char[] and char -> contradiction -> T=Any -> FU711
        Assert.Throws<FunnyParseException>(
            () => "y = 'h' in 'hello'".Build());
    }

    [Test]
    public void TextInTextArray_ShouldWork() {
        // 'abc' is text, ['abc','def'] is text[]. in(T, T[]) with T=text. Correct.
        "y = 'abc' in ['abc', 'def']".AssertReturns("y", true);
    }

    [Test]
    public void CharInText_ShouldWork() {
        // /'h' is char, 'hello' is char[]. in(T, T[]) with T=char. Correct.
        "y = /'h' in 'hello'".AssertReturns("y", true);
    }

    [Test]
    public void ArrayInArrayOfArrays_ShouldWork() {
        // [1,2] is int[], [[1,2],[3,4]] is int[][]. in(T, T[]) with T=int[]. Correct.
        "y = [1,2] in [[1,2],[3,4]]".AssertReturns("y", true);
    }

    [Test]
    public void ArrayInFlatArray_ShouldError() {
        // [1,2] is int[], [1,2,3] is int[]. in(T, T[]): T must be both int[] and int -> FU711
        Assert.Throws<FunnyParseException>(
            () => "y = [1,2] in [1,2,3]".Build());
    }

    [Test]
    public void IntInIntArray_ShouldWork() {
        // 1 is int, [1,2,3] is int[]. in(T, T[]) with T=int. Correct.
        "y = 1 in [1,2,3]".AssertReturns("y", true);
    }

    [Test]
    public void TextInTextArrayLiteral_ShouldWork() {
        // 'hello' is text, ['hello','world'] is text[]. in(T, T[]) with T=text. Correct.
        "y = 'hello' in ['hello', 'world']".AssertReturns("y", true);
    }

    // ═══════════════════════════════════════════════════════════════
    // 2. Other generic functions -- should NOT trigger FU711
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ConcatOfTexts_ShouldWork() {
        // concat(T[], T[]) -> T[] with T=char. All T at depth 1 in args. No depth mismatch.
        "y = concat('abc', 'def')".AssertReturns("y", "abcdef");
    }

    [Test]
    public void MaxOfTexts_ShouldWork() {
        // max(T, T) -> T with T=text (char[]). Same depth in both args.
        "y = max('hello', 'world')".AssertReturns("y", "world");
    }

    [Test]
    public void ConcatOfIntArrays_ShouldWork() {
        // concat(T[], T[]) -> T[] with T=int. Correct.
        "y = [1,2].concat([3,4])".AssertReturns("y", new[] { 1, 2, 3, 4 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 3. Edge cases for the validation
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void IntInText_ShouldError() {
        // 1 is int, 'hello' is char[]. in(T, T[]): T must be both int and char -> Any -> FU711
        Assert.Throws<FunnyParseException>(
            () => "y = 1 in 'hello'".Build());
    }

    [Test]
    public void BoolInIntArray_ShouldError() {
        // true is bool, [1,2,3] is int[]. in(T, T[]): T must be both bool and int -> Any -> FU711
        Assert.Throws<FunnyParseException>(
            () => "y = true in [1,2,3]".Build());
    }

    [Test, Ignore("Optional types experimental - none literal requires dialect flag")]
    public void NoneInIntArray_ShouldWork() {
        // none in [1,2,3] -- none is opt(T), [1,2,3] is int[].
        // Requires optional types to be enabled.
        "y = none in [1,2,3]"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .Calc();
    }

    // ═══════════════════════════════════════════════════════════════
    // 4. Functions where T=Any is legitimate or already errors
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MaxOfIncompatibleTypes_ShouldError() {
        // max(T, T) with int and text. TIC catches this before FU711 (comparable constraint).
        Assert.Throws<FunnyParseException>(
            () => "y = max(1, 'hello')".Build());
    }

    [Test]
    public void GenericUserFunction_IdOfInt_ShouldWork() {
        // id(x) = x; y = id(42). T=int. No depth mismatch.
        "id(x) = x \r y = id(42)".AssertReturns("y", 42);
    }

    [Test]
    public void GenericUserFunction_IdOfArray_ShouldWork() {
        // id(x) = x; y = id([1,2]). T=int[]. No depth mismatch.
        "id(x) = x \r y = id([1,2])".AssertReturns("y", new[] { 1, 2 });
    }

    // ═══════════════════════════════════════════════════════════════
    // 5. Negative: expressions that should still work (no false positives)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void MapWithLambda_ShouldWork() {
        // map(T[], rule(T)->TR) -> TR[] with T=int, TR=int. No depth mismatch.
        "y = [1, 2, 3].map(rule it * 2)".AssertReturns("y", new[] { 2, 4, 6 });
    }

    [Test]
    public void FilterWithLambda_ShouldWork() {
        // filter(T[], rule(T)->bool) -> T[]. T=int at depth 1 in all arg positions.
        "y = [1, 2, 3].filter(rule it > 1)".AssertReturns("y", new[] { 2, 3 });
    }

    [Test]
    public void MaxOfInts_ShouldWork() {
        // max(T, T) -> T with T=real (generic int literals resolve to real). Same depth in both args.
        "y = max(1, 2)".AssertReturns("y", 2.0);
    }

    [Test]
    public void FoldWithLambda_ShouldWork() {
        // fold(T[], rule(T,T)->T) -> T. T at depth 1 in all arg positions.
        "y = [1,2,3].fold(rule it1 + it2)".AssertReturns("y", 6);
    }
}
