using System;
using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.BuiltInFunctions;

using Tic;

[TestFixture]
public class BuiltInFunctionsTest {
    [TestCase("toText([1,2,3])", "[1,2,3]")]
    [TestCase("toText(-1)", "-1")]
    [TestCase("toText(-0.123)", "-0.123")]
    [TestCase("abs(0x1)", 1)]
    [TestCase("abs(-0x1)", 1)]
    [TestCase("abs(1.0)", 1.0)]
    [TestCase("abs(-1.0)", 1.0)]
    [TestCase("abs(0x1-0x4)", 3)]
    [TestCase("15 - min(abs(1-4), 0x7)", 12)]
    // abs on int8 — single-equation form
    [TestCase("out:int8 = abs(-5)",  (sbyte)5)]
    [TestCase("out:int8 = abs(0)",   (sbyte)0)]
    [TestCase("out:int8 = abs(127)", (sbyte)127)]
    // sign(T):int32 — returns -1/0/1; constraint is SignedNumber so int8 reachable
    [TestCase("sign(-5)",   -1)]
    [TestCase("sign(0)",     0)]
    [TestCase("sign(5)",     1)]
    [TestCase("sign(0.0)",   0)]
    [TestCase("sign(3.14)",  1)]
    [TestCase("sign(-3.14)",-1)]
    [TestCase("sqrt(0x0)", 0.0)]
    [TestCase("sqrt(1.0)", 1.0)]
    [TestCase("sqrt(4.0)", 2.0)]
    [TestCase("cos(0)", 1.0)]
    [TestCase("sin(0)", 0.0)]
    [TestCase("acos(1)", 0.0)]
    [TestCase("asin(0)", 0.0)]
    [TestCase("atan(0)", 0.0)]
    [TestCase("tan(0)", 0.0)]
    [TestCase("exp(0)", 1.0)]
    [TestCase("log(1,10)", 0.0)]
    [TestCase("log(1)", 0.0)]
    [TestCase("log10(1)", 0.0)]
    [TestCase("round(1.66666,1)", 1.7)]
    [TestCase("round(1.222,2)", 1.22)]
    [TestCase("round(1.66666,0)", 2.0)]
    [TestCase("round(1.2,0)", 1.0)]
    // Half values: MidpointRounding.AwayFromZero, matching the text format
    // specifier (`'{0.5:0}'` = `'1'` per Specs/Texts.md L165). Default
    // Math.Round uses banker's rounding which would give round(0.5,0)=0.
    [TestCase("round(0.5, 0)", 1.0)]
    [TestCase("round(-0.5, 0)", -1.0)]
    [TestCase("round(2.5, 0)", 3.0)]
    [TestCase("round(-1.5, 0)", -2.0)]
    [TestCase("min(0.5, 1)", 0.5)]
    [TestCase("[1,2,3].count()", 3)]
    [TestCase("['1','2','3'].count()", 3)]
    [TestCase("[1..10].filter(rule it>3).count()", 7)]
    [TestCase("[].count()", 0)]
    [TestCase("count([1,2,3])", 3)]
    [TestCase("count([])", 0)]
    [TestCase("count([1.0,2.0,3.0])", 3)]
    [TestCase("count([[1,2],[3,4]])", 2)]
    [TestCase("avg([1,2,3])", 2.0)]
    [TestCase("avg([1.0,2.0,6.0])", 3.0)]
    [TestCase("sum([1.0,2,3])", 6.0)]
    [TestCase("[1,2,3].sum()", 6)]
    [TestCase("out:int64 = sum([1,2,3])", (long)6)]
    [TestCase("out:uint64= sum([1,2,3])", (ulong)6)]
    [TestCase("out:int   = sum([1,2,3])", (int)6)]
    [TestCase("out:uint  = sum([1,2,3])", (uint)6)]
    [TestCase("sum([1.0,2.5,6.0])", 9.5)]
    [TestCase("max([1.0,10.5,6.0])", 10.5)]
    [TestCase("max([1,-10,0.0])", 1.0)]
    [TestCase("max([1,0.0])", 1.0)]
    [TestCase("max(1.0,3.4)", 3.4)]
    [TestCase("max(0x4,3)", 4)]
    [TestCase("max('hello','world')", "world")]
    [TestCase("out:int64  = max([1,10,6])", (long)10)]
    [TestCase("out:uint64 = max([1,10,6])", (ulong)10)]
    [TestCase("out:int    = max([1,10,6])", 10)]
    [TestCase("out:uint   = max([1,10,6])", (uint)10)]
    [TestCase("out:int16  = max([1,10,6])", (short)10)]
    [TestCase("out:uint16 = max([1,10,6])", (ushort)10)]
    [TestCase("out:byte   = max([1,10,6])", (byte)10)]
    [TestCase("out:int8   = max([1,10,6])", (sbyte)10)]
    [TestCase("out:float32 = max([1.0,10.0,6.0])", 10.0f, Ignore = "Float32 phase 4: max generic dispatch for Float32 pending")]
    [TestCase("min([1.0,10.5,6.0])", 1.0)]
    [TestCase("min([0x1,-10,0])", -10)]
    [TestCase("min(1.0,3.4)", 1.0)]
    [TestCase("min(4,0x3)", 3)]
    [TestCase("out:int64  = min([1,10,6])", (long)1)]
    [TestCase("out:uint64 = min([1,10,6])", (ulong)1)]
    [TestCase("out:int    = min([1,10,6])", 1)]
    [TestCase("out:uint   = min([1,10,6])", (uint)1)]
    [TestCase("out:int16  = min([1,10,6])", (short)1)]
    [TestCase("out:uint16 = min([1,10,6])", (ushort)1)]
    [TestCase("out:byte   = min([1,10,6])", (byte)1)]
    [TestCase("out:int8   = min([1,10,6])", (sbyte)1)]
    [TestCase("out:float32 = min([1.0,10.0,6.0])", 1.0f, Ignore = "Float32 phase 4: min generic dispatch for Float32 pending")]
    [TestCase("median([1.0,10.5,6.0])", 6.0)]
    [TestCase("median([1,-10,0])", 0)]
    [TestCase("median([1])", 1)]
    [TestCase("median(['a','b','c'])", "b")]
    [TestCase("median([1,-10.0,0])", 0.0)]
    [TestCase("out:int64  = median([1,10,6])", (long)6)]
    [TestCase("out:uint64 = median([1,10,6])", (ulong)6)]
    [TestCase("out:int32  = median([1,10,6])", (int)6)]
    [TestCase("out:uint32 = median([1,10,6])", (uint)6)]
    [TestCase("out:int16  = median([1,10,6])", (Int16)6)]
    [TestCase("out:uint16 = median([1,10,6])", (UInt16)6)]
    [TestCase("out:uint8  = median([1,10,6])", (byte)6)]
    [TestCase("out:int8   = median([1,10,6])", (sbyte)6)]
    [TestCase("[1.0,2.0,3.0].any()", true)]
    [TestCase("['a'].any()", true)]
    [TestCase("[1..10].filter(rule it>3).any()", true)]
    [TestCase("[1..10].filter(rule it>10).any()", false)]
    [TestCase("[1,2,3,4].fold(rule it1+it2)", 10)]
    [TestCase("[1,2,3,4].fold(0,(rule it1+it2))", 10)]
    [TestCase("[1,2,3,4].fold(-10,(rule it1+it2))", 0)]
    [TestCase("[1,2,3,4].fold('', rule '{it1}{it2}')", "1234")]
    [TestCase("any([])", false)]
    [TestCase("[0x4,0x3,0x5,0x1].sort()", new[] { 1, 3, 4, 5 })]
    [TestCase("[4.0,3.0,5.0,1.0].sort()", new[] { 1.0, 3.0, 4.0, 5.0 })]
    [TestCase("['a','hey','what','up'].sort(rule it.count())", new[] { "a", "up", "hey", "what" })]
    [TestCase("['a','hey','what','up'].sortDescending(rule it.count())", new[] { "what", "hey", "up", "a", })]
    [TestCase("[4.0,3.0,5.0,1.0].sort(rule it%2)", new[] { 4.0, 3.0, 5.0, 1.0 })]
    [TestCase("[4.0,3.0,5.0,1.0].sortDescending(rule it%2)", new[] { 3.0, 5.0, 1.0, 4.0 })]
    [TestCase("out:int64[]  = [4,3,5,1].sort()", new long[] { 1, 3, 4, 5 })]
    [TestCase("out:uint64[] = [4,3,5,1].sort()", new ulong[] { 1, 3, 4, 5 })]
    [TestCase("out:int32[]  = [4,3,5,1].sort()", new int[] { 1, 3, 4, 5 })]
    [TestCase("out:uint32[] = [4,3,5,1].sort()", new UInt32[] { 1, 3, 4, 5 })]
    [TestCase("out:int16[]  = [4,3,5,1].sort()", new Int16[] { 1, 3, 4, 5 })]
    [TestCase("out:uint16[] = [4,3,5,1].sort()", new UInt16[] { 1, 3, 4, 5 })]
    [TestCase("out:uint8[]  = [4,3,5,1].sort()", new Byte[] { 1, 3, 4, 5 })]
    [TestCase("out:int8[]   = [4,3,5,1].sort()", new sbyte[] { 1, 3, 4, 5 })]
    [TestCase("out:float32[] = [4.0,3.0,5.0,1.0].sort()", new float[] { 1.0f, 3.0f, 4.0f, 5.0f }, Ignore = "Float32 phase 4: sort for Float32 pending")]
    [TestCase("out:float32[] = [4.0,3.0,5.0,1.0].sortDescending()", new float[] { 5.0f, 4.0f, 3.0f, 1.0f }, Ignore = "Float32 phase 4: sortDescending for Float32")]
    [TestCase("['4.0','3.0','5.0','1.0'].sort()", new[] { "1.0", "3.0", "4.0", "5.0" })]
    [TestCase("out:real[]   = range(0,5)", new[] { 0.0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int64[]  = range(0,5)", new long[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:uint64[] = range(0,5)", new ulong[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int32[]  = range(0,5)", new int[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:uint32[] = range(0,5)", new UInt32[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int16[]  = range(0,5)", new Int16[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:uint16[] = range(0,5)", new UInt16[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:uint8[]  = range(0,5)", new Byte[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int8[]   = range(0,5)", new sbyte[] { 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int8[]   = range(-5,5)", new sbyte[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int8[]   = range(5,0)", new sbyte[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:int8[]   = range(1,10,2)", new sbyte[] { 1, 3, 5, 7, 9 })]
    [TestCase("out:float32[] = range(0,5)", new float[] { 0, 1, 2, 3, 4, 5 }, Ignore = "Float32 phase 4: range for Float32 pending")]
    [TestCase("out:float32[] = range(0.0,2.5,0.5)", new float[] { 0.0f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f }, Ignore = "Float32 phase 4: range step for Float32 pending")]
    [TestCase("out:real[]   = range(-2.5,2.4)", new[] { -2.5, -1.5, -0.5, 0.5, 1.5 })]
    [TestCase("out:real[]   = range(-2.5,2.5)", new[] { -2.5, -1.5, -0.5, 0.5, 1.5, 2.5 })]
    [TestCase("out:real[]   = range(-2.5,2.6)", new[] { -2.5, -1.5, -0.5, 0.5, 1.5, 2.5 })]
    [TestCase("out:real[]   = range(-2.4,2.4)", new[] { -2.4, -1.4, -1.4 + 1, -1.4 + 2, -1.4 + 3 })]
    [TestCase("out:real[]   = range(-2.4,2.5)", new[] { -2.4, -1.4, -1.4 + 1, -1.4 + 2, -1.4 + 3 })]
    [TestCase("out:real[]   = range(-2.4,2.6)", new[] { -2.4, -1.4, -1.4 + 1, -1.4 + 2, -1.4 + 3, -1.4 + 4 })]
    [TestCase("out:real[]   = range(-2.4,2.7)", new[] { -2.4, -1.4, -1.4 + 1, -1.4 + 2, -1.4 + 3, -1.4 + 4 })]
    [TestCase("out:real[]   = range(-5,5)", new double[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int64[]  = range(-5,5)", new long[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int32[]  = range(-5,5)", new int[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:int16[]  = range(-5,5)", new Int16[] { -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 })]
    [TestCase("out:real[]   = range(5,0)", new[] { 5, 4, 3, 2, 1, 0.0 })]
    [TestCase("out:int64[]  = range(5,0)", new long[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:uint64[] = range(5,0)", new ulong[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:int32[]  = range(5,0)", new int[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:uint32[] = range(5,0)", new UInt32[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:int16[]  = range(5,0)", new Int16[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:uint16[] = range(5,0)", new UInt16[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:uint8[]  = range(5,0)", new Byte[] { 5, 4, 3, 2, 1, 0 })]
    [TestCase("out:real[]   = range(5,-5)", new double[] { 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5 })]
    [TestCase("out:real[]   = range(2.5,-2.4)", new[] { 2.5, 1.5, 0.5, -0.5, -1.5 })]
    [TestCase("out:real[]   = range(2.5,-2.5)", new[] { 2.5, 1.5, 0.5, -0.5, -1.5, -2.5 })]
    [TestCase("out:real[]   = range(2.5,-2.6)", new[] { 2.5, 1.5, 0.5, -0.5, -1.5, -2.5 })]
    [TestCase("out:real[]   = range(2.4,-2.4)", new[] { 2.4, 1.4, 1.4 - 1, 1.4 - 2, 1.4 - 3 })]
    [TestCase("out:real[]   = range(2.4,-2.5)", new[] { 2.4, 1.4, 1.4 - 1, 1.4 - 2, 1.4 - 3 })]
    [TestCase("out:real[]   = range(2.4,-2.6)", new[] { 2.4, 1.4, 1.4 - 1, 1.4 - 2, 1.4 - 3, 1.4 - 4 })]
    [TestCase("out:real[]   = range(2.4,-2.7)", new[] { 2.4, 1.4, 1.4 - 1, 1.4 - 2, 1.4 - 3, 1.4 - 4 })]
    [TestCase("out:int64[]  = range(5,-5)", new long[] { 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5 })]
    [TestCase("out:int32[]  = range(5,-5)", new int[] { 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5 })]
    [TestCase("out:int16[]  = range(5,-5)", new Int16[] { 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5 })]
    [TestCase("range(7,10)", new[] { 7, 8, 9, 10 })]
    [TestCase("range(7,10.0)", new[] { 7.0, 8, 9, 10 })]
    [TestCase("range(7.0,10.0)", new[] { 7.0, 8, 9, 10 })]
    [TestCase("range(1,10,2.0)", new[] { 1.0, 3.0, 5.0, 7.0, 9.0 })]
    public void ConstantEquationWithPredefinedFunction(string expr, object expected) {
        using var _ = TraceLog.Scope;
        expr.AssertAnonymousOut(expected);
    }

    [TestCase("ceil(7.03)", 8.0)]
    [TestCase("ceil(7.64)", 8.0)]
    [TestCase("ceil(0.12)", 1.0)]
    [TestCase("ceil(-0.12)", 0.0)]
    [TestCase("ceil(-7.1)", -7.0)]
    [TestCase("ceil(-7.6)", -7.0)]
    [TestCase("floor(7.03)", 7.0)]
    [TestCase("floor(7.64)", 7.0)]
    [TestCase("floor(0.12)", 0.0)]
    [TestCase("floor(-0.12)", -1.0)]
    [TestCase("floor(-7.1)", -8.0)]
    [TestCase("floor(-7.6)", -8.0)]
    public void CeilFloorConstantEquations(string expr, object expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("['a'].sort(rule it)", new[] { "a" })]
    [TestCase("['a'].sort()", new[] { "a" })]
    [TestCase("[12].sort(rule it)", new[] { 12 })]
    [TestCase("['a','hey','what','up'].sort(rule it.reverse())", new[] { "a", "up", "what", "hey" })]
    [TestCase("['a'].sort(rule it.reverse())", new[] { "a"})]
    public void MergeComparableArray(string expr, object expected) {
        using var _ = TraceLog.Scope;
        expr.AssertAnonymousOut(expected);
    }

    [TestCase((long)42, "x:int64\r y = max(1,x)", (long)42)]
    [TestCase((long)42, "x:int64\r y = min(1,x)", (long)1)]
    [TestCase((long)42, "x:int64\r y = min(100,x)", (long)42)]
    [TestCase((ulong)42, "x:uint64\r y = max(1,x)", (ulong)42)]
    [TestCase((ulong)42, "x:uint64\r y = min(1,x)", (ulong)1)]
    [TestCase((ulong)42, "x:uint64\r y = min(100,x)", (ulong)42)]
    [TestCase((int)42, "x:int32\r y = max(1,x)", (int)42)]
    [TestCase((int)42, "x:int32\r y = min(1,x)", (int)1)]
    [TestCase((int)42, "x:int32\r y = min(100,x)", (int)42)]
    [TestCase((uint)42, "x:uint32\r y = max(1,x)", (uint)42)]
    [TestCase((uint)42, "x:uint32\r y = min(1,x)", (uint)1)]
    [TestCase((uint)42, "x:uint32\r y = min(100,x)", (uint)42)]
    [TestCase((byte)42, "x:byte\r y = max(1,x)", (byte)42)]
    [TestCase((byte)42, "x:byte\r y = max(100,x)", (byte)100)]
    [TestCase((byte)42, "x:byte\r y = min(1,x)", (byte)1)]
    [TestCase((byte)42, "x:byte\r y = min(100,x)", (byte)42)]
    [TestCase((Int16)42, "x:int16\r y = max(1,x)", (Int16)42)]
    [TestCase((Int16)42, "x:int16\r y = max(100,x)", (Int16)100)]
    [TestCase((Int16)42, "x:int16\r y = min(1,x)", (Int16)1)]
    [TestCase((Int16)42, "x:int16\r y = min(100,x)", (Int16)42)]
    [TestCase((UInt16)42, "x:uint16\r y = max(1,x)", (UInt16)42)]
    [TestCase((UInt16)42, "x:uint16\r y = max(100,x)", (UInt16)100)]
    [TestCase((UInt16)42, "x:uint16\r y = min(1,x)", (UInt16)1)]
    [TestCase((UInt16)42, "x:uint16\r y = min(100,x)", (UInt16)42)]
    public void SingleVariableEquation(object input, string expr, object expected) =>
        expr.Calc("x", input).AssertReturns("y", expected);

    [TestCase("y:real  = abs(-x)", -1.0, 1.0)]
    [TestCase("y:real  = abs(x)", 1.0, 1.0)]
    [TestCase("y:int64 = abs(x)", (long)-1, (long)1)]
    [TestCase("y:int32 = abs(x)", (int)-1, (int)1)]
    [TestCase("y:int16 = abs(x)", (Int16)(-1), (Int16)1)]
    [TestCase("y = abs(x-4.0)", 1.0, 3.0)]
    [TestCase("y = abs(x-4)", 1, 3)]
    [TestCase("y = abs(x-4.0)", 1.0, 3.0)]
    public void EquationWithPredefinedFunction(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("y", expected);

    [Ignore("toInt/toReal/toBits/toBytes/toUnicode/toUtf8 functions are not implemented")]
    [TestCase("y = abs(toInt(x)-toInt(4))", 1, 3)]
    [TestCase("y = abs(x-toInt(4))", 1, 3)]
    [TestCase("x:int; y = abs(toInt(x)-toInt(4))", 1, 3)]
    public void EquationWithUnimplementedToIntFunction(string expr, object arg, object expected) =>
        expr.Calc("x", arg).AssertReturns("y", expected);


    [Ignore("toInt/toReal/toBits/toBytes/toUnicode/toUtf8 functions are not implemented")]
    [TestCase("toInt(1.2)", 1)]
    [TestCase("toInt(-1.2)", -1)]
    [TestCase("toInt('1')", 1)]
    [TestCase("toInt('-123')", -123)]
    [TestCase("toInt([0x21,0x33,0x12])", 1_192_737)]
    [TestCase("toInt([0x21,0x33,0x12,0x00])", 1_192_737)]
    [TestCase("toInt([0x21,0x00,0x00,0x00])", 0x21)]
    [TestCase("toInt([0x21,0x00,0x00,0x00])", 0x21)]
    [TestCase("toInt([0x21])", 0x21)]
    [TestCase("toReal('1')", 1.0)]
    [TestCase("toReal('1.1')", 1.1)]
    [TestCase("toReal('-0.123')", -0.123)]
    [TestCase("toReal(1)", 1.0)]
    [TestCase("toReal(-1)", -1.0)]
    [TestCase(
        "toBits(123)",
        new[] {
            true, true, false, true, true, true, true, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false
        })]
    [TestCase("toBytes(123)", new[] { 123, 0, 0, 0 })]
    [TestCase("toBytes(1_192_737)", new[] { 0x21, 0x33, 0x12, 0 })]
    [TestCase(
        "toUnicode('hi there')",
        new[] { 0x68, 00, 0x69, 00, 0x20, 00, 0x74, 00, 0x68, 00, 0x65, 00, 0x72, 00, 0x65, 00 })]
    [TestCase("toUtf8('hi there')", new[] { 0x68, 0x69, 0x20, 0x74, 0x68, 0x65, 0x72, 0x65 })]
    public void ToIntToRealToBitsConstants(string expr, object expected)
        => expr.AssertAnonymousOut(expected);

    [TestCase("y = pi(")]
    [TestCase("y = pi(1)")]
    [TestCase("y = abs(")]
    [TestCase("y = abs)")]
    [TestCase("y = abs()")]
    [TestCase("y = abs(())")]
    [TestCase("y = abs()()")]
    [TestCase("y = ()abs()")]
    [TestCase("y = abs(1,,2)")]
    [TestCase("y = abs(,,2)")]
    [TestCase("y = abs(,,)")]
    [TestCase("y = abs(2,,)")]
    [TestCase("y = abs(,)")]
    [TestCase("y = abs(1,2)")]
    [TestCase("y = abs(1 2)")]
    [TestCase("y = add(")]
    [TestCase("y = add()")]
    [TestCase("y = add(1)")]
    [TestCase("y = add 1")]
    [TestCase("y = add(1,2,3)")]
    [TestCase("y = avg(['1','2','3'])")]
    [TestCase("y= max(1,2,3)")]
    [TestCase("y= ~1.5")]
    [TestCase("y= max(1,true)")]
    [TestCase("y= max(1,'test')")]
    [TestCase("y= max(1,'test'[0])")]
    [TestCase("y= max(1,(j)->j)")]
    public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();

    [TestCase("y = [1,2] in [1,2,3,4]")] // FU711 now catches array-in-array type mismatch
    public void TodoObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();

    [TestCase("y= max([])")]
    public void ObviouslyFailsInRuntime(string expr) => expr.AssertObviousFailsOnRuntime();

    // ═══════════════════════════════════════════════════════════════
    // Bool toText should produce lowercase
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BoolToText_ShouldBeLowercase() {
        "out = true.toText()".Calc().AssertResultHas("out", "true");
        "out = false.toText()".Calc().AssertResultHas("out", "false");
    }

    // ═══════════════════════════════════════════════════════════════
    // Text in text — should be type error
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void TextInText_ShouldBeTypeError() {
        Assert.Throws<Exceptions.FunnyParseException>(
            () => "out = 'h' in 'hello'".Calc());
    }

    // ═══════════════════════════════════════════════════════════════
    // Float32 surface — opt-in via FloatFamilySupport.Float32AndFloat64.
    // Default dialect rejects float32 keyword, so these need CalcWithFloats.
    // ═══════════════════════════════════════════════════════════════

    // abs / sign on float32.
    [TestCase("out:float32 = abs(-5.0)",  5.0f)]
    [TestCase("out:float32 = abs(0.0)",   0.0f)]
    [TestCase("out:float32 = abs(3.14)",  3.14f)]
    [TestCase("sign(-3.14:float32)", -1, Ignore = "inline cast `:float32` syntax not supported")]
    public void Float32_AbsAndSign(string expr, object expected) =>
        expr.CalcWithFloats().AssertAnonymousOut(expected);

    // Math fns generic over Floats — float32 dispatch.
    [TestCase("out:float32 = sqrt(4.0)",  2.0f)]
    [TestCase("out:float32 = sqrt(9.0)",  3.0f)]
    [TestCase("out:float32 = sin(0.0)",   0.0f)]
    [TestCase("out:float32 = cos(0.0)",   1.0f)]
    [TestCase("out:float32 = tan(0.0)",   0.0f)]
    [TestCase("out:float32 = exp(0.0)",   1.0f)]
    [TestCase("out:float32 = log(1.0)",   0.0f)]
    [TestCase("out:float32 = log10(1.0)", 0.0f)]
    [TestCase("out:float32 = log10(100.0)", 2.0f)]
    [TestCase("out:float32 = asin(0.0)",  0.0f)]
    [TestCase("out:float32 = acos(1.0)",  0.0f)]
    [TestCase("out:float32 = atan(0.0)",  0.0f)]
    [TestCase("out:float32 = atan2(0.0, 1.0)", 0.0f)]
    [TestCase("out:float32 = ceil(7.03)", 8.0f)]
    [TestCase("out:float32 = floor(7.6)", 7.0f)]
    [TestCase("out:float32 = round(1.66666, 1)", 1.7f)]
    [TestCase("out:float32 = avg([1.0,2.0,6.0])", 3.0f)]
    [TestCase("out:float32 = 6.0 / 2.0",  3.0f)]
    public void Float32_GenericMath(string expr, object expected) =>
        expr.CalcWithFloats().AssertAnonymousOut(expected);

    // convert() to/from float32 via numeric converter.
    [TestCase("x:int = 5\rout:float32 = convert(x)",   5.0f)]
    [TestCase("x:int16 = -5\rout:float32 = convert(x)", -5.0f)]
    [TestCase("x:int8 = -5\rout:float32 = convert(x)",  -5.0f)]
    [TestCase("x:byte = 200\rout:float32 = convert(x)", 200.0f)]
    [TestCase("x:float32 = 1.5\rout:real = convert(x)", 1.5)]
    [TestCase("x:real = 1.5\rout:float32 = convert(x)", 1.5f)]
    public void Float32_Convert(string expr, object expected) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        Assert.AreEqual(expected, rt["out"].Value);
    }

    // Lossy conversions per Specs/Functions.md — silent precision loss above 2^24
    // (Float32 mantissa = 24 bits). NFun marks these ⚠ — succeeds, doesn't throw,
    // but may not round-trip exactly. Pin the current (correct) behavior.
    [Test]
    public void Float32_LossyFromUInt32_NoThrow() {
        // 2^25 + 1 = 33554433, but float32 mantissa rounds it to 33554432 (loss of 1).
        var rt = "x:uint32 = 33554433\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(33554432.0f, rt["out"].Value);  // silent precision loss
    }

    [Test]
    public void Float32_LossyFromInt64_NoThrow() {
        var rt = "x:int64 = 9007199254740993\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        // Precision loss expected; just verify no exception and result is float.
        Assert.IsInstanceOf<float>(rt["out"].Value);
    }

    [Test]
    public void Float32_LossyFromReal_NoThrow() {
        // Real has ~15 digits, float32 has ~7. 1.23456789 rounds.
        var rt = "x:real = 1.23456789\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.IsInstanceOf<float>(rt["out"].Value);
        Assert.That(System.Math.Abs((float)rt["out"].Value - 1.2345679f), Is.LessThan(0.00001f));
    }

    [Test]
    public void Float32_OverflowReal_BecomesInfinity() {
        // Real value larger than Float32.MaxValue (~3.4e38) → +Infinity per IEEE.
        var rt = "x:real = 1e40\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.PositiveInfinity, rt["out"].Value);
    }

    // ─────────────────────────────────────────────────────────────────
    // Optional float32 — float32? holds a float or `none`.
    // Optional requires OptionalTypesSupport.Enabled in dialect.
    // ─────────────────────────────────────────────────────────────────

    [Test]
    public void OptionalFloat32_HoldsValue() {
        var rt = Funny.Hardcore
            .WithDialect(
                floatFamilySupport: FloatFamilySupport.Float32AndFloat64,
                optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("out:float32? = 1.5");
        rt.Run();
        Assert.AreEqual("Float32?", rt["out"].Type.ToString());
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void OptionalFloat32_HoldsNone() {
        var rt = Funny.Hardcore
            .WithDialect(
                floatFamilySupport: FloatFamilySupport.Float32AndFloat64,
                optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("out:float32? = none");
        rt.Run();
        // None in NFun's output API surfaces as `null` (FunnyNone sentinel
        // collapses to null at the variable accessor boundary, matching Int8? etc).
        Assert.IsNull(rt["out"].Value);
    }

    // ─────────────────────────────────────────────────────────────────
    // Convert paths uncovered by independent audit — bool/text/byte[]
    // ─────────────────────────────────────────────────────────────────

    [Test]
    public void Float32_ConvertBool_FromTrue() {
        var rt = "out:float32 = convert(true)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertBool_FromFalse() {
        var rt = "out:float32 = convert(false)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(0.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToBool_NonZero() {
        var rt = "x:float32=3.14\rout:bool = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToBool_Zero() {
        var rt = "x:float32=0.0\rout:bool = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToBool_NaN_IsFalse() {
        // IEEE 754: NaN → false per existing real→bool rule.
        var rt = "a:float32=0.0\rb:float32=0.0\rnan = a / b\rout:bool = convert(nan)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertFromText() {
        var rt = "out:float32 = convert('3.14')".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.14f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToText() {
        var rt = "x:float32=3.14\rout:text = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("3.14", rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertToBytes() {
        // serialize as 4-byte byte[] (IEEE 754 binary32).
        var rt = "x:float32=1.0\rout:byte[] = convert(x)".BuildWithFloats();
        rt.Run();
        var bytes = (byte[])rt["out"].Value;
        Assert.AreEqual(4, bytes.Length);
        Assert.AreEqual(1.0f, System.BitConverter.ToSingle(bytes, 0));
    }

    [Test]
    public void Float32_ConvertFromBytes_RoundTrip() {
        // float32 → byte[4] → float32 round-trip.
        var rt = "a:float32=2.5\rbytes = convert(a)\rout:float32 = convert(bytes)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(2.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_ConvertFromChar() {
        // char → float32: code point as float.
        var rt = "c = /'A'\rout:float32 = convert(c)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(65.0f, rt["out"].Value);
    }

    [Test]
    public void OptionalFloat32_NullCoalesce() {
        var rt = Funny.Hardcore
            .WithDialect(
                floatFamilySupport: FloatFamilySupport.Float32AndFloat64,
                optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("x:float32? = none\rout = x ?? 3.14");
        rt.Run();
        // out is whatever ?? resolves to (float32 since left is float32?).
        Assert.AreEqual(3.14f, rt["out"].Value);
    }

    // sum / range / range-step on float32.
    [Test]
    public void Float32_Sum_Array() {
        var rt = "x:float32[] = [1.0, 2.5, 6.0]\rout = x.sum()".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(9.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_Range_Array() {
        var rt = "out:float32[] = [1.0..3.0]".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, rt["out"].Value);
    }

    [Test]
    public void Float32_RangeStep_Array() {
        var rt = "out:float32[] = [0.0..1.0 step 0.5]".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(new[] { 0.0f, 0.5f, 1.0f }, rt["out"].Value);
    }

    // toText on float32 uses System.Single.ToString — works automatically.
    [TestCase("x:float32 = 3.14\rout = x.toText()", "3.14")]
    [TestCase("x:float32 = -0.5\rout = x.toText()", "-0.5")]
    public void Float32_ToText(string expr, string expected) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        Assert.AreEqual(expected, rt["out"].Value.ToString());
    }

    // ─────────────────────────────────────────────────────────────────
    // Float32 IEEE 754 special values: ±Infinity, NaN, ±0.0
    // ─────────────────────────────────────────────────────────────────

    [Test]
    public void Float32_DivByZero_PositiveInfinity() {
        var rt = "a:float32=5.0\rb:float32=0.0\rout = a / b".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.PositiveInfinity, rt["out"].Value);
    }

    [Test]
    public void Float32_DivByZero_NegativeInfinity() {
        var rt = "a:float32=-5.0\rb:float32=0.0\rout = a / b".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.NegativeInfinity, rt["out"].Value);
    }

    [Test]
    public void Float32_ZeroDivZero_IsNaN() {
        var rt = "a:float32=0.0\rb:float32=0.0\rout = a / b".BuildWithFloats();
        rt.Run();
        Assert.IsTrue(float.IsNaN((float)rt["out"].Value));
    }

    [Test]
    public void Float32_Sqrt_NegativeIsNaN() {
        var rt = "a:float32=-1.0\rout = sqrt(a)".BuildWithFloats();
        rt.Run();
        Assert.IsTrue(float.IsNaN((float)rt["out"].Value));
    }

    [Test]
    public void Float32_Log_ZeroIsNegInfinity() {
        var rt = "a:float32=0.0\rout = log(a)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.NegativeInfinity, rt["out"].Value);
    }

    [Test]
    public void Float32_Log_NegativeIsNaN() {
        var rt = "a:float32=-1.0\rout = log(a)".BuildWithFloats();
        rt.Run();
        Assert.IsTrue(float.IsNaN((float)rt["out"].Value));
    }

    [Test]
    public void Float32_Asin_OutOfDomainIsNaN() {
        var rt = "a:float32=2.0\rout = asin(a)".BuildWithFloats();
        rt.Run();
        Assert.IsTrue(float.IsNaN((float)rt["out"].Value));
    }

    [Test]
    public void Float32_Acos_OutOfDomainIsNaN() {
        var rt = "a:float32=-2.0\rout = acos(a)".BuildWithFloats();
        rt.Run();
        Assert.IsTrue(float.IsNaN((float)rt["out"].Value));
    }

    [Test]
    public void Float32_Exp_LargeArgIsInfinity() {
        var rt = "a:float32=1000.0\rout = exp(a)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.PositiveInfinity, rt["out"].Value);
    }

    // NaN comparison semantics (IEEE 754): NaN is unordered.
    // Per existing convention in NFun's Less/More fns (IEEE754Guard), comparisons
    // with NaN return false. NaN != NaN is true.
    [Test]
    public void Float32_NaN_NotEqualToItself() {
        var rt = "a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan != nan".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_NaN_EqualReturnsFalse() {
        var rt = "a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan == nan".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Float32_NaN_LessThanReturnsFalse() {
        var rt = "a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan < 1.0".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Float32_PositiveZero_EqualsNegativeZero() {
        // IEEE 754: +0.0 == -0.0
        var rt = "pos:float32=0.0\rneg:float32=-0.0\rout = pos == neg".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_Modulus_ByZeroIsNaN() {
        var rt = "a:float32=5.5\rb:float32=0.0\rout = a % b".BuildWithFloats();
        rt.Run();
        Assert.IsTrue(float.IsNaN((float)rt["out"].Value));
    }

    // min / max on float32[] via IComparable.
    [Test]
    public void Float32_MaxArray() {
        var rt = "x:float32[] = [1.0, 10.0, 6.0]\rout = max(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(10.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_MinArray() {
        var rt = "x:float32[] = [1.0, 10.0, 6.0]\rout = min(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("Float32", rt["out"].Type.ToString());
        Assert.AreEqual(1.0f, rt["out"].Value);
    }

    // abs(MinValue) cannot be represented in same width (two's-complement
    // asymmetry) — Math.Abs throws. Symmetric for Int8 and Int16.
    [TestCase("x:int8=-128\r out=x.abs()")]
    [TestCase("x:int16=-32768\r out=x.abs()")]
    public void Abs_OfMinValue_Throws(string expr) =>
        Assert.Throws<Exceptions.FunnyRuntimeException>(
            () => Funny.Hardcore.Build(expr).Calc());

    #region Float32AndFloat64 exhaustive

    private const float F32Tol = 1e-6f;
    private const double F64Tol = 1e-14;

    private static float RunF32(string expr) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        return (float)rt["out"].Value;
    }

    private static double RunF64(string expr) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        return (double)rt["out"].Value;
    }

    private static object RunAny(string expr) {
        var rt = expr.BuildWithFloats();
        rt.Run();
        return rt["out"].Value;
    }

    private static void AssertApproxF32(float expected, float actual, float tol = F32Tol) =>
        Assert.That(Math.Abs(actual - expected), Is.LessThan(tol),
            $"expected≈{expected}, got {actual}");

    private static void AssertApproxF64(double expected, double actual, double tol = F64Tol) =>
        Assert.That(Math.Abs(actual - expected), Is.LessThan(tol),
            $"expected≈{expected}, got {actual}");

    // ────────────────────────────────────────────────────────────────
    // sqrt
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_sqrt_positive()   => Assert.AreEqual(4.0f,       RunF32("out:float32 = sqrt(16.0)"));
    [Test] public void Float32_sqrt_zero()       => Assert.AreEqual(0.0f,       RunF32("out:float32 = sqrt(0.0)"));
    [Test] public void Float32_sqrt_one()        => Assert.AreEqual(1.0f,       RunF32("out:float32 = sqrt(1.0)"));
    [Test] public void Float32_sqrt_small()      => AssertApproxF32(0.001f,      RunF32("a:float32=1e-6\rout = sqrt(a)"));
    [Test] public void Float32_sqrt_maxvalue_no_overflow() =>
        Assert.IsFalse(float.IsInfinity(RunF32("a:float32 = 1e30\rout = sqrt(a)")));
    [Test] public void Float64_sqrt_positive()   => Assert.AreEqual(4.0,   RunF64("out:real = sqrt(16.0)"));
    [Test] public void Float64_sqrt_two()        => AssertApproxF64(Math.Sqrt(2.0), RunF64("out:real = sqrt(2.0)"));
    [Test] public void Float64_sqrt_zero()       => Assert.AreEqual(0.0,   RunF64("out:real = sqrt(0.0)"));
    [Test] public void Float64_sqrt_negative_isNaN() =>
        Assert.IsTrue(double.IsNaN(RunF64("a:real = -1.0\rout = sqrt(a)")));

    // ────────────────────────────────────────────────────────────────
    // sin / cos / tan
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_sin_zero()        => Assert.AreEqual(0.0f, RunF32("out:float32 = sin(0.0)"));
    [Test] public void Float32_sin_pi_half()     => AssertApproxF32(1.0f, RunF32("a:float32=1.5707963\rout = sin(a)"));
    [Test] public void Float32_sin_pi()          => AssertApproxF32(0.0f, RunF32("a:float32=3.14159265\rout = sin(a)"), 1e-5f);
    [Test] public void Float32_sin_negative()    => AssertApproxF32(-1.0f, RunF32("a:float32=-1.5707963\rout = sin(a)"));
    [Test] public void Float32_cos_zero()        => Assert.AreEqual(1.0f, RunF32("out:float32 = cos(0.0)"));
    [Test] public void Float32_cos_pi()          => AssertApproxF32(-1.0f, RunF32("a:float32=3.14159265\rout = cos(a)"), 1e-5f);
    [Test] public void Float32_cos_pi_half()     => AssertApproxF32(0.0f, RunF32("a:float32=1.5707963\rout = cos(a)"), 1e-5f);
    [Test] public void Float32_tan_zero()        => Assert.AreEqual(0.0f, RunF32("out:float32 = tan(0.0)"));
    [Test] public void Float32_tan_pi_quarter()  => AssertApproxF32(1.0f, RunF32("a:float32=0.78539816\rout = tan(a)"));
    [Test] public void Float32_tan_negative()    => AssertApproxF32(-1.0f, RunF32("a:float32=-0.78539816\rout = tan(a)"));

    [Test] public void Float64_sin_zero()        => Assert.AreEqual(0.0, RunF64("out:real = sin(0.0)"));
    [Test] public void Float64_sin_pi_half()     => AssertApproxF64(1.0, RunF64("out:real = sin(3.141592653589793/2.0)"));
    [Test] public void Float64_cos_zero()        => Assert.AreEqual(1.0, RunF64("out:real = cos(0.0)"));
    [Test] public void Float64_cos_pi()          => AssertApproxF64(-1.0, RunF64("out:real = cos(3.141592653589793)"));
    [Test] public void Float64_tan_zero()        => Assert.AreEqual(0.0, RunF64("out:real = tan(0.0)"));
    [Test] public void Float64_tan_pi_quarter()  => AssertApproxF64(1.0, RunF64("out:real = tan(3.141592653589793/4.0)"));

    // ────────────────────────────────────────────────────────────────
    // asin / acos / atan
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_asin_zero()       => Assert.AreEqual(0.0f, RunF32("out:float32 = asin(0.0)"));
    [Test] public void Float32_asin_one()        => AssertApproxF32((float)(Math.PI / 2), RunF32("out:float32 = asin(1.0)"));
    [Test] public void Float32_asin_minus_one()  => AssertApproxF32((float)(-Math.PI / 2), RunF32("out:float32 = asin(-1.0)"));
    [Test] public void Float32_asin_half()       => AssertApproxF32(0.5235988f, RunF32("out:float32 = asin(0.5)"));
    [Test] public void Float32_acos_one()        => Assert.AreEqual(0.0f, RunF32("out:float32 = acos(1.0)"));
    [Test] public void Float32_acos_zero()       => AssertApproxF32((float)(Math.PI / 2), RunF32("out:float32 = acos(0.0)"));
    [Test] public void Float32_acos_minus_one()  => AssertApproxF32((float)Math.PI, RunF32("out:float32 = acos(-1.0)"));
    [Test] public void Float32_atan_zero()       => Assert.AreEqual(0.0f, RunF32("out:float32 = atan(0.0)"));
    [Test] public void Float32_atan_one()        => AssertApproxF32((float)(Math.PI / 4), RunF32("out:float32 = atan(1.0)"));
    [Test] public void Float32_atan_negative()   => AssertApproxF32((float)(-Math.PI / 4), RunF32("out:float32 = atan(-1.0)"));

    [Test] public void Float64_asin_one()        => AssertApproxF64(Math.PI / 2, RunF64("out:real = asin(1.0)"));
    [Test] public void Float64_asin_out_of_domain_isNaN() =>
        Assert.IsTrue(double.IsNaN(RunF64("a:real = 2.0\rout = asin(a)")));
    [Test] public void Float64_acos_one()        => Assert.AreEqual(0.0, RunF64("out:real = acos(1.0)"));
    [Test] public void Float64_acos_out_of_domain_isNaN() =>
        Assert.IsTrue(double.IsNaN(RunF64("a:real = -2.0\rout = acos(a)")));
    [Test] public void Float64_atan_one()        => AssertApproxF64(Math.PI / 4, RunF64("out:real = atan(1.0)"));

    // ────────────────────────────────────────────────────────────────
    // atan2 (2-arg)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_atan2_one_one()       => AssertApproxF32((float)(Math.PI / 4), RunF32("out:float32 = atan2(1.0, 1.0)"));
    [Test] public void Float32_atan2_zero_one()      => Assert.AreEqual(0.0f, RunF32("out:float32 = atan2(0.0, 1.0)"));
    [Test] public void Float32_atan2_one_zero()      => AssertApproxF32((float)(Math.PI / 2), RunF32("out:float32 = atan2(1.0, 0.0)"));
    [Test] public void Float32_atan2_neg_one_zero()  => AssertApproxF32((float)(-Math.PI / 2), RunF32("out:float32 = atan2(-1.0, 0.0)"));
    [Test] public void Float32_atan2_zero_neg_one()  => AssertApproxF32((float)Math.PI, RunF32("out:float32 = atan2(0.0, -1.0)"));

    [Test] public void Float64_atan2_one_one()       => AssertApproxF64(Math.PI / 4, RunF64("out:real = atan2(1.0, 1.0)"));
    [Test] public void Float64_atan2_zero_zero()     => Assert.AreEqual(0.0, RunF64("out:real = atan2(0.0, 0.0)"));

    // ────────────────────────────────────────────────────────────────
    // exp / log / log10 / log(x, base)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_exp_zero()            => Assert.AreEqual(1.0f, RunF32("out:float32 = exp(0.0)"));
    [Test] public void Float32_exp_one()             => AssertApproxF32((float)Math.E, RunF32("out:float32 = exp(1.0)"));
    [Test] public void Float32_exp_negative()        => AssertApproxF32(1.0f / (float)Math.E, RunF32("out:float32 = exp(-1.0)"));
    [Test] public void Float32_exp_neg_large_isZero() =>
        Assert.AreEqual(0.0f, RunF32("a:float32 = -1000.0\rout = exp(a)"));

    [Test] public void Float32_log_one()             => Assert.AreEqual(0.0f, RunF32("out:float32 = log(1.0)"));
    [Test] public void Float32_log_e()               => AssertApproxF32(1.0f, RunF32("a:float32 = 2.7182818\rout = log(a)"));
    [Test] public void Float32_log10_ten()           => AssertApproxF32(1.0f, RunF32("out:float32 = log10(10.0)"));
    [Test] public void Float32_log10_hundred()       => AssertApproxF32(2.0f, RunF32("out:float32 = log10(100.0)"));
    [Test] public void Float32_log10_tenth()         => AssertApproxF32(-1.0f, RunF32("out:float32 = log10(0.1)"));
    [Test] public void Float32_log_base_two_eight()  => AssertApproxF32(3.0f, RunF32("out:float32 = log(8.0, 2.0)"));
    [Test] public void Float32_log_base_ten_100()    => AssertApproxF32(2.0f, RunF32("out:float32 = log(100.0, 10.0)"));

    [Test] public void Float64_exp_zero()            => Assert.AreEqual(1.0, RunF64("out:real = exp(0.0)"));
    [Test] public void Float64_exp_one()             => AssertApproxF64(Math.E, RunF64("out:real = exp(1.0)"));
    [Test] public void Float64_log_one()             => Assert.AreEqual(0.0, RunF64("out:real = log(1.0)"));
    [Test] public void Float64_log_zero_isNegInf()   =>
        Assert.AreEqual(double.NegativeInfinity, RunF64("a:real = 0.0\rout = log(a)"));
    [Test] public void Float64_log10_1000()          => AssertApproxF64(3.0, RunF64("out:real = log10(1000.0)"));
    [Test] public void Float64_log_base_two_1024()   => AssertApproxF64(10.0, RunF64("out:real = log(1024.0, 2.0)"));

    // ────────────────────────────────────────────────────────────────
    // ceil / floor
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_ceil_positive()       => Assert.AreEqual(4.0f,  RunF32("out:float32 = ceil(3.14)"));
    [Test] public void Float32_ceil_zero()           => Assert.AreEqual(0.0f,  RunF32("out:float32 = ceil(0.0)"));
    [Test] public void Float32_ceil_negative()       => Assert.AreEqual(-3.0f, RunF32("out:float32 = ceil(-3.14)"));
    [Test] public void Float32_ceil_integer_valued() => Assert.AreEqual(5.0f,  RunF32("out:float32 = ceil(5.0)"));
    [Test] public void Float32_floor_positive()      => Assert.AreEqual(3.0f,  RunF32("out:float32 = floor(3.99)"));
    [Test] public void Float32_floor_zero()          => Assert.AreEqual(0.0f,  RunF32("out:float32 = floor(0.0)"));
    [Test] public void Float32_floor_negative()      => Assert.AreEqual(-4.0f, RunF32("out:float32 = floor(-3.14)"));
    [Test] public void Float32_floor_integer_valued() => Assert.AreEqual(5.0f,  RunF32("out:float32 = floor(5.0)"));

    [Test] public void Float64_ceil_positive()       => Assert.AreEqual(4.0,  RunF64("out:real = ceil(3.14)"));
    [Test] public void Float64_floor_negative()      => Assert.AreEqual(-4.0, RunF64("out:real = floor(-3.14)"));

    // ────────────────────────────────────────────────────────────────
    // round (1-arg and 2-arg). MidpointRounding.AwayFromZero.
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_round_2arg_one_decimal() =>
        Assert.AreEqual(1.7f, RunF32("out:float32 = round(1.66666, 1)"));
    [Test] public void Float32_round_2arg_two_decimals() =>
        Assert.AreEqual(1.22f, RunF32("out:float32 = round(1.222, 2)"));
    [Test] public void Float32_round_2arg_zero_decimals() =>
        Assert.AreEqual(2.0f, RunF32("out:float32 = round(1.66666, 0)"));
    [Test] public void Float32_round_2arg_half_up_positive() =>
        Assert.AreEqual(1.0f, RunF32("out:float32 = round(0.5, 0)"));
    [Test] public void Float32_round_2arg_half_up_negative() =>
        Assert.AreEqual(-1.0f, RunF32("out:float32 = round(-0.5, 0)"));
    [Test] public void Float32_round_2arg_two_and_half() =>
        Assert.AreEqual(3.0f, RunF32("out:float32 = round(2.5, 0)"));
    [Test] public void Float32_round_2arg_zero()  =>
        Assert.AreEqual(0.0f, RunF32("out:float32 = round(0.0, 2)"));

    [Test] public void Float64_round_2arg_one_decimal() =>
        AssertApproxF64(1.7, RunF64("out:real = round(1.66666, 1)"));
    [Test] public void Float64_round_2arg_half_away_positive() =>
        Assert.AreEqual(1.0, RunF64("out:real = round(0.5, 0)"));
    [Test] public void Float64_round_2arg_half_away_negative() =>
        Assert.AreEqual(-1.0, RunF64("out:real = round(-0.5, 0)"));

    // ────────────────────────────────────────────────────────────────
    // avg (Floats)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_avg_basic()   => Assert.AreEqual(2.0f, RunF32("x:float32[] = [1.0, 2.0, 3.0]\rout = avg(x)"));
    [Test] public void Float32_avg_single()  => Assert.AreEqual(5.0f, RunF32("x:float32[] = [5.0]\rout = avg(x)"));
    [Test] public void Float32_avg_negative_mix() =>
        Assert.AreEqual(0.0f, RunF32("x:float32[] = [-1.0, 0.0, 1.0]\rout = avg(x)"));
    [Test] public void Float32_avg_duplicates() =>
        Assert.AreEqual(2.5f, RunF32("x:float32[] = [2.5, 2.5, 2.5]\rout = avg(x)"));
    [Test] public void Float32_avg_empty_Throws() =>
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => {
            var rt = "x:float32[] = []\rout = avg(x)".BuildWithFloats();
            rt.Run();
        });

    [Test] public void Float64_avg_basic()   => Assert.AreEqual(2.0, RunF64("x:real[] = [1.0, 2.0, 3.0]\rout = avg(x)"));
    [Test] public void Float64_avg_single()  => Assert.AreEqual(5.0, RunF64("x:real[] = [5.0]\rout = avg(x)"));
    [Test] public void Float64_avg_empty_Throws() =>
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => {
            var rt = "x:real[] = []\rout = avg(x)".BuildWithFloats();
            rt.Run();
        });

    // ────────────────────────────────────────────────────────────────
    // sum
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_sum_single()      => Assert.AreEqual(5.0f, RunF32("x:float32[] = [5.0]\rout = sum(x)"));
    [Test] public void Float32_sum_negative()    => Assert.AreEqual(-6.0f, RunF32("x:float32[] = [-1.0, -2.0, -3.0]\rout = sum(x)"));
    [Test] public void Float32_sum_mixed()       => Assert.AreEqual(0.0f, RunF32("x:float32[] = [-2.5, 2.5]\rout = sum(x)"));
    [Test] public void Float32_sum_zeroes()      => Assert.AreEqual(0.0f, RunF32("x:float32[] = [0.0, 0.0, 0.0]\rout = sum(x)"));

    [Test] public void Float64_sum_basic()       => Assert.AreEqual(9.5, RunF64("x:real[] = [1.0, 2.5, 6.0]\rout = sum(x)"));
    [Test] public void Float64_sum_single()      => Assert.AreEqual(5.0, RunF64("x:real[] = [5.0]\rout = sum(x)"));

    // ────────────────────────────────────────────────────────────────
    // min / max (Comparable)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_min_single()      => Assert.AreEqual(5.0f, RunF32("x:float32[] = [5.0]\rout = min(x)"));
    [Test] public void Float32_min_duplicates()  => Assert.AreEqual(1.0f, RunF32("x:float32[] = [1.0, 1.0, 1.0]\rout = min(x)"));
    [Test] public void Float32_min_negatives()   => Assert.AreEqual(-10.0f, RunF32("x:float32[] = [-1.0, -10.0, -5.0]\rout = min(x)"));
    [Test] public void Float32_max_single()      => Assert.AreEqual(5.0f, RunF32("x:float32[] = [5.0]\rout = max(x)"));
    [Test] public void Float32_max_duplicates()  => Assert.AreEqual(1.0f, RunF32("x:float32[] = [1.0, 1.0, 1.0]\rout = max(x)"));
    [Test] public void Float32_max_negatives()   => Assert.AreEqual(-1.0f, RunF32("x:float32[] = [-1.0, -10.0, -5.0]\rout = max(x)"));
    [Test] public void Float32_max_mixed_signs() => Assert.AreEqual(3.0f, RunF32("x:float32[] = [-5.0, 3.0, -1.0]\rout = max(x)"));
    [Test] public void Float32_min_mixed_signs() => Assert.AreEqual(-5.0f, RunF32("x:float32[] = [-5.0, 3.0, -1.0]\rout = min(x)"));

    [Test] public void Float64_min_basic()       => Assert.AreEqual(1.0, RunF64("x:real[] = [1.0, 10.0, 6.0]\rout = min(x)"));
    [Test] public void Float64_max_basic()       => Assert.AreEqual(10.0, RunF64("x:real[] = [1.0, 10.0, 6.0]\rout = max(x)"));
    [Test] public void Float64_min_empty_Throws() =>
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => {
            var rt = "x:real[] = []\rout = min(x)".BuildWithFloats();
            rt.Run();
        });
    [Test] public void Float64_max_empty_Throws() =>
        Assert.Throws<Exceptions.FunnyRuntimeException>(() => {
            var rt = "x:real[] = []\rout = max(x)".BuildWithFloats();
            rt.Run();
        });

    // min/max 2-arg
    [Test] public void Float32_max_2arg()        => Assert.AreEqual(3.4f, RunF32("out:float32 = max(1.0, 3.4)"));
    [Test] public void Float32_min_2arg()        => Assert.AreEqual(1.0f, RunF32("out:float32 = min(1.0, 3.4)"));
    [Test] public void Float32_max_2arg_equal()  => Assert.AreEqual(2.0f, RunF32("out:float32 = max(2.0, 2.0)"));
    [Test] public void Float32_min_2arg_equal()  => Assert.AreEqual(2.0f, RunF32("out:float32 = min(2.0, 2.0)"));

    // ────────────────────────────────────────────────────────────────
    // median (Comparable)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_median_odd()      => Assert.AreEqual(2.0f, RunF32("x:float32[] = [1.0, 2.0, 3.0]\rout = median(x)"));
    [Test] public void Float32_median_single()   => Assert.AreEqual(5.0f, RunF32("x:float32[] = [5.0]\rout = median(x)"));
    [Test] public void Float32_median_negatives_and_zero() =>
        Assert.AreEqual(0.0f, RunF32("x:float32[] = [-1.0, 0.0, 1.0]\rout = median(x)"));
    [Test] public void Float32_median_unsorted() =>
        Assert.AreEqual(3.0f, RunF32("x:float32[] = [5.0, 1.0, 3.0]\rout = median(x)"));

    [Test] public void Float64_median_odd()      => Assert.AreEqual(6.0, RunF64("x:real[] = [1.0, 10.5, 6.0]\rout = median(x)"));
    [Test] public void Float64_median_single()   => Assert.AreEqual(1.0, RunF64("x:real[] = [1.0]\rout = median(x)"));

    // ────────────────────────────────────────────────────────────────
    // sort / sortDescending
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Float32_sort_ascending() {
        var rt = "x:float32[] = [4.0, 3.0, 5.0, 1.0]\rout:float32[] = x.sort()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 1.0f, 3.0f, 4.0f, 5.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_sortDescending() {
        var rt = "x:float32[] = [4.0, 3.0, 5.0, 1.0]\rout:float32[] = x.sortDescending()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 5.0f, 4.0f, 3.0f, 1.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_sort_negatives() {
        var rt = "x:float32[] = [-1.0, -10.0, 5.0, 0.0]\rout:float32[] = x.sort()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { -10.0f, -1.0f, 0.0f, 5.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_sort_single() {
        var rt = "x:float32[] = [7.5]\rout:float32[] = x.sort()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 7.5f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float64_sort_ascending() {
        var rt = "x:real[] = [4.0, 3.0, 5.0, 1.0]\rout:real[] = x.sort()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 1.0, 3.0, 4.0, 5.0 }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float64_sortDescending() {
        var rt = "x:real[] = [4.0, 3.0, 5.0, 1.0]\rout:real[] = x.sortDescending()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 5.0, 4.0, 3.0, 1.0 }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    // ────────────────────────────────────────────────────────────────
    // range / range-step
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Float32_range_0_5() {
        var rt = "out:float32[] = range(0.0, 5.0)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 0.0f, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f },
            (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_range_step_quarter() {
        var rt = "out:float32[] = range(0.0, 1.0, 0.25)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f },
            (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_range_descending() {
        var rt = "out:float32[] = range(5.0, 0.0)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 5.0f, 4.0f, 3.0f, 2.0f, 1.0f, 0.0f },
            (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_range_step_descending() {
        var rt = "out:float32[] = range(1.0, 0.0, 0.25)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 1.0f, 0.75f, 0.5f, 0.25f, 0.0f },
            (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float64_range_basic() {
        var rt = "out:real[] = range(0.0, 3.0)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 0.0, 1.0, 2.0, 3.0 },
            (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float64_range_step_basic() {
        var rt = "out:real[] = range(0.0, 1.0, 0.25)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 0.0, 0.25, 0.5, 0.75, 1.0 },
            (System.Collections.IEnumerable)rt["out"].Value);
    }

    // ────────────────────────────────────────────────────────────────
    // convert: numeric source types → float32 (positive & negative where signed)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_convert_from_int_positive()   => Assert.AreEqual(42.0f, RunAny("x:int = 42\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int_negative()   => Assert.AreEqual(-42.0f, RunAny("x:int = -42\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int_zero()       => Assert.AreEqual(0.0f, RunAny("x:int = 0\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int16_positive() => Assert.AreEqual(30000.0f, RunAny("x:int16 = 30000\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int16_negative() => Assert.AreEqual(-30000.0f, RunAny("x:int16 = -30000\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int8_positive()  => Assert.AreEqual(100.0f, RunAny("x:int8 = 100\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int8_negative()  => Assert.AreEqual(-100.0f, RunAny("x:int8 = -100\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int64_positive() => Assert.AreEqual(1000000.0f, RunAny("x:int64 = 1000000\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_int64_negative() => Assert.AreEqual(-1000000.0f, RunAny("x:int64 = -1000000\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_byte()           => Assert.AreEqual(255.0f, RunAny("x:byte = 255\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_uint16()         => Assert.AreEqual(65535.0f, RunAny("x:uint16 = 65535\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_uint32()         => Assert.AreEqual(4000000.0f, RunAny("x:uint32 = 4000000\rout:float32 = convert(x)"));
    [Test] public void Float32_convert_from_uint64()         => Assert.AreEqual(1000000.0f, RunAny("x:uint64 = 1000000\rout:float32 = convert(x)"));

    // ────────────────────────────────────────────────────────────────
    // convert: float32 → each numeric target
    // ────────────────────────────────────────────────────────────────

    // float32 → integer: truncation toward zero (spec Functions.md convert matrix "fractional
    // part silently truncated"). Same as real → integer; matches C/Java/Go/Rust cast semantics.
    [Test] public void Float32_convert_to_int_truncates()   => Assert.AreEqual(3, RunAny("x:float32 = 3.9\rout:int = convert(x)"));
    [Test] public void Float32_convert_to_int_negative_truncates() =>
        Assert.AreEqual(-3, RunAny("x:float32 = -3.9\rout:int = convert(x)"));
    [Test] public void Float32_convert_to_int_exact_integer() =>
        Assert.AreEqual(5, RunAny("x:float32 = 5.0\rout:int = convert(x)"));
    [Test] public void Float32_convert_to_int_zero() => Assert.AreEqual(0, RunAny("x:float32 = 0.0\rout:int = convert(x)"));
    [Test] public void Float32_convert_to_int16()    => Assert.AreEqual((short)42, RunAny("x:float32 = 42.7\rout:int16 = convert(x)"));
    [Test] public void Float32_convert_to_int16_exact() =>
        Assert.AreEqual((short)42, RunAny("x:float32 = 42.0\rout:int16 = convert(x)"));
    [Test] public void Float32_convert_to_int8()     => Assert.AreEqual((sbyte)-5, RunAny("x:float32 = -5.7\rout:int8 = convert(x)"));
    [Test] public void Float32_convert_to_int8_exact() =>
        Assert.AreEqual((sbyte)-5, RunAny("x:float32 = -5.0\rout:int8 = convert(x)"));
    [Test] public void Float32_convert_to_int64()    => Assert.AreEqual(100000L, RunAny("x:float32 = 100000.7\rout:int64 = convert(x)"));
    [Test] public void Float32_convert_to_int64_exact() =>
        Assert.AreEqual(100000L, RunAny("x:float32 = 100000.0\rout:int64 = convert(x)"));
    [Test] public void Float32_convert_to_byte()     => Assert.AreEqual((byte)200, RunAny("x:float32 = 200.7\rout:byte = convert(x)"));
    [Test] public void Float32_convert_to_byte_exact() =>
        Assert.AreEqual((byte)200, RunAny("x:float32 = 200.0\rout:byte = convert(x)"));
    [Test] public void Float32_convert_to_uint16()   => Assert.AreEqual((ushort)60000, RunAny("x:float32 = 60000.9\rout:uint16 = convert(x)"));
    [Test] public void Float32_convert_to_uint16_exact() =>
        Assert.AreEqual((ushort)60000, RunAny("x:float32 = 60000.0\rout:uint16 = convert(x)"));
    [Test] public void Float32_convert_to_uint32()   => Assert.AreEqual(4000000u, RunAny("x:float32 = 4000000.7\rout:uint32 = convert(x)"));
    [Test] public void Float32_convert_to_uint32_exact() =>
        Assert.AreEqual(4000000u, RunAny("x:float32 = 4000000.0\rout:uint32 = convert(x)"));
    [Test] public void Float32_convert_to_uint64()   => Assert.AreEqual(1000000UL, RunAny("x:float32 = 1000000.9\rout:uint64 = convert(x)"));
    [Test] public void Float32_convert_to_uint64_exact() =>
        Assert.AreEqual(1000000UL, RunAny("x:float32 = 1000000.0\rout:uint64 = convert(x)"));

    // widening float32 → real
    [Test] public void Float32_convert_to_real_widens() =>
        AssertApproxF64(2.5, (double)RunAny("x:float32 = 2.5\rout:real = convert(x)"), 1e-6);

    // ────────────────────────────────────────────────────────────────
    // comparison operators on float32 (all 6)
    // ────────────────────────────────────────────────────────────────

    // (1.5, 2.5)
    [Test] public void Float32_cmp_lt_true()   => Assert.AreEqual(true,  RunAny("a:float32=1.5\rb:float32=2.5\rout = a < b"));
    [Test] public void Float32_cmp_gt_false()  => Assert.AreEqual(false, RunAny("a:float32=1.5\rb:float32=2.5\rout = a > b"));
    [Test] public void Float32_cmp_le_true()   => Assert.AreEqual(true,  RunAny("a:float32=1.5\rb:float32=2.5\rout = a <= b"));
    [Test] public void Float32_cmp_ge_false()  => Assert.AreEqual(false, RunAny("a:float32=1.5\rb:float32=2.5\rout = a >= b"));
    [Test] public void Float32_cmp_eq_false()  => Assert.AreEqual(false, RunAny("a:float32=1.5\rb:float32=2.5\rout = a == b"));
    [Test] public void Float32_cmp_ne_true()   => Assert.AreEqual(true,  RunAny("a:float32=1.5\rb:float32=2.5\rout = a != b"));

    // (1.5, 1.5)
    [Test] public void Float32_cmp_eq_equal()  => Assert.AreEqual(true,  RunAny("a:float32=1.5\rb:float32=1.5\rout = a == b"));
    [Test] public void Float32_cmp_ne_equal()  => Assert.AreEqual(false, RunAny("a:float32=1.5\rb:float32=1.5\rout = a != b"));
    [Test] public void Float32_cmp_le_equal()  => Assert.AreEqual(true,  RunAny("a:float32=1.5\rb:float32=1.5\rout = a <= b"));
    [Test] public void Float32_cmp_ge_equal()  => Assert.AreEqual(true,  RunAny("a:float32=1.5\rb:float32=1.5\rout = a >= b"));

    // NaN comparison semantics (IEEE 754 unordered → false)
    [Test] public void Float32_cmp_NaN_lt_x_false() =>
        Assert.AreEqual(false, RunAny("a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan < 1.0"));
    [Test] public void Float32_cmp_NaN_gt_x_false() =>
        Assert.AreEqual(false, RunAny("a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan > 1.0"));
    [Test] public void Float32_cmp_NaN_le_x_false() =>
        Assert.AreEqual(false, RunAny("a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan <= 1.0"));
    [Test] public void Float32_cmp_NaN_ge_x_false() =>
        Assert.AreEqual(false, RunAny("a:float32=0.0\rb:float32=0.0\rnan = a / b\rout = nan >= 1.0"));

    // ────────────────────────────────────────────────────────────────
    // toText on float32 — additional edges
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_toText_zero()             =>
        Assert.AreEqual("0", RunAny("x:float32 = 0.0\rout = x.toText()"));
    [Test] public void Float32_toText_integer_valued()   =>
        Assert.AreEqual("5", RunAny("x:float32 = 5.0\rout = x.toText()"));
    // NFun uses `∞`/`-∞`/`NaN` symbols (Specs/Texts.md) rather than the CLR default strings.
    [Test] public void Float32_toText_positive_infinity() =>
        Assert.AreEqual("∞", RunAny("a:float32=1.0\rb:float32=0.0\rout = (a / b).toText()"));
    [Test] public void Float32_toText_negative_infinity() =>
        Assert.AreEqual("-∞", RunAny("a:float32=-1.0\rb:float32=0.0\rout = (a / b).toText()"));
    [Test] public void Float32_toText_nan() {
        var s = (string)RunAny("a:float32=0.0\rb:float32=0.0\rout = (a / b).toText()");
        Assert.IsNotEmpty(s);
        StringAssert.AreEqualIgnoringCase("NaN", s);
    }

    // ────────────────────────────────────────────────────────────────
    // toHexText / toBinText should REJECT float32 (integer-only constraint)
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_toHexText_Rejected() =>
        Assert.Throws<Exceptions.FunnyParseException>(() => {
            var rt = "x:float32 = 3.14\rout = x.toHexText()".BuildWithFloats();
            rt.Run();
        });

    [Test] public void Float32_toBinText_Rejected() =>
        Assert.Throws<Exceptions.FunnyParseException>(() => {
            var rt = "x:float32 = 3.14\rout = x.toBinText()".BuildWithFloats();
            rt.Run();
        });

    // ────────────────────────────────────────────────────────────────
    // Array element operations — HOFs on float32[]
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Float32_filter_positive() {
        var rt = "x:float32[] = [1.0, -2.0, 3.0, -4.0]\rout:float32[] = x.filter(rule it > 0.0)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 1.0f, 3.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_filter_none_match() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout:float32[] = x.filter(rule it > 10.0)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(Array.Empty<float>(), (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_map_double() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout:float32[] = x.map(rule it * 2.0)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 2.0f, 4.0f, 6.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_map_negate() {
        var rt = "x:float32[] = [1.0, -2.0, 3.0]\rout:float32[] = x.map(rule -it)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { -1.0f, 2.0f, -3.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_fold_sum() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout:float32 = x.fold(rule it1 + it2)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(6.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_fold_with_seed() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout:float32 = x.fold(10.0, rule it1 + it2)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(16.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_count_with_predicate() {
        var rt = "x:float32[] = [1.0, -2.0, 3.0, -4.0, 5.0]\rout = x.count(rule it > 0.0)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3, rt["out"].Value);
    }

    [Test]
    public void Float32_any_with_predicate_true() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout = x.any(rule it > 2.5)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_any_with_predicate_false() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout = x.any(rule it > 10.0)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Float32_all_with_predicate_true() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout = x.all(rule it > 0.0)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(true, rt["out"].Value);
    }

    [Test]
    public void Float32_all_with_predicate_false() {
        var rt = "x:float32[] = [1.0, -2.0, 3.0]\rout = x.all(rule it > 0.0)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(false, rt["out"].Value);
    }

    [Test]
    public void Float32_reverse() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0]\rout:float32[] = x.reverse()".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 3.0f, 2.0f, 1.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_concat() {
        var rt = "a:float32[] = [1.0, 2.0]\rb:float32[] = [3.0, 4.0]\rout:float32[] = concat(a, b)".BuildWithFloats();
        rt.Run();
        CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f, 4.0f }, (System.Collections.IEnumerable)rt["out"].Value);
    }

    [Test]
    public void Float32_first() {
        var rt = "x:float32[] = [1.5, 2.5, 3.5]\rout:float32 = x.first()".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(1.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_last() {
        var rt = "x:float32[] = [1.5, 2.5, 3.5]\rout:float32 = x.last()".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(3.5f, rt["out"].Value);
    }

    [Test]
    public void Float32_count_array() {
        var rt = "x:float32[] = [1.0, 2.0, 3.0, 4.0]\rout = x.count()".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(4, rt["out"].Value);
    }

    // ────────────────────────────────────────────────────────────────
    // Boundary values — Float32 MinValue/MaxValue/Epsilon/precision limit
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Float32_maxvalue_literal() {
        // Float32.MaxValue ≈ 3.4028235e38. Written as real then converted narrows exactly.
        var rt = "x:real = 3.4028235e38\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.MaxValue, rt["out"].Value);
    }

    [Test]
    public void Float32_minvalue_literal() {
        // Float32.MinValue ≈ -3.4028235e38.
        var rt = "x:real = -3.4028235e38\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(float.MinValue, rt["out"].Value);
    }

    [Test]
    public void Float32_precision_2pow24_exact() {
        // 2^24 = 16777216 is exactly representable.
        var rt = "x:int = 16777216\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(16777216.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_precision_2pow24_plus_one_loses_precision() {
        // 2^24 + 1 = 16777217 is NOT representable → rounds to 16777216.
        var rt = "x:int = 16777217\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        Assert.AreEqual(16777216.0f, rt["out"].Value);
    }

    [Test]
    public void Float32_epsilon_smallest_positive_survives_convert() {
        // A very small positive real → convert to float32 → still positive nonzero.
        // 1e-40 is denormalized in float32 but not zero.
        var rt = "x:real = 1e-40\rout:float32 = convert(x)".BuildWithFloats();
        rt.Run();
        var v = (float)rt["out"].Value;
        Assert.That(v, Is.GreaterThan(0.0f), "denormal preserved");
        Assert.That(v, Is.LessThan(1e-30f));
    }

    // ────────────────────────────────────────────────────────────────
    // Real-divide (`/`) explicit — generic over Floats
    // ────────────────────────────────────────────────────────────────

    [Test] public void Float32_divide_basic()     => Assert.AreEqual(2.5f, RunF32("a:float32=5.0\rb:float32=2.0\rout = a / b"));
    [Test] public void Float32_divide_by_one()    => Assert.AreEqual(3.14f, RunF32("a:float32=3.14\rb:float32=1.0\rout = a / b"));
    [Test] public void Float32_divide_neg_by_neg() => Assert.AreEqual(2.0f, RunF32("a:float32=-4.0\rb:float32=-2.0\rout = a / b"));
    [Test] public void Float64_divide_basic()     => Assert.AreEqual(2.5, RunF64("a:real=5.0\rb:real=2.0\rout = a / b"));

    // ────────────────────────────────────────────────────────────────
    // Cross-width interop: Float32 op Real widens to Real
    // ────────────────────────────────────────────────────────────────

    [Test]
    public void Mixed_Float32_And_Real_Widens_To_Real() {
        var rt = "x:float32 = 1.5\ry:real = 2.5\rout = x + y".BuildWithFloats();
        rt.Run();
        Assert.IsInstanceOf<double>(rt["out"].Value);
        Assert.AreEqual(4.0, (double)rt["out"].Value);
    }

    // Format specifier on float32 negative-zero and Inf display
    [Test]
    public void Float32_Format_NegativeZero() {
        // -0.0 default formats as "0" or "-0" depending on culture; assert non-throwing and non-empty.
        var rt = "x:float32 = -0.0\rout = '{x}'".BuildWithFloats();
        rt.Run();
        Assert.IsNotEmpty((string)rt["out"].Value);
    }

    [Test]
    public void Float32_Format_Infinity_DoesNotThrow() {
        var rt = "a:float32 = 1.0\rb:float32 = 0.0\rout = '{a/b}'".BuildWithFloats();
        rt.Run();
        Assert.IsNotEmpty((string)rt["out"].Value);
    }

    [Test]
    public void Float32_Format_Mask_TwoDecimals() {
        var rt = "x:float32 = 3.14159\rout = '{x:0.00}'".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("3.14", rt["out"].Value);
    }

    [Test]
    public void Float32_Format_Mask_PadInteger() {
        var rt = "x:float32 = 42.0\rout = '{x:0000}'".BuildWithFloats();
        rt.Run();
        Assert.AreEqual("0042", rt["out"].Value);
    }

    // Sci format for float32
    [Test]
    public void Float32_Format_Scientific() {
        var rt = "x:float32 = 314.159\rout = '{x:sci}'".BuildWithFloats();
        rt.Run();
        // Format is `1.234567e+002`-style (see FormatSpecifierTest). Just verify shape.
        var s = (string)rt["out"].Value;
        StringAssert.Contains("e", s.ToLowerInvariant());
    }

    #endregion Float32AndFloat64 exhaustive
}
