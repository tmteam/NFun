using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Functions;

[TestFixture]
public class MutualRecursionTest {

    // ─── Expression mode (single-line `fun(args):ret = expr`) ───

    [Test]
    public void TwoFunctions_EvenOdd_FullyTyped() {
        // Simplest mutual recursion: isEven calls isOdd, isOdd calls isEven.
        // Both fully typed so prototypes can be pre-registered.
        ("isEven(n:int):bool = if(n == 0) true else isOdd(n - 1)\r"
        +"isOdd(n:int):bool = if(n == 0) false else isEven(n - 1)\r"
        +"y = isEven(10)")
            .AssertReturns("y", true);
    }

    [Test]
    public void TwoFunctions_EvenOdd_OddInput() {
        ("isEven(n:int):bool = if(n == 0) true else isOdd(n - 1)\r"
        +"isOdd(n:int):bool = if(n == 0) false else isEven(n - 1)\r"
        +"y = isEven(7)")
            .AssertReturns("y", false);
    }

    [Test]
    public void TwoFunctions_Ping_Pong_AccumulatorPattern() {
        ("ping(n:int, acc:int):int = if(n == 0) acc else pong(n - 1, acc + 1)\r"
        +"pong(n:int, acc:int):int = if(n == 0) acc else ping(n - 1, acc + 10)\r"
        +"y = ping(4, 0)")
            // n=4 acc=0 → pong(3,1) → ping(2,11) → pong(1,12) → ping(0,22) → 22
            .AssertReturns("y", 22);
    }

    [Test]
    public void ThreeFunctions_CyclicChain() {
        // a → b → c → a. SCC of size 3.
        ("a(n:int):int = if(n <= 0) 0 else b(n - 1) + 1\r"
        +"b(n:int):int = if(n <= 0) 0 else c(n - 1) + 1\r"
        +"c(n:int):int = if(n <= 0) 0 else a(n - 1) + 1\r"
        +"y = a(6)")
            .AssertReturns("y", 6);
    }

    [Test]
    public void TwoFunctions_NoCycle_OneCallsOther() {
        // Sanity: forward reference (caller calls helper) — should still work
        // exactly as before, not classified as mutual recursion.
        ("caller(x:int):int = helper(x) + 1\r"
        +"helper(x:int):int = x * 2\r"
        +"y = caller(5)")
            .AssertReturns("y", 11);
    }

    // ─── Type inference (no annotations) ───

    [Test]
    public void TwoFunctions_NoAnnotations_InferredTypes() {
        // No explicit types: TIC solves the SCC and infers (int) -> bool for both.
        ("isEven(n) = if(n == 0) true else isOdd(n - 1)\r"
        +"isOdd(n) = if(n == 0) false else isEven(n - 1)\r"
        +"y = isEven(10)")
            .AssertReturns("y", true);
    }

    [Test]
    public void TwoFunctions_NoAnnotations_OddInput() {
        ("isEven(n) = if(n == 0) true else isOdd(n - 1)\r"
        +"isOdd(n) = if(n == 0) false else isEven(n - 1)\r"
        +"y = isEven(7)")
            .AssertReturns("y", false);
    }

    [Test]
    public void ThreeFunctions_NoAnnotations_InferredTypes() {
        ("a(n) = if(n <= 0) 0 else b(n - 1) + 1\r"
        +"b(n) = if(n <= 0) 0 else c(n - 1) + 1\r"
        +"c(n) = if(n <= 0) 0 else a(n - 1) + 1\r"
        +"y = a(6)")
            .AssertReturns("y", 6);
    }

    // ─── Default parameters in mutual cycle ───
    // The peer-call site fills the missing arg via the user-declared default.
    // Dep analysis (FindFunctionDependenciesVisitor) considers default-expanded
    // arity so the SCC group is detected and all members solve together.

    [Test]
    public void MutualWithDefault_Typed() {
        // a(3) → b(6) → 6<=50 → a(6) → b(12) → … → a(48) → b(96) → 96>50 → 96
        ("a(x:int, n:int=2):int = b(x*n)\r"
        +"b(s:int):int = if(s>50) s else a(s)\r"
        +"y = a(3)")
            .AssertReturns("y", 96);
    }

    [Test]
    public void MutualWithDefault_Untyped() {
        ("a(x, n=2) = b(x*n)\r"
        +"b(s) = if(s>50) s else a(s)\r"
        +"y = a(3)")
            .AssertReturns("y", 96);
    }

    [Test]
    public void SelfRecursion_TypedDefault_PreservesUserValue() {
        // Regression: the typed-default branch in ResolveNamedArgs used to insert
        // a DefaultValueSyntaxNode (type default 0) instead of the user's `2`,
        // breaking self-recursive calls that relied on the default.
        ("a(x:int, n:int=2):int = if(x>20) x else a(x*n)\r"
        +"y = a(3)")
            .AssertReturns("y", 24);
    }
}
