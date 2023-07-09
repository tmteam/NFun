using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ConcurrentTests;

public class FluentApiCalcDynamicTConcurrentTest {
    [TestCase("(Age == 13) and (NAME == 'vasa')", true)]
    [TestCase("(age == 13) and (name == 'vasa')", true)]
    [TestCase("(age != 13) or (name != 'vasa')", false)]
    [TestCase("'{name}{age}'.reverse()=='31asav'", true)]
    [TestCase("'mama'=='{name}{age}'.reverse()", false)]
    [TestCase("1<2<age>-100>-150 != 1<4<age>-100>-150", false)]
    [TestCase("(1<2<age>-100>-150) == (1<2<age>-100>-150) == true", true)]
    public void ReturnsBoolean(string expr, bool expected)
        => expr.CalcDynamicTypedInDifferentWays(new UserInputModel("vasa", 13), expected);

    [Test]
    public void IoComplexTypeTransforms()
        => "{id = age; items = ids.map(rule '{it}'); price = size*2 + balance; taxes = balance}"
            .CalcDynamicTypedInDifferentWays(
                input: new UserInputModel("vasa", 13, size: 21, balance: new Decimal(1.5), iq: 12, 1, 2, 3, 4),
                expected: new ContractOutputModel {
                    Id = 13, Items = new[] { "1", "2", "3", "4" }, Price = 21 * 2 + 1.5, Taxes = new decimal(1.5)
                });

    [TestCase("ids.count(rule it>2)", 2)]
    [TestCase("1", 1)]
    public void ReturnsInt(string expr, int expected)
        => expr.CalcDynamicTypedInDifferentWays(
            new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 12, 1, 2, 3, 4),
            expected);

    [TestCase("IDS.filter(rule it>aGe).map(rule it*it)", new[] { 10201, 10404 })]
    [TestCase("ids.filter(rule it>age).map(rule it*it)", new[] { 10201, 10404 })]
    [TestCase("ids.filter(rule it>2)", new[] { 101, 102 })]
    [TestCase("out:int[]=ids.filter(rule it>age).map(rule it*it)", new[] { 10201, 10404 })]
    public void ReturnsIntArray(string expr, int[] expected)
        => expr.CalcDynamicTypedInDifferentWays(
            new UserInputModel("vasa", 2, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 101, 102),
            expected);

    [Test]
    public void InputFieldIsCharArray() =>
        "[letters.reverse()]".CalcDynamicTypedInDifferentWays(
            new ModelWithCharArray2 { Letters = new[] { 't', 'e', 's', 't' } }, new[] { "tset" });

    [TestCase("IDS.reverse().join(',')", "4,3,2,1")]
    [TestCase("Ids.reverse().join(',')", "4,3,2,1")]
    [TestCase("ids.reverse().join(',')", "4,3,2,1")]
    [TestCase("'Hello world'", "Hello world")]
    [TestCase("'{name}{age}'.reverse()", "31asav")]
    [TestCase("'{Name}{Age}'.reverse()", "31asav")]
    [TestCase("name.reverse()", "asav")]
    public void ReturnsText(string expr, string expected)
        => expr.CalcDynamicTypedInDifferentWays(
            new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 3, 4),
            expected);

    [TestCase("ids.map(rule it.toText())", new[] { "1", "2", "101", "102" })]
    [TestCase("['Hello','world']", new[] { "Hello", "world" })]
    public void ReturnsArrayOfTexts(string expr, string[] expected)
        => expr.CalcDynamicTypedInDifferentWays(
            input: new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 101, 102),
            expected: expected);

    [Test]
    public void ReturnsComplexIntArrayConstant()
        => "[[[1,2],[]],[[3,4]],[[]]]".CalcDynamicTypedInDifferentWays(
            input: new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 3, 4),
            expected: new[] {
                new[] { new[] { 1, 2 }, Array.Empty<int>() }, new[] { new[] { 3, 4 } }, new[] { Array.Empty<int>() }
            }
        );
}
