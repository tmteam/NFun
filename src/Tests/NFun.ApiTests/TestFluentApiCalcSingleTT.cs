using System;
using System.Net;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests; 

public class TestFluentApiCalcSingleTT {

    [TestCase("(Age == 13) and (NAME == 'vasa')", true)]
    [TestCase("(age == 13) and (name == 'vasa')", true)]
    [TestCase("(age != 13) or (name != 'vasa')", false)]
    [TestCase("'{name}{age}'.reverse()=='31asav'", true)]
    [TestCase("'mama'=='{name}{age}'.reverse()", false)]
    public void ReturnsBoolean(string expr, bool expected)
        => CalcInDifferentWays(expr, new UserInputModel("vasa", 13), expected);

    [Test]
    public void AccessToInput() {
        var res = Funny.Calc<ModelWithInt, long>("id+1", new ModelWithInt { id = 54 });
        Assert.IsInstanceOf<long>(res);
        Assert.AreEqual(55, res);
    }

    [Test]
    public void BuildLambdaTwoTimes() {
        var calculator1 = Funny.BuildForCalc<ModelWithInt, string>();
        var calculator2 = Funny.BuildForCalc<ModelWithInt, string>();

        Func<ModelWithInt, string> lambda1 = calculator1.ToLambda("'{id}'");
        Func<ModelWithInt, string> lambda2 = calculator2.ToLambda("'{id}'");

        var result1 = lambda1(new ModelWithInt { id = 42 });
        var result2 = lambda2(new ModelWithInt { id = 1 });
        Assert.AreEqual(result1, "42");
        Assert.AreEqual(result2, "1");
    }

    [Test]
    public void IoComplexTypeTransforms()
        => CalcInDifferentWays(
            expr: "{id = age; items = ids.map(rule '{it}'); price = size*2 + balance; taxes = balance}",
            input: new UserInputModel("vasa", 13, size: 21, balance: new Decimal(1.5), iq: 12, 1, 2, 3, 4),
            expected: new ContractOutputModel {
                Id = 13,
                Items = new[] { "1", "2", "3", "4" },
                Price = 21 * 2 + 1.5,
                Taxes = new decimal(1.5)
            });

    [TestCase("ids.count(rule it>2)", 2)]
    [TestCase("1", 1)]
    public void ReturnsInt(string expr, int expected)
        => CalcInDifferentWays(expr, new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 12, 1, 2, 3, 4), expected);

    [TestCase("default", "0.0.0.0")]
    [TestCase("127.1.2.24", "127.1.2.24")]
    public void ReturnsIp(string expr, string expected)
        => CalcInDifferentWays(expr, new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 12, 1, 2, 3, 4), IPAddress.Parse(expected));
    
    [TestCase("IDS.filter(rule it>aGe).map(rule it*it)", new[] { 10201, 10404 })]
    [TestCase("ids.filter(rule it>age).map(rule it*it)", new[] { 10201, 10404 })]
    [TestCase("ids.filter(rule it>2)", new[] { 101, 102 })]
    [TestCase("out:int[]=ids.filter(rule it>age).map(rule it*it)", new[] { 10201, 10404 })]
    [TestCase("out:int[]=127.0.0.1.convert()", new[] { 127, 0,0,1 })]
    public void ReturnsIntArray(string expr, int[] expected)
        => CalcInDifferentWays(expr, new UserInputModel("vasa", 2, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 101, 102), expected);

    [Test]
    public void InputFieldIsCharArray()
        => CalcInDifferentWays(
            "[letters.reverse()]", new ModelWithCharArray2 {
                Letters = new[] { 't', 'e', 's', 't' }
            }, new[] { "tset" });

    [TestCase("IDS.reverse().join(',')", "4,3,2,1")]
    [TestCase("Ids.reverse().join(',')", "4,3,2,1")]
    [TestCase("ids.reverse().join(',')", "4,3,2,1")]
    [TestCase("'Hello world'", "Hello world")]
    [TestCase("'{name}{age}'.reverse()", "31asav")]
    [TestCase("'{Name}{Age}'.reverse()", "31asav")]
    [TestCase("name.reverse()", "asav")]
    public void ReturnsText(string expr, string expected)
        => CalcInDifferentWays(expr, new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 3, 4), expected);

    [TestCase("ids.map(rule it.toText())", new[] { "1", "2", "101", "102" })]
    [TestCase("['Hello','world']", new[] { "Hello", "world" })]
    public void ReturnsArrayOfTexts(string expr, string[] expected)
        => CalcInDifferentWays(
            expr: expr,
            input: new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 101, 102),
            expected: expected);

    private static void CalcInDifferentWays<TInput, TOutput>(string expr, TInput input, TOutput expected) {
        var result1 = Funny.Calc<TInput, TOutput>(expr, input);
        var calculator = Funny.BuildForCalc<TInput, TOutput>();
        var result2 = calculator.Calc(expr, input);
        var result3 = calculator.Calc(expr, input);
        var lambda1 = calculator.ToLambda(expr);
        var result4 = lambda1(input);
        var result5 = lambda1(input);
        var lambda2 = calculator.ToLambda(expr);
        var result6 = lambda2(input);
        var result7 = lambda2(input);
        var result8 = Funny
                      .WithConstant("SomeNotUsedConstant", 42)
                      .BuildForCalc<TInput, TOutput>()
                      .Calc(expr, input);

        Assert.IsTrue(TestHelper.AreSame(expected, result1), $"Funny.Calc<TInput, TOutput>. \r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()}");
        Assert.IsTrue(TestHelper.AreSame(expected, result2), $"Funny.BuildForCalc<TInput, TOutput>().Calc() #1\r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()} ");
        Assert.IsTrue(TestHelper.AreSame(expected, result3), $"Funny.BuildForCalc<TInput, TOutput>().Calc() #2\r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()}");
        Assert.IsTrue(TestHelper.AreSame(expected, result4), $"Funny.BuildForCalc<TInput, TOutput>().ToLambda()(input) #1\r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()}");
        Assert.IsTrue(TestHelper.AreSame(expected, result5), $"Funny.BuildForCalc<TInput, TOutput>().ToLambda()(input) #2\r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()}");
        Assert.IsTrue(TestHelper.AreSame(expected, result6),
            $"Funny.BuildForCalc<TInput, TOutput>().ToLambda #2()(input) #1\r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()}");
        Assert.IsTrue(TestHelper.AreSame(expected, result7),
            $"Funny.BuildForCalc<TInput, TOutput>().ToLambda #2()(input) #2\r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()}");
        Assert.IsTrue(TestHelper.AreSame(expected, result8), $"WithConstant(SomeNotUsedConstant, 42)\r\nExpected: {expected.ToStringSmart()} \r\nActual: {result1.ToStringSmart()}");
    }

    [Test]
    public void ReturnsComplexIntArrayConstant()
        => CalcInDifferentWays(
            expr: "[[[1,2],[]],[[3,4]],[[]]]",
            input: new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 3, 4),
            expected: new[] {
                new[] { new[] { 1, 2 }, Array.Empty<int>() },
                new[] { new[] { 3, 4 } },
                new[] { Array.Empty<int>() }
            }
        );

    [TestCase("")]
    [TestCase("x:int = 2")]
    [TestCase("a = 12; b = 32; x = a*b")]
    [TestCase("y = age")]
    public void NoOutputSpecified_throws(string expr)
        => Assert.Throws<FunnyParseException>(
            () =>
                Funny.Calc<UserInputModel, int>(expr, new UserInputModel(age: 42)));

    [TestCase("age*AGE")]
    public void UseDifferentInputCase_throws(string expression)
        => Assert.Throws<FunnyParseException>(
            () =>
                Funny.Calc<UserInputModel, int>(expression, new UserInputModel(age: 22)));

    [Test]
    public void OutputTypeContainsNoEmptyConstructor_throws()
        => Assert.Throws<FunnyInvalidUsageException>(
            () => Funny.Calc<UserInputModel, ModelWithoutEmptyConstructor>(
                "{name = name}"
              , new UserInputModel("vasa")));

    [TestCase("age>someUnknownvariable")]
    [TestCase("x:int;")]
    public void UseUnknownInput_throws(string expr)
        => Assert.Throws<FunnyParseException>(
            () =>
                Funny.Calc<UserInputModel, bool>(expr, new UserInputModel(age: 22)));

    [Test]
    public void UseDecimalWithoutDialect_throws()
        => TestHelper.AssertObviousFailsOnApiUsage(() =>
            Funny.Calc<UserInputModel, decimal>("123", new UserInputModel(age: 22)));

    [Test]
    public void UseDecimalWithBuilderWithoutDialect_throws()
        => TestHelper.AssertObviousFailsOnApiUsage(() =>
            Funny.WithConstant("id", 42).Calc<UserInputModel, decimal>("123", new UserInputModel(age: 22)));
}