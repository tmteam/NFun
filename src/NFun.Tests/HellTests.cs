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
       
    }
}
