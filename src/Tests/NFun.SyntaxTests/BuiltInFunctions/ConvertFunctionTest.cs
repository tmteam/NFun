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
    [TestCase("int8",  (sbyte)-12, "int16", (short)-12)]
    [TestCase("int8",  (sbyte)123, "int16", (short)123)]
    [TestCase("uint16", (ushort)123, "int16", (short)123)]
    [TestCase("uint32", (uint)123, "int16", (short)123)]
    [TestCase("uint64", (ulong)123, "int16", (short)123)]
    [TestCase("int", (int)-123, "int", -123)]
    [TestCase("int", (int)123, "int", 123)]
    [TestCase("int16", (short)-123, "int", -123)]
    [TestCase("int32", (int)-123, "int", -123)]
    [TestCase("int64", (long)-123, "int", -123)]
    [TestCase("uint8", (byte)123, "int", 123)]
    [TestCase("int8",  (sbyte)-12, "int", -12)]
    [TestCase("int8",  (sbyte)123, "int", 123)]
    [TestCase("uint16", (ushort)123, "int", 123)]
    [TestCase("uint32", (uint)123, "int", 123)]
    [TestCase("uint64", (ulong)123, "int", 123)]
    [TestCase("int", (int)-123, "int64", (long)-123)]
    [TestCase("int", (int)123, "int64", (long)123)]
    [TestCase("int16", (short)-123, "int64", (long)-123)]
    [TestCase("int32", (int)-123, "int64", (long)-123)]
    [TestCase("int64", (long)-123, "int64", (long)-123)]
    [TestCase("uint8", (byte)123, "int64", (long)123)]
    [TestCase("int8",  (sbyte)-12, "int64", (long)-12)]
    [TestCase("int8",  (sbyte)123, "int64", (long)123)]
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
    // → int8 direction (only values that fit -128..127):
    [TestCase("int",    (int)-123,    "int8", (sbyte)-123)]
    [TestCase("int",    (int)123,     "int8", (sbyte)123)]
    [TestCase("int16",  (short)-123,  "int8", (sbyte)-123)]
    [TestCase("int64",  (long)-123,   "int8", (sbyte)-123)]
    [TestCase("uint8",  (byte)123,    "int8", (sbyte)123)]
    [TestCase("uint16", (ushort)123,  "int8", (sbyte)123)]
    [TestCase("uint32", (uint)123,    "int8", (sbyte)123)]
    [TestCase("uint64", (ulong)123,   "int8", (sbyte)123)]
    [TestCase("int8",   (sbyte)-123,  "int8", (sbyte)-123)]
    // ── Float32 conversion matrix (phase 6) ─────────────────────────
    // int → float32 (exact for small ints, ⚠ for u32/u64/i32/i64).
    [TestCase("uint8",  (byte)123,   "float32", 123.0f, Ignore = "Float32 phase 6: int→f32 convert pending")]
    [TestCase("uint16", (ushort)123, "float32", 123.0f, Ignore = "Float32 phase 6: int→f32 convert pending")]
    [TestCase("int8",   (sbyte)-12,  "float32", -12.0f, Ignore = "Float32 phase 6: int→f32 convert pending")]
    [TestCase("int16",  (short)-42,  "float32", -42.0f, Ignore = "Float32 phase 6: int→f32 convert pending")]
    [TestCase("int32",  -1000,       "float32", -1000.0f,Ignore = "Float32 phase 6: int→f32 convert (lossy for large)")]
    [TestCase("uint32", (uint)1000,  "float32", 1000.0f, Ignore = "Float32 phase 6: u32→f32 convert (⚠ lossy for >2^24)")]
    [TestCase("uint64", (ulong)1000, "float32", 1000.0f, Ignore = "Float32 phase 6: u64→f32 convert (⚠ lossy)")]
    [TestCase("int64",  (long)-1000, "float32", -1000.0f,Ignore = "Float32 phase 6: i64→f32 convert (⚠ lossy)")]
    // float32 → wider numeric (real always loss-less).
    [TestCase("float32", 1.5f,       "real",    1.5,    Ignore = "Float32 phase 6: f32→real widening")]
    [TestCase("float32", 1.5f,       "float32", 1.5f,   Ignore = "Float32 phase 6: f32→f32 identity")]
    // real → float32 (⚠ precision loss).
    [TestCase("real",    1.5,        "float32", 1.5f,   Ignore = "Float32 phase 6: real→f32 narrowing")]
    public void ConvertIntegersFunctionsTest(
        string inputType, object inputValue, string outputType,
        object expectedOutput) {
        var expr = $"x:{inputType}; y:{outputType} = convert(x)";
        expr.Calc("x", inputValue).AssertReturns("y", expectedOutput);
    }

    [Ignore("convert() for bool[]->int, int->char, int[]->char not implemented")]
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
    public void ConstantConvertTest(string expr, object expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("y:byte[]='a'[0].convert(); ", new byte[] { 0x61 })]
    [TestCase("y:int= convert(1.2)", 1)]
    [TestCase("y:int= convert(-1.2)", -1)]
    [TestCase("y:int= convert('1')", 1)]
    [TestCase("y:int= convert('-123')", -123)]
    // byte[] → integer deserialization is now strict-length per PRAGMATIC matrix §1.6.
    // Previously short arrays were silently zero-padded via AsByteArray; that masked
    // the strictness the spec promises. Test cases below use exact widths (4 for int32).
    // Length mismatch tests live in ConvertSpecMatrixTest.ByteArrayToIntOpt_WrongLength_ReturnsNone.
    [TestCase("x:byte[]=[0x21,0x33,0x12,0x00]; y:int= convert(x)", 1_192_737)]
    [TestCase("x:byte[]=[0x21,0x00,0x00,0x00]; y:int= convert(x)", 0x21)]
    [TestCase("x:byte[]=[0x21,0x00,0x00,0x00]; y:int= convert(x)", 0x21)]
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

    // real→char is now ✗ per PRAGMATIC matrix §1.3 — see
    // ConvertSpecMatrixTest.RealToChar_StaticReject. Users must convert via an
    // integer first: convert(convert(x):int):char. The previous RealToCharConvert
    // test cases are removed.

    [TestCase("out:int64    = 127.3.2.1.convert()", (long)0x0102037f)]
    [TestCase("out:uint64   = 127.3.2.1.convert()", (ulong)0x0102037f)]
    // ip→int32 is now ✗ per PRAGMATIC matrix §1.4 — see ConvertSpecMatrixTest.IpToInt32_StaticReject.
    // Use :uint or :long instead. The previous test cases that exercised ip→int32 are removed.
    [TestCase("out:uint     = 127.3.2.1.convert()", (uint)0x0102037f)]
    [TestCase("out:int64    = 255.254.253.252.convert()", (long)0xfcfdfeff)]
    [TestCase("out:uint64   = 255.254.253.252.convert()", (ulong)0xfcfdfeff)]
    [TestCase("out:uint     = 255.254.253.252.convert()", (uint)0xfcfdfeff)]
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
    // Negative i32 → ip is now 🪂 per PRAGMATIC matrix §1.4 (was: raw byte
    // reinterpret → "255.254.253.252"). See ConvertSpecMatrixTest.Int32NegativeToIp_Throws
    // and Int32NegativeToIpOpt_ReturnsNone for the new behavior.
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

    // ═══════════════════════════════════════════════════════════════
    // convert text to bool
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ConvertTextToBool_Invalid_Throws() {
        var r = "y:bool = convert('invalid')"
            .BuildWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => r.Run());
    }

    [Test]
    public void ConvertTextToBool_True_Works() {
        var r = "y:bool = convert('true')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(true, r.Get("y"));
    }

    [Test]
    public void ConvertTextToBool_False_Works() {
        var r = "y:bool = convert('false')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(false, r.Get("y"));
    }

    [Test]
    public void ConvertTextToBool_One_Works() {
        var r = "y:bool = convert('1')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(true, r.Get("y"));
    }

    [Test]
    public void ConvertTextToBool_Zero_Works() {
        var r = "y:bool = convert('0')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(false, r.Get("y"));
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug2 — `convert()` to an unsupported destination type used to
    //   throw raw `InvalidOperationException`. After the convert redesign
    //   (PRAGMATIC matrix) unsupported pairs throw FU887 via
    //   `Errors.ConvertNotSupported`. The original test expression
    //   `convert(true):int` is no longer unsupported (C-style bool→int
    //   is now ✓ per matrix §1.2), so we target a pair that is still ✗:
    //   composite → primitive (array → int).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug2_ConvertUnsupportedDest_ThrowsFunnyParseException() {
        Assert.Throws<Exceptions.FunnyParseException>(() => "out:int = convert([1,2,3])".Calc());
    }

    // ───────────────────────────────────────────────────────────────
    // MR7Bug4 — `convert(text):char` and `convert(text):char?` are
    //   compile-rejected with FU887, but per PRAGMATIC matrix (Specs/
    //   Functions.md §convert) text→char is 🪂 (Soft): "char only if
    //   len == 1, throws Oops on failure; :char? returns none on fail".
    //
    //   Other text→primitive conversions work correctly:
    //     convert('42'):int       # ✓ 42
    //     convert('true'):bool    # ✓ true
    //     convert('127.0.0.1'):ip # ✓ Ip
    //
    //   Only text→char is missing from the implementation despite being
    //   in the matrix.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR7Bug4_TextToChar_StrictReturnsFirstChar() {
        "y:char = convert('A')".Calc().AssertResultHas("y", 'A');
    }

    [Test]
    public void MR7Bug4_TextToCharOpt_GoodInput_ReturnsSome() {
        var rt = "y:char? = convert('A')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual('A', rt.Get("y"));
    }

    [Test]
    public void MR7Bug4_TextToCharOpt_MultiChar_ReturnsNone() {
        var rt = "y:char? = convert('AB')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
    }

    // 4d. Control: convert(text):int works correctly.
    [Test]
    public void MR7Bug4_Control_TextToIntWorks() {
        "y:int = convert('42')".AssertResultHas("y", 42);
    }

    // 4e. Control: convert(text):bool works correctly.
    [Test]
    public void MR7Bug4_Control_TextToBoolWorks() {
        "y:bool = convert('true')".AssertResultHas("y", true);
    }

    // ───────────────────────────────────────────────────────────────
    // MR8Bug1 — `convert(arr):T[]?` (optional-array target) crashes with
    //   raw NullReferenceException on element-overflow / range failure,
    //   instead of either the graceful FunnyRuntimeException (matching
    //   the non-optional `T[]` path) or `none` (matching the documented
    //   soft-fallible pattern for `convert():T?` on primitives).
    //
    //     y:byte[]? = convert([1, 500, 3])   # NullReferenceException
    //     y:byte[]  = convert([1, 500, 3])   # FunnyRuntimeException ✓
    //     y:byte?   = convert(1000)          # none ✓
    //
    //   The NRE escapes try/catch. Reproduces with byte[]?, int[]?,
    //   byte?[]?, etc. Only the *optional-array* target path is bad —
    //   the optional-primitive path is fine.
    // ───────────────────────────────────────────────────────────────
    // After fix: SoftFailureConverter returns FunnyNone.Instance (the runtime
    // `none` value) instead of CLR null on overflow/parse failure. Composite
    // opt targets (T[]?, opt structs) read through array/struct paths that
    // dereference the result — null caused NRE; FunnyNone.Instance is safe.
    [Test]
    public void MR8Bug1_ConvertToOptArray_OverflowReturnsNone() {
        var rt = "y:byte[]? = convert([1, 500, 3])"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
    }

    // Control: byte[]? with all-in-range values still produces the array.
    [Test]
    public void MR8Bug1_ConvertToOptArray_GoodValues_ReturnsArray() {
        "y:byte[]? = convert([1, 2, 3])"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", new byte[] { 1, 2, 3 });
    }

    #region Float32AndFloat64 dialect
    // convert() matrix for Float32 — full source × target coverage
    // (int/real/text/bool/char/byte[] ↔ float32).

    // Int → float32 (small ints, exact).
    [TestCase("uint8",  (byte)42,     42.0f)]
    [TestCase("uint16", (ushort)42,   42.0f)]
    [TestCase("uint32", (uint)42,     42.0f)]
    [TestCase("uint64", (ulong)42,    42.0f)]
    [TestCase("int8",   (sbyte)-42,   -42.0f)]
    [TestCase("int16",  (short)-42,   -42.0f)]
    [TestCase("int32",  -42,          -42.0f)]
    [TestCase("int64",  (long)-42,    -42.0f)]
    public void Float32_ConvertFromInt(string sourceType, object sourceValue, float expected) {
        var rt = $"x:{sourceType}; out:float32 = convert(x)".BuildWithFloats();
        rt["x"].Value = sourceValue;
        rt.Run();
        Assert.AreEqual(expected, rt["out"].Value);
    }

    // Int → float32 zero.
    [TestCase("uint8",  (byte)0,      0.0f)]
    [TestCase("int8",   (sbyte)0,     0.0f)]
    [TestCase("int32",  0,            0.0f)]
    [TestCase("int64",  (long)0,      0.0f)]
    public void Float32_ConvertFromInt_Zero(string sourceType, object sourceValue, float expected) {
        var rt = $"x:{sourceType}; out:float32 = convert(x)".BuildWithFloats();
        rt["x"].Value = sourceValue;
        rt.Run();
        Assert.AreEqual(expected, rt["out"].Value);
    }

    // real → float32 (⚠ narrowing).
    [Test]
    public void Float32_ConvertFromReal_ExactValue() {
        var rt = "x:real=1.5\r out:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    // real → float32 with precision loss (past f32's ~7 decimals).
    [Test]
    public void Float32_ConvertFromReal_PrecisionLoss() {
        var rt = "x:real=1.123456789\r out:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        // f32 can't hold 9 decimals; rounds to ~1.1234568.
        Assert.AreEqual((float)1.123456789, rt["out"].Value);
    }

    // real → float32 overflow becomes Infinity (IEEE behavior).
    [Test]
    public void Float32_ConvertFromReal_Overflow_IsInfinity() {
        var rt = "x:real=1e40\r out:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.PositiveInfinity, rt["out"].Value);
    }

    // float32 → float32 identity.
    [Test]
    public void Float32_ConvertToF32_Identity() {
        var rt = "x:float32=3.14\r out:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.14f, rt["out"].Value);
    }

    // float32 → real (widening).
    [Test]
    public void Float32_ConvertToReal_Widen() {
        var rt = "x:float32=1.5\r out:real = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    // float32 → each integer target — value in-range succeeds.
    [TestCase((float)42.0,  "uint8",  (byte)42)]
    [TestCase((float)42.0,  "uint16", (ushort)42)]
    [TestCase((float)42.0,  "uint32", (uint)42)]
    [TestCase((float)42.0,  "uint64", (ulong)42)]
    [TestCase((float)-42.0, "int8",   (sbyte)-42)]
    [TestCase((float)-42.0, "int16",  (short)-42)]
    [TestCase((float)-42.0, "int32",  -42)]
    [TestCase((float)-42.0, "int64",  (long)-42)]
    public void Float32_ConvertToInt_InRange(float sourceValue, string targetType, object expected) {
        var rt = $"x:float32; out:{targetType} = convert(x)".BuildWithFloats();
        rt["x"].Value = sourceValue;
        rt.Run();
        Assert.AreEqual(expected, rt["out"].Value);
    }

    // float32 → int: truncation toward zero (spec Functions.md matrix). Same as real→int.
    [Test]
    public void Float32_ConvertToInt_Truncates() {
        var rt = "x:float32=1.9\r out:int = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToInt_NegativeTruncates() {
        var rt = "x:float32=-1.9\r out:int = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(-1, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToInt_ExactValue() {
        var rt = "x:float32=42.0\r out:int = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(42, rt["out"].Value);
    }

    // float32 → uint8 overflow — 🪂 runtime throw.
    [Test]
    public void Float32_ConvertToByte_Overflow_Throws() {
        var rt = "x:float32=500.0\r out:byte = convert(x)".BuildWithFloats();
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    // float32 → uint8 negative — 🪂 runtime throw.
    [Test]
    public void Float32_ConvertToByte_Negative_Throws() {
        var rt = "x:float32=-1.0\r out:byte = convert(x)".BuildWithFloats();
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    // float32 → int8 overflow.
    [Test]
    public void Float32_ConvertToInt8_Overflow_Throws() {
        var rt = "x:float32=200.0\r out:int8 = convert(x)".BuildWithFloats();
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    // float32 → text (Sonica-invariant formatted).
    [Test]
    public void Float32_ConvertToText_Simple() {
        var rt = "x:float32=3.5\r out:text = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("3.5", rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToText_Integer() {
        var rt = "x:float32=42.0\r out:text = convert(x)".BuildWithFloats();
        rt.Run();
        StringAssert.StartsWith("42", rt["out"].Value.ToString());
    }

    // text → float32.
    [Test]
    public void Float32_ConvertFromText_Simple() {
        var rt = "out:float32 = convert('3.5')".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertFromText_Negative() {
        var rt = "out:float32 = convert('-3.5')".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(-3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertFromText_Invalid_Throws() {
        Assert.Throws<Exceptions.FunnyRuntimeException>(() =>
            "out:float32 = convert('notanumber')".BuildWithFloats().Run());
    }

    // bool → float32 (1/0).
    [Test]
    public void Float32_ConvertFromBool_True_Is1() {
        var rt = "out:float32 = convert(true)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertFromBool_False_Is0() {
        var rt = "out:float32 = convert(false)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(0.0f, rt["out"].Value);
    }

    // float32 → bool (nonzero = true).
    [Test]
    public void Float32_ConvertToBool_Positive_IsTrue() {
        var rt = "x:float32=3.14\r out:bool = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToBool_Negative_IsTrue() {
        var rt = "x:float32=-3.14\r out:bool = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToBool_Zero_IsFalse() {
        var rt = "x:float32=0.0\r out:bool = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    // char → float32.
    [Test]
    public void Float32_ConvertFromChar_CodePoint() {
        var rt = "c = /'A'\r out:float32 = convert(c)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(65.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertFromChar_Zero_CodePoint() {
        var rt = "c = /'0'\r out:float32 = convert(c)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(48.0f, rt["out"].Value);
    }

    // byte[] → float32 (4-byte IEEE binary32).
    [Test]
    public void Float32_ConvertFromByteArray_1_0f() {
        var bytes = System.BitConverter.GetBytes(1.0f);
        var rt = $"x:byte[]=[{bytes[0]},{bytes[1]},{bytes[2]},{bytes[3]}]\r out:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    // float32 → byte[] round trip.
    [Test]
    public void Float32_RoundTrip_Bytes_2_5f() {
        var rt = "a:float32=2.5\r bytes = convert(a)\r out:float32 = convert(bytes)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    // float32 → byte[] length is 4.
    [Test]
    public void Float32_ConvertToByteArray_LengthIs4() {
        var rt = "x:float32=1.0\r out:byte[] = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(4, ((byte[])rt["out"].Value).Length);
    }

    // float32 → char: Java-style. Truncate f32 → int, check codepoint range, cast to char.
    // Valid codepoints (0..65535) succeed.
    [Test]
    public void Float32_ConvertToChar_ValidCodepoint() {
        var rt = "x:float32=65.0\r out:char = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual('A', rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToChar_Overflow_Throws() {
        var rt = "x:float32=99999.0\r out:char = convert(x)".BuildWithFloats();
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => rt.Run());
    }

    // ip → float32 is ✗.
    [Test]
    public void Float32_ConvertFromIp_ParseError() =>
        Assert.Throws<Exceptions.FunnyParseException>(() =>
            "x:ip = 127.0.0.1\r out:float32 = convert(x)".BuildWithFloats());

    // float32 → ip is ✗.
    [Test]
    public void Float32_ConvertToIp_ParseError() =>
        Assert.Throws<Exceptions.FunnyParseException>(() =>
            "x:float32=1.0\r out:ip = convert(x)".BuildWithFloats());

    // NaN handling: float32 → bool → false.
    [Test]
    public void Float32_NaN_ConvertToBool_IsFalse() {
        var rt = "a:float32=0.0\r b:float32=0.0\r nan = a/b\r out:bool = convert(nan)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    // Infinity → text.
    [Test]
    public void Float32_Infinity_ConvertToText() {
        var rt = "a:float32=1.0\r b:float32=0.0\r inf = a/b\r out:text = convert(inf)".BuildWithFloats();
        rt.Run();
        // The exact text representation is culture-dependent; just verify non-empty.
        Assert.IsNotEmpty(rt["out"].Value.ToString());
    }
    #endregion

    #region Soft parse failure with :T? target (WO7 — SoftFailureConverter)

    // Soft failures (FormatException, OverflowException, ArgumentException) on `:T?`
    // targets surface as `none`. Non-soft exceptions remain hard errors.
    [Test]
    public void SoftParseFailure_OptionalTarget_ReturnsNone() {
        var r = "y:int? = convert('not-a-number')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(r.Get("y"));
    }

    #endregion

    #region Convert hard-reject dispatcher (WO8 — Try-shaped TryBuildConverterFn)

    // Hard reject flows through `any` at runtime. ConcreteConverter wraps as FunnyRuntimeException.
    [Test]
    public void HardReject_RuntimeThroughAny_FunnyRuntimeException() =>
        Assert.Throws<Exceptions.FunnyRuntimeException>(()
            => ("x:ip = convert('1.2.3.4')\r"
              + "y:any = x\r"
              + "z:int = convert(y)").Calc());

    // Statically-known (from, to) pairs are hard-rejected at compile time with a directional hint.
    [Test]
    public void HardReject_CompileTime_IpToInt_HintsUint() {
        var ex = Assert.Throws<Exceptions.FunnyParseException>(()
            => "x:ip = convert('1.2.3.4')\ry:int = convert(x)".Calc());
        StringAssert.Contains(":uint", ex.Message);
    }

    [Test]
    public void HardReject_CompileTime_RealToChar_HintsCodepoint() {
        var ex = Assert.Throws<Exceptions.FunnyParseException>(()
            => "y:char = convert(65.0)".Calc());
        StringAssert.Contains("not a codepoint", ex.Message);
    }

    // Wrapping the destination in `:T?` must NOT rescue a hard reject.
    [Test]
    public void HardReject_OptionalTarget_DoesNotRescue() =>
        Assert.Throws<Exceptions.FunnyParseException>(()
            => "x:ip = convert('1.2.3.4')\ry:int? = convert(x)"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));

    #endregion
}
