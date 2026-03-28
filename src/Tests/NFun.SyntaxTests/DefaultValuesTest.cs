using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>Default values for function args (#87)</summary>
[TestFixture]
public class DefaultValuesTest {

    // ── Basic defaults ──────────────────────────────────────────────────────

    [Test]
    public void Default_SingleOptionalArg() =>
        "f(a, b = 10) = a + b \r y = f(5)".AssertReturns("y", 15);

    [Test]
    public void Default_AllArgsProvided() =>
        "f(a, b = 10) = a + b \r y = f(5, 20)".AssertReturns("y", 25);

    [Test]
    public void Default_MultipleDefaults() =>
        "f(x, a = 42, b = 'foo') = if(x > a) 'baz' else b \r y = f(12)".AssertReturns("y", "foo");

    [Test]
    public void Default_MultipleDefaults_PartialOverride() =>
        "f(x, a = 42, b = 'foo') = if(x > a) 'baz' else b \r y = f(50)".AssertReturns("y", "baz");

    [Test]
    public void Default_OverrideFirstDefault() =>
        "f(x, a = 42, b = 'foo') = if(x > a) 'baz' else b \r y = f(12, 5)".AssertReturns("y", "baz");

    [Test]
    public void Default_OverrideBothDefaults() =>
        "f(x, a = 42, b = 'foo') = if(x > a) 'baz' else b \r y = f(12, 5, 'bar')".AssertReturns("y", "baz");

    // ── Defaults with named args ────────────────────────────────────────────

    [Test]
    public void Default_SkipMiddleWithNamed() =>
        "f(x, a = 42, b = 'foo') = if(x > a) 'baz' else b \r y = f(12, b = 'bee')".AssertReturns("y", "bee");

    [Test]
    public void Default_AllNamed() =>
        "f(a = 1, b = 2) = a + b \r y = f()".AssertReturns("y", 3);

    [Test]
    public void Default_OverrideOneByName() =>
        "f(a = 1, b = 2) = a + b \r y = f(b = 10)".AssertReturns("y", 11);

    // ── Default value expressions ───────────────────────────────────────────

    [Test]
    public void Default_ExpressionAsDefault() =>
        "f(a, b = 2 * 3) = a + b \r y = f(4)".AssertReturns("y", 10);

    // ── Error: required after default ───────────────────────────────────────

    [Test]
    public void Error_RequiredAfterDefault() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a = 1, b) = a + b".Build());

    // ── Verify required-then-default is OK ──────────────────────────────────

    [Test]
    public void Default_RequiredThenDefault_OK() =>
        "f(a, b = 5) = a * b \r y = f(3)".AssertReturns("y", 15);

    // ── Default value types ────────────────────────────────────────────────

    [Test]
    public void Default_BoolDefault() =>
        "f(a, flag = true) = if(flag) a else -a \r y = f(5)".AssertReturns("y", 5);

    [Test]
    public void Default_BoolDefault_Overridden() =>
        "f(a, flag = true) = if(flag) a else -a \r y = f(5, false)".AssertReturns("y", -5);

    [Test]
    public void Default_ArrayDefault() =>
        "f(a, items = [1,2,3]) = a + items.count() \r y = f(10)".AssertReturns("y", 13);

    [Test]
    public void Default_ZeroDefault() =>
        "f(a, b = 0) = a + b \r y = f(7)".AssertReturns("y", 7);

    // ── Multiple defaults various arities ───────────────────────────────────

    [Test]
    public void Default_ThreeDefaults_NoneProvided() =>
        "f(a = 1, b = 2, c = 3) = a * 100 + b * 10 + c \r y = f()".AssertReturns("y", 123);

    [Test]
    public void Default_ThreeDefaults_OneProvided() =>
        "f(a = 1, b = 2, c = 3) = a * 100 + b * 10 + c \r y = f(9)".AssertReturns("y", 923);

    [Test]
    public void Default_ThreeDefaults_TwoProvided() =>
        "f(a = 1, b = 2, c = 3) = a * 100 + b * 10 + c \r y = f(9, 8)".AssertReturns("y", 983);

    [Test]
    public void Default_ThreeDefaults_AllProvided() =>
        "f(a = 1, b = 2, c = 3) = a * 100 + b * 10 + c \r y = f(9, 8, 7)".AssertReturns("y", 987);

    // ── Defaults + named: skip first default by name ────────────────────────

    [Test]
    public void Default_NamedSkipFirst() =>
        "f(a = 1, b = 2, c = 3) = a * 100 + b * 10 + c \r y = f(c = 9)".AssertReturns("y", 129);

    [Test]
    public void Default_NamedSkipMiddle() =>
        "f(a, b = 2, c = 3) = a * 100 + b * 10 + c \r y = f(1, c = 9)".AssertReturns("y", 129);

    // ── Defaults in user function called from expression ────────────────────

    [Test]
    public void Default_CalledTwice() =>
        "f(a, b = 10) = a + b \r y = f(1) + f(2, 20)".AssertReturns("y", 33);

    [Test]
    public void Default_CalledInCondition() =>
        "f(a, b = 0) = a + b \r y = if(f(1) > 0) f(1, 5) else 0".AssertReturns("y", 6);

    // ── Error: too many args with defaults ───────────────────────────────────

    [Test]
    public void Error_TooManyArgsWithDefaults() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a, b = 1) = a + b \r y = f(1, 2, 3)".Build());

    // ── Regression ──────────────────────────────────────────────────────────

    [Test]
    public void Regression_NormalFunction_Unaffected() =>
        "f(a, b) = a + b \r y = f(3, 4)".AssertReturns("y", 7);

    [Test]
    public void Regression_NamedArgs_StillWork() =>
        "f(a, b) = a - b \r y = f(b = 3, a = 10)".AssertReturns("y", 7);
}
