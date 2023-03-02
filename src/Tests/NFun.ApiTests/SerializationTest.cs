namespace NFun.ApiTests;

using System;
using NUnit.Framework;
using TestTools;

public class SerializationTest {
    void AssertSerializationIgnoreFormatting(object input, string expected) {
        var result = Serialization.Serializer.Serialize(input);
        var nonFormatExpected = expected.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        var nonFormatResult = result.Replace(" ", "").Replace("\r", "").Replace("\n", "");
        Assert.AreEqual(nonFormatExpected, nonFormatResult);
    }

    void AssertSerialization(object input, string expected) {
        var result = Serialization.Serializer.Serialize(input);
        Assert.AreEqual(expected, result);
    }

    [TestCase(1, "1")]
    [TestCase(-10, "-10")]
    [TestCase("hey", "'hey'")]
    [TestCase("it's me", "'it\\'s me'")]
    [TestCase(true, "true")]
    [TestCase(false, "false")]
    [TestCase(new[] { 1, 2, 3, 4 }, "[1, 2, 3, 4]")]
    public void AssertPrimitiveSerialization(object input, string expected) =>
        AssertSerializationIgnoreFormatting(input, expected);


    [Test]
    public void AssertStructFormatSerialization_1() => AssertSerialization(
        new UserInputModel(
            name: "Kate",
            age: 42,
            size: 3.5,
            balance: Decimal.One,
            iq: 50,
            ids: new[] { 1, 2, 3, 4 }
        ),
@"{
    ids = [1, 2, 3, 4]
    name = 'Kate'
    age = 42
    size = 3.5
    iq = 50
    balance = 1
}");

    [Test]
    public void AssertStructFormatSerialization_2() => AssertSerialization(
        new ContextModel2(
            id: 42,
            inputs: new[] { 1, 2, 3, 4, 5 },
            users: new[] {
                new UserInputModel(
                    name: "Kate",
                    age: 42,
                    size: 3.5,
                    iq: 50,
                    balance: Decimal.One,
                    ids: new[] { 1, 2, 3 }
                ),
                new UserInputModel(
                    name: "",
                    age: -1,
                    size: 0.01,
                    iq: -1,
                    balance: Decimal.Zero,
                    ids: new int[0]
                ),
            }
        ) {
            Price = 123.4,
            Results = new[] { "", "" },
            Taxes = Decimal.MinusOne,
            Contracts = new[] {
                new ContractOutputModel {
                    Id = 26, Items = new[] { "1", "2", "3" }, Price = 142.2, Taxes = Decimal.Zero
                }
            }
        },
@"{
    users = [
        {
            ids = [1, 2, 3]
            name = 'Kate'
            age = 42
            size = 3.5
            iq = 50
            balance = 1
        },
        {
            ids = []
            name = ''
            age = -1
            size = 0.01
            iq = -1
            balance = 0
        }
    ]
    id = 42
    inputs = [1, 2, 3, 4, 5]
    price = 123.4
    results = ['', '']
    taxes = -1
    contracts = [
        {
            id = 26
            items = ['1', '2', '3']
            price = 142.2
            taxes = 0
        }
    ]
}");

    [Test]
    public void AssertStructSerialization_2() => AssertSerializationIgnoreFormatting(
        new ContextModel2(
            id: 42,
            inputs: new[] { 1, 2, 3, 4, 5 },
            users: new[] {
                new UserInputModel(
                    name: "Kate",
                    age: 42,
                    size: 3.5,
                    iq: 50,
                    balance: Decimal.One,
                    ids: new[] { 1, 2, 3 }
                ),
                new UserInputModel(
                    name: "",
                    age: -1,
                    size: 0.01,
                    iq: -1,
                    balance: Decimal.Zero,
                    ids: new int[0]
                ),
            }
        ) {
            Price = 123.4,
            Results = new[] { "", "" },
            Taxes = Decimal.MinusOne,
            Contracts = new[] {
                new ContractOutputModel {
                    Id = 26, Items = new[] { "1", "2", "3" }, Price = 142.2, Taxes = Decimal.Zero
                }
            }
        },
        @"
            {
                users = [
                    {
                        ids = [1,2,3]
                        name = 'Kate'
                        age = 42
                        size = 3.5
                        iq = 50
                        balance = 1
                    },
                    {
                        ids = []
                        name = ''
                        age = -1
                        size = 0.01
                        iq = -1
                        balance = 0
                    }
                ]
                id = 42
                inputs = [1,2,3,4,5]
                price = 123.4
                results = ['','']
                taxes = -1
                contracts = [{
                    id = 26
                    items = ['1', '2', '3']
                    price = 142.2
                    taxes = 0
                }]
            }");

    [Test]
    public void AssertStructSerialization_1() => AssertSerializationIgnoreFormatting(
        new UserInputModel(
            name: "Kate",
            age: 42,
            size: 3.5,
            balance: Decimal.One,
            iq: 50,
            ids: new[] { 1, 2, 3, 4 }
        ),
        @"
            {
                ids = [1,2,3,4]
                name = 'Kate'
                age = 42
                size = 3.5
                iq = 50
                balance = 1
            }");
}
