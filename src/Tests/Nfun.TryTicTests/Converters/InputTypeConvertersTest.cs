using System;
using System.Linq;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests.Converters {

public class InputTypeConvertersTest {
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
        var converter = FunnyTypeConverters.GetInputConverter(clrType);
        Assert.AreEqual(expectedTypeName, converter.FunnyType.BaseType);
        var convertedValue = converter.ToFunObject(primitiveValue);
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
        var converter = FunnyTypeConverters.GetInputConverter(clrType);

        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.PrimitiveOf(expectedTypeName)), converter.FunnyType);
        var convertedValue = converter.ToFunObject(primitiveValue);
        Assert.AreEqual(primitiveValue, convertedValue);
    }

    [TestCase("")]
    [TestCase("v")]
    [TestCase("value")]
    public void ConvertString(string value) {
        var converter = FunnyTypeConverters.GetInputConverter(typeof(string));

        Assert.AreEqual(FunnyType.Text, converter.FunnyType);
        Assert.AreEqual(new TextFunnyArray(value), converter.ToFunObject(value));
    }

    [Test]
    public void ConvertArrayOfStrings() {
        string[] inputValue = {
            "vasa", "kata", ""
        };

        var converter = FunnyTypeConverters.GetInputConverter(inputValue.GetType());

        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.Text), converter.FunnyType);
        Assert.AreEqual(
            new ImmutableFunnyArray(
                inputValue.Select(i => new TextFunnyArray(i)).ToArray(), FunnyType.Text),
            converter.ToFunObject(inputValue));
    }

    [Test]
    public void ArrayOfArrayOfAnything() {
        object[][] inputValue = {
            new object[] { 1, 2, "kate" },
            new object[] { 2, 1, "kate" },
            new object[] { }
        };
        var converter = FunnyTypeConverters.GetInputConverter(inputValue.GetType());
        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.Any)), converter.FunnyType);
        var value = converter.ToFunObject(inputValue);
        Assert.IsInstanceOf<ImmutableFunnyArray>(value);
        Assert.AreEqual(3, ((ImmutableFunnyArray)value).Count);
    }

    [Test]
    public void StructType() {
        var inputUser = new UserMoqType("vasa", 42, 17.1);
        var converter = FunnyTypeConverters.GetInputConverter(inputUser.GetType());
        Assert.AreEqual(
            FunnyType.StructOf(
                ("name", FunnyType.Text),
                ("age", FunnyType.Int32),
                ("size", FunnyType.Real)), converter.FunnyType);
        var value = converter.ToFunObject(inputUser);
        Assert.IsInstanceOf<FunnyStruct>(value);
        var converted = (FunnyStruct)value;
        Assert.AreEqual(inputUser.Name, (converted.GetValue("name") as TextFunnyArray).ToText());
        Assert.AreEqual(inputUser.Age, converted.GetValue("age"));
        Assert.AreEqual(inputUser.Size, converted.GetValue("size"));
    }

    [Test]
    public void ArrayOfStructTypes() {
        var inputUsers = new[] {
            new UserMoqType("vasa", 42, 17.1),
            new UserMoqType("peta", 41, 17.0),
            new UserMoqType("kata", 40, -17.1)
        };

        var converter = FunnyTypeConverters.GetInputConverter(inputUsers.GetType());
        Assert.AreEqual(
            FunnyType.ArrayOf(
                FunnyType.StructOf(
                    ("name", FunnyType.Text),
                    ("age", FunnyType.Int32),
                    ("size", FunnyType.Real))), converter.FunnyType);
        var value = converter.ToFunObject(inputUsers);
        Assert.IsInstanceOf<ImmutableFunnyArray>(value);
        var secondElememt = ((ImmutableFunnyArray)value).GetElementOrNull(1) as FunnyStruct;
        Assert.IsNotNull(
            secondElememt,
            ((ImmutableFunnyArray)value).GetElementOrNull(1).GetType().Name + " is wrong type");
        Assert.AreEqual(inputUsers[1].Name, (secondElememt.GetValue("name") as TextFunnyArray).ToText());
        Assert.AreEqual(inputUsers[1].Age, secondElememt.GetValue("age"));
        Assert.AreEqual(inputUsers[1].Size, secondElememt.GetValue("size"));
    }

    [Test]
    public void RequrisiveType_Throws() {
        NodeMoqType obj = new NodeMoqType("vasa", new NodeMoqType("peta"));
        Assert.Throws<ArgumentException>(() => FunnyTypeConverters.GetInputConverter(obj.GetType()));
    }
}

class NodeMoqType {
    public NodeMoqType(string name, params NodeMoqType[] children) {
        Name = name;
        Children = children;
    }

    public string Name { get; }
    public NodeMoqType[] Children { get; }
}

class UserMoqType {
    public UserMoqType(string name, int age, double size) {
        Name = name;
        Age = age;
        Size = size;
    }

    public string Name { get; }
    public int Age { get; }
    public double Size { get; }
    public bool State { set; private get; }
}

}