namespace NFun.ConcurrentTests;
using System;
using System.Linq;
using NFun.TestTools;
using NUnit.Framework;

public class FluentApiCalcContextConcurrentTest {
    [Test]
    public void Const_SomeFieldInitialized_DefaultValuesInUninitalizedFields() =>
        "id = 321; somenotExisted = 32"
            .CalcContextInDifferentWays(new ContractOutputModel(), new ContractOutputModel { Id = 321 });

    //--------------
    [TestCase(
        "omodel =  { id = iModel.age*2; items = iModel.ids.map(toText);  Price = 42.1 + iModel.bAlAnce; taXes = 1.23}")]
    [TestCase(
        "omodel =  { ID = imodel.age*2, Items = imodel.iDs.map(toText),  price = 42.1 + imodel.balAncE, TaXes = 1.23}")]
    public void MapContracts(string expr) {
        var input = new UserInputModel("vasa", 13, ids: new[] { 1, 2, 3 }, balance: new Decimal(100.1));
        var expected = new ContractOutputModel {
            Id = 26, Items = new[] { "1", "2", "3" }, Price = 142.2, Taxes = new decimal(1.23)
        };

        var context = new ContextModel1(imodel: input);
        var expectedContext = new ContextModel1(context.IntRVal, (UserInputModel)input.Clone()) { OModel = expected };

        expr.CalcContextInDifferentWays(context, expectedContext);
    }

    [Test]
    public void FullConstInitialization() {
        var input = new UserInputModel();
        var expected = new ContractOutputModel {
            Id = 42, Price = 42.1, Taxes = new decimal(42.2), Items = new[] { "vasa", "kate" }
        };
        var context = new ContextModel1(imodel: input);
        var expectedContext = new ContextModel1(context.IntRVal, (UserInputModel)input.Clone()) { OModel = expected };
        "omodel = {id = 42; items = ['vasa','kate']; price = 42.1; taxes = 42.2}"
            .CalcContextInDifferentWays(context, expectedContext);
    }

    [Test]
    public void CompositeCase() {
        var origin = new ContextModel2(id: 42, inputs: new[] { 1, 2, 3, 4 },
            new[] { new UserInputModel(name: "kate", age: 33, size: 15.5) });
        var expected = (ContextModel2)origin.Clone();
        expected.Price = origin.Inputs.Max() + origin.Users[0].Size;
        expected.Results = origin.Inputs.Reverse().Select(r => r.ToString()).ToArray();
        expected.Contracts = new[] {
            new ContractOutputModel { Id = 1, Items = new[] { "single" }, Price = 456, Taxes = 789 }
        };

        @"
            nonExistedStruct = {value = 1}
            price = inputs.max() + users[0].size; 
            results =  inputs.reverse().map(rule it.toText());
            Contracts = [{Id = 1, Items = ['single'], Price = 456, Taxes = 789}]"
            .CalcContextInDifferentWays(origin, expected);
    }
}
