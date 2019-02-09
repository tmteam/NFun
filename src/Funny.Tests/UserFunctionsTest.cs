using System;
using Funny.Runtime;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class UserFunctionsTest
    {
        [TestCase("inc(a) = a+1\r y = inc(2)",3)]
        [TestCase("inc(y) = y+1\r y = inc(2)",3)]
        [TestCase("mult(a,b) = a*b \r y = mult(3,4)+1",13)]
        [TestCase("div(a,b) = a/b  \r mult(a,b) = a*b         \r y = mult(3,4)+div(4,2)",14)]
        [TestCase("div(a,b) = a/b  \r div3(a,b,c) = div(a,b)/c\r y = div3(16,4,2)",2)]
        public void ConstantEquatation_NonRecursiveFunction(string expr, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }

        [TestCase("plus3(a,b,c) = plus(a,b)+c \r plus(a,b) = a+b  \r y = plus3(16,4,2)",22)]
        public void ConstantEquatation_ReversedImplementationsOfFunctions(string expr, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }
        
        
        [TestCase(
            "max3(a,b,c) =  max(max(a,b),c) \r max(a,b)= if a<b then b else a\r y = max3(16,32,2)",32)]
        [TestCase(
            "fact(a) = if a<2 then 1 else a*fact(a-1) \r y = fact(5)",5*4*3*2*1)]
        public void ConstantEquatation_RecFunctions(string expr, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }
        [TestCase(1,1)]
        [TestCase(2,1)]
        [TestCase(3,2)]
        [TestCase(4,3)]
        [TestCase(5,5)]
        [TestCase(6,8)]
        [TestCase(7,13)]
        [TestCase(8,21)]
        [TestCase(9,34)]

        public void ClassicRecFibonachi(int x, int y)
        {
            string text =
                @"fibrec(n, iter, p1,p2) =
                          if n >iter then fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if n<3 then 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = Interpreter.BuildOrThrow(text);
                runtime.Calculate(Var.New("x",x)).AssertReturns(0.00001, Var.New("y", y));    
        }
        
        [TestCase(1,1)]
        [TestCase(2,1)]
        [TestCase(3,2)]
        [TestCase(4,3)]
        [TestCase(5,5)]
        [TestCase(6,8)]
        [TestCase(7,13)]
        [TestCase(8,21)]
        [TestCase(9,34)]
        public void PrimitiveRecFibonachi(int x, int y)
        {
            string text =
                @"  
                   fib(n) = if n<3 then 1 else fib(n-1)+fib(n-2)
                   y = fib(x)";
            var runtime = Interpreter.BuildOrThrow(text);
            runtime.Calculate(Var.New("x",x)).AssertReturns(0.00001, Var.New("y", y));    
        }
    }
}