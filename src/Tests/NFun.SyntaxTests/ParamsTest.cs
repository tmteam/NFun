using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>Vararg params syntax (#86)</summary>
[TestFixture]
public class ParamsTest {

    // ── Basic params ────────────────────────────────────────────────────────

    [Test]
    public void Params_CollectsExtraArgs() =>
        "f(a, ...x) = x.filter(rule it > a) \r y = f(2, 1, 2, 3, 4)".AssertReturns("y", new[] { 3, 4 });

    [Test]
    public void Params_EmptyVarargs() =>
        "f(a, ...x) = x.count() \r y = f(42)".AssertReturns("y", 0);

    [Test]
    public void Params_SingleVararg() =>
        "f(a, ...x) = x.count() \r y = f(42, 1)".AssertReturns("y", 1);

    [Test]
    public void Params_OnlyVarargs() =>
        "f(...x) = x.count() \r y = f(1, 2, 3)".AssertReturns("y", 3);

    [Test]
    public void Params_OnlyVarargs_Empty() =>
        "f(...x) = x.count() \r y = f()".AssertReturns("y", 0);

    [Test]
    public void Params_ManyVarargs() =>
        "f(...x) = x.count() \r y = f(1,2,3,4,5,6,7,8,9,10)".AssertReturns("y", 10);

    // ── Params type inference ───────────────────────────────────────────────

    [Test]
    public void Params_InferredIntArray() =>
        "f(...x) = x.sum() \r y = f(1, 2, 3)".AssertReturns("y", 6);

    [Test]
    public void Params_InferredRealArray() =>
        "f(...x) = x.sum() \r y = f(1.0, 2.0, 3.0)".AssertReturns("y", 6.0);

    // ── Typed params ────────────────────────────────────────────────────────

    [Test]
    public void Params_TypedVarargs() =>
        "f(a:int, ...x:int[]) = a + x.sum() \r y = f(10, 1, 2, 3)".AssertReturns("y", 16);

    // ── Params with operations ──────────────────────────────────────────────

    [Test]
    public void Params_FoldVarargs() =>
        "mysum(...args) = args.fold(0, rule(a,b) a + b) \r y = mysum(1, 2, 3, 4, 5)".AssertReturns("y", 15);

    [Test]
    public void Params_MapVarargs() =>
        "doubled(...x) = x.map(rule it * 2) \r y = doubled(1, 2, 3)".AssertReturns("y", new[] { 2, 4, 6 });

    [Test]
    public void Params_CountAndFirst() =>
        "first_or_zero(...x) = if(x.count() > 0) x[0] else 0 \r y = first_or_zero(42, 1, 2)".AssertReturns("y", 42);

    // ── Params with required args ───────────────────────────────────────────

    [Test]
    public void Params_RequiredPlusVarargs() =>
        "f(prefix, ...items) = prefix.concat(items.map(rule it.toText()).fold('', rule(a,b) a.concat(b))) \r y = f('nums:', 1, 2, 3)".AssertReturns("y", "nums:123");

    [Test]
    public void Params_TwoRequiredPlusVarargs() =>
        "f(a, b, ...rest) = a + b + rest.count() \r y = f(10, 20, 1, 2, 3)".AssertReturns("y", 33);

    // ── Params errors ───────────────────────────────────────────────────────

    [Test]
    public void Error_ParamsNotLast() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(...x, a) = x".Build());

    [Test]
    public void Error_MultipleParams() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(...x, ...y) = x".Build());

    [Test]
    public void Error_ParamsBeforeRequired() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(...x, a, b) = x".Build());

    // ── Combined: defaults + params ─────────────────────────────────────────

    [Test]
    public void Combined_DefaultThenParams_NoExtras() =>
        "f(a, b = 10, ...rest) = a + b + rest.count() \r y = f(1)".AssertReturns("y", 11);

    [Test]
    public void Combined_DefaultOverriddenThenParams() =>
        "f(a, b = 10, ...rest) = a + b + rest.count() \r y = f(1, 20, 3, 4)".AssertReturns("y", 23);

    [Test]
    public void Combined_NamedDefaultWithParams() =>
        "f(a, b = 10, ...rest) = a + b + rest.count() \r y = f(1, b = 5)".AssertReturns("y", 6);

    [Test]
    public void Combined_DefaultsAndParams_AllProvided() =>
        "f(a, b = 0, c = 0, ...rest) = a + b + c + rest.sum() \r y = f(1, 2, 3, 10, 20)".AssertReturns("y", 36);

    // ── Varargs used in different ways ────────────────────────────────────

    [Test]
    public void Params_ConcatVarargs() =>
        "joinAll(...items) = items.fold('', rule(a,b) a.concat(b.toText())) \r y = joinAll(1, 2, 3)".AssertReturns("y", "123");

    [Test]
    public void Params_VarargsPassedToAnotherFunction() =>
        "inner(arr) = arr.count() \r outer(...x) = inner(x) \r y = outer(1, 2, 3)".AssertReturns("y", 3);

    // ── Params with named args ──────────────────────────────────────────────

    [Test]
    public void Params_RequiredNamed_VarargsPositional() =>
        "f(a, ...rest) = a + rest.sum() \r y = f(a = 10, 1, 2, 3)".AssertReturns("y", 16);

    // ── Named arg passes array to params directly ─────────────────────────

    [Test]
    public void Params_NamedArrayForVarargs() =>
        "f(a, ...b) = a + b.sum() \r y = f(42, b = [1,2,3])".AssertReturns("y", 48);

    [Test]
    public void Params_NamedEmptyArrayForVarargs() =>
        "f(a, ...b) = a + b.count() \r y = f(42, b = [])".AssertReturns("y", 42);

    // ── Params: exact boundary between required and varargs ─────────────────

    [Test]
    public void Params_ExactRequiredCount() =>
        "f(a, b, ...rest) = a + b + rest.count() \r y = f(10, 20)".AssertReturns("y", 30);

    [Test]
    public void Params_OneMoreThanRequired() =>
        "f(a, b, ...rest) = a + b + rest.count() \r y = f(10, 20, 1)".AssertReturns("y", 31);

    // ── Error: fewer than required with params ──────────────────────────────

    [Test]
    public void Error_TooFewArgsForParams() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a, b, ...rest) = a + b \r y = f(1)".Build());

    // ── Regression ──────────────────────────────────────────────────────────

    [Test]
    public void Regression_NormalFunction_Unaffected() =>
        "f(a, b) = a + b \r y = f(3, 4)".AssertReturns("y", 7);

    [Test]
    public void Regression_NamedArgs_StillWork() =>
        "f(a, b) = a - b \r y = f(b = 3, a = 10)".AssertReturns("y", 7);
}
