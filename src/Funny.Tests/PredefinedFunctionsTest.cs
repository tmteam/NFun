using System;
using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class PredefinedFunctionsTest
    {
        [TestCase("y = abs(1)",1)]
        [TestCase("y = abs(-1)",1)]
        [TestCase("y = abs(1.0)",1.0)]
        [TestCase("y = abs(-1.0)",1.0)]
        [TestCase("y = add(1,2)",3.0)]
        [TestCase("y = add(add(1,2),add(3,4))",10.0)]
        [TestCase("y = abs(1-4)",3)]
        [TestCase("y = 15 - add(abs(1-4), 7)",5.0)]
        [TestCase("y = pi()",Math.PI)]
        [TestCase("y = e()",Math.E)]
        [TestCase("y = count([1,2,3])",3)]
        [TestCase("y = count([])",0)]
        [TestCase("y = count([1.0,2.0,3.0])",3)]
        [TestCase("y = count([[1,2],[3,4]])",2)]
        [TestCase("y = avg([1,2,3])",2.0)]
        [TestCase("y = avg([1.0,2.0,6.0])",3.0)]
        [TestCase("y = sum([1,2,3])",6)]
        [TestCase("y = sum([1.0,2.5,6.0])",9.5)]
        [TestCase("y = max([1.0,10.5,6.0])",10.5)]
        [TestCase("y = max([1,-10,0])",1)]
        [TestCase("y = max(1.0,3.4)",3.4)]
        [TestCase("y = max(4,3)",4)]
        [TestCase("y = min([1.0,10.5,6.0])",1.0)]
        [TestCase("y = min([1,-10,0])",-10)]
        [TestCase("y = min(1.0,3.4)",1.0)]
        [TestCase("y = min(4,3)",3)]
        [TestCase("y = median([1.0,10.5,6.0])",6.0)]
        [TestCase("y = median([1,-10,0])",0)]        
        [TestCase( "y = [1.0,2.0,3.0]|>any",true)]
        [TestCase( "y = any([])",false)]
        [TestCase( "y = [4,3,5,1] |> sort",new []{1,3,4,5})]
        [TestCase( "y = [4.0,3.0,5.0,1.0] |> sort",new []{1.0,3.0,4.0,5.0})]
        [TestCase( "y = ['4.0','3.0','5.0','1.0'] |> sort",new []{"1.0","3.0","4.0","5.0"})]
        public void ConstantEquationWithPredefinedFunction(string expr, object expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(0.00001, Var.New("y", expected));
        }
       
        
        //todo fun overloads
        //[TestCase("y = [0,7,1,2,3] |> fold(max)", 7)]
        [TestCase("mysum(x:int, y:int):int = x+y \r" +
                  "y = [0,7,1,2,3] |> fold(mysum)", 13)]
        [TestCase( @"rr(x:real):bool = x>10
                     y = filter([11.0,20.0,1.0,2.0],rr)",new[]{11.0,20.0})]
        [TestCase( @"ii(x:int):bool = x>10
                     y = filter([11,20,1,2],ii)",new[]{11,20})]
        [TestCase( @"ii(x:int):int = x*x
                     y = map([1,2,3],ii)",new[]{1,4,9})]
        [TestCase( @"ii(x:int):real = x/2
                     y = map([1,2,3],ii)",new[]{0.5,1.0,1.5})]
        [TestCase( @"isodd(x:int):bool = (x%2) == 0
                     y = map([1,2,3],isodd)",new[]{false, true,false})]
        [TestCase( "y = [1.0,2.0,3.0]|>any((i)=> i == 1.0)",true)]
        [TestCase( "y = [1.0,2.0,3.0]|>any((i)=> i == 0.0)",false)]
        [TestCase( "y = [1.0,2.0,3.0]|>all((i)=> i >0)",true)]
        [TestCase( "y = [1.0,2.0,3.0]|>all((i)=> i >1.0)",false)]
        public void HiOrderFunConstantEquatation(string expr, object expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(Var.New("y", expected));
        }
        //[TestCase( @"y = filter([11,20,1,2], (i) => i>10)",new[]{11,20})]
        [TestCase( @"y = [11.0,20.0,1.0,2.0]|>filter(i => i>10)",new[]{11.0,20.0})]
        [TestCase( @"y = [11.0,20.0,1.0,2.0]|>filter((i) => i>10)",new[]{11.0,20.0})]
        [TestCase( @"y = [11,20,1,2]|>filter((i:int) => i>10)",new[]{11,20})]
        [TestCase( @"y = [11,20,1,2]|>filter(i:int => i>10)",new[]{11,20})]
        //[TestCase( @"y = [11,20,1,2]|>filter(i => i>10)",new[]{11,20})]
        //[TestCase( @"y = [11,20,1,2]|>filter((i) => i>10)",new[]{11,20})]
        [TestCase( @"y = map([1,2,3], i:int  =>i*i)",new[]{1,4,9})]
        [TestCase( @"y = [1,2,3] |> map(i:int=>i*i)",new[]{1,4,9})]
        //[TestCase( @"y = [1,2,3] |> map(i=>i*i)",new[]{1,4,9})]
        [TestCase( @"y = [1.0,2.0,3.0] |> map(i=>i*i)",new[]{1.0,4.0,9.0})]
        [TestCase( @"y = [1.0,2.0,3.0] |> fold((i,j)=>i+j)",6.0)]
        [TestCase( @"y = fold([1.0,2.0,3.0],(i,j)=>i+j)",6.0)]
        [TestCase( @"y = [1,2,3] |> fold((i:int, j:int)=>i+j)",6)]
        [TestCase( @"y = fold([1,2,3],(i:int, j:int)=>i+j)",6)]

        public void AnonymousFunctions(string expr, object expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(Var.New("y", expected));
        }
        
        [TestCase("y = take([1,2,3,4,5],3)",new []{1,2,3})]        
        [TestCase("y = take([1.0,2.0,3.0,4.0,5.0],4)",new []{1.0,2.0,3.0,4.0})]        
        [TestCase("y = take([1.0,2.0,3.0],20)",new []{1.0,2.0,3.0})]        
        [TestCase("y = take([1.0,2.0,3.0],0)",new double[0])]        
        [TestCase("y = skip([1,2,3,4,5],3)",new []{4,5})]        
        [TestCase("y = skip(['1','2','3','4','5'],3)",new []{"4","5"})]        
        [TestCase("y = skip([1.0,2.0,3.0,4.0,5.0],4)",new []{5.0})]        
        [TestCase("y = skip([1.0,2.0,3.0],20)",new double[0])]        
        [TestCase("y = skip([1.0,2.0,3.0],0)",new []{1.0,2.0,3.0})]        
        [TestCase("y = repeat('abc',3)",new []{"abc","abc","abc"})]        
        [TestCase("y = repeat('abc',0)",new string[0])]        
        [TestCase("y = take(skip([1.0,2.0,3.0],1),1)",new []{2.0})]        
        [TestCase("mypage(x:int[]):int[] = take(skip(x,1),1) \r y = mypage([1,2,3]) ",new []{2})]        
        [TestCase("y = [1,2,3]|> reverse",new[]{3,2,1})]
        [TestCase("y = [1,2,3]|> reverse |> reverse",new[]{1,2,3})]
        [TestCase("y = []|> reverse",new object[0])]

        public void ConstantEquationWithGenericPredefinedFunction(string expr, object expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate()
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        
        [TestCase("y = abs(x)",1,1)]
        [TestCase("y = abs(-x)",-1,1)]
        [TestCase("y = add(x,2)",1,3)]
        [TestCase("y = add(1,x)",2,3)]
        [TestCase("y = add(add(x,x),add(x,x))",1,4)]
        [TestCase("y = abs(x-4)",1,3)]
        public void EquationWithPredefinedFunction(string expr, double arg, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate(Var.New("x", arg))
                .AssertReturns(0.00001, Var.New("y", expected));
        }
        
        [TestCase("y = pi(")]
        [TestCase("y = pi(1)")]
        [TestCase("y = abs(")]
        [TestCase("y = abs)")]
        [TestCase("y = abs()")]
        [TestCase("y = abs(1,)")]
        [TestCase("y = abs(1,,2)")]
        [TestCase("y = abs(,,2)")]
        [TestCase("y = abs(,,)")]
        [TestCase("y = abs(2,)")]
        [TestCase("y = abs(1,2)")]
        [TestCase("y = abs(1 2)")]
        [TestCase("y = add(")]
        [TestCase("y = add()")]
        [TestCase("y = add(1)")]
        [TestCase("y = add 1")]
        [TestCase("y = add(1,2,3)")]
        [TestCase("y = [1.0] |> fold(((i,j)=>i+j)")]
        [TestCase("f(i,j,k) = 12.0 \r y = f(((1,2),3)=>i+j)")]
        [TestCase("f((i,j),k) = 12.0 \r y = f(((1,2),3)=>i+j)")]
        [TestCase("y = fold(((i,j),k)=>i+j)")]

        [TestCase("y = avg(['1','2','3'])")]
        [TestCase("y= max([])")]
        [TestCase("y= max(['a','b'])")]
        [TestCase("y= max('a','b')")]
        [TestCase("y= max(1,2,3)")]
        [TestCase("y= max(1,true)")]
        [TestCase("y= max(1,(j)=>j)")]

        [TestCase( @"y = [1.0,2.0,3.0] |> fold((i*2,j)=>i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0] |> fold((2,j)=>i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0] |> fold((j)=>i+j)")]
        [TestCase( @"y = [1.0,2.0,3.0] |> fold((j)=>j)")]
        [TestCase( @"y = [1.0,2.0,3.0] |> fold((i,j,k)=>i+j+k)")]
        [TestCase( @"y = [1.0,2.0,3.0] |> fold((i,j)=>i+j+k)")]
        [TestCase( @"y = [1.0,2.0,3.0] |> fold((i,j)=> k)")]

        public void ObviouslyFails(string expr) =>
            Assert.Throws<ParseException>(
                ()=> Interpreter.BuildOrThrow(expr));
        
    }
}