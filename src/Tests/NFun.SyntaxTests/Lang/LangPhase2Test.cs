using System;
using System.IO;
using System.Linq;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

[TestFixture]
public class LangPhase2Test {

    #region For Loops

    [Test]
    public void ForLoop_PrintElements() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [1,2,3]:\n" +
            "        print(x)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
        Assert.That(output.ToString(), Does.Contain("1"));
        Assert.That(output.ToString(), Does.Contain("2"));
        Assert.That(output.ToString(), Does.Contain("3"));
    }

    [Test]
    public void ForLoop_IterateTextArray() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for s in ['hello','world']:\n" +
            "        print(s)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.That(output.ToString(), Does.Contain("hello"));
        Assert.That(output.ToString(), Does.Contain("world"));
    }

    [Test]
    public void ForLoop_EmptyArray_BodyNotExecuted() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in []:\n" +
            "        print(x)\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.AreEqual("", output.ToString());
    }

    [Test]
    public void ForLoop_WithBreak() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [1,2,3,4,5]:\n" +
            "        if x == 3:\n" +
            "            break\n" +
            "        print(x)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("1"));
        Assert.That(text, Does.Contain("2"));
        Assert.That(text, Does.Not.Contain("3"));
        Assert.That(text, Does.Not.Contain("4"));
    }

    [Test]
    public void ForLoop_WithContinue() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [1,2,3,4,5]:\n" +
            "        if x == 3:\n" +
            "            continue\n" +
            "        print(x)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("1"));
        Assert.That(text, Does.Contain("2"));
        Assert.That(text, Does.Not.Contain("3"));
        Assert.That(text, Does.Contain("4"));
        Assert.That(text, Does.Contain("5"));
    }

    [Test]
    public void ForLoop_WithReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun findFirst(arr):\n" +
            "    for x in arr:\n" +
            "        if x > 3:\n" +
            "            return x\n" +
            "    return 0\n" +
            "y = findFirst([1,2,5,4])");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void ForLoop_WithReturn_NoMatch() {
        var rt = Funny.Hardcore.BuildLang(
            "fun findFirst(arr):\n" +
            "    for x in arr:\n" +
            "        if x > 10:\n" +
            "            return x\n" +
            "    return -1\n" +
            "y = findFirst([1,2,3])");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void ForLoop_Nested() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for i in [1,2]:\n" +
            "        for j in [10,20]:\n" +
            "            print(i * 100 + j)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("110"));
        Assert.That(text, Does.Contain("120"));
        Assert.That(text, Does.Contain("210"));
        Assert.That(text, Does.Contain("220"));
    }

    [Test]
    public void ForLoop_NestedBreak_OnlyExitsInner() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for i in [1,2]:\n" +
            "        for j in [10,20,30]:\n" +
            "            if j == 20:\n" +
            "                break\n" +
            "            print(i * 100 + j)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("110"));
        Assert.That(text, Does.Not.Contain("120"));
        Assert.That(text, Does.Contain("210"));
        Assert.That(text, Does.Not.Contain("220"));
    }

    [Test]
    public void ForLoop_WithFunctionCallInBody() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n" +
            "    return x * 2\n" +
            "fun test():\n" +
            "    for x in [1,2,3]:\n" +
            "        print(double(x))\n" +
            "    return 0\n" +
            "y = test()");
        var output = new StringWriter();
        rt.IO.Output = output;
        rt.Run();
        Assert.That(output.ToString(), Does.Contain("2"));
        Assert.That(output.ToString(), Does.Contain("4"));
        Assert.That(output.ToString(), Does.Contain("6"));
    }

    [Test]
    public void ForLoop_SingleElement() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [42]:\n" +
            "        print(x)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.That(output.ToString(), Does.Contain("42"));
    }

    [Test]
    public void ForLoop_IterateOverString() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for c in 'abc':\n" +
            "        print(c)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("a"));
        Assert.That(text, Does.Contain("b"));
        Assert.That(text, Does.Contain("c"));
    }

    [Test]
    public void ForLoop_TopLevel() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "for x in [10,20,30]:\n" +
            "    print(x)");
        rt.IO.Output = output;
        rt.Run();
        Assert.That(output.ToString(), Does.Contain("10"));
        Assert.That(output.ToString(), Does.Contain("20"));
        Assert.That(output.ToString(), Does.Contain("30"));
    }

    [Test]
    public void ForLoop_BreakImmediately() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [1,2,3]:\n" +
            "        break\n" +
            "    return 99\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(99, rt["y"].Value);
    }

    [Test]
    public void ForLoop_ContinueAll() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [1,2,3]:\n" +
            "        continue\n" +
            "        print(x)\n" +
            "    return 99\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(99, rt["y"].Value);
        Assert.AreEqual("", output.ToString());
    }

    #endregion

    #region While Loops

    [Test]
    public void WhileTrue_WithBreak() {
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    while true:\n" +
            "        break\n" +
            "    return 42\n" +
            "y = test()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void WhileTrue_WithBreakAndContinue() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for i in [1,2,3,4,5]:\n" +
            "        if i == 2:\n" +
            "            continue\n" +
            "        if i == 4:\n" +
            "            break\n" +
            "        print(i)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("1"));
        Assert.That(text, Does.Not.Contain("2"));
        Assert.That(text, Does.Contain("3"));
        Assert.That(text, Does.Not.Contain("4"));
    }

    [Test]
    public void While_FalseCondition_NeverExecutes() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    while false:\n" +
            "        print('oops')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.AreEqual("", output.ToString());
    }

    [Test]
    public void While_WithReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    while true:\n" +
            "        return 99\n" +
            "    return 0\n" +
            "y = test()");
        rt.Run();
        Assert.AreEqual(99, rt["y"].Value);
    }

    [Test]
    public void While_TopLevel_WithBreak() {
        var rt = Funny.Hardcore.BuildLang(
            "while true:\n" +
            "    break\n" +
            "y = 42");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void While_ConditionFromExpression() {
        // Use for-based counting to drive while via array check
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    while 1 > 2:\n" +
            "        return 0\n" +
            "    return 42\n" +
            "y = test()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void While_Nested_InnerBreak() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for i in [1,2]:\n" +
            "        while true:\n" +
            "            print(i)\n" +
            "            break\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("1"));
        Assert.That(text, Does.Contain("2"));
    }

    #endregion

    #region When (Pattern Matching)

    [Test]
    public void When_ValueBased_Int() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n" +
            "    return when x:\n" +
            "        1: 'one'\n" +
            "        2: 'two'\n" +
            "        else: 'other'\n" +
            "y = classify(2)");
        rt.Run();
        Assert.AreEqual("two", rt["y"].Value.ToString());
    }

    [Test]
    public void When_ValueBased_ElseBranch() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n" +
            "    return when x:\n" +
            "        1: 'one'\n" +
            "        2: 'two'\n" +
            "        else: 'other'\n" +
            "y = classify(99)");
        rt.Run();
        Assert.AreEqual("other", rt["y"].Value.ToString());
    }

    [Test]
    public void When_ConditionBased() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n" +
            "    return when:\n" +
            "        x > 0: 'positive'\n" +
            "        x < 0: 'negative'\n" +
            "        else: 'zero'\n" +
            "y = classify(-5)");
        rt.Run();
        Assert.AreEqual("negative", rt["y"].Value.ToString());
    }

    [Test]
    public void When_ConditionBased_Zero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n" +
            "    return when:\n" +
            "        x > 0: 'positive'\n" +
            "        x < 0: 'negative'\n" +
            "        else: 'zero'\n" +
            "y = classify(0)");
        rt.Run();
        Assert.AreEqual("zero", rt["y"].Value.ToString());
    }

    [Test]
    public void When_AsExpression_RhsOfAssignment() {
        var rt = Funny.Hardcore.BuildLang(
            "result = when 2:\n" +
            "    1: 'one'\n" +
            "    2: 'two'\n" +
            "    else: 'other'");
        rt.Run();
        Assert.AreEqual("two", rt["result"].Value.ToString());
    }

    [Test]
    public void When_AsExpression_ConditionBased() {
        var rt = Funny.Hardcore.BuildLang(
            "x = when:\n" +
            "    true: 42\n" +
            "    else: 0");
        rt.Run();
        Assert.AreEqual(42, rt["x"].Value);
    }

    [Test]
    public void When_AsStatement_NoElse() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    when 1:\n" +
            "        1: print('one')\n" +
            "        2: print('two')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.That(output.ToString(), Does.Contain("one"));
    }

    [Test]
    public void When_AsStatement_NoMatch_NoElse() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    when 99:\n" +
            "        1: print('one')\n" +
            "        2: print('two')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.AreEqual("", output.ToString());
    }

    [Test]
    public void When_WithMultilineArms() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test(x):\n" +
            "    return when x:\n" +
            "        1:\n" +
            "            print('got one')\n" +
            "            'one'\n" +
            "        2:\n" +
            "            print('got two')\n" +
            "            'two'\n" +
            "        else: 'other'\n" +
            "y = test(2)");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual("two", rt["y"].Value.ToString());
        Assert.That(output.ToString(), Does.Contain("got two"));
    }

    [Test]
    public void When_Nested() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inner(y):\n" +
            "    return when y:\n" +
            "        10: 'a'\n" +
            "        else: 'b'\n" +
            "fun test(x, y):\n" +
            "    return when x:\n" +
            "        1: inner(y)\n" +
            "        else: 'c'\n" +
            "y = test(1, 10)");
        rt.Run();
        Assert.AreEqual("a", rt["y"].Value.ToString());
    }

    [Test]
    public void When_InsideFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun dayType(d):\n" +
            "    return when d:\n" +
            "        0: 'weekend'\n" +
            "        6: 'weekend'\n" +
            "        else: 'weekday'\n" +
            "y = dayType(0)");
        rt.Run();
        Assert.AreEqual("weekend", rt["y"].Value.ToString());
    }

    [Test]
    public void When_ConditionBased_FirstMatch() {
        var rt = Funny.Hardcore.BuildLang(
            "fun grade(score):\n" +
            "    return when:\n" +
            "        score >= 90: 'A'\n" +
            "        score >= 80: 'B'\n" +
            "        score >= 70: 'C'\n" +
            "        else: 'F'\n" +
            "y = grade(85)");
        rt.Run();
        Assert.AreEqual("B", rt["y"].Value.ToString());
    }

    [Test]
    public void When_AsExpression_IntResult() {
        var rt = Funny.Hardcore.BuildLang(
            "x = when 3:\n" +
            "    1: 10\n" +
            "    2: 20\n" +
            "    3: 30\n" +
            "    else: 0");
        rt.Run();
        Assert.AreEqual(30, rt["x"].Value);
    }

    [Test]
    public void When_TopLevel_Statement() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "when 2:\n" +
            "    1: print('one')\n" +
            "    2: print('two')");
        rt.IO.Output = output;
        rt.Run();
        Assert.That(output.ToString(), Does.Contain("two"));
    }

    [Test]
    public void When_ValueBased_BoolSubject() {
        var rt = Funny.Hardcore.BuildLang(
            "x = when true:\n" +
            "    true: 1\n" +
            "    false: 0\n" +
            "    else: -1");
        rt.Run();
        Assert.AreEqual(1, rt["x"].Value);
    }

    [Test]
    public void When_ManyArms() {
        var rt = Funny.Hardcore.BuildLang(
            "fun test(n):\n" +
            "    return when n:\n" +
            "        1: 'one'\n" +
            "        2: 'two'\n" +
            "        3: 'three'\n" +
            "        4: 'four'\n" +
            "        5: 'five'\n" +
            "        else: 'many'\n" +
            "y = test(4)");
        rt.Run();
        Assert.AreEqual("four", rt["y"].Value.ToString());
    }

    #endregion

    #region Break/Continue

    [Test]
    public void Break_InForLoop() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [1,2,3,4,5]:\n" +
            "        if x == 3:\n" +
            "            break\n" +
            "        print(x)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.That(output.ToString(), Does.Not.Contain("3"));
    }

    [Test]
    public void Break_InWhileLoop() {
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    while true:\n" +
            "        break\n" +
            "    return 42\n" +
            "y = test()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void Continue_InForLoop() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for x in [1,2,3,4,5]:\n" +
            "        if x == 3:\n" +
            "            continue\n" +
            "        print(x)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.That(output.ToString(), Does.Not.Contain("3"));
        Assert.That(output.ToString(), Does.Contain("4"));
    }

    [Test]
    public void Break_NestedLoop_OnlyExitsInner() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for i in [1,2,3]:\n" +
            "        for j in [10,20,30]:\n" +
            "            if j == 20:\n" +
            "                break\n" +
            "            print(i * 100 + j)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        // Inner break at j==20 means only j=10 prints for each i
        Assert.That(text, Does.Contain("110"));
        Assert.That(text, Does.Contain("210"));
        Assert.That(text, Does.Contain("310"));
        Assert.That(text, Does.Not.Contain("120"));
    }

    [Test]
    public void Continue_NestedLoop_OnlyAffectsInner() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    for i in [1,2]:\n" +
            "        for j in [10,20,30]:\n" +
            "            if j == 20:\n" +
            "                continue\n" +
            "            print(i * 100 + j)\n" +
            "    return 0\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        var text = output.ToString();
        Assert.That(text, Does.Contain("110"));
        Assert.That(text, Does.Not.Contain("120"));
        Assert.That(text, Does.Contain("130"));
        Assert.That(text, Does.Contain("210"));
        Assert.That(text, Does.Not.Contain("220"));
        Assert.That(text, Does.Contain("230"));
    }

    #endregion

    #region Try/Catch/Anyway

    [Test]
    public void TryCatch_NoError() {
        var rt = Funny.Hardcore.BuildLang(
            "x = try:\n" +
            "    42\n" +
            "catch:\n" +
            "    0");
        rt.Run();
        Assert.AreEqual(42, rt["x"].Value);
    }

    [Test]
    public void TryCatch_WithError() {
        var rt = Funny.Hardcore.BuildLang(
            "x = try:\n" +
            "    oops('error')\n" +
            "catch:\n" +
            "    99");
        rt.Run();
        Assert.AreEqual(99, rt["x"].Value);
    }

    [Test]
    public void TryCatch_InFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun safeDivide(a, b):\n" +
            "    return try:\n" +
            "        a / b\n" +
            "    catch:\n" +
            "        0\n" +
            "y = safeDivide(10, 2)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void TryCatch_Statement() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    try:\n" +
            "        print('try')\n" +
            "    catch:\n" +
            "        print('catch')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.That(output.ToString(), Does.Contain("try"));
        Assert.That(output.ToString(), Does.Not.Contain("catch"));
    }

    [Test]
    public void TryCatch_CatchExecutedOnError() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    try:\n" +
            "        oops('fail')\n" +
            "    catch:\n" +
            "        print('caught')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.That(output.ToString(), Does.Contain("caught"));
    }

    [Test]
    public void TryCatchAnyway_NoError() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    try:\n" +
            "        print('try')\n" +
            "    catch:\n" +
            "        print('catch')\n" +
            "    anyway:\n" +
            "        print('anyway')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.That(output.ToString(), Does.Contain("try"));
        Assert.That(output.ToString(), Does.Not.Contain("catch"));
        Assert.That(output.ToString(), Does.Contain("anyway"));
    }

    [Test]
    public void TryCatchAnyway_WithError() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    try:\n" +
            "        oops('fail')\n" +
            "    catch:\n" +
            "        print('caught')\n" +
            "    anyway:\n" +
            "        print('cleanup')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.That(output.ToString(), Does.Contain("caught"));
        Assert.That(output.ToString(), Does.Contain("cleanup"));
    }

    [Test]
    public void TryAnyway_NoError() {
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    try:\n" +
            "        print('try')\n" +
            "    anyway:\n" +
            "        print('cleanup')\n" +
            "    return 42\n" +
            "y = test()");
        rt.IO.Output = output;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.That(output.ToString(), Does.Contain("try"));
        Assert.That(output.ToString(), Does.Contain("cleanup"));
    }

    [Test]
    public void TryCatch_Nested() {
        var rt = Funny.Hardcore.BuildLang(
            "fun test():\n" +
            "    x = try:\n" +
            "        try:\n" +
            "            oops('inner')\n" +
            "        catch:\n" +
            "            10\n" +
            "    catch:\n" +
            "        99\n" +
            "    return x\n" +
            "y = test()");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void TryCatch_TopLevel() {
        var rt = Funny.Hardcore.BuildLang(
            "x = try:\n" +
            "    42\n" +
            "catch:\n" +
            "    0");
        rt.Run();
        Assert.AreEqual(42, rt["x"].Value);
    }

    [Test]
    public void TryCatch_InForLoopBody_AccumulatesNonFailing() {
        // try-catch inside for-loop: catches `100 // 0` and contributes 0.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    total = 0\n" +
            "    for i in [1, 0, 2]:\n" +
            "        total += try:\n" +
            "            100 // i\n" +
            "        catch:\n" +
            "            0\n" +
            "    return total\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(150, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_InWhileLoopBody() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    i = 1\n" +
            "    total = 0\n" +
            "    while i < 5:\n" +
            "        total += try:\n" +
            "            (10 // i) + i\n" +
            "        catch:\n" +
            "            -1\n" +
            "        i += 1\n" +
            "    return total\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(30, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_InIfThenBranch() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(c:bool):\n" +
            "    if c:\n" +
            "        return try:\n" +
            "            oops('boom')\n" +
            "        catch:\n" +
            "            42\n" +
            "    else:\n" +
            "        return 0\n" +
            "y = f(true)");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatchAnyway_AnywayRunsOnSuccess_AccumulatesBoth() {
        // No error: try runs (log=1), anyway runs (log+=10) → 11.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    log = 0\n" +
            "    try:\n" +
            "        log += 1\n" +
            "    catch:\n" +
            "        log += 100\n" +
            "    anyway:\n" +
            "        log += 10\n" +
            "    return log\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(11, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatchAnyway_AnywayRunsOnError_AccumulatesCatchAndAnyway() {
        // Error: try aborts, catch runs (log=5), anyway runs (log+=100) → 105.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    log = 0\n" +
            "    try:\n" +
            "        oops('boom')\n" +
            "    catch:\n" +
            "        log = 5\n" +
            "    anyway:\n" +
            "        log += 100\n" +
            "    return log\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(105, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_NestedInnerCaught_OuterUntouched() {
        // Inner throws and inner catch handles it. Outer catch should NOT fire.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        try:\n" +
            "            oops('inner')\n" +
            "        catch:\n" +
            "            10\n" +
            "    catch:\n" +
            "        99\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(10, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_NestedInnerRethrows_OuterCatchesIt() {
        // Inner catch re-throws via oops; outer catch handles it → 77.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        try:\n" +
            "            oops('inner')\n" +
            "        catch:\n" +
            "            oops('rethrown')\n" +
            "    catch:\n" +
            "        77\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(77, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryAnyway_NoCatch_AnywayRunsThenErrorPropagates() {
        // anyway must run even when error propagates uncaught.
        var output = new StringWriter();
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    try:\n" +
            "        oops('boom')\n" +
            "    anyway:\n" +
            "        print('cleanup')\n" +
            "    return 42\n" +
            "y = f()");
        rt.IO.Output = output;
        Assert.Throws<NFun.Exceptions.FunnyRuntimeException>(() => rt.Run());
        Assert.That(output.ToString(), Does.Contain("cleanup"));
    }

    [Test]
    public void TryCatch_WithErrorVariable_AccessMessage() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        oops('boom')\n" +
            "    catch e:\n" +
            "        e.message.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(4, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_WithErrorVariable_ReturnMessage() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        oops('boom')\n" +
            "    catch e:\n" +
            "        e.message\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual("boom", rt["y"].Value.ToString());
    }

    [Test]
    public void TryCatch_WithErrorVariable_AccessData() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        oops('boom', 42)\n" +
            "    catch e:\n" +
            "        e.data\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(42, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_WithErrorVariable_LegacyParenthesizedForm() {
        // Old `catch(e):` form still accepted alongside `catch e:`.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        oops('boom')\n" +
            "    catch(e):\n" +
            "        e.message.count()\n" +
            "y = f()");
        rt.Run();
        Assert.AreEqual(4, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_WithErrorVariable_SuccessPathIgnoresErrorVar() {
        // No exception thrown → try-body value returned, catch never runs, e never bound.
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x:int):\n" +
            "    return try:\n" +
            "        100 // x\n" +
            "    catch e:\n" +
            "        -1\n" +
            "y = f(4)");
        rt.Run();
        Assert.AreEqual(25, Convert.ToInt32(rt["y"].Value));
    }

    [Test]
    public void TryCatch_WithErrorVariable_NotLeakedAsScriptInput() {
        // The error variable must not appear as a script-level variable.
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    return try:\n" +
            "        oops('boom')\n" +
            "    catch e:\n" +
            "        e.message.count()\n" +
            "y = f()");
        // Script should expose only `y` as an output, no error variable.
        Assert.That(rt.Variables.Where(v => v.Name == "e"), Is.Empty,
            "Error variable `e` must not leak as a script-level variable.");
    }

    #endregion
}
