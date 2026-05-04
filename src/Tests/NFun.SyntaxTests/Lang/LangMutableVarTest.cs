using System;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

[TestFixture]
public class LangMutableVarTest {

    [Test]
    public void SimpleReassignment() {
        var rt = Funny.Hardcore.BuildLang("x = 42\nx = 10\ny = x");
        rt.Run();
        Assert.AreEqual(10, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void SelfReassignment() {
        var rt = Funny.Hardcore.BuildLang("x = 10\nx = x + 1\ny = x");
        rt.Run();
        Assert.AreEqual(11, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void MultipleReassignments() {
        var rt = Funny.Hardcore.BuildLang("a = 1\nb = 2\na = a + b\nb = a + b\ny = a + b");
        rt.Run();
        // a=1, b=2, a=1+2=3, b=3+2=5, y=3+5=8
        Assert.AreEqual(8, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ReassignmentInWhileLoop() {
        var rt = Funny.Hardcore.BuildLang("x = 0\nwhile x < 5:\n    x = x + 1\ny = x");
        rt.Run();
        Assert.AreEqual(5, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ForLoopAccumulator() {
        var rt = Funny.Hardcore.BuildLang("sum = 0\nfor item in [1,2,3,4,5]:\n    sum = sum + item\ny = sum");
        rt.Run();
        Assert.AreEqual(15, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void WhileCountdown() {
        var rt = Funny.Hardcore.BuildLang(
            "n = 10\nresult = 1\nwhile n > 0:\n    result = result * n\n    n = n - 1\ny = result");
        rt.Run();
        // 10! = 3628800
        Assert.AreEqual(3628800, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ReassignmentDoesNotAffectExpressionMode() {
        // In expression mode (Build, not BuildLang), reassignment should still be an error
        Assert.Throws<FunnyParseException>(() =>
            Funny.Hardcore.Build("x = 42\nx = 10"));
    }

    [Test]
    public void ReassignToSameValue() {
        var rt = Funny.Hardcore.BuildLang("x = 42\nx = 42\ny = x");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void ReassignmentWithDifferentExpressions() {
        var rt = Funny.Hardcore.BuildLang("x = 1 + 2\nx = x * 3\ny = x");
        rt.Run();
        // x = 3, x = 3*3 = 9
        Assert.AreEqual(9, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void FunctionBodyReassignment() {
        // Reassignment inside function body (already works via BlockExpressionNode)
        var rt = Funny.Hardcore.BuildLang(
            "fun sum_arr(arr):\n    total = 0\n    for item in arr:\n        total = total + item\n    return total\ny = sum_arr([1,2,3,4,5])");
        rt.Run();
        Assert.AreEqual(15, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TopLevelReassignmentReadInFunction() {
        var rt = Funny.Hardcore.BuildLang("x = 10\nx = x + 5\ny = x * 2");
        rt.Run();
        // x=10, x=15, y=30
        Assert.AreEqual(30, Convert.ToInt32(rt["y"].Value));
    }
}
