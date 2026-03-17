namespace NFun.SyntaxTests.OptionalTypes;

using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

[TestFixture]
public class OptionalDisabledTest {

    // ?? operator
    [TestCase("y = none ?? 42")]
    [TestCase("x:int?\r y = x ?? 0")]
    [TestCase("y = 42 ?? 0")]
    public void CoalesceOperator_FailsWhenDisabled(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Disabled);

    // ! operator
    [TestCase("y = 1!")]
    [TestCase("x:int?\r y = x!")]
    public void ForceUnwrapOperator_FailsWhenDisabled(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Disabled);

    // none literal
    [TestCase("y = none")]
    [TestCase("none")]
    [TestCase("y = if(true) 1 else none")]
    public void NoneLiteral_FailsWhenDisabled(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Disabled);

    // ?. operator
    [TestCase("x = {a=1}\r y = x?.a")]
    public void SafeAccess_FailsWhenDisabled(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Disabled);

    // optional type declarations
    [TestCase("x:int?")]
    [TestCase("x:real?")]
    [TestCase("x:bool?")]
    [TestCase("x:text?")]
    [TestCase("x:int?[]")]
    [TestCase("x:int[]?")]
    [TestCase("y:int? = 42")]
    public void OptionalTypeDeclaration_FailsWhenDisabled(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Disabled);

    // function args with optional types
    [TestCase("f(a:int?) = a\r y = f(1)")]
    public void FunctionArgOptionalType_FailsWhenDisabled(string expr) =>
        expr.AssertObviousFailsOnParse(optionalTypesSupport: OptionalTypesSupport.Disabled);

    // Error code verification for each token type
    [Test]
    public void NoneLiteral_HasCorrectErrorCode() {
        var ex = Assert.Throws<FunnyParseException>(
            () => "none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        Assert.AreEqual(883, ex!.ErrorCode);
    }

    [Test]
    public void SafeAccess_HasCorrectErrorCode() {
        var ex = Assert.Throws<FunnyParseException>(
            () => "x = {a=1}\r y = x?.a"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        Assert.AreEqual(884, ex!.ErrorCode);
    }

    [Test]
    public void OptionalType_HasCorrectErrorCode() {
        var ex = Assert.Throws<FunnyParseException>(
            () => "x:int?".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        Assert.AreEqual(885, ex!.ErrorCode);
    }

    [Test]
    public void CoalesceOperator_HasCorrectErrorCode() {
        var ex = Assert.Throws<FunnyParseException>(
            () => "y = 42 ?? 0".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        Assert.AreEqual(882, ex!.ErrorCode);
    }

    [Test]
    public void ForceUnwrap_HasCorrectErrorCode() {
        var ex = Assert.Throws<FunnyParseException>(
            () => "y = 1!".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        Assert.AreEqual(882, ex!.ErrorCode);
    }

    // Error range verification
    [Test]
    public void NoneLiteral_ErrorRange() {
        var ex = Assert.Throws<FunnyParseException>(
            () => "y = none".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        // 'none' starts at position 4, ends at 8
        Assert.AreEqual(4, ex!.Start);
        Assert.AreEqual(8, ex.End);
    }

    [Test]
    public void SafeAccess_ErrorRange() {
        // "x = {a=1}\r y = x?.a"
        // position:         15 = 'x', 16='?', 17='.', 18='a', end=19
        var ex = Assert.Throws<FunnyParseException>(
            () => "x = {a=1}\r y = x?.a"
                .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        Assert.That(ex!.Start, Is.GreaterThanOrEqualTo(15));
        Assert.That(ex.End, Is.LessThanOrEqualTo(20));
    }

    [Test]
    public void OptionalTypeDeclaration_ErrorRange() {
        var ex = Assert.Throws<FunnyParseException>(
            () => "x:int?".BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Disabled));
        // the whole declaration 'x:int?' is interval [0..6]
        Assert.AreEqual(0, ex!.Start);
        Assert.AreEqual(6, ex.End);
    }
}
