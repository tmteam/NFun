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
            foreachi(arr, f) = [0..arr.count()-1].reduce(arr[0], f)
            
            res:int =  t.foreachi( (acc,i)-> if (acc>t[i]) acc else t[i] )";

            FunBuilder.BuildDefault(expr).Calculate(
                VarVal.New("t",new[]{1,2,7,34,1,2}))
                .AssertReturns(VarVal.New("res",34));
        }

        [Test]
        public void CustomForeachiWithUserFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].reduce(arr[0], f)

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
            foreachi(arr, f) = [0..arr.count()-1].reduce(arr[0], f)

            res:int =  t.foreachi((acc,i)-> max(acc,t[i]))";

            FunBuilder.BuildDefault(expr).Calculate(
                    VarVal.New("t", new[] { 1, 2, 7, 34, 1, 2 }))
                .AssertReturns(VarVal.New("res", 34));
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
        public void ReduceOfHiOrder2()
        {
            var expr = @"
                        #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                         swapIfNotSorted(c, i) = if (c[i]<c[i+1]) c else c

                          # run thru array 
                          # and swap every unsorted values
                          onelineSort(input) =  
  	                        [0..input.count()].reduce(input, swapIfNotSorted)";

            FunBuilder.BuildDefault(expr);
        }

        [Test]
        public void ReduceOfHiOrder3()
        {
            var expr = @"
                        #swapIfNotSorted(T_0[],Int32):T_0[]  where T_0: <>

                         swapIfNotSorted(c, i) = if (c[i]<c[i]) c else c

                          # run thru array 
                          # and swap every unsorted values
                          onelineSort(input) = [0..input.count()].reduce(input, swapIfNotSorted)";

            FunBuilder.BuildDefault(expr);
        }

        [Test]
        public void ReduceOfHiOrder()
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
  	                        [0..input.count()].reduce(input, swapIfNotSorted)";

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
  	                        [0..input.count()-2].reduce(input, swapIfNotSorted)		

                          bubbleSort(input:int[]):int[]=
  	                        [0..input.count()-1]
  		                        .reduce(
  			                        input, 
  			                        (c,i)-> c.onelineSort())

                          
                          i:int[]  = [1,4,3,2,5].bubbleSort()";


            FunBuilder.BuildDefault(expr).Calculate()
                .AssertReturns(VarVal.New("i", new[] { 1, 2, 3, 4, 5 }));

        }

        [Test]
        public void BubbleSort()
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
  	                        [0..input.count()-2].reduce(input, swapIfNotSorted)		

                          bubbleSort(input)=
  	                        [0..input.count()-1]
  		                        .reduce(
  			                        input, 
  			                        (c,i)-> c.onelineSort())

                          
                          i:int[]  = [1,4,3,2,5].bubbleSort()";


            FunBuilder.BuildDefault(expr).Calculate()
                .AssertReturns(VarVal.New("i", new[]{1,2,3,4,5}));

        }
    }
}
