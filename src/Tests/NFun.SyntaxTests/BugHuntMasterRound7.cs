using NFun;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 7,
/// post Round 1-6 fixes + convert-redesign + MR-refactor).
/// 3 agents × ~100 iterations. 4 confirmed bug families.
/// </summary>
public class BugHuntMasterRound7 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

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

    // ───────────────────────────────────────────────────────────────
    // MR7Bug3 — `arr?.method()[idx]` compiles but crashes at runtime when
    //   source is none. The post-`?.` chained `[idx]` doesn't propagate
    //   Optional through to the subsequent operation:
    //
    //     arr:int[]? = none
    //     y = arr?.sort()[0]
    //     # Runtime: "Unable to cast FunnyNone to IFunnyArray"
    //
    //   Spec (Optionals.md L196): "After ?., none propagates through the
    //   entire chain — both field accesses and method calls". Indexing
    //   `[0]` is treated as a regular operator, not a chain step, so the
    //   propagation is lost.
    //
    //   Direct equivalent IS rejected at compile time:
    //     z:int[]? = none; y = z[0]      # FU780
    //
    //   Reproduces with reverse(), filter(), sort() — any method-call
    //   chain after `?.` followed by `[]` indexing.
    //
    //   Family-related to MR6Bug2 (?[ loses Optional through composite
    //   element). Different symptom: here Optional is lost through
    //   `?.method() → method-result` step before the `[]` op.
    // ───────────────────────────────────────────────────────────────
    // Post-rebase onto nfun-lang-v4: lang-mode's Optional propagation
    // infrastructure (SetSafeMethodCall/SetSafeArrayAccess) propagates
    // Optional through `?.method()[idx]` per Optionals.md "After ?., none
    // propagates through entire chain". Earlier master-only fix rejected
    // this at compile (FU780) as a conservative compromise; nfun-lang-v4
    // does the spec-correct thing — result is `T?`, none when source is none.
    [Test]
    public void MR7Bug3_SafeMethodChainThenIndex_PropagatesOptional() {
        var rt = "arr:int[]? = none\ry = arr?.sort()[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
    }

    [Test]
    public void MR7Bug3_SafeMethodChainReverse_PropagatesOptional() {
        var rt = "arr:int[]? = none\ry = arr?.reverse()[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
    }

    // 3c. Control: direct opt-array indexing IS rejected at compile.
    [Test]
    public void MR7Bug3_Control_DirectOptArrayIndexRejected() {
        Assert.Throws<FunnyParseException>(() =>
            "z:int[]? = none\ry = z[0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // 3d. Workaround: use `?[` instead of `[` (per MR6Bug2 fix).
    [Test]
    public void MR7Bug3_Workaround_UseSafeIndex() {
        var rt = "arr:int[]? = none\ry = arr?.sort()?[0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
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
}
