using NFun;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Bugs found by automated expression-mode hunting on master (round 5,
/// post Round 1-4 fixes). 3 agents × ~100 iterations. 7 confirmed bugs.
/// </summary>
public class BugHuntMasterRound5 {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ───────────────────────────────────────────────────────────────
    // MR5Bug1 — Range expression to MaxValue crashes at runtime with
    //   "Index was outside the bounds of the array". Reproducible for
    //   every integer type at its max value, and the symmetric descending
    //   case to MinValue. Range generator likely off-by-one with overflow.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug1_RangeToMaxValue_Int32() {
        "out = [2147483646..2147483647]".Calc()
            .AssertResultHas("out", new[] { 2147483646, 2147483647 });
    }

    [Test]
    public void MR5Bug1_RangeDescToMinValue_Int32() {
        "out = [-2147483647..-2147483648]".Calc()
            .AssertResultHas("out", new[] { -2147483647, -2147483648 });
    }

    [Test]
    public void MR5Bug1_RangeToMaxValue_Int64() {
        "out = [9223372036854775806..9223372036854775807]".Calc()
            .AssertResultHas("out", new[] { 9223372036854775806L, 9223372036854775807L });
    }

    [Test]
    public void MR5Bug1_RangeToMaxValue_Byte() {
        "out:byte[] = [254..255]".Calc()
            .AssertResultHas("out", new byte[] { 254, 255 });
    }

    [Test]
    public void MR5Bug1_RangeToMaxValue_Int16() {
        "out:int16[] = [32766..32767]".Calc()
            .AssertResultHas("out", new short[] { 32766, 32767 });
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug2 — `convert()` to an unsupported destination type throws
    //   raw `InvalidOperationException` ("Function convert cannot be
    //   generated for types [...]") instead of clean FU-coded parse
    //   error. Other generic functions correctly produce FU783.
    // ───────────────────────────────────────────────────────────────
    [Test]
    [Ignore("MR5Bug2: convert(bool) to int throws raw InvalidOperationException")]
    public void MR5Bug2_ConvertBoolToInt_RawInvalidOperationException() {
        Assert.Throws<FunnyParseException>(() => "out:int = convert(true)".Calc());
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug3 — TIC constraint-propagation through Array→Struct→Rule
    //   fails for 2+ anonymous structs with function-valued fields using
    //   single-arg shorthand. Specifically:
    //     type s = {f:rule(int)->int}
    //     arr:s[] = [{f=rule it*2}, {f=rule it*3}]   # FU719
    //   Workarounds: single-element array; explicit `s{...}` ctor;
    //   `rule(it:int)=...` annotation; zero-arg rule signature.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug3_ArrayOfAnonStructWithFnField_FU719() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type s = {f:rule(int)->int}\rarr:s[] = [{f=rule it*2}, {f=rule it*3}]"));
    }

    [Test]
    public void MR5Bug3_ArrayOfAnonStructWithFnField_ThreeElements() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(namedTypesSupport: NamedTypesSupport.Enabled)
                .Build("type s = {f:rule(int)->int}\rarr:s[] = [{f=rule it*2}, {f=rule it*3}, {f=rule it*4}]"));
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug4 (CRITICAL) — Stack overflow in TIC Destruction when
    //   `?.` is used on an anonymous-typed optional struct that has a
    //   function-valued field:
    //     a:{f:rule(int)->int}? = {f = rule it+1}
    //     y = a?.f(5)   # CRASH — process exit code 134
    //
    //   Trace: alternating Destruction(StateStruct,StateStruct) →
    //   Destruction(StateFun,StateFun) → repeat — cyclic constraint.
    //   Works with NAMED type (`type p={f:rule(int)->int}; a:p? = ...`).
    //   Works with `!.` instead of `?.`. Specific to ?. + anon struct
    //   type + function field combination.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug4_SafeAccessAnonOptStructFnField_StackOverflow() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("a:{f:rule(int)->int}? = {f = rule it+1}\ry = a?.f(5)"));
    }

    [Test]
    public void MR5Bug4_SafeAccessAnonNonOptStructFnField_StackOverflow() {
        // The bug also reproduced WITHOUT Optional — `?.` on a non-opt anon struct with fn field.
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("a:{f:rule(int)->int} = {f = rule it+1}\ry = a?.f(5)"));
    }

    // ───────────────────────────────────────────────────────────────
    // MR5Bug5 — `a = {b=1}; y = a?.b` widens `a`'s inferred type to
    //   `{b:Int32}?` (Optional) instead of keeping the concrete
    //   non-optional shape. Spec allows `?.` on non-optional receivers
    //   as no-op; the receiver type should stay non-optional.
    //
    //   Asymmetric with `?[`: `a = [1,2,3]; y = a?[0]` keeps `a:Int32[]`
    //   (non-optional). Practical impact: confusing typeof/printing.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug5_SafeAccessOnNonOptInferred_ReceiverStaysNonOpt() {
        // Bug: `a = {b=1}; y = a?.b` widened `a`'s inferred type to {b:int}? (Optional),
        // silently infecting subsequent regular `.field` access on `a`. The cascade
        // assertion is the strongest signal: if `a` is widened to opt(struct), regular
        // `.c` is rejected with FU755. The fix makes `a` stay {b:int,c:int} so all three
        // succeed end-to-end.
        "a = {b=1, c=2}\ry = a?.b\rz = a.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas(("y", (object)1), ("z", (object)2));
    }


    // ───────────────────────────────────────────────────────────────
    // MR5Bug6 — Struct annotation with FEWER fields than source is
    //   silently ignored:
    //     a = {x=1, y=2, z=3}
    //     b:{x:int, y:int} = a    # annotation says 2 fields
    //     out = b.z               # but z is still accessible! returns 3
    //
    //   Per spec annotation should be the static type. Either reject
    //   assignment with extra fields, or drop them at the boundary.
    //   Equality also compares all 3 fields against the annotation's 2.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void MR5Bug6_NarrowStructAnnotation_RejectsExtraFieldAccess() {
        // Width subtyping per Specs/Types.md L90-91: `{x,y,z}` IS convertible to `{x,y}`.
        // The assignment must succeed, but `b`'s static type is the annotation `{x,y}` —
        // accessing the extra `z` through `b` must be rejected at compile time. (MR5Bug6.)
        Assert.Throws<FunnyParseException>(() =>
            "a = {x=1, y=2, z=3}\rb:{x:int, y:int} = a\rout = b.z".Calc());
    }

    [Test]
    public void MR5Bug6_NarrowStructAnnotation_LegitNarrowingWorks() {
        // The same assignment should accept (width subtyping); accessing fields that
        // ARE in the declared annotation must continue to work post-fix.
        "a = {x=1, y=2, z=3}\rb:{x:int, y:int} = a\rout = b.x"
            .Calc().AssertResultHas("out", 1);
    }

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
    // MR5Bug5 BOUNDARY PROBES — different contexts of `?.`. Hypothesis
    // (per professor) is that PushConstraintsFunctions.cs:224-240 uses
    // `!IsSolved` to discriminate "inferred abstract struct" vs
    // "concrete literal struct" — but should use `IsOpen` instead.
    //
    //   Literal `{b=1}` → closed struct (IsOpen=false), already shape-rigid.
    //   `?.field`-introduced shape → open struct (IsOpen=true).
    //
    // These probes lock down behavior across contexts so any future fix
    // doesn't regress. All marked [Ignore] until fix lands.
    // ───────────────────────────────────────────────────────────────

    // Probe 1a: cascade ?. then regular . over 3 fields.
    //   Pre-fix: FU755 at `z = a.c` — `a` widened to Opt, regular `.c` rejected.
    //   Post-fix: a stays {b,c,d}; y:int?=1, z:int=2, w:int=3.
    [Test]
    public void MR5Bug5b_CascadeSafeThenRegular_ThreeFields() {
        var rt = "a = {b=1, c=2, d=3}\ry = a?.b\rz = a.c\rw = a.d"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas(("y", 1), ("z", 2), ("w", 3));
    }

    // Probe 2: chained `?.` after another `?.` on inferred nested struct.
    //   Pre-fix: outermost `a` widened to {b:{c:int}}? — regular `.b` fails FU755.
    //   Post-fix: a stays {b:{c:int}} non-opt; both ?.-chain and regular `.b` work.
    [Test]
    public void MR5Bug5b_ChainedSafeAccess_OuterStaysNonOpt() {
        "a = {b={c=1}}\ry = a?.b?.c\rz = a.b.c"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
            .AssertResultHas(("y", 1), ("z", 1));
    }

    // Probe 3: ?. on annotated optional receiver. This is the canonical
    // ?. usage and MUST KEEP WORKING after the fix.
    [Test]
    public void MR5Bug5b_SafeAccessOnAnnotatedOpt_StillWorks() {
        var rt = "a:{b:int}? = {b=1}\ry = a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", 1);
    }

    // Probe 4: ?. on if-else-inferred opt receiver. This is the natural
    // way to acquire an optional value without annotation; MUST KEEP WORKING.
    [Test]
    public void MR5Bug5b_SafeAccessOnIfElseInferredOpt_StillWorks() {
        var rt = "a = if(true) {b=1} else none\ry = a?.b"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", 1);
    }

    // Probe 5: `map(rule it?.b)` over array of inferred non-opt struct.
    // Current: arr stays {b:int}[], y becomes int?[] (lambda param `it`
    // is not widened to opt; ?. always wraps result in opt). This is the
    // scenario PushConstraintsFunctions.cs:224 was originally written for.
    // The fix must NOT regress this.
    [Test]
    public void MR5Bug5b_MapWithSafeAccess_LambdaParamNotWidened() {
        var rt = "arr = [{b=1}, {b=2}]\ry = arr.map(rule it?.b)"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", new int?[] { 1, 2 });
    }

    // Probe 5b: map with truly opt elements (annotated array). MUST KEEP WORKING.
    // Asserting only via DoesNotThrow + type — AssertResultHas does not handle
    // null-bearing arrays (test infra NRE in ToStringSmart).
    [Test]
    public void MR5Bug5b_MapWithSafeAccess_OptElements() {
        Assert.DoesNotThrow(() =>
            Funny.Hardcore
                .WithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled)
                .Build("arr:{b:int}?[] = [{b=1}, {b=2}, none]\ry = arr.map(rule it?.b)")
                .Run());
    }

    // Probe 6: F-bounded recursive named type (GH126-style). Currently
    // works on master. The fix MUST NOT regress this.
    [Test]
    public void MR5Bug5b_FBoundedRecursiveNamedType_StillWorks() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build(
                "type n = {v:int = 0, next:n? = none}\r" +
                "loop(x, acc) = if(x==none) acc else loop(x?.next, n{next=acc})\r" +
                "out = loop(n{}, n{})");
        Assert.DoesNotThrow(() => rt.Run());
    }

    // Probe 7: user-defined function with `?.` on parameter. The parameter
    // type and return type should be principled regardless of fix.
    // Current: works — y:int?=42 with input of multi-field literal.
    // Post-fix: must continue to work.
    [Test]
    public void MR5Bug5b_UserFnWithSafeAccess_StillWorks() {
        var rt = "f(x) = x?.value\ry = f({value=42, other=1})"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        rt.AssertResultHas("y", 42);
    }

    // Probe 8 (control): NAMED-type cascade — already works because named
    // type pins the receiver shape. The bug is specific to anon inferred
    // structs. Provides "good half" of the discriminator for the fix.
    [Test]
    public void MR5Bug5b_CascadeOnNamedType_AlreadyWorks() {
        var rt = Funny.Hardcore
            .WithDialect(
                optionalTypesSupport: OptionalTypesSupport.Enabled,
                namedTypesSupport: NamedTypesSupport.Enabled)
            .Build("type p={b:int,c:int}\ra:p = p{b=1,c=2}\ry = a?.b\rz = a.c");
        Assert.DoesNotThrow(() => rt.Run());
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
}
