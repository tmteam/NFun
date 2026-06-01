using NFun;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Behaviour matrix for primitive-int type inference under all three
/// IntegerPreferredType dialects, exercising the paths through
/// SimplePrimitiveSolver (PureGeneric ops) and the fallback TIC
/// (GenericFunctionBase ops like shifts).
///
/// Dialects:
///   I32  — default. Integer literals prefer Int32.
///   I64  — integer literals prefer Int64.
///   Real — Sonica's dialect. Integer literals prefer Real (Double).
///
/// Tests split into:
///   OBVIOUS  — single answer, no ambiguity, regression shield.
///   TODECIDE — current behaviour recorded, semantics under discussion.
///
/// The Real-dialect cases mirror exactly the failing tests in
/// sonica-gh/tests/.../FunBlockValuesTest.cs that broke on the
/// 1.0.3 → 1.1.0 NFun upgrade.
/// </summary>
[TestFixture]
public class SonicaCompatTest {
    private static FunnyRuntime Build(string s, IntegerPreferredType pref) =>
        Funny.Hardcore
            .WithDialect(IfExpressionSetup.IfIfElse, pref, RealClrType.IsDouble)
            .Build(s);

    private static object Run(string s, IntegerPreferredType pref) {
        var rt = Build(s, pref);
        rt.Run();
        return rt["out"].Value;
    }

    private static BaseFunnyType TypeOf(string s, IntegerPreferredType pref) =>
        Build(s, pref)["out"].Type.BaseType;

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — bare literal under each dialect picks Preferred.
    // ───────────────────────────────────────────────────────────────
    [TestCase(IntegerPreferredType.I32, BaseFunnyType.Int32)]
    [TestCase(IntegerPreferredType.I64, BaseFunnyType.Int64)]
    [TestCase(IntegerPreferredType.Real, BaseFunnyType.Real)]
    public void BareLiteral_FollowsDialect(IntegerPreferredType pref, BaseFunnyType expected)
        => Assert.AreEqual(expected, TypeOf("1", pref));

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — literal arithmetic (+ - *) keeps Preferred.
    // Arithmetical constraint (Real, U24): Real fits → Pref wins.
    // ───────────────────────────────────────────────────────────────
    [TestCase("1 + 1", IntegerPreferredType.I32, BaseFunnyType.Int32, 2)]
    [TestCase("1 - 1", IntegerPreferredType.I32, BaseFunnyType.Int32, 0)]
    [TestCase("2 * 3", IntegerPreferredType.I32, BaseFunnyType.Int32, 6)]
    [TestCase("1 + 1", IntegerPreferredType.I64, BaseFunnyType.Int64, 2L)]
    [TestCase("1 + 1", IntegerPreferredType.Real, BaseFunnyType.Real, 2.0)]
    public void LiteralArithmetic_FollowsDialect(string expr, IntegerPreferredType pref,
        BaseFunnyType type, object value) {
        Assert.AreEqual(type, TypeOf(expr, pref));
        Assert.AreEqual(value, Run(expr, pref));
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — literal bitwise & | ^ under I32 dialect: Pref fits
    // [U8..I96] interval, returns I32 directly.
    // ───────────────────────────────────────────────────────────────
    [TestCase("1 & 1",   1)]
    [TestCase("15 & 1",  1)]
    [TestCase("1 | 1",   1)]
    [TestCase("15 | 1",  15)]
    [TestCase("15 ^ 0",  15)]
    [TestCase("15 ^ 15", 0)]
    public void LiteralBitwise_I32Dialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.I32));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.I32));
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS (regression-shield, sonica-gh) — literal bitwise under
    // Real dialect. Pref=Real doesn't fit Integers constraint; SPS must
    // refine the abstract I96 result down to a concrete integer type.
    // 1.0.3 produced Int32 here via TicTypesConverter's smart rule;
    // 1.1.0 SPS bypassed that and widened to Int64 (the bug).
    // ───────────────────────────────────────────────────────────────
    [TestCase("1 & 1",   1)]
    [TestCase("1 & 0",   0)]
    [TestCase("15 & 1",  1)]
    [TestCase("1 | 1",   1)]
    [TestCase("15 | 1",  15)]
    [TestCase("15 ^ 0",  15)]
    [TestCase("15 ^ 15", 0)]
    public void LiteralBitwise_RealDialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.Real));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.Real));
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — literal shifts. GenericFunctionBase, not PureGeneric;
    // routed through full TIC (not SPS), so already-correct in all
    // dialects. Result follows LHS.
    // ───────────────────────────────────────────────────────────────
    [TestCase("1<<0", 1)]
    [TestCase("1<<1", 2)]
    [TestCase("1<<2", 4)]
    [TestCase("1>>0", 1)]
    [TestCase("2>>1", 1)]
    [TestCase("4>>2", 1)]
    public void LiteralShift_RealDialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.Real));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.Real));
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — large positive literal (> Int32.Max). Setup logic in
    // SPS overrides Preferred to I64 for such values, so ans is I64
    // regardless of dialect.
    // ───────────────────────────────────────────────────────────────
    // 5_000_000_000 is even; & 1 = 0. Value verifies the chosen Int64
    // path uses the full bit-width, not a narrowed truncation.
    [TestCase(IntegerPreferredType.I32)]
    [TestCase(IntegerPreferredType.I64)]
    [TestCase(IntegerPreferredType.Real)]
    public void HugeLiteral_AlwaysInt64(IntegerPreferredType pref) {
        Assert.AreEqual(BaseFunnyType.Int64, TypeOf("5000000000 & 1", pref));
        Assert.AreEqual(0L, Run("5000000000 & 1", pref));
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — typed input × typed input: types fully determined by
    // operand annotations; dialect irrelevant.
    // ───────────────────────────────────────────────────────────────
    [TestCase("x:int;  q:int;  out = x & q", BaseFunnyType.Int32)]
    [TestCase("x:uint; q:uint; out = x & q", BaseFunnyType.UInt32)]
    [TestCase("x:byte; q:byte; out = x & q", BaseFunnyType.UInt8)]
    [TestCase("x:int16; q:int16; out = x & q", BaseFunnyType.Int16)]
    [TestCase("x:int64; q:int64; out = x & q", BaseFunnyType.Int64)]
    public void TypedInputs_Bitwise_ResultIsArgType(string expr, BaseFunnyType expected) {
        foreach (var pref in new[] { IntegerPreferredType.I32, IntegerPreferredType.I64, IntegerPreferredType.Real })
            Assert.AreEqual(expected, TypeOf(expr, pref), $"dialect={pref}");
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — typed shift LHS: result follows LHS type.
    // ───────────────────────────────────────────────────────────────
    [TestCase("x:int;  out = x << 1",  BaseFunnyType.Int32)]
    [TestCase("x:uint; out = x << 1",  BaseFunnyType.UInt32)]
    [TestCase("x:byte; out = x << 1",  BaseFunnyType.UInt8)]
    [TestCase("x:int64; out = x << 1", BaseFunnyType.Int64)]
    public void TypedShift_ResultIsLhsType(string expr, BaseFunnyType expected)
        => Assert.AreEqual(expected, TypeOf(expr, IntegerPreferredType.Real));

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — explicit output annotation pins the result type.
    // ───────────────────────────────────────────────────────────────
    [TestCase("out:int16 = 1 & 1", BaseFunnyType.Int16)]
    [TestCase("out:byte = 1 & 1",  BaseFunnyType.UInt8)]
    [TestCase("out:uint = 1 & 1",  BaseFunnyType.UInt32)]
    [TestCase("out:int64 = 1 & 1", BaseFunnyType.Int64)]
    [TestCase("out:int = 1 & 1",   BaseFunnyType.Int32)]
    public void OutputAnnotation_OverridesInference(string expr, BaseFunnyType expected) {
        foreach (var pref in new[] { IntegerPreferredType.I32, IntegerPreferredType.I64, IntegerPreferredType.Real })
            Assert.AreEqual(expected, TypeOf(expr, pref), $"dialect={pref}");
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — `//` (DivideInt) is PureGeneric with Integers constraint
    // — same SPS path as bitwise; refinement rule must apply identically.
    // ───────────────────────────────────────────────────────────────
    [TestCase("10 // 3", 3)]
    public void NonBit_PureGenericIntegers_RealDialect_IsInt32(string expr, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, IntegerPreferredType.Real));
        Assert.AreEqual(expected, Run(expr, IntegerPreferredType.Real));
    }

    // ───────────────────────────────────────────────────────────────
    // OBVIOUS — `%` (Remainder) uses Numbers constraint (Anc=Real),
    // NOT Integers — under Real dialect Pref=Real fits and result is
    // Real. Recording to lock this in; do NOT migrate `%` to Integers
    // without intent.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void Remainder_RealDialect_IsReal() {
        Assert.AreEqual(BaseFunnyType.Real, TypeOf("10 % 3", IntegerPreferredType.Real));
        Assert.AreEqual(1.0, Run("10 % 3", IntegerPreferredType.Real));
    }

    [Test]
    public void Remainder_I32Dialect_IsInt32() {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf("10 % 3", IntegerPreferredType.I32));
        Assert.AreEqual(1, Run("10 % 3", IntegerPreferredType.I32));
    }

    // ───────────────────────────────────────────────────────────────
    // TODECIDE — hex literals. They have integer-only descendant
    // (SetupHexBinPositive: desc=U8, anc=I96, pref=I32) so SPS resolves
    // independently of dialect. Recording current behaviour.
    // ───────────────────────────────────────────────────────────────
    [TestCase("0xff & 0xf",  IntegerPreferredType.Real, 15)]
    [TestCase("0xff & 0xf",  IntegerPreferredType.I32,  15)]
    [TestCase("0xff & 0xf",  IntegerPreferredType.I64,  15)]
    public void HexLiteral_Bitwise_RecordedBehaviour(string expr, IntegerPreferredType pref, int expected) {
        Assert.AreEqual(BaseFunnyType.Int32, TypeOf(expr, pref));
        Assert.AreEqual(expected, Run(expr, pref));
    }

    // ───────────────────────────────────────────────────────────────
    // TODECIDE — typed × literal under different dialects. Literal '1'
    // is unbounded — should it adapt to the typed operand or fight it?
    // 1.0.3 silently adapted; current behaviour recorded.
    // ───────────────────────────────────────────────────────────────
    [TestCase("x:int;  out = x & 1", BaseFunnyType.Int32)]
    [TestCase("x:uint; out = x & 1", BaseFunnyType.UInt32)]
    [TestCase("x:byte; out = x & 1", BaseFunnyType.UInt8)]
    [TestCase("x:int16; out = x & 1", BaseFunnyType.Int16)]
    public void TypedTimesLiteral_Bitwise_AdaptsToTyped(string expr, BaseFunnyType expected)
        => Assert.AreEqual(expected, TypeOf(expr, IntegerPreferredType.Real));

    // ───────────────────────────────────────────────────────────────
    // TODECIDE — user's example: `x:uint; out = x << x` — shift LHS
    // and RHS both UInt32. C# rejects (RHS must be int), but NFun
    // shift signature is `(T:Integers, UInt8) → T`. Need to check
    // whether RHS narrowing to UInt8 is forced or NFun is loose.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void UintShiftByUint_RecordedBehaviour() {
        // Status: PARSE ERROR — `<<`'s RHS is fixed UInt8, can't accept UInt32.
        Assert.Throws<NFun.Exceptions.FunnyParseException>(
            () => Build("x:uint; out = x << x", IntegerPreferredType.Real));
    }

    // ───────────────────────────────────────────────────────────────
    // RESOLVED — abstract anc paths other than I96 are NOT reachable
    // from built-in operators. Audit (grep `GenericConstrains.*` in
    // src/NFun/Functions/): only Integers (I96), Arithmetical (Real),
    // Numbers (Real), SignedNumber (Real), Any, Comparable are used.
    //   Integers3264 = (I96, U24)  — defined, unused.
    //   Integers32   = (I48, null) — defined, unused.
    // I96 is the ONLY abstract that arises as a SPS resolution result,
    // so RefineAncestor handling I96-only is complete for current
    // built-ins. If a future op adopts Integers3264/Integers32, extend
    // RefineAncestor in lockstep.
    // ───────────────────────────────────────────────────────────────
}
