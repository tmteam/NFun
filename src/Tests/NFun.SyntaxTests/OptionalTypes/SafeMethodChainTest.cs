namespace NFun.SyntaxTests.OptionalTypes;

using Exceptions;
using TestTools;
using NUnit.Framework;

/// <summary>
/// Tests for TypeScript-style safe method chain propagation.
/// Once ?. is used, none propagates through the entire chain of .method() calls.
/// </summary>
[TestFixture]
public class SafeMethodChainTest {

    private static CalculationResult Calc(string expr) =>
        expr.CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);

    // ═══════════════════════════════════════════════════════════
    // Single ?.method() — basic
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void SingleMethod_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.count() ?? 0").AssertResultHas("out", 3);

    [Test]
    public void SingleMethod_None() =>
        Calc("arr:int[]? = none; out = arr?.count() ?? 0").AssertResultHas("out", 0);

    [Test]
    public void SingleMethod_Sort_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort() ?? []").AssertResultHas("out", new[] { 1, 2, 3 });

    [Test]
    public void SingleMethod_Sort_None() =>
        Calc("arr:int[]? = none; out = arr?.sort() ?? []").AssertResultHas("out", new int[0]);

    // ═══════════════════════════════════════════════════════════
    // Chained ?.method().method() — propagation
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void Chain_SortReverse_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort().reverse() ?? []")
            .AssertResultHas("out", new[] { 3, 2, 1 });

    [Test]
    public void Chain_SortReverse_None() =>
        Calc("arr:int[]? = none; out = arr?.sort().reverse() ?? []")
            .AssertResultHas("out", new int[0]);

    [Test]
    public void Chain_SortReverseCount_HasValue() =>
        Calc("arr:int[]? = [3,1,2]; out = arr?.sort().reverse().count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void Chain_SortReverseCount_None() =>
        Calc("arr:int[]? = none; out = arr?.sort().reverse().count() ?? 0")
            .AssertResultHas("out", 0);

    // ═══════════════════════════════════════════════════════════
    // Text chains
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void TextChain_HasValue() =>
        Calc("s:text? = 'hello'; out = s?.reverse().count() ?? 0")
            .AssertResultHas("out", 5);

    [Test]
    public void TextChain_None() =>
        Calc("s:text? = none; out = s?.reverse().count() ?? 0")
            .AssertResultHas("out", 0);

    // ═══════════════════════════════════════════════════════════
    // ?.method() on struct field
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void StructField_SafeMethodChain() =>
        Calc("y = {items = if(true) [3,1,2] else none}; out = y.items?.sort().count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void StructField_SafeMethodChain_None() =>
        Calc("y = {items = if(false) [3,1,2] else none}; out = y.items?.sort().count() ?? 0")
            .AssertResultHas("out", 0);

    // ═══════════════════════════════════════════════════════════
    // Mixed: ?.field then .method()
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void FieldThenMethod_HasValue() =>
        Calc("x = if(true) {items = [1,2,3]} else none; out = x?.items.count() ?? 0")
            .AssertResultHas("out", 3);

    [Test]
    public void FieldThenMethod_None() =>
        Calc("x = if(false) {items = [1,2,3]} else none; out = x?.items.count() ?? 0")
            .AssertResultHas("out", 0);

    // ───────────────────────────────────────────────────────────────
    // MR5Bug7 — Method-call through `?.` on a NAMED-type optional
    //   struct with function field loses precise return type:
    //     type p={f:rule(int)->int}
    //     a:p? = {f = rule it*2}
    //     y = a?.f(7)   # y:Any? — should be Int32?
    //
    //   The fn signature is statically Int32-returning. Wrapping in
    //   Optional via ?. should give Int32?, not Any?. If downstream
    //   constraint pins the type (e.g., `out:int = y!`), it resolves
    //   back to Int32?. Lost during TIC unconstrained resolution.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug7_SafeMethodCallOnOptNamed_PreciseReturnType() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={f:rule(int)->int}\ra:p? = {f = rule it*2}\ry = a?.f(7)");
        // Expect y of declared type Int32?, not Any?.
        var yVar = rt["y"];
        StringAssert.Contains("Int32", yVar.Type.ToString());
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug7 BOUNDARY PROBES — `?.method()` on opt struct with rule
    // field. Bug: 3-step Pull propagation (opt → struct → fun) doesn't
    // complete in topological Pull — by the time edges are added,
    // downstream nodes are finalized, so the rule's Int32 return
    // type is lost and the result widens to Any?.
    //
    // Probes confirm scope: cascade infection, multi-arg signatures,
    // anon vs named receiver, and verify that downstream pinning
    // (annotation, ??, arithmetic) recovers the type as a workaround.
    // ───────────────────────────────────────────────────────────────

    // Probe 1: cascade — does the bug also infect subsequent `?.` method
    // calls on the same receiver? Pre-fix: both y and z are Any?.
    // Post-fix: both must be Int32?.
    [Test]
    public void MR5Bug7b_CascadeSafeMethodCalls_BothPreservePreciseType() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={f:rule(int)->int, g:rule(int)->int}\ra:p? = {f=rule it*2, g=rule it*3}\ry = a?.f(7)\rz = a?.g(8)");
        var yType = rt["y"].Type.ToString();
        var zType = rt["z"].Type.ToString();
        StringAssert.Contains("Int32", yType);
        StringAssert.Contains("Int32", zType);
    }

    // Probe 2 (control workaround): `?? 99` pins the type via the right
    // operand. Currently works on master — y:Int32. Post-fix: must keep
    // working. Not marked [Ignore].
    [Test]
    public void MR5Bug7b_CoalescePinsType_Workaround() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry = a?.f(7) ?? 99"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", 14);
    }

    // Probe 3: multi-arg method through ?. — same Pull-chain bug, more
    // structural depth. Pre-fix: y:Any?=8. Post-fix: y:Int32?=8.
    [Test]
    public void MR5Bug7b_MultiArgSafeMethodCall_PreservesPreciseReturnType() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={f:rule(int,int)->int}\ra:p? = {f=rule it1+it2}\ry = a?.f(3, 5)");
        StringAssert.Contains("Int32", rt["y"].Type.ToString());
    }

    // Probe 4 (control workaround): explicit `y:int? = ...` annotation
    // pins the result type. Currently works — y:Int32?=14. Post-fix:
    // must keep working. Not marked [Ignore].
    [Test]
    public void MR5Bug7b_ExplicitAnnotationPinsType_Workaround() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry:int? = a?.f(7)"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", (int?)14);
    }

    // Probe 5 (control workaround): arithmetic context after ?? pins
    // type. Currently works — y:Int32=15. Post-fix: must keep working.
    // Not marked [Ignore].
    [Test]
    public void MR5Bug7b_ArithmeticContextPinsType_Workaround() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry = (a?.f(7) ?? 0) + 1"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", 15);
    }

    // Probe 6: anon struct receiver (no `type p=...`). Same Pull-chain
    // bug as named-type case — anon receiver does NOT help here.
    // Pre-fix: y:Any?=14. Post-fix: y:Int32?=14. Locks in the parallel
    // anon/named behavior so a future "fix named-type only" patch is
    // caught.
    [Test]
    public void MR5Bug7b_AnonStructReceiver_PreservesPreciseReturnType() {
        var rt = Funny.Hardcore
            .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .Build("a:{f:rule(int)->int}? = {f=rule it*2}\ry = a?.f(7)");
        StringAssert.Contains("Int32", rt["y"].Type.ToString());
    }

    // Probe 7 (value sanity): even though the static type is wrong, the
    // computed value is correct (14). This locks in that the bug is
    // purely about TYPE inference, not evaluation — so a "fix" that
    // breaks the value is immediately caught. Currently works.
    // Not marked [Ignore].
    [Test]
    public void MR5Bug7b_ValueIsCorrect_OnlyTypeIsWrong() {
        "type p={f:rule(int)->int}\ra:p? = {f=rule it*2}\ry = a?.f(7) ?? 0"
            .CalcWithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .AssertResultHas("y", 14);
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
    [Test]
    public void MR7Bug3_SafeMethodChainThenIndex_NoneCrashes() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[]? = none\ry = arr?.sort()[0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    [Test]
    public void MR7Bug3_SafeMethodChainReverse_NoneCrashes() {
        Assert.Throws<FunnyParseException>(() =>
            "arr:int[]? = none\ry = arr?.reverse()[0]"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
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
}
