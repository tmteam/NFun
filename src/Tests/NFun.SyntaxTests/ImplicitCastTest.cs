using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests;

[TestFixture]
public class ImplicitCastTest {
    [TestCase("uint8", (byte)42, "uint8", (byte)42)]
    [TestCase("uint8", (byte)42, "uint16", (ushort)42)]
    [TestCase("uint16", (ushort)42, "uint16", (ushort)42)]
    [TestCase("uint8", (byte)42, "uint32", (uint)42)]
    [TestCase("uint16", (ushort)42, "uint32", (uint)42)]
    [TestCase("uint32", (uint)42, "uint32", (uint)42)]
    [TestCase("uint8", (byte)42, "uint64", (ulong)42)]
    [TestCase("uint16", (ushort)42, "uint64", (ulong)42)]
    [TestCase("uint32", (uint)42, "uint64", (ulong)42)]
    [TestCase("uint64", (ulong)42, "uint64", (ulong)42)]
    [TestCase("int16", (short)42, "int16", (short)42)]
    [TestCase("uint8", (byte)42, "int16", (short)42)]
    [TestCase("int16", (short)42, "int32", (int)42)]
    [TestCase("int32", (int)42, "int32", (int)42)]
    [TestCase("uint8", (byte)42, "int32", (int)42)]
    [TestCase("uint16", (ushort)42, "int32", (int)42)]
    [TestCase("int16", (short)42, "int64", (long)42)]
    [TestCase("int32", (int)42, "int64", (long)42)]
    [TestCase("int64", (long)42, "int64", (long)42)]
    [TestCase("uint8", (byte)42, "int64", (long)42)]
    [TestCase("uint16", (ushort)42, "int64", (long)42)]
    [TestCase("uint32", (uint)42, "int64", (long)42)]
    [TestCase("int16", (short)42, "real", 42.0)]
    [TestCase("int32", (int)42, "real", 42.0)]
    [TestCase("int64", (long)42, "real", 42.0)]
    [TestCase("uint8", (byte)42, "real", 42.0)]
    [TestCase("uint16", (ushort)42, "real", 42.0)]
    [TestCase("uint32", (uint)42, "real", 42.0)]
    [TestCase("uint64", (ulong)42, "real", 42.0)]
    [TestCase("real", 42.2, "real", 42.2)]
    public void Allowed_ImplicitNumbersCast(string typeFrom, object valueFrom, string typeTo, object valueTo) {
        var expr = $"conv(a:{typeTo}):{typeTo} = a; x:{typeFrom}; y = conv(x)";
        expr.Calc("x", valueFrom).AssertReturns("y", valueTo);
    }

    // Float32 widening targets — ints widen to float32 implicitly.
    // Small ints (u8/u16/i8/i16) are exact; wider ints (i32/u32) may lose precision
    // beyond 2^24 but are still permitted (⚠ in spec). Requires FloatFamily opt-in.
    [TestCase("uint8",  (byte)42,    "float32", 42.0f)]
    [TestCase("uint16", (ushort)42,  "float32", 42.0f)]
    [TestCase("int8",   (sbyte)-42,  "float32", -42.0f)]
    [TestCase("int16",  (short)-42,  "float32", -42.0f)]
    [TestCase("int32",  -42,         "float32", -42.0f)]
    [TestCase("float32", 1.5f,       "float32", 1.5f)]
    [TestCase("float32", 1.5f,       "real",    1.5)]
    public void Float32_ImplicitNumbersCast(string typeFrom, object valueFrom, string typeTo, object valueTo) {
        var expr = $"conv(a:{typeTo}):{typeTo} = a; x:{typeFrom}; y = conv(x)";
        var rt = expr.BuildWithFloats();
        rt.Calc(("x", valueFrom)).AssertReturns("y", valueTo);
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
    [TestCase("-1", "int16")]
    [TestCase("-300", "int16")]
    public void Allowed_NumberConstantImplicitCast(string constant, string typeTo) {
        var expr = $"conv(a:{typeTo}):{typeTo} = a; y = conv({constant})";
        var runtime = expr.Build();
        Assert.AreEqual(typeTo.ToLower(), runtime.Variables[0].Type.ToString().ToLower());
    }

    [TestCase("int16", "uint8")]
    [TestCase("int32", "uint8")]
    [TestCase("int64", "uint8")]
    [TestCase("uint16", "uint8")]
    [TestCase("uint32", "uint8")]
    [TestCase("uint64", "uint8")]
    [TestCase("real", "uint8")]
    [TestCase("int8", "uint16")]
    [TestCase("int16", "uint16")]
    [TestCase("int32", "uint16")]
    [TestCase("int64", "uint16")]
    [TestCase("uint32", "uint16")]
    [TestCase("uint64", "uint16")]
    [TestCase("real", "uint16")]
    [TestCase("int8", "uint32")]
    [TestCase("int16", "uint32")]
    [TestCase("int32", "uint32")]
    [TestCase("int64", "uint32")]
    [TestCase("uint64", "uint32")]
    [TestCase("real", "uint32")]
    [TestCase("int8", "uint64")]
    [TestCase("int16", "uint64")]
    [TestCase("int32", "uint64")]
    [TestCase("int64", "uint64")]
    [TestCase("real", "uint64")]
    [TestCase("int16", "int8")]
    [TestCase("int32", "int8")]
    [TestCase("int64", "int8")]
    [TestCase("uint8", "int8")]
    [TestCase("uint16", "int8")]
    [TestCase("uint32", "int8")]
    [TestCase("uint64", "int8")]
    [TestCase("real", "int8")]
    [TestCase("int32", "int16")]
    [TestCase("int64", "int16")]
    [TestCase("uint16", "int16")]
    [TestCase("uint32", "int16")]
    [TestCase("uint64", "int16")]
    [TestCase("real", "int16")]
    [TestCase("int64", "int32")]
    [TestCase("uint32", "int32")]
    [TestCase("uint64", "int32")]
    [TestCase("real", "int32")]
    [TestCase("uint64", "int64")]
    [TestCase("real", "int64")]
    public void ObviousFails_ImplicitNumbersCast(string typeFrom, string typeTo)
        => $"x:{typeFrom}; y:{typeTo} = x".AssertObviousFailsOnParse();


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
        => $"customConvert(a:{typeTo}):{typeTo} = a; y = customConvert({constant})".AssertObviousFailsOnParse();

    // ───────────────────────────────────────────────────────────────
    // MR4Bug3 — `out:byte = 1 + 1` parses with cryptic FU761 "Seems like
    //   expression ` + 1` cannot be used here" instead of a clear type
    //   mismatch FU740 (which is what `out:byte = if(true) 1+1 else 5`
    //   correctly produces). UX issue — the parse error misattributes
    //   the failure to the `+1` token.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug3_ByteAnnotation_OnArithmetic_CrypticErrorCode() {
        var ex = Assert.Throws<FunnyParseException>(() => "out:byte = 1 + 1".Calc());
        // Now produces clean FU740 ("Variable 'out' cannot be initialized ...")
        // instead of cryptic FU761. Assert the FU740 hallmarks.
        StringAssert.Contains("'out'", ex.Message);
        StringAssert.Contains("cannot be initialized", ex.Message);
    }

    #region FloatFamily dialect
    // Rules:
    //   int → f32 : widening (i32/i64 ⚠ lossy but permitted)
    //   f32 → real : widening
    //   real → f32 : NARROWING → parse error

    // Int literal narrows to float32 when caller passes to f32-typed function param.
    [Test]
    public void Float32_IntLiteral_NarrowsToF32_InFunctionCall() {
        var rt = "f(x:float32):float32 = x; y = f(5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(5.0f, rt["y"].Value);
        Assert.AreEqual("Float32", rt["y"].Type.ToString());
    }

    [Test]
    public void Float32_HexLiteral_NarrowsToF32_InFunctionCall() {
        var rt = "f(x:float32):float32 = x; y = f(0x0A)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(10.0f, rt["y"].Value);
    }

    [Test]
    public void Float32_BinaryLiteral_NarrowsToF32_InFunctionCall() {
        var rt = "f(x:float32):float32 = x; y = f(0b1010)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(10.0f, rt["y"].Value);
    }

    // Small unsigned/signed integer inputs widen to float32 via variable assignment.
    [Test]
    public void Float32_ByteInput_WidensToF32() {
        var rt = "x:byte; y:float32 = x".BuildWithFloats();
        rt["x"].Value = (byte)5;
        rt.Run();
        Assert.AreEqual(5.0f, rt["y"].Value);
    }

    [Test]
    public void Float32_Int8Input_WidensToF32() {
        var rt = "x:int8; y:float32 = x".BuildWithFloats();
        rt["x"].Value = (sbyte)-5;
        rt.Run();
        Assert.AreEqual(-5.0f, rt["y"].Value);
    }

    [Test]
    public void Float32_Int16Input_WidensToF32() {
        var rt = "x:int16; y:float32 = x".BuildWithFloats();
        rt["x"].Value = (short)-1000;
        rt.Run();
        Assert.AreEqual(-1000.0f, rt["y"].Value);
    }

    [Test]
    public void Float32_UInt16Input_WidensToF32() {
        var rt = "x:uint16; y:float32 = x".BuildWithFloats();
        rt["x"].Value = (ushort)1000;
        rt.Run();
        Assert.AreEqual(1000.0f, rt["y"].Value);
    }

    // int32 → f32 is allowed but ⚠ lossy beyond 2^24 (exactly representable up to 16_777_216).
    [Test]
    public void Float32_Int32Input_Lossy_WidensToF32() {
        var rt = "x:int32; y:float32 = x".BuildWithFloats();
        rt["x"].Value = -42;
        rt.Run();
        Assert.AreEqual(-42.0f, rt["y"].Value);
    }

    // int64 → f32 is allowed but ⚠ heavily lossy.
    [Test]
    public void Float32_Int64Input_Lossy_WidensToF32() {
        var rt = "x:int64; y:float32 = x".BuildWithFloats();
        rt["x"].Value = 42L;
        rt.Run();
        Assert.AreEqual(42.0f, rt["y"].Value);
    }

    // float32 → real is loss-less widening (implicit).
    [Test]
    public void Float32_ImplicitWidenToReal_ViaAssignment() {
        var rt = "x:float32; y:real = x".BuildWithFloats();
        rt["x"].Value = 1.5f;
        rt.Run();
        Assert.AreEqual(1.5, rt["y"].Value);
    }

    // Passing float32 to `f(a:real):real` — widens implicitly at call site.
    [Test]
    public void Float32_PassToRealParam_ImplicitWiden() {
        var rt = "f(a:real):real = a\r x:float32\r y = f(x)".BuildWithFloats();
        rt["x"].Value = 2.5f;
        rt.Run();
        Assert.AreEqual(2.5, rt["y"].Value);
    }

    // real → float32 assignment is a NARROWING and must fail at parse time.
    [Test]
    public void Float32_RealToF32_Assignment_ParseError() =>
        Assert.Throws<FunnyParseException>(() =>
            "x:real; y:float32 = x".BuildWithFloats());

    // Passing real to float32-typed parameter must fail.
    [Test]
    public void Float32_RealPassedToF32Param_ParseError() =>
        Assert.Throws<FunnyParseException>(() =>
            "f(a:float32):float32 = a\r x:real = 1.5; y = f(x)".BuildWithFloats());

    // uint32 → f32 (⚠ lossy).
    [Test]
    public void Float32_UInt32Input_Lossy_WidensToF32() {
        var rt = "x:uint32; y:float32 = x".BuildWithFloats();
        rt["x"].Value = 1000u;
        rt.Run();
        Assert.AreEqual(1000.0f, rt["y"].Value);
    }

    // uint64 → f32 (⚠ lossy).
    [Test]
    public void Float32_UInt64Input_Lossy_WidensToF32() {
        var rt = "x:uint64; y:float32 = x".BuildWithFloats();
        rt["x"].Value = 1000UL;
        rt.Run();
        Assert.AreEqual(1000.0f, rt["y"].Value);
    }

    // Two-step widening: int → f32 → real.
    [Test]
    public void Float32_IntThroughF32ToReal_ChainedWiden() {
        var rt = "x:int; f:float32 = x; y:real = f".BuildWithFloats();
        rt["x"].Value = 42;
        rt.Run();
        Assert.AreEqual(42.0, rt["y"].Value);
    }
    #endregion
}
