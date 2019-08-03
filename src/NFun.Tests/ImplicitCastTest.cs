using System.Linq;
using NFun;
using NFun.ParseErrors;
using NFun.Types;
using NUnit.Framework;

namespace Funny.Tests
{
    [TestFixture]
    public class ImplicitCastTest{

        [TestCase("uint8",    (byte)42,    "uint8", (byte)42)]

        [TestCase("uint8",    (byte)42,    "uint16", (ushort)42)]
        [TestCase("uint16", (ushort)42,    "uint16", (ushort)42)]

        [TestCase("uint8",    (byte)42,    "uint32", (uint)42)]
        [TestCase("uint16", (ushort)42,    "uint32", (uint)42)]
        [TestCase("uint32",   (uint)42,    "uint32", (uint)42)]
  
        [TestCase("uint8",    (byte)42,    "uint64", (ulong)42)]
        [TestCase("uint16", (ushort)42,    "uint64", (ulong)42)]
        [TestCase("uint32",   (uint)42,    "uint64", (ulong)42)]
        [TestCase("uint64",  (ulong)42,    "uint64", (ulong)42)]

        [TestCase("int16",  (short)42,    "int16", (short)42)]
        [TestCase("uint8",   (byte)42,    "int16", (short)42)]

        [TestCase("int16",  (short)42,    "int32", (int)42)]
        [TestCase("int32",  (int)  42,    "int32", (int)42)]
        [TestCase("uint8",   (byte)42,    "int32", (int)42)]
        [TestCase("uint16",(ushort)42,    "int32", (int)42)]
        
        [TestCase("int16",  (short)42,    "int64", (long)42)]
        [TestCase("int32",  (int)  42,    "int64", (long)42)]
        [TestCase("int64",  (long) 42,    "int64", (long)42)]
        [TestCase("uint8",   (byte)42,    "int64", (long)42)]
        [TestCase("uint16",(ushort)42,    "int64", (long)42)]
        [TestCase("uint32",  (uint)42,    "int64", (long)42)]
        
        [TestCase("int16",  (short)42,    "real", 42.0)]
        [TestCase("int32",  (int)  42,    "real", 42.0)]
        [TestCase("int64",  (long) 42,    "real", 42.0)]
        [TestCase("uint8",   (byte)42,    "real", 42.0)]
        [TestCase("uint16",(ushort)42,    "real", 42.0)]
        [TestCase("uint32",  (uint)42,    "real", 42.0)]
        [TestCase("uint64",  (ulong)42,   "real", 42.0)]
        [TestCase("real",    42.2,        "real", 42.2)]
        public void Allowed_ImplicitNumbersCast(string typeFrom,  object valueFrom, string typeTo, object valueTo)
        {
            var expr = $"conv(a:{typeTo}):{typeTo} = a; x:{typeFrom}; y = conv(x)";
            var runtime = FunBuilder.BuildDefault(expr);
            
            runtime.Calculate(Var.New("x", valueFrom)).AssertReturns(Var.New("y", valueTo));
        }

        [TestCase("1", "uint8")]
        [TestCase("1", "uint16")]
        [TestCase("1", "uint32")]
        [TestCase("1", "uint64")]
        [TestCase("1", "int16")]
        [TestCase("1", "int32")]
        [TestCase("1", "int64")]
        [TestCase("1", "real")]

        [TestCase("-1", "int32")]
        [TestCase("-1", "int64")]
        [TestCase("-1", "real")]
      
        
        [TestCase("-300", "int32")]
        [TestCase("-300", "int64")]
        [TestCase("-300", "real")]

        [TestCase("0xFF", "uint16")]
        [TestCase("0xFF", "uint32")]
        [TestCase("0xFF", "uint64")]
        [TestCase("0xFF", "int16")]
        [TestCase("0xFF", "int32")]
        [TestCase("0xFF", "int64")]
        [TestCase("0xFF", "real")]

        [TestCase("0xFFF", "uint16")]
        [TestCase("0xFFF", "uint32")]
        [TestCase("0xFFF", "uint64")]
        [TestCase("0xFFF", "int32")]
        [TestCase("0xFFF", "int64")]
        [TestCase("0xFFF", "real")]
        
        [TestCase("0xFFFF", "uint32")]
        [TestCase("0xFFFF", "uint64")]
        [TestCase("0xFFFF", "int32")]
        [TestCase("0xFFFF", "int64")]
        [TestCase("0xFFFF", "real")]

        [TestCase("0xFFFF_FFFF", "uint64")]
        [TestCase("0xFFFF_FFFF", "int64")]
        [TestCase("0xFFFF_FFFF", "real")]
        //Cannot be solved with current TI system
        //[TestCase("-1", "int16")]
        //[TestCase("-300", "int16")]
        public void Allowed_NumberConstantImplicitCast(string constant, string typeTo)
        {
            var expr = $"conv(a:{typeTo}):{typeTo} = a; y = conv({constant})";
            var runtime = FunBuilder.BuildDefault(expr);
            Assert.AreEqual(typeTo.ToLower(), runtime.Outputs.Single().Type.ToString().ToLower());
        }
        
        [TestCase("int16",   "uint8")]
        [TestCase("int32",   "uint8")]
        [TestCase("int64",   "uint8")]
        [TestCase("uint16",  "uint8")]
        [TestCase("uint32",  "uint8")]
        [TestCase("uint64",  "uint8")]
        [TestCase("real",    "uint8")]
        
        [TestCase("int8",    "uint16")]
        [TestCase("int16",   "uint16")]
        [TestCase("int32",   "uint16")]
        [TestCase("int64",   "uint16")]
        [TestCase("uint32",  "uint16")]
        [TestCase("uint64",  "uint16")]
        [TestCase("real",    "uint16")]

        [TestCase("int8",    "uint32")]
        [TestCase("int16",   "uint32")]
        [TestCase("int32",   "uint32")]
        [TestCase("int64",   "uint32")]
        [TestCase("uint64",  "uint32")]
        [TestCase("real",    "uint32")]
        
        [TestCase("int8",    "uint64")]
        [TestCase("int16",   "uint64")]
        [TestCase("int32",   "uint64")]
        [TestCase("int64",   "uint64")]
        [TestCase("real",    "uint64")]
        
        [TestCase("int16",   "int8")]
        [TestCase("int32",   "int8")]
        [TestCase("int64",   "int8")]
        [TestCase("uint8",   "int8")]
        [TestCase("uint16",  "int8")]
        [TestCase("uint32",  "int8")]
        [TestCase("uint64",  "int8")]
        [TestCase("real",    "int8")]

        [TestCase("int32",   "int16")]
        [TestCase("int64",   "int16")]
        [TestCase("uint16",  "int16")]
        [TestCase("uint32",  "int16")]
        [TestCase("uint64",  "int16")]
        [TestCase("real",    "int16")]

        [TestCase("int64",   "int32")]
        [TestCase("uint32",  "int32")]
        [TestCase("uint64",  "int32")]
        [TestCase("real",    "int32")]

        [TestCase("uint64",  "int64")]
        [TestCase("real",    "int64")]
        public void ObviousFails_ImplicitNumbersCast(string typeFrom,   string typeTo)
        {
            var expr = $"conv(a:{typeTo}):{typeTo} = a; x:{typeFrom}; y = conv(x)";
            Assert.Throws<FunParseException>(()=>FunBuilder.BuildDefault(expr));
        }
        
        [TestCase("-1", "uint8")]
        [TestCase("-1", "uint16")]
        [TestCase("-1", "uint32")]
        [TestCase("-1", "uint64")]

        [TestCase("0xFF", "int8")]
        [TestCase("0xFFF", "uint8")]
        
        [TestCase("0xFFFF", "uint8")]
        [TestCase("0xFFFF", "int16")]
        [TestCase("0x10000", "uint16")]

        [TestCase("0xFFFF_FFFF", "uint8")]
        [TestCase("0xFFFF_FFFF", "int16")]
        [TestCase("0xFFFF_FFFF", "uint16")]
        [TestCase("0xFFFF_FFFF", "int32")]
        [TestCase("0x1_0000_0000", "uint32")]
        public void ObviousFails_NumberConstantImplicitCast(string constant, string typeTo)
        {
            var expr = $"customConvert(a:{typeTo}):{typeTo} = a; y = customConvert({constant})";
            Assert.Throws<FunParseException>(()=>FunBuilder.BuildDefault(expr));
        }
    }
}