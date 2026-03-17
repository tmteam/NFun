using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class StressTestBugsTest {
    // ═══════════════════════════════════════════════════════════════
    // Bug: NullReferenceException when redefining builtin with same arity
    //
    // User-defined recursive function with same name AND same arity
    // as a builtin (sum, count, fold) crashes with NullReferenceException
    // instead of giving a proper error or using the user-defined function.
    //
    // sum(arr) is a builtin (1 arg). Defining sum(n) = recursive crashes.
    // Same for count(n), fold(arr, acc).
    // Different arity (e.g. sum(a,b)) works fine.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: NullReferenceException when redefining builtin with same arity")]
    public void RecursiveFunction_SameNameAsBuiltin_Sum_ShouldNotCrash() {
        // sum(n) has same arity as builtin sum(arr)
        // Should either work (user fn shadows builtin) or give a proper error
        Assert.DoesNotThrow(
            () => "sum(n) = if(n <= 0) 0 else n + sum(n-1)\r y = sum(10)".Calc(),
            "NullReferenceException crash when redefining 'sum' with same arity as builtin");
    }

    [Test, Ignore("Bug: NullReferenceException when redefining builtin with same arity")]
    public void RecursiveFunction_SameNameAsBuiltin_Count_ShouldNotCrash() {
        Assert.DoesNotThrow(
            () => "count(n) = if(n <= 0) 0 else 1 + count(n-1)\r y = count(5)".Calc(),
            "NullReferenceException crash when redefining 'count' with same arity as builtin");
    }

    [Test, Ignore("Bug: NullReferenceException when redefining builtin with same arity")]
    public void RecursiveFunction_SameNameAsBuiltin_Fold_ShouldNotCrash() {
        Assert.DoesNotThrow(
            () => "fold(arr, acc) = if(arr.count()==0) acc else fold(arr[1:], acc+arr[0])\r y = fold([1,2,3], 0)".Calc(),
            "NullReferenceException crash when redefining 'fold' with same arity as builtin");
    }

    [TestCase("sum(a,b) = a + b\r y = sum(3,5)", 8, Description = "Different arity: works")]
    [TestCase("max(a,b) = if(a>b) a else b\r y = max(3,5)", 5, Description = "max(2 args) shadows builtin max(2 args)")]
    public void RecursiveFunction_DifferentArityOrNonRecursive_Works(string expr, int expected) =>
        expr.AssertResultHas("y", expected);


    // ═══════════════════════════════════════════════════════════════
    // Bug: InvalidCastException on map/coalesce over inferred optional array
    //
    // When array type is inferred (not annotated) and you use ?? on elements,
    // it crashes with InvalidCastException. With explicit type annotation
    // (x:int?[]) it works fine. Inline (no intermediate variable) also works.
    //
    // Root cause: inferred type for `x = [1, none, 3]` when later used with
    // ?? changes the type inference path, producing incompatible CLR array types.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void InferredOptionalArray_WithCoalesce_XShouldBeOptional() {
        // When y = x.map(rule it ?? 0), TIC resolves x as Int32[] (wrong).
        // It should be Int32?[] since [1, none, 3] contains none.
        // Reading x crashes with InvalidCastException because null can't
        // be stored in Int32[].
        var result = "x = [1, none, 3]\r y = x.map(rule it ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        // Reading y works (all nones are coalesced to 0)
        var yArr = (int[])result.Get("y");
        Assert.AreEqual(new[] { 1, 0, 3 }, yArr);
        // But reading x should also work — it should be Int32?[]
        Assert.DoesNotThrow(
            () => result.Get("x"),
            "Reading x throws InvalidCastException because TIC inferred Int32[] instead of Int32?[]");
    }

    [Test]
    public void InferredOptionalArray_ElementCoalesce_XShouldBeOptional() {
        // x[0] ?? 0 causes TIC to resolve x as Int32[] instead of Int32?[]
        var result = "x = [1, none, 3]\r y = x[0] ?? 0"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled);
        Assert.AreEqual(1, result.Get("y"));
        // x should still be readable as an optional array
        Assert.DoesNotThrow(
            () => result.Get("x"),
            "Reading x throws InvalidCastException because TIC inferred Int32[] instead of Int32?[]");
    }

    [Test]
    public void AnnotatedOptionalArray_MapWithCoalesce_Works() =>
        // Same expression with explicit type annotation works fine
        "x:int?[] = [1, none, 3]\r y = x.map(rule it ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 0, 3 });

    [Test]
    public void InlineOptionalArray_MapWithCoalesce_Works() =>
        // Inline (no intermediate variable) works fine
        "y = [1, none, 3].map(rule it ?? 0)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.ExperimentalEnabled)
            .AssertResultHas("y", new[] { 1, 0, 3 });


    // ═══════════════════════════════════════════════════════════════
    // Bug: Recursive concat produces extra element (off-by-one)
    //
    // range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))
    // range(0,3) returns [0,1,2,3] (4 elements) instead of [0,1,2] (3 elements).
    //
    // range(5,5) correctly returns [] when called directly.
    // But inside the recursive chain, the base case check appears to be
    // evaluated one step too late, producing one extra element.
    //
    // Likely cause: lazy LINQ evaluation of concat captures function argument
    // slots by reference. When the lazy sequence is materialized, it reads
    // stale/wrong values.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: Recursive concat off-by-one due to lazy LINQ evaluation")]
    public void RecursiveConcat_Range_ShouldNotHaveExtraElement() =>
        // range(0,3) should return [0,1,2], not [0,1,2,3]
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 0, 1, 2 });

    [Test]
    public void RecursiveConcat_Range_BaseCase_ReturnsEmpty() =>
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(5,5)"
            .AssertResultHas("y", Array.Empty<int>());

    [Test, Ignore("Bug: Recursive concat off-by-one due to lazy LINQ evaluation")]
    public void RecursiveConcat_Range_SingleElement() =>
        // range(0,1) should return [0], not [0,1]
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(0,1)"
            .AssertResultHas("y", new[] { 0 });


    // ═══════════════════════════════════════════════════════════════
    // Bug: Recursive concat ignores expressions — uses raw parameter value
    //
    // [a*100].concat(range(a+1,b)) returns [0,1,2,3] instead of [0,100,200].
    // The *100 is completely ignored for all elements from recursive calls.
    //
    // [99].concat(range(a+1,b)) returns [99,1,2,3] — first element correct,
    // but recursive elements are raw 'a' values (1,2,3) instead of 99.
    //
    // [b].concat(range(a+1,b)) returns [3,1,2,3] — first element reads b=3
    // correctly, but recursive elements are raw 'a' values.
    //
    // This is a critical lazy evaluation bug: concat creates a lazy LINQ
    // sequence that captures mutable function argument slots. Recursive calls
    // overwrite the argument, and the lazy array expression for inner calls
    // reads the raw parameter instead of evaluating the expression.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: Recursive concat captures mutable arg slots by reference")]
    public void RecursiveConcat_ExpressionInArray_ShouldBeEvaluated() =>
        // [a*100] should produce 0, 100, 200 — not 0, 1, 2
        "range(a,b) = if(a>=b) [] else [a*100].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 0, 100, 200 });

    [Test, Ignore("Bug: Recursive concat captures mutable arg slots by reference")]
    public void RecursiveConcat_ConstantInArray_ShouldRepeat() =>
        // [99] should appear for every recursive call, not just the first
        "range(a,b) = if(a>=b) [] else [99].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 99, 99, 99 });

    [Test, Ignore("Bug: Recursive concat captures mutable arg slots by reference")]
    public void RecursiveConcat_SecondParamInArray_ShouldUseCorrectParam() =>
        // [b] should always be 3, not the value of 'a' in recursive calls
        "range(a,b) = if(a>=b) [] else [b].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 3, 3, 3 });


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
        // quicksort with filter+concat+recursion causes StackOverflow even on [3,1,2]
        // due to lazy LINQ evaluation cascading through recursion.
        // Cannot test this with Assert.DoesNotThrow because StackOverflow is uncatchable.
        ("quicksort(arr) = if(arr.count() <= 1) arr " +
         "else quicksort(arr[1:].filter(rule it < arr[0]))" +
         ".concat([arr[0]])" +
         ".concat(quicksort(arr[1:].filter(rule it >= arr[0])))\r " +
         "y = quicksort([3,1,2])").Calc();
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: Typed array annotation with 2+ none literals fails
    //
    // x:int?[] = [1, none, none] fails with FU775.
    // x:int?[] = [1, none] works fine (1 none).
    // y = [1, none, none] (untyped) works fine.
    // x:int?[] = [none, none] (only nones) works fine.
    //
    // The bug triggers when: typed annotation + array literal with
    // at least 1 non-none value AND 2+ none literals.
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TypedOptionalArray_TwoNoneLiterals_ShouldCompile() {
        Assert.DoesNotThrow(
            () => "x:int?[] = [1, none, none]".Build(),
            "FU775 parse error on typed array with 2 none literals");
    }

    [Test]
    public void TypedOptionalArray_ThreeNoneLiterals_ShouldCompile() {
        Assert.DoesNotThrow(
            () => "x:int?[] = [none, 1, none, 2, none]".Build(),
            "FU775 parse error on typed array with 3 none literals");
    }

    [TestCase("x:int?[] = [1, none]", Description = "1 none works")]
    [TestCase("x:int?[] = [none, 1]", Description = "1 none at start works")]
    [TestCase("x:int?[] = [none, none]", Description = "only nones works")]
    public void TypedOptionalArray_SingleOrAllNone_Works(string expr) {
        Assert.DoesNotThrow(() => expr.Build());
    }

    [Test]
    public void UntypedArray_MultipleNones_Works() {
        var result = "y = [1, none, none]".Calc();
        Assert.IsNotNull(result.Get("y"));
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: OverflowException not caught as FunnyRuntimeException
    //
    // Integer overflow (2147483647 + 1) throws raw OverflowException
    // instead of FunnyRuntimeException. Users of the library would need
    // to catch .NET OverflowException separately.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: OverflowException not wrapped as FunnyRuntimeException")]
    public void IntegerOverflow_ShouldThrowFunnyRuntimeException() {
        var runtime = "y = 2147483647 + 1".Build();
        Assert.Throws<FunnyRuntimeException>(
            () => runtime.Calc(),
            "OverflowException should be wrapped as FunnyRuntimeException");
    }

    [Test, Ignore("Bug: OverflowException not wrapped as FunnyRuntimeException")]
    public void IntegerUnderflow_ShouldThrowFunnyRuntimeException() {
        var runtime = "y = -2147483648 - 1".Build();
        Assert.Throws<FunnyRuntimeException>(
            () => runtime.Calc(),
            "OverflowException should be wrapped as FunnyRuntimeException");
    }

    [Test, Ignore("Bug: OverflowException not wrapped as FunnyRuntimeException")]
    public void PowerOverflow_ShouldThrowFunnyRuntimeException() {
        var runtime = "y = 2 ** 32".Build();
        Assert.Throws<FunnyRuntimeException>(
            () => runtime.Calc(),
            "OverflowException should be wrapped as FunnyRuntimeException");
    }
}
