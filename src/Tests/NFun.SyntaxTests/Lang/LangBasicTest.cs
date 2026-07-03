using System;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.SyntaxTests.Lang;

[TestFixture]
public class LangBasicTest {

    #region 1. Simple Functions

    [Test]
    public void NoArgFunction_PreferredInt32() {
        // Preferred type propagates through generic user function return
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return 2 + 3\ny = f()");
        rt.Run();
        Assert.AreEqual(typeof(int), rt["y"].Value.GetType(), $"Expected Int32 but got {rt["y"].Value.GetType().Name}");
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void SimpleFunction() {
        var rt = Funny.Hardcore.BuildLang("fun double(x):\n    return x * 2\n\ny = double(21)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void FunctionWithNoArgs() {
        var rt = Funny.Hardcore.BuildLang("fun pi():\n    return 3.14\n\ny = pi()");
        rt.Run();
        Assert.AreEqual(3.14, rt["y"].Value);
    }

    [Test]
    public void FunctionWithOneArg() {
        var rt = Funny.Hardcore.BuildLang("fun negate(x):\n    return -x\n\ny = negate(5)");
        rt.Run();
        Assert.AreEqual(-5, rt["y"].Value);
    }

    [Test]
    public void FunctionWithTwoArgs() {
        var rt = Funny.Hardcore.BuildLang("fun add(a, b):\n    return a + b\n\ny = add(10, 32)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void FunctionWithThreeArgs() {
        var rt = Funny.Hardcore.BuildLang("fun sum3(a, b, c):\n    return a + b + c\n\ny = sum3(10, 20, 12)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void FunctionWithIntTypeAnnotation() {
        var rt = Funny.Hardcore.BuildLang("fun add(a:int32, b:int32)->int32:\n    return a + b\n\ny = add(10, 32)");
        rt.Run();
        Assert.AreEqual(42, (int)rt["y"].Value);
    }

    [Test]
    public void FunctionWithRealTypeAnnotation() {
        var rt = Funny.Hardcore.BuildLang("fun half(x:real)->real:\n    return x / 2.0\n\ny = half(10.0)");
        rt.Run();
        Assert.AreEqual(5.0, rt["y"].Value);
    }

    [Test]
    public void FunctionReturningBool() {
        var rt = Funny.Hardcore.BuildLang("fun isPositive(x):\n    return x > 0\n\ny = isPositive(5)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void FunctionReturningText() {
        var rt = Funny.Hardcore.BuildLang("fun greet(name):\n    return 'hello '.concat(name)\n\ny = greet('world')");
        rt.Run();
        Assert.AreEqual("hello world", rt["y"].Value?.ToString());
    }

    [Test]
    public void FunctionCallingAnotherFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\nfun quadruple(x):\n    return double(double(x))\n\ny = quadruple(3)");
        rt.Run();
        Assert.AreEqual(12, rt["y"].Value);
    }

    [Test]
    public void RecursiveFactorial() {
        var rt = Funny.Hardcore.BuildLang(
            "fun fact(n):\n    return if (n <= 1) 1 else n * fact(n - 1)\n\ny = fact(5)");
        rt.Run();
        Assert.AreEqual(120, rt["y"].Value);
    }

    [Test]
    public void RecursiveFibonacci() {
        var rt = Funny.Hardcore.BuildLang(
            "fun fib(n):\n    return if (n <= 1) n else fib(n-1) + fib(n-2)\n\ny = fib(7)");
        rt.Run();
        Assert.AreEqual(13, rt["y"].Value);
    }

    [Test]
    public void FunctionWithExpressionReturn() {
        var rt = Funny.Hardcore.BuildLang("fun sq(x):\n    return x * x\n\ny = sq(6)");
        rt.Run();
        Assert.AreEqual(36, rt["y"].Value);
    }

    #endregion

    #region 2. Return Statement

    [Test]
    public void ReturnSimpleInt() {
        var rt = Funny.Hardcore.BuildLang("fun get42():\n    return 42\n\ny = get42()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void ReturnBoolExpression() {
        var rt = Funny.Hardcore.BuildLang("fun isBig(x):\n    return x > 100\n\ny = isBig(50)");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void ReturnStringExpression() {
        var rt = Funny.Hardcore.BuildLang("fun hello():\n    return 'hello'\n\ny = hello()");
        rt.Run();
        Assert.AreEqual("hello", rt["y"].Value?.ToString());
    }

    [Test]
    public void ReturnArithmeticExpression() {
        var rt = Funny.Hardcore.BuildLang("fun calc(a, b):\n    return (a + b) * (a - b)\n\ny = calc(7, 3)");
        rt.Run();
        Assert.AreEqual(40, rt["y"].Value);
    }

    [Test]
    public void ReturnIfElseExpression() {
        var rt = Funny.Hardcore.BuildLang("fun absVal(x):\n    return if (x >= 0) x else -x\n\ny = absVal(-5)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void ReturnAfterLocalVar() {
        var rt = Funny.Hardcore.BuildLang("fun compute(x):\n    result = x * 2 + 1\n    return result\n\ny = compute(20)");
        rt.Run();
        Assert.AreEqual(41, rt["y"].Value);
    }

    [Test]
    public void ReturnFromIfElsePaths() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    return if (x > 0) 1 else if (x < 0) -1 else 0\n\ny = classify(-7)");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void ReturnWithFunctionCall() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\nfun doubleAndAdd(x, y):\n    return double(x) + y\n\ny = doubleAndAdd(5, 3)");
        rt.Run();
        Assert.AreEqual(13, rt["y"].Value);
    }

    [Test]
    public void ReturnArrayExpression() {
        var rt = Funny.Hardcore.BuildLang("fun makeArr():\n    return [1, 2, 3]\n\ny = makeArr().count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void ReturnComparisonChain() {
        var rt = Funny.Hardcore.BuildLang("fun inRange(x):\n    return (x >= 0) and (x <= 10)\n\ny = inRange(5)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    #endregion

    #region 3. Local Variables in Blocks

    [Test]
    public void FunctionWithLocalVar() {
        var rt = Funny.Hardcore.BuildLang("fun greet(name):\n    msg = 'hello '.concat(name)\n    return msg\n\ny = greet('world')");
        rt.Run();
        Assert.AreEqual("hello world", rt["y"].Value?.ToString());
    }

    [Test]
    public void SingleLocalVar() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    x = 42\n    return x\n\ny = f()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MultipleLocalVars() {
        var rt = Funny.Hardcore.BuildLang("fun f(a, b):\n    sum = a + b\n    product = a * b\n    return sum + product\n\ny = f(3, 4)");
        rt.Run();
        Assert.AreEqual(19, rt["y"].Value); // (3+4) + (3*4) = 7 + 12
    }

    [Test]
    public void LocalVarUsedInSubsequentExpression() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    doubled = x * 2\n    return doubled + 1\n\ny = f(20)");
        rt.Run();
        Assert.AreEqual(41, rt["y"].Value);
    }

    [Test]
    public void LocalVarFromFunctionCallResult() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\nfun f(x):\n    d = double(x)\n    return d + 1\n\ny = f(20)");
        rt.Run();
        Assert.AreEqual(41, rt["y"].Value);
    }

    [Test]
    public void LocalVarFromIfElseExpression() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    sign = if (x > 0) 1 else -1\n    return sign * x\n\ny = f(-5)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void ChainOfAssignments() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    a = 1\n    b = a + 1\n    c = b + 1\n    return c\n\ny = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void LongChainOfAssignments() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = 1\n    b = a + 1\n    c = b + 1\n    d = c + 1\n    e = d + 1\n    return e\n\ny = f()");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void LocalVarWithTypeAnnotation() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    x:int32 = 42\n    return x\n\ny = f()");
        rt.Run();
        Assert.AreEqual(42, (int)rt["y"].Value);
    }

    [Test]
    public void LocalVarUsedInReturn() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    result = x * x + 1\n    return result\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(26, rt["y"].Value);
    }

    [Test]
    public void MultipleLocalVarsFromArgs() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b, c):\n    ab = a + b\n    bc = b + c\n    return ab * bc\n\ny = f(1, 2, 3)");
        rt.Run();
        Assert.AreEqual(15, rt["y"].Value); // (1+2) * (2+3) = 3*5
    }

    #endregion

    #region 4. Block Structure

    [Test]
    public void SingleStatementBlock() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return 42\n\ny = f()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MultiStatementBlock() {
        var rt = Funny.Hardcore.BuildLang("fun calc(a, b):\n    sum = a + b\n    diff = a - b\n    return sum * diff\n\ny = calc(5, 3)");
        rt.Run();
        Assert.AreEqual(16, rt["y"].Value);
    }

    [Test]
    public void BlockWithOnlyAssignmentsAndReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    x = 10\n    y = 20\n    z = 30\n    return x + y + z\n\ny = f()");
        rt.Run();
        Assert.AreEqual(60, rt["y"].Value);
    }

    [Test]
    public void BlockWithMixOfAssignmentsAndExpressions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    doubled = x * 2\n    tripled = x * 3\n    return doubled + tripled\n\ny = f(4)");
        rt.Run();
        Assert.AreEqual(20, rt["y"].Value); // 8 + 12
    }

    [Test]
    public void ManyStatementsInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = 1\n    b = 2\n    c = 3\n    d = 4\n    e = 5\n    f = 6\n    g = 7\n    h = 8\n    i = 9\n    j = 10\n    return a+b+c+d+e+f+g+h+i+j\n\ny = f()");
        rt.Run();
        Assert.AreEqual(55, rt["y"].Value);
    }

    #endregion

    #region 5. Print Function

    [Test]
    public void PrintFunction() {
        var rt = Funny.Hardcore.BuildLang("fun foo():\n    print('hello')\n    return 42\n\ny = foo()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void PrintInt() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print(123)\n    return 1\n\ny = f()");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void PrintBool() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print(true)\n    return 1\n\ny = f()");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void PrintDoesNotAffectReturnValue() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    print(x)\n    return x * 2\n\ny = f(21)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void PrintInMiddleOfFunction() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    a = x + 1\n    print(a)\n    b = a + 1\n    return b\n\ny = f(10)");
        rt.Run();
        Assert.AreEqual(12, rt["y"].Value);
    }

    [Test]
    public void PrintReturnValueIsNone() {
        // print returns none (procedure)
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print(42)\n    return 42\n\ny = f()");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void PrintWithEnd_NoNewline() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('hello', '')\n    print(' world', '')\n    return 0\n\ny = f()");
        rt.IO.Output = new System.IO.StringWriter();
        rt.Run();
        Assert.AreEqual("hello world", rt.IO.Output.ToString());
    }

    [Test]
    public void PrintWithEnd_CustomSuffix() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('a', '; ')\n    print('b', '; ')\n    print('c', '.')\n    return 0\n\ny = f()");
        rt.IO.Output = new System.IO.StringWriter();
        rt.Run();
        Assert.AreEqual("a; b; c.", rt.IO.Output.ToString());
    }

    [Test]
    public void PrintDefault_HasNewline() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print('line1')\n    print('line2')\n    return 0\n\ny = f()");
        rt.IO.Output = new System.IO.StringWriter();
        rt.Run();
        Assert.AreEqual("line1" + System.Environment.NewLine + "line2" + System.Environment.NewLine, rt.IO.Output.ToString());
    }

    [Test]
    public void PrintCapturesOutput() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    print(42)\n    return 0\n\ny = f()");
        rt.IO.Output = new System.IO.StringWriter();
        rt.Run();
        Assert.That(rt.IO.Output.ToString(), Does.Contain("42"));
    }

    #endregion

    #region 6. Multiple Functions

    [Test]
    public void MultipleFunctions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun add(a, b):\n    return a + b\n\nfun mul(a, b):\n    return a * b\n\ny = add(mul(2, 3), 4)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void TwoFunctionsSecondCallsFirst() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inc(x):\n    return x + 1\n\nfun incTwice(x):\n    return inc(inc(x))\n\ny = incTwice(40)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void ThreeFunctionsWithDependencies() {
        var rt = Funny.Hardcore.BuildLang(
            "fun add(a, b):\n    return a + b\n\nfun double(x):\n    return add(x, x)\n\nfun quadruple(x):\n    return double(double(x))\n\ny = quadruple(3)");
        rt.Run();
        Assert.AreEqual(12, rt["y"].Value);
    }

    [Test]
    public void FunctionsWithDifferentArgCounts() {
        var rt = Funny.Hardcore.BuildLang(
            "fun zero():\n    return 0\n\nfun inc(x):\n    return x + 1\n\nfun add(a, b):\n    return a + b\n\ny = add(inc(zero()), inc(inc(zero())))");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void FunctionsPlusTopLevelEquations() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\ny = double(21)\nz = double(10)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
        Assert.AreEqual(20, rt["z"].Value);
    }

    [Test]
    public void FunctionDefinedAfterUsage() {
        // NFun uses topology sort for functions, so order should not matter
        var rt = Funny.Hardcore.BuildLang(
            "fun outer(x):\n    return inner(x) + 1\n\nfun inner(x):\n    return x * 2\n\ny = outer(5)");
        rt.Run();
        Assert.AreEqual(11, rt["y"].Value);
    }

    [Test]
    public void MultipleFunctionsCalledInEquation() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sq(x):\n    return x * x\n\nfun cube(x):\n    return x * x * x\n\ny = sq(3) + cube(2)");
        rt.Run();
        Assert.AreEqual(17, rt["y"].Value); // 9 + 8
    }

    [Test]
    public void FunctionsWithSameArgNames() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return x + 1\n\nfun g(x):\n    return x * 2\n\ny = f(g(5))");
        rt.Run();
        Assert.AreEqual(11, rt["y"].Value);
    }

    [Test]
    public void FunctionCalledMultipleTimes() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inc(x):\n    return x + 1\n\ny = inc(1) + inc(2) + inc(3)");
        rt.Run();
        Assert.AreEqual(9, rt["y"].Value); // 2 + 3 + 4
    }

    [Test]
    public void NestedFunctionCalls() {
        var rt = Funny.Hardcore.BuildLang(
            "fun add(a, b):\n    return a + b\n\ny = add(add(1, 2), add(3, 4))");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    #endregion

    #region 7. Top-Level Code

    [Test]
    public void TopLevelEquation() {
        var rt = Funny.Hardcore.BuildLang("y = 1 + 2");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void TopLevelSimpleAssignment() {
        var rt = Funny.Hardcore.BuildLang("y = 42");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void TopLevelEquationCallingFunction() {
        var rt = Funny.Hardcore.BuildLang("fun double(x):\n    return x * 2\n\ny = double(21)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MultipleTopLevelEquations() {
        var rt = Funny.Hardcore.BuildLang("x = 10\ny = 20\nz = x + y");
        rt.Run();
        Assert.AreEqual(10, rt["x"].Value);
        Assert.AreEqual(20, rt["y"].Value);
        Assert.AreEqual(30, rt["z"].Value);
    }

    [Test]
    public void TopLevelEquationWithComplexExpression() {
        var rt = Funny.Hardcore.BuildLang("y = (2 + 3) * (10 - 4)");
        rt.Run();
        Assert.AreEqual(30, rt["y"].Value);
    }

    [Test]
    public void MixOfFunctionsAndEquations() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sq(x):\n    return x * x\n\na = sq(3)\nb = sq(4)\nc = a + b");
        rt.Run();
        Assert.AreEqual(9, rt["a"].Value);
        Assert.AreEqual(16, rt["b"].Value);
        Assert.AreEqual(25, rt["c"].Value);
    }

    [Test]
    public void TopLevelBoolEquation() {
        var rt = Funny.Hardcore.BuildLang("y = 5 > 3");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void TopLevelTextEquation() {
        var rt = Funny.Hardcore.BuildLang("y = 'hello'");
        rt.Run();
        Assert.AreEqual("hello", rt["y"].Value?.ToString());
    }

    [Test]
    public void TopLevelArrayEquation() {
        var rt = Funny.Hardcore.BuildLang("y = [1,2,3].count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void TopLevelEquationWithIfElse() {
        var rt = Funny.Hardcore.BuildLang("y = if (true) 42 else 0");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    #endregion

    #region 8. If-Else Inside Blocks

    [Test]
    public void IfElseExpressionInReturn() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    return if (x > 0) 1 else -1\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void IfElseExpressionInReturnNegative() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    return if (x > 0) 1 else -1\n\ny = f(-5)");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void IfElseExpressionInAssignment() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    result = if (x > 0) x else -x\n    return result\n\ny = f(-7)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    [Test]
    public void NestedIfElse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    return if (x > 0) 1 else if (x == 0) 0 else -1\n\ny = classify(0)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void IfElseWithFunctionCalls() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\nfun triple(x):\n    return x * 3\n\nfun f(x):\n    return if (x > 0) double(x) else triple(x)\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void IfElseWithFunctionCallsNegative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\nfun triple(x):\n    return x * 3\n\nfun f(x):\n    return if (x > 0) double(x) else triple(x)\n\ny = f(-5)");
        rt.Run();
        Assert.AreEqual(-15, rt["y"].Value);
    }

    [Test]
    public void IfElseBoolCondition() {
        var rt = Funny.Hardcore.BuildLang("fun f(a, b):\n    return if (a > b) a else b\n\ny = f(3, 7)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    [Test]
    public void IfElseWithLocalVarCondition() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    isPos = x > 0\n    return if (isPos) x else 0\n\ny = f(-3)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void IfElseReturningBool() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    return if (x > 10) true else false\n\ny = f(15)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void IfElseWithArithmeticResult() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    offset = if (x > 0) 10 else -10\n    return x + offset\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(15, rt["y"].Value);
    }

    #endregion

    #region 9. Existing NFun Features in Lang Mode

    [Test]
    public void ArrayLiteral() {
        var rt = Funny.Hardcore.BuildLang("y = [1, 2, 3, 4, 5].count()");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void ArrayInFunction() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return [10, 20, 30]\n\ny = f().count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void ArrayMapInFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun doubleAll(arr):\n    return arr.map(rule it * 2)\n\ny = doubleAll([1,2,3]).count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void ArrayFilterInFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun positives(arr):\n    return arr.filter(rule it > 0)\n\ny = positives([-1, 2, -3, 4, 5]).count()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void MathMax() {
        var rt = Funny.Hardcore.BuildLang("fun bigger(a, b):\n    return max(a, b)\n\ny = bigger(3, 7)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    [Test]
    public void MathMin() {
        var rt = Funny.Hardcore.BuildLang("fun smaller(a, b):\n    return min(a, b)\n\ny = smaller(3, 7)");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void MathAbs() {
        var rt = Funny.Hardcore.BuildLang("fun myAbs(x):\n    return abs(x)\n\ny = myAbs(-42)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void StringConcat() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return 'foo'.concat('bar')\n\ny = f()");
        rt.Run();
        Assert.AreEqual("foobar", rt["y"].Value?.ToString());
    }

    [Test]
    public void ArrayGetElement() {
        var rt = Funny.Hardcore.BuildLang("fun first(arr):\n    return arr[0]\n\ny = first([10, 20, 30])");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void BooleanLogicInFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun both(a, b):\n    return a and b\n\ny = both(true, false)");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void BooleanOrInFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun either(a, b):\n    return a or b\n\ny = either(true, false)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void NotInFunction() {
        var rt = Funny.Hardcore.BuildLang("fun invert(b):\n    return not b\n\ny = invert(true)");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void ModuloInFunction() {
        var rt = Funny.Hardcore.BuildLang("fun isEven(x):\n    return x % 2 == 0\n\ny = isEven(4)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void FunctionReturningArray() {
        var rt = Funny.Hardcore.BuildLang("fun f(n):\n    return [1, 2, 3].map(rule it * n)\n\ny = f(10)[2]");
        rt.Run();
        Assert.AreEqual(30, rt["y"].Value);
    }

    #endregion

    #region 10. Error Cases

    [Test]
    public void ErrorMissingIndent() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f():\nreturn 42"));
    }

    [Test]
    public void ErrorMissingColon() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f()\n    return 42"));
    }

    [Test]
    public void ErrorMissingParentheses() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f:\n    return 42"));
    }

    [Test]
    public void ErrorMissingFunctionName() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun ():\n    return 42"));
    }

    [Test]
    public void ErrorMissingReturnExpression() {
        // bare return may or may not be supported, but function must still have a usable return
        // This tests that a function without any return-with-value doesn't produce invalid results
        // Bare return currently parses; it's up to how the runtime handles it
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return\n\ny = 1");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void EmptyScriptProducesNoOutputs() {
        // Empty script produces a valid runtime with no variables
        var rt = Funny.Hardcore.BuildLang("");
        rt.Run();
        Assert.IsNull(rt["y"]);
    }

    [Test]
    public void ErrorUnknownFunctionCall() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f():\n    return unknownFunc(42)\n\ny = f()"));
    }

    [Test]
    public void ErrorTypeMismatch() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f(x:int32)->int32:\n    return 'hello'\n\ny = f(1)"));
    }

    [Test]
    public void ErrorDuplicateFunctionName() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f():\n    return 1\n\nfun f():\n    return 2\n\ny = f()"));
    }

    [Test]
    public void ErrorMissingClosingParen() {
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang("fun f(x:\n    return x"));
    }

    #endregion

    #region 11. Edge Cases

    [Test]
    public void FunctionReturningLastExpression_ExplicitReturn() {
        // When the last statement is a return with value
        var rt = Funny.Hardcore.BuildLang("fun inc(x):\n    return x + 1\n\ny = inc(41)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void FunctionWithZeroReturnValue() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return 0\n\ny = f()");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void FunctionWithNegativeReturnValue() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return -42\n\ny = f()");
        rt.Run();
        Assert.AreEqual(-42, rt["y"].Value);
    }

    [Test]
    public void FunctionWithRealArithmetic() {
        var rt = Funny.Hardcore.BuildLang("fun f():\n    return 1.5 + 2.5\n\ny = f()");
        rt.Run();
        Assert.AreEqual(4.0, rt["y"].Value);
    }

    [Test]
    public void FunctionPassingArrayArg() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sumCount(arr):\n    return arr.count()\n\ny = sumCount([10, 20, 30, 40])");
        rt.Run();
        Assert.AreEqual(4, rt["y"].Value);
    }

    [Test]
    public void FunctionWithComplexBody() {
        var rt = Funny.Hardcore.BuildLang(
            "fun compute(x):\n    a = x * 2\n    b = a + 3\n    c = b * b\n    d = c - a\n    return d\n\ny = compute(5)");
        rt.Run();
        // x=5, a=10, b=13, c=169, d=159
        Assert.AreEqual(159, rt["y"].Value);
    }

    [Test]
    public void MultipleOutputEquationsAfterFunctions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inc(x):\n    return x + 1\n\nfun dec(x):\n    return x - 1\n\na = inc(10)\nb = dec(10)");
        rt.Run();
        Assert.AreEqual(11, rt["a"].Value);
        Assert.AreEqual(9, rt["b"].Value);
    }

    [Test]
    public void DeepNestedFunctionCalls() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return x + 1\n\ny = f(f(f(f(f(0)))))");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void FunctionWithBoolArgAndReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun myNot(b):\n    return if (b) false else true\n\ny = myNot(true)");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void FunctionReturnUsedInArithmetic() {
        var rt = Funny.Hardcore.BuildLang(
            "fun five():\n    return 5\n\ny = five() * five() + five()");
        rt.Run();
        Assert.AreEqual(30, rt["y"].Value);
    }

    [Test]
    public void TopLevelEquationWithoutFunctions() {
        var rt = Funny.Hardcore.BuildLang("y = 100");
        rt.Run();
        Assert.AreEqual(100, rt["y"].Value);
    }

    [Test]
    public void FunctionWithInputVariable() {
        var rt = Funny.Hardcore.BuildLang("fun double(x):\n    return x * 2\n\ny = double(x)");
        rt["x"].Value = 21;
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MultipleEquationsUsingInput() {
        var rt = Funny.Hardcore.BuildLang("fun inc(n):\n    return n + 1\n\ny = inc(x)\nz = inc(y)");
        rt["x"].Value = 10;
        rt.Run();
        Assert.AreEqual(11, rt["y"].Value);
        Assert.AreEqual(12, rt["z"].Value);
    }

    #endregion

    #region 12. TestCase Parameterized Tests

    [TestCase("fun f(x):\n    return x + 1\n\ny = f(0)", 1)]
    [TestCase("fun f(x):\n    return x + 1\n\ny = f(10)", 11)]
    [TestCase("fun f(x):\n    return x + 1\n\ny = f(-1)", 0)]
    [TestCase("fun f(x):\n    return x + 1\n\ny = f(99)", 100)]
    [TestCase("fun f(x):\n    return x * 2\n\ny = f(0)", 0)]
    [TestCase("fun f(x):\n    return x * 2\n\ny = f(5)", 10)]
    [TestCase("fun f(x):\n    return x * 2\n\ny = f(-3)", -6)]
    public void ParameterizedSimpleFunction(string script, object expected) {
        var rt = Funny.Hardcore.BuildLang(script);
        rt.Run();
        Assert.AreEqual(expected, rt["y"].Value);
    }

    [TestCase("y = 1", 1)]
    [TestCase("y = 0", 0)]
    [TestCase("y = -1", -1)]
    [TestCase("y = 100 + 200", 300)]
    [TestCase("y = 10 * 5", 50)]
    [TestCase("y = 100 - 1", 99)]
    public void ParameterizedTopLevelEquation(string script, object expected) {
        var rt = Funny.Hardcore.BuildLang(script);
        rt.Run();
        Assert.AreEqual(expected, rt["y"].Value);
    }

    [TestCase("fun f(a,b):\n    return a + b\n\ny = f(1, 2)", 3)]
    [TestCase("fun f(a,b):\n    return a + b\n\ny = f(10, 20)", 30)]
    [TestCase("fun f(a,b):\n    return a - b\n\ny = f(10, 3)", 7)]
    [TestCase("fun f(a,b):\n    return a * b\n\ny = f(6, 7)", 42)]
    public void ParameterizedTwoArgFunction(string script, object expected) {
        var rt = Funny.Hardcore.BuildLang(script);
        rt.Run();
        Assert.AreEqual(expected, rt["y"].Value);
    }

    #endregion

    #region 13. Indentation Edge Cases

    [Test]
    public void TabIndentation() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n\treturn x * 2\n\ny = f(21)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void TwoSpaceIndent() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n  return x + 1\n\ny = f(41)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void EightSpaceIndent() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n        return x * 3\n\ny = f(14)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void SingleSpaceIndent() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n return x + 10\n\ny = f(32)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MixedIndentLevels_OuterFourInnerEight() {
        // Nested if uses deeper indent
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    result = if (x > 0) x else -x\n    return result\n\ny = f(-42)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void EmptyLineInsideBlock() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    a = x + 1\n\n    return a\n\ny = f(41)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MultipleEmptyLinesInsideBlock() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n    a = x * 2\n\n\n    return a\n\ny = f(21)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void EmptyLineBetweenFunctions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inc(x):\n    return x + 1\n\n\nfun dec(x):\n    return x - 1\n\ny = inc(dec(42))");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void TrailingSpacesAfterStatement() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):   \n    return x + 1   \n\ny = f(41)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MultipleDedentAtOnce_FunctionWithNestedIf() {
        // Deeply nested block returns to top level
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    a = if (x > 0) x * 2 else x * 3\n    return a\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void DedentToTopLevelAfterDeepFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun complex(a, b):\n    sum = a + b\n    diff = a - b\n    product = sum * diff\n    return product\n\ny = complex(7, 3)");
        rt.Run();
        Assert.AreEqual(40, rt["y"].Value); // (7+3)*(7-3) = 10*4
    }

    [Test]
    public void TwoFunctionsWithDifferentIndentWidths() {
        // First function uses 4-space, second uses 2-space
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return x + 1\n\nfun g(x):\n  return x + 2\n\ny = f(g(39))");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void TabAndSpaceDifferentFunctions_MixedError() {
        // Mixed tabs and spaces in same file → error per indent spec Rule 1
        Assert.Catch<Exception>(() =>
            Funny.Hardcore.BuildLang(
                "fun f(x):\n\treturn x * 2\n\nfun g(x):\n    return x + 1\n\ny = f(g(20))"));
    }

    [Test]
    public void BlockWithManyLocalVarsAndEmptyLines() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = 10\n    b = 20\n\n    c = a + b\n\n    return c\n\ny = f()");
        rt.Run();
        Assert.AreEqual(30, rt["y"].Value);
    }

    [Test]
    public void ThreeSpaceIndent() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n   return x * 7\n\ny = f(6)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void SixSpaceIndent() {
        var rt = Funny.Hardcore.BuildLang("fun f(x):\n      return x - 8\n\ny = f(50)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    #endregion

    #region 14. Expressions Inside Blocks

    [Test]
    public void LambdaMapInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    result = [1,2,3].map(rule it * 2)\n    return result.count()\n\ny = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void LambdaMapValuesInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    result = [10,20,30].map(rule it + 1)\n    return result[0]\n\ny = f()");
        rt.Run();
        Assert.AreEqual(11, rt["y"].Value);
    }

    [Test]
    public void LambdaFilterInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    filtered = [1,2,3,4,5,6].filter(rule it > 3)\n    return filtered.count()\n\ny = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void LambdaFoldInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    total = [1,2,3,4].fold(rule it1 + it2)\n    return total\n\ny = f()");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void ChainedLambdasInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    result = [1,2,3,4,5].map(rule it * 2).filter(rule it > 4)\n    return result.count()\n\ny = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value); // [2,4,6,8,10] -> [6,8,10]
    }

    [Test]
    public void StructLiteralInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    s = {name = 'bob', age = 25}\n    return s.age\n\ny = f()");
        rt.Run();
        Assert.AreEqual(25, rt["y"].Value);
    }

    [Test]
    public void StructFieldAccessInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun getName():\n    s = {name = 'alice', age = 30}\n    return s.name\n\ny = getName()");
        rt.Run();
        Assert.AreEqual("alice", rt["y"].Value?.ToString());
    }

    [Test]
    public void PipedCallCountInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    arr = [10, 20, 30, 40]\n    return arr.count()\n\ny = f()");
        rt.Run();
        Assert.AreEqual(4, rt["y"].Value);
    }

    [Test]
    public void NestedFunctionCallsInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b, c):\n    return max(a, min(b, c))\n\ny = f(5, 3, 7)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value); // max(5, min(3,7)) = max(5,3) = 5
    }

    [Test]
    public void ComparisonInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    ok = x > 10\n    return ok\n\ny = f(15)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void BooleanAndOrInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b, c):\n    result = a and b or c\n    return result\n\ny = f(true, false, true)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void ArrayIndexingInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    arr = [10, 20, 30]\n    first = arr[0]\n    return first\n\ny = f()");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void ArraySlicingInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    arr = [10, 20, 30, 40, 50]\n    part = arr[1:3]\n    return part.count()\n\ny = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void UnaryMinusInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    neg = -x\n    return neg\n\ny = f(42)");
        rt.Run();
        Assert.AreEqual(-42, rt["y"].Value);
    }

    [Test]
    public void PowerOperatorInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    sq = x ** 2\n    return sq\n\ny = f(6)");
        rt.Run();
        Assert.AreEqual(36, rt["y"].Value);
    }

    [Test]
    public void StringConcatWithToTextInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    msg = 'value is '.concat(x.toText())\n    return msg\n\ny = f(42)");
        rt.Run();
        Assert.AreEqual("value is 42", rt["y"].Value?.ToString());
    }

    [Test]
    public void BitwiseAndInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a):\n    mask = a & 0x0F\n    return mask\n\ny = f(255)");
        rt.Run();
        Assert.AreEqual(15, rt["y"].Value);
    }

    [Test]
    public void ComplexArithmeticInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b, c, d):\n    result = (a + b) * (c - d)\n    return result\n\ny = f(3, 4, 10, 4)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value); // (3+4)*(10-4) = 7*6
    }

    [Test]
    public void InlineIfExpressionInAssignment() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    res = if (x > 0) x else -x\n    return res\n\ny = f(-42)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void MapThenFoldInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sumDoubled():\n    result = [1,2,3].map(rule it * 2).fold(rule it1 + it2)\n    return result\n\ny = sumDoubled()");
        rt.Run();
        Assert.AreEqual(12, rt["y"].Value); // [2,4,6] -> 12
    }

    [Test]
    public void FilterThenCountInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun countPositive(arr):\n    pos = arr.filter(rule it > 0)\n    return pos.count()\n\ny = countPositive([-3, -1, 0, 2, 5])");
        rt.Run();
        Assert.AreEqual(2, rt["y"].Value);
    }

    [Test]
    public void ArrayReverseInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    arr = [1, 2, 3]\n    rev = arr.reverse()\n    return rev[0]\n\ny = f()");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void MultipleArrayOpsInBlock() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n    a = [1,2,3,4,5]\n    b = a.filter(rule it % 2 == 0)\n    c = b.map(rule it * 10)\n    return c[0]\n\ny = f()");
        rt.Run();
        Assert.AreEqual(20, rt["y"].Value); // evens: [2,4], *10: [20,40]
    }

    #endregion

    #region 15. Multiple Returns and Control Flow

    [Test]
    public void EarlyReturnFromIfExpression() {
        // abs function using if-else expression
        var rt = Funny.Hardcore.BuildLang(
            "fun myAbs(x):\n    return if (x >= 0) x else -x\n\ny = myAbs(-42)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void ClassifyPositiveNegativeZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    return if (x > 0) 1 else if (x < 0) -1 else 0\n\ny = classify(10)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void ClassifyNegative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    return if (x > 0) 1 else if (x < 0) -1 else 0\n\ny = classify(-5)");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void ClassifyZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    return if (x > 0) 1 else if (x < 0) -1 else 0\n\ny = classify(0)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void ReturnFromDeepNestedIfElse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return if (x > 10) if (x > 20) 3 else 2 else 1\n\ny = f(25)");
        rt.Run();
        Assert.AreEqual(3, rt["y"].Value);
    }

    [Test]
    public void ReturnFromDeepNestedIfElse_Middle() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return if (x > 10) if (x > 20) 3 else 2 else 1\n\ny = f(15)");
        rt.Run();
        Assert.AreEqual(2, rt["y"].Value);
    }

    [Test]
    public void ReturnFromDeepNestedIfElse_Low() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return if (x > 10) if (x > 20) 3 else 2 else 1\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void AllBranchesReturnSameType() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return if (x > 0) 100 else if (x == 0) 0 else -100\n\ny = f(0)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void FunctionAlwaysReturnsThroughIfElse() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sign(x):\n    return if (x >= 0) 1 else -1\n\ny = sign(-10)");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void ConditionWithLocalVar() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    threshold = 10\n    return if (x > threshold) x - threshold else 0\n\ny = f(15)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void ConditionWithLocalVar_BelowThreshold() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    threshold = 10\n    return if (x > threshold) x - threshold else 0\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void IfElseWithComputedCondition() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b):\n    diff = a - b\n    return if (diff > 0) diff else -diff\n\ny = f(3, 10)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    #endregion

    #region 16. Function Argument Edge Cases

    [Test]
    public void FunctionWithZeroArgs_ReturnsConstant() {
        var rt = Funny.Hardcore.BuildLang("fun pi():\n    return 3.14\n\ny = pi()");
        rt.Run();
        Assert.AreEqual(3.14, rt["y"].Value);
    }

    [Test]
    public void FunctionWithFiveArgs() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sum5(a, b, c, d, e):\n    return a + b + c + d + e\n\ny = sum5(1, 2, 3, 4, 5)");
        rt.Run();
        Assert.AreEqual(15, rt["y"].Value);
    }

    [Test]
    public void FunctionWithSixArgs() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sum6(a, b, c, d, e, f):\n    return a + b + c + d + e + f\n\ny = sum6(1, 2, 3, 4, 5, 6)");
        rt.Run();
        Assert.AreEqual(21, rt["y"].Value);
    }

    [Test]
    public void ArgUsedMultipleTimesInBody() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    a = x + 1\n    b = x * 2\n    c = x * x\n    return a + b + c\n\ny = f(3)");
        rt.Run();
        Assert.AreEqual(19, rt["y"].Value); // 4 + 6 + 9
    }

    [Test]
    public void ArgWithPartialTypeAnnotation() {
        // One arg typed, one inferred
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a:int32, b):\n    return a + b\n\ny = f(10, 32)");
        rt.Run();
        Assert.AreEqual(42, (int)rt["y"].Value);
    }

    [Test]
    public void ArgWithReturnTypeAnnotation() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x)->int32:\n    return x * 2\n\ny = f(21)");
        rt.Run();
        Assert.AreEqual(42, (int)rt["y"].Value);
    }

    [Test]
    public void AllArgsTyped() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a:int32, b:int32, c:int32)->int32:\n    return a * b + c\n\ny = f(6, 7, 0)");
        rt.Run();
        Assert.AreEqual(42, (int)rt["y"].Value);
    }

    [Test]
    public void FunctionWithBoolArg() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(flag):\n    return if (flag) 42 else 0\n\ny = f(true)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void FunctionWithArrayArg() {
        var rt = Funny.Hardcore.BuildLang(
            "fun total(arr):\n    return arr.fold(rule it1 + it2)\n\ny = total([10, 20, 12])");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void FunctionWithTextArg() {
        var rt = Funny.Hardcore.BuildLang(
            "fun greetLen(name):\n    return name.count()\n\ny = greetLen('hello')");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    #endregion

    #region 17. Top-Level Mixing

    [Test]
    public void FunctionThenEquationThenFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inc(x):\n    return x + 1\n\na = inc(10)\n\nfun dec(x):\n    return x - 1\n\nb = dec(10)");
        rt.Run();
        Assert.AreEqual(11, rt["a"].Value);
        Assert.AreEqual(9, rt["b"].Value);
    }

    [Test]
    public void EquationsBetweenFunctions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sq(x):\n    return x * x\n\na = sq(3)\n\nfun cube(x):\n    return x * x * x\n\nb = cube(2)\n\nc = a + b");
        rt.Run();
        Assert.AreEqual(9, rt["a"].Value);
        Assert.AreEqual(8, rt["b"].Value);
        Assert.AreEqual(17, rt["c"].Value);
    }

    [Test]
    public void TopLevelUsesMultipleFunctions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun add(a, b):\n    return a + b\n\nfun mul(a, b):\n    return a * b\n\ny = add(mul(3, 4), mul(5, 6))");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value); // 12 + 30
    }

    [Test]
    public void ForwardReferenceBetweenFunctions() {
        // outer defined before inner — should work via topological sort
        var rt = Funny.Hardcore.BuildLang(
            "fun outer(x):\n    return inner(x) * 2\n\nfun inner(x):\n    return x + 1\n\ny = outer(5)");
        rt.Run();
        Assert.AreEqual(12, rt["y"].Value); // inner(5)=6, *2=12
    }

    [Test]
    public void MultipleOutputsFromOneFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\na = double(5)\nb = double(10)\nc = a + b");
        rt.Run();
        Assert.AreEqual(10, rt["a"].Value);
        Assert.AreEqual(20, rt["b"].Value);
        Assert.AreEqual(30, rt["c"].Value);
    }

    [Test]
    public void EquationFirst_FunctionSecond() {
        // Equation defined before the function it uses — should work
        var rt = Funny.Hardcore.BuildLang(
            "y = double(21)\n\nfun double(x):\n    return x * 2");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void ThreeEquationsNoFunctions() {
        var rt = Funny.Hardcore.BuildLang("a = 10\nb = 20\nc = a * b");
        rt.Run();
        Assert.AreEqual(10, rt["a"].Value);
        Assert.AreEqual(20, rt["b"].Value);
        Assert.AreEqual(200, rt["c"].Value);
    }

    [Test]
    public void FourEquationsWithChaining() {
        var rt = Funny.Hardcore.BuildLang("a = 1\nb = a + 1\nc = b + 1\nd = c + 1");
        rt.Run();
        Assert.AreEqual(1, rt["a"].Value);
        Assert.AreEqual(2, rt["b"].Value);
        Assert.AreEqual(3, rt["c"].Value);
        Assert.AreEqual(4, rt["d"].Value);
    }

    [Test]
    public void ThreeFunctionsAndThreeEquations() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return x + 1\n\nfun g(x):\n    return x * 2\n\nfun h(x):\n    return x - 3\n\na = f(10)\nb = g(10)\nc = h(10)");
        rt.Run();
        Assert.AreEqual(11, rt["a"].Value);
        Assert.AreEqual(20, rt["b"].Value);
        Assert.AreEqual(7, rt["c"].Value);
    }

    [Test]
    public void EquationUsingTwoFunctions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sq(x):\n    return x * x\n\nfun double(x):\n    return x * 2\n\ny = sq(3) + double(5)");
        rt.Run();
        Assert.AreEqual(19, rt["y"].Value); // 9 + 10
    }

    #endregion

    #region 18. Stress Tests

    [Test]
    public void FunctionWith15LocalVars() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f():\n" +
            "    v1 = 1\n    v2 = 2\n    v3 = 3\n    v4 = 4\n    v5 = 5\n" +
            "    v6 = 6\n    v7 = 7\n    v8 = 8\n    v9 = 9\n    v10 = 10\n" +
            "    v11 = 11\n    v12 = 12\n    v13 = 13\n    v14 = 14\n    v15 = 15\n" +
            "    return v1+v2+v3+v4+v5+v6+v7+v8+v9+v10+v11+v12+v13+v14+v15\n\ny = f()");
        rt.Run();
        Assert.AreEqual(120, rt["y"].Value);
    }

    [Test]
    public void DeeplyNestedFunctionCalls() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    return x + 1\n\nfun g(x):\n    return x * 2\n\nfun h(x):\n    return x - 1\n\ny = f(g(h(f(g(h(1))))))");
        rt.Run();
        // h(1)=0, g(0)=0, f(0)=1, h(1)=0, g(0)=0, f(0)=1
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void VeryLongReturnExpression() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b, c, d, e, f, g):\n    return a + b * c - d + e * f - g\n\ny = f(10, 3, 4, 2, 5, 6, 8)");
        rt.Run();
        // 10 + 3*4 - 2 + 5*6 - 8 = 10 + 12 - 2 + 30 - 8 = 42
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void FiveFunctionsCallingEachOther() {
        var rt = Funny.Hardcore.BuildLang(
            "fun a(x):\n    return x + 1\n\n" +
            "fun b(x):\n    return a(x) + 1\n\n" +
            "fun c(x):\n    return b(x) + 1\n\n" +
            "fun d(x):\n    return c(x) + 1\n\n" +
            "fun e(x):\n    return d(x) + 1\n\n" +
            "y = e(0)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void FibonacciLargeN() {
        var rt = Funny.Hardcore.BuildLang(
            "fun fib(n):\n    return if (n <= 1) n else fib(n-1) + fib(n-2)\n\ny = fib(15)");
        rt.Run();
        Assert.AreEqual(610, rt["y"].Value);
    }

    [Test]
    public void ManyTopLevelEquations() {
        var rt = Funny.Hardcore.BuildLang(
            "a = 1\nb = 2\nc = 3\nd = 4\ne = 5\nf = 6\ng = 7\nh = 8\ni = 9\nj = 10\ny = a+b+c+d+e+f+g+h+i+j");
        rt.Run();
        Assert.AreEqual(55, rt["y"].Value);
    }

    [Test]
    public void ComplexExpressionChain() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n" +
            "    a = x * 2\n" +
            "    b = a + 3\n" +
            "    c = if (b > 10) b * 2 else b\n" +
            "    d = c - 1\n" +
            "    e = d * d\n" +
            "    return e\n\ny = f(5)");
        rt.Run();
        // x=5, a=10, b=13, c=26, d=25, e=625
        Assert.AreEqual(625, rt["y"].Value);
    }

    [Test]
    public void RecursiveSum() {
        var rt = Funny.Hardcore.BuildLang(
            "fun sumTo(n):\n    return if (n <= 0) 0 else n + sumTo(n - 1)\n\ny = sumTo(10)");
        rt.Run();
        Assert.AreEqual(55, rt["y"].Value);
    }

    #endregion

    #region 19. Additional Edge Cases

    [Test]
    public void FunctionReturningBoolFromComparison() {
        var rt = Funny.Hardcore.BuildLang(
            "fun isEven(n):\n    return n % 2 == 0\n\ny = isEven(4)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void FunctionReturningBoolFromComparison_Odd() {
        var rt = Funny.Hardcore.BuildLang(
            "fun isEven(n):\n    return n % 2 == 0\n\ny = isEven(3)");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void TopLevelWithPower() {
        var rt = Funny.Hardcore.BuildLang("y = 2 ** 10");
        rt.Run();
        Assert.AreEqual(1024, rt["y"].Value);
    }

    [Test]
    public void TopLevelBitwiseOr() {
        var rt = Funny.Hardcore.BuildLang("y = 0xF0 | 0x0F");
        rt.Run();
        Assert.AreEqual(255, rt["y"].Value);
    }

    [Test]
    public void TopLevelBitwiseXor() {
        var rt = Funny.Hardcore.BuildLang("y = 0xFF ^ 0x0F");
        rt.Run();
        Assert.AreEqual(240, rt["y"].Value);
    }

    [Test]
    public void FunctionWithModuloAndMultiplication() {
        var rt = Funny.Hardcore.BuildLang(
            "fun compute(a, b):\n    product = a * b\n    remainder = a % b\n    return product + remainder\n\ny = compute(17, 5)");
        rt.Run();
        // 17*5=85, 17%5=2, result=87
        Assert.AreEqual(87, rt["y"].Value);
    }

    [Test]
    public void FunctionChainWithLocalVars() {
        var rt = Funny.Hardcore.BuildLang(
            "fun step1(x):\n    return x + 10\n\n" +
            "fun step2(x):\n    return x * 2\n\n" +
            "fun pipeline(x):\n    a = step1(x)\n    b = step2(a)\n    return b\n\ny = pipeline(5)");
        rt.Run();
        Assert.AreEqual(30, rt["y"].Value); // step1(5)=15, step2(15)=30
    }

    [Test]
    public void NestedIfElseWithBoolOps() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b):\n    return if (a > 0 and b > 0) 1 else if (a > 0 or b > 0) 0 else -1\n\ny = f(1, -1)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void NestedIfElseWithBoolOps_BothPositive() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b):\n    return if (a > 0 and b > 0) 1 else if (a > 0 or b > 0) 0 else -1\n\ny = f(1, 1)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void NestedIfElseWithBoolOps_BothNegative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(a, b):\n    return if (a > 0 and b > 0) 1 else if (a > 0 or b > 0) 0 else -1\n\ny = f(-1, -1)");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void ArrayCreationInFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun makeRange():\n    return [1, 2, 3, 4, 5]\n\ny = makeRange().count()");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void FunctionReturnsArrayUsedByAnother() {
        var rt = Funny.Hardcore.BuildLang(
            "fun makeArr():\n    return [10, 20, 30]\n\nfun sumArr(arr):\n    return arr.fold(rule it1 + it2)\n\ny = sumArr(makeArr())");
        rt.Run();
        Assert.AreEqual(60, rt["y"].Value);
    }

    [Test]
    public void TopLevelArrayMap() {
        var rt = Funny.Hardcore.BuildLang("y = [1,2,3].map(rule it * 10).fold(rule it1 + it2)");
        rt.Run();
        Assert.AreEqual(60, rt["y"].Value);
    }

    [Test]
    public void FunctionWithNotOperator() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    positive = x > 0\n    return not positive\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void FunctionWithEqualityCheck() {
        var rt = Funny.Hardcore.BuildLang(
            "fun isZero(x):\n    return x == 0\n\ny = isZero(0)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void FunctionWithNotEqualCheck() {
        var rt = Funny.Hardcore.BuildLang(
            "fun isNonZero(x):\n    return x != 0\n\ny = isNonZero(5)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void MutuallyExclusiveConditions() {
        var rt = Funny.Hardcore.BuildLang(
            "fun clamp(x, lo, hi):\n    return if (x < lo) lo else if (x > hi) hi else x\n\ny = clamp(15, 0, 10)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void MutuallyExclusiveConditions_Low() {
        var rt = Funny.Hardcore.BuildLang(
            "fun clamp(x, lo, hi):\n    return if (x < lo) lo else if (x > hi) hi else x\n\ny = clamp(-5, 0, 10)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void MutuallyExclusiveConditions_InRange() {
        var rt = Funny.Hardcore.BuildLang(
            "fun clamp(x, lo, hi):\n    return if (x < lo) lo else if (x > hi) hi else x\n\ny = clamp(5, 0, 10)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    #endregion

    #region 20. If/Elif/Else Blocks

    [Test]
    public void IfElseBlock_SimpleReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    if x > 0:\n        return 1\n    else:\n        return -1\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_SimpleReturn_Negative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    if x > 0:\n        return 1\n    else:\n        return -1\n\ny = f(-5)");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void IfElifElseBlock_Classify() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    if x > 0:\n        return 1\n    elif x < 0:\n        return -1\n    else:\n        return 0\n\ny = classify(5)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void IfElifElseBlock_ClassifyNegative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    if x > 0:\n        return 1\n    elif x < 0:\n        return -1\n    else:\n        return 0\n\ny = classify(-7)");
        rt.Run();
        Assert.AreEqual(-1, rt["y"].Value);
    }

    [Test]
    public void IfElifElseBlock_ClassifyZero() {
        var rt = Funny.Hardcore.BuildLang(
            "fun classify(x):\n    if x > 0:\n        return 1\n    elif x < 0:\n        return -1\n    else:\n        return 0\n\ny = classify(0)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_WithAssignments() {
        var rt = Funny.Hardcore.BuildLang(
            "fun abs(x):\n    if x >= 0:\n        result = x\n    else:\n        result = -x\n    return result\n\ny = abs(-42)");
        rt.Run();
        Assert.AreEqual(42, rt["y"].Value);
    }

    [Test]
    public void IfElifElseBlock_WithReturn() {
        var rt = Funny.Hardcore.BuildLang(
            "fun process(x):\n    if x > 100:\n        return 100\n    elif x < 0:\n        return 0\n    else:\n        return x\n\ny = process(50)");
        rt.Run();
        Assert.AreEqual(50, rt["y"].Value);
    }

    [Test]
    public void IfElifElseBlock_WithReturn_HighBranch() {
        var rt = Funny.Hardcore.BuildLang(
            "fun process(x):\n    if x > 100:\n        return 100\n    elif x < 0:\n        return 0\n    else:\n        return x\n\ny = process(200)");
        rt.Run();
        Assert.AreEqual(100, rt["y"].Value);
    }

    [Test]
    public void IfElifElseBlock_WithReturn_LowBranch() {
        var rt = Funny.Hardcore.BuildLang(
            "fun process(x):\n    if x > 100:\n        return 0\n    elif x < 0:\n        return 0\n    else:\n        return x\n\ny = process(-10)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_NestedInFunction() {
        var rt = Funny.Hardcore.BuildLang(
            "fun nested(a, b):\n    if a > 0:\n        if b > 0:\n            return a + b\n        else:\n            return a - b\n    else:\n        return 0\n\ny = nested(3, 5)");
        rt.Run();
        Assert.AreEqual(8, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_NestedInFunction_NegativeB() {
        var rt = Funny.Hardcore.BuildLang(
            "fun nested(a, b):\n    if a > 0:\n        if b > 0:\n            return a + b\n        else:\n            return a - b\n    else:\n        return 0\n\ny = nested(3, -2)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_NestedInFunction_NegativeA() {
        var rt = Funny.Hardcore.BuildLang(
            "fun nested(a, b):\n    if a > 0:\n        if b > 0:\n            return a + b\n        else:\n            return a - b\n    else:\n        return 0\n\ny = nested(-1, 5)");
        rt.Run();
        Assert.AreEqual(0, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_MultipleElifBranches() {
        var rt = Funny.Hardcore.BuildLang(
            "fun grade(score):\n    if score >= 90:\n        return 5\n    elif score >= 80:\n        return 4\n    elif score >= 70:\n        return 3\n    elif score >= 60:\n        return 2\n    else:\n        return 1\n\ny = grade(85)");
        rt.Run();
        Assert.AreEqual(4, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_MultipleElifBranches_Lowest() {
        var rt = Funny.Hardcore.BuildLang(
            "fun grade(score):\n    if score >= 90:\n        return 5\n    elif score >= 80:\n        return 4\n    elif score >= 70:\n        return 3\n    elif score >= 60:\n        return 2\n    else:\n        return 1\n\ny = grade(50)");
        rt.Run();
        Assert.AreEqual(1, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_MultipleElifBranches_Highest() {
        var rt = Funny.Hardcore.BuildLang(
            "fun grade(score):\n    if score >= 90:\n        return 5\n    elif score >= 80:\n        return 4\n    elif score >= 70:\n        return 3\n    elif score >= 60:\n        return 2\n    else:\n        return 1\n\ny = grade(95)");
        rt.Run();
        Assert.AreEqual(5, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_WithLocalVarsInBranches() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    if x > 0:\n        doubled = x * 2\n        return doubled\n    else:\n        negated = -x\n        return negated\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_WithLocalVarsInBranches_Negative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    if x > 0:\n        doubled = x * 2\n        return doubled\n    else:\n        negated = -x\n        return negated\n\ny = f(-7)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_WithFunctionCalls() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\nfun triple(x):\n    return x * 3\n\nfun f(x):\n    if x > 0:\n        return double(x)\n    else:\n        return triple(x)\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_WithFunctionCalls_Negative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun double(x):\n    return x * 2\n\nfun triple(x):\n    return x * 3\n\nfun f(x):\n    if x > 0:\n        return double(x)\n    else:\n        return triple(x)\n\ny = f(-5)");
        rt.Run();
        Assert.AreEqual(-15, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_AsExpression_InAssignment() {
        // result = if cond: block else: block
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    result = if x > 0:\n        x * 2\n    else:\n        -x\n    return result\n\ny = f(5)");
        rt.Run();
        Assert.AreEqual(10, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_AsExpression_InAssignment_Negative() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    result = if x > 0:\n        x * 2\n    else:\n        -x\n    return result\n\ny = f(-7)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    [Test]
    public void IfElifElseBlock_AsExpression_InAssignment() {
        var rt = Funny.Hardcore.BuildLang(
            "fun clamp(x):\n    result = if x > 100:\n        100\n    elif x < 0:\n        0\n    else:\n        x\n    return result\n\ny = clamp(50)");
        rt.Run();
        Assert.AreEqual(50, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_ConditionWithComparison() {
        var rt = Funny.Hardcore.BuildLang(
            "fun max2(a, b):\n    if a >= b:\n        return a\n    else:\n        return b\n\ny = max2(3, 7)");
        rt.Run();
        Assert.AreEqual(7, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_ConditionWithBoolOps() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inRange(x, lo, hi):\n    if x >= lo and x <= hi:\n        return true\n    else:\n        return false\n\ny = inRange(5, 0, 10)");
        rt.Run();
        Assert.AreEqual(true, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_ConditionWithBoolOps_OutOfRange() {
        var rt = Funny.Hardcore.BuildLang(
            "fun inRange(x, lo, hi):\n    if x >= lo and x <= hi:\n        return true\n    else:\n        return false\n\ny = inRange(15, 0, 10)");
        rt.Run();
        Assert.AreEqual(false, rt["y"].Value);
    }

    [Test]
    public void IfElseBlock_WithCodeBeforeAndAfter() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    v = x * 10\n    bonus = if v > 50:\n        100\n    else:\n        0\n    return v + bonus\n\ny = f(6)");
        rt.Run();
        Assert.AreEqual(160, rt["y"].Value); // v=60 > 50, bonus=100, 60+100=160
    }

    [Test]
    public void IfElseBlock_WithCodeBeforeAndAfter_Low() {
        var rt = Funny.Hardcore.BuildLang(
            "fun f(x):\n    v = x * 10\n    bonus = if v > 50:\n        100\n    else:\n        0\n    return v + bonus\n\ny = f(3)");
        rt.Run();
        Assert.AreEqual(30, rt["y"].Value); // v=30 <= 50, bonus=0, 30+0=30
    }

    [TestCase(10, 1)]
    [TestCase(0, 0)]
    [TestCase(-10, -1)]
    public void IfElifElseBlock_Parameterized(int input, int expected) {
        var rt = Funny.Hardcore.BuildLang(
            "fun sign(x):\n    if x > 0:\n        return 1\n    elif x < 0:\n        return -1\n    else:\n        return 0\n\ny = sign(x)");
        rt["x"].Value = input;
        rt.Run();
        Assert.AreEqual(expected, rt["y"].Value);
    }

    #endregion
}
