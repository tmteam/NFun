using System;
using Funny.Runtime;
using Funny.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class CustomFunctionsTest
    {
        [Test]
        public void TestOfTheTest()
        {
            Assert.Pass();
        }
        [TestCase("myor(a:bool, b:bool):bool = a or b \r y = myor(true,false)",true)]
        [TestCase("mysum(a:int, b:int):int = a + b \r    y = mysum(1,2)",3)]
        [TestCase("mysum(a:real, b:real):real = a + b \r y = mysum(1,2)",3.0)]
        [TestCase("mysum(a:int, b:real):real = a + b \r  y = mysum(1,2)",3.0)]
        [TestCase("mysum(a:int, b:real):real = a + b \r  y = mysum(1,2.0)",3.0)]
        [TestCase("mysum(a:real, b:int):real = a + b \r  y = mysum(1,2)",3.0)]
        [TestCase("myconcat(a:text, b:text):text = a + b \r  y = myconcat(\"my\",\"test\")","mytest")]
        [TestCase("myconcat(a:text, b:text):text = a + b \r  y = myconcat(1,\"test\")","1test")]
        [TestCase("myconcat(a:text, b:text):text = a + b \r  y = myconcat(1,2)","12")]
        [TestCase("myconcat(a:text, b):text = a + b \r  y = myconcat(1,2.5)","12.5")]
        [TestCase("arr(a:real[]):real[] = a    \r  y = arr([1.0,2.0])",new[]{1.0,2.0})]
        [TestCase("arr(a:real[]):real[] = a::a \r  y = arr([1.0,2.0])",new[]{1.0,2.0,1.0,2.0})]
        [TestCase("arr(a:int[]):int[] = a \r  y = arr([1,2])",new[]{1,2})]
        [TestCase("arr(a:text[]):text[] = a::a \r  y = arr(['qwe','rty'])",new[]{"qwe","rty","qwe","rty"})]
        public void TypedConstantEquation_NonRecursiveFunction(string expr, object expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(Var.New("y", expected));
        }
        
        
        [TestCase("inc(a) = a+1.0\r y = inc(2.0)",3.0)]
        [TestCase("inc(a) = a+1\r y = inc(2)",3)]
        [TestCase("inc(y) = y+1\r y = inc(2)",3)]
        [TestCase("mult(a,b) = a*b \r y = mult(3,4)+1",13)]
        [TestCase("div(a,b) = a/b  \r mult(a,b) = a*b         \r y = mult(3,4)+div(4,2)",14)]
        [TestCase("div(a,b) = a/b  \r div3(a,b,c) = div(a,b)/c\r y = div3(16,4,2)",2)]
        public void ConstantEquation_NonRecursiveFunction(string expr, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }

        [TestCase("plus3(a,b,c) = plus(a,b)+c \r plus(a,b) = a+b  \r y = plus3(16,4,2)",22)]
        public void ConstantEquation_ReversedImplementationsOfFunctions(string expr, double expected)
        {
            var runtime = Interpreter.BuildOrThrow(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }
        
        
        [TestCase(
            "max3(a,b,c) =  max2(max2(a,b),c) \r max2(a,b)= if a<b then b else a\r y = max3(16,32,2)",32)]
        [TestCase(
            "fact(a) = if a<2 then 1 else a*fact(a-1) \r y = fact(5)",5*4*3*2*1)]
        public void ConstantEquation_RecFunctions(string expr, double expected)
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
        public void ClassicRecFibonachi(double x, double y)
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
        public void PrimitiveRecFibonachi(double x, double y)
        {
            string text =
                @"  
                   fib(n) = if n<3 then 1 else fib(n-1)+fib(n-2)
                   y = fib(x)";
            var runtime = Interpreter.BuildOrThrow(text);
            runtime.Calculate(Var.New("x",x)).AssertReturns(0.00001, Var.New("y", y));    
        }
        [TestCase(1,1)]
        [TestCase(3,2)]
        [TestCase(6,8)]
        [TestCase(9,34)]
        public void PrimitiveRecFibonachi_Typed(int x, int y)
        {
            string text =
                @"  
                   fib(n:int):int = if n<3 then 1 else fib(n-1)+fib(n-2)
                   x:int
                   y = fib(x)";
            var runtime = Interpreter.BuildOrThrow(text);
            runtime.Calculate(Var.New("x",x)).AssertReturns(Var.New("y", y));    
        }
        
        [TestCase("y = raise(1)\r raise(x) = raise(x)")]
        [TestCase("y = f(1)\r f(x) = g(x) \r g(x) = f(x)")]
        [TestCase("y = f(1)\r f(x) = g(x) \r g(x) = l(x)\r l(x) = f(x)")]
        public void StackOverflow_throws_FunStackOverflow(string text)
        {
            Assert.Throws<FunStackoverflowException>(
                () => Interpreter.BuildOrThrow(text).Calculate());
        }

        [TestCase("y(1)=1")]
        [TestCase("y(x,y=1")]
        [TestCase("y(x y)=1")]
        [TestCase("y(x, l) x+l")]
        [TestCase("y(x,  l) ==x+l")]
        [TestCase("y(x, l)) =x+l")]
        [TestCase("1y(x, l) =x+l")]
        [TestCase("(y(x, l)) =x+l")]
        [TestCase("(y(x, l)) =x+g(c)=12")]
        [TestCase("y(x, l) = y(x)")]
        [TestCase("y(x, l) = y(1,2")]
        [TestCase("y(x, l) = (1,2)")]
        [TestCase("y(, l) = 1")]
        [TestCase("y(x, (l)) = 1")]
        [TestCase("y((x)) = x*2")]
        [TestCase("y(,) = 2")]
        [TestCase("y(x) = 2*z")]
        [TestCase("y(x) = 2*y")]
        [TestCase("y(x)=")]
        [TestCase("y(x)-1")]
        [TestCase("y:int(x)-1")]
        [TestCase("y(x):foo=x")]
        [TestCase("y(x:foo)=x")]
        [TestCase("y(x:int)= x+\"vasa\"")]
        [TestCase("y(x):real= \"vasa\"")]
        [TestCase("y(x:int)= x+1\n out = y(\"test\")")]
        [TestCase("y(x:real[)= x")]
        [TestCase("y(x:foo[])= x")]
        [TestCase("y(x:real])= x")]
        [TestCase("y(x):real]= x")]
        [TestCase("y(x):real[= x")]
        [TestCase("a(x)=x\r a(y)=y\r")]
        public void ObviousFails(string expr)
        {
            try
            {
                Interpreter.BuildOrThrow(expr);
            }
            catch (Exception e)
            {
                Assert.IsInstanceOf<ParseException>(e);
            }
        }
    }
}