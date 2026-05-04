using System;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

[TestFixture]
public class LangCompoundAssignTest {

    [Test]
    public void PlusEquals() {
        var rt = Funny.Hardcore.BuildLang("x = 10\nx += 5\ny = x");
        rt.Run();
        Assert.AreEqual(15, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void MinusEquals() {
        var rt = Funny.Hardcore.BuildLang("x = 10\nx -= 3\ny = x");
        rt.Run();
        Assert.AreEqual(7, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void MulEquals() {
        var rt = Funny.Hardcore.BuildLang("x = 5\nx *= 3\ny = x");
        rt.Run();
        Assert.AreEqual(15, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void DivEquals() {
        var rt = Funny.Hardcore.BuildLang("x = 10.0\nx /= 2.0\ny = x");
        rt.Run();
        Assert.AreEqual(5.0, rt["y"].Value);
    }

    [Test]
    public void ModEquals() {
        var rt = Funny.Hardcore.BuildLang("x = 10\nx %= 3\ny = x");
        rt.Run();
        Assert.AreEqual(1, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void IntDivEquals() {
        var rt = Funny.Hardcore.BuildLang("x = 10\nx //= 3\ny = x");
        rt.Run();
        Assert.AreEqual(3, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void CompoundInWhileLoop() {
        var rt = Funny.Hardcore.BuildLang("sum = 0\ni = 0\nwhile i < 10:\n    sum += i\n    i += 1\ny = sum");
        rt.Run();
        Assert.AreEqual(45, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void CompoundInForLoop() {
        var rt = Funny.Hardcore.BuildLang("sum = 0\nfor x in [1,2,3,4,5]:\n    sum += x\ny = sum");
        rt.Run();
        Assert.AreEqual(15, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void MultipleCompoundAssignments() {
        var rt = Funny.Hardcore.BuildLang("x = 1\nx += 2\nx *= 3\nx -= 1\ny = x");
        rt.Run();
        Assert.AreEqual(8, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void CompoundWithExpression() {
        var rt = Funny.Hardcore.BuildLang("x = 10\nx += 2 * 3\ny = x");
        rt.Run();
        Assert.AreEqual(16, Convert.ToInt32(rt["y"].Value));
    }
}
