using System;
using System.Collections.Generic;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters;

public class MockyType {
    public int Age { get; set; }
    public Dictionary<string, object> Nested { get; set; }
}

public class ConvertInputTest {
    [TestCase(42, BaseFunnyType.Int32, 42)]
    [TestCase(42, BaseFunnyType.Int64, (long)42)]
    [TestCase(42.0, BaseFunnyType.Int32, 42)]
    [TestCase(42.0, BaseFunnyType.Real, 42.0)]
    [TestCase((float)0.5, BaseFunnyType.Real, 0.5)]
    [TestCase((byte)42, BaseFunnyType.Real, 42.0)]
    public void ConvertInputOrThrow_PrimitivesTest(object input, BaseFunnyType target, object expected)
        => Assert.AreEqual(expected,
            FunnyConverter.RealIsDouble.ConvertInputOrThrow(input, FunnyType.PrimitiveOf(target)));

    [Test]
    public void ConvertInputDecimalOrThrow_PrimitivesTest()
        => Assert.AreEqual(-42.5, FunnyConverter.RealIsDouble.ConvertInputOrThrow(new Decimal(-42.5), FunnyType.Real));

    [Test]
    public void ConvertComplexInputStructFromDictionariesAndTypes1() {
        var type = FunnyType.StructOf(
            ("age", FunnyType.Int32),
            ("nested", FunnyType.StructOf(
                ("end", FunnyType.Bool))));
        var input = new MockyType { Age = 31, Nested = new Dictionary<string, object> { { "end", true } } };
        var mocky = FunnyConverter.RealIsDouble.ConvertInputOrThrow(input, type) as FunnyStruct;
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
                ("arr", FunnyType.ArrayOf(
                    FunnyType.StructOf(
                        ("age", FunnyType.Int32),
                        ("nested", FunnyType.StructOf(
                            ("end", FunnyType.Bool))))
                )))));
        var input = new Dictionary<string, object> {
            { "i", 42 }, {
                "str",
                new Dictionary<string, object> {
                    {
                        "arr",
                        new[] {
                            new MockyType { Age = 31, Nested = new Dictionary<string, object> { { "end", true } } }
                        }
                    }
                }
            }
        };
        var converted = FunnyConverter.RealIsDouble.ConvertInputOrThrow(input, type) as FunnyStruct;
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
        var result =
            FunnyConverter.RealIsDouble.ConvertInputOrThrow(new { age = 42 },
                FunnyType.StructOf(("age", FunnyType.Any)));
        Assert.IsInstanceOf<FunnyStruct>(result);
    }

    [Test]
    public void NestedConvertionDoesNotThrow() {
        var result = FunnyConverter.RealIsDouble.ConvertInputOrThrow(new { age = 42, size = 1.1, name = "vasa" },
            FunnyType.StructOf(
                ("age", FunnyType.Int32),
                ("size", FunnyType.Real),
                ("name", FunnyType.Any)
            ));
        Assert.IsInstanceOf<FunnyStruct>(result);
    }

    [Test]
    public void PrimitiveTypeDoesNotCreateItemsInCache() {
        var converter = FunnyConverter.RealIsDouble;
        converter.ClearCaches();
        Assert.AreEqual(0, converter.CacheSize);
        converter.GetInputConverterFor(typeof(bool));

        converter.GetInputConverterFor(typeof(short));
        converter.GetInputConverterFor(typeof(int));
        converter.GetInputConverterFor(typeof(long));

        converter.GetInputConverterFor(typeof(ushort));
        converter.GetInputConverterFor(typeof(uint));
        converter.GetInputConverterFor(typeof(ulong));

        converter.GetInputConverterFor(typeof(float));
        converter.GetInputConverterFor(typeof(double));

        Assert.AreEqual(0, converter.CacheSize);
    }

    [Test]
    public void GetStructTypeCreatesSingleItemInCache() {
        var converter = FunnyConverter.RealIsDouble;
        converter.ClearCaches();
        Assert.AreEqual(0, converter.CacheSize);
        converter.GetInputConverterFor(typeof(UserInputModel));
        var size = converter.CacheSize;
        converter.GetInputConverterFor(typeof(UserInputModel));
        converter.GetInputConverterFor(typeof(UserInputModel));
        Assert.AreEqual(size, converter.CacheSize);
    }

    [Test]
    public void GetIntArrayTypeCreatesSingleItemInCache() {
        var converter = FunnyConverter.RealIsDouble;
        converter.ClearCaches();
        Assert.AreEqual(0, converter.CacheSize);
        converter.GetInputConverterFor(typeof(int[]));
        var size = converter.CacheSize;
        converter.GetInputConverterFor(typeof(int[]));
        converter.GetInputConverterFor(typeof(int[]));
        Assert.AreEqual(size, converter.CacheSize);
    }
}
