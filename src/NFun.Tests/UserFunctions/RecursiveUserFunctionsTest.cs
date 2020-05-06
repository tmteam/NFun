using NFun;
using NFun.Exceptions;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.UserFunctions
{
    [TestFixture]
    public class RecursiveUserFunctionsTest
    {
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 6)]
        [TestCase(4, 24)]
        [TestCase(5, 120)]
        [TestCase(6, 720)]
        public void FactorialConcrete(int x, int y)
        {
            string text =
                @"fact(n):int = if(n<=1) 1 else fact(n-1)*n
                  y = fact(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 6)]
        [TestCase(4, 24)]
        [TestCase(5, 120)]
        [TestCase(6, 720)]
        public void FactorialGeneric(double x, double y)
        {
            string text =
                @"fact(n) = if(n<=1) 1 else fact(n-1)*n
                  y = fact(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 5)]
        [TestCase(6, 8)]
        [TestCase(7, 13)]
        [TestCase(8, 21)]
        [TestCase(9, 34)]
        public void ClassicRecFibonachi(double x, double y)
        {
            string text =
                @"fibrec(n, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 5)]
        [TestCase(6, 8)]
        [TestCase(7, 13)]
        [TestCase(8, 21)]
        [TestCase(9, 34)]
        public void PrimitiveRecFibonachi(double x, double y)
        {
            string text =
                @"  
                   fib(n) = if (n<3) 1 else fib(n-1)+fib(n-2)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }

        [TestCase(1, 1)]
        [TestCase(3, 2)]
        [TestCase(6, 8)]
        [TestCase(9, 34)]
        public void PrimitiveRecFibonachi_Typed(int x, int y)
        {
            string text =
                @"  
                   fib(n:int):int = if (n<3) 1 else fib(n-1)+fib(n-2)
                   x:int
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(VarVal.New("y", y));
        }

        [TestCase("y = raise(1)\r raise(x) = raise(x)")]

        public void StackOverflow_throws_FunStackOverflow(string text)
        {
            Assert.Throws<FunRuntimeStackoverflowException>(
                () => FunBuilder.BuildDefault(text).Calculate());
        }

        [TestCase(
            "max3(a,b,c) =  max2(max2(a,b),c) \r max2(a,b)= if (a<b) b else a\r y = max3(16,32,2)", 32)]
        [TestCase(
            "fact(a) = if (a<2) 1 else a*fact(a-1) \r y = fact(5)", 5 * 4 * 3 * 2 * 1)]

        public void ConstantEquationOfReal_RecFunctions(string expr, double expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(0.00001, VarVal.New("y", expected));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 6)]
        [TestCase(4, 24)]
        [TestCase(5, 120)]
        [TestCase(6, 720)]
        public void RecFactorial_Concrete(int x, int y)
        {
            string text =
                @"fact(n:int):int = if(n>1) n*fact(n-1)
                                    else 1
                    
                   y = fact(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 5)]
        [TestCase(6, 8)]
        [TestCase(7, 13)]
        public void ClassicRecFibonachi_AllTypesConcrete(int x, int y)
        {
            string text =
                @"fibrec(n:int, iter:int, p1:int,p2:int):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n:int) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 5)]
        [TestCase(6, 8)]
        [TestCase(7, 13)]
        public void ClassicRecFibonachi_specifyOutputType(int x, int y)
        {
            string text =
                @"fibrec(n:int, iter, p1,p2):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }

        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 5)]
        [TestCase(6, 8)]
        [TestCase(7, 13)]
        public void ClassicRecFibonachi_specifyNType(double x, double y)
        {
            string text =
                @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(VarVal.New("x", x)).AssertReturns(0.00001, VarVal.New("y", y));
        }
    }
}