using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.UserFunctions; 

[TestFixture]
public class RecursiveUserFunctionsTest {
    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 6)]
    [TestCase(4, 24)]
    [TestCase(5, 120)]
    [TestCase(6, 720)]
    public void FactorialConcrete(int x, int y) =>
        @"fact(n):int = if(n<=1) 1 else fact(n-1)*n
               y = fact(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 6)]
    [TestCase(4, 24)]
    [TestCase(5, 120)]
    [TestCase(6, 720)]
    public void FactorialGeneric(double x, double y) =>
        @"fact(n) = if(n<=1) 1 else fact(n-1)*n
              y = fact(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 5)]
    [TestCase(6, 8)]
    [TestCase(7, 13)]
    [TestCase(8, 21)]
    [TestCase(9, 34)]
    public void ClassicRecFibonachi(double x, double y) =>
        @"fibrec(n, iter, p1,p2) =
                      if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                      else p1+p2  
                
               fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
               y = fib(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 5)]
    [TestCase(6, 8)]
    [TestCase(7, 13)]
    [TestCase(8, 21)]
    [TestCase(9, 34)]
    public void PrimitiveRecFibonachi(double x, double y) =>
        @" fib(n) = if (n<3) 1 else fib(n-1)+fib(n-2)
                   y = fib(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(3, 2)]
    [TestCase(6, 8)]
    [TestCase(9, 34)]
    public void PrimitiveRecFibonachi_Typed(int x, int y) =>
        @"  
                   fib(n:int):int = if (n<3) 1 else fib(n-1)+fib(n-2)
                   x:int
                   y = fib(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase("y = raise(1)\r raise(x) = raise(x)")]
    public void StackOverflow_throws_FunStackOverflow(string text) =>
        Assert.Throws<FunnyRuntimeStackoverflowException>(() => text.Calc());

    [TestCase(
        "max3(a,b,c) =  max2(max2(a,b),c) \r max2(a,b)= if (a<b) b else a\r y = max3(16,32,2)", 32)]
    [TestCase(
        "fact(a) = if (a<2) 1 else a*fact(a-1) \r y = fact(5)", 5 * 4 * 3 * 2 * 1)]
    [TestCase("g(x) = if(x>0) g(x-1)+1 else 0; y:real = g(42.5)", 43.0)]
    public void ConstantEquationOfReal_RecFunctions(string expr, object expected) =>
        expr.AssertReturns("y", expected);
    
    [Ignore("Todo. Save generic function input type info, to have preferred type as result")]
    [Test]
    public void UserFunctionPreferedTypeIsUsedInBody() =>
        "g(x) = g(x-1); out = g(x)".Build().AssertContains("x", FunnyType.Int32);
    
    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 6)]
    [TestCase(4, 24)]
    [TestCase(5, 120)]
    [TestCase(6, 720)]
    public void RecFactorial_Concrete(int x, int y) =>
        @"fact(n:int):int = if(n>1) n*fact(n-1)
                                    else 1
                    
              y = fact(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 5)]
    [TestCase(6, 8)]
    [TestCase(7, 13)]
    public void ClassicRecFibonachi_AllTypesConcrete(int x, int y) =>
        @"fibrec(n:int, iter:int, p1:int,p2:int):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n:int) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)".Calc("x", x)
                              .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 5)]
    [TestCase(6, 8)]
    [TestCase(7, 13)]
    public void ClassicRecFibonachi_specifyOutputType(int x, int y) =>
        @"fibrec(n:int, iter, p1,p2):int =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)".Calc("x", x)
                              .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 3)]
    [TestCase(5, 5)]
    [TestCase(6, 8)]
    [TestCase(7, 13)]
    public void ClassicRecFibonachi_specifyNType(int x, double y) =>
        @"fibrec(n:int, iter, p1,p2) =
                          if (n >iter) fibrec(n, iter+1, p1+p2, p1)
                          else p1+p2  
                    
                   fib(n) = if (n<3) 1 else fibrec(n-1,2,1,1)
                   y = fib(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase("g(x) = g(x); y = g(1.0)")]
    [TestCase("g(x) = g(x-1); g(1.0)")]
    [TestCase("g(x) = g([x[0]-1])-1; [1.0].g()")]

    public void BuildsSomehow(string expr)
    {
        TraceLog.IsEnabled = true;
        expr.Build();
        TraceLog.IsEnabled = false;
    }
}