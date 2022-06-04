using System;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.ApiTests; 

public class TestFluentApiWithDialect {
    [Test]
    public void PreferredIntTypeIsI32() {
        var result = Funny
                     .WithDialect(integerPreferredType: IntegerPreferredType.I32)
                     .BuildForCalc<ModelWithInt, Object>()
                     .Calc("42", new ModelWithInt());
        Assert.IsInstanceOf<int>(result);
        Assert.AreEqual(42, result);
    }

    [Test]
    public void PreferredIntTypeIsI64() {
        var result = Funny
                     .WithDialect(integerPreferredType: IntegerPreferredType.I64)
                     .BuildForCalc<ModelWithInt, Object>()
                     .Calc("42", new ModelWithInt());
        Assert.IsInstanceOf<long>(result);
        Assert.AreEqual(42L, result);
    }

    [Test]
    public void PreferredIntTypeIsReal() {
        var result = Funny
                     .WithDialect(integerPreferredType: IntegerPreferredType.Real)
                     .BuildForCalc<ModelWithInt, Object>()
                     .Calc("42", new ModelWithInt());
        Assert.IsInstanceOf<double>(result);
        Assert.AreEqual(42.0, result);
    }

    [Test]
    public void DenyIf_EquationWithIfThrows() =>
        Assert.Throws<FunnyParseException>(
            () => Funny
                  .WithDialect(IfExpressionSetup.Deny)
                  .BuildForCalc<ModelWithInt, Object>()
                  .Calc("if(true) false else true", new ModelWithInt()));

    [Test]
    public void DenyIfIfElse_EquationWithIfIfElseThrows() =>
        Assert.Throws<FunnyParseException>(
            () => Funny
                  .WithDialect(ifExpressionSyntax: IfExpressionSetup.IfIfElse)
                  .BuildForCalc<ModelWithInt, Object>()
                  .Calc("if(true) false if(false) true else true", new ModelWithInt()));


    [TestCase(IfExpressionSetup.Deny)]
    [TestCase(IfExpressionSetup.IfElseIf)]
    [TestCase(IfExpressionSetup.IfIfElse)]
    public void EquationWithoutIfsCalculates(IfExpressionSetup setup) =>
        Assert.AreEqual(
            12, Funny
                .WithDialect(setup, IntegerPreferredType.I32)
                .BuildForCalc<ModelWithInt, Object>()
                .Calc("12", new ModelWithInt()));

    [TestCase(IfExpressionSetup.IfElseIf)]
    [TestCase(IfExpressionSetup.IfIfElse)]
    public void EquationWithIfCalculates(IfExpressionSetup setup) =>
        Assert.AreEqual(
            false, Funny
                   .WithDialect(setup)
                   .BuildForCalc<ModelWithInt, Object>()
                   .Calc("if(true) false else true", new ModelWithInt()));
}