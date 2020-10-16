using System;
using NFun;
using NFun.Exceptions;
using NUnit.Framework;

namespace Funny.Tests
{
    public class ConstantsTest
    {
        [TestCase("y = 2",2.0)]
        [TestCase("y:int64 = -1",(Int64)(-1.0))]
        [TestCase("y:int32 = -1",(Int32)(-1.0))]
        [TestCase("y:int16 = -1",(Int16)(-1.0))]
        [TestCase("y:int64 = 143",(Int64)(143))]
        [TestCase("y:int32 = 143",(Int32)(143))]
        [TestCase("y:int16 = 143",(Int16)(143))]
        [TestCase("y:uint64 = 143",(UInt64)(143))]
        [TestCase("y:uint32 = 143",(UInt32)(143))]
        [TestCase("y:uint16 = 143",(UInt16)(143))]
        
        [TestCase("y:int16 = 32767", Int16.MaxValue)]
        [TestCase("y:int16 = -32768",Int16.MinValue)]
        [TestCase("y:int32 = 2147483647", Int32.MaxValue)]
        [TestCase("y:int32 = -2147483648",Int32.MinValue)]
        [TestCase("y:int64 = 9223372036854775807", Int64.MaxValue)]
        [TestCase("y:int64 = -9223372036854775808",Int64.MinValue)]

        
        [TestCase("y:byte   = 255",byte.MaxValue)]
        [TestCase("y:uint16 = 65535",UInt16.MaxValue)]
        [TestCase("y:uint32 = 4294967295",UInt32.MaxValue)]
        [TestCase("y:uint64 = 18446744073709551615", UInt64.MaxValue)]
        
        [TestCase("y:byte   = 0xFF",byte.MaxValue)]
        [TestCase("y:uint16 = 0xFFFF",UInt16.MaxValue)]
        [TestCase("y:uint32 = 0xFFFF_FFFF",UInt32.MaxValue)]
        [TestCase("y:uint64 = 0xFFFF_FFFF_FFFF_FFFF", UInt64.MaxValue)]
        
        [TestCase("y:int16 = 0",  (Int16)0)]
        [TestCase("y:int32 = 0",  (Int32)(0))]
        [TestCase("y:int64 = 0",  (Int64)0)]
        
        [TestCase("y:byte = 0",  (byte)0)]
        [TestCase("y:uint16 = 0", (UInt16)0)]
        [TestCase("y:uint32 = 0", (UInt32)(0))]
        [TestCase("y:uint64 = 0", (UInt64)0)]

        [TestCase("y = 0.2",0.2)]
        [TestCase("y = 11.222  ",11.222)]
        [TestCase("y = 1111  ", 1111.0)]
        [TestCase("y = 11_11  ", 1111.0)]
        [TestCase("y = 1.1_11  ",1.111)]
        public void NumericConstantEqualsExpected(string expression, object expected) 
            => TestTools.AssertConstantCalc("y",expression, expected);
        
        [TestCase("y = 0xfF  ",255)]
        [TestCase("y = 0x00_Ff  ",255)]
        [TestCase("y = 0b001  ",1)]
        [TestCase("y = 0b11  ",3)]
        [TestCase("y = 0x_1",1)]
        [TestCase("y = 0xFFFFFFFF  ",(Int64)0xFFFFFFFF)]
        [TestCase("y = 0xFFFFFFFFFFFFFFFF  ",(UInt64)0xFFFFFFFF_FFFFFFFF)]
        [TestCase("y:uint64 = 0xFFFFFFFFFFFFFFFF",(UInt64)0xFFFFFFFF_FFFFFFFF)]
        [TestCase("y:int64 = 0x8000000_00000000",(Int64)0x8000000_00000000)]
        [TestCase("y:uint32 = 0xFFFFFFFF",(UInt32)0xFFFFFFFF)]
        [TestCase("y:int32 = 0x7FFF_FFFF",(Int32)0x7FFF_FFFF)]
        [TestCase("y:uint16 = 0xFFFF",(UInt16)0xFFFF)]
        [TestCase("y:byte = 0xFF",    (byte)0xFF)]
        [TestCase("y:real = 0xfF  ",   (double)255)]
        [TestCase("y:real = 0x00_Ff  ",(double)255)]
        [TestCase("y:real = 0b001  ",  (double)1)]
        [TestCase("y:real = 0b11  ",   (double)3)]
        [TestCase("y:real = 0x_1",     (double)1)]
        [TestCase("y:int64 = 0xfF  ",   (Int64)255)]
        [TestCase("y:int64 = 0x00_Ff  ",(Int64)255)]
        [TestCase("y:int64 = 0b001  ",  (Int64)1)]
        [TestCase("y:int64 = 0b11  ",   (Int64)3)]
        [TestCase("y:int64 = 0x_1",     (Int64)1)]
        [TestCase("y:int32 = 0xfF  ",   (Int32)255)]
        [TestCase("y:int32 = 0x00_Ff  ",(Int32)255)]
        [TestCase("y:int32 = 0b001  ",  (Int32)1)]
        [TestCase("y:int32 = 0b11  ",   (Int32)3)]
        [TestCase("y:int32 = 0x_1",     (Int32)1)]
        [TestCase("y:int16 = 0xfF  ",   (Int16)255)]
        [TestCase("y:int16 = 0x00_Ff  ",(Int16)255)]
        [TestCase("y:int16 = 0b001  ",  (Int16)1)]
        [TestCase("y:int16 = 0b11  ",   (Int16)3)]
        [TestCase("y:int16 = 0x_1",     (Int16)1)]
        
        [TestCase("y:real = -0xfF  ",    (Double)(-255))]
        [TestCase("y:real = -0x00_Ff  ", (Double)(-255))]
        [TestCase("y:real = -0b001  ",   (Double)(-1))]
        [TestCase("y:real = -0b11  ",    (Double)(-3))]
        [TestCase("y:real = -0x_1",      (Double)(-1))]
        [TestCase("y:int64 = -0xfF  ",   (Int64)(-255))]
        [TestCase("y:int64 = -0x00_Ff  ",(Int64)(-255))]
        [TestCase("y:int64 = -0b001  ",  (Int64)(-1))]
        [TestCase("y:int64 = -0b11  ",   (Int64)(-3))]
        [TestCase("y:int64 = -0x_1",     (Int64)(-1))]
        [TestCase("y:int32 = -0xfF  ",   (Int32)(-255))]
        [TestCase("y:int32 = -0x00_Ff  ",(Int32)(-255))]
        [TestCase("y:int32 = -0b001  ",  (Int32)(-1))]
        [TestCase("y:int32 = -0b11  ",   (Int32)(-3))]
        [TestCase("y:int32 = -0x_1",     (Int32)(-1))]
        [TestCase("y:int16 = -0xfF  ",   (Int16)(-255))]
        [TestCase("y:int16 = -0x00_Ff  ",(Int16)(-255))]
        [TestCase("y:int16 = -0b001  ",  (Int16)(-1))]
        [TestCase("y:int16 = -0b11  ",   (Int16)(-3))]
        [TestCase("y:int16 = -0x_1",     (Int16)(-1))]
        
        [TestCase("y:uint64 = 0xfF  ",   (UInt64)255)]
        [TestCase("y:uint64 = 0x00_Ff  ",(UInt64)255)]
        [TestCase("y:uint64 = 0b001  ",  (UInt64)1)]
        [TestCase("y:uint64 = 0b11  ",   (UInt64)3)]
        [TestCase("y:uint64 = 0x_1",     (UInt64)1)]
        
        [TestCase("y:uint32 = 0xfF  ",   (UInt32)255)]
        [TestCase("y:uint32 = 0x00_Ff  ",(UInt32)255)]
        [TestCase("y:uint32 = 0b001  ",  (UInt32)1)]
        [TestCase("y:uint32 = 0b11  ",   (UInt32)3)]
        [TestCase("y:uint32 = 0x_1",     (UInt32)1)]
        
        [TestCase("y:uint16 = 0xfF  ",   (UInt16)255)]
        [TestCase("y:uint16 = 0x00_Ff  ",(UInt16)255)]
        [TestCase("y:uint16 = 0b001  ",  (UInt16)1)]
        [TestCase("y:uint16 = 0b11  ",   (UInt16)3)]
        [TestCase("y:uint16 = 0x_1",     (UInt16)1)]
        public void PrimitiveHexConstantEqualsExpectedIgnoreCase(string expression, object expected)
        {
            var index = expression.IndexOf("0x", StringComparison.Ordinal);
            if (index == -1) index = expression.IndexOf("0b", StringComparison.Ordinal);
            index += 2;
            var upperExpression = expression.Substring(0, index) + expression.Substring(index).ToUpper();
            TestTools.AssertConstantCalc("y", upperExpression, expected);
            var lowerExpression = expression.Substring(0, index) + expression.Substring(index).ToLower();
            TestTools.AssertConstantCalc("y", upperExpression, expected);
            
        }

        [TestCase("y = 91111111111111111111111111111111111111")]
        [TestCase("y = .2")]
        [TestCase("y = 0bx2")]
        [TestCase("y = 02.")]
        [TestCase("y = 0x2.3")]
        [TestCase("y = 0x99GG")]
        [TestCase("y = 0bFF")]
        [TestCase("y=0.")]
        [TestCase("y:uint64 = true")]
        [TestCase("y:int64  = true")]
        [TestCase("y:uint32 = true")]
        [TestCase("y:int32  = false")]
        [TestCase("y:uint16 = false")]
        [TestCase("y:byte   = false")]
        [TestCase("y:bool = 1")]
        [TestCase("y:bool = 1.0")]
        [TestCase("y:bool = 'vasa'")]
      
        [TestCase("y:uint64 = 0x1FFFF_FFFF_FFFF_FFFF")]
        [TestCase("y:uint64 = 0xFFFFFFFFF_FFFFFFFF")]
        [TestCase("y:int64 = 0xF000_0000_00000000")]
        [TestCase("y:int64 = 0x1_0000_0000_00000000")]
        [TestCase("y:uint32 = 0x1FFFFFFFF")]
        [TestCase("y:int32 = 0xFFFF_FFFF")]
        [TestCase("y:uint16 = 0x1FFFF")]
        [TestCase("y:byte = 0x1FF")]
        
        [TestCase("y:int16 = 65535")]
        [TestCase("y:int32 = 4294967295")]
        [TestCase("y:int64 = 18446744073709551615")]
        [TestCase("y:int16 = 32768")]
        [TestCase("y:int16 = -32769")]
        [TestCase("y:int32 = 2147483648")]
        [TestCase("y:int32 = -2147483649")]
        [TestCase("y:int64 = 9223372036854775808")]
        [TestCase("y:int64 = 9223372036854775809")]
        [TestCase("y:int64 = 19223372036854775809")]
        [TestCase("y:int64 = -9223372036854775809")]
        [TestCase("y:int64 = -19223372036854775809")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<FunParseException>(()=> FunBuilder.Build(expr));
    }
}