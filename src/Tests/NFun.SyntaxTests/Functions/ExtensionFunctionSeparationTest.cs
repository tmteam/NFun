using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Functions;

[TestFixture]
public class ExtensionFunctionSeparationTest {

    #region Disabled (default) — current behavior unchanged

    [Test]
    public void Disabled_PipedCallWorksForRegularFunction() =>
        "f(x) = x * 2\r y = 5.f()".AssertReturns("y", 10);

    [Test]
    public void Disabled_RegularCallWorks() =>
        "f(x) = x * 2\r y = f(5)".AssertReturns("y", 10);

    [Test]
    public void Disabled_PipedCallWithArgs() =>
        "add(x, n) = x + n\r y = 10.add(5)".AssertReturns("y", 15);

    [Test]
    public void Disabled_ExtensionSyntaxStillWorkAsRegularFunction() =>
        // Even with x.f() syntax, when separation is disabled it's the same as f(x)
        "x.f() = x * 2\r y = f(5)".AssertReturns("y", 10);

    #endregion

    #region Enabled — extension functions via pipe only

    [Test]
    public void Enabled_ExtensionOnlyCallableViaPipe() {
        var result = "x.double() = x * 2\r y = 5.double()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", 10);
    }

    [Test]
    public void Enabled_RegularCallWorks() {
        var result = "double(x) = x * 2\r y = double(5)"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", 10);
    }

    [Test]
    public void Enabled_RegularCallableViaPipe() =>
        // Regular (non-extension) user fns are pipe-callable too in separation mode —
        // extension takes precedence when both exist (see Enabled_BothCanCoexist),
        // but a bare regular fn falls through the pipe path. The previous strict
        // rejection was inconsistent: typed-arg regular fns succeeded while generic
        // ones threw MJ78 (BugHunt-stmt #44).
        "double(x) = x * 2\r y = 5.double()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled)
            .AssertReturns("y", 10);

    [Test]
    public void Enabled_ExtensionNotCallableDirectly() {
        // x.double() = x * 2 -> double(5) should NOT work
        // May throw FunnyParseException (TIC rejects) or NFunImpossibleException (ExpressionBuilder can't resolve)
        Assert.That(() =>
            "x.double() = x * 2\r y = double(5)"
                .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled),
            Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Enabled_BothCanCoexist() {
        // double(x) = x + 1 (regular) AND x.double() = x * 2 (extension) — different functions
        var result = "double(x) = x + 1\r x.double() = x * 2\r a = double(5)\r b = 5.double()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns(("a", 6), ("b", 10));
    }

    [Test]
    public void Enabled_TypedReceiver() {
        var result = "x:int.isEven() = x % 2 == 0\r y = 42.isEven()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", true);
    }

    [Test]
    public void Enabled_ExtensionWithExtraArgs() {
        var result = "x.add(n) = x + n\r y = 10.add(5)"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", 15);
    }

    [Test]
    public void Enabled_BuiltinPipedCallsStillWork() {
        // Built-in functions should still work via piped syntax
        var result = "y = [3,1,2].sort()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", new[] { 1, 2, 3 });
    }

    [Test]
    public void Enabled_BuiltinCountStillWorksViaPipe() {
        var result = "y = [1,2,3].count()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", 3);
    }

    [Test]
    public void Enabled_BuiltinReverseStillWorksViaPipe() {
        var result = "y = [1,2,3].reverse()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", new[] { 3, 2, 1 });
    }

    [Test]
    public void Enabled_GenericExtensionFunction() {
        // Generic extension function
        var result = "x.identity() = x\r y = 42.identity()"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", 42);
    }

    [Test]
    public void Enabled_GenericExtensionNotCallableDirectly() {
        Assert.That(() =>
            "x.identity() = x\r y = identity(42)"
                .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled),
            Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Enabled_ExtensionWithMultipleArgs() {
        var result = "x.clamp(lo, hi) = if(x < lo) lo\r if(x > hi) hi\r else x\r y = 15.clamp(0, 10)"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", 10);
    }

    #endregion

    #region LINQ built-ins marked IExtensionFunction — strict under Enabled

    // Built-ins tagged via IExtensionFunction (count, map, filter, first, last, sort, etc.)
    // stay bi-callable under Disabled (backward-compat) but become piped-only under Enabled.

    [Test]
    public void Disabled_BuiltinCountDirect_Works() =>
        "y = count([1,2,3])".AssertReturns("y", 3);

    [Test]
    public void Disabled_BuiltinCountPiped_Works() =>
        "y = [1,2,3].count()".AssertReturns("y", 3);

    [Test]
    public void Enabled_BuiltinCountDirect_Rejected() {
        Assert.That(() =>
            "y = count([1,2,3])"
                .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled),
            Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Enabled_BuiltinMapDirect_Rejected() {
        Assert.That(() =>
            "y = map([1,2,3], rule it*2)"
                .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled),
            Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Enabled_BuiltinFilterDirect_Rejected() {
        Assert.That(() =>
            "y = filter([1,2,3], rule it > 1)"
                .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled),
            Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Enabled_BuiltinFirstDirect_Rejected() {
        Assert.That(() =>
            "y = first([1,2,3])"
                .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled),
            Throws.InstanceOf<Exception>());
    }

    [Test]
    public void Enabled_BuiltinMapPiped_Works() {
        var result = "y = [1,2,3].map(rule it*2)"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", new[] { 2, 4, 6 });
    }

    [Test]
    public void Enabled_BuiltinFilterPiped_Works() {
        var result = "y = [1,2,3].filter(rule it > 1)"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", new[] { 2, 3 });
    }

    [Test]
    public void Enabled_NonLinqBuiltin_RangeStillDirect() {
        // Factories (range, abs, etc.) are NOT IExtensionFunction — still callable directly under Enabled.
        var result = "y = range(1, 3)"
            .CalcWithDialect(extensionFunctionsSeparation: ExtensionFunctionsSeparation.Enabled);
        result.AssertReturns("y", new[] { 1, 2, 3 });
    }

    #endregion
}
