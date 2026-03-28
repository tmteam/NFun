using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;
using static NFun.OptionalTypesSupport;

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

    [Test, Ignore("Bug: OverflowException not wrapped — Int64")]
    public void Int64Overflow_ShouldThrowFunnyRuntimeException() {
        var runtime = "y:int64 = 9223372036854775807 + 1".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: DivideByZeroException not caught as FunnyRuntimeException
    //
    // Integer division (//) and modulo (%) by zero throw raw CLR
    // DivideByZeroException instead of FunnyRuntimeException.
    // Real division (/) correctly returns Infinity.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: DivideByZeroException not wrapped as FunnyRuntimeException")]
    public void IntDivisionByZero_ShouldThrowFunnyRuntimeException() {
        var runtime = "y = 1 // 0".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test, Ignore("Bug: DivideByZeroException not wrapped as FunnyRuntimeException")]
    public void ModuloByZero_ShouldThrowFunnyRuntimeException() {
        var runtime = "y = 5 % 0".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: IndexOutOfRangeException in last() on empty array
    //
    // [].last() throws raw CLR IndexOutOfRangeException.
    // [].first() correctly throws FunnyRuntimeException("Array is empty").
    // LastFunction calculates arr.Count-1 = -1 and passes it to
    // GetElementOrNull which doesn't handle negative indices.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: IndexOutOfRangeException in last() on empty array")]
    public void LastOfEmptyArray_ShouldThrowFunnyRuntimeException() {
        var runtime = "y:int[] = []\r z = y.last()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: InvalidOperationException in median() and avg() on empty array
    //
    // median([]) and avg([]) throw raw CLR InvalidOperationException
    // ("Empty collection" / "Sequence contains no elements")
    // instead of FunnyRuntimeException.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: InvalidOperationException in median() on empty array")]
    public void MedianOfEmptyArray_ShouldThrowFunnyRuntimeException() {
        var runtime = "y:int[] = []\r z = y.median()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test, Ignore("Bug: InvalidOperationException in avg() on empty array")]
    public void AvgOfEmptyArray_ShouldThrowFunnyRuntimeException() {
        var runtime = "y:real[] = []\r z = y.avg()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: ArgumentOutOfRangeException in repeat() with negative count
    //
    // repeat(1, -1) throws raw CLR ArgumentOutOfRangeException
    // instead of FunnyRuntimeException.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: ArgumentOutOfRangeException in repeat() with negative count")]
    public void RepeatNegativeCount_ShouldThrowFunnyRuntimeException() {
        var runtime = "y = repeat(1, -1)".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: max()/min() on empty array returns null for non-optional type
    //
    // [].max() returns null but the output type is Int32 (not Int32?).
    // The runtime silently puts null into a non-optional variable.
    // Should either throw FunnyRuntimeException or require optional type.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: max() on empty array returns null for non-optional type")]
    public void MaxOfEmptyArray_ShouldThrowOrReturnOptional() {
        // Currently: z gets null despite being typed as Int32
        var runtime = "y:int[] = []\r z = y.max()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }

    [Test, Ignore("Bug: min() on empty array returns null for non-optional type")]
    public void MinOfEmptyArray_ShouldThrowOrReturnOptional() {
        var runtime = "y:int[] = []\r z = y.min()".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: InvalidCastException on inline nested array with none
    //
    // [[1,none],[none,2]] causes InvalidCastException at runtime.
    // The inner [1,none] infers as int?[], but the outer array
    // creation uses the wrong CLR array type.
    // Workaround: assign inner arrays to typed variables first.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: InvalidCastException on inline nested array with none")]
    public void InlineNestedArrayWithNone_ShouldNotCrash() {
        // [[1,none],[none,2]] → InvalidCastException at runtime
        Assert.DoesNotThrow(
            () => "y = [[1,none],[none,2]]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled)
                .Calc());
    }

    [Test, Ignore("Bug: Type inference fails on typed inline nested array with none")]
    public void TypedNestedArrayWithNone_ShouldWork() {
        // Even explicit type annotation fails:
        // "Seems like array [...] cannot be used here"
        Assert.DoesNotThrow(
            () => "y:int?[][] = [[1,none],[none,2]]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }

    [Test, Ignore("Bug: Cannot mix typed array and [none] in array literal")]
    public void ArrayWithNoneSubarray_ShouldInferOptional() {
        // [[1,2,3],[none]] → "Unable to cast from none to Real"
        Assert.DoesNotThrow(
            () => "y = [[1,2,3],[none]]"
                .BuildWithDialect(optionalTypesSupport: ExperimentalEnabled));
    }


    // ═══════════════════════════════════════════════════════════════
    // Bug: All-named call to user function with defaults fails
    //
    // f(a, b=0) = a+b; y = f(a=5)  → error "Named arguments are
    // not supported for built-in function 'f'" — but f IS a user function.
    //
    // Root cause: FindUserFunctionByNameWithDefaults checks
    // positionalArgCount >= requiredCount, but when all args are named
    // (positionalArgCount=0), the check fails even though named args
    // can fill required slots.
    //
    // Fix: check (positionalArgCount + namedArgCount) >= requiredCount
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: All-named call to user function with defaults fails")]
    public void AllNamedCall_UserFunc_WithDefaults_ShouldWork() =>
        "f(a, b=0) = a+b \r y = f(a=5)".AssertReturns("y", 5);

    [Test, Ignore("Bug: All-named call to user function with defaults fails")]
    public void AllNamedCall_UserFunc_WithMultipleDefaults_ShouldWork() =>
        "f(a, b=0, c=0) = a+b+c \r y = f(a=1, c=3)".AssertReturns("y", 4);

    [Test, Ignore("Bug: All-named call to user function with defaults fails")]
    public void AllNamedCall_UserFunc_WithParams_ShouldWork() =>
        "f(a, ...rest) = a + rest.count() \r y = f(a=5)".AssertReturns("y", 5);

    [Test, Ignore("Bug: All-named call to user function with defaults fails")]
    public void AllNamedCall_UserFunc_DefaultsAndParams_ShouldWork() =>
        "f(a, b=0, ...rest) = a+b+rest.sum() \r y = f(a=1)".AssertReturns("y", 1);


    // ═══════════════════════════════════════════════════════════════
    // Bug: Default value [] and struct literals have wrong type
    //
    // f(x, acc:int[]=[]) → "Unable to cast from Any[] to Int32[]"
    // f(x, opts:{v:bool,n:int}={v=false, n=10}) → int field inferred as Real
    //
    // Default value expressions don't receive type constraints from
    // the parameter type annotation.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: Empty array default doesn't respect parameter type")]
    public void DefaultEmptyArray_ShouldInferFromParamType() =>
        "f(n, acc:int[]=[]) = if(n<=0) acc else f(n-1, acc.append(n)) \r y = f(3)"
            .AssertReturns("y", new[] { 3, 2, 1 });

    [Test, Ignore("Bug: Struct default literal int field inferred as Real")]
    public void DefaultStructLiteral_ShouldRespectParamType() =>
        "f(x, opts:{verbose:bool,limit:int}={verbose=false, limit=10}) = if(opts.verbose) x*opts.limit else x \r y = f(5)"
            .AssertReturns("y", 5);


    // ═══════════════════════════════════════════════════════════════
    // Bug: OverflowException in backwards slice [start:end] where start>end
    //
    // [1,2,3][2:0] throws OverflowException.
    // The slice function special-cases end==0 as "no end" (→ int.MaxValue),
    // then the array size calculation overflows.
    // ═══════════════════════════════════════════════════════════════

    [Test, Ignore("Bug: OverflowException in backwards slice")]
    public void BackwardsSlice_ShouldThrowFunnyRuntimeException() {
        var runtime = "y = [1,2,3,4,5][3:1]".Build();
        Assert.Throws<FunnyRuntimeException>(() => runtime.Calc());
    }
}
