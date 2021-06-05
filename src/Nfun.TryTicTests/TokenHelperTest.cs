using NFun.Tokenization;
using NFun.Types;
using NUnit.Framework;

namespace NFun.UnitTests
{
    [TestFixture]
    public class TokenHelperTest
    {
        [TestCase("1",1, BaseVarType.Int32)]
        [TestCase("0xFF",0xFF, BaseVarType.Int32)]
        [TestCase("0.1",0.1, BaseVarType.Real)]
        [TestCase("0xFEDCBA987654321",0xFEDCBA987654321, BaseVarType.Int64)]
        [TestCase("0xFEDCBA",0xFEDCBA, BaseVarType.Int32)]
        [TestCase("9876543210",9876543210, BaseVarType.Int64)]
        [TestCase("-9876543210",-9876543210, BaseVarType.Int64)]
        [TestCase("2147483647",2147483647, BaseVarType.Int32)]
        [TestCase("-2147483648",-2147483648, BaseVarType.Int32)]
        [TestCase("0b1111_1111_1111_1111_1111_1111_1111",0b1111_1111_1111_1111_1111_1111_1111, BaseVarType.Int32)]
        public void ToConstant_NumberConstant_ParsesWell(string value, object expectedVal, BaseVarType expectedType)
        {
            var (obj, type) = TokenHelper.ToConstant(value);
            Assert.AreEqual(expectedVal, obj);
            Assert.AreEqual(expectedType,type.BaseType);
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

        [TestCase("int", BaseVarType.Int32)]
        [TestCase("int;[]", BaseVarType.Int32)]
        [TestCase("int;[][]", BaseVarType.Int32)]
        [TestCase("int16", BaseVarType.Int16)]
        [TestCase("int32", BaseVarType.Int32)]
        [TestCase("int64", BaseVarType.Int64)]
        [TestCase("uint", BaseVarType.UInt32)]
        [TestCase("byte", BaseVarType.UInt8)]
        [TestCase("int=", BaseVarType.Int32)]
        [TestCase("int16=", BaseVarType.Int16)]
        [TestCase("int32=", BaseVarType.Int32)]
        [TestCase("int64=", BaseVarType.Int64)]
        [TestCase("uint=", BaseVarType.UInt32)]
        [TestCase("byte=", BaseVarType.UInt8)]
        [TestCase("uint16=", BaseVarType.UInt16)]
        [TestCase("uint32;", BaseVarType.UInt32)]
        [TestCase("uint64;(", BaseVarType.UInt64)]
        [TestCase("uint64;a", BaseVarType.UInt64)]

        [TestCase("real", BaseVarType.Real)]
        [TestCase("real:", BaseVarType.Real)]
        [TestCase("bool", BaseVarType.Bool)]
        [TestCase("any", BaseVarType.Any)]
        public void ReadVarType_PrimitiveTypes(string expr, BaseVarType expected) => 
            AssertVarType(expr, VarType.PrimitiveOf(expected));

        [TestCase("int8")]
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
        [TestCase("fun")]
        [TestCase("fun(?):?")]
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
        [TestCase("char")]
        [TestCase("char[]")]
        [TestCase("t")]
        public void ReadVarType_Throws(string expr)
        {
            var flow = Tokenizer.ToFlow(expr);
            Assert.Catch(() => flow.ReadType());
        }
        [TestCase("text")]
        [TestCase("text=")]
        [TestCase("text:")]
        public void ReadTextType(string expr) => AssertVarType(expr,VarType.Text);

        [TestCase("int[]", BaseVarType.Int32)]
        [TestCase("int[]\r", BaseVarType.Int32)]
        [TestCase("int[]\r[]", BaseVarType.Int32)]
        [TestCase("int[];[]", BaseVarType.Int32)]
        [TestCase("int16[]", BaseVarType.Int16)]

        [TestCase("int32[]", BaseVarType.Int32)]
        [TestCase("int64[]", BaseVarType.Int64)]
        [TestCase("uint[]", BaseVarType.UInt32)]
        [TestCase("byte[]", BaseVarType.UInt8)]
        [TestCase("int[]=", BaseVarType.Int32)]
        [TestCase("int16[]=", BaseVarType.Int16)]
        [TestCase("int32[]=", BaseVarType.Int32)]
        [TestCase("int64[]=", BaseVarType.Int64)]
        [TestCase("uint[]=", BaseVarType.UInt32)]
        [TestCase("byte[]=", BaseVarType.UInt8)]
        [TestCase("uint16[]=", BaseVarType.UInt16)]
        [TestCase("uint32[];", BaseVarType.UInt32)]
        [TestCase("uint64[];(", BaseVarType.UInt64)]
        [TestCase("uint64[];a", BaseVarType.UInt64)]

        [TestCase("real[]", BaseVarType.Real)]
        [TestCase("real[]:", BaseVarType.Real)]
        [TestCase("bool[]", BaseVarType.Bool)]
        [TestCase("any[]", BaseVarType.Any)]
        public void ReadArrayType(string expr, BaseVarType elementType) =>
            AssertVarType(expr, VarType.ArrayOf(VarType.PrimitiveOf(elementType)));
        
        [TestCase("int[][]", BaseVarType.Int32)]
        [TestCase("int16[][]", BaseVarType.Int16)]
        [TestCase("int32[][]", BaseVarType.Int32)]
        [TestCase("int64[][]", BaseVarType.Int64)]
        [TestCase("uint[][]", BaseVarType.UInt32)]
        [TestCase("byte[][]", BaseVarType.UInt8)]
        [TestCase("int[][]=", BaseVarType.Int32)]
        [TestCase("int16[][]=", BaseVarType.Int16)]
        [TestCase("int32[][]=", BaseVarType.Int32)]
        [TestCase("int64[][]=", BaseVarType.Int64)]
        [TestCase("uint[][]=", BaseVarType.UInt32)]
        [TestCase("byte[][]=", BaseVarType.UInt8)]
        [TestCase("uint16[][]=", BaseVarType.UInt16)]
        [TestCase("uint32[][];", BaseVarType.UInt32)]
        [TestCase("uint64[][];(", BaseVarType.UInt64)]
        [TestCase("uint64[][];a", BaseVarType.UInt64)]

        [TestCase("real[][]", BaseVarType.Real)]
        [TestCase("real[][]:", BaseVarType.Real)]
        [TestCase("bool[][]", BaseVarType.Bool)]
        [TestCase("any[][]", BaseVarType.Any)]
        public void ReadTwinArrayType(string expr, BaseVarType elementType) =>
            AssertVarType(expr, VarType.ArrayOf(VarType.ArrayOf(VarType.PrimitiveOf(elementType))));
        
        public void AssertVarType(string expr, VarType expected)
        {
            var flow = Tokenizer.ToFlow(expr);
            var actual = flow.ReadType();
            Assert.AreEqual(expected,actual);
        }
    }
}