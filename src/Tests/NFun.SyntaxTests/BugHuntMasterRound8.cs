using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 8,
/// post Round 1-7 fixes including MR7Bug1-4 + convert text→char).
/// 3 agents (Agent 1 stalled; Agents 2 + 3 returned). 2 confirmed bugs.
/// </summary>
public class BugHuntMasterRound8 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

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

    // ───────────────────────────────────────────────────────────────
    // MR8Bug2 — Type-soundness violation in `bool?` narrowing rule.
    //
    //   Spec (Specs/Optionals.md:265) claims: "Comparing a `bool?` with
    //   `true` or `false` proves not-`none` in BOTH branches (since
    //   `none != true` and `none != false`)."
    //
    //   This is logically incorrect:
    //     `flag == true` is FALSE when flag is `false` OR `none`.
    //   So the else branch does NOT prove `flag != none`.
    //
    //   The implementation faithfully follows the wrong spec, narrowing
    //   `flag` to bare `bool` in both branches. Reaching the else branch
    //   with `flag = none` produces a `bool`-typed value holding `none`
    //   at runtime → InvalidCastException when used.
    //
    //     flag:bool? = none
    //     y = if(flag == true) flag else flag    # narrows flag:bool
    //     z = y and true                          # InvalidCastException
    //
    //   The spec's own example `if(flag == true) flag else false` is
    //   safe only because it doesn't USE `flag` in the else — but the
    //   narrowing it claims is unsound. Either:
    //     (a) Fix the narrowing: `flag == true` proves not-none only
    //         in the then-branch (not the else).
    //     (b) Update the spec to match correct logic.
    // ───────────────────────────────────────────────────────────────
    // After fix: only the THEN-branch of `var == bool_lit` narrows to bool;
    // else-branch keeps T?. So `if(flag==true) flag else flag` over `bool?`
    // yields `bool?` (LCA of bool and bool?), not unsound bare `bool`.
    // No runtime crash — the previously-unsound bool slot now holds the
    // Optional faithfully.
    [Test]
    public void MR8Bug2_BoolOptNarrowing_ElseStaysOptional() {
        var rt = "flag:bool? = none\ry = if(flag == true) flag else flag"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
    }

    // After fix: `y and true` over the resulting bool? must be a COMPILE
    // rejection (and requires bool, not bool?), not a runtime cast crash.
    [Test]
    public void MR8Bug2_BoolOptNarrowing_DownstreamUseRejectedAtCompile() {
        Assert.Throws<FunnyParseException>(() => {
            ("flag:bool? = none\r"
           + "y = if(flag == true) flag else flag\r"
           + "z = y and true")
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        });
    }

    // After fix: `!= true` narrows in ELSE branch only.
    [Test]
    public void MR8Bug2_NotEqualTrue_NarrowsElseOnly() {
        "flag:bool? = false\ry = if(flag != true) false else (flag and true)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", false);
    }

    // 2b. Control: spec's own example (using `false` literal in else) — safe.
    [Test]
    public void MR8Bug2_Control_SpecExampleWithFalseLiteral_Works() {
        "flag:bool? = none\ry = if(flag == true) flag else false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", false);
    }

    // 2c. Control: explicit not-none check works correctly.
    [Test]
    public void MR8Bug2_Control_ExplicitNotNoneCheck_NarrowsCorrectly() {
        "flag:bool? = none\ry = if(flag != none) flag else false"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas("y", false);
    }
}
