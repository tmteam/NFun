using System;
using NFun.Tokenization;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests.UnitTests
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
        [TestCase("0b1111_1111_1111_1111_1111_1111_1111_0011",0b1111_1111_1111_1111_1111_1111_1111_0011, BaseVarType.UInt32)]
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

        public void ToConstant_SomeCrap_ThrowsFormatException(string value)
        {
            
            try
            {
                TokenHelper.ToConstant(value);
                Assert.Fail("Exception does not throw");
            }
            catch (SystemException)
            {
                Assert.Pass();
            } 
        }
    }
}