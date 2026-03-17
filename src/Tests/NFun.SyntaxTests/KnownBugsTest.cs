using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class KnownBugsTest {
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


    // ═══════════════════════════════════════════════════════════════
    // Bug: Recursive concat produces extra element (off-by-one)
    //
    // range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))
    // range(0,3) returns [0,1,2,3] (4 elements) instead of [0,1,2] (3 elements).
    //
    // Likely cause: lazy LINQ evaluation of concat captures function argument
    // slots by reference. When the lazy sequence is materialized, it reads
    // stale/wrong values.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: Recursive concat off-by-one due to lazy LINQ evaluation")]
    public void RecursiveConcat_Range_ShouldNotHaveExtraElement() =>
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 0, 1, 2 });

    [Test, Ignore("Bug: Recursive concat off-by-one due to lazy LINQ evaluation")]
    public void RecursiveConcat_Range_SingleElement() =>
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(0,1)"
            .AssertResultHas("y", new[] { 0 });


    // ═══════════════════════════════════════════════════════════════
    // Bug: Recursive concat ignores expressions — uses raw parameter value
    //
    // [a*100].concat(range(a+1,b)) returns [0,1,2,3] instead of [0,100,200].
    // The *100 is completely ignored for all elements from recursive calls.
    //
    // This is a critical lazy evaluation bug: concat creates a lazy LINQ
    // sequence that captures mutable function argument slots. Recursive calls
    // overwrite the argument, and the lazy array expression for inner calls
    // reads the raw parameter instead of evaluating the expression.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: Recursive concat captures mutable arg slots by reference")]
    public void RecursiveConcat_ExpressionInArray_ShouldBeEvaluated() =>
        "range(a,b) = if(a>=b) [] else [a*100].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 0, 100, 200 });

    [Test, Ignore("Bug: Recursive concat captures mutable arg slots by reference")]
    public void RecursiveConcat_ConstantInArray_ShouldRepeat() =>
        "range(a,b) = if(a>=b) [] else [99].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 99, 99, 99 });

    [Test, Ignore("Bug: Recursive concat captures mutable arg slots by reference")]
    public void RecursiveConcat_SecondParamInArray_ShouldUseCorrectParam() =>
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
        ("quicksort(arr) = if(arr.count() <= 1) arr " +
         "else quicksort(arr[1:].filter(rule it < arr[0]))" +
         ".concat([arr[0]])" +
         ".concat(quicksort(arr[1:].filter(rule it >= arr[0])))\r " +
         "y = quicksort([3,1,2])").Calc();
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
