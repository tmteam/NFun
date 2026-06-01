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

    [Ignore("Recursive generic function input type not propagated as preferred — x inferred as Real instead of Int32")]
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

    // ───────────────────────────────────────────────────────────────
    // MR6Bug1 — Recursive user function called through `?.` crashes at
    //   runtime with "The given key 'foo' was not present in the
    //   dictionary".
    //
    //     type n = {v:int, next:n?=none}
    //     foo(node:n):int = (node.next?.foo() ?? 0) + 1
    //     chain = n{v=1, next=n{v=2}}
    //     out = foo(chain)                # crash
    //
    //   Same `foo` works via non-?. path: `if(node.next != none) foo(node.next!) else 0`.
    //   Non-recursive user fn via `?.` works.
    //   Specific combination: recursion + safe-field-access dispatch.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR6Bug1_RecursiveUserFnViaSafeAccess_2NodeChain() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type n = {v:int, next:n?=none}\rfoo(node:n):int = (node.next?.foo() ?? 0) + 1\rchain = n{v=1, next=n{v=2}}\rout = foo(chain)");
        rt.Run();
        Assert.AreEqual(2, rt["out"].Value);
    }

    [Test]
    public void MR6Bug1_RecursiveUserFnViaSafeAccess_4NodeChain() {
        // Deeper chain — verifies the recursive call dispatches correctly through `?.`
        // at every depth, not just the top frame.
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type n = {v:int, next:n?=none}\rfoo(node:n):int = (node.next?.foo() ?? 0) + 1\rchain = n{v=1, next=n{v=2, next=n{v=3, next=n{v=4}}}}\rout = foo(chain)");
        rt.Run();
        Assert.AreEqual(4, rt["out"].Value);
    }
}
