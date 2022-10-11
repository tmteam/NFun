using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters; 

public class OutputTypeConvertersTest {
    [TestCase((byte)1, BaseFunnyType.UInt8)]
    [TestCase((ushort)2, BaseFunnyType.UInt16)]
    [TestCase((uint)3, BaseFunnyType.UInt32)]
    [TestCase((ulong)4, BaseFunnyType.UInt64)]
    [TestCase((Int16)1, BaseFunnyType.Int16)]
    [TestCase((int)2, BaseFunnyType.Int32)]
    [TestCase((long)3, BaseFunnyType.Int64)]
    [TestCase((double)-15.1, BaseFunnyType.Real)]
    [TestCase(true, BaseFunnyType.Bool)]
    public void ConvertPrimitiveType(object primitiveValue, BaseFunnyType expectedTypeName) {
        var clrType = primitiveValue.GetType();
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(clrType);
        Assert.AreEqual(expectedTypeName, converter.FunnyType.BaseType);
        var convertedValue = converter.ToClrObject(primitiveValue);
        Assert.AreEqual(primitiveValue, convertedValue);
    }

    [TestCase(0.0)]
    [TestCase(1.0)]
    [TestCase(1.5)]
    [TestCase(-0.5)]
    public void ConvertFloatType(double origin) => ConvertRealTypes((float)origin, origin);
    
    [TestCase(0.0)]
    [TestCase(1.0)]
    [TestCase(1.5)]
    [TestCase(-0.5)]
    public void ConvertDecimalType(double origin) => ConvertRealTypes(new Decimal(origin), origin);

    private void ConvertRealTypes(object primitiveValue, double origin) {
        var clrType = primitiveValue.GetType();
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(clrType);
        Assert.AreEqual(FunnyType.Real, converter.FunnyType);
        var convertedValue = converter.ToClrObject(origin);
        Assert.AreEqual(primitiveValue, convertedValue);
    }
    
    [TestCase(new byte[] { 1, 2, 3 }, BaseFunnyType.UInt8)]
    [TestCase(new UInt16[] { 1, 2, 3 }, BaseFunnyType.UInt16)]
    [TestCase(new UInt32[] { 1, 2, 3 }, BaseFunnyType.UInt32)]
    [TestCase(new UInt64[] { 1, 2, 3 }, BaseFunnyType.UInt64)]
    [TestCase(new Int16[] { 1, 2, 3 }, BaseFunnyType.Int16)]
    [TestCase(new Int32[] { 1, 2, 3 }, BaseFunnyType.Int32)]
    [TestCase(new Int64[] { 1, 2, 3 }, BaseFunnyType.Int64)]
    [TestCase(new Double[] { 1, 2, 3 }, BaseFunnyType.Real)]
    [TestCase(new[] { true, false }, BaseFunnyType.Bool)]
    public void ConvertPrimitiveTypeArrays(object primitiveValue, BaseFunnyType expectedTypeName) {
        var clrType = primitiveValue.GetType();
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(clrType);

        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.PrimitiveOf(expectedTypeName)), converter.FunnyType);
        var convertedValue = converter.ToClrObject(
            new ImmutableFunnyArray(primitiveValue as Array, FunnyType.PrimitiveOf(expectedTypeName)));
        Assert.AreEqual(primitiveValue, convertedValue);
    }

    [TestCase("")]
    [TestCase("v")]
    [TestCase("value")]
    public void ConvertString(string value) {
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(typeof(string));
        Assert.AreEqual(FunnyType.Text, converter.FunnyType);

        Assert.AreEqual(value, converter.ToClrObject(new TextFunnyArray(value)));
    }

    [Test]
    public void ConvertArrayOfStrings() {
        string[] clrValue = { "vasa", "kata", "" };

        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(clrValue.GetType());

        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.Text), converter.FunnyType);
        var funValue = new ImmutableFunnyArray(
            clrValue.Select(i => new TextFunnyArray(i)).ToArray(), FunnyType.Text);

        Assert.AreEqual(clrValue, converter.ToClrObject(funValue));
    }

    [Test]
    public void ArrayOfArrayOfAnything() {
        object[][] inputValue = {
            new object[] { 1, 2, "kate" },
            new object[] { 2, 1, "kate" },
            new object[] { }
        };
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(inputValue.GetType());
        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Any)), converter.FunnyType);
    }

    [Test]
    public void StructType() {
        var inputUser = new UserMoqOutputType("vasa", 42, 17.1, Decimal.Zero);
        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(inputUser.GetType());
        Assert.AreEqual(
            FunnyType.StructOf(
                ("name", FunnyType.Text),
                ("age", FunnyType.Int32),
                ("size", FunnyType.Real),
                ("balance", FunnyType.Real)), converter.FunnyType);
    }

    [Test]
    public void ArrayOfStructTypesWithoutNewContructor() {
        var inputUsers = new[] {
            new UserMoqType("vasa", 42, 17.1,  new Decimal(31.1)),
            new UserMoqType("peta", 41, 17.0,  new Decimal(31)),
            new UserMoqType("kata", 40, -17.1, new Decimal(0))
        };
        Assert.Catch(() => FunnyConverter.RealIsDouble.GetOutputConverterFor(inputUsers.GetType()));
    }

    [Test]
    public void ArrayOfStructTypes() {
        var inputUsers = new[] {
            new UserMoqOutputType("vasa", 42, 17.1, Decimal.One),
            new UserMoqOutputType("peta", 41, 17.0, Decimal.Zero),
            new UserMoqOutputType("kata", 40, -17.1, new decimal(42.2))
        };

        var converter = FunnyConverter.RealIsDouble.GetOutputConverterFor(inputUsers.GetType());
        Assert.AreEqual(
            FunnyType.ArrayOf(
                FunnyType.StructOf(
                    ("name", FunnyType.Text),
                    ("age", FunnyType.Int32),
                    ("size", FunnyType.Real),
                    ("balance", FunnyType.Real)
                )), converter.FunnyType);
    }

    [Test]
    public void RequrisiveType_Throws()
        => Assert.Catch(() => FunnyConverter.RealIsDouble.GetOutputConverterFor(typeof(NodeMoqRecursiveOutputType)));

    [Test]    
    public void PrimitiveTypeCreateNoItemInCache()
    {
        var converter = FunnyConverter.RealIsDouble;
        converter.ClearCaches();
        Assert.AreEqual(0, converter.CacheSize);
        for (int i = 0; i < 10; i++)
        {
            converter.GetOutputConverterFor(FunnyType.Any);
            converter.GetOutputConverterFor(FunnyType.Bool);
            converter.GetOutputConverterFor(FunnyType.Char);
            converter.GetOutputConverterFor(FunnyType.Int16);
            converter.GetOutputConverterFor(FunnyType.Int32);
            converter.GetOutputConverterFor(FunnyType.Int64);
            converter.GetOutputConverterFor(FunnyType.Real);
            converter.GetOutputConverterFor(FunnyType.UInt8);
            converter.GetOutputConverterFor(FunnyType.UInt16);
            converter.GetOutputConverterFor(FunnyType.UInt32);
            converter.GetOutputConverterFor(FunnyType.UInt64);
        }

        Assert.AreEqual(0, converter.CacheSize);
    }

    [Test]
    public void Text_TypeCreatedOnce() => AssertCreatesItemInCacheOnce(FunnyType.Text);
    
    [Test]
    public void Array_1_TypeCreatedOnce() => AssertCreatesItemInCacheOnce(FunnyType.ArrayOf(FunnyType.Any));
    
    [Test]
    public void Array_2_TypeCreatedOnce() => AssertCreatesItemInCacheOnce(FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Any)));
    
    [Test]
    public void Array_3_TypeCreatedOnce() => AssertCreatesItemInCacheOnce(FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Text)));
    
    [Test]
    public void Struct_1_TypeCreatedOnce() => AssertCreatesItemInCacheOnce(
        FunnyType.StructOf(
            ("a", FunnyType.Int16), 
            ("b", FunnyType.FunOf(FunnyType.Int32, FunnyType.FunOf(FunnyType.Int16, FunnyType.Bool), FunnyType.ArrayOf(FunnyType.Text)))));
    
    [Test]
    public void Struct_2_TypeCreatedOnce() => AssertCreatesItemInCacheOnce(
        FunnyType.StructOf(
            ("a", FunnyType.StructOf(("c", FunnyType.Bool))),
            ("b", FunnyType.StructOf(("c", FunnyType.Bool))),
            ("c", FunnyType.FunOf(FunnyType.Int32, FunnyType.FunOf(FunnyType.Int16, FunnyType.Bool), FunnyType.ArrayOf(FunnyType.Text)))));
    

    private void AssertCreatesItemInCacheOnce(FunnyType type)
    {
        var converter = FunnyConverter.RealIsDouble;
        converter.ClearCaches();
        Assert.AreEqual(0, converter.CacheSize);
        converter.GetOutputConverterFor(type);
        var size = converter.CacheSize;
        for (int i = 0; i < 10; i++)
        {
            converter.GetOutputConverterFor(type);
        }
        Assert.AreEqual(size, converter.CacheSize);
    }
}

class NodeMoqRecursiveOutputType {
    // ReSharper disable once UnusedMember.Global
    public string Name { get; set; }
    // ReSharper disable once UnusedMember.Global
    public NodeMoqRecursiveOutputType[] Children { get; set; }
}

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
class UserMoqOutputType {
    public UserMoqOutputType(string name, int age, double size, Decimal balance) {
        Name = name;
        Age = age;
        Size = size;
        Balance = balance;
    }

    public UserMoqOutputType() { }
    public string Name { get; set; }
    public int Age { get; set; }
    public double Size { get; set; }
    public decimal Balance { get; set; }
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    // ReSharper disable once UnusedMember.Global
    public bool State { get; }
}