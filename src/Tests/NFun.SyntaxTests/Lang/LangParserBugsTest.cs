using System;
using NFun.Exceptions;
using NFun.Functions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

/// <summary>
/// Reproduce and verify fixes for parser bugs found in algorithm tests.
/// </summary>
[TestFixture]
public class LangParserBugsTest {

    // Bug 1: @Test without parens + fun with zero args
    // Error: "Type testX has wrong generic definition"
    [Test]
    public void Bug_TestAnnotationNoParens_ZeroArgFunction() {
        var rt = Funny.Hardcore.WithTestKit().BuildLang(
            "@Test\nfun testBasic():\n    assert(true)");
        rt.Run();
    }

    // Bug 1b: @Test without parens works in other .fun files — check what's different
    [Test]
    public void Bug_TestAnnotationNoParens_WithPrecedingFunctions() {
        var rt = Funny.Hardcore.WithTestKit().BuildLang(
            "fun helper():\n    return 42\n\n@Test\nfun testHelper():\n    assertEqual(helper(), 42)");
        rt.Run();
    }

    // Bug 2: return when: (condition-based when after return)
    [Test]
    public void Bug_ReturnWhenConditionBased() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(n):\n    return when:\n        n > 0: 'pos'\n        n < 0: 'neg'\n        else: 'zero'\ny = classify(5)");
        rt.Run();
        Assert.AreEqual("pos", rt["y"].Value.ToString());
    }

    // Bug 2b: return when x: (value-based when after return)
    [Test]
    public void Bug_ReturnWhenValueBased() {
        var rt = Funny.Hardcore.BuildLang(
            "fun name(n):\n    return when n:\n        1: 'one'\n        2: 'two'\n        else: 'other'\ny = name(2)");
        rt.Run();
        Assert.AreEqual("two", rt["y"].Value.ToString());
    }

    // Bug 3: Multiple @Test annotations followed by fun
    [Test]
    public void Bug_MultipleTestAnnotations() {
        var rt = Funny.Hardcore.WithTestKit().BuildLang(
            "@Test(1, 1)\n@Test(2, 4)\nfun testSquare(x, expected):\n    assertEqual(x * x, expected)");
        rt.Run();
    }

    // Bug 4: @Test with no parens, function with complex body
    [Test]
    public void Bug_TestNoParens_ComplexBody() {
        var rt = Funny.Hardcore.WithTestKit().BuildLang(
            "fun fib(n):\n    if n <= 1: return n\n    return fib(n-1) + fib(n-2)\n\n" +
            "@Test\nfun testFib():\n    assertEqual(fib(5), 5)\n    assertEqual(fib(10), 55)");
        rt.Run();
    }

    // Bug 5: when as expression on RHS with condition-based arms
    [Test]
    public void Bug_WhenConditionBasedAsExpression() {
        var rt = Funny.Hardcore.BuildLang(
            "x = 5\nresult = when:\n    x > 0: 'pos'\n    x < 0: 'neg'\n    else: 'zero'");
        rt.Run();
        Assert.AreEqual("pos", rt["result"].Value.ToString());
    }

    // Bug: SelfTestRunner appending zero-arg call to script
    [Test]
    public void Bug_SelfTestRunner_AppendedZeroArgCall() {
        var script = "fun myHelper():\n    return 42\n\n@Test\nfun testMyHelper():\n    assertEqual(myHelper(), 42)";
        var fullScript = script + "\ntestMyHelper()";
        var rt = Funny.Hardcore.WithTestKit().BuildLang(fullScript);
        rt.Run();
    }

    // Bug: larger script with many functions + appended call
    [Test]
    public void Bug_SelfTestRunner_LargerScript() {
        var script =
            "fun fib(n):\n    if n <= 1: return n\n    return fib(n-1) + fib(n-2)\n\n" +
            "fun gcd(a, b):\n    if b == 0: return a\n    return gcd(b, a % b)\n\n" +
            "@Test\nfun testBinarySearch():\n    assertEqual(fib(5), 5)\n    assertEqual(gcd(12, 8), 4)";
        var fullScript = script + "\ntestBinarySearch()";
        var rt = Funny.Hardcore.WithTestKit().BuildLang(fullScript);
        rt.Run();
    }

    // Bug repro: binarySearch with while loop + zero-arg @Test + appended call
    [Test]
    public void Bug_ZeroArgTestWithWhileLoop() {
        var script =
            "fun binarySearch(arr, target):\n" +
            "    lo = 0\n" +
            "    hi = arr.count() - 1\n" +
            "    while lo <= hi:\n" +
            "        mid = (lo + hi) // 2\n" +
            "        if arr[mid] == target:\n" +
            "            return mid\n" +
            "        elif arr[mid] < target:\n" +
            "            lo = mid + 1\n" +
            "        else:\n" +
            "            hi = mid - 1\n" +
            "    return -1\n\n" +
            "fun testBS():\n" +
            "    assertEqual(binarySearch([2, 5, 8, 12, 16, 23], 5), 1)\n";
        var fullScript = script + "\ntestBS()";
        var rt = Funny.Hardcore.WithTestKit().BuildLang(fullScript);
        rt.Run();
    }

    // Bug: recursive function with local var in lang mode
    [Test]
    public void Bug_RecursiveWithLocalVar() {
        var rt = Funny.Hardcore.BuildLang(
            "fun power(x, n):\n" +
            "    if n == 0: return 1\n" +
            "    if n % 2 == 0:\n" +
            "        half = power(x, n // 2)\n" +
            "        return half * half\n" +
            "    return x * power(x, n - 1)\n" +
            "y = power(2, 5)");
        rt.Run();
        Assert.AreEqual(32, Convert.ToInt32(rt["y"].Value));
    }

    // Control: recursive without local var works?
    [Test]
    public void Bug_RecursiveWithoutLocalVar() {
        var rt = Funny.Hardcore.BuildLang(
            "fun power(x, n):\n" +
            "    if n == 0: return 1\n" +
            "    if n % 2 == 0:\n" +
            "        return power(x, n // 2) * power(x, n // 2)\n" +
            "    return x * power(x, n - 1)\n" +
            "y = power(2, 5)");
        rt.Run();
        Assert.AreEqual(32, Convert.ToInt32(rt["y"].Value));
    }

    // Control: simpler recursion with local var
    [Test]
    public void Bug_SimpleRecursiveWithLocalVar() {
        var rt = Funny.Hardcore.BuildLang(
            "fun fact(n):\n" +
            "    if n <= 1: return 1\n" +
            "    prev = fact(n - 1)\n" +
            "    return n * prev\n" +
            "y = fact(5)");
        rt.Run();
        Assert.AreEqual(120, Convert.ToInt32(rt["y"].Value));
    }

    // Bug 3: SelfTestRunner + struct functions
    [Test]
    public void Bug_SelfTestRunner_StructFunction() {
        var script =
            "fun manhattan(p1, p2):\n" +
            "    return abs(p1.x - p2.x) + abs(p1.y - p2.y)\n\n" +
            "@Test\nfun testManhattan():\n" +
            "    a = {x = 0, y = 0}\n" +
            "    b = {x = 3, y = 4}\n" +
            "    assertEqual(manhattan(a, b), 7)";
        var fullScript = script + "\ntestManhattan()";
        var rt = Funny.Hardcore.WithTestKit().BuildLang(fullScript);
        rt.Run();
    }

    [Test]
    public void Bug_SelfTestRunner_StructFieldMutation() {
        var script =
            "fun swapXY(s):\n" +
            "    tmp = s.x\n" +
            "    s.x = s.y\n" +
            "    s.y = tmp\n" +
            "    return s\n\n" +
            "@Test\nfun testSwap():\n" +
            "    p = {x = 10, y = 20}\n" +
            "    q = swapXY(p)\n" +
            "    assertEqual(q.x, 20)\n" +
            "    assertEqual(q.y, 10)";
        var fullScript = script + "\ntestSwap()";
        var rt = Funny.Hardcore.WithTestKit().BuildLang(fullScript);
        rt.Run();
    }
}
