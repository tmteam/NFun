using System;
using System.Collections.Generic;
using System.Text;
using NFun;
using NFun.TypeInferenceCalculator;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    class HellTests
    {
        [SetUp] public void Initialize() => TraceLog.IsEnabled = true;
        [TearDown] public void Deinitiazlize() => TraceLog.IsEnabled = false;

        [Test]
        public void CustomForeachi()
        {
            var expr = @" 
            foreachi(arr, f) = [1..arr.count()-1].reduce(arr[0], f)
            
            res:int =  t.foreachi((acc,i)-> if (acc>t[i]) acc else t[i] )";

            FunBuilder.BuildDefault(expr).Calculate(
                VarVal.New("t",new[]{1,2,7,34,1,2}))
                .AssertReturns(VarVal.New("res",34));
        }
        [Test]
        public void TwinGenericFunCall()
        {
            var expr = @"maxOfArray(t) = t.reduce(max)

  maxOfMatrix(t) = t.reduce(maxOfArray)

  origin = [
              [12,05,06],
              [42,33,12],
              [01,15,18]
             ] 

  res = origin.maxOfMatrix()";
            FunBuilder.BuildDefault(expr).Calculate()
                .AssertHas(VarVal.New("res", 42));
        }

        [Test]
        public void GenericRecursive()
        {
            var expr = 
                @"fact(n) = if (n==0) 0
                            if (n == 1) 1
                            else fact(n - 1) * n

                res =[0..4].map(fact)";
            FunBuilder.BuildDefault(expr).Calculate()
                .AssertHas(VarVal.New("res", 42));

        }
       

    }
}
