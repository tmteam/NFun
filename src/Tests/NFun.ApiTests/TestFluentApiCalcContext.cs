using System;
using System.Collections.Generic;
using System.Globalization;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests {

public class TestFluentApiCalcContext {
    [Test]
    public void Const_FullInitialization() {
        var context = new ContractOutputModel();
        Funny.CalcContext("id = 42; items = ['vasa','kate']; price = 42.1; taxes = 123.5",context);
        Assert.AreEqual(42, context.Id);
        Assert.AreEqual(42.1, context.Price);
        Assert.AreEqual(new Decimal(123.5), context.Taxes);

        CollectionAssert.AreEqual(new[] { "vasa", "kate" }, context.Items);
    }

    [Test]
    public void Const_OutputFieldIsConstCharArray() {
        var context = new ModelWithCharArray();
        Funny.CalcContext("Chars = 'test'", context);
        Assert.IsTrue(
            TestHelper.AreSame(
                new ModelWithCharArray {
                    Chars = new[] { 't', 'e', 's', 't' }
                }, context));
    }


    [Test]
    public void Const_NofieldsInitialized_throws()
        => Assert.Throws<FunnyParseException>(
            () =>
                Funny.CalcContext("someField1 = 13.1; somefield2 = 2",new ContractOutputModel()));

    [Test]
    public void Const_AnonymousEquation_throws()
        => Assert.Throws<FunnyParseException>(() => Funny.CalcContext("13.1",new ContractOutputModel()));

    [Test]
    public void Const_UnknownInputIdUsed_throws()
        => Assert.Throws<FunnyParseException>(() => Funny.CalcContext("id = someInput",new ContractOutputModel()));

    [TestCase("id = 42; price = ID")]
    [TestCase("id = 42; ID = 13")]
    public void Const_UseDifferentInputCase_throws(string expression) =>
        Assert.Throws<FunnyParseException>(() => Funny.CalcContext(expression,new ContractOutputModel()));

    [Test]
    public void Const_SomeFieldInitialized_DefaultValuesInUninitalizedFields() {
        var context = new ContractOutputModel();

        Funny.CalcContext("id = 321; somenotExisted = 32",context);
        Assert.AreEqual(321, context.Id);
        Assert.AreEqual(new ContractOutputModel().Price, context.Price);
        Assert.AreEqual(new ContractOutputModel().Taxes, context.Taxes);
        CollectionAssert.AreEqual(new ContractOutputModel().Items, context.Items);
    }
    
    //--------------
    [TestCase("omodel =  { id = imodel.age*2; items = imodel.ids.map(toText);  Price = 42.1 + imodel.balance}")]
    [TestCase("omodel =  { ID = imodel.age*2; Items = imodel.iDs.map(toText);  price = 42.1 + imodel.balAncE")]
    public void MapContracts(string expr) =>
        CalcInDifferentWays(
            expr,
            input:  new UserInputModel("vasa", 13, ids: new[] { 1, 2, 3 }, balance: new Decimal(100.1)),
            expected: new ContractOutputModel { Id = 26, Items = new[] { "1", "2", "3" }, Price = 142.2 });

    [Test]
    public void FullConstInitialization() =>
        CalcInDifferentWays(
            "omodel = {id = 42; items = ['vasa','kate']; price = 42.1; taxes = 42.2}", new UserInputModel(),
            new ContractOutputModel {
                Id = 42,
                Price = 42.1,
                Taxes = new decimal(42.2),
                Items = new[] { "vasa", "kate" }
            }
        );
    
    // [Test]
    // public void NoFieldsInitialized_throws()
    //     => Assert.Throws<FunnyParseException>(
    //         () =>
    //             Funny.CalcMany<UserInputModel, ContractOutputModel>(
    //                 expression: "omodel = {someField1 = imodel.age; somefield2 = 2}",
    //                 input: new UserInputModel()));
    //
    // [TestCase("13.1")]
    // [TestCase("age")]
    // [TestCase("ids")]
    // public void AnonymousEquation_throws(string expr)
    //     => Assert.Throws<FunnyParseException>(
    //         () => Funny.CalcMany<UserInputModel, ContractOutputModel>(expr, new UserInputModel()));

    // [Test]
    // public void UnknownInputIdUsed_throws()
    //     => Assert.Throws<FunnyParseException>(
    //         () => Funny.CalcMany<UserInputModel, ContractOutputModel>(
    //             "omodel = {id = imodel.someInput*imodel.age}", new UserInputModel()));

    // [Test]
    // public void SomeFieldInitialized_DefaultValuesInUninitalizedFields() {
    //     var result = Funny.CalcMany<UserInputModel, ContractOutputModel>(
    //         "id = 321; somenotExisted = age", new UserInputModel());
    //     Assert.AreEqual(321, result.Id);
    //     Assert.AreEqual(new ContractOutputModel().Price, result.Price);
    //     CollectionAssert.AreEqual(new ContractOutputModel().Items, result.Items);
    // }
    //
    // [TestCase("Id = age*Age; ")]
    // [TestCase("Id = 321; Price = ID;")]
    // public void UseDifferentInputCase_throws(string expression) =>
    //     Assert.Throws<FunnyParseException>(
    //         () => Funny.CalcMany<UserInputModel, ContractOutputModel>(expression, new UserInputModel()));

    private void CalcInDifferentWays(string expr, UserInputModel input, ContractOutputModel expected) {
        var context = new TheContext(model: input);
        var expectedContext = new TheContext(context.IntRVal, (UserInputModel)input.Clone()) {
            OModel = expected
        };
        CalcInDifferentWays(expr, context, expectedContext);
    }
    
    private void CalcInDifferentWays<TContext>(string expr, TContext origin, TContext expected) where TContext:ICloneable {
        var c1 = (TContext)origin.Clone();        
        Funny.CalcContext(expr, c1);
        var calculator = Funny.BuildForCalc<TContext>();
        var c2 = (TContext)origin.Clone();        
        calculator.Calc(expr, c2);
        var c3 = (TContext)origin.Clone();        
        calculator.Calc(expr, c3);
        var calculator2 = Funny.WithDialect(IfExpressionSetup.IfIfElse, IntegerPreferredType.I32, RealClrType.IsDouble, IntegerOverflow.Checked).BuildForCalcContext<TContext>();
        
        var c4 = (TContext)origin.Clone();        
        calculator2.Calc(expr, c4);
        var c5 = (TContext)origin.Clone();        
        calculator2.Calc(expr, c5);
        Assert.IsTrue(TestHelper.AreSame(expected, c1));
        Assert.IsTrue(TestHelper.AreSame(expected, c2));
        Assert.IsTrue(TestHelper.AreSame(expected, c3));
        Assert.IsTrue(TestHelper.AreSame(expected, c4));
        Assert.IsTrue(TestHelper.AreSame(expected, c5));

    }
}
}