using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests;

using Tic;

public class TestFluentApiCalcSingleObjectConst {

    [TestCase("(13 == 13) and ('vasa' == 'vasa')", true)]
    [TestCase("[1,2,3,4].count(rule it>2)", 2)]
    [TestCase("[1..4].filter(rule it>2).map(rule it**2)", new[] { 9.0, 16.0 })]
    [TestCase("[1..4].reverse().join(',')", "4,3,2,1")]
    [TestCase("'Hello world'", "Hello world")]
    [TestCase("['Hello','world']", new[] { "Hello", "world" })]
    [TestCase("[1..4].map(rule it.toText())", new[] { "1", "2", "3", "4" })]
    [TestCase("1<2<13>-100>-150 != 1<4<13>-100>-150", false)]
    [TestCase("(1<2<13>-100>-150) == (1<2<13>-100>-150) == true", true)]
    public void GeneralCalcTest(string expr, object expected) =>
        Assert.AreEqual(expected, Funny.Calc(expr));

    [Test]
    public void ReturnsComplexIntArrayConstant() {
        using var _ = TraceLog.Scope;
        var result = Funny.Calc(
            "[[[1,2],[]],[[3,4]],[[]]]");
        Assert.IsInstanceOf<object[]>(result);
        Assert.AreEqual(
            new[] {
                new[] { new[] { 1, 2 }, Array.Empty<int>() }, new[] { new[] { 3, 4 } }, new[] { Array.Empty<int>() }
            }, result);
    }

    [Test]
    public void ReturnsComplexIntArrayConstant2() {
        using var _ = TraceLog.Scope;
        var result = Funny.Calc("[[[1]],[[]]]");
        Assert.IsInstanceOf<int[][][]>(result);
        Assert.AreEqual(
            new[] {
                new[] { new[] { 1 } }, new[] { Array.Empty<int>() }
            }, result);
    }

    [Test]
    public void ReturnsComplexIntArrayConstant3() {
        using var _ = TraceLog.Scope;
        var result = Funny.Calc("[[1],[]]");
        Assert.IsInstanceOf<int[][]>(result);
        Assert.AreEqual(
                new[] { new[] { 1 } ,  Array.Empty<int>() }
            , result);
    }

    [Test]
    public void OutputTypeIsStruct_returnsFunnyStruct() {
        var str = Funny.Calc(
            "{name = 'alaska'}");
        Assert.IsInstanceOf<IReadOnlyDictionary<string, object>>(str);
        var rs = str as IReadOnlyDictionary<string, object>;
        Assert.AreEqual(1, rs.Count);
        Assert.AreEqual("alaska", rs["name"]);
    }

    [Test]
    public void OutputTypeIsDictionary_returnsStruct() {
        var dic = Funny.Calc("{a = 1, b = 'test'}") as IReadOnlyDictionary<string, object>;
        Assert.IsNotNull(dic);
        Assert.AreEqual(dic["a"], 1);
        Assert.AreEqual(dic["b"], "test");
    }

    [Test]
    public void OutputTypeIsDictionary_returnsEmptyStruct() {
        var dic = Funny.Calc("{}") as IReadOnlyDictionary<string, object>;
        Assert.IsNotNull(dic);
        Assert.AreEqual(0, dic.Count);
    }

    [TestCase("")]
    [TestCase("x:int = 2")]
    [TestCase("a = 12; b = 32; x = a*b")]
    public void NoOutputSpecified_throws(string expr)
        => Assert.Throws<FunnyParseException>(() => Funny.Calc(expr));

    [TestCase("{id = age; items = [1,2,3,4].map(rule '{it}'); price = 21*2}")]
    [TestCase("[1..4].filter(rule it>age).map(rule it**2)")]
    [TestCase("age>someUnknownvariable")]
    [TestCase("x:int;")]
    public void UseUnknownInput_throws(string expression) =>
        Assert.Throws<FunnyParseException>(() => Funny.Calc(expression));

    [Test]
    public void ConstOfDecimalTest() {
        var result = Funny.WithDialect(realClrType: RealClrType.IsDecimal)
            .Calc("13.5");
        Assert.AreEqual(result, (decimal)13.5);
    }

    [Test]
    public void ConstOfDecimalStructsTest() {
        var result = Funny.WithDialect(realClrType: RealClrType.IsDecimal)
            .Calc("{name = 'test', price = 13.5}");
        Assert.IsInstanceOf<Dictionary<string, object>>(result);
        var dic = (Dictionary<string, object>)result;
        Assert.AreEqual(dic["name"], "test");
        Assert.AreEqual(dic["price"], (decimal)13.5);
    }
}
