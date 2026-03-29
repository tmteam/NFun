namespace NFun.SyntaxTests.Functions;

using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for function overloading, shadowing built-ins, and interaction
/// between overloads + named args + defaults + params.
/// </summary>
[TestFixture]
public class FunctionOverloadTest {

    // ═══════════════════════════════════════════════════════════════════════
    // User function overloads by arity
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Overload_TwoArities() =>
        "f(a) = a * 10 \r f(a,b) = a + b \r y = f(3) + f(1, 2)".AssertReturns("y", 33);

    [Test]
    public void Overload_ThreeArities() =>
        @"f(a) = a
          f(a,b) = a + b
          f(a,b,c) = a + b + c
          y = f(1) + f(2,3) + f(4,5,6)".AssertReturns("y", 21);

    [Test]
    public void Overload_ZeroAndOneArity() =>
        "f() = 42 \r f(a) = a \r y = f() + f(10)".AssertReturns("y", 52);

    [Test]
    public void Overload_DifferentReturnTypes() =>
        @"f(a:int):int = a * 2
          f(a:int, b:int):int = a + b
          y = f(5) + f(3, 7)".AssertReturns("y", 20);

    // ═══════════════════════════════════════════════════════════════════════
    // User function shadows built-in — same name, same arity
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Shadow_MaxSameArity() =>
        @"max(a:int, b:int):int = if(a > b) a * 100 else b * 100
          y = max(3, 5)".AssertReturns("y", 500);

    [Test]
    public void Shadow_MinSameArity() =>
        @"min(a:int, b:int):int = if(a < b) a * 100 else b * 100
          y = min(3, 5)".AssertReturns("y", 300);

    [Test]
    public void Shadow_CountSameArity() =>
        @"count(arr:int[]):int = arr.count() * 2
          y = count([1,2,3])".AssertReturns("y", 6);

    [Test]
    public void Shadow_AbsSameArity() =>
        @"abs(x:int):int = if(x < 0) -x * 10 else x * 10
          y = abs(-3)".AssertReturns("y", 30);

    // ═══════════════════════════════════════════════════════════════════════
    // User function extends built-in — same name, different arity
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Extend_MaxWithThreeArgs() =>
        @"max(a,b,c) = max(max(a,b), c)
          y = max(3, 7, 5)".AssertReturns("y", 7);

    [Test]
    public void Extend_MinWithThreeArgs() =>
        @"min(a,b,c) = min(min(a,b), c)
          y = min(3, 7, 5)".AssertReturns("y", 3);

    [Test]
    public void Extend_BuiltInStillWorksAtOriginalArity() =>
        @"max(a,b,c) = max(max(a,b), c)
          y = max(10, 20) + max(1, 2, 3)".AssertReturns("y", 23);

    // ═══════════════════════════════════════════════════════════════════════
    // Named args with user overloads
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void NamedArgs_SelectsCorrectOverload() =>
        @"f(a) = a * 10
          f(a,b) = a + b
          y = f(b = 3, a = 2)".AssertReturns("y", 5);

    [Test]
    public void NamedArgs_SingleArgOverload() =>
        @"f(a) = a * 10
          f(a,b) = a + b
          y = f(a = 7)".AssertReturns("y", 70);

    [Test]
    public void NamedArgs_OverloadWithDifferentArgNames() =>
        @"f(x) = x * 2
          f(a, b) = a - b
          y = f(x = 5) + f(b = 1, a = 10)".AssertReturns("y", 19);

    // ═══════════════════════════════════════════════════════════════════════
    // Named args — user function shadows built-in
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void NamedArgs_UserShadowsBuiltIn_UserArgNames() =>
        @"max(x, y) = if(x > y) x else y
          y = max(y = 1, x = 5)".AssertReturns("y", 5);

    [Test]
    public void NamedArgs_BuiltInCalledPositionally_UserWithNamed() =>
        @"max(a, b, c) = max(max(a,b), c)
          y = max(c = 1, a = 5, b = 3)".AssertReturns("y", 5);

    // ═══════════════════════════════════════════════════════════════════════
    // Defaults with overloads
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Defaults_OverloadExactMatchPreferred() =>
        @"f(a) = a * 100
          f(a, b) = a + b
          y = f(5)".AssertReturns("y", 500);

    [Test]
    public void Defaults_FallsBackToDefaultWhenNoExactMatch() =>
        @"f(a, b = 10) = a + b
          y = f(5)".AssertReturns("y", 15);

    [Test]
    public void Defaults_ExactMatchOverDefaultMatch() =>
        @"f(a) = a * 100
          f(a, b = 10) = a + b
          y = f(5)".AssertReturns("y", 500);

    [Test]
    public void Defaults_TwoArgsCallGoesToTwoArgFunction() =>
        @"f(a) = a * 100
          f(a, b = 10) = a + b
          y = f(5, 20)".AssertReturns("y", 25);

    [Test]
    public void Defaults_NamedArgDisambiguates() =>
        @"f(a, b = 10) = a + b
          y = f(5, b = 20)".AssertReturns("y", 25);

    // ═══════════════════════════════════════════════════════════════════════
    // Params with overloads (different declared arity)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Params_OverloadDifferentArity() =>
        @"f(a) = a * 100
          f(a, b, ...rest) = a + b + rest.sum()
          y = f(5) + f(1, 2, 3, 4)".AssertReturns("y", 510);

    [Test]
    public void Params_OverloadSameArityError() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a, b) = a - b \r f(a, ...rest) = a + rest.sum() \r y = f(1,2)".Build());

    // ═══════════════════════════════════════════════════════════════════════
    // Named args on built-in with user overload at different arity
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void BuiltInNamedArgs_UserExtendsDifferentArity() =>
        @"max(a, b, c) = max(max(a,b), c)
          y = max(a = 5, b = 1)".AssertReturns("y", 5);

    [Test]
    public void BuiltInNamedArgs_UserAtSameArity_Shadows() =>
        @"max(x, y) = if(x > y) x + 1000 else y + 1000
          y = max(x = 3, y = 5)".AssertReturns("y", 1005);

    // ═══════════════════════════════════════════════════════════════════════
    // Overloads calling each other
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void OverloadCallsOtherOverload() =>
        @"f(a) = a
          f(a,b) = f(a) + f(b)
          y = f(3, 7)".AssertReturns("y", 10);

    [Test]
    public void OverloadChain() =>
        @"f(a) = a * 2
          f(a,b) = f(a) + f(b)
          f(a,b,c) = f(a,b) + f(c)
          y = f(1, 2, 3)".AssertReturns("y", 12);

    [Test]
    public void UserFunctionCallsBuiltInSameName() =>
        @"max(a,b,c) = max(a, max(b, c))
          y = max(1, 2, 3)".AssertReturns("y", 3);

    // ═══════════════════════════════════════════════════════════════════════
    // Pipe-forward with overloads
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void PipeForward_SelectsCorrectOverload() =>
        @"f(a) = a * 10
          f(a, b) = a + b
          y = 5.f()".AssertReturns("y", 50);

    [Test]
    public void PipeForward_TwoArgOverload() =>
        @"f(a) = a * 10
          f(a, b) = a + b
          y = 5.f(3)".AssertReturns("y", 8);

    [Test]
    public void PipeForward_NamedArgInOverload() =>
        @"f(a) = a * 10
          f(a, b) = a + b
          y = 5.f(b = 3)".AssertReturns("y", 8);

    [Test]
    public void PipeForward_ShadowsBuiltIn() =>
        @"sort(arr, desc) = if(desc) arr.sortDescending() else arr.sort()
          y = [3,1,2].sort(true)".AssertReturns("y", new[] { 3, 2, 1 });

    // ═══════════════════════════════════════════════════════════════════════
    // Complex: overloads + defaults + named args
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Complex_DefaultsAndNamed_Overloads() =>
        @"f(a) = a * 100
          f(a, b, c = 0) = a + b + c
          y = f(1, 2) + f(1, 2, c = 3)".AssertReturns("y", 9);

    [Test]
    public void Complex_NamedArgs_MultipleFunctions() =>
        @"add(a, b) = a + b
          mul(a, b) = a * b
          y = add(b = mul(b = 3, a = 2), a = 1)".AssertReturns("y", 7);

    // ═══════════════════════════════════════════════════════════════════════
    // Cross-feature: built-in named + user overloads + pipe
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void CrossFeature_BuiltInNamedAndUserOverload() =>
        @"round(x) = round(x, 0)
          y = round(3.7)".AssertReturns("y", 4.0);

    [Test]
    public void CrossFeature_SplitWithUserWrapper() =>
        @"splitFirst(s, sep) = split(s, sep)[0]
          y = splitFirst(sep = '-', s = 'a-b-c')".AssertReturns("y", "a");

    // ═══════════════════════════════════════════════════════════════════════
    // Error cases
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public void Error_DuplicateExactSignature() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a,b) = a + b \r f(a,b) = a * b \r y = f(1,2)".Build());

    [Test]
    public void Error_NamedArgWrongOverload() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a) = a \r f(a,b) = a+b \r y = f(z = 1)".Build());

    [Test]
    public void Error_TooManyArgsNoMatchingOverload() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a) = a \r f(a,b) = a+b \r y = f(1,2,3,4)".Build());

    // ── Recursive shadow of builtin ─────────────────────────────────

    [Test]
    public void RecursiveShadow_Sum() =>
        "sum(n) = if(n <= 0) 0 else n + sum(n-1)\r y = sum(10)".AssertReturns("y", 55);

    [Test]
    public void RecursiveShadow_Count() =>
        "count(n) = if(n <= 0) 0 else 1 + count(n-1)\r y = count(5)".AssertReturns("y", 5.0);

    [Test]
    public void RecursiveShadow_Fold() =>
        "fold(arr, acc) = if(arr.count()==0) acc else fold(arr[1:], acc+arr[0])\r y = fold([1,2,3], 0)"
            .AssertReturns("y", 6);
}
