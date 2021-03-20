﻿using NFun;
using NFun.Exceptions;
using NFun.Tic;
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
            
            res:int =  t.foreachi {if (it1>t[it2]) it1 else t[it2]} ";

            FunBuilder.Build(expr).Calculate(
                VarVal.New("t",new[]{1,2,7,34,1,2}))
                .AssertReturns(VarVal.New("res",34));
        }

        [Test]
        public void CustomForeachiWithUserFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            max(a, t, i) = max(a, t[i])             

            res:int =  t.foreachi {max(it1,t,it2)}";

            FunBuilder.Build(expr).Calculate(
                    VarVal.New("t", new[] { 1, 2, 7, 34, 1, 2 }))
                .AssertReturns(VarVal.New("res", 34));
        }
        [Test]
        public void CustomForeachiWithBuiltInFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].fold(arr[0], f)

            res:int =  t.foreachi{ max(it1,t[it2])}";

            FunBuilder.Build(expr).Calculate(
                    VarVal.New("t", new[] { 1, 2, 7, 34, 1, 2 }))
                .AssertReturns(VarVal.New("res", 34));
        }

        [Test]
        public void ConcatExperiments()
        {
            var expr = "'res: '.concat((n >5).toText())";
            FunBuilder.Build(expr).Calculate(VarVal.New("n",1.0)).AssertOutEquals("res: False");
        }
        [Test]
        public void ArrayWithUpcast_lambdaConstCalculate()
        {
            var expr = "x:byte = 42; y:real[] = [1,2,x].map {it+1}";
            Assert.Throws<FunParseException>(() => FunBuilder.Build(expr));
            //todo Support upcast
            //FunBuilder.Build(expr).Calculate().AssertHas(VarVal.New("y",new []{2.0,3.0, 43.0}));
        }
        
        [Test]
        public void TwinArrayWithUpcast_lambdaSum()
        {
            var expr = "x:byte = 4; y:real = [[0,1],[2,3],[x]].map {sum(it)}.sum()";
            FunBuilder.Build(expr).Calculate().AssertHas(VarVal.New("y",10.0));
        }
        
        [Test]
        public void TwinArrayWithUpcast_lambdaConstCalculate()
        {
            var expr = "x:byte = 5; y:real = [[0,1],[2,3],[x]].map {it.map{it+1}.sum()}.sum()";
            FunBuilder.Build(expr).Calculate().AssertHas(VarVal.New("y",16.0));
        }

        [Test]

        public void SomeFun3()
        {
            var expr = @"   swapIfNotSorted(c, i)
  	                        =	if   (c[i]<c[i+1]) c
  		                        else c.set(i, 1)";
            FunBuilder.Build(expr);

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

            FunBuilder.Build(expr);
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

            FunBuilder.Build(expr);
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

            FunBuilder.Build(expr);
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

            FunBuilder.Build(expr);
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
  			                        {onelineSort(it1)})

                          
                          i:int[]  = [1,4,3,2,5].bubbleSort()";


            FunBuilder.Build(expr).Calculate()
                .AssertReturns(VarVal.New("i", new[] { 1, 2, 3, 4, 5 }));

        }

        [Test]
        public void ManyOutputsTest()
        {
            var expr ="x:int; " +
                      "i = x; " +
                      "r = x*100.0; " +
                      "t = x.toText(); " +
                      "tr = x.toText().reverse(); " +
                      "ia = [1,2,3,x];" +
                      "ir = [1.0, 2.0, x];" +
                      "c = 123;" +
                      "d = 'mama ja pokakal';" +
                      "etext = ''";
            var res = FunBuilder.Build(expr).Calculate(VarVal.New("x",42));
            Assert.Multiple(() =>
            {
                res.AssertHas(VarVal.New("i", 42));
                res.AssertHas(VarVal.New("r", 4200.0));
                res.AssertHas(VarVal.New("t", "42"));
                res.AssertHas(VarVal.New("tr", "24"));
                res.AssertHas(VarVal.New("ia", new int[] {1, 2, 3, 42}));
                res.AssertHas(VarVal.New("ir", new[] {1.0, 2.0, 42.0}));
                res.AssertHas(VarVal.New("c", 123.0));
                res.AssertHas(VarVal.New("d", "mama ja pokakal"));
                res.AssertHas(VarVal.New("etext", ""));
            });
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

                          bubbleSort(input)= [0..input.count()-1].fold(input, {onelineSort(it1)})

                          #body  
                          ins:int[]  = [1,5,3,5,6,1,2,100,0,3,2,10,3,50,6,42,43,53]
                          rns:real[] = ins
                          tns  = ins.filter{it.rema(2)==0}.map(toText).concat(['vasa','kate'])
                        
                          i  = ins.bubbleSort() == ins.reverse().sort()
                          r  = rns.bubbleSort() == rns.sort()
                          t  = tns == tns

                          myOr(a,b):bool = a or b  
                          k =  [0..100].map{i and r or t xor i}.fold(myOr)

                          mySum(a,b) = a + b  
                          j =  [0..100].map{(ins[1]+ it- ins[2])/it}.fold(mySum);
                   ";
            var res = FunBuilder.Build(expr).Calculate();

        }
    }
}
