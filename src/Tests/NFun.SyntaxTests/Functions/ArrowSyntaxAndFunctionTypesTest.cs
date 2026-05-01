namespace NFun.SyntaxTests.Functions;

using NFun.TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for:
/// 1. Arrow return type syntax: f(x:int)->int = expr
/// 2. Function type syntax: rule(int)->int
/// 3. Type aliases for function types: type transform = rule(int)->int
/// </summary>
[TestFixture]
public class ArrowSyntaxAndFunctionTypesTest {

    // ═══════════════════════════════════════════════════════════
    // ARROW RETURN TYPE — user functions (basics)
    // ═══════════════════════════════════════════════════════════

    [TestCase("f(x:int)->int = x * 2\r out = f(21)", 42)]
    [TestCase("f(x:int)->real = x + 0.5\r out = f(1)", 1.5)]
    [TestCase("f(a:int, b:int)->int = a + b\r out = f(20, 22)", 42)]
    [TestCase("f(x:bool)->bool = not x\r out = f(false)", true)]
    [TestCase("f(x:int)->int = x\r g(x:int)->int = f(x) * 2\r out = g(21)", 42)]
    public void ArrowReturn_UserFunction(string expr, object expected) =>
        expr.AssertReturns("out", expected);

    [TestCase("conv(x:int):real = x; y = conv(2);", 2.0)]
    [TestCase("mysum(a:int, b:int):int = a + b \r y = mysum(20,22)", 42)]
    public void ColonReturn_StillWorks(string expr, object expected) =>
        expr.AssertReturns("y", expected);

    // ═══════════════════════════════════════════════════════════
    // ARROW RETURN TYPE — various return types
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Arrow_ReturnText() =>
        "f(x:text)->text = x.concat('!')\r out = f('hello')".AssertReturns("out", "hello!");

    [Test]
    public void Arrow_ReturnArray() =>
        "f(x:int)->int[] = [x, x+1, x+2]\r out = f(10)".AssertReturns("out", new[] { 10, 11, 12 });

    [Test]
    public void Arrow_ReturnOptional() =>
        "f(x:int)->int? = if(x>0) x else none\r out = f(0) ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -1);

    [Test]
    public void Arrow_ReturnOptional_HasValue() =>
        "f(x:int)->int? = if(x>0) x else none\r out = f(5) ?? -1"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 5);

    // ═══════════════════════════════════════════════════════════
    // ARROW — recursive functions
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Arrow_Recursive_Factorial() =>
        "fact(n:int)->int = if(n <= 1) 1 else n * fact(n-1)\r out = fact(5)"
            .AssertReturns("out", 120);

    [Test]
    public void Arrow_Recursive_NamedType() =>
        ("type node = {v:int, next:node? = none}\r" +
         "lastVal(n:node)->int = if(n.next == none) n.v else lastVal(n.next!)\r" +
         "out = lastVal(node{v=1, next=node{v=42}})")
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);

    [Test]
    public void Arrow_Recursive_Depth3() =>
        ("type node = {v:int, next:node? = none}\r" +
         "lastVal(n:node)->int = if(n.next == none) n.v else lastVal(n.next!)\r" +
         "out = lastVal(node{v=1, next=node{v=2, next=node{v=42}}})")
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);

    // ═══════════════════════════════════════════════════════════
    // ARROW — anonymous functions (rule)
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Arrow_Rule_Typed() =>
        "out = [1,2,3].map(rule(x:int)->int = x * 10)".AssertReturns("out", new[] { 10, 20, 30 });

    [Test]
    public void Colon_Rule_StillWorks() =>
        "out = [1,2,3].map(rule(x:int):int = x * 10)".AssertReturns("out", new[] { 10, 20, 30 });

    // ═══════════════════════════════════════════════════════════
    // FUNCTION TYPE — inline annotations  rule(A)->R
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void FunType_OneArg() =>
        "apply(f:rule(int)->int, x:int)->int = f(x)\r out = apply(rule it*2, 21)"
            .AssertReturns("out", 42);

    [Test]
    public void FunType_TwoArgs() =>
        "apply(f:rule(int,int)->int, a:int, b:int)->int = f(a,b)\r out = apply(rule it1+it2, 20, 22)"
            .AssertReturns("out", 42);

    [Test]
    public void FunType_ZeroArgs() =>
        "call(f:rule()->int)->int = f()\r out = call(rule 42)"
            .AssertReturns("out", 42);

    [Test]
    public void FunType_ReturningBool() =>
        "check(f:rule(int)->bool, x:int)->bool = f(x)\r out = check(rule it > 10, 42)"
            .AssertReturns("out", true);

    [Test]
    public void FunType_ReturningReal() =>
        "apply(f:rule(real)->real, x:real)->real = f(x)\r out = apply(rule it / 2.0, 84.0)"
            .AssertReturns("out", 42.0);

    [Test]
    public void FunType_ReturningText() =>
        "apply(f:rule(text)->text, s:text)->text = f(s)\r out = apply(rule it.concat('!'), 'hi')"
            .AssertReturns("out", "hi!");

    [Test]
    public void FunType_ArrayArg() =>
        "applyAll(f:rule(int)->int, arr:int[])->int[] = arr.map(f)\r out = applyAll(rule it*10, [1,2,3])"
            .AssertReturns("out", new[] { 10, 20, 30 });

    [Test]
    public void FunType_TwoFunParams() =>
        "apply(f:rule(int)->int, g:rule(int)->int, x:int)->int = g(f(x))\r out = apply(rule it+1, rule it*2, 5)"
            .AssertReturns("out", 12);

    [Test]
    public void FunType_Compose() =>
        "compose(f:rule(int)->int, g:rule(int)->int)->rule(int)->int = rule f(g(it))\r out = compose(rule it+1, rule it*2)(5)"
            .AssertReturns("out", 11);

    // ═══════════════════════════════════════════════════════════
    // TYPE ALIAS — function types
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TypeAlias_Simple() =>
        "type transform = rule(int)->int\r apply(f:transform, x:int)->int = f(x)\r out = apply(rule it*3, 14)"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);

    [Test]
    public void TypeAlias_TwoArgs() =>
        "type binop = rule(int,int)->int\r calc(f:binop, a:int, b:int)->int = f(a,b)\r out = calc(rule it1*it2, 6, 7)"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);

    [Test]
    public void TypeAlias_Predicate() =>
        "type pred = rule(int)->bool\r check(p:pred, x:int)->bool = p(x)\r out = check(rule it > 2, 42)"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", true);

    [Test]
    public void TypeAlias_Chain() =>
        "type intOp = rule(int)->int\r type myOp = intOp\r apply(f:myOp, x:int)->int = f(x)\r out = apply(rule it+1, 41)"
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);

    // ═══════════════════════════════════════════════════════════
    // COMBINED — arrow + function types + named types
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Combined_NamedStruct_WithFunctionParam() =>
        ("type point = {x:int, y:int}\r" +
         "transform(p:point, f:rule(int)->int)->point = point{x=f(p.x), y=f(p.y)}\r" +
         "out = transform(point{x=1,y=2}, rule it*10).x")
            .CalcWithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 10);

    [Test]
    public void Combined_RecursiveType_WithFunctionParam() =>
        ("type node = {v:int, next:node? = none}\r" +
         "applyToVal(n:node, f:rule(int)->int)->int = f(n.v)\r" +
         "out = applyToVal(node{v=21}, rule it*2)")
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("out", 42);

    [Test]
    public void Combined_HigherOrder_MapWithApply() =>
        "apply(f:rule(int)->int, x:int)->int = f(x)\r out = [1,2,3].map(rule apply(rule it*2, it))"
            .AssertReturns("out", new[] { 2, 4, 6 });

    [Test]
    public void Combined_ArrowWithOptional() =>
        ("safeApply(f:rule(int)->int, x:int?)->int? = if(x != none) f(x!) else none\r" +
         "out = safeApply(rule it*2, 21) ?? -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 42);

    [Test]
    public void Combined_ArrowWithOptional_None() =>
        ("safeApply(f:rule(int)->int, x:int?)->int? = if(x != none) f(x!) else none\r" +
         "out = safeApply(rule it*2, none) ?? -1")
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", -1);

    // ═══════════════════════════════════════════════════════════
    // TIC INFERENCE — arrow doesn't break inference
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Arrow_InferenceStillWorks_NoAnnotation() =>
        "f(x) = x * 2\r out = f(21)".AssertReturns("out", 42);

    [Test]
    public void Arrow_PartialAnnotation() =>
        "f(x:int) = x * 2\r out = f(21)".AssertReturns("out", 42);

    [Test]
    public void FunType_GenericStillWorks() =>
        "apply(f, x:int)->int = f(x)\r out = apply(rule it*2, 21)".AssertReturns("out", 42);

    // ═══════════════════════════════════════════════════════════
    // ERROR CASES
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Error_ArrowOnVariable() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "x->int = 42".Calc());

    [Test]
    public void Error_MissingArrowInFunctionType() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "apply(f:rule(int), x:int):int = f(x)\r out = apply(rule it, 1)".Calc());

    [Test]
    public void Error_MissingReturnTypeAfterArrow() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "f(x:int)-> = x".Calc());

    [Test]
    public void Error_WrongReturnType() =>
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => "f(x:int)->bool = x * 2\r out = f(1)".Calc());
}
