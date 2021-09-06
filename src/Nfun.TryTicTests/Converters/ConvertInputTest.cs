using System.Collections.Generic;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters {

public class MockyType {
    public int Age { get; set; }
    public Dictionary<string, object> Nested { get; set; }
}

public class ConvertInputTest {
    [TestCase(42, BaseFunnyType.Int32, 42)]
    [TestCase(42, BaseFunnyType.Int64, (long)42)]
    [TestCase(42.0, BaseFunnyType.Int32, 42)]
    [TestCase(42.0, BaseFunnyType.Real, 42.0)]
    [TestCase((byte)42, BaseFunnyType.Real, 42.0)]
    public void ConvertInputOrThrow_PrimitivesTest(object input, BaseFunnyType target, object expected)
        => Assert.AreEqual(expected, FunnyTypeConverters.ConvertInputOrThrow(input, FunnyType.PrimitiveOf(target)));

    [Test]
    public void ConvertComplexInputStructFromDictionariesAndTypes1() {
        var type = FunnyType.StructOf(
            ("age", FunnyType.Int32),
            ("nested", FunnyType.StructOf(
                "end", FunnyType.Bool)));
        var input = new MockyType {
            Age = 31,
            Nested = new Dictionary<string, object> {
                { "end", true }
            }
        };
        var mocky = FunnyTypeConverters.ConvertInputOrThrow(input, type) as FunnyStruct;
        Assert.AreEqual(31, mocky["age"]);
        var endstr = mocky["nested"] as FunnyStruct;
        Assert.AreEqual(true, endstr["end"]);
    }

    [Test]
    public void ConvertComplexInputStructFromDictionariesAndTypes2() {
        /*
        {
            i:i32
            str:{
                arr:  [MockyType{
                    age:int
                    Nested:{
                        end:bool
                    }
                }] 
            }
        */
        var type = FunnyType.StructOf(
            ("i", FunnyType.Int32),
            ("str", FunnyType.StructOf(
                "arr", FunnyType.ArrayOf(
                    FunnyType.StructOf(
                        ("age", FunnyType.Int32),
                        ("nested", FunnyType.StructOf(
                            "end", FunnyType.Bool)))
                ))));
        var input = new Dictionary<string, object> {
            { "i", 42 }, {
                "str", new Dictionary<string, object> {
                    {
                        "arr", new[] {
                            new MockyType {
                                Age = 31,
                                Nested = new Dictionary<string, object> {
                                    { "end", true }
                                }
                            }
                        }
                    }
                }
            }
        };
        var converted = FunnyTypeConverters.ConvertInputOrThrow(input, type) as FunnyStruct;
        Assert.IsNotNull(converted);
        Assert.AreEqual(42, converted["i"]);
        var str = converted["str"] as FunnyStruct;
        var arr = str["arr"] as IFunnyArray;
        var mocky = arr.GetElementOrNull(0) as FunnyStruct;
        Assert.AreEqual(31, mocky["age"]);
        var endstr = mocky["nested"] as FunnyStruct;
        Assert.AreEqual(true, endstr["end"]);
    }


    [Test]
    public void NestedConvertionDoesNotThrow2() {
        var result = FunnyTypeConverters.ConvertInputOrThrow(
            new { age = 42 },
            FunnyType.StructOf(("age", FunnyType.Any)));
        Assert.IsInstanceOf<FunnyStruct>(result);
    }

    [Test]
    public void NestedConvertionDoesNotThrow() {
        var result = FunnyTypeConverters.ConvertInputOrThrow(new {
            age = 42,
            size = 1.1,
            name = "vasa"
        }, FunnyType.StructOf(
            ("age", FunnyType.Int32),
            ("size", FunnyType.Real),
            ("name", FunnyType.Any)
        ));
        Assert.IsInstanceOf<FunnyStruct>(result);
    }
}

}