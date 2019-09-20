using System;
using NFun;
using NFun.BuiltInFunctions;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ConcreteUserFunctionsTest
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
        [TestCase("conv(x:int):real = x; y = conv(2);", 2.0)] 

       //todo toString
        // [TestCase("myconcat(a:text, b:text):text = a.strConcat(b) \r  y = myconcat(\"my\",\"test\")","mytest")]
       // [TestCase("myconcat(a:text, b:text):text = a.strConcat(b) \r  y = myconcat(1,\"test\")","1test")]
       // [TestCase("myconcat(a:text, b:text):text = a.strConcat(b) \r  y = myconcat(1,2)","12")]
       // [TestCase("myconcat(a:text, b):text = a.strConcat(b)\r  y = myconcat(1,2.5)","12.5")]
        [TestCase("arr(a:real[]):real[] = a    \r  y = arr([1.0,2.0])",new[]{1.0,2.0})]
        [TestCase("arr(a:real[]):real[] = a.concat(a) \r  y = arr([1.0,2.0])",new[]{1.0,2.0,1.0,2.0})]
        [TestCase("arr(a:int[]):int[] = a \r  y = arr([1,2])",new[]{1,2})]
        [TestCase("arr(a:text[]):text[] = a.concat(a) \r  y = arr(['qwe','rty'])",new[]{"qwe","rty","qwe","rty"})]
       public void TypedConstantEquation_NonRecursiveFunction(string expr, object expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(Var.New("y", expected));
        }
        
        
        [TestCase("_inc(a) = a+1.0\r y = _inc(2.0)",3.0)]
        [TestCase("_inc(a) = a+1\r y = _inc(2)",3)]
        [TestCase("_inc(y) = y+1\r y = _inc(2)",3)]
        [TestCase("mult2(a,b) = a*b \r y = mult2(3,4)+1",13)]
        [TestCase("div2(a,b) = a/b  \r mult2(a,b) = a*b         \r y = mult2(3,4)+div2(4,2)",14)]
        [TestCase("div2(a,b) = a/b  \r div3(a,b,c) = div2(a,b)/c\r y = div3(16,4,2)",2)]
        public void ConstantEquation_NonRecursiveFunction(string expr, double expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }

        [TestCase("plus3(a,b,c) = plus2(a,b)+c \r plus2(a,b) = a+b  \r y = plus3(16,4,2)",22)]
        public void ConstantEquation_ReversedImplementationsOfFunctions(string expr, double expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }
        
        
        [TestCase(
            "max3(a,b,c) =  max2(max2(a,b),c) \r max2(a,b)= if (a<b) b else a\r y = max3(16,32,2)",32)]
        [TestCase(
            "fact(a) = if (a<2) 1 else a*fact(a-1) \r y = fact(5)",5*4*3*2*1)]
        public void ConstantEquation_RecFunctions(string expr, double expected)
        {
            var runtime = FunBuilder.BuildDefault(expr);
            runtime.Calculate().AssertReturns(0.00001, Var.New("y", expected));
        }

        [TestCase(1,1)]
        [TestCase(2,1)]
        [TestCase(3,2)]
        [TestCase(4,3)]
        [TestCase(5,5)]
        [TestCase(6,8)]
        [TestCase(7,13)]
        public void ClassicRecFibonachi_specifyOutputType(int x, int y)
        {
            string text =
                @"fibrec(n:int, iter, p1,p2):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(Var.New("x",x)).AssertReturns(0.00001, Var.New("y", y));    
        }
        
        [TestCase(1,1)]
        [TestCase(2,1)]
        [TestCase(3,2)]
        [TestCase(4,3)]
        [TestCase(5,5)]
        [TestCase(6,8)]
        [TestCase(7,13)]
        public void ClassicRecFibonachi_specifyNType(double x, double y)
        {
            string text =
                @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
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
        public void ClassicRecFibonachi(double x, double y)
        {
            string text =
                @"fibrec(n, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
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
                   fib(n) = if (n<3) 1 else fib(n-1)+fib(n-2)
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
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
                   fib(n:int):int = if (n<3) 1 else fib(n-1)+fib(n-2)
                   x:int
                   y = fib(x)";
            var runtime = FunBuilder.BuildDefault(text);
            runtime.Calculate(Var.New("x",x)).AssertReturns(Var.New("y", y));    
        }
        
        [TestCase("y = raise(1)\r raise(x) = raise(x)")]
        
        public void StackOverflow_throws_FunStackOverflow(string text)
        {
            Assert.Throws<FunRuntimeStackoverflowException>(
                () => FunBuilder.BuildDefault(text).Calculate());
        }
        
        [TestCase("y = f(1)\r f(x) = g(x) \r g(x) = f(x)")]
        [TestCase("y = f(1)\r f(x) = g(x) \r g(x) = l(x)\r l(x) = f(x)")]
        [TestCase("y(1)=1")]
        [TestCase("y(x,y=1")]
        [TestCase("y(x y)=1")]
        [TestCase("y(x, l) x+l")]
        [TestCase("y(x,  l) ==x+l")]
        [TestCase("y(x, l)) =x+l")]
        [TestCase("y(x, x) =x")]
        [TestCase("y(x, x) =1.0")]
        [TestCase("y(x:int, x:int):int =1")]
        [TestCase("1y(x, l) =x+l")]
        [TestCase("(y(x, l)) =x+l")]
        [TestCase("(y(x, l)) =x+g(c)=12")]
        [TestCase("y(x, l) = y(x)")]
        [TestCase("y(x, l) = y(1,2")]
        [TestCase("y(x, l) = (1,2)")]
        [TestCase("y(x, l) = 1,2")]
        [TestCase("y(x, l) = 1,2*3")]
        [TestCase("y(x, l) = 4*(1,2)")]
        [TestCase("y(, l) = 1")]
        [TestCase("y(x, (l)) = 1.0")]
        [TestCase("y((x)) = x*2")]
        [TestCase("y(,) = 2")]
        [TestCase("y(x) = 2*z")]
        [TestCase("y(x) = 2*y")]
        [TestCase("y(x)=")]        
        [TestCase("y(x)-1")]
        [TestCase("y:int(x)-1")]
        [TestCase("y(x):foo=x")]
        [TestCase("y(x+1)=x")]
        [TestCase("y(,x)=x")]
        [TestCase("y(x,)=x")]
        [TestCase("y(x,1)=x")]
        [TestCase("y(1)=x")]
        [TestCase("y(x:foo)=x")]
        [TestCase("y(x:int)= x+\"vasa\"")]
        [TestCase("y(x:int)= x+1.0\n out = y(\"test\")")]
        [TestCase("y(x:real[)= x")]
        [TestCase("y(x:foo[])= x")]
        [TestCase("y(x:real])= x")]
        [TestCase("y(x):real]= x")]
        [TestCase("y(x):real[= x")]
        [TestCase("a(x)=x\r a(y)=y\r")]
        [TestCase("(x)=x\r y = out(x)\r")]
        [TestCase("f(i,j,k) = 12.0 \r y = f(((1,2),3)->i+j)")]
        [TestCase("f((i,j),k) = 12.0 \r y = f(((1,2),3)->i+j)")]
        [TestCase("f(x*2) = 12.0 \r y = f(3)")]
        [TestCase("f(x*2) = 12.0")]
        [TestCase("y(x):real= 'vasa'")]
        [TestCase("j = 1 y(x)= x+1")]
        [TestCase("y:real(x)= 1")]
        [TestCase("y:real(x:real)= 1")]
        [TestCase("y:real(x):real= 1")]

        public void ObviousFails(string expr){
            Assert.Throws<FunParseException>(()=>FunBuilder.BuildDefault(expr));
        }
    }
}