using NFun.TestTools;
using NUnit.Framework;
using static NFun.OptionalTypesSupport;

namespace NFun.SyntaxTests.OptionalTypes;

/// <summary>
/// Syntax-level reproduction of the 6 stale snapshot bugs.
///
/// These mirror the TIC-level tests in NFun.Tic.Tests.Optional.StaleSnapshotTests
/// but go through the full pipeline (tokenizer -> parser -> TIC -> runtime).
///
/// Root cause: ConstraintsState.AddDescendant() calls Concretest() which
/// snapshots types before PullNoneNode Phase 2 wraps them in Optional.
/// </summary>
[TestFixture]
public class StaleSnapshotSyntaxTests {

    [Test, Ignore("Stale snapshot: LCA of [int] vs [none] in if-else")]
    public void IfElse_ArrayWithInt_vs_ArrayWithNone() {
        // Bug 1: if(true) [1] else [none]
        // Expected: builds successfully, y = opt(int)[]
        Assert.DoesNotThrow(
            () => "y = if(true) [1] else [none]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Stale snapshot: LCA of {a=int} vs {a=none} in if-else")]
    public void IfElse_StructFieldInt_vs_StructFieldNone() {
        // Bug 2: if(true) {a=1} else {a=none}
        // Expected: builds successfully, y = {a: opt(int)}
        Assert.DoesNotThrow(
            () => "y = if(true) {a=1} else {a=none}"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Stale snapshot: LCA of {a=int} vs {a=none} in array literal")]
    public void ArrayLiteral_StructsWithNoneField() {
        // Bug 3: [{a=1},{a=none}]
        // Expected: builds successfully, y = {a: opt(int)}[]
        Assert.DoesNotThrow(
            () => "y = [{a=1},{a=none}]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Stale snapshot: LCA of [[int]] vs [[none]] in if-else")]
    public void IfElse_NestedArrayWithNone() {
        // Bug 4: if(true) [[1]] else [[none]]
        // Expected: builds successfully, y = opt(int)[][]
        Assert.DoesNotThrow(
            () => "y = if(true) [[1]] else [[none]]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Stale snapshot: map lambda producing [int] vs [none] branches")]
    public void Map_LambdaIfElse_ArrayWithNone() {
        // Bug 5: [1,2,3].map(rule if(it>1) [it] else [none])
        // Expected: builds successfully, y = opt(int)[][]
        Assert.DoesNotThrow(
            () => "y = [1,2,3].map(rule if(it>1) [it] else [none])"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Stale snapshot: [[int,int,int],[none]] partial array with none")]
    public void ArrayLiteral_NestedIntArray_and_NoneArray() {
        // Bug 6: [[1,2,3],[none]]
        // Expected: builds successfully, y = opt(int)[][]
        Assert.DoesNotThrow(
            () => "y = [[1,2,3],[none]]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }
}
