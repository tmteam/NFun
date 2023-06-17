namespace NFun.ConcurrentTests;
using NFun.TestTools;
using NUnit.Framework;
using Tic;

public class HardcoreApiConcurrentTest {
    [TestCase("y = 2*x", 3, 6)]
    [TestCase("y = 2.0*x", 3.5, 7.0)]
    [TestCase("y = x/4", 10, 2.5)]
    [TestCase("y = (x + 4/x)", 2, 4)]
    [TestCase("y = x**3", 2, 8)]
    [TestCase("y = x % -4", 5, 1)]
    [TestCase("y = x % 4", -5, -1)]
    [TestCase("y = -(-(-x))", 2, -2)]
    [TestCase("y ={a=x}.a", 2, 2)]
    [TestCase("y = x/0.2", 1, 5)]
    [TestCase("f(a) = a*2; y = x.f().f()", 2, 8)]
    [TestCase("y = [1,2].fold(rule it1+it2) + x", 1, 4)]
    [TestCase(@"
        fact(n) = if(n==0) 1 else n * fact(n-1)
        y = fact(x)
        ", 5, 120)]
    [TestCase(@"f(x) = x*2; z = f(x); y = f(x)", 1, 2)]
    [TestCase(@"f(x) = x*2; z:int = f(1); y:real = f(x);", 1, 2)]
    [TestCase("y = 1<2<x>-100>-150 != 1<4<x>-100>-150", 13, false)]
    [TestCase("y = (1<2<x>-100>-150) == (1<2<x>-100>-150) == true",13, true)]
    public void SingleVariableEquation(string expr, double arg, object expected) =>
        expr.AssertConcurrentHardcore(runtime => {
            var ySource = runtime["y"];
            var xSource = runtime["x"];
            Assert.IsTrue(ySource.IsOutput);
            Assert.IsFalse(xSource.IsOutput);
            xSource.Value = arg;
            runtime.Run();
            Assert.AreEqual(expected, ySource.FunnyValue);
        });


    [Test]
    public void CustomForeachi() {
        var expr = @"
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi (rule if (it1>t[it2]) it1 else t[it2]) ";

        expr.AssertConcurrentHardcore(
            e => e.Calc("t", new[] { 1, 2, 7, 34, 1, 2 })
                .AssertReturns("res", 34));
    }

    [Test]
    public void CustomForeachiWithUserFun() {
        var expr = @"
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            max(a, t, i) = max(a, t[i])

            res:int =  t.foreachi (rule max(it1,t,it2))";
        expr.AssertConcurrentHardcore(
            e => e.Calc("t", new[] { 1, 2, 7, 34, 1, 2 }).AssertReturns("res", 34));
    }

    [Test]
    public void CustomForeachiWithBuiltInFun() {
        var expr = @"
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi(rule max(it1,t[it2]))";
        expr.AssertConcurrentHardcore(
            e => e.Calc("t", new[] { 1, 2, 7, 34, 1, 2 }).AssertReturns("res", 34));
    }

    [Test]
    public void ConcatExperiments() =>
        "'res: '.concat((n >5).toText())".AssertConcurrentHardcore(
            e => e.Calc("n", 1.0).AssertAnonymousOut("res: False"));


    [Test]
    public void TwinArrayWithUpcast_lambdaSum() =>
        "x:byte = 4; y:real = [[0,1],[2,3],[x]].map (rule sum(it)).sum()".AssertConcurrentHardcore(
            e => e.Calc().AssertResultHas("y", 10.0));

    [Test]
    public void TwinArrayWithUpcast_lambdaConstCalculate() =>
        "x:byte = 5; y:real = [[0,1],[2,3],[x]].map (rule it.map(rule it+1).sum()).sum()".AssertConcurrentHardcore(
            e => e.Calc().AssertResultHas("y", 16.0));

    [Test]
    public void SomeFun3() =>
        @"   swapIfNotSorted(c, i)
  	                =	if   (c[i]<c[i+1]) c
  		                else c.set(i, 1)".AssertConcurrentHardcore();

    [Test]
    public void SomeFun4() =>
        @"twiceSet(arr,i,j,ival,jval)
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr, i, j)
                            = arr.twiceSet(i,j,arr[j], arr[i])

                          swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)".AssertConcurrentHardcore();

    [Test]
    public void foldOfHiOrder2() =>
        @"
                #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                 swapIfNotSorted(c, i) = if (c[i]<c[i+1]) c else c

                  # run thru array
                  # and swap every unsorted values
                  onelineSort(input) =
  	                [0..input.count()].fold(input, swapIfNotSorted)".AssertConcurrentHardcore();

    [Test]
    public void foldOfHiOrder3() =>
        @"
                #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                 swapIfNotSorted(c, i) = if (c[i]<c[i]) c else c

                 # run thru array
                 # and swap every unsorted values
                 onelineSort(input) = [0..input.count()].fold(input, swapIfNotSorted)".AssertConcurrentHardcore();

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
  	    [0..input.count()].fold(input, swapIfNotSorted)".AssertConcurrentHardcore();

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
            .AssertConcurrentHardcore(e => e.Calc().AssertReturns("i", new[] { 1, 2, 3, 4, 5 }));

    [Test]
    public void WIP() {
        var expr = @"twiceSet(arr,i,j,ival,jval)
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

                  input:int[];
                  out:int[] = [0..10].fold(input, rule onelineSort(it1))
                  ";
        TraceLog.WithTrace(() => {
            Funny.Hardcore.Build(expr);
        });
    }

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
        .AssertConcurrentHardcore(e => e
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
            expr.AssertConcurrentHardcore(e => e.Calc()));
    }
}
