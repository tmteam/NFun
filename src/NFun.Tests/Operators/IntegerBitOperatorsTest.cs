using System;
using NFun;
using NFun.Exceptions;
using NUnit.Framework;

namespace Funny.Tests.Operators
{
    public class IntegerBitOperatorsTest
    {
        [TestCase("y = 1 & 1",1)]
        [TestCase("y = 1 & 2",0)]
        [TestCase("y = 1 & 3",1)]
        [TestCase("y = 0xFFFFFFFF & 0x0", (long)0)]
        [TestCase("y = 0xFFFFFFFF & 0xFFFFFFFF", (long)0xFFFFFFFF)]
        public void ConstantBitAnd(string expression, object expected) 
            => TestTools.AssertConstantCalc("y",expression, expected);
        
        [TestCase("y = 0 | 2",2)]
        [TestCase("y = 1 | 2",3)]
        [TestCase("y = 1 | 4",5)]
        [TestCase("y = 0xFFFFFFFF | 0x0",(long)0xFFFFFFFF)]
        [TestCase("y = 0xFFFFFFFF | 0xFFFFFFFF",(long)0xFFFFFFFF)]
        public void ConstantBitOr(string expression, object expected) 
            => TestTools.AssertConstantCalc("y",expression, expected);
        
        [TestCase("y = 1 ^ 0",1)]
        [TestCase("y = 1 ^ 1",0)]
        [TestCase("y = 1 ^ 1",0)]
        [TestCase("y = 0xFFFFFFFF ^ 0x0", (long)0xFFFFFFFF)]
        [TestCase("y = 0xFFFFFFFF ^ 0xFFFFFFFF",(long)0)]
        public void ConstantBitXor(string expression, object expected) 
            => TestTools.AssertConstantCalc("y",expression, expected);
        
        [TestCase("y = 1 << 3",8)]
        [TestCase("y = 8 >> 3",1)]
        public void ConstantBitShift(string expression, object expected) 
            => TestTools.AssertConstantCalc("y",expression, expected);
        
        [TestCase("y:int16 = ~1",         (Int16)(-2))]
        [TestCase("y:int16 = ~-1",        (Int16)0)]
        [TestCase("y:int16 = ~0xF0F",     (Int16) (-3856))]
        [TestCase("y:uint16 = ~1",        (UInt16)0xFFFE)]
        [TestCase("y:uint16 = ~0xF0F0",   (UInt16)0x0F0F)]
        [TestCase("y = ~1",               (int)-2)]
        [TestCase("y = ~-1",              (int)0)]
        [TestCase("y:int = ~1",           (int)-2)]
        [TestCase("y:int = ~-1",          (int)0)]
        [TestCase("y:int = ~0x00F0F0F0",  (int)-15790321)]
        [TestCase("y:uint = ~1",          (uint)0xFFFFFFFE)]
        [TestCase("y:uint = ~0xF0F0F0F0", (uint)0xF0F0F0F)]
       
        [TestCase("y:int64 = ~1",           (long)-2)]
        [TestCase("y:int64 = ~-1",          (long) 0)]
        [TestCase("y:int64 = ~0xF0F0F0F0",  (long)-4042322161)]
        [TestCase("y:uint64 = ~1",          (ulong)0xFFFF_FFFF_FFFF_FFFE)]
        [TestCase("y:uint64 = ~0xF0F0F0F0", (ulong)0xFFFF_FFFF_0F0F_0F0F)]
        
        [TestCase("y = 1 == ~~1",         true)]
        [TestCase("y = 0 == ~~0",         true)]
        [TestCase("y = -1 == ~~-1",       true)]
        [TestCase("y = 0xA == ~~0xA",     true)]
        [TestCase("y = -0xA == ~~-0xA",   true)]

        [TestCase("y = 0xABCD_EF01 == ~~0xABCD_EF01",  true)]
        [TestCase("y = -0xABCD_EF01 == ~~-0xABCD_EF01",true)]
        public void ConstantBitInvert(string expression, object expected) 
            => TestTools.AssertConstantCalc("y",expression, expected);
        
        [TestCase("y = ^2")]
        [TestCase("y = 2^^")]
        [TestCase("y = ^^2")]
        [TestCase("y = -")]
        [TestCase("y = ~")]
        [TestCase("~y=3")]
        [TestCase("y = ~")]
        [TestCase("y = ~-")]
        [TestCase("y = ~1.5")]
        public void ObviouslyFails(string expr) =>
            Assert.Throws<FunParseException>(
                ()=> FunBuilder.Build(expr));
    }
}