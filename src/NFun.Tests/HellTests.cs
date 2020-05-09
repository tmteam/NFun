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

            res:int =  t.foreachi((acc,i)-> max(acc,t,i)";

            FunBuilder.BuildDefault(expr).Calculate(
                    VarVal.New("t", new[] { 1, 2, 7, 34, 1, 2 }))
                .AssertReturns(VarVal.New("res", 34));
        }
        [Test]
        public void CustomForeachiWithBuiltInFun()
        {
            var expr = @" 
            foreachi(arr, f) = [0..arr.count()-1].reduce(arr[0], f)

            res:int =  t.foreachi((acc,i)-> max(acc,t[i])";

            FunBuilder.BuildDefault(expr).Calculate(
                    VarVal.New("t", new[] { 1, 2, 7, 34, 1, 2 }))
                .AssertReturns(VarVal.New("res", 34));
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
    }
}
