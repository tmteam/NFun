using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

public class HellTests {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitiazlize() => TraceLog.IsEnabled = false;

    [Test]
    public void CustomForeachi() {
        var expr = @"
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi (rule if (it1>t[it2]) it1 else t[it2]) ";

        expr.AssertRuntimes(
            e => e.Calc("t", new[] { 1, 2, 7, 34, 1, 2 })
                .AssertReturns("res", 34));
    }

    [Test]
    public void CustomForeachiWithUserFun() {
        var expr = @"
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            max(a, t, i) = max(a, t[i])

            res:int =  t.foreachi (rule max(it1,t,it2))";
        expr.AssertRuntimes(
            e => e.Calc("t", new[] { 1, 2, 7, 34, 1, 2 }).AssertReturns("res", 34));
    }

    [Test]
    public void CustomForeachiWithBuiltInFun() {
        var expr = @"
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi(rule max(it1,t[it2]))";
        expr.AssertRuntimes(
            e => e.Calc("t", new[] { 1, 2, 7, 34, 1, 2 }).AssertReturns("res", 34));
    }

    [Test]
    public void ConcatExperiments() =>
        "'res: '.concat((n >5).toText())".AssertRuntimes(
            e => e.Calc("n", 1.0).AssertAnonymousOut("res: false"));

    [Test]
    public void ArrayWithUpcast_lambdaConstCalculate() {
        var expr = "x:byte = 42; y:real[] = [1,2,x].map (rule it+1}";
        Assert.Throws<FunnyParseException>(() => expr.Build());
    }

    [Test]
    public void TwinArrayWithUpcast_lambdaSum() =>
        "x:byte = 4; y:real = [[0,1],[2,3],[x]].map (rule sum(it)).sum()".AssertRuntimes(
            e => e.Calc().AssertResultHas("y", 10.0));

    [Test]
    public void TwinArrayWithUpcast_lambdaConstCalculate() =>
        "x:byte = 5; y:real = [[0,1],[2,3],[x]].map (rule it.map(rule it+1).sum()).sum()".AssertRuntimes(
            e => e.Calc().AssertResultHas("y", 16.0));

    [Test]
    public void SomeFun3() =>
        @"   swapIfNotSorted(c, i)
  	                =	if   (c[i]<c[i+1]) c
  		                else c.set(i, 1)".AssertRuntimes();

    [Test]
    public void SomeFun4() =>
        @"twiceSet(arr,i,j,ival,jval)
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr, i, j)
                            = arr.twiceSet(i,j,arr[j], arr[i])

                          swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)".AssertRuntimes();

    [Test]
    public void foldOfHiOrder2() =>
        @"
                #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                 swapIfNotSorted(c, i) = if (c[i]<c[i+1]) c else c

                  # run thru array
                  # and swap every unsorted values
                  onelineSort(input) =
  	                [0..input.count()].fold(input, swapIfNotSorted)".AssertRuntimes();

    [Test]
    public void foldOfHiOrder3() =>
        @"
                #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                 swapIfNotSorted(c, i) = if (c[i]<c[i]) c else c

                 # run thru array
                 # and swap every unsorted values
                 onelineSort(input) = [0..input.count()].fold(input, swapIfNotSorted)".AssertRuntimes();

    [Test]
    public void foldOfHiOrder() =>
        @"twiceSet(arr,i,j,ival,jval)
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr, i, j)
                            = arr.twiceSet(i,j,arr[j], arr[i])

                          swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)

                          # run thru array
                          # and swap every unsorted values
                          onelineSort(input) =
  	                        [0..input.count()].fold(input, swapIfNotSorted)".AssertRuntimes();

    [Test]
    public void BubbleSortSemiConcrete() =>
        @"twiceSet(arr,i,j,ival,jval)
  	                = arr.set(i,ival).set(j,jval)

                  swap(arr, i, j)
                    = arr.twiceSet(i,j,arr[j], arr[i])

                  swapIfNotSorted(c, i)
  	                =	if   (c[i]<c[i+1]) c
  		                else c.swap(i, i+1)

                  # run thru array
                  # and swap every unsorted values
                  onelineSort(input) =
  	                [0..input.count()-2].fold(input, swapIfNotSorted)

                  bubbleSort(input:int[]):int[]=
  	                [0..input.count()-1]
  		                .fold(
  			                input,
  			                rule onelineSort(it1))

                  i:int[]  = [1,4,3,2,5].bubbleSort()"
            .AssertRuntimes(e => e.Calc().AssertReturns("i", new[] { 1, 2, 3, 4, 5 }));

    [Test]
    public void ManyOutputsTest() =>
        ("x:int; " +
         "i = x; " +
         "r = x*100.0; " +
         "t = x.toText(); " +
         "tr = x.toText().reverse(); " +
         "ia = [1,2,3,x];" +
         "ir = [1.0, 2.0, x];" +
         "c = 123;" +
         "d = 'mama ja pokakal';" +
         "etext = ''")
        .AssertRuntimes(e => e
            .Calc("x", 42)
            .AssertResultHas(
                ("i", 42),
                ("r", 4200.0),
                ("t", "42"),
                ("tr", "24"),
                ("ia", new[] { 1, 2, 3, 42 }),
                ("ir", new[] { 1.0, 2.0, 42.0 }),
                ("c", 123),
                ("d", "mama ja pokakal"),
                ("etext", "")
            ));

    [Test]
    public void TestEverything() {
        var expr = @"       twiceSet(arr,i,j,ival,jval) = arr.set(i,ival).set(j,jval)

                          #swap elements i,j in array arr
                          swap(arr, i, j)
                            = arr.twiceSet(i,j,arr[j], arr[i])

                          #swap elements i, i+1 if they are not sorted
                          swapIfNotSorted(c, i) =	if(c[i]<c[i+1]) c else c.swap(i, i+1)

                          # run thru array and swap every unsorted values
                          onelineSort(input) =  [0..input.count()-2].fold(input, swapIfNotSorted)

                          bubbleSort(input)= [0..input.count()-1].fold(input, rule onelineSort(it1))

                          #body
                          ins:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53]
                          rns:real[] = ins
                          tns  = ins.filter(rule it%2==0).map(toText).concat(['vasa','kate'])

                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = tns == tns

                          myOr(a,b):bool = a or b
                          k =  [0..100].map(rule i and r or t xor i).fold(myOr)

                          mySum(a,b) = a + b
                          j =  [0..100].map(rule (ins[1]+ it- ins[2])/it).fold(mySum);
                   ";
        Assert.DoesNotThrow(() =>
            expr.AssertRuntimes(e => e.Calc()));
    }

    // ═══════════════════════════════════════════════════════════════
    // Integration stress: complex combinations of structs, arrays,
    // functions, lambdas, recursion, and generics. Moved from former
    // TicStressTest.cs — these exercise the full pipeline, not TIC alone.
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Generic function with struct construction + field access.</summary>
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

    /// <summary>Generic function over nested structs.</summary>
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

    /// <summary>Array of structs with LCA + map + fold over field.</summary>
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

    /// <summary>If-else with nested struct LCA + field access chain.</summary>
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

    /// <summary>struct→array→map→filter→fold pipeline.</summary>
    [Test]
    public void StructToArrayPipeline() =>
        @"
        data = {values = [1,2,3,4,5,6,7,8,9,10], threshold = 5}
        out:int = data.values.filter(rule it > data.threshold).fold(rule(a,b) = a+b)
        ".Calc()
         .AssertResultHas("out", 40);

    /// <summary>Multiple generics sharing a struct type via GCD.</summary>
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
        Assert.AreEqual(NFun.Types.BaseFunnyType.Struct, personType.BaseType);
        Assert.AreEqual(3, personType.StructTypeSpecification.Count);
    }

    /// <summary>Recursive factorial returning array of intermediate results.</summary>
    [Test]
    public void RecursiveWithArrayAccumulation() =>
        @"
        factSteps(n:int):int[] =
            if(n <= 1) [1]
            else factSteps(n-1).concat([factSteps(n-1).last() * n])

        out = factSteps(5)
        ".Calc()
         .AssertResultHas("out", new[] { 1, 2, 6, 24, 120 });

    /// <summary>Lambda returning struct, used in array init + map.</summary>
    [Test]
    public void LambdaReturningStructInArray() =>
        @"
        mkPair = rule(x) = {first = x, second = x * 2}
        pairs = [1,2,3].map(mkPair)
        out = pairs.map(rule it.second)
        ".Calc()
         .AssertResultHas("out", new[] { 2, 4, 6 });

    /// <summary>If-else of functions (function LCA) applied to array of structs.</summary>
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

    /// <summary>Recursion + struct with array field + map + fold + nested lambda.</summary>
    [Test]
    public void DeepCompositionRecStructArrayLambda() =>
        @"
        sumMatrix(rows:int[][]):int = rows.map(rule it.fold(rule(a,b) = a+b)).fold(rule(a,b) = a+b)

        m = [[1,2,3],[4,5,6],[7,8,9]]
        out = sumMatrix(m)
        ".Calc()
         .AssertResultHas("out", 45);

    /// <summary>Generic function called with struct containing array field.</summary>
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

    /// <summary>Chained user functions with mixed types and struct returns.</summary>
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
