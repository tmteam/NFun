using System;
using NUnit.Framework;
using NFun.Functions;
using NFun.Runtime;

namespace NFun.SyntaxTests.Lang;

/// <summary>Tests for FunnyTestKit: assert, assertEqual, assertType.</summary>
[TestFixture]
public class TestKitTest {

    private FunnyRuntime BuildWithTestKit(string script) =>
        Funny.Hardcore.WithTestKit().BuildLang(script);

    // ═══════════════════════════════════════════════
    //  assert(condition)
    // ═══════════════════════════════════════════════

    [Test]
    public void Assert_True_Passes() {
        var rt = BuildWithTestKit("assert(true)\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void Assert_False_Throws() {
        var rt = BuildWithTestKit("assert(false)\ny = 1");
        Assert.Catch<Exception>(() => rt.Run());
    }

    [Test]
    public void Assert_Expression_Passes() {
        var rt = BuildWithTestKit("assert(2 + 2 == 4)\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void Assert_Expression_Fails() {
        var rt = BuildWithTestKit("assert(2 + 2 == 5)\ny = 1");
        Assert.Catch<Exception>(() => rt.Run());
    }

    // ═══════════════════════════════════════════════
    //  assert(condition, message)
    // ═══════════════════════════════════════════════

    [Test]
    public void AssertWithMessage_True_Passes() {
        var rt = BuildWithTestKit("assert(true, 'should pass')\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertWithMessage_False_ThrowsWithMessage() {
        var rt = BuildWithTestKit("assert(false, 'custom error')\ny = 1");
        var ex = Assert.Catch<Exception>(() => rt.Run());
        Assert.That(ex.Message, Does.Contain("custom error"));
    }

    // ═══════════════════════════════════════════════
    //  assertEqual(actual, expected)
    // ═══════════════════════════════════════════════

    [Test]
    public void AssertEqual_SameInt_Passes() {
        var rt = BuildWithTestKit("assertEqual(42, 42)\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertEqual_DifferentInt_Fails() {
        var rt = BuildWithTestKit("assertEqual(42, 43)\ny = 1");
        var ex = Assert.Catch<Exception>(() => rt.Run());
        Assert.That(ex.Message, Does.Contain("42"));
        Assert.That(ex.Message, Does.Contain("43"));
    }

    [Test]
    public void AssertEqual_ComputedValues_Passes() {
        var rt = BuildWithTestKit(
            "fun add(a, b):\n    return a + b\nassertEqual(add(2, 3), 5)\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertEqual_ComputedValues_Fails() {
        var rt = BuildWithTestKit(
            "fun add(a, b):\n    return a + b\nassertEqual(add(2, 3), 6)\ny = 1");
        Assert.Catch<Exception>(() => rt.Run());
    }

    [Test]
    public void AssertEqual_Booleans() {
        var rt = BuildWithTestKit("assertEqual(true, true)\nassertEqual(false, false)\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  assertNotEqual(actual, notExpected)
    // ═══════════════════════════════════════════════

    [Test]
    public void AssertNotEqual_Different_Passes() {
        var rt = BuildWithTestKit("assertNotEqual(1, 2)\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertNotEqual_Same_Fails() {
        var rt = BuildWithTestKit("assertNotEqual(42, 42)\ny = 1");
        Assert.Catch<Exception>(() => rt.Run());
    }

    // ═══════════════════════════════════════════════
    //  assertType(value, expectedType)
    // ═══════════════════════════════════════════════

    [Test]
    public void AssertType_Int() {
        var rt = BuildWithTestKit("assertType(42, 'int')\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertType_Real() {
        var rt = BuildWithTestKit("assertType(3.14, 'real')\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertType_Bool() {
        var rt = BuildWithTestKit("assertType(true, 'bool')\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertType_Text() {
        var rt = BuildWithTestKit("assertType('hello', 'text')\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertType_IntArray() {
        var rt = BuildWithTestKit("assertType([1,2,3], 'int[]')\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AssertType_Wrong_Fails() {
        var rt = BuildWithTestKit("assertType(42, 'text')\ny = 1");
        var ex = Assert.Catch<Exception>(() => rt.Run());
        Assert.That(ex.Message, Does.Contain("int"));
        Assert.That(ex.Message, Does.Contain("text"));
    }

    [Test]
    public void AssertType_ComputedValue() {
        var rt = BuildWithTestKit(
            "fun double(x):\n    return x * 2\nassertType(double(5), 'int')\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    // ═══════════════════════════════════════════════
    //  Realistic test scenarios
    // ═══════════════════════════════════════════════

    [Test]
    public void Scenario_TestSuite() {
        var rt = BuildWithTestKit(
            "fun add(a, b):\n" +
            "    return a + b\n\n" +
            "fun testAdd():\n" +
            "    assertEqual(add(1, 2), 3)\n" +
            "    assertEqual(add(0, 0), 0)\n" +
            "    assertEqual(add(-1, 1), 0)\n" +
            "    return true\n\n" +
            "passed = testAdd()");
        rt.Run();
        Assert.AreEqual(true, rt["passed"].Value);
    }

    [Test]
    public void Scenario_MultipleAsserts() {
        var rt = BuildWithTestKit(
            "fun add(a, b):\n" +
            "    return a + b\n\n" +
            "assertEqual(add(1, 2), 3)\n" +
            "assertEqual(add(0, 0), 0)\n" +
            "assertEqual(add(10, 20), 30)\n" +
            "y = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void Scenario_TypeChecks() {
        var rt = BuildWithTestKit(
            "x = 42\n" +
            "assertType(x, 'int')\n" +
            "assertType(3.14, 'real')\n" +
            "assertType(true, 'bool')\n" +
            "y = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }
}
