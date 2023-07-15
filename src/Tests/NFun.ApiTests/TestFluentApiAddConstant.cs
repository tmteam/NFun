using System;
using System.Net;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests;

public class TestFluentApiAddConstant {

    [Test]
    public void Smoke() {
        var calculator = Funny
            .WithConstant("age", 100)
            .WithConstant("name", "vasa")
            .BuildForCalc<ModelWithInt, string>();

        Func<ModelWithInt, string> lambda = calculator.ToLambda("'{name}\\'s id is {id} and age is {age}'");
        var result1 = lambda(new ModelWithInt { id = 42 });
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result1, "vasa's id is 42 and age is 100");
        Assert.AreEqual(result2, "vasa's id is 1 and age is 100");
    }


    [TestCase("id", "id")]
    [TestCase("Id", "id")]
    [TestCase("Id", "Id")]
    [TestCase("id", "Id")]
    public void InputNameOverridesConstant(string constantName, string varName) {
        var calculator = Funny
            .WithConstant(constantName, 100)
            .BuildForCalc<ModelWithInt, string>();

        Func<ModelWithInt, string> lambda = calculator.ToLambda("'id= {" + varName + "}'");

        var result = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result, "id= 42");

        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "id= 1");
    }

    [Test]
    public void AddIpConstant() {
        var res = Funny.WithConstant("myIp", new IPAddress(new byte[] { 123, 45, 67, 89 }))
            .Calc<IPAddress>("myIp");
        Assert.AreEqual(new IPAddress(new byte[] { 123, 45, 67, 89 }), res);
    }


    [TestCase("id", "id")]
    [TestCase("Id", "id")]
    [TestCase("Id", "Id")]
    [TestCase("id", "Id")]
    public void OutputNameOverridesConstant(string constantName, string varName) {
        var calculator = Funny
            .WithConstant(constantName, 100)
            .BuildForCalcContext<ContextModel>();

        var lambda = calculator.ToLambda($"{varName}= age");

        var model = new ContextModel(age: 42);
        lambda(model);
        Assert.AreEqual(model.Id, 42);
        var model2 = new ContextModel(age: 11);
        lambda(model2);
        Assert.AreEqual(model2.Id, 11);
    }

    [Test]
    public void AddDecimalConstantAsDecimalEarly() {
        var calculator = Funny
            .WithConstant("ultra", (Decimal)100.5)
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .BuildForCalcContext<ContextModel>();
        AssertConstantSetToDecimalAndDouble(calculator, 100.5);
    }

    [Test]
    public void AddDecimalConstantAsDecimalLate() {
        var calculator = Funny
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .WithConstant("ultra", (Decimal)100.5)
            .BuildForCalcContext<ContextModel>();
        AssertConstantSetToDecimalAndDouble(calculator, 100.5);
    }

    [Test]
    public void AddDecimalConstantAsDoubleEarly() {
        var calculator = Funny
            .WithConstant("ultra", (Decimal)100.5)
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .BuildForCalcContext<ContextModel>();
        AssertConstantSetToDecimalAndDouble(calculator, 100.5);
    }

    [Test]
    public void AddDecimalConstantAsDoubleLate() {
        var calculator = Funny
            .WithDialect(realClrType: RealClrType.IsDouble)
            .WithConstant("ultra", (Decimal)100.5)
            .BuildForCalcContext<ContextModel>();
        AssertConstantSetToDecimalAndDouble(calculator, 100.5);
    }

    [Test]
    public void AddDoubleConstantAsDecimalEarly() {
        var calculator = Funny
            .WithConstant("ultra", (double)100.5)
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .BuildForCalcContext<ContextModel>();
        AssertConstantSetToDecimalAndDouble(calculator, 100.5);
    }

    [Test]
    public void AddDoubleConstantAsDecimalLate() {
        var calculator = Funny
            .WithDialect(realClrType: RealClrType.IsDecimal)
            .WithConstant("ultra", (double)100.5)
            .BuildForCalcContext<ContextModel>();
        AssertConstantSetToDecimalAndDouble(calculator, 100.5);
    }

    private static void AssertConstantSetToDecimalAndDouble(IContextCalculator<ContextModel> calculator,
        double expected) {
        var lambda = calculator.ToLambda("Price = ultra; Taxes = ultra ");
        var model = new ContextModel(age: 42);
        lambda(model);
        Assert.AreEqual(model.Taxes, (Decimal)expected);
        Assert.AreEqual(model.Price, expected);
    }

    class ContextModel {
        public ContextModel(string name = "vasa", int age = 22, double size = 13.5, Decimal balance = Decimal.One,
            float iq = 50, params int[] ids) {
            Ids = ids;
            Name = name;
            Age = age;
            Size = size;
            Iq = iq;
            Balance = balance;
        }
        public int[] Ids { get; }
        public string Name { get; }
        public int Age { get; }
        public double Size { get; }
        public float Iq { get; }
        public Decimal Balance { get; }

        public int Id { get; set; } = 123;
        public string[] Items { get; set; } = { "default" };
        public double Price { get; set; } = 12.3;
        public Decimal Taxes { get; set; } = Decimal.One;

    }
}
