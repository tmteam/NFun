using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests;

public class TestFluentApiCalcManyT {
    [Test]
    public void FullInitialization() {
        var result =
            Funny.CalcMany<ContractOutputModel>("id = 42; items = ['vasa','kate']; price = 42.1; taxes = 123.5");
        Assert.AreEqual(42, result.Id);
        Assert.AreEqual(42.1, result.Price);
        Assert.AreEqual(new Decimal(123.5), result.Taxes);

        CollectionAssert.AreEqual(new[] { "vasa", "kate" }, result.Items);
    }

    [Test]
    public void OutputFieldIsConstCharArray()
        => FunnyAssert.AreSame(
                new ModelWithCharArray { Chars = new[] { 't', 'e', 's', 't' } },
                Funny.CalcMany<ModelWithCharArray>("Chars = 'test'"));


    [Test]
    public void NofieldsInitialized_throws()
        => Assert.Throws<FunnyParseException>(
            () =>
                Funny.CalcMany<ContractOutputModel>("someField1 = 13.1; somefield2 = 2"));

    [Test]
    public void AnonymousEquation_throws()
        => Assert.Throws<FunnyParseException>(() => Funny.CalcMany<ContractOutputModel>("13.1"));

    [Test]
    public void UnknownInputIdUsed_throws()
        => Assert.Throws<FunnyParseException>(() => Funny.CalcMany<ContractOutputModel>("id = someInput"));

    [TestCase("id = 42; price = ID")]
    [TestCase("id = 42; ID = 13")]
    public void UseDifferentInputCase_throws(string expression)
        => Assert.Throws<FunnyParseException>(() => Funny.CalcMany<ContractOutputModel>(expression));

    [Test]
    public void SomeFieldInitialized_DefaultValuesInUninitalizedFields() {
        var result = Funny.CalcMany<ContractOutputModel>("id = 321; somenotExisted = 32");
        Assert.AreEqual(321, result.Id);
        Assert.AreEqual(new ContractOutputModel().Price, result.Price);
        Assert.AreEqual(new ContractOutputModel().Taxes, result.Taxes);
        CollectionAssert.AreEqual(new ContractOutputModel().Items, result.Items);
    }
}
