using NFun.TestTools;
using NUnit.Framework;
using static NFun.OptionalTypesSupport;

namespace NFun.SyntaxTests;

[TestFixture]
public class KnownBugsTest {

    // ═══════════════════════════════════════════════════════════════
    // Bug: Quicksort with filter + concat + recursion → StackOverflow
    //
    // Even on tiny input [3,1,2], quicksort implemented with
    // filter + concat + recursion causes a StackOverflowException.
    //
    // Likely related to the lazy LINQ evaluation of filter creating
    // cascading lazy chains that exhaust the stack when materialized.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    [Ignore("StackOverflowException aborts the process, cannot be caught in NUnit")]
    public void RecursiveQuicksort_SmallArray_ShouldNotStackOverflow() {
        ("quicksort(arr) = if(arr.count() <= 1) arr " +
         "else quicksort(arr[1:].filter(rule it < arr[0]))" +
         ".concat([arr[0]])" +
         ".concat(quicksort(arr[1:].filter(rule it >= arr[0])))\r " +
         "y = quicksort([3,1,2])").Calc();
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: Cannot mix typed array and [none] in array literal
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: Cannot mix typed array and [none] in array literal")]
    public void ArrayWithNoneSubarray_ShouldInferOptional() {
        // [[1,2,3],[none]] → "Unable to cast from none to Real"
        // Root cause: output generic resolution [opt(U8)..Re] → Real, losing Optional
        Assert.DoesNotThrow(
            () => "y = [[1,2,3],[none]]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: LCA of containers with none in separate branches
    //
    // When none and concrete values are in DIFFERENT containers that
    // get LCA'd, type inference fails. Same values in ONE container
    // work fine ([1, none] → int?[]).
    //
    // Root cause: ConstraintsState.AddDescendant calls Concretest()
    // which snapshots types before PullNoneNode Phase 2 wraps them
    // in Optional. The stale snapshot propagates up through LCA.
    //
    // Affects: if-else branches, array literals, struct field LCA.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: LCA of [int] vs [none] in if-else")]
    public void IfElse_ArrayWithNone_VsArrayWithInt() {
        Assert.DoesNotThrow(() =>
            "y = if(true) [1] else [none]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Bug: LCA of {a=int} vs {a=none} in if-else")]
    public void IfElse_StructFieldNone() {
        Assert.DoesNotThrow(() =>
            "y = if(true) {a=1} else {a=none}"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Bug: LCA of {a=int} vs {a=none} in array")]
    public void Array_StructsWithNoneField() {
        Assert.DoesNotThrow(() =>
            "y = [{a=1},{a=none}]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Bug: LCA of [[int]] vs [[none]] in if-else")]
    public void IfElse_NestedArrayWithNone() {
        Assert.DoesNotThrow(() =>
            "y = if(true) [[1]] else [[none]]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Bug: map producing [int] vs [none] branches")]
    public void Map_IfElse_ArrayWithNone() {
        Assert.DoesNotThrow(() =>
            "y = [1,2,3].map(rule if(it>1) [it] else [none])"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: Default value struct literal int field inferred as Real
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: Struct default literal int field inferred as Real")]
    public void DefaultStructLiteral_ShouldRespectParamType() =>
        "f(x, opts:{verbose:bool,limit:int}={verbose=false, limit=10}) = if(opts.verbose) x*opts.limit else x \r y = f(5)"
            .AssertReturns("y", 5);
}
