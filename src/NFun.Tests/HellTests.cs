using NFun;
using NFun.TypeInferenceCalculator;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
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
            
            res:int =  t.foreachi( (acc,i)-> if (acc>t[i]) acc else t[i] )";

            FunBuilder.BuildDefault(expr).Calculate(
                VarVal.New("t",new[]{1,2,7,34,1,2}))
                .AssertReturns(VarVal.New("res",34));
        }

        [Test]
        public void CustomForeachiWithUserFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            max(a, t, i) = max(a, t[i])             

            res:int =  t.foreachi((acc,i)-> max(acc,t,i))";

            FunBuilder.BuildDefault(expr).Calculate(
                    VarVal.New("t", new[] { 1, 2, 7, 34, 1, 2 }))
                .AssertReturns(VarVal.New("res", 34));
        }
        [Test]
        public void CustomForeachiWithBuiltInFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi((acc,i)-> max(acc,t[i]))";

            FunBuilder.BuildDefault(expr).Calculate(
                    VarVal.New("t", new[] { 1, 2, 7, 34, 1, 2 }))
                .AssertReturns(VarVal.New("res", 34));
        }

        [Test]
        public void ConcatExperiments()
        {
            var expr = "'res: '.concat((n >5).toText())";
            FunBuilder.BuildDefault(expr).Calculate(VarVal.New("n",1.0)).AssertOutEquals("res: False");
        }

        [Test]

        public void SomeFun3()
        {
            var expr = @"   swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.set(i, 1)";
            FunBuilder.BuildDefault(expr);

        }
        [Test]
        public void SomeFun4()
        {
            var expr = @"twiceSet(arr,i,j,ival,jval)
  	                        = arr.set(i,ival).set(j,jval)

                          swap(arr, i, j) 
                            = arr.twiceSet(i,j,arr[j], arr[i])
                          
                          swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.swap(i, i+1)";

            FunBuilder.BuildDefault(expr);
        }
        [Test]
        public void foldOfHiOrder2()
        {
            var expr = @"
                        #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                         swapIfNotSorted(c, i) = if (c[i]<c[i+1]) c else c

                          # run thru array 
                          # and swap every unsorted values
                          onelineSort(input) =  
  	                        [0..input.count()].fold(input, swapIfNotSorted)";

            FunBuilder.BuildDefault(expr);
        }

        [Test]
        public void foldOfHiOrder3()
        {
            var expr = @"
                        #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                         swapIfNotSorted(c, i) = if (c[i]<c[i]) c else c

                          # run thru array 
                          # and swap every unsorted values
                          onelineSort(input) = [0..input.count()].fold(input, swapIfNotSorted)";

            FunBuilder.BuildDefault(expr);
        }

        [Test]
        public void foldOfHiOrder()
        {
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
  	                        [0..input.count()].fold(input, swapIfNotSorted)";

            FunBuilder.BuildDefault(expr);
        }

        [Test]
        public void BubbleSortSemiConcrete()
        {
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

                          bubbleSort(input:int[]):int[]=
  	                        [0..input.count()-1]
  		                        .fold(
  			                        input, 
  			                        (c,i)-> c.onelineSort())

                          
                          i:int[]  = [1,4,3,2,5].bubbleSort()";


            FunBuilder.BuildDefault(expr).Calculate()
                .AssertReturns(VarVal.New("i", new[] { 1, 2, 3, 4, 5 }));

        }
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

                          bubbleSort(input)= [0..input.count()-1].fold(input, (c,i)-> c.onelineSort())

                          #body  
                          ins:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53]
                          rns:real[] = ins
                          tns  = ins.filter(x->x%2==0).map(toText).concat(['vasa','kate'])
                        
                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = tns == tns

                          myOr(a,b):bool = a or b  
                          k =  [0..100].map(x->i and r or t xor i).fold(myOr)

                          mySum(a,b) = a + b  
                          j =  [0..100].map(x->(ins[1]+ x- ins[2])/x).fold(mySum);
                   ";
            var res = FunBuilder.BuildDefault(expr).Calculate();

        }
        [Test]
        public void simple()
        {
            var expr = @"        
                ins:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53]
                rns: real[] = ins
                tns = ins.filter(x->x % 2 == 0).map(toText).concat(['vasa', 'kate'])

                i  = ins == ins.reverse().sort()
                r  = rns == rns.sort()
                t  = tns == tns

                
                myOr(a,b):bool = a or b  
                k =  [0..100].map(x->i and r or t xor i).fold(myOr)

                mySum(a,b) = a + b  
                j =  [0..100].map(x->(ins[1]+ x- ins[2])/x).fold(mySum);
            ";
            var res = FunBuilder.BuildDefault(expr).Calculate();

        }

    }
}
