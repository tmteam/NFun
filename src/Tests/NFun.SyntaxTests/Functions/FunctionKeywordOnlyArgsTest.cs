namespace NFun.SyntaxTests.Functions;

using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Keyword-only arguments: parameters declared after ... (varargs).
/// Can only be passed by name, must have defaults.
/// </summary>
[TestFixture]
public class FunctionKeywordOnlyArgsTest {

    // ── Basic keyword-only ──────────────────────────────────────────

    [Test]
    public void KeywordOnly_UsesDefault() =>
        "f(...items, sep=' ') = sep \r y = f(1,2,3)".AssertReturns("y", " ");

    [Test]
    public void KeywordOnly_OverrideByName() =>
        "f(...items, sep=' ') = sep \r y = f(1,2,3, sep='-')".AssertReturns("y", "-");

    [Test]
    public void KeywordOnly_VarargsCollected() =>
        "f(...items, scale=1) = items.map(rule it*scale) \r y = f(1,2,3, scale=10)".AssertReturns("y", new[] { 10, 20, 30 });

    [Test]
    public void KeywordOnly_EmptyVarargs() =>
        "f(...items, scale=1) = items.count() + scale \r y = f(scale=5)".AssertReturns("y", 5);

    // ── Positional + varargs + keyword-only ─────────────────────────

    [Test]
    public void PositionalAndKeywordOnly() =>
        "f(a, ...rest, verbose=false) = if(verbose) rest.count() else a + rest.sum() \r y = f(1, 2, 3)".AssertReturns("y", 6);

    [Test]
    public void PositionalAndKeywordOnly_Override() =>
        "f(a, ...rest, verbose=false) = if(verbose) rest.count() else a + rest.sum() \r y = f(1, 2, 3, verbose=true)".AssertReturns("y", 2);

    [Test]
    public void PositionalAndKeywordOnly_EmptyRest() =>
        "f(a, ...rest, verbose=false) = if(verbose) rest.count() else a \r y = f(42, verbose=true)".AssertReturns("y", 0);

    // ── Multiple keyword-only ───────────────────────────────────────

    [Test]
    public void MultipleKeywordOnly() =>
        "f(...items, sep='-', prefix='') = prefix.concat(items.map(rule it.toText()).join(sep)) \r y = f(1,2,3, sep=':', prefix='>')".AssertReturns("y", ">1:2:3");

    [Test]
    public void MultipleKeywordOnly_AllDefaults() =>
        "f(...items, sep='-', prefix='') = prefix.concat(items.map(rule it.toText()).join(sep)) \r y = f(1,2,3)".AssertReturns("y", "1-2-3");

    [Test]
    public void MultipleKeywordOnly_OverrideOne() =>
        "f(...items, sep='-', prefix='') = prefix.concat(items.map(rule it.toText()).join(sep)) \r y = f(1,2,3, prefix='>')".AssertReturns("y", ">1-2-3");

    // ── Defaults before varargs + keyword-only after ────────────────

    [Test]
    public void DefaultsThenVarargsThenKeyword() =>
        "f(a, b=0, ...rest, scale=1) = (a+b+rest.sum()) * scale \r y = f(1, 2, 3, 4, scale=10)".AssertReturns("y", 100);

    [Test]
    public void DefaultsThenVarargsThenKeyword_AllDefaults() =>
        "f(a, b=0, ...rest, scale=1) = (a+b+rest.sum()) * scale \r y = f(5)".AssertReturns("y", 5);

    // ── Pipe-forward with keyword-only ──────────────────────────────

    [Test]
    public void PipeForward_KeywordOnly() =>
        "f(a, ...rest, scale=1) = (a + rest.sum()) * scale \r y = 10.f(1, 2, scale=5)".AssertReturns("y", 65);

    // ── Keyword-only with typed params ──────────────────────────────

    [Test]
    public void KeywordOnly_TypedVarargs() =>
        "f(...items:int[], sep:text='-') = items.map(rule it.toText()).join(sep) \r y = f(1,2,3, sep=':')".AssertReturns("y", "1:2:3");

    // ── Error cases ─────────────────────────────────────────────────

    [Test]
    public void KeywordOnly_ExtraPositionalGoesToVarargs() =>
        // With varargs, extra positional args get absorbed into the varargs (Python-compatible)
        "f(...items, sep=' ') = items.count() \r y = f(1,2,3, '-')".AssertReturns("y", 4);

    [Test]
    public void Error_KeywordOnly_WithoutDefault() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(...items, sep) = sep".Build());

    [Test]
    public void Error_KeywordOnly_DuplicateName() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(...items, sep=' ', sep='-') = sep".Build());

    [Test]
    public void Error_KeywordOnly_UnknownName() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(...items, sep=' ') = sep \r y = f(1,2,3, unknown='-')".Build());

    // ── Keyword-only order independence ─────────────────────────────

    [Test]
    public void KeywordOnly_ReversedOrder() =>
        "f(...items, a=1, b=2) = a * 10 + b \r y = f(1, b=20, a=10)".AssertReturns("y", 120);

    [Test]
    public void KeywordOnly_PartialOverrideMultiple() =>
        "f(...items, a=1, b=2, c=3) = a + b + c \r y = f(1, a=10, c=30)".AssertReturns("y", 42);

    // ── Named positional + keyword-only ──────────────────────────────

    [Test]
    public void KeywordOnly_NamedPositionalPlusKeyword() =>
        "f(a, ...rest, kw=1) = a + kw \r y = f(a=10, kw=5)".AssertReturns("y", 15);

    [Test]
    public void KeywordOnly_OnlyKeywordNamed_EmptyVarargs() =>
        "f(a, ...items, sep='-') = a \r y = f(a=1, sep='|')".AssertReturns("y", 1);

    // ── Pipe-forward edge cases ──────────────────────────────────────

    [Test]
    public void PipeForward_OnlyKeywordOverride() =>
        "f(a, ...items, sep='-') = sep \r y = 1.f(sep='|')".AssertReturns("y", "|");

    // ── Typed keyword-only ───────────────────────────────────────────

    [Test]
    public void KeywordOnly_TypedWithLiteralDefault() =>
        "f(...items, scale:int=6) = items.map(rule it*scale) \r y = f(1,2)".AssertReturns("y", new[] { 6, 12 });

    [Test]
    public void KeywordOnly_ExpressionDefault() =>
        "f(...items, scale=2*3) = items.map(rule it*scale) \r y = f(1,2)".AssertReturns("y", new[] { 6, 12 });

    [Test]
    public void KeywordOnly_TypedExpressionDefault() =>
        "f(...items, scale:int=2*3) = items.map(rule it*scale) \r y = f(1,2)".AssertReturns("y", new[] { 6, 12 });

    [Test]
    public void KeywordOnly_BothTyped() =>
        "f(...items:int[], count:int=0) = items.count() + count \r y = f(1,2,3, count=10)".AssertReturns("y", 13);

    // ── Recursive with keyword-only ──────────────────────────────────

    [Test]
    public void KeywordOnly_Recursive() =>
        "f(n, ...items, step=1) = if(n<=0) items.count() else f(n-step, step=step) \r y = f(3)".AssertReturns("y", 0);

    // ── Overload with keyword-only ──────────────────────────────────

    [Test]
    public void Overload_KeywordOnlyDoesNotChangeArity() =>
        "f(a) = a * 100 \r f(a, ...rest, sep='-') = a + rest.sum() \r y = f(5)".AssertReturns("y", 500);

    [Test]
    public void Overload_KeywordOnlyCallsCorrectOverload() =>
        "f(a) = a * 100 \r f(a, ...rest, sep='-') = a + rest.sum() \r y = f(5, 1, 2, sep=':')".AssertReturns("y", 8);

    // ── Error: name collisions ───────────────────────────────────────

    [Test]
    public void Error_KeywordOnly_NameSameAsPositional() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a, ...items, a=5) = a".Build());

    [Test]
    public void Error_KeywordOnly_NameSameAsVarargs() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(...items, items=5) = items".Build());

    // ── Regression: regular params still work ───────────────────────

    [Test]
    public void Regression_ParamsWithoutKeywordOnly() =>
        "f(a, ...rest) = a + rest.sum() \r y = f(1, 2, 3)".AssertReturns("y", 6);

    [Test]
    public void Regression_DefaultsStillWork() =>
        "f(a, b=10) = a + b \r y = f(5)".AssertReturns("y", 15);

    [Test]
    public void Regression_NamedArgsStillWork() =>
        "f(a, b) = a - b \r y = f(b=3, a=10)".AssertReturns("y", 7);
}
