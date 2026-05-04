using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

[TestFixture]
public class FunLambdaTest {

    #region fun as expression lambda (like rule)

    [Test]
    public void FunAsLambda_SuperAnonymous_Map() {
        // fun with implicit 'it' — super anonymous function
        var rt = Funny.Hardcore.BuildLang("y = [1,2,3].map(fun it * 2).count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void FunAsLambda_SuperAnonymous_MapValues() {
        var rt = Funny.Hardcore.BuildLang("y = [1,2,3].map(fun it * 2)");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 2, 4, 6 }, result);
    }

    [Test]
    public void FunAsLambda_ExplicitArg_Filter() {
        var rt = Funny.Hardcore.BuildLang("y = [1,2,3,4,5].filter(fun(x) = x > 3).count()");
        rt.Run();
        Assert.AreEqual(2, rt["y"].Value);
    }

    [Test]
    public void FunAsLambda_ExplicitArgs_Map() {
        var rt = Funny.Hardcore.BuildLang("y = [1,2,3].map(fun(x) = x * 10)");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 10, 20, 30 }, result);
    }

    [Test]
    public void FunAsLambda_InFunctionDefinition() {
        var rt = Funny.Hardcore.BuildLang(
            "fun doubled(items):\n" +
            "    return items.map(fun it * 2)\n" +
            "\n" +
            "y = doubled([1,2,3])");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 2, 4, 6 }, result);
    }

    [Test]
    public void FunAsLambda_WithExplicitArgInFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun tripled(items):\n" +
            "    return items.map(fun(x) = x * 3)\n" +
            "\n" +
            "y = tripled([10,20])");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 30, 60 }, result);
    }

    #endregion

    #region Block lambdas

    [Test]
    public void BlockLambda_SimpleMap() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [1,2,3].map(fun(x):\n" +
            "    a = x * 2\n" +
            "    a + 1\n" +
            ")");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 3, 5, 7 }, result);
    }

    [Test]
    public void BlockLambda_SingleStatement() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [1,2,3].map(fun(x):\n" +
            "    x * 10\n" +
            ")");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 10, 20, 30 }, result);
    }

    [Test]
    public void BlockLambda_MultipleLocalVars() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [1,2,3].map(fun(x):\n" +
            "    a = x * 2\n" +
            "    b = a + 1\n" +
            "    b * 3\n" +
            ")");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 9, 15, 21 }, result);
    }

    [Test]
    public void BlockLambda_InsideFunctionDefinition() {
        var rt = Funny.Hardcore.BuildLang(
            "fun process(items):\n" +
            "    return items.map(fun(x):\n" +
            "        a = x * 10\n" +
            "        a + 1\n" +
            "    )\n" +
            "\n" +
            "y = process([1,2,3])");
        rt.Run();
        var result = (int[])rt["y"].Value;
        Assert.AreEqual(new[] { 11, 21, 31 }, result);
    }

    [Test]
    public void BlockLambda_Count() {
        var rt = Funny.Hardcore.BuildLang(
            "y = [1,2,3].map(fun(x):\n" +
            "    x + 1\n" +
            ").count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    #endregion
}
