using System;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

/// <summary>
/// Tests for:
/// 1. Single-line block syntax: if cond: expr, for x in arr: expr, while cond: expr
/// 2. return as expression operator: x ?? return, if cond: return
/// 3. when without colon after subject
/// </summary>
[TestFixture]
public class LangSingleLineAndReturnTest {

    #region 1. Single-line blocks

    [Test]
    public void IfSingleLine() {
        var rt = Funny.Hardcore.BuildLang("y = 0\nif true: y = 42");
        rt.Run();
        // Note: y = 0 then y = 42 — two separate equations. Last one wins.
    }

    [Test]
    public void IfSingleLine_WithElse() {
        var rt = Funny.Hardcore.BuildLang("y = if true: 1 else: 0");
        rt.Run();
        Assert.AreEqual(1, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ForSingleLine() {
        // for x in arr: print(x) — single-line for
        var rt = Funny.Hardcore.BuildLang("y = 0\nfor x in [1,2,3]: y = x");
        rt.Run();
    }

    [Test]
    public void WhileSingleLine() {
        var rt = Funny.Hardcore.BuildLang("while false: print('never')");
        rt.Run();
    }

    [Test]
    public void WhenSingleLine() {
        var rt = Funny.Hardcore.BuildLang("x = 2\ny = when x: 1: 'one' 2: 'two' else: 'other'");
        rt.Run();
    }

    #endregion

    #region 2. return as operator

    [Test]
    public void ReturnInCoalesceOperator() {
        // fun foo(x): a = x ?? return; return a + 1
        // foo(42) should return 43, foo(none) should return none
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(x):\n    a = x ?? return\n    return a + 1\ny = foo(42)");
        rt.Run();
        Assert.AreEqual(43, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ReturnInCoalesceOperator_NoneInput() {
        var rt = Funny.Hardcore.BuildLang(
            "fun foo(x):\n    a = x ?? return\n    return a + 1\ny = foo(none)");
        rt.Run();
        // foo(none) → x is none → x ?? return → return (none) → foo returns none
    }

    [Test]
    public void ReturnBare_InFunction() {
        // bare return (no value) returns none
        var rt = Funny.Hardcore.BuildLang(
            "fun greet(name):\n    print(name)\n    return\ny = greet('hello')");
        rt.Run();
    }

    #endregion

    #region 3. when without colon after subject

    [Test]
    public void WhenWithoutColonAfterSubject() {
        // when x (no colon) — subject on same line, arms indented
        var rt = Funny.Hardcore.BuildLang(
            "x = 2\nresult = when x\n    1: 'one'\n    2: 'two'\n    else: 'other'");
        rt.Run();
        Assert.AreEqual("two", rt["result"].Value.ToString());
    }

    [Test]
    public void WhenWithColonAfterSubject() {
        // when x: (with colon) — should also work
        var rt = Funny.Hardcore.BuildLang(
            "x = 2\nresult = when x:\n    1: 'one'\n    2: 'two'\n    else: 'other'");
        rt.Run();
        Assert.AreEqual("two", rt["result"].Value.ToString());
    }

    #endregion

    #region 4. break/continue as expression

    [Test]
    public void CoalesceContinue_SkipsNone() {
        // x ?? continue — skip none values in loop
        var rt = Funny.Hardcore.BuildLang(
            "sum = 0\nfor item in [1, none, 3, none, 5]:\n    v = item ?? continue\n    sum += v\ny = sum");
        rt.Run();
        Assert.AreEqual(9, Convert.ToInt32(rt["y"].Value)); // 1+3+5
    }

    [Test]
    public void CoalesceBreak_StopsAtNone() {
        // x ?? break — stop at first none
        var rt = Funny.Hardcore.BuildLang(
            "sum = 0\nfor item in [1, 2, none, 4, 5]:\n    v = item ?? break\n    sum += v\ny = sum");
        rt.Run();
        Assert.AreEqual(3, Convert.ToInt32(rt["y"].Value)); // 1+2
    }

    [Test]
    public void CoalesceBreak_InWhile() {
        var rt = Funny.Hardcore.BuildLang(
            "items = [10, 20, none, 40]\ni = 0\nsum = 0\nwhile i < 4:\n    v = items[i] ?? break\n    sum += v\n    i += 1\ny = sum");
        rt.Run();
        Assert.AreEqual(30, Convert.ToInt32(rt["y"].Value)); // 10+20
    }

    #endregion
}
