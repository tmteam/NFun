namespace NFun.SyntaxTests.Functions;

using Exceptions;
using TestTools;
using Tic;
using NUnit.Framework;

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

    // Generic fact called in three ways — caller decides T via context:
    //   `y:int  = fact(x)` → x inferred as Int32 (descendant constraint pins T=Int32)
    //   `y:real = fact(x)` → x inferred as Real (descendant constraint pins T=Real)
    //   `y       = fact(x)` → no caller context, body's Preferred (I32 from literal `1`)
    //                          wins, so x inferred as Int32.

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 6)]
    [TestCase(4, 24)]
    [TestCase(5, 120)]
    [TestCase(6, 720)]
    public void FactorialGeneric_OutInt(int x, int y) =>
        @"fact(n) = if(n<=1) 1 else fact(n-1)*n
              y:int = fact(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1.0, 1.0)]
    [TestCase(2.0, 2.0)]
    [TestCase(3.0, 6.0)]
    [TestCase(4.0, 24.0)]
    [TestCase(5.0, 120.0)]
    [TestCase(6.0, 720.0)]
    public void FactorialGeneric_OutReal(double x, double y) =>
        @"fact(n) = if(n<=1) 1 else fact(n-1)*n
              y:real = fact(x)"
            .Calc("x", x)
            .AssertReturns("y", y);

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 6)]
    [TestCase(4, 24)]
    [TestCase(5, 120)]
    [TestCase(6, 720)]
    public void FactorialGeneric_PreferredFromBody(int x, int y) =>
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
    public void ClassicRecFibonachi(int x, int y) =>
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
    public void PrimitiveRecFibonachi(int x, int y) =>
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
    public void ClassicRecFibonachi_specifyNType(int x, int y) =>
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
    public void BuildsSomehow(string expr) {
        TraceLog.IsEnabled = true;
        expr.Build();
        TraceLog.IsEnabled = false;
    }

    [Test]
    public void RecursiveConcat_Range_BaseCase_ReturnsEmpty() =>
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(5,5)"
            .AssertResultHas("y", System.Array.Empty<int>());

    // ── Recursive concat ────────────────────────────────────────────

    [Test]
    public void RecursiveConcat_Range() =>
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 0, 1, 2 });

    [Test]
    public void RecursiveConcat_SingleElement() =>
        "range(a,b) = if(a>=b) [] else [a].concat(range(a+1,b))\r y = range(0,1)"
            .AssertResultHas("y", new[] { 0 });

    [Test]
    public void RecursiveConcat_Expression() =>
        "range(a,b) = if(a>=b) [] else [a*100].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 0, 100, 200 });

    [Test]
    public void RecursiveConcat_Constant() =>
        "range(a,b) = if(a>=b) [] else [99].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 99, 99, 99 });

    [Test]
    public void RecursiveConcat_SecondParam() =>
        "range(a,b) = if(a>=b) [] else [b].concat(range(a+1,b))\r y = range(0,3)"
            .AssertResultHas("y", new[] { 3, 3, 3 });

    // ═══════════════════════════════════════════════════════════════
    // Recursive function with lambda — circular ancestor guard
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RecursiveFunctionWithLambda_CircularAncestor() {
        "applyN(x, f, n:int) = if(n > 0) applyN(f(x), f, n-1) else x; out = applyN(1, rule it*2, 3)"
            .AssertReturns("out", 8);
    }

    // ═══════════════════════════════════════════════════════════════
    // Recursive function shadows built-in
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void RecursiveFuncShadowsBuiltin() {
        // 'last' shadows built-in last(T[]):T. Recursive call must resolve to the user function.
        "last(x:int):int = if(x > 0) last(x-1) else 0; out = last(5)".Calc()
            .AssertResultHas("out", 0);
        // 'count' shadows built-in count(T[]):int.
        "count(x:int):int = if(x > 0) 1 + count(x-1) else 0; out = count(5)".Calc()
            .AssertResultHas("out", 5);
    }
}
