using NFun.TestTools;
using NFun.Tic;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Stress tests for TIC solver: complex combinations of
/// structs, arrays, functions, lambdas, recursion, and generics.
/// </summary>
[TestFixture]
public class TicStressTest {

    /// <summary>
    /// 1. Generic function with struct construction + field access.
    /// Tests: struct construction in function return + field access + type inference
    /// </summary>
    [Test]
    public void GenericFunctionReturningStruct() =>
        @"
        wrap(x, y) = {left = x, right = y, sum = x + y}
        r1 = wrap(10, 20)
        r2 = wrap(1.5, 2.5)
        out1:int = r1.sum
        out2 = r2.sum
        out3 = r1.left
        ".Calc()
         .AssertResultHas("out1", 30)
         .AssertResultHas("out2", 4.0)
         .AssertResultHas("out3", 10);

    /// <summary>
    /// 2. Generic function over nested structs.
    /// Tests: generic field access + nested struct + function application
    /// </summary>
    [Test]
    public void GenericFunctionOverNestedStructs() =>
        @"
        getInner(x) = x.inner.value

        a = {inner = {value = 42}, name = 'test'}
        b = {inner = {value = true}}
        c = {inner = {value = [1,2,3]}, extra = 99}

        r1 = getInner(a)
        r2 = getInner(b)
        r3 = getInner(c)
        ".Calc()
         .AssertResultHas("r1", 42)
         .AssertResultHas("r2", true)
         .AssertResultHas("r3", new[] { 1, 2, 3 });

    /// <summary>
    /// 3. Array of structs with LCA + map + field access.
    /// Tests: struct LCA in array + higher-order function + lambda
    /// </summary>
    [Test]
    public void ArrayOfStructsLcaWithMap() =>
        @"
        items = [
            {id = 1, score = 10},
            {id = 2, score = 20, name = 'extra'},
            {id = 3, score = 30, tag = true}
        ]
        scores = items.map(rule it.score)
        out = scores.fold(rule(a,b) = a + b)
        ".Calc()
         .AssertResultHas("out", 60);

    /// <summary>
    /// 4. If-else with nested struct results + field access chain.
    /// Tests: struct LCA across if-else branches with matching nested structure
    /// </summary>
    [Test]
    public void IfElseNestedStructLca() =>
        @"
        x = if(true)
                {meta = {id = 1, tag = 'a'}, score = 10}
            else
                {meta = {id = 2, tag = 'b'}, score = 20, extra = true}

        out1 = x.meta.id
        out2 = x.score
        ".Calc()
         .AssertResultHas("out1", 1)
         .AssertResultHas("out2", 10);

    /// <summary>
    /// 5. Function pipeline: struct → array → map → filter → fold.
    /// Tests: chained higher-order functions + struct fields + type inference through pipeline
    /// </summary>
    [Test]
    public void StructToArrayPipeline() =>
        @"
        data = {values = [1,2,3,4,5,6,7,8,9,10], threshold = 5}
        out:int = data.values.filter(rule it > data.threshold).fold(rule(a,b) = a+b)
        ".Calc()
         .AssertResultHas("out", 40);

    /// <summary>
    /// 6. Multiple generic functions sharing struct type via GCD.
    /// Tests: GCD (greatest common descendant) forcing struct fields from different call sites
    /// </summary>
    [Test]
    public void MultipleGenericFunctionsGcd() {
        var expr = @"
            getName(x) = x.name
            getAge(x)  = x.age
            getSize(x) = x.size

            n = getName(person)
            a = getAge(person)
            s = getSize(person)
        ";
        var runtime = Funny.Hardcore.Build(expr);
        var personType = runtime["person"].Type;
        Assert.AreEqual(BaseFunnyType.Struct, personType.BaseType);
        Assert.AreEqual(3, personType.StructTypeSpecification.Count);
    }

    /// <summary>
    /// 7. Recursive factorial returning array of intermediate results.
    /// Tests: recursion + array construction + concat + generic type inference
    /// </summary>
    [Test]
    public void RecursiveWithArrayAccumulation() =>
        @"
        factSteps(n:int):int[] =
            if(n <= 1) [1]
            else factSteps(n-1).concat([factSteps(n-1).last() * n])

        out = factSteps(5)
        ".Calc()
         .AssertResultHas("out", new[] { 1, 2, 6, 24, 120 });

    /// <summary>
    /// 8. Lambda returning struct, used in array init + map.
    /// Tests: lambda with struct return + array of struct + chained field access
    /// </summary>
    [Test]
    public void LambdaReturningStructInArray() =>
        @"
        mkPair = rule(x) = {first = x, second = x * 2}
        pairs = [1,2,3].map(mkPair)
        out = pairs.map(rule it.second)
        ".Calc()
         .AssertResultHas("out", new[] { 2, 4, 6 });

    /// <summary>
    /// 9. If-else of functions (function LCA) applied to array of structs.
    /// Tests: function type LCA + struct LCA + array operations
    /// </summary>
    [Test]
    public void FunctionLcaAppliedToStructArray() =>
        @"
        scorer1 = rule it.score * 2
        scorer2 = rule it.score + it.bonus
        picker = if(true) scorer1 else scorer2

        items = [{score = 10, bonus = 5}, {score = 20, bonus = 3}]
        out = items.map(picker)
        ".Calc()
         .AssertResultHas("out", new[] { 20, 40 });

    /// <summary>
    /// 10. Deep composition: recursive function + struct + array + nested lambda.
    /// Tests: recursion + struct with array field + map + fold + all at once
    /// </summary>
    [Test]
    public void DeepCompositionRecStructArrayLambda() =>
        @"
        sumMatrix(rows:int[][]):int = rows.map(rule it.fold(rule(a,b) = a+b)).fold(rule(a,b) = a+b)

        m = [[1,2,3],[4,5,6],[7,8,9]]
        out = sumMatrix(m)
        ".Calc()
         .AssertResultHas("out", 45);

    /// <summary>
    /// 11. Generic function called with struct containing array field.
    /// Tests: generic over struct with composite field type
    /// </summary>
    [Test]
    public void GenericFunctionWithArrayField() =>
        @"
        firstItem(x) = x.items[0]

        a = {items = [10, 20, 30], label = 'nums'}
        b = {items = ['hello', 'world']}

        r1 = firstItem(a)
        r2 = firstItem(b)
        ".Calc()
         .AssertResultHas("r1", 10)
         .AssertResultHas("r2", "hello");

    /// <summary>
    /// 12. Chained user functions with mixed types and struct returns.
    /// Tests: multiple user functions + struct construction + type propagation
    /// </summary>
    [Test]
    public void ChainedFunctionsWithStructReturns() =>
        @"
        mkRange(lo, hi) = {min = lo, max = hi, span = hi - lo}
        scaleRange(r, factor) = mkRange(r.min * factor, r.max * factor)
        r = scaleRange(mkRange(1, 10), 3)
        out1 = r.min
        out2 = r.max
        out3 = r.span
        ".Calc()
         .AssertResultHas("out1", 3)
         .AssertResultHas("out2", 30)
         .AssertResultHas("out3", 27);
}
