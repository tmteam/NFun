using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests
{
    public class HellTests
    {
        [SetUp] public void Initialize() => TraceLog.IsEnabled = true;
        [TearDown] public void Deinitiazlize() => TraceLog.IsEnabled = false;

        [Test]
        public void CustomForeachi()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)
            
            res:int =  t.foreachi (fun if (it1>t[it2]) it1 else t[it2]) ";

            expr.Calc("t", new[] {1, 2, 7, 34, 1, 2})
                .AssertReturns("res", 34);
        }

        [Test]
        public void CustomForeachiWithUserFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            max(a, t, i) = max(a, t[i])             

            res:int =  t.foreachi (fun max(it1,t,it2))";
            expr.Calc("t", new[] { 1, 2, 7, 34, 1, 2 }).AssertReturns("res", 34);
        }
        [Test]
        public void CustomForeachiWithBuiltInFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi(fun max(it1,t[it2]))";
            expr.Calc("t", new[] { 1, 2, 7, 34, 1, 2 }).AssertReturns("res", 34);
        }

        [Test]
        public void ConcatExperiments() => 
            "'res: '.concat((n >5).toText())".Calc("n",1.0).AssertOut("res: False");

        [Test]
        public void ArrayWithUpcast_lambdaConstCalculate()
        {
            var expr = "x:byte = 42; y:real[] = [1,2,x].map (fun it+1}";
            Assert.Throws<FunnyParseException>(() => expr.Build());
            //todo Support upcast
            //FunBuilder.Build(expr).Calculate().AssertHas(VarVal.New("y",new []{2.0,3.0, 43.0}));
        }
        
        [Test]
        public void TwinArrayWithUpcast_lambdaSum() => 
            "x:byte = 4; y:real = [[0,1],[2,3],[x]].map (fun sum(it)).sum()".AssertResultHas("y", 10.0);

        [Test]
        public void TwinArrayWithUpcast_lambdaConstCalculate() => 
            "x:byte = 5; y:real = [[0,1],[2,3],[x]].map (fun it.map(fun it+1).sum()).sum()".AssertResultHas("y", 16.0);

        [Test]

        public void SomeFun3() =>
            @"   swapIfNotSorted(c, i)
  	                =	if   (c[i]<c[i+1]) c
  		                else c.set(i, 1)".Build();

        [Test]
        public void SomeFun4() =>
            @"twiceSet(arr,i,j,ival,jval)
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr, i, j) 
                            = arr.twiceSet(i,j,arr[j], arr[i])
                          
                          swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)".Build();

        [Test]
        public void foldOfHiOrder2() =>
            @"
                #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                 swapIfNotSorted(c, i) = if (c[i]<c[i+1]) c else c

                  # run thru array 
                  # and swap every unsorted values
                  onelineSort(input) =  
  	                [0..input.count()].fold(input, swapIfNotSorted)".Build();

        [Test]
        public void foldOfHiOrder3() =>
            @"
                #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                 swapIfNotSorted(c, i) = if (c[i]<c[i]) c else c

                 # run thru array 
                 # and swap every unsorted values
                 onelineSort(input) = [0..input.count()].fold(input, swapIfNotSorted)".Build();

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
  	                        [0..input.count()].fold(input, swapIfNotSorted)".Build();

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
  			                fun onelineSort(it1))

                  i:int[]  = [1,4,3,2,5].bubbleSort()".AssertReturns("i", new[] { 1, 2, 3, 4, 5 });

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
                .Calc("x",42).AssertResultHas(
                    ("i", 42),
                    ("r", 4200.0),
                    ("t", "42"),
                    ("tr", "24"),
                    ("ia", new[] {1, 2, 3, 42}),
                    ("ir", new[] {1.0, 2.0, 42.0}),
                    ("c", 123),
                    ("d", "mama ja pokakal"),
                    ("etext", "")
                );

        [Test]
        public void TestEverything()
        {
            var expr = @"       twiceSet(arr,i,j,ival,jval) = arr.set(i,ival).set(j,jval)

                          #swap elements i,j in array arr  
                          swap(arr, i, j) 
                            = arr.twiceSet(i,j,arr[j], arr[i])
                          
                          #swap elements i, i+1 if they are not sorted
                          swapIfNotSorted(c, i) =	if(c[i]<c[i+1]) c else c.swap(i, i+1)

                          # run thru array and swap every unsorted values
                          onelineSort(input) =  [0..input.count()-2].fold(input, swapIfNotSorted)		

                          bubbleSort(input)= [0..input.count()-1].fold(input, fun onelineSort(it1))

                          #body  
                          ins:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53]
                          rns:real[] = ins
                          tns  = ins.filter(fun it%2==0).map(toText).concat(['vasa','kate'])
                        
                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = tns == tns

                          myOr(a,b):bool = a or b  
                          k =  [0..100].map(fun i and r or t xor i).fold(myOr)

                          mySum(a,b) = a + b  
                          j =  [0..100].map(fun (ins[1]+ it- ins[2])/it).fold(mySum);
                   ";
            var res = expr.Calc();

        }
    }
}
