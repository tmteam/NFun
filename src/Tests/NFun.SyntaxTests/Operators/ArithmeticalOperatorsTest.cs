using System;
using NFun.Exceptions;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.Operators;

public class ArithmeticalOperatorsTest {
    [TestCase("y = 2*3", 6)]
    [TestCase("y = -2*-4", 8)]
    [TestCase("y = -2.5*4", -10.0)]
    [TestCase("y = 1.5*-3", -4.5)]
    [TestCase("y:real = 2*3", 6.0)]
    [TestCase("y:real = -2*-4", 8.0)]
    [TestCase("y:real = -2.5*4", -10.0)]
    [TestCase("y:real = 1.5*-3", -4.5)]
    [TestCase("y:real = 1.5*0", 0.0)]
    [TestCase("y:real = -1.5*0", 0.0)]
    [TestCase("y:real = 0*1.5", 0.0)]
    [TestCase("y:real = 0*-1.5", 0.0)]
    [TestCase("y:int64 = 2*3", (Int64)6)]
    [TestCase("y:int64 = -2*-4", (Int64)8)]
    [TestCase("y:int64 = -2*5", (Int64)(-10))]
    [TestCase("y:int64 = 2*-6", (Int64)(-12))]
    [TestCase("y:int64 = 100*0", (Int64)(0))]
    [TestCase("y:int64 = 0*100", (Int64)(0))]
    [TestCase("y:int64 = -100*0", (Int64)(0))]
    [TestCase("y:int64 = 0*-100", (Int64)(0))]
    [TestCase("y:int32 = 2*3", (Int32)6)]
    [TestCase("y:int32 = -2*-4", (Int32)8)]
    [TestCase("y:int32 = -2*5", (Int32)(-10))]
    [TestCase("y:int32 = 2*-6", (Int32)(-12))]
    [TestCase("y:int32 = -100*0", (Int32)(0))]
    [TestCase("y:int32 = 0*-100", (Int32)(0))]
    [TestCase("y:uint64 = 1*1", (UInt64)1)]
    [TestCase("y:uint64 = 2*1", (UInt64)2)]
    [TestCase("y:uint64 = 2*3", (UInt64)6)]
    [TestCase("y:uint64 = 100*0", (UInt64)(0))]
    [TestCase("y:uint64 = 0*100", (UInt64)(0))]
    [TestCase("y:uint64 = 0*0", (UInt64)(0))]
    [TestCase("y:uint32 = 1*1", (UInt32)1)]
    [TestCase("y:uint32 = 2*1", (UInt32)2)]
    [TestCase("y:uint32 = 2*3", (UInt32)6)]
    [TestCase("y:uint32 = 100*0", (UInt32)(0))]
    [TestCase("y:uint32 = 0*100", (UInt32)(0))]
    [TestCase("y:uint32 = 0*0", (UInt32)(0))]
    public void ConstantMultiply(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y = x*3.0", 2.5, 7.5)]
    [TestCase("y = -2.0*x", 1.0, -2.0)]
    [TestCase("y = x*-2", 0, 0)]
    [TestCase("y = x*-2.0", 1.5, -3.0)]
    [TestCase("y:int64  = 2*x", (Int64)3, (Int64)6)]
    [TestCase("y:int32  = x*2", (Int32)1, (Int32)2)]
    [TestCase("y:uint64 = x*3", (UInt64)4, (UInt64)12)]
    [TestCase("y:uint32 = x*3", (UInt32)4, (UInt32)12)]
    public void VarMultiply(string expression, object input, object expected)
        => expression.Calc("x", input).AssertResultHas("y", expected);

    [TestCase("y = 2+3", 5)]
    [TestCase("y = -2+-4", -6)]
    [TestCase("y = -2.5+4", 1.5)]
    [TestCase("y = 1.5+-3", -1.5)]
    [TestCase("y:real = 2+3", 5.0)]
    [TestCase("y:real = -2+-4", -6.0)]
    [TestCase("y:real = -2.5+4", 1.5)]
    [TestCase("y:real = 1.5+-3", -1.5)]
    [TestCase("y:real = 1.5+0", 1.5)]
    [TestCase("y:real = -1.5+0", -1.5)]
    [TestCase("y:real = 0+1.5", 1.5)]
    [TestCase("y:real = 0+-1.5", -1.5)]
    [TestCase("y:int64 = 2+3", (Int64)5)]
    [TestCase("y:int64 = -2+-4", (Int64)(-6))]
    [TestCase("y:int64 = -2+5", (Int64)(3))]
    [TestCase("y:int64 = 2+-6", (Int64)(-4))]
    [TestCase("y:int64 = 100+0", (Int64)(100))]
    [TestCase("y:int64 = 0+100", (Int64)(100))]
    [TestCase("y:int64 = -100+0", (Int64)(-100))]
    [TestCase("y:int64 = 0+-100", (Int64)(-100))]
    [TestCase("y:int32 = 2+3", (Int32)(5))]
    [TestCase("y:int32 = -2+-4", (Int32)(-6))]
    [TestCase("y:int32 = -2+5", (Int32)(3))]
    [TestCase("y:int32 = 2+-6", (Int32)(-4))]
    [TestCase("y:int32 = -100+0", (Int32)(-100))]
    [TestCase("y:int32 = 0+-100", (Int32)(-100))]
    [TestCase("y:uint64 = 2+1", (UInt64)3)]
    [TestCase("y:uint64 = 2+3", (UInt64)5)]
    [TestCase("y:uint64 = 100+0", (UInt64)(100))]
    [TestCase("y:uint64 = 0+100", (UInt64)(100))]
    [TestCase("y:uint64 = 0+0", (UInt64)(0))]
    [TestCase("y:uint32 = 2+1", (UInt32)3)]
    [TestCase("y:uint32 = 2+3", (UInt32)5)]
    [TestCase("y:uint32 = 100+0", (UInt32)(100))]
    [TestCase("y:uint32 = 0+100", (UInt32)(100))]
    [TestCase("y:uint32 = 0+0", (UInt32)(0))]
    [TestCase("y:int32 = 2+(-2147483647)", -2147483645)]
    [TestCase("y:int64 = 2+(-9223372036854775808)", -9223372036854775806)]
    public void ConstantAddition(string expression, object expected)
        => expression.AssertReturns("y", expected);


    [TestCase("y = x+3", 2, 5)]
    [TestCase("y = -2+x", 1, -1)]
    [TestCase("y = x+-2", 0, -2)]
    [TestCase("y = x+-2", 1, -1)]
    [TestCase("y:int64  = 2+x", (Int64)3, (Int64)5)]
    [TestCase("y:int32  = x+2", (Int32)1, (Int32)3)]
    [TestCase("y:uint64 = x+3", (UInt64)4, (UInt64)7)]
    [TestCase("y:uint32 = x+3", (UInt32)4, (UInt32)7)]
    public void VarAddition(string expression, object input, object expected)
        => expression.Calc("x", input).AssertResultHas("y", expected);


    [TestCase("y = 2-3", -1)]
    [TestCase("y = -2.5-4", -6.5)]
    [TestCase("y:real = 2-3", -1.0)]
    [TestCase("y:real = -2.5-4", -6.5)]
    [TestCase("y:real = 1.5-0", 1.5)]
    [TestCase("y:real = -1.5-0", -1.5)]
    [TestCase("y:real = 0-1.5", -1.5)]
    [TestCase("y:int64 = 2-3", (Int64)(-1))]
    [TestCase("y:int64 = -2-5", (Int64)(-7))]
    [TestCase("y:int64 = 100-0", (Int64)(100))]
    [TestCase("y:int64 = 0-100", (Int64)(-100))]
    [TestCase("y:int64 = -100-0", (Int64)(-100))]
    [TestCase("y:int32 = 2-3", (Int32)(-1))]
    [TestCase("y:int32 = -2-5", (Int32)(-7))]
    [TestCase("y:int32 = -100-0", (Int32)(-100))]
    [TestCase("y:uint64 = 2-1", (UInt64)1)]
    [TestCase("y:uint64 = 100-0", (UInt64)(100))]
    [TestCase("y:uint64 = 0-0", (UInt64)(0))]
    [TestCase("y:uint32 = 2-1", (UInt32)1)]
    [TestCase("y:uint32 = 100-0", (UInt32)(100))]
    [TestCase("y:uint32 = 0-0", (UInt32)(0))]
    [TestCase("y:int32 = 2-2147483646", (Int32)(-2147483644))]
    [TestCase("y:int64 = 2-9223372036854775807", (Int64)(-9223372036854775805))]
    [TestCase("y:int64 = 9223372036854775807-9223372036854775807", (Int64)(0))]
    public void ConstantSubstraction(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y = x-3.0", 2.5, -0.5)]
    [TestCase("y = -2.0-x", 1.0, -3.0)]
    [TestCase("y = x-2.0", 0.0, -2.0)]
    [TestCase("y:real = x-3", 2.5, -0.5)]
    [TestCase("y:real = -2-x", 1.0, -3.0)]
    [TestCase("y:real = x-2", 0.0, -2.0)]
    [TestCase("y:int64  = 2-x", (Int64)3, (Int64)(-1))]
    [TestCase("y:int32  = x-2", (Int32)1, (Int32)(-1))]
    [TestCase("y:uint64 = x-3", (UInt64)4, (UInt64)1)]
    [TestCase("y:uint32 = x-3", (UInt32)4, (UInt32)1)]
    public void VarSubstract(string expression, object input, object expected)
        => expression.Calc("x", input).AssertResultHas("y", expected);

    [TestCase("y = 4/2", 2.0)]
    [TestCase("y = 2/4", 0.5)]
    [TestCase("y = -2/4", -0.5)]
    [TestCase("y = -2/-4", 0.5)]
    [TestCase("y = 2/-4", -0.5)]
    [TestCase("y = 0/4", 0.0)]
    [TestCase("y = 3/1.5", 2.0)]
    [TestCase("y = 4.5/1.5", 3.0)]
    public void ConstantDivision(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("2//x", 2, 1)]
    [TestCase("out:int64 = 2//x", (long)2, (long)1)]
    [TestCase("out:uint64 = 2//x", (ulong)2, (ulong)1)]
    [TestCase("out:int16 = 2//x", (Int16)2, (Int16)1)]
    [TestCase("out:int8  = 2//x", (sbyte)2, (sbyte)1)]
    [TestCase("out:byte = 2//x", (byte)2, (byte)1)]
    [TestCase("out:int = 2//x", (int)2, (int)1)]
    [TestCase("out:uint16 = 2//x", (UInt16)2, (UInt16)1)]
    [TestCase("out:uint64 = 2//x", (ulong)2, (ulong)1)]
    [TestCase("out:uint = 2//x", (uint)3, (uint)0)]
    [TestCase("out:int16 = x//4", (Int16)3, (Int16)0)]
    public void DivisionInt(string expr, object argument, object expected) =>
        expr.Build().Calc("x", argument).AssertAnonymousOut(expected);

    [TestCase("2%x", 2, 0)]
    [TestCase("out:int64 = 2%x", (long)3, (long)2)]
    [TestCase("out:uint64 = 2%x", (ulong)1, (ulong)0)]
    [TestCase("out:int16 = 2%x", (Int16)5, (Int16)2)]
    [TestCase("out:int8  = 2%x", (sbyte)5, (sbyte)2)]
    [TestCase("out:byte = 2%x", (byte)2, (byte)0)]
    [TestCase("out:uint16 = 2%x", (UInt16)2, (UInt16)0)]
    [TestCase("out:uint64 = 2%x", (ulong)2, (ulong)0)]
    [TestCase("out:uint = 8%x", (uint)3, (uint)2)]
    [TestCase("out:int16 = x%4", (Int16)1, (Int16)1)]
    [TestCase("out:real  = x%4", 5.5, 1.5)]
    public void Remainder(string expr, object argument, object expected) =>
        expr.Build().Calc("x", argument).AssertAnonymousOut(expected);

    [TestCase("y = x/3", 1.5, 0.5)]
    [TestCase("y = x/3", -3.0, -1.0)]
    [TestCase("y = x/-3", -3.0, 1.0)]
    [TestCase("y = -x/-3", -3.0, -1.0)]
    [TestCase("y = -2/x", 1.0, -2.0)]
    [TestCase("y = -2/x", -2.0, 1.0)]
    [TestCase("y = -2/-x", -2.0, -1.0)]
    public void VarDivision(string expression, object input, object expected)
        => expression.Calc("x", input).AssertResultHas("y", expected);

    [TestCase("y = 4**2", 16)]
    [TestCase("y = 2**4", 16)]
    [TestCase("y = 0**4", 0)]
    [TestCase("y = 0**0", 1)]
    [TestCase("y = 2**0", 1)]
    [TestCase("y = 0.1**0", 1.0)]
    [TestCase("y = 1.5**2", 2.25)]
    [TestCase("y = 4.5**1.5", 9.5459415460183923)]
    [TestCase("y:int = 2**10", 1024)]
    [TestCase("y:int = 3**3", 27)]
    [TestCase("y = 2**(-1)", 0.5)]
    [TestCase("y:int64 = 2**32", (Int64)4294967296)]
    public void ConstantPow(string expression, object expected)
        => expression.AssertReturns("y", expected);

    // Unary `-` binds LESS tightly than `**` (math convention, Python/Ruby):
    // `-2**x` parses as `-(2**x)`. Previously was `(-2)**x` per the older
    // precedence table; spec corrected and behavior aligned. Math-Sugar's
    // claim that `²` ≡ `**` now actually holds.
    [TestCase("y = -2**x", 2.0, -4.0)]
    [TestCase("y = -2**-x", 1.0, -0.5)]   // -(2**(-1)) = -(0.5) = -0.5 (same as before; (-2)**(-1) was also -0.5)
    [TestCase("y = x**2", (Int32)1, (Int32)1)]
    [TestCase("y = x**2", (Int32)0, (Int32)0)]
    [TestCase("y = x**2", (Int32)2, (Int32)4)]
    [TestCase("y:real = x**2", 1.0, 1.0)]
    [TestCase("y:real = x**2", 0.0, 0.0)]
    [TestCase("y:real = x**2", 2.0, 4.0)]
    [TestCase("y = x**-1", 1.0, 1.0)]
    [TestCase("y = x**x", 0.0, 1.0)]
    [TestCase("y:int32 = x**2", (Int32)3, (Int32)9)]
    public void VarPow(string expression, object input, object expected)
        => expression.Calc("x", input).AssertResultHas("y", expected);

    [TestCase("y = -1", -1)]
    [TestCase("y:real  = -(1)", (double)(-1.0))]
    [TestCase("y:int64 = -(1)", (Int64)(-1.0))]
    [TestCase("y:int32 = -(1)", (Int32)(-1.0))]
    [TestCase("y:int16 = -(1)", (Int16)(-1.0))]
    [TestCase("y:int8  = -(1)", (sbyte)(-1))]
    // Hex/bin negative literal binning admits Int8 (the bin starts at I8, not I16).
    [TestCase("y:int8 = -0x05",   (sbyte)(-5))]
    [TestCase("y:int8 = -0b0101", (sbyte)(-5))]
    [TestCase("y = -0x1 ", -1)]
    [TestCase("y = -(-1)", 1)]
    [TestCase("y = -(-(-1))", -1)]
    public void ConstantNegate(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y = 1 + 4/2 + 3 +2*3 -1", 11.0)]
    [TestCase("y = 1 + (1 + 4)/2 - (3 +2)*(3 -1)", -6.5)]
    [TestCase("y = -(1+2)", -3)]
    [TestCase("y = -2*(-4+2)", 4)]
    public void ConstantExpression(string expression, object expected)
        => expression.AssertReturns("y", expected);

    [TestCase("y = x%3", 2, 2)]
    [TestCase("y = x%4", 5, 1)]
    [TestCase("y = x%-4", 5, 1)]
    [TestCase("y = x%4", -5, -1)]
    [TestCase("y = x%-4", -5, -1)]
    [TestCase("y = x%4", -5, -1)]
    [TestCase("y = -(-(-x))", 2.0, -2.0)]
    public void SingleIntVariableEquation(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("y", expected);


    [TestCase("y = (x + 4.0/x)", 2, 4)]
    [TestCase("y = x % 3.0", 2, 2)]
    [TestCase("y = x % 4.0", 5, 1)]
    [TestCase("y = x % -4.0", 5, 1)]
    [TestCase("y = x % 4.0", -5, -1)]
    [TestCase("y = x % -4.0", -5, -1)]
    [TestCase("y = x % 4.0", -5, -1)]
    [TestCase("y = x % 2.0", -5.2, -1.2)]
    [TestCase("y = 5.0 % x", 2.2, 0.6)]
    [TestCase("y:real = -x ", 0.3, -0.3)]
    [TestCase("y = -(-(-1.0*x))", 2, -2)]
    public void SingleRealVariableEquation(string expr, double arg, double expected) =>
        expr.Calc("x", arg).AssertReturns("y", expected);

    [TestCase("y = x1+x2", 2.0, 3.0, 5.0)]
    [TestCase("y = 2*x1*x2", 3, 6, 36)]
    [TestCase("y = x1*4/x2", 2.0, 2.0, 4.0)]
    [TestCase("y = (x1+x2)/4", 2.0, 2.0, 1.0)]
    public void TwoVariablesEquation(string expr, object arg1, object arg2, object expected) =>
        expr.Calc(("x1", arg1), ("x2", arg2)).AssertResultHas("y", expected);

    // Integer overflow with checked arithmetic (default dialect) → runtime error
    [TestCase("y:uint64 = 2-3")]
    [TestCase("y:uint64 = 0-100")]
    [TestCase("y:uint32 = 2-3")]
    [TestCase("y:uint32 = 0xFFFF_FFFF+13")]
    [TestCase("y:int32  = 2_147_483_647 + 1")]
    [TestCase("y:int32  = 2_147_483_647 * 2")]
    [TestCase("y:int64  = 9_223372_036854_775807 * 2")]
    [TestCase("y:int64  = 9_223372_036854_775807 + 1")]
    [TestCase("y:uint32 = 0-100")]
    public void IntegerOverflow_CheckedArithmetic_ThrowsRuntimeError(string expression)
        => expression.AssertObviousFailsOnRuntime();

    [TestCase("y = /2")]
    [TestCase("y = *2")]
    [TestCase("y = 2++")]
    [TestCase("y = 2//")]
    [TestCase("y = //2")]
    [TestCase("y = //2//")]
    [TestCase("y = 1//2//")]
    [TestCase("y = 1///2")]
    [TestCase("y = 1////2")]
    [TestCase("y = ++2")]
    [TestCase("y = 2--")]
    [TestCase("y = --2")]
    [TestCase("y = 0x123GG")]
    [TestCase("y = 2+ 3 + 4 +")]
    [TestCase("y = 2++x")]
    [TestCase("y = x++2")]
    // Note: `2--x` is now valid: parsed as `2 - (-x)` (Bug10 fix)
    [TestCase("y = *2a")]
    [TestCase("y = x+2+ 3 + 4 +")]
    [TestCase("y=0.*1")]
    [TestCase("x*2 \rx*3")]
    [TestCase("y = 2%%")]
    [TestCase("y = %%2")]
    [TestCase("y = =a")]
    [TestCase("y = =a")]
    [TestCase("y = \"")]
    [TestCase("y = 0x")]
    [TestCase("y = 0b")]
    [TestCase("y = 0..2")]
    [TestCase("1 2")]
    [TestCase("1 \r2")]
    [TestCase("=x*2")]
    [TestCase("y = y")]
    [TestCase("y = y+x")]
    [TestCase("a: int a=4")]
    [TestCase("a:int = 2/4")]
    public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();

    // ═══════════════════════════════════════════════════════════════
    // Binary minus before unary minus: `5 - -3` = 8
    // ═══════════════════════════════════════════════════════════════

    [TestCase("5 - -3", 8)]
    [TestCase("10 - -10", 20)]
    [TestCase("0 - -1", 1)]
    public void BinaryMinusBeforeUnaryMinus_Works(string expr, object expected) {
        expr.AssertReturns(expected);
    }

    [TestCase("5 + -3", 2)]
    [TestCase("5 * -3", -15)]
    [TestCase("10 / -2", -5.0)]
    public void OtherOperatorsBeforeUnaryMinus_StillWork(string expr, object expected) {
        expr.AssertReturns(expected);
    }

    [Test]
    public void DoubleNegation_StillForbidden() {
        Assert.Throws<FunnyParseException>(() => "--3".Build());
    }

    [Test]
    public void DoubleNegationWithSpace_StillForbidden() {
        Assert.Throws<FunnyParseException>(() => "- -3".Build());
    }

    [TestCase("y = x - -1", 2, 3)]
    [TestCase("y = x - -x", 5.0, 10.0)]
    public void BinaryMinusUnaryMinus_WithVariables(string expr, object x, object expected) {
        if (x is int xi)
            expr.Calc("x", xi).AssertResultHas("y", expected);
        else
            expr.Calc("x", (double)x).AssertResultHas("y", expected);
    }

    // ═══════════════════════════════════════════════════════════════
    // Arithmetic on bool should be a type error
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void ArithmeticOnBool_TypeError() {
        Assert.Throws<FunnyParseException>(() => "y = true + 1".Calc());
    }

    // ───────────────────────────────────────────────────────────────
    // MR2Bug4 — Arithmetic operators (`+`, `-`, `*`) over-promote
    //   sub-Arithmetics operands to Real in two corners; `%` does not
    //   promote at all. All three are facets of the same gap in
    //   TIC preferred-type resolution for operands below the
    //   Arithmetics range (`int32 | uint32 ≤ T ≤ real`):
    //
    //   Over-promote → Real (should be Int32 / UInt32):
    //     y:byte = 5, z:byte = 5; out = y + z       → out:Real     (uint16+uint16 → Int32, so byte should too)
    //     y:int16 = -5, z:int16 = -5; out = y + z   → out:Real     (int16+int16 pos → Int32)
    //
    //   No promotion (operator definition says Arithmetics but accepts byte):
    //     y:byte = 5, z:byte = 2; out = y % z       → out:UInt8    (% is Arithmetics per spec)
    //
    //   Filed as one bug since the underlying inference defect — TIC
    //   defaulting to the top of Arithmetics (Real) for some preferred
    //   chains while leaving others entirely unpromoted — is the same.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR2Bug4a_BytePlusByte_OverPromotesToReal() {
        "y:byte = 5\rz:byte = 5\rout = y + z".AssertReturns(
            ("y", (byte)5), ("z", (byte)5), ("out", 10));
    }

    [Test]
    public void MR2Bug4b_NegInt16PlusNegInt16_OverPromotesToReal() {
        "y:int16 = -5\rz:int16 = -5\rout = y + z".AssertReturns(
            ("y", (short)-5), ("z", (short)-5), ("out", -10));
    }

    // (c) was filed as a bug but turned out to be a spec/impl mismatch: `%` uses
    // GenericConstrains.Numbers in the implementation (any numeric type, no widening),
    // so byte%byte → byte is correct. Spec table updated to say `%` is Numbers,
    // not Arithmetics. Test asserts the correct byte-typed result.
    [Test]
    public void MR2Bug4c_ByteModuloByte_KeepsByteType() {
        "y:byte = 5\rz:byte = 2\rout = y % z".AssertReturns(
            ("y", (byte)5), ("z", (byte)2), ("out", (byte)1));
    }

    // Unary negate keeps the operand's signed width (no widening to Int32) —
    // SignedNumber constraint admits Int8 as concrete descendant.
    [Test]
    public void Negate_Int8_StaysInt8() {
        var rt = Funny.Hardcore.Build("x:int8 = -5\nout = -x");
        rt.Run();
        Assert.AreEqual("Int8", rt["out"].Type.ToString());
        Assert.AreEqual((sbyte)5, rt["out"].Value);
    }

    // ─── Float32 arithmetic ──────────────────────────

    [Test]
    public void Float32PlusFloat32_StaysFloat32() =>
        "x:float32 = 1.5\rz:float32 = 2.5\rout = x + z".CalcWithFloats()
            .AssertResultHas(("x", 1.5f), ("z", 2.5f), ("out", 4.0f));

    [Test]
    public void Float32MinusFloat32_StaysFloat32() =>
        "x:float32 = 5.0\rz:float32 = 1.5\rout = x - z".CalcWithFloats()
            .AssertResultHas(("x", 5.0f), ("z", 1.5f), ("out", 3.5f));

    [Test]
    public void Float32MultiplyFloat32_StaysFloat32() =>
        "x:float32 = 1.5\rz:float32 = 2.0\rout = x * z".CalcWithFloats()
            .AssertResultHas(("x", 1.5f), ("z", 2.0f), ("out", 3.0f));

    [Test, Ignore("Float32 phase 5: real-divide '/' is Real-only concrete; needs generic Floats")]
    public void Float32DivideFloat32_StaysFloat32() =>
        "x:float32 = 5.0\rz:float32 = 2.0\rout = x / z".CalcWithFloats()
            .AssertResultHas(("x", 5.0f), ("z", 2.0f), ("out", 2.5f));

    [Test]
    public void Float32ModulusFloat32_StaysFloat32() =>
        "x:float32 = 5.5\rz:float32 = 2.0\rout = x % z".CalcWithFloats()
            .AssertResultHas(("x", 5.5f), ("z", 2.0f), ("out", 1.5f));

    // Pow with non-const-int exponent forces T=Real per TIC convention (line 1118 in
    // TicSetupVisitor). Float32 inputs widen to Real for the result.
    [Test]
    public void Float32PowerFloat32_WidensToReal() {
        var rt = "x:float32 = 2.0\rz:float32 = 3.0\rout = x ** z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(8.0, rt["out"].Value);
    }

    [Test]
    public void Negate_Float32_StaysFloat32() {
        var rt = "x:float32 = 1.5\rout = -x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-1.5f, rt["out"].Value);
    }

    [Test]
    public void IntPlusFloat32_WidensToFloat32() {
        var rt = "x:int = 1\rz:float32 = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32PlusReal_WidensToReal() {
        var rt = "x:float32 = 1.5\rz:real = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(4.0, rt["out"].Value);
    }

    [Test]
    public void NegateThenAssignBack_Int8_NoNarrowingError() {
        var rt = Funny.Hardcore.Build("x:int8 = -5\nout:int8 = -x");
        rt.Run();
        Assert.AreEqual((sbyte)5, rt["out"].Value);
    }

    // ───────────────────────────────────────────────────────────────
    // MR4Bug7 — `**` is now right-associative: `2**3**2 = 2**(3**2) = 512`,
    // matching math/Python/Ruby/JS/Fortran/Haskell/Lua convention.
    // Previously was left-associative (= 64).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR4Bug7_PowerOperator_RightAssociative_PerMathConvention() {
        "out = 2**3**2".AssertResultHas("out", 512.0);
    }

    // ───────────────────────────────────────────────────────────────
    // MR7Bug1 — Struct-in-array field arithmetic loses real widening
    //   (ORDER-DEPENDENT):
    //
    //     data:{x:int, y:real}[] = [{x=1, y=2.5}]
    //     out = data[0].x + data[0].y    # Int32=3  ← BUG (should be Real=3.5)
    //     out = data[0].y + data[0].x    # Real=3.5 ← correct, same operands swapped
    //
    //   Workarounds:
    //     d = data[0]; out = d.x + d.y   # correct
    //     direct struct (no [] indirection) works correctly
    //
    //   Likely the same Preferred-propagation issue as MR2Bug4 family —
    //   when the int field is accessed FIRST through `[idx].x` chain, the
    //   subsequent `+ data[0].y` doesn't widen to real correctly.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR7Bug1_StructArrayFieldArith_IntPlusReal_Truncates() {
        "data:{x:int, y:real}[] = [{x=1, y=2.5}]\rout = data[0].x + data[0].y"
            .Calc()
            .AssertResultHas("out", 3.5);
    }

    [Test]
    public void MR7Bug1_StructArrayFieldArith_MapIntPlusReal_Truncates() {
        "data:{x:int, y:real}[] = [{x=1, y=2.5}]\rout = data.map(rule it.x + it.y)"
            .Calc()
            .AssertResultHas("out", new[] { 3.5 });
    }

    // 1c. Order-swap control: real+int works correctly. Locks the order-sensitivity.
    [Test]
    public void MR7Bug1_OrderSwap_RealPlusInt_StillCorrect() {
        "data:{x:int, y:real}[] = [{x=1, y=2.5}]\rout = data[0].y + data[0].x"
            .Calc()
            .AssertResultHas("out", 3.5);
    }

    // 1d. Workaround: extract intermediate variable.
    [Test]
    public void MR7Bug1_Workaround_ExtractIntermediate() {
        "data:{x:int, y:real}[] = [{x=1, y=2.5}]\rd = data[0]\rout = d.x + d.y"
            .Calc()
            .AssertResultHas("out", 3.5);
    }

    // 1e. Direct struct (no array indirection) is unaffected.
    [Test]
    public void MR7Bug1_Control_DirectStructWorks() {
        "d:{x:int, y:real} = {x=1, y=2.5}\rout = d.x + d.y"
            .Calc()
            .AssertResultHas("out", 3.5);
    }

    // ───────────────────────────────────────────────────────────────
    // MR7Bug2 — Same family as MR7Bug1, but int+int64 → runtime overflow
    //   crash instead of silent truncation:
    //
    //     data:{x:int, y:int64}[] = [{x=1, y=4000000000}]
    //     out = data[0].x + data[0].y
    //     # Runtime: "Value was either too large or too small for an Int32"
    //
    //   The TIC infers Int32 for the result; runtime tries to fit the int64
    //   value 4000000000 (> int32 max) into int32 → crash.
    //   Same root cause as MR7Bug1.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR7Bug2_StructArrayFieldArith_IntPlusInt64_CrashesOnOverflow() {
        "data:{x:int, y:int64}[] = [{x=1, y=4000000000}]\rout = data[0].x + data[0].y"
            .Calc()
            .AssertResultHas("out", 4000000001L);
    }

    #region Float32AndFloat64 dialect

    // ─── Literal narrowing: real → float32 ─────────────────────────

    [Test]
    public void Float32_Literal_ZeroPointZero() {
        var rt = "out:float32 = 0.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(0.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_OnePointZero() {
        var rt = "out:float32 = 1.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_NegativeOnePointZero() {
        var rt = "out:float32 = -1.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_Pi() {
        var rt = "out:float32 = 3.14".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.14f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_NegativePi() {
        var rt = "out:float32 = -3.14".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-3.14f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_ScientificLarge() {
        var rt = "out:float32 = 1e10".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1e10f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_ScientificSmall() {
        var rt = "out:float32 = 1e-10".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1e-10f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_NearF32Max() {
        var rt = "out:float32 = 1e38".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1e38f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_UnderscoreSeparators() {
        var rt = "out:float32 = 1_000_000.5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1_000_000.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_ScientificMantissa() {
        var rt = "out:float32 = 3.14e2".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(314.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_NegativeZero() {
        var rt = "out:float32 = -0.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        // Bitwise negative zero survives narrowing
        Assert.AreEqual(-0.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Literal_TinyPositiveScientific() {
        var rt = "out:float32 = 3.14e-3".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.14e-3f, rt["out"].Value);
    }

    // ─── Int literal widening to F32 ─────────────────────────────────

    [Test]
    public void Float32_IntLiteral_5() {
        var rt = "out:float32 = 5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(5.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntLiteral_Negative5() {
        var rt = "out:float32 = -5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-5.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntLiteral_Zero() {
        var rt = "out:float32 = 0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(0.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntLiteral_127() {
        var rt = "out:float32 = 127".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(127.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntLiteral_OneMillion() {
        var rt = "out:float32 = 1000000".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1000000.0f, rt["out"].Value);
    }

    // Hex/bin literals to F32
    [Test]
    public void Float32_HexLiteral_FF() {
        var rt = "out:float32 = 0xFF".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(255.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_HexLiteral_Negative10() {
        var rt = "out:float32 = -0x10".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-16.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_BinLiteral_1010() {
        var rt = "out:float32 = 0b1010".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(10.0f, rt["out"].Value);
    }

    // ─── Binary arithmetic on float32 × float32 ────────────────────

    // Plus (+)
    [Test]
    public void Float32_Plus_15_25() =>
        "out:float32 = 1.5 + 2.5".BuildWithFloats().AssertOutFloat32("out", 4.0f);

    [Test]
    public void Float32_Plus_10_3() =>
        "out:float32 = 10.0 + 3.0".BuildWithFloats().AssertOutFloat32("out", 13.0f);

    [Test]
    public void Float32_Plus_Neg5_3() =>
        "out:float32 = -5.0 + 3.0".BuildWithFloats().AssertOutFloat32("out", -2.0f);

    [Test]
    public void Float32_Plus_5_Neg3() =>
        "out:float32 = 5.0 + -3.0".BuildWithFloats().AssertOutFloat32("out", 2.0f);

    [Test]
    public void Float32_Plus_0_5() =>
        "out:float32 = 0.0 + 5.0".BuildWithFloats().AssertOutFloat32("out", 5.0f);

    [Test]
    public void Float32_Plus_5_HalfPoint5() =>
        "out:float32 = 5.0 + 0.5".BuildWithFloats().AssertOutFloat32("out", 5.5f);

    [Test]
    public void Float32_Plus_15_15() =>
        "out:float32 = 1.5 + 1.5".BuildWithFloats().AssertOutFloat32("out", 3.0f);

    // Minus (-)
    [Test]
    public void Float32_Minus_15_25() =>
        "out:float32 = 1.5 - 2.5".BuildWithFloats().AssertOutFloat32("out", -1.0f);

    [Test]
    public void Float32_Minus_10_3() =>
        "out:float32 = 10.0 - 3.0".BuildWithFloats().AssertOutFloat32("out", 7.0f);

    [Test]
    public void Float32_Minus_Neg5_3() =>
        "out:float32 = -5.0 - 3.0".BuildWithFloats().AssertOutFloat32("out", -8.0f);

    [Test]
    public void Float32_Minus_5_Neg3() =>
        "out:float32 = 5.0 - -3.0".BuildWithFloats().AssertOutFloat32("out", 8.0f);

    [Test]
    public void Float32_Minus_0_5() =>
        "out:float32 = 0.0 - 5.0".BuildWithFloats().AssertOutFloat32("out", -5.0f);

    [Test]
    public void Float32_Minus_5_HalfPoint5() =>
        "out:float32 = 5.0 - 0.5".BuildWithFloats().AssertOutFloat32("out", 4.5f);

    [Test]
    public void Float32_Minus_15_15() =>
        "out:float32 = 1.5 - 1.5".BuildWithFloats().AssertOutFloat32("out", 0.0f);

    // Multiply (*)
    [Test]
    public void Float32_Multiply_15_25() =>
        "out:float32 = 1.5 * 2.5".BuildWithFloats().AssertOutFloat32("out", 3.75f);

    [Test]
    public void Float32_Multiply_10_3() =>
        "out:float32 = 10.0 * 3.0".BuildWithFloats().AssertOutFloat32("out", 30.0f);

    [Test]
    public void Float32_Multiply_Neg5_3() =>
        "out:float32 = -5.0 * 3.0".BuildWithFloats().AssertOutFloat32("out", -15.0f);

    [Test]
    public void Float32_Multiply_5_Neg3() =>
        "out:float32 = 5.0 * -3.0".BuildWithFloats().AssertOutFloat32("out", -15.0f);

    [Test]
    public void Float32_Multiply_0_5() =>
        "out:float32 = 0.0 * 5.0".BuildWithFloats().AssertOutFloat32("out", 0.0f);

    [Test]
    public void Float32_Multiply_5_HalfPoint5() =>
        "out:float32 = 5.0 * 0.5".BuildWithFloats().AssertOutFloat32("out", 2.5f);

    [Test]
    public void Float32_Multiply_15_15() =>
        "out:float32 = 1.5 * 1.5".BuildWithFloats().AssertOutFloat32("out", 2.25f);

    // Divide (/)
    [Test]
    public void Float32_Divide_15_25() =>
        "out:float32 = 1.5 / 2.5".BuildWithFloats().AssertOutFloat32("out", 0.6f);

    [Test]
    public void Float32_Divide_10_3() =>
        "out:float32 = 10.0 / 3.0".BuildWithFloats().AssertOutFloat32("out", 10.0f / 3.0f);

    [Test]
    public void Float32_Divide_Neg5_3() =>
        "out:float32 = -5.0 / 3.0".BuildWithFloats().AssertOutFloat32("out", -5.0f / 3.0f);

    [Test]
    public void Float32_Divide_5_Neg3() =>
        "out:float32 = 5.0 / -3.0".BuildWithFloats().AssertOutFloat32("out", 5.0f / -3.0f);

    [Test]
    public void Float32_Divide_0_5() =>
        "out:float32 = 0.0 / 5.0".BuildWithFloats().AssertOutFloat32("out", 0.0f);

    [Test]
    public void Float32_Divide_5_HalfPoint5() =>
        "out:float32 = 5.0 / 0.5".BuildWithFloats().AssertOutFloat32("out", 10.0f);

    [Test]
    public void Float32_Divide_15_15() =>
        "out:float32 = 1.5 / 1.5".BuildWithFloats().AssertOutFloat32("out", 1.0f);

    // Modulus (%)
    [Test]
    public void Float32_Modulus_15_25() =>
        "out:float32 = 1.5 % 2.5".BuildWithFloats().AssertOutFloat32("out", 1.5f);

    [Test]
    public void Float32_Modulus_10_3() =>
        "out:float32 = 10.0 % 3.0".BuildWithFloats().AssertOutFloat32("out", 1.0f);

    [Test]
    public void Float32_Modulus_Neg5_3() =>
        "out:float32 = -5.0 % 3.0".BuildWithFloats().AssertOutFloat32("out", -2.0f);

    [Test]
    public void Float32_Modulus_5_Neg3() =>
        "out:float32 = 5.0 % -3.0".BuildWithFloats().AssertOutFloat32("out", 2.0f);

    [Test]
    public void Float32_Modulus_0_5() =>
        "out:float32 = 0.0 % 5.0".BuildWithFloats().AssertOutFloat32("out", 0.0f);

    [Test]
    public void Float32_Modulus_5_HalfPoint5() =>
        "out:float32 = 5.0 % 0.5".BuildWithFloats().AssertOutFloat32("out", 0.0f);

    [Test]
    public void Float32_Modulus_15_15() =>
        "out:float32 = 1.5 % 1.5".BuildWithFloats().AssertOutFloat32("out", 0.0f);

    // ─── Mixed-type binary arithmetic ─────────────────────────────
    // int + float32 → float32 (widening)
    [Test]
    public void Float32_IntPlusFloat32_WidensToFloat32() {
        var rt = "x:int = 1\rz:float32 = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntMinusFloat32_WidensToFloat32() {
        var rt = "x:int = 5\rz:float32 = 2.5\rout = x - z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntMultiplyFloat32_WidensToFloat32() {
        var rt = "x:int = 3\rz:float32 = 2.5\rout = x * z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(7.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntDivideFloat32_WidensToFloat32() {
        var rt = "x:int = 5\rz:float32 = 2.0\rout = x / z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_IntModulusFloat32_WidensToFloat32() {
        var rt = "x:int = 7\rz:float32 = 2.5\rout = x % z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // int16 + float32
    [Test]
    public void Float32_Int16PlusFloat32_WidensToFloat32() {
        var rt = "x:int16 = 1\rz:float32 = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int16MinusFloat32_WidensToFloat32() {
        var rt = "x:int16 = 5\rz:float32 = 2.5\rout = x - z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int16MultiplyFloat32_WidensToFloat32() {
        var rt = "x:int16 = 3\rz:float32 = 2.0\rout = x * z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(6.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int16DivideFloat32_WidensToFloat32() {
        var rt = "x:int16 = 5\rz:float32 = 2.0\rout = x / z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int16ModulusFloat32_WidensToFloat32() {
        var rt = "x:int16 = 7\rz:float32 = 2.5\rout = x % z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // int8 + float32
    [Test]
    public void Float32_Int8PlusFloat32_WidensToFloat32() {
        var rt = "x:int8 = 1\rz:float32 = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int8MinusFloat32_WidensToFloat32() {
        var rt = "x:int8 = 5\rz:float32 = 2.5\rout = x - z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int8MultiplyFloat32_WidensToFloat32() {
        var rt = "x:int8 = 3\rz:float32 = 2.0\rout = x * z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(6.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int8DivideFloat32_WidensToFloat32() {
        var rt = "x:int8 = 5\rz:float32 = 2.0\rout = x / z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Int8ModulusFloat32_WidensToFloat32() {
        var rt = "x:int8 = 7\rz:float32 = 2.5\rout = x % z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // byte + float32
    [Test]
    public void Float32_BytePlusFloat32_WidensToFloat32() {
        var rt = "x:byte = 1\rz:float32 = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ByteMinusFloat32_WidensToFloat32() {
        var rt = "x:byte = 5\rz:float32 = 2.5\rout = x - z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ByteMultiplyFloat32_WidensToFloat32() {
        var rt = "x:byte = 3\rz:float32 = 2.0\rout = x * z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(6.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ByteDivideFloat32_WidensToFloat32() {
        var rt = "x:byte = 5\rz:float32 = 2.0\rout = x / z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ByteModulusFloat32_WidensToFloat32() {
        var rt = "x:byte = 7\rz:float32 = 2.5\rout = x % z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // float32 + real → Real
    [Test]
    public void Float32_PlusReal_WidensToReal() {
        var rt = "x:float32 = 1.5\rz:real = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(4.0, rt["out"].Value);
    }

    [Test]
    public void Float32_MinusReal_WidensToReal() {
        var rt = "x:float32 = 5.0\rz:real = 2.5\rout = x - z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(2.5, rt["out"].Value);
    }

    [Test]
    public void Float32_MultiplyReal_WidensToReal() {
        var rt = "x:float32 = 1.5\rz:real = 2.0\rout = x * z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(3.0, rt["out"].Value);
    }

    [Test]
    public void Float32_DivideReal_WidensToReal() {
        var rt = "x:float32 = 5.0\rz:real = 2.0\rout = x / z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(2.5, rt["out"].Value);
    }

    [Test]
    public void Float32_ModulusReal_WidensToReal() {
        var rt = "x:float32 = 5.5\rz:real = 2.0\rout = x % z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    // float64 (= real) + float32 → Real
    [Test]
    public void Float32_Float64PlusFloat32_WidensToReal() {
        var rt = "x:float64 = 1.5\rz:float32 = 2.5\rout = x + z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(4.0, rt["out"].Value);
    }

    [Test]
    public void Float32_Float64MinusFloat32_WidensToReal() {
        var rt = "x:float64 = 5.0\rz:float32 = 2.5\rout = x - z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(2.5, rt["out"].Value);
    }

    [Test]
    public void Float32_Float64MultiplyFloat32_WidensToReal() {
        var rt = "x:float64 = 1.5\rz:float32 = 2.0\rout = x * z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(3.0, rt["out"].Value);
    }

    [Test]
    public void Float32_Float64DivideFloat32_WidensToReal() {
        var rt = "x:float64 = 5.0\rz:float32 = 2.0\rout = x / z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(2.5, rt["out"].Value);
    }

    [Test]
    public void Float32_Float64ModulusFloat32_WidensToReal() {
        var rt = "x:float64 = 5.5\rz:float32 = 2.0\rout = x % z".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    // f32 + literal (int const, real const) — should stay float32
    [Test]
    public void Float32_PlusConstIntLiteral_StaysFloat32() {
        var rt = "x:float32 = 2.0\rout = x + 5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(7.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_MinusConstIntLiteral_StaysFloat32() {
        var rt = "x:float32 = 10.0\rout = x - 5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(5.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_MultiplyConstIntLiteral_StaysFloat32() {
        var rt = "x:float32 = 2.0\rout = x * 3".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(6.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_DivideConstIntLiteral_StaysFloat32() {
        var rt = "x:float32 = 10.0\rout = x / 4".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ModulusConstIntLiteral_StaysFloat32() {
        var rt = "x:float32 = 10.0\rout = x % 4".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // f32 + real literal (const-real) — real lit narrows to f32
    [Test]
    public void Float32_PlusConstRealLiteral_StaysFloat32() {
        var rt = "x:float32 = 2.0\rout = x + 5.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(7.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_MinusConstRealLiteral_StaysFloat32() {
        var rt = "x:float32 = 10.0\rout = x - 3.5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(6.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_MultiplyConstRealLiteral_StaysFloat32() {
        var rt = "x:float32 = 2.0\rout = x * 1.5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_DivideConstRealLiteral_StaysFloat32() {
        var rt = "x:float32 = 5.0\rout = x / 2.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ModulusConstRealLiteral_StaysFloat32() {
        var rt = "x:float32 = 5.0\rout = x % 3.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.0f, rt["out"].Value);
    }

    // ─── Unary operators ──────────────────────────────────────────

    [Test]
    public void Float32_Negate_ConstLiteral() {
        var rt = "out:float32 = -(1.5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Negate_TypedVariable() {
        var rt = "x:float32 = 3.5\rout = -x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Negate_ZeroTypedVariable() {
        var rt = "x:float32 = 0.0\rout = -x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-0.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_DoubleNegate_TypedVariable() {
        var rt = "x:float32 = 3.5\rout = -(-x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    // ─── Power operator ** ────────────────────────────────────────
    // Const-int exponent lets result stay Float32 (TIC special-case).
    // Non-const-int exponent or negative literal forces Real.

    [Test]
    public void Float32_Pow_ConstIntExponent3() {
        var rt = "x:float32 = 2.0\rout = x ** 3".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(8.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Pow_ConstIntExponent0() {
        var rt = "x:float32 = 2.0\rout = x ** 0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Pow_ZeroToZero() {
        var rt = "out:float32 = 0.0 ** 0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Pow_ZeroToFive() {
        var rt = "out:float32 = 0.0 ** 5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(0.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Pow_OneToHundred() {
        var rt = "out:float32 = 1.0 ** 100".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Pow_NonConstFloat32Exponent_WidensToReal() {
        var rt = "x:float32 = 2.0\ry:float32 = 3.0\rout = x ** y".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(8.0, rt["out"].Value);
    }

    [Test]
    public void Float32_Pow_NegativeConstExponent_WidensToReal() {
        var rt = "x:float32 = 2.0\rout = x ** -1".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(0.5, rt["out"].Value);
    }

    // ─── Modulus semantics ─────────────────────────────────────────
    [Test]
    public void Float32_Mod_55_20() {
        var rt = "x:float32 = 5.5\rout = x % 2.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Mod_ByZero_ProducesNaN() {
        var rt = "x:float32 = 5.0\rout = x % 0.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.IsTrue(float.IsNaN((float)rt["out"].Value));
    }

    [Test]
    public void Float32_Mod_Neg55_20() {
        var rt = "x:float32 = -5.5\rout = x % 2.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Mod_ZeroBySomething() {
        var rt = "out:float32 = 0.0 % 5.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(0.0f, rt["out"].Value);
    }

    // ─── Precedence & associativity ────────────────────────────────
    [Test]
    public void Float32_Precedence_MultBeforeAdd() {
        var rt = "out:float32 = 1.0 + 2.0 * 3.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(7.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Precedence_ParensOverrideMult() {
        var rt = "out:float32 = (1.0 + 2.0) * 3.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(9.0f, rt["out"].Value);
    }

    // `**` is right-associative: 2**3**2 = 2**(3**2) = 512.
    // Outer `2**(3**2)` has non-const-int RHS, so pow widens to Real.
    [Test]
    public void Float32_Precedence_PowerRightAssociative_WidensToReal() {
        var rt = "out = 2.0 ** 3 ** 2".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(512.0, rt["out"].Value);
    }

    [Test]
    public void Float32_Mixed_MulThenAdd() {
        var rt = "x:float32 = 5.0\ry = x * 2.0 + 1.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["y"].Type.ToString());
        Assert.AreEqual(11.0f, rt["y"].Value);
    }

    [Test]
    public void Float32_Chained_Add4() {
        var rt = "out:float32 = 1.0 + 2.0 + 3.0 + 4.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(10.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Chained_Mul4() {
        var rt = "out:float32 = 1.0 * 2.0 * 3.0 * 4.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(24.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Chained_SubIsLeftAssoc() {
        // 10 - 3 - 2 = (10-3)-2 = 5
        var rt = "out:float32 = 10.0 - 3.0 - 2.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(5.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_Chained_DivIsLeftAssoc() {
        // 20 / 4 / 2 = (20/4)/2 = 2.5
        var rt = "out:float32 = 20.0 / 4.0 / 2.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    // ─── Variables & inputs ───────────────────────────────────────
    [Test]
    public void Float32_Input_TypedFloat32_Mult2() {
        var rt = "x:float32; y = x * 2.0".BuildWithFloats();
        rt["x"].Value = 1.5f;
        rt.Run();
        Assert.AreEqual("Float32", rt["y"].Type.ToString());
        Assert.AreEqual(3.0f, rt["y"].Value);
    }

    [Test]
    public void Float32_TwoInputsFloat32_Sum() {
        var rt = "x:float32; y:float32; z = x + y".BuildWithFloats();
        rt["x"].Value = 1.5f;
        rt["y"].Value = 2.5f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(4.0f, rt["z"].Value);
    }

    [Test]
    public void Float32_TwoInputsFloat32_Product() {
        var rt = "x:float32; y:float32; z = x * y".BuildWithFloats();
        rt["x"].Value = 2.0f;
        rt["y"].Value = 3.0f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(6.0f, rt["z"].Value);
    }

    [Test]
    public void Float32_TwoInputsFloat32_Sub() {
        var rt = "x:float32; y:float32; z = x - y".BuildWithFloats();
        rt["x"].Value = 5.0f;
        rt["y"].Value = 3.0f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(2.0f, rt["z"].Value);
    }

    [Test]
    public void Float32_TwoInputsFloat32_Div() {
        var rt = "x:float32; y:float32; z = x / y".BuildWithFloats();
        rt["x"].Value = 5.0f;
        rt["y"].Value = 2.0f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(2.5f, rt["z"].Value);
    }

    [Test]
    public void Float32_TwoInputsFloat32_Mod() {
        var rt = "x:float32; y:float32; z = x % y".BuildWithFloats();
        rt["x"].Value = 5.0f;
        rt["y"].Value = 3.0f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(2.0f, rt["z"].Value);
    }

    // Mixed input types
    [Test]
    public void Float32_InputInt_PlusInputFloat32() {
        var rt = "x:int; y:float32; z = x + y".BuildWithFloats();
        rt["x"].Value = 3;
        rt["y"].Value = 2.5f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(5.5f, rt["z"].Value);
    }

    [Test]
    public void Float32_InputByte_PlusInputFloat32() {
        var rt = "x:byte; y:float32; z = x + y".BuildWithFloats();
        rt["x"].Value = (byte)3;
        rt["y"].Value = 2.5f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(5.5f, rt["z"].Value);
    }

    [Test]
    public void Float32_InputInt16_PlusInputFloat32() {
        var rt = "x:int16; y:float32; z = x + y".BuildWithFloats();
        rt["x"].Value = (short)3;
        rt["y"].Value = 2.5f;
        rt.Run();
        Assert.AreEqual("Float32", rt["z"].Type.ToString());
        Assert.AreEqual(5.5f, rt["z"].Value);
    }

    // Local variable propagation
    [Test]
    public void Float32_LocalVar_Propagation() {
        var rt = "a:float32=1.5\rb = a * 2\rout = b".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["b"].Type.ToString());
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(3.0f, rt["b"].Value);
        Assert.AreEqual(3.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_LocalVar_MultilineChain() {
        var rt = "a:float32=1.0\rb = a + 2.0\rc = b * 3.0\rout = c".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(9.0f, rt["out"].Value);
    }

    // ─── Constant expression folding ──────────────────────────────
    [Test]
    public void Float32_ConstFold_AddPair() {
        var rt = "out:float32 = 1.5 + 2.5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(4.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConstFold_PowIntExp() {
        var rt = "out:float32 = 2.0 ** 3".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(8.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConstFold_NegateSum() {
        var rt = "out:float32 = -(1.5 + 2.5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-4.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConstFold_IntSum() {
        var rt = "out:float32 = 5 + 3".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(8.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConstFold_NegateIntSum() {
        var rt = "out:float32 = -(1 + 2)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(-3.0f, rt["out"].Value);
    }

    // ─── Float32 / Float64 interchange ───────────────────────────
    [Test]
    public void Float32_Float64Literal_ProducesReal() {
        var rt = "out:float64 = 1.5".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    [Test]
    public void Float32_Float64_AssignToReal_Interchangeable() {
        var rt = "x:float64 = 1.5\rout:real = x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    [Test]
    public void Float32_Real_AssignToFloat64_Interchangeable() {
        var rt = "x:real = 1.5\rout:float64 = x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    // f32 -> f64 widening: allowed (f32 ≤ Real in lattice).
    [Test]
    public void Float32_ToFloat64_Widening() {
        var rt = "x:float32 = 1.5\rout:float64 = x".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Real", rt["out"].Type.ToString());
        Assert.AreEqual(1.5, rt["out"].Value);
    }

    // f64 -> f32 narrowing: NOT permitted implicitly (Real ≰ Float32).
    // Expected: FunnyParseException.
    [Test]
    public void Float32_FromFloat64_NarrowingRejected() {
        Assert.Throws<FunnyParseException>(
            () => "x:float64 = 1.5\rout:float32 = x".BuildWithFloats());
    }

    [Test]
    public void Float32_FromReal_NarrowingRejected() {
        Assert.Throws<FunnyParseException>(
            () => "x:real = 1.5\rout:float32 = x".BuildWithFloats());
    }

    // ─── Overflow / underflow ────────────────────────────────────
    // Runtime overflow through arithmetic — → ±Infinity, no exception.
    [Test]
    public void Float32_Overflow_ToPositiveInfinity() {
        var rt = "x:float32 = 3.4e38\rout = x * 10.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(float.PositiveInfinity, rt["out"].Value);
    }

    [Test]
    public void Float32_Overflow_ToNegativeInfinity() {
        var rt = "x:float32 = -3.4e38\rout = x * 10.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(float.NegativeInfinity, rt["out"].Value);
    }

    [Test]
    public void Float32_Underflow_ToZero() {
        var rt = "x:float32 = 1e-38\rout = x * 1e-20".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(0.0f, rt["out"].Value);
    }

    // Constant literal beyond f32 max: narrowed to +Infinity at load time.
    [Test]
    public void Float32_LiteralBeyondMax_ProducesInfinity() {
        var rt = "out:float32 = 1e40".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(float.PositiveInfinity, rt["out"].Value);
    }

    [Test]
    public void Float32_NegativeLiteralBeyondMax_ProducesNegInfinity() {
        var rt = "out:float32 = -1e40".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(float.NegativeInfinity, rt["out"].Value);
    }

    // Smallest positive subnormal representable f32 ≈ 1e-45
    [Test]
    public void Float32_SubnormalLiteral_Preserved() {
        var rt = "out:float32 = 1e-45".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1e-45f, rt["out"].Value);
    }

    // ─── Precision loss awareness ────────────────────────────────
    // Pin the actual f32 value for 0.1 + 0.2 (differs from 0.3 exactly).
    [Test]
    public void Float32_Precision_ZeroPointOnePlusZeroPointTwo() {
        var rt = "out:float32 = 0.1 + 0.2".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        // Constant-fold uses real arithmetic, then narrows: 0.3 -> 0.3f
        Assert.AreEqual(0.3f, rt["out"].Value);
    }

    // Same expression through an input variable — arithmetic done in f32,
    // producing the classic 0.1+0.2 discrepancy for float32.
    [Test]
    public void Float32_Precision_ZeroPointOnePlusZeroPointTwo_ViaInputs() {
        var rt = "x:float32; y:float32; out = x + y".BuildWithFloats();
        rt["x"].Value = 0.1f;
        rt["y"].Value = 0.2f;
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(0.1f + 0.2f, rt["out"].Value);
    }

    [Test]
    public void Float32_Precision_OneThird() {
        var rt = "out:float32 = 1.0 / 3.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        // Constant-fold in Real -> narrowed to f32. Actual bits differ from
        // 1.0f/3.0f which is computed in Single directly. Pin the fold result.
        Assert.AreEqual((float)(1.0 / 3.0), rt["out"].Value);
    }

    // 2^24 + 1 = 16777217 is not representable exactly in f32; nearest even is 16777216.
    [Test]
    public void Float32_Precision_2Pow24Plus1_RoundsDown() {
        var rt = "out:float32 = 16777217".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(16777216.0f, rt["out"].Value);
    }

    #endregion

    #region Preferred metadata survives Push (WO3 — PropagatePreferred phase order)

    // Negative int literals — CS collapse in Push must not erase the I32 Preferred.
    // Before phase reordering: `(-1) + (-2)` defaulted to Real. After: stays I32.
    [Test]
    public void NegativeLiteralAdd_ResolvesToI32() =>
        "y = (-1) + (-2)".Calc().AssertResultHas("y", -3);

    #endregion
}

internal static class Float32TestExtensions {
    // Extension used by shorthand assertions in the Float32 region above.
    // Declared in a separate static class because C# forbids extension methods
    // inside a non-static host class.
    public static void AssertOutFloat32(this NFun.Runtime.FunnyRuntime rt, string outName, float expected) {
        rt.Run();
        NUnit.Framework.Assert.AreEqual("Float32", rt[outName].Type.ToString());
        NUnit.Framework.Assert.AreEqual(expected, rt[outName].Value);
    }
}
