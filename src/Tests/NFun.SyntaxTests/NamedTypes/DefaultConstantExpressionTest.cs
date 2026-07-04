using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests.NamedTypes;

/// <summary>
/// Tests that default values in type definitions AND function parameters
/// must be constant expressions — no variable references, no function calls.
/// Both share the same rule for consistency.
/// </summary>
public class DefaultConstantExpressionTest {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    #region Type defaults — allowed (constant expressions)

    [Test]
    public void TypeDefault_IntLiteral() =>
        "type t = {x = 42}; out = t{}.x".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_RealLiteral() =>
        "type t = {x = 3.14}; out = t{}.x".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_BoolLiteral() =>
        "type t = {x = true}; out = t{}.x".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_TextLiteral() =>
        "type t = {x = 'hello'}; out = t{}.x".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_Arithmetic() {
        var r = "type t = {x = 30 * 60}; out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(1800, r.Get("out"));
    }

    [Test]
    public void TypeDefault_Negation() {
        var r = "type t = {x = -1}; out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(-1, r.Get("out"));
    }

    [Test]
    public void TypeDefault_BoolExpression() =>
        "type t = {x = true and false}; out = t{}.x".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_EmptyArray() =>
        "type t = {x:int[] = []}; out = t{}".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_ArrayOfLiterals() =>
        "type t = {x = [1, 2, 3]}; out = t{}".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_NestedStruct() =>
        "type t = {x = {a = 1, b = 2}}; out = t{}".CalcWithNamedTypes();

    [Test]
    public void TypeDefault_ComplexConstant() {
        var r = "type t = {x = (10 + 20) * 3}; out = t{}.x".CalcWithNamedTypes();
        Assert.AreEqual(90, r.Get("out"));
    }

    #endregion

    #region Type defaults — forbidden (variable references)

    [Test]
    public void TypeDefault_VariableRef_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "v = 150; type t = {x = v * 2}; out = t{}".BuildWithNamedTypes());

    [Test]
    public void TypeDefault_VariableRef_SameName_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "x = 100; type t = {x = x * 2}; out = t{}".BuildWithNamedTypes());

    [Test]
    public void TypeDefault_InputVariable_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "type t = {x = input * 2}; out = t{}".BuildWithNamedTypes());

    #endregion

    #region Type defaults — forbidden (function calls)

    [Test]
    public void TypeDefault_FunctionCall_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "type t = {x = max(1, 2)}; out = t{}".BuildWithNamedTypes());

    [Test]
    public void TypeDefault_MethodCall_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "type t = {x = 'hello'.reverse()}; out = t{}".BuildWithNamedTypes());

    [Test]
    public void TypeDefault_ArrayFunction_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "type t = {x = [1,2,3].count()}; out = t{}".BuildWithNamedTypes());

    #endregion

    #region Function defaults — allowed (constant expressions)

    [Test]
    public void FuncDefault_IntLiteral() {
        var r = "f(x:int = 42) = x; out = f()".CalcWithNamedTypes();
        Assert.AreEqual(42, r.Get("out"));
    }

    [Test]
    public void FuncDefault_BoolLiteral() {
        var r = "f(x:bool = true) = x; out = f()".CalcWithNamedTypes();
        Assert.AreEqual(true, r.Get("out"));
    }

    [Test]
    public void FuncDefault_TextLiteral() {
        var r = "f(x:text = 'hi') = x; out = f()".CalcWithNamedTypes();
        Assert.AreEqual("hi", r.Get("out"));
    }

    [Test]
    public void FuncDefault_Arithmetic() {
        var r = "f(x:int = 30 * 60) = x; out = f()".CalcWithNamedTypes();
        Assert.AreEqual(1800, r.Get("out"));
    }

    [Test]
    public void FuncDefault_Negation() {
        var r = "f(x:int = -1) = x; out = f()".CalcWithNamedTypes();
        Assert.AreEqual(-1, r.Get("out"));
    }

    [Test]
    public void FuncDefault_Override() {
        var r = "f(x:int = 42) = x; out = f(100)".CalcWithNamedTypes();
        Assert.AreEqual(100, r.Get("out"));
    }

    [Test]
    public void FuncDefault_MultipleParams() {
        var r = "f(a:int, b:int = 10, c:int = 20) = a + b + c; out = f(1)".CalcWithNamedTypes();
        Assert.AreEqual(31, r.Get("out"));
    }

    #endregion

    #region Function defaults — forbidden (variable references) — consistency with type defaults

    [Test]    public void FuncDefault_VariableRef_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "v = 150; f(x:int = v) = x; out = f()".BuildWithNamedTypes());

    [Test]    public void FuncDefault_InputVariable_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "f(x:int = input) = x; out = f()".BuildWithNamedTypes());

    #endregion

    #region Function defaults — forbidden (function calls) — consistency with type defaults

    [Test]    public void FuncDefault_FunctionCall_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "f(x:int = max(1, 2)) = x; out = f()".BuildWithNamedTypes());

    [Test]    public void FuncDefault_UserFunction_Error() =>
        Assert.Throws<FunnyParseException>(
            () => "g() = 42; f(x:int = g()) = x; out = f()".BuildWithNamedTypes());

    #endregion

    #region Consistency: same expression valid in both type and function context

    [Test]
    public void Consistency_LiteralWorksInBoth() {
        var r = ("type t = {x:int = 42}; " +
                 "f(x:int = 42) = x; " +
                 "a = t{}.x; b = f(); " +
                 "out = a + b")
            .CalcWithNamedTypes();
        Assert.AreEqual(84, r.Get("out"));
    }

    [Test]
    public void Consistency_ArithmeticWorksInBoth() {
        var r = ("type t = {x:int = 10 + 20}; " +
                 "f(x:int = 10 + 20) = x; " +
                 "a = t{}.x; b = f(); " +
                 "out = a + b")
            .CalcWithNamedTypes();
        Assert.AreEqual(60, r.Get("out"));
    }

    #endregion
}
