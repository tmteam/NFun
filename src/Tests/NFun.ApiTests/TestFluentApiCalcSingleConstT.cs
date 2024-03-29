using System;
using System.Net;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ApiTests;

public class TestFluentApiCalcSingleConstT {
    [TestCase("(13 == 13) and ('vasa' == 'vasa')", true)]
    [TestCase("1<2<13>-100>-150 != 1<4<13>-100>-150", false)]
    [TestCase("(1<2<13>-100>-150) == (1<2<13>-100>-150) == true", true)]
    public void ReturnsBool(string expr, bool expected) {
        var result = Funny.Calc<bool>(expr);
        Assert.AreEqual(expected, result);
    }

    [TestCase("{id = 13; items = [1,2,3,4].map(rule '{it}'); price = 21*2; taxes = 0}")]
    // Todo: Ignore cases in structs: [TestCase("{Id = 13; Items = [1,2,3,4].map(rule '{it}'); Price = 21*2}")]
    public void IoComplexTypeTransforms(string expr) {
        var result = Funny.Calc<ContractOutputModel>(expr);
        var expected = new ContractOutputModel {
            Id = 13, Items = new[] { "1", "2", "3", "4" }, Taxes = Decimal.Zero, Price = 42
        };
        FunnyAssert.AreSame(expected, result);
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

    [Test]
    public void ComplexOutputType() {
        var res = Funny.Calc<SuperPuperComplexModel[][][]>(
            @"
[[[
    {
        y = [[
                    {
                        x = [
                            {a = {id = 1}, b = {id = 2}},
                            {a = {id = 3}, b = {id = 4}}
                        ],
                        y = {a = {id = 5}, b = {id = 6}}
                    },
                    {
                        x = [
                            {a = {id = 7}, b = {id = 8}},
                            {a = {id = 9}, b = {id = 10}}
                        ],
                        y = {a = {id = 11}, b = {id = 12}}
                    }
                ],
                []
        ]
        x= [
            { x = default, y = {a = {id = 13}, b = {id = 14}} },
            { x = [{a = {id = 15}, b = {id = 16}}], y = default }
        ]
        }]]]");
        FunnyAssert.AreSame(expected:
            new[] {
                new[] {
                    new[] {
                        new SuperPuperComplexModel {
                            y = new[] {
                                new[] {
                                    new SuperComplexModel {
                                        x = new[] {
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 1 }, b = new ModelWithInt { id = 2 }
                                            },
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 3 }, b = new ModelWithInt { id = 4 }
                                            }
                                        },
                                        y = new ComplexModel {
                                            a = new ModelWithInt { id = 5 }, b = new ModelWithInt { id = 6 }
                                        }
                                    },
                                    new SuperComplexModel {
                                        x = new[] {
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 7 }, b = new ModelWithInt { id = 8 }
                                            },
                                            new ComplexModel {
                                                a = new ModelWithInt { id = 9 }, b = new ModelWithInt { id = 10 }
                                            }
                                        },
                                        y = new ComplexModel {
                                            a = new ModelWithInt { id = 11 }, b = new ModelWithInt { id = 12 }
                                        }
                                    }
                                },
                                new SuperComplexModel[0]
                            },
                            x = new[] {
                                new SuperComplexModel {
                                    x = new ComplexModel[0],
                                    y = new ComplexModel {
                                        a = new ModelWithInt { id = 13 }, b = new ModelWithInt { id = 14 }
                                    },
                                },
                                new SuperComplexModel {
                                    x = new[] {
                                        new ComplexModel {
                                            a = new ModelWithInt { id = 15 }, b = new ModelWithInt { id = 16 }
                                        }
                                    },
                                    y = new ComplexModel {
                                        a = new ModelWithInt { id = 0 }, b = new ModelWithInt { id = 0 }
                                    }
                                }
                            },
                        }
                    }
                }
            },
            res
            );
    }

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
        => FunnyAssert.ObviousFailsOnApiUsage(() => Funny.Calc<decimal>("123"));

    [Test]
    public void UseDecimalWithBuilderWithoutDialect_throws()
        => FunnyAssert.ObviousFailsOnApiUsage(() => Funny.WithConstant("id", 42).Calc<decimal>("123"));
}
