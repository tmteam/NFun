using System;
using System.Net;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests.BuiltInFunctions;

[TestFixture]
public class ConvertFunctionsTest {
    [TestCase("int", (int)-123, "int16", (short)-123)]
    [TestCase("int", (int)123, "int16", (short)123)]
    [TestCase("int16", (short)-123, "int16", (short)-123)]
    [TestCase("int32", (int)-123, "int16", (short)-123)]
    [TestCase("int64", (long)-123, "int16", (short)-123)]
    [TestCase("uint8", (byte)123, "int16", (short)123)]
    [TestCase("uint16", (ushort)123, "int16", (short)123)]
    [TestCase("uint32", (uint)123, "int16", (short)123)]
    [TestCase("uint64", (ulong)123, "int16", (short)123)]
    [TestCase("int", (int)-123, "int", -123)]
    [TestCase("int", (int)123, "int", 123)]
    [TestCase("int16", (short)-123, "int", -123)]
    [TestCase("int32", (int)-123, "int", -123)]
    [TestCase("int64", (long)-123, "int", -123)]
    [TestCase("uint8", (byte)123, "int", 123)]
    [TestCase("uint16", (ushort)123, "int", 123)]
    [TestCase("uint32", (uint)123, "int", 123)]
    [TestCase("uint64", (ulong)123, "int", 123)]
    [TestCase("int", (int)-123, "int64", (long)-123)]
    [TestCase("int", (int)123, "int64", (long)123)]
    [TestCase("int16", (short)-123, "int64", (long)-123)]
    [TestCase("int32", (int)-123, "int64", (long)-123)]
    [TestCase("int64", (long)-123, "int64", (long)-123)]
    [TestCase("uint8", (byte)123, "int64", (long)123)]
    [TestCase("uint16", (ushort)123, "int64", (long)123)]
    [TestCase("uint32", (uint)123, "int64", (long)123)]
    [TestCase("uint64", (ulong)123, "int64", (long)123)]
    [TestCase("int", (int)123, "byte", (byte)123)]
    [TestCase("int16", (short)123, "byte", (byte)123)]
    [TestCase("int64", (long)123, "byte", (byte)123)]
    [TestCase("uint8", (byte)123, "byte", (byte)123)]
    [TestCase("uint16", (ushort)123, "byte", (byte)123)]
    [TestCase("uint32", (uint)123, "byte", (byte)123)]
    [TestCase("uint64", (ulong)123, "byte", (byte)123)]
    [TestCase("int", (int)123, "uint16", (ushort)123)]
    [TestCase("int16", (short)123, "uint16", (ushort)123)]
    [TestCase("int64", (long)123, "uint16", (ushort)123)]
    [TestCase("uint8", (byte)123, "uint16", (ushort)123)]
    [TestCase("uint16", (ushort)123, "uint16", (ushort)123)]
    [TestCase("uint32", (uint)123, "uint16", (ushort)123)]
    [TestCase("uint64", (ulong)123, "uint16", (ushort)123)]
    [TestCase("int", (int)123, "uint32", (uint)123)]
    [TestCase("int16", (short)123, "uint32", (uint)123)]
    [TestCase("int64", (long)123, "uint32", (uint)123)]
    [TestCase("uint8", (byte)123, "uint32", (uint)123)]
    [TestCase("uint16", (ushort)123, "uint32", (uint)123)]
    [TestCase("uint32", (uint)123, "uint32", (uint)123)]
    [TestCase("uint64", (ulong)123, "uint32", (uint)123)]
    [TestCase("int", (int)123, "uint64", (ulong)123)]
    [TestCase("int16", (short)123, "uint64", (ulong)123)]
    [TestCase("int64", (long)123, "uint64", (ulong)123)]
    [TestCase("uint8", (byte)123, "uint64", (ulong)123)]
    [TestCase("uint16", (ushort)123, "uint64", (ulong)123)]
    [TestCase("uint32", (uint)123, "uint64", (ulong)123)]
    [TestCase("uint64", (ulong)123, "uint64", (ulong)123)]
    public void ConvertIntegersFunctionsTest(
        string inputType, object inputValue, string outputType,
        object expectedOutput) {
        var expr = $"x:{inputType}; y:{outputType} = convert(x)";
        expr.Calc("x", inputValue).AssertReturns("y", expectedOutput);
    }

    [Ignore("todo")]
    [TestCase("x:bool[] = convert(0b1111011); y:int = convert(x)", 0b1111011)]
    [TestCase("out:int32 = '😃'[0].convert()", 1234)]
    [TestCase("x:int32 = 65533;   out:char = x.convert()", 's')]
    [TestCase("x:int32[] = [97];           out:char = x.convert()", 'a')]
    [TestCase("x:int32[] = [122];          out:char = x.convert()", 'z')]
    [TestCase("x:int32[] = [48];           out:char = x.convert()", '0')]
    [TestCase("x:int32[] = [65];           out:char = x.convert()", 'A')]
    [TestCase("x:int32[] = [32];           out:char = x.convert()", ' ')]
    [TestCase("x:int32[] = [0x4b, 0x04];   out:char = x.convert()", 'ы')]
    [TestCase("x:uint64[] = [97];           out:char = x.convert()", 'a')]
    [TestCase("x:uint64[] = [122];          out:char = x.convert()", 'z')]
    [TestCase("x:uint64[] = [48];           out:char = x.convert()", '0')]
    [TestCase("x:uint64[] = [65];           out:char = x.convert()", 'A')]
    [TestCase("x:uint64[] = [32];           out:char = x.convert()", ' ')]
    [TestCase("x:uint64[] = [0x4b,0x04];    out:char = x.convert()", 'ы')]
    [TestCase("x:real[] = [97];           out:char = x.convert()", 'a')]
    [TestCase("x:real[] = [122];          out:char = x.convert()", 'z')]
    [TestCase("x:real[] = [48];           out:char = x.convert()", '0')]
    [TestCase("x:real[] = [65];           out:char = x.convert()", 'A')]
    [TestCase("x:real[] = [32];           out:char = x.convert()", ' ')]
    [TestCase("x:real[] = [0x04, 0x4b];   out:char = x.convert()", 'ы')]
    public void TODOConstantConvertTest(string expr, object expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("y:byte[]='a'[0].convert(); ", new byte[] { 0x61 })]
    [TestCase("y:int= convert(1.2)", 1)]
    [TestCase("y:int= convert(-1.2)", -1)]
    [TestCase("y:int= convert('1')", 1)]
    [TestCase("y:int= convert('-123')", -123)]
    [TestCase("x:byte[]=[0x21,0x33,0x12];  y:int= x.convert()", 1_192_737)]
    [TestCase("x:byte[]=[0x21,0x33,0x12,0x00]; y:int= convert(x)", 1_192_737)]
    [TestCase("x:byte[]=[0x21,0x00,0x00,0x00]; y:int= convert(x)", 0x21)]
    [TestCase("x:byte[]=[0x21,0x00,0x00,0x00]; y:int= convert(x)", 0x21)]
    [TestCase("x:byte[]=[0x21]; y:int= convert(x)", 0x21)]
    [TestCase("y:real = convert('1')", 1.0)]
    [TestCase("y:real = convert('1.1')", 1.1)]
    [TestCase("y:real = convert('-0.123')", -0.123)]
    [TestCase("y:real = convert(1)", 1.0)]
    [TestCase("y:real = convert(-1)", -1.0)]
    [TestCase(
        "y:bool[] = convert(0b1111011)",
        new[] {
            true, true, false, true, true, true, true, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false
        })]
    [TestCase("x:bool[] = convert(0b1111011); y = x.count()", 32)]
    [TestCase("y:byte[] = convert(0x123)", new byte[] { 35, 1, 0, 0 })]
    [TestCase("y:byte[] = convert(0xFA00FA)", new byte[] { 250, 0, 250, 0 })]
    [TestCase("x:byte[] = convert(0x123); y = x.count()", 4)]
    [TestCase("x:byte[] = convert(0xFA00FA); y =x.count()", 4)]
    [TestCase("y:bool=convert(1)", true)]
    [TestCase("y:bool=convert(0)", false)]
    [TestCase("y:byte[]=convert('hi there')",
        new byte[] { 0x68, 0, 0x69, 0, 0x20, 0, 0x74, 0, 0x68, 0, 0x65, 0, 0x72, 0, 0x65, 0 })]
    [TestCase("y=convert('hi there').count()", 8)]
    [TestCase("x:byte[]=convert('hi there'); y:text = convert(x) ", "hi there")]
    [TestCase("y:text=convert(42)", "42")]
    public void ConstantConvertFunctionTest(string expr, object expected)
        => expr.AssertResultHas("y", expected);


    [TestCase("out:byte = 'a'[0].convert()", (byte)97)]
    [TestCase("out:byte = 'z'[0].convert()", (byte)122)]
    [TestCase("out:byte = '0'[0].convert()", (byte)48)]
    [TestCase("out:byte = 'A'[0].convert()", (byte)65)]
    [TestCase("out:byte = ' '[0].convert()", (byte)32)]
    [TestCase("x:byte = 97;   out:char = x.convert()", 'a')]
    [TestCase("x:byte = 122;  out:char = x.convert()", 'z')]
    [TestCase("x:byte = 48;   out:char = x.convert()", '0')]
    [TestCase("x:byte = 65;   out:char = x.convert()", 'A')]
    [TestCase("x:byte = 32;   out:char = x.convert()", ' ')]
    [TestCase("out:int16 = 'a'[0].convert()", (Int16)97)]
    [TestCase("out:int16 = 'z'[0].convert()", (Int16)122)]
    [TestCase("out:int16 = '0'[0].convert()", (Int16)48)]
    [TestCase("out:int16 = 'A'[0].convert()", (Int16)65)]
    [TestCase("out:int16 = ' '[0].convert()", (Int16)32)]
    [TestCase("out:int16 = 'ы'[0].convert()", (Int16)1099)]
    [TestCase("x:int16 = 97;     out:char = x.convert()", 'a')]
    [TestCase("x:int16 = 122;    out:char = x.convert()", 'z')]
    [TestCase("x:int16 = 48;     out:char = x.convert()", '0')]
    [TestCase("x:int16 = 65;     out:char = x.convert()", 'A')]
    [TestCase("x:int16 = 32;     out:char = x.convert()", ' ')]
    [TestCase("x:int16 = 1099;   out:char = x.convert()", 'ы')]
    [TestCase("out:uint16 = 'a'[0].convert()", (UInt16)97)]
    [TestCase("out:uint16 = 'z'[0].convert()", (UInt16)122)]
    [TestCase("out:uint16 = '0'[0].convert()", (UInt16)48)]
    [TestCase("out:uint16 = 'A'[0].convert()", (UInt16)65)]
    [TestCase("out:uint16 = ' '[0].convert()", (UInt16)32)]
    [TestCase("out:uint16 = 'ы'[0].convert()", (UInt16)1099)]
    [TestCase("x:uint16 = 97;     out:char = x.convert()", 'a')]
    [TestCase("x:uint16 = 122;    out:char = x.convert()", 'z')]
    [TestCase("x:uint16 = 48;     out:char = x.convert()", '0')]
    [TestCase("x:uint16 = 65;     out:char = x.convert()", 'A')]
    [TestCase("x:uint16 = 32;     out:char = x.convert()", ' ')]
    [TestCase("x:uint16 = 1099;   out:char = x.convert()", 'ы')]
    [TestCase("out:int32 = 'a'[0].convert()", 97)]
    [TestCase("out:int32 = 'z'[0].convert()", 122)]
    [TestCase("out:int32 = '0'[0].convert()", 48)]
    [TestCase("out:int32 = 'A'[0].convert()", 65)]
    [TestCase("out:int32 = ' '[0].convert()", 32)]
    [TestCase("out:int32 = 'ы'[0].convert()", 1099)]
    [TestCase("x:int32 = 97;     out:char = x.convert()", 'a')]
    [TestCase("x:int32 = 122;    out:char = x.convert()", 'z')]
    [TestCase("x:int32 = 48;     out:char = x.convert()", '0')]
    [TestCase("x:int32 = 65;     out:char = x.convert()", 'A')]
    [TestCase("x:int32 = 32;     out:char = x.convert()", ' ')]
    [TestCase("x:int32 = 1099;   out:char = x.convert()", 'ы')]
    [TestCase("out:uint32 = 'a'[0].convert()", (UInt32)97)]
    [TestCase("out:uint32 = 'z'[0].convert()", (UInt32)122)]
    [TestCase("out:uint32 = '0'[0].convert()", (UInt32)48)]
    [TestCase("out:uint32 = 'A'[0].convert()", (UInt32)65)]
    [TestCase("out:uint32 = ' '[0].convert()", (UInt32)32)]
    [TestCase("out:uint32 = 'ы'[0].convert()", (UInt32)1099)]
    [TestCase("x:uint32 = 97;     out:char = x.convert()", 'a')]
    [TestCase("x:uint32 = 122;    out:char = x.convert()", 'z')]
    [TestCase("x:uint32 = 48;     out:char = x.convert()", '0')]
    [TestCase("x:uint32 = 65;     out:char = x.convert()", 'A')]
    [TestCase("x:uint32 = 32;     out:char = x.convert()", ' ')]
    [TestCase("x:uint32 = 1099;   out:char = x.convert()", 'ы')]
    [TestCase("out:int64 = 'a'[0].convert()", (Int64)97)]
    [TestCase("out:int64 = 'z'[0].convert()", (Int64)122)]
    [TestCase("out:int64 = '0'[0].convert()", (Int64)48)]
    [TestCase("out:int64 = 'A'[0].convert()", (Int64)65)]
    [TestCase("out:int64 = ' '[0].convert()", (Int64)32)]
    [TestCase("out:int64 = 'ы'[0].convert()", (Int64)1099)]
    [TestCase("x:int64 = 97;     out:char = x.convert()", 'a')]
    [TestCase("x:int64 = 122;    out:char = x.convert()", 'z')]
    [TestCase("x:int64 = 48;     out:char = x.convert()", '0')]
    [TestCase("x:int64 = 65;     out:char = x.convert()", 'A')]
    [TestCase("x:int64 = 32;     out:char = x.convert()", ' ')]
    [TestCase("x:int64 = 1099;   out:char = x.convert()", 'ы')]
    [TestCase("out:uint64 = 'a'[0].convert()", (UInt64)97)]
    [TestCase("out:uint64 = 'z'[0].convert()", (UInt64)122)]
    [TestCase("out:uint64 = '0'[0].convert()", (UInt64)48)]
    [TestCase("out:uint64 = 'A'[0].convert()", (UInt64)65)]
    [TestCase("out:uint64 = ' '[0].convert()", (UInt64)32)]
    [TestCase("out:uint64 = 'ы'[0].convert()", (UInt64)1099)]
    [TestCase("x:uint64 = 97;     out:char = x.convert()", 'a')]
    [TestCase("x:uint64 = 122;    out:char = x.convert()", 'z')]
    [TestCase("x:uint64 = 48;     out:char = x.convert()", '0')]
    [TestCase("x:uint64 = 65;     out:char = x.convert()", 'A')]
    [TestCase("x:uint64 = 32;     out:char = x.convert()", ' ')]
    [TestCase("x:uint64 = 1099;   out:char = x.convert()", 'ы')]
    [TestCase("out:byte[] = 'a'[0].convert()", new byte[] { 97 })]
    [TestCase("out:byte[] = 'z'[0].convert()", new byte[] { 122 })]
    [TestCase("out:byte[] = '0'[0].convert()", new byte[] { 48 })]
    [TestCase("out:byte[] = 'A'[0].convert()", new byte[] { 65 })]
    [TestCase("out:byte[] = ' '[0].convert()", new byte[] { 32 })]
    [TestCase("out:byte[] = 'ы'[0].convert()", new byte[] { 0x4b, 0x04 })]
    [TestCase("x:byte[] = [97];           out:char = x.convert()", 'a')]
    [TestCase("x:byte[] = [122];          out:char = x.convert()", 'z')]
    [TestCase("x:byte[] = [48];           out:char = x.convert()", '0')]
    [TestCase("x:byte[] = [65];           out:char = x.convert()", 'A')]
    [TestCase("x:byte[] = [32];           out:char = x.convert()", ' ')]
    [TestCase("x:byte[] = [0x4b,0x04];    out:char = x.convert()", 'ы')]
    [TestCase("x:byte[] = [97,0];           out:char = x.convert()", 'a')]
    [TestCase("x:byte[] = [122,0];          out:char = x.convert()", 'z')]
    [TestCase("x:byte[] = [48,0];           out:char = x.convert()", '0')]
    [TestCase("x:byte[] = [65,0];           out:char = x.convert()", 'A')]
    [TestCase("x:byte[] = [32,0];           out:char = x.convert()", ' ')]
    [TestCase("x:byte[] = [0x4b,0x04];      out:char = x.convert()", 'ы')]
    public void CharToIntConvert(string expr, object expected)
        => expr.AssertResultHas("out", expected);

    [TestCase("out:real = 'a'[0].convert()", 97)]
    [TestCase("out:real = 'z'[0].convert()", 122)]
    [TestCase("out:real = '0'[0].convert()", 48)]
    [TestCase("out:real = 'A'[0].convert()", 65)]
    [TestCase("out:real = ' '[0].convert()", 32)]
    [TestCase("out:real = 'ы'[0].convert()", 1099)]
    public void CharToRealConvert(string expr, double expected) {
        var res = Funny.WithDialect(realClrType: RealClrType.IsDouble).Calc<double>(expr);
        Assert.AreEqual(expected, res);

        var resDec = Funny.WithDialect(realClrType: RealClrType.IsDecimal).Calc<decimal>(expr);
        Assert.AreEqual(new decimal(expected), resDec);
    }

    [TestCase("x:real = 97;     out:char = x.convert()", 'a')]
    [TestCase("x:real = 122;    out:char = x.convert()", 'z')]
    [TestCase("x:real = 48;     out:char = x.convert()", '0')]
    [TestCase("x:real = 65;     out:char = x.convert()", 'A')]
    [TestCase("x:real = 32;     out:char = x.convert()", ' ')]
    [TestCase("x:real = 1099;   out:char = x.convert()", 'ы')]
    public void RealToCharConvert(string expr, char expected) {
        var res = Funny.WithDialect(realClrType: RealClrType.IsDouble).Calc<char>(expr);
        Assert.AreEqual(expected, res);

        var resDec = Funny.WithDialect(realClrType: RealClrType.IsDecimal).Calc<char>(expr);
        Assert.AreEqual(expected, resDec);
    }

    [TestCase("out:int64    = 127.3.2.1.convert()", (long)0x0102037f)]
    [TestCase("out:uint64   = 127.3.2.1.convert()", (ulong)0x0102037f)]
    [TestCase("out:int      = 127.3.2.1.convert()", (int)0x0102037f)]
    [TestCase("out:uint     = 127.3.2.1.convert()", (uint)0x0102037f)]
    [TestCase("out:int64    = 255.254.253.252.convert()", (long)0xfcfdfeff)]
    [TestCase("out:uint64   = 255.254.253.252.convert()", (ulong)0xfcfdfeff)]
    [TestCase("out:uint     = 255.254.253.252.convert()", (uint)0xfcfdfeff)]
    [TestCase("out:int      = 255.254.253.252.convert()", (int)-50462977)]
    [TestCase("out:byte[]   = 127.3.2.1.convert()", new byte[] { 127, 3, 2, 1 })]
    [TestCase("out:uint16[] = 127.3.2.1.convert()", new ushort[] { 127, 3, 2, 1 })]
    [TestCase("out:uint32[] = 127.3.2.1.convert()", new uint[] { 127, 3, 2, 1 })]
    [TestCase("out:uint64[] = 127.3.2.1.convert()", new UInt64[] { 127, 3, 2, 1 })]
    [TestCase("out:int16[]  = 127.3.2.1.convert()", new Int16[] { 127, 3, 2, 1 })]
    [TestCase("out:int32[]  = 127.3.2.1.convert()", new Int32[] { 127, 3, 2, 1 })]
    [TestCase("out:int64[]  = 127.3.2.1.convert()", new Int64[] { 127, 3, 2, 1 })]
    [TestCase("out:bool[]   = 127.3.2.1.convert()",
        new Boolean[] {
            true, true, true, true, true, true, true, false, true, true, false, false, false, false, false, false,
            false, true, false, false, false, false, false, false, true, false, false, false, false, false, false, false
        })]
    public void FromIpConvert(string expr, object expected) =>
        Funny.Hardcore.Build(expr).Calc().AssertResultHas("out", expected);

    [TestCase("x:int64  = 0x0102037f; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:uint64 = 0x0102037f; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:int    = 0x0102037f; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:uint   = 0x0102037f; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:int64  = 0xfcfdfeff; out:ip = x.convert()", "255.254.253.252")]
    [TestCase("x:uint64 = 0xfcfdfeff; out:ip = x.convert()", "255.254.253.252")]
    [TestCase("x:int    =  -50462977; out:ip = x.convert()", "255.254.253.252")]
    [TestCase("x:uint   = 0xfcfdfeff; out:ip = x.convert()", "255.254.253.252")]
    [TestCase("x:byte[]   = [127,3,2,1]; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:uint16[] = [127,3,2,1]; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:uint32[] = [127,3,2,1]; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:uint64[] = [127,3,2,1]; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:int16[]  = [127,3,2,1]; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:int32[]  = [127,3,2,1]; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("x:int64[]  = [127,3,2,1]; out:ip = x.convert()", "127.3.2.1")]
    [TestCase("out:ip = '127.3.2.1'.convert()", "127.3.2.1")]
    // [TestCase(@"
    //     x:bool[] = [
    //         true, true, true, true, true, true, true,false,
    //         true, true,false,false,false,false,false,false,
    //         false,true,false,false,false,false,false,false,
    //         true,false,false,false,false,false,false,false]
    //     out:ip = x.convert()","127.3.2.1")]
    public void ToIpConvert(string expr, string expectedIp) =>
        Funny.Hardcore.Build(expr).Calc().AssertResultHas("out", IPAddress.Parse(expectedIp));

    [TestCase("out:byte = 'ы'[0].convert()")]
    [TestCase("out:byte = '😃'[0].convert()")]
    public void ObviousFailsWithRuntimeException(string expr) =>
        expr.AssertObviousFailsOnRuntime();
}
