using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 9,
/// post Round 1-8 fixes). 3 agents × 50 iterations (151 total). 2 confirmed
/// minor UX bugs from Agent 3 (Edge & Creative).
/// </summary>
public class BugHuntMasterRound9 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR9Bug1 — `default` without contextual type resolves to a literal
    //   `System.Object` CLR instance exposed to the user:
    //
    //     y = default          # y:Any = System.Object   ← BUG
    //     y:any = default      # y:Any = System.Object   ← BUG (same)
    //     y:any? = default     # y:Any? = none           ← correct fallback
    //
    //   The unconstrained-Any path materializes a fresh sentinel object
    //   that isn't useful (can't compare, has no `default` semantics,
    //   printing shows the type name as the value). Context propagation
    //   works for other cases:
    //
    //     y = default + 1      # y:Int32 = 0    ← correct
    //     y = default ?? 5     # y:Int32 = 5    ← correct
    //
    //   Right fix: either reject `default` without a usable context with
    //   a clear diagnostic, or default to `none` / `null` (so the value
    //   round-trips meaningfully). Severity: minor.
    // ───────────────────────────────────────────────────────────────
    // After fix: `default` without context returns `none` (FunnyNone.Instance),
    // not a raw `new System.Object()`. Since `any ≡ any?` semantically in NFun,
    // the default of `any` is the same as the default of any Optional type.
    [Test]
    public void MR9Bug1_DefaultWithoutContext_ReturnsNone() {
        Assert.IsNull("y = default".Calc().Get("y"));
    }

    // 1b. Control: explicit Optional context gives sensible `none`.
    [Test]
    public void MR9Bug1_Control_DefaultWithOptContext_ReturnsNone() {
        var rt = "y:any? = default"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(rt.Get("y"));
    }

    // 1c. Control: numeric context propagates correctly.
    [Test]
    public void MR9Bug1_Control_DefaultWithNumericContext_ReturnsZero() {
        "y = default + 1".AssertResultHas("y", 1);
    }

    // ───────────────────────────────────────────────────────────────
    // MR9Bug2 — Struct literal with duplicate field name produces a
    //   cryptic TIC-internal diagnostic instead of a clear validation:
    //
    //     y = {a=1, a=2}
    //     # FU798: Types cannot be solved: Node 2:[..] cannot has state {a:..}
    //
    //   Same for `;` separator. User has no way to tell that "duplicate
    //   field 'a'" is the actual issue. The parser/semantic layer should
    //   catch duplicate field names BEFORE TIC and emit a clean error.
    //   Severity: minor (UX, not correctness).
    // ───────────────────────────────────────────────────────────────
    // After fix: parser detects duplicate field names BEFORE TIC and emits a
    // clear FU502 "Duplicate field '{name}' in struct literal" instead of the
    // cryptic FU798 internal-node-state message. Case-insensitive — matches
    // struct field lookup elsewhere.
    [Test]
    public void MR9Bug2_DuplicateFieldInStructLiteral_ClearError() {
        var ex = Assert.Throws<FunnyParseException>(() => "y = {a=1, a=2}".Calc());
        StringAssert.Contains("duplicate", ex.Message.ToLowerInvariant());
        StringAssert.Contains("'a'", ex.Message);
    }

    [Test]
    public void MR9Bug2_DuplicateFieldCaseInsensitive_ClearError() {
        var ex = Assert.Throws<FunnyParseException>(() => "y = {a=1, A=2}".Calc());
        StringAssert.Contains("duplicate", ex.Message.ToLowerInvariant());
    }

    // 2b. Control: distinct field names work as expected.
    [Test]
    public void MR9Bug2_Control_DistinctFields_Work() {
        "y = {a=1, b=2}".Calc(); // Just verify it parses without error.
    }
}
