using System;
using System.Linq;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters {

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
        var converter = FunnyTypeConverters.GetOutputConverter(clrType);
        Assert.AreEqual(expectedTypeName, converter.FunnyType.BaseType);
        var convertedValue = converter.ToClrObject(primitiveValue);
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
        var converter = FunnyTypeConverters.GetOutputConverter(clrType);

        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.PrimitiveOf(expectedTypeName)), converter.FunnyType);
        var convertedValue = converter.ToClrObject(
            new ImmutableFunnyArray(primitiveValue as Array, FunnyType.PrimitiveOf(expectedTypeName)));
        Assert.AreEqual(primitiveValue, convertedValue);
    }

    [TestCase("")]
    [TestCase("v")]
    [TestCase("value")]
    public void ConvertString(string value) {
        var converter = FunnyTypeConverters.GetOutputConverter(typeof(string));
        Assert.AreEqual(FunnyType.Text, converter.FunnyType);

        Assert.AreEqual(value, converter.ToClrObject(new TextFunnyArray(value)));
    }

    [Test]
    public void ConvertArrayOfStrings() {
        string[] clrValue = { "vasa", "kata", "" };

        var converter = FunnyTypeConverters.GetOutputConverter(clrValue.GetType());

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
        var converter = FunnyTypeConverters.GetOutputConverter(inputValue.GetType());
        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Any)), converter.FunnyType);
    }

    [Test]
    public void StructType() {
        var inputUser = new UserMoqOutputType("vasa", 42, 17.1);
        var converter = FunnyTypeConverters.GetOutputConverter(inputUser.GetType());
        Assert.AreEqual(
            FunnyType.StructOf(
                ("name", FunnyType.Text),
                ("age", FunnyType.Int32),
                ("size", FunnyType.Real)), converter.FunnyType);
    }

    [Test]
    public void ArrayOfStructTypesWithoutNewContructor() {
        var inputUsers = new[] {
            new UserMoqType("vasa", 42, 17.1),
            new UserMoqType("peta", 41, 17.0),
            new UserMoqType("kata", 40, -17.1)
        };
        Assert.Catch(() => FunnyTypeConverters.GetOutputConverter(inputUsers.GetType()));
    }

    [Test]
    public void ArrayOfStructTypes() {
        var inputUsers = new[] {
            new UserMoqOutputType("vasa", 42, 17.1),
            new UserMoqOutputType("peta", 41, 17.0),
            new UserMoqOutputType("kata", 40, -17.1)
        };

        var converter = FunnyTypeConverters.GetOutputConverter(inputUsers.GetType());
        Assert.AreEqual(
            FunnyType.ArrayOf(
                FunnyType.StructOf(
                    ("name", FunnyType.Text),
                    ("age", FunnyType.Int32),
                    ("size", FunnyType.Real))), converter.FunnyType);
    }

    [Test]
    public void RequrisiveType_Throws()
        => Assert.Catch(() => FunnyTypeConverters.GetOutputConverter(typeof(NodeMoqRecursiveOutputType)));
}

class NodeMoqRecursiveOutputType {
    public string Name { get; set; }
    public NodeMoqRecursiveOutputType[] Children { get; set; }
}

class UserMoqOutputType {
    public UserMoqOutputType(string name, int age, double size) {
        Name = name;
        Age = age;
        Size = size;
    }

    public UserMoqOutputType() { }
    public string Name { get; set; }
    public int Age { get; set; }
    public double Size { get; set; }
    public bool State { get; }
}

}