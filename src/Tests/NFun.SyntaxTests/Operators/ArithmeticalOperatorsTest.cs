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
}
