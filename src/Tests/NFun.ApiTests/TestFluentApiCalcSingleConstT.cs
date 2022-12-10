using System;
using System.Net;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests;

public class TestFluentApiCalcSingleConstT {
    [Test]
    public void Smoke() {
        var result = Funny.Calc<bool>("(13 == 13) and ('vasa' == 'vasa')");
        Assert.AreEqual(true, result);
    }

    [TestCase("{id = 13; items = [1,2,3,4].map(rule '{it}'); price = 21*2; taxes = 0}")]
    // Todo: Ignore cases in structs: [TestCase("{Id = 13; Items = [1,2,3,4].map(rule '{it}'); Price = 21*2}")]
    public void IoComplexTypeTransforms(string expr) {
        var result = Funny.Calc<ContractOutputModel>(expr);
        var expected = new ContractOutputModel {
            Id = 13, Items = new[] { "1", "2", "3", "4" }, Taxes = Decimal.Zero, Price = 42
        };
        Assert.IsTrue(TestHelper.AreSame(expected, result));
    }

    [Test]
    public void ArrayTransforms() {
        var result = Funny.Calc<int>("[1,2,3,4].count(rule it>2)");
        Assert.AreEqual(2, result);
    }

    [Test]
    public void ReturnsRealArray() {
        var result = Funny.Calc<double[]>("[1..4].filter(rule it>1).map(rule it**2)");
        Assert.AreEqual(new[] { 4, 9, 16 }, result);
    }

    [Test]
    public void ReturnsText() {
        var result = Funny.Calc<string>("[1..4].reverse().join(',')");
        Assert.AreEqual("4,3,2,1", result);
    }

    [Test]
    public void ReturnsConstantIp() {
        var result = Funny.Calc<IPAddress>("127.0.0.2");
        Assert.AreEqual(new IPAddress(new byte[] { 127, 0, 0, 2 }), result);
    }

    [Test]
    public void ReturnsConstantText() {
        var result = Funny.Calc<string>("'Hello world'");
        Assert.AreEqual("Hello world", result);
    }

    [Test]
    public void ReturnsConstantArrayOfTexts() {
        var result = Funny.Calc<string[]>("['Hello','world']");
        Assert.AreEqual(new[] { "Hello", "world" }, result);
    }

    [Test]
    public void ReturnsArrayOfTexts() {
        var result = Funny.Calc<string[]>("[1..4].map(rule it.toText())");
        Assert.AreEqual(new[] { "1", "2", "3", "4" }, result);
    }

    [Test]
    public void ReturnsArrayOfChars()
        => Assert.AreEqual(new[] { 'T', 'e', 's', 't' }, Funny.Calc<char[]>("'Test'"));

    [Test]
    public void ReturnsComplexIntArrayConstant() {
        var result = Funny.Calc<int[][][]>(
            "[[[1,2],[]],[[3,4]],[[]]]");
        Assert.AreEqual(
            new[] {
                new[] { new[] { 1, 2 }, Array.Empty<int>() }, new[] { new[] { 3, 4 } }, new[] { Array.Empty<int>() }
            }, result);
    }

    [Test]
    public void CalcWithBuilder() {
        var result = Funny
            .WithConstant("pipi", 6)
            .WithFunction<double, double>("toto", (d) => d - 1)
            .Calc("toto(pipi)");
        Assert.AreEqual(5, result);
    }

    [TestCase("")]
    [TestCase("x:int;")]
    [TestCase("x:int = 2")]
    [TestCase("a = 12; b = 32; x = a*b")]
    public void NoOutputSpecified_throws(string expr)
        => Assert.Throws<FunnyInvalidUsageException>(() => Funny.Calc<UserInputModel>(expr));

    [Test]
    public void OutputTypeContainsNoEmptyConstructor_throws() =>
        Assert.Throws<FunnyInvalidUsageException>(
            () => Funny.Calc<UserInputModel>(
                "{name = 'alaska'}"));

    [TestCase("[1..4].filter(rule it>age).map(rule it**2)")]
    [TestCase("age>someUnknownvariable")]
    public void UseUnknownInput_throws(string expression) =>
        Assert.Throws<FunnyParseException>(() => Funny.Calc<object>(expression));

    [TestCase("[1..4].filter(rule it>age).map(rule it*it).any(rule it>12}")]
    [TestCase("age>someUnknownvariable")]
    public void UseUnknownInputWithWrongIntOutputType_throws(string expression) =>
        Assert.Throws<FunnyParseException>(() => Funny.Calc<bool>(expression));

    [Test]
    public void UseDecimalWithoutDialect_throws()
        => TestHelper.AssertObviousFailsOnApiUsage(() => Funny.Calc<decimal>("123"));

    [Test]
    public void UseDecimalWithBuilderWithoutDialect_throws()
        => TestHelper.AssertObviousFailsOnApiUsage(() => Funny.WithConstant("id", 42).Calc<decimal>("123"));
}
