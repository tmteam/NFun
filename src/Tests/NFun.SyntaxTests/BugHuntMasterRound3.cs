using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 3,
/// post Round-1/2 fixes). 3 agents × ~100 iterations. 2 confirmed bugs.
/// </summary>
public class BugHuntMasterRound3 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR3Bug1 — `??` rejects when LCA(unwrap(left), right) would be Any.
    //   Per Optionals.md L106: "The right operand can be any type V. The
    //   result type is LCA(U, V) — the lowest common ancestor of the
    //   unwrapped left element type and the right operand type."
    //
    //   Impl currently rejects cross-tree types with FU887, even though
    //   the symmetric `if-else` widens to Any:
    //     if(true) 1 else 'hello'  →  Any
    //     1 ?? 'hello'             →  FU887 "Incompatible types in '??'"
    //
    //   Spec is explicit, impl is inconsistent. Either restrict spec to
    //   say `??` rejects cross-tree LCA, or fix impl to widen to Any.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR3Bug1_CoalesceCrossTreeLca_ShouldWidenToAny() {
        "out = 1 ?? 'hello'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 1);
    }

    [Test]
    public void MR3Bug1_CoalesceTypedOptionalCrossTreeLca_ShouldWidenToAny() {
        "x:int? = 5\rout = x ?? 'hello'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("out", 5);
    }

    // ───────────────────────────────────────────────────────────────
    // MR3Bug2 — Trailing `?` after any expression crashes with raw
    //   `InvalidOperationException: Start is greater then finish`
    //   from `Interval` constructor instead of clean FU parse error.
    //
    //   Symmetric `5 ??` produces clean FU609 — `5 ?` should too.
    //   Affects literals, variables, paren, arithmetic, arrays, structs.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR3Bug2_TrailingQuestionMark_Literal_CleanFU() {
        Assert.Throws<FunnyParseException>(() => "a = 5 ?".Calc());
    }

    [Test]
    public void MR3Bug2_TrailingQuestionMark_None_CleanFU() {
        Assert.Throws<FunnyParseException>(() =>
            "a = none?".CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR3Bug2_TrailingQuestionMark_Array_CleanFU() {
        Assert.Throws<FunnyParseException>(() => "a = [1,2,3]?".Calc());
    }

    [Test]
    public void MR3Bug2_TrailingQuestionMark_Arithmetic_CleanFU() {
        Assert.Throws<FunnyParseException>(() => "a = 1 + 2 ?".Calc());
    }
}
