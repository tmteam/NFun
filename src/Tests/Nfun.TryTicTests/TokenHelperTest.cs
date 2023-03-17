using NFun.TestTools;
using NFun.Tokenization;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests;

[TestFixture]
public class TokenHelperTest {
    [TestCase("1", 1, BaseFunnyType.Int32)]
    [TestCase("0xFF", 0xFF, BaseFunnyType.Int32)]
    [TestCase("0.1", 0.1, BaseFunnyType.Real)]
    [TestCase("0xFEDCBA987654321", 0xFEDCBA987654321, BaseFunnyType.Int64)]
    [TestCase("0xFEDCBA", 0xFEDCBA, BaseFunnyType.Int32)]
    [TestCase("9876543210", 9876543210, BaseFunnyType.Int64)]
    [TestCase("-9876543210", -9876543210, BaseFunnyType.Int64)]
    [TestCase("2147483647", 2147483647, BaseFunnyType.Int32)]
    [TestCase("-2147483648", -2147483648, BaseFunnyType.Int32)]
    [TestCase("0b1111_1111_1111_1111_1111_1111_1111", 0b1111_1111_1111_1111_1111_1111_1111, BaseFunnyType.Int32)]
    public void ToConstant_NumberConstant_ParsesWell(string value, object expectedVal, BaseFunnyType expectedType) {
        var (obj, type) = TokenHelper.ToConstant(value);
        Assert.AreEqual(expectedVal, obj);
        Assert.AreEqual(expectedType, type.BaseType);
    }

    [TestCase("false")]
    [TestCase("hi bro")]
    [TestCase("0a123")]
    [TestCase("0xFEDCBA9876543210")]
    [TestCase("0xFEDCBA9876543211")]
    [TestCase("0xFEDCBA9876543211_FEDCBA9876543211")]
    [TestCase("0b1111_1111_1111_1111_1111_1111_1111_0011_100101_10101_10_10101001010100101010")]
    [TestCase("-0xFEDCBA987654")]
    [TestCase("-0xFF")]
    [TestCase("0b000121")]
    [TestCase("0.000.121")]
    [TestCase("-0b000101")]
    [TestCase("")]
    public void ToConstant_SomeCrap_ThrowsFormatException(string value) =>
        Assert.Catch(() => TokenHelper.ToConstant(value));

    [TestCase("int", BaseFunnyType.Int32)]
    [TestCase("int;[]", BaseFunnyType.Int32)]
    [TestCase("int;[][]", BaseFunnyType.Int32)]
    [TestCase("int16", BaseFunnyType.Int16)]
    [TestCase("int32", BaseFunnyType.Int32)]
    [TestCase("int64", BaseFunnyType.Int64)]
    [TestCase("uint", BaseFunnyType.UInt32)]
    [TestCase("byte", BaseFunnyType.UInt8)]
    [TestCase("int=", BaseFunnyType.Int32)]
    [TestCase("int16=", BaseFunnyType.Int16)]
    [TestCase("int32=", BaseFunnyType.Int32)]
    [TestCase("int64=", BaseFunnyType.Int64)]
    [TestCase("uint=", BaseFunnyType.UInt32)]
    [TestCase("byte=", BaseFunnyType.UInt8)]
    [TestCase("uint16=", BaseFunnyType.UInt16)]
    [TestCase("uint32;", BaseFunnyType.UInt32)]
    [TestCase("uint64;(", BaseFunnyType.UInt64)]
    [TestCase("uint64;a", BaseFunnyType.UInt64)]
    [TestCase("real", BaseFunnyType.Real)]
    [TestCase("real:", BaseFunnyType.Real)]
    [TestCase("bool", BaseFunnyType.Bool)]
    [TestCase("any", BaseFunnyType.Any)]
    public void ReadType_PrimitiveTypes(string expr, BaseFunnyType expected) =>
        AssertFunnyType(expr, FunnyType.PrimitiveOf(expected));

    [TestCase("int10")]
    [TestCase("uint10")]
    [TestCase("uint9")]
    [TestCase("uint1")]
    [TestCase("real32")]
    [TestCase("asdasd")]
    [TestCase("a")]
    [TestCase("")]
    [TestCase("[[")]
    [TestCase("|")]
    [TestCase("*")]
    [TestCase("rule")]
    [TestCase("rule(?):?")]
    [TestCase("fon")]
    [TestCase("{}")]
    [TestCase("bool8")]
    [TestCase("boolean")]
    [TestCase("int[[;")]
    [TestCase("int[;]")]
    [TestCase("int[] []")]
    [TestCase("int  []")]
    [TestCase("anything")]
    [TestCase("default")]
    [TestCase("t")]
    public void ReadType_Throws(string expr) {
        var flow = Tokenizer.ToFlow(expr);
        Assert.Catch(() => flow.ReadType());
    }

    [TestCase("int8")]
    [TestCase("async")]
    public void ReservedWord_Throw(string expr) => FunnyAssert.ObviousFailsOnParse(() => Tokenizer.ToFlow(expr));

    [TestCase("text")]
    [TestCase("text=")]
    [TestCase("text:")]
    public void ReadTextType(string expr) => AssertFunnyType(expr, FunnyType.Text);

    [TestCase("int[]", BaseFunnyType.Int32)]
    [TestCase("int[]\r", BaseFunnyType.Int32)]
    [TestCase("int[]\r[]", BaseFunnyType.Int32)]
    [TestCase("int[];[]", BaseFunnyType.Int32)]
    [TestCase("int16[]", BaseFunnyType.Int16)]
    [TestCase("int32[]", BaseFunnyType.Int32)]
    [TestCase("int64[]", BaseFunnyType.Int64)]
    [TestCase("uint[]", BaseFunnyType.UInt32)]
    [TestCase("byte[]", BaseFunnyType.UInt8)]
    [TestCase("int[]=", BaseFunnyType.Int32)]
    [TestCase("int16[]=", BaseFunnyType.Int16)]
    [TestCase("int32[]=", BaseFunnyType.Int32)]
    [TestCase("int64[]=", BaseFunnyType.Int64)]
    [TestCase("uint[]=", BaseFunnyType.UInt32)]
    [TestCase("byte[]=", BaseFunnyType.UInt8)]
    [TestCase("uint16[]=", BaseFunnyType.UInt16)]
    [TestCase("uint32[];", BaseFunnyType.UInt32)]
    [TestCase("uint64[];(", BaseFunnyType.UInt64)]
    [TestCase("uint64[];a", BaseFunnyType.UInt64)]
    [TestCase("real[]", BaseFunnyType.Real)]
    [TestCase("real[]:", BaseFunnyType.Real)]
    [TestCase("bool[]", BaseFunnyType.Bool)]
    [TestCase("any[]", BaseFunnyType.Any)]
    public void ReadArrayType(string expr, BaseFunnyType elementType) =>
        AssertFunnyType(expr, FunnyType.ArrayOf(FunnyType.PrimitiveOf(elementType)));

    [TestCase("int[][]", BaseFunnyType.Int32)]
    [TestCase("int16[][]", BaseFunnyType.Int16)]
    [TestCase("int32[][]", BaseFunnyType.Int32)]
    [TestCase("int64[][]", BaseFunnyType.Int64)]
    [TestCase("uint[][]", BaseFunnyType.UInt32)]
    [TestCase("byte[][]", BaseFunnyType.UInt8)]
    [TestCase("int[][]=", BaseFunnyType.Int32)]
    [TestCase("int16[][]=", BaseFunnyType.Int16)]
    [TestCase("int32[][]=", BaseFunnyType.Int32)]
    [TestCase("int64[][]=", BaseFunnyType.Int64)]
    [TestCase("uint[][]=", BaseFunnyType.UInt32)]
    [TestCase("byte[][]=", BaseFunnyType.UInt8)]
    [TestCase("uint16[][]=", BaseFunnyType.UInt16)]
    [TestCase("uint32[][];", BaseFunnyType.UInt32)]
    [TestCase("uint64[][];(", BaseFunnyType.UInt64)]
    [TestCase("uint64[][];a", BaseFunnyType.UInt64)]
    [TestCase("real[][]", BaseFunnyType.Real)]
    [TestCase("real[][]:", BaseFunnyType.Real)]
    [TestCase("bool[][]", BaseFunnyType.Bool)]
    [TestCase("any[][]", BaseFunnyType.Any)]
    public void ReadTwinArrayType(string expr, BaseFunnyType elementType) =>
        AssertFunnyType(expr, FunnyType.ArrayOf(FunnyType.ArrayOf(FunnyType.PrimitiveOf(elementType))));

    private void AssertFunnyType(string expr, FunnyType expected) {
        var flow = Tokenizer.ToFlow(expr);
        var actual = flow.ReadType();
        Assert.AreEqual(expected, actual);
    }
}
