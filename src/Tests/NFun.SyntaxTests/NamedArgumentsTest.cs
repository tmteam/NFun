using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class NamedArgumentsTest {

    // ── Basic named args ────────────────────────────────────────────────────

    [Test]
    public void AllNamedArgs_TwoParams() =>
        "f(a,b) = a - b \r y = f(a = 10, b = 3)".AssertReturns("y", 7);

    [Test]
    public void AllNamedArgs_ReversedOrder() =>
        "f(a,b) = a - b \r y = f(b = 3, a = 10)".AssertReturns("y", 7);

    [Test]
    public void AllNamedArgs_ThreeParams() =>
        "f(a,b,c) = a + b + c \r y = f(c = 3, a = 1, b = 2)".AssertReturns("y", 6);

    [Test]
    public void SingleNamedArg() =>
        "f(a) = a * 2 \r y = f(a = 5)".AssertReturns("y", 10);

    // ── Mixed positional + named ────────────────────────────────────────────

    [Test]
    public void FirstPositional_RestNamed() =>
        "f(a,b,c) = a + b + c \r y = f(1, c = 3, b = 2)".AssertReturns("y", 6);

    [Test]
    public void TwoPositional_OneNamed() =>
        "f(a,b,c) = a * b + c \r y = f(2, 3, c = 10)".AssertReturns("y", 16);

    // ── Named args with typed parameters ────────────────────────────────────

    [Test]
    public void NamedArgs_WithTypedParams() =>
        "f(a:int, b:int) = a - b \r y = f(b = 1, a = 5)".AssertReturns("y", 4);

    [Test]
    public void NamedArgs_WithReturnType() =>
        "f(a:int, b:int):int = a * b \r y = f(b = 7, a = 3)".AssertReturns("y", 21);

    // ── Named args with different types ─────────────────────────────────────

    [Test]
    public void NamedArgs_DifferentTypes() =>
        "f(a, b) = if(b) a else -a \r y = f(b = true, a = 5)".AssertReturns("y", 5);

    [Test]
    public void NamedArgs_TextArgs() =>
        "f(greeting, name) = greeting.concat(' ').concat(name) \r y = f(name = 'world', greeting = 'hello')".AssertReturns("y", "hello world");

    // ── Named args with built-in functions ──────────────────────────────────

    [Test]
    public void NamedArgs_BuiltInFunction_NotSupported() =>
        Assert.Throws<FunnyParseException>(() =>
            "y = max(b = 1, a = 5)".Build());

    // ── Named args in expressions ───────────────────────────────────────────

    [Test]
    public void NamedArgs_InComplexExpression() =>
        "f(a,b) = a * b \r y = f(b = 3, a = 2) + f(a = 10, b = 1)".AssertReturns("y", 16);

    [Test]
    public void NamedArgs_Nested() =>
        "f(a,b) = a + b \r y = f(a = f(a = 1, b = 2), b = 10)".AssertReturns("y", 13);

    // ── Named args with default-like patterns ───────────────────────────────

    [Test]
    public void NamedArgs_AllSameValue() =>
        "f(a,b,c) = a + b + c \r y = f(a = 1, b = 1, c = 1)".AssertReturns("y", 3);

    // ── Error cases ─────────────────────────────────────────────────────────

    [Test]
    public void Error_DuplicateNamedArg() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a,b) = a + b \r y = f(a = 1, a = 2)".Build());

    [Test]
    public void Error_NamedArgOverlapsPositional() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a,b) = a + b \r y = f(1, a = 2)".Build());

    [Test]
    public void Error_NamedAfterPositional_ThenPositional() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a,b,c) = a+b+c \r y = f(1, b = 2, 3)".Build());

    [Test]
    public void Error_UnknownArgName() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a,b) = a + b \r y = f(x = 1, b = 2)".Build());

    [Test]
    public void Error_TooManyArgs_WithNamed() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a,b) = a + b \r y = f(a = 1, b = 2, c = 3)".Build());

    // ── Regression: normal calls still work ────────────────────────────────

    [Test]
    public void Regression_PositionalCallUnchanged() =>
        "f(a,b) = a - b \r y = f(10, 3)".AssertReturns("y", 7);

    [Test]
    public void Regression_NoArgsCallUnchanged() =>
        "f() = 42 \r y = f()".AssertReturns("y", 42.0);

    [Test]
    public void Regression_SinglePositionalArg() =>
        "f(a) = a * 3 \r y = f(5)".AssertReturns("y", 15);

    // ── Partial named: only some args named ─────────────────────────────────

    [Test]
    public void OnlyLastArgNamed() =>
        "f(a,b,c) = a * 100 + b * 10 + c \r y = f(1, 2, c = 3)".AssertReturns("y", 123);

    [Test]
    public void OnlyMiddleAndLastNamed() =>
        "f(a,b,c) = a * 100 + b * 10 + c \r y = f(1, c = 3, b = 2)".AssertReturns("y", 123);

    // ── Recursive function with named args ──────────────────────────────────

    [Test]
    public void NamedArgs_RecursiveFunction() =>
        "factorial(n) = if(n <= 1) 1 else n * factorial(n = n - 1) \r y = factorial(n = 5)".AssertReturns("y", 120);

    // ── Case sensitivity ────────────────────────────────────────────────────

    [Test]
    public void NamedArgs_CaseInsensitive() =>
        "f(myArg, other) = myArg + other \r y = f(MyArg = 1, Other = 2)".AssertReturns("y", 3);

    // ── Named arg where value is another function call ──────────────────────

    [Test]
    public void NamedArgs_FunctionCallAsValue() =>
        "f(a,b) = a + b \r g(x) = x * 10 \r y = f(b = g(3), a = 1)".AssertReturns("y", 31);

    // ── Four+ params with mixed order ───────────────────────────────────────

    [Test]
    public void FourParams_AllNamedReversed() =>
        "f(a,b,c,d) = a*1000 + b*100 + c*10 + d \r y = f(d=4, c=3, b=2, a=1)".AssertReturns("y", 1234);

    [Test]
    public void FourParams_TwoPositionalTwoNamed() =>
        "f(a,b,c,d) = a*1000 + b*100 + c*10 + d \r y = f(1, 2, d=4, c=3)".AssertReturns("y", 1234);

    // ── Error: missing argument after named fill ────────────────────────────

    [Test]
    public void Error_MissingArgument() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a,b,c) = a+b+c \r y = f(a = 1, c = 3)".Build());

    // ── Edge cases ──────────────────────────────────────────────────────────

    [Test]
    public void NamedArgs_ArgNameDiffFromVariable() =>
        "f(a,b) = a + b \r x = 10 \r y = f(b = 3, a = x)".Calc().AssertResultHas(("y", 13));

    [Test]
    public void NamedArgs_ExpressionAsValue() =>
        "f(a,b) = a + b \r y = f(b = 2 * 3, a = 1 + 1)".AssertReturns("y", 8);

    [Test]
    public void NamedArgs_ArrayAsValue() =>
        "f(a, b) = a.concat(b) \r y = f(b = [4,5], a = [1,2,3])".AssertReturns("y", new[] { 1, 2, 3, 4, 5 });

    [Test]
    public void NamedArgs_BoolExpressionAsValue() =>
        "f(a,b) = a and b \r y = f(b = true, a = true)".AssertReturns("y", true);

    // ── Multiline ───────────────────────────────────────────────────────────

}
