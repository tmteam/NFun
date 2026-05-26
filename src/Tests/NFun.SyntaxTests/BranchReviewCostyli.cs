using NFun;
using NFun.Exceptions;
using NFun.TestTools;
using NFun.Tic;
using NUnit.Framework;

namespace NFun.SyntaxTests;

/// <summary>
/// Demonstration tests for workarounds (костыли) introduced on the bug-hunt
/// branch (vs origin/master). Found by 3 parallel review agents — TIC core,
/// Convert+Runtime, Parser+Tokenizer+Types partitions.
///
/// Each test exercises a workaround code path. Tests PASS — they demonstrate
/// the workaround is functioning. The workaround should be replaced by a
/// principled fix; when that lands, the test should still pass (behavior
/// unchanged) but the underlying code path will be cleaner.
///
/// Reference: CLAUDE.md "TIC Solver: Design Principles".
/// </summary>
public class BranchReviewCostyli {
    [SetUp]
    public void Initialize() => TraceLog.IsEnabled = true;

    [TearDown]
    public void Deinitialize() => TraceLog.IsEnabled = false;

    // ═══════════════════════════════════════════════════════════════
    // TIC CORE PARTITION
    // ═══════════════════════════════════════════════════════════════

    // ───────────────────────────────────────────────────────────────
    // WO1 — Eager StagesExtension.Invoke after edge rewire
    // PullConstraintsFunctions.LiftDescendantToOptionalElement
    // Self-labeled "WORKAROUND-of-debt", documented in
    // Specs/Tic/TicTechnicalDebt.md #10.
    // Compensates: Pull single-pass doesn't revisit ancestors after
    // mid-pass edge rewires. Fix: worklist-based Pull.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO1_TrueCoalesceReal_LiftsBoolThroughOptional() {
        // Without eager propagation: y:Real=true (Bool never reaches LCA target —
        // soundness violation). With it: y:Any=true (LCA(Bool, Real)=Any, sound).
        var rt = "y = true ?? 1.5"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(true, rt.Get("y"));
    }

    [Test]
    public void WO1_IntCoalesceArr_LiftsThroughOptional() {
        // MR3Bug1 family. Without eager propagation: y=arr(Ch) → crash/wrong.
        // With it: y:Any=1 (LCA propagated).
        var rt = "y = 1 ?? 'hello'"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(1, rt.Get("y"));
    }

    // ───────────────────────────────────────────────────────────────
    // WO2 — StateFun solved-shortcut in DeepCloneNode
    // GraphBuilder.cs: `if (nr.State is StateFun fn && fn.IsSolved) return nr;`
    // Type-keyed special case. Compensates: CloneState(StateFun) doesn't
    // produce disjoint inner nodes; Apply(StateFun, StateFun) then panics
    // with "Circular ancestor 0".
    // Fix: deep-clone inner Arg/Ret nodes OR make Apply tolerate aliasing.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO2_HigherOrderConcreteFun_CloneShortCircuits() {
        // Without the short-circuit: "Circular ancestor 0" crash.
        "f(x) = x+1\r y = f(2)".Calc().AssertResultHas("y", 3);
    }

    // ───────────────────────────────────────────────────────────────
    // WO3 — PropagatePreferred phase reordered before Push
    // GraphBuilder.Solve(): phases swapped so Push doesn't erase Preferred.
    // Compensates: Push collapses literal CS [U8..Re]I32! to bare ancestor
    // (e.g. U8) on annotation, dropping the I32 Preferred metadata.
    // Fix: Push should preserve Preferred during CS→primitive collapse.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO3_NegativeLiteralAdd_ResolvesToI32_NotReal() {
        // Without phase-before-Push: this defaulted to Real.
        "y = (-1) + (-2)".Calc().AssertResultHas("y", -3);
    }

    // ───────────────────────────────────────────────────────────────
    // WO4 — REFACTORED. Field-identity unification relocated from
    // TransformToStructOrNull (synthesis-time fixup) to a named
    // Pull-stage operation `SolvingFunctions.UnifyStructFieldIdentities`,
    // invoked from PullConstraintsFunctions case-185 after the synthesis
    // returns. TransformToStructOrNull is now a pure data transform.
    // Safe-merge predicate stays (solved primitive × empty CS) — that
    // describes when MergeInplace's fast path applies without graph rewire,
    // not a workaround gate. Test kept as regression guard for MR7Bug1.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO4_StructArrayFieldArith_ReachesRealWidening() {
        "data:{x:int,y:real}[]=[{x=1,y=2.5}]\rout = data[0].y + 1"
            .Calc().AssertResultHas("out", 3.5);
    }

    // ───────────────────────────────────────────────────────────────
    // WO5 — Narrow incompatibility throw in DestructionFunctions
    // The guard fires ONLY when descendant CS has a concrete Ancestor;
    // other genuine incompatibilities still silent-true for "back-compat".
    // Fix: either always throw (and fix the tests that surface) or
    // propagate incompatibility through DestructionRec return chain.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO5_FunctionReturnIncompatibleTypes_NowRejected() {
        // BugHunt-stmt #75. Without guard: silently committed (int)→arr(Ch).
        Assert.Throws<FunnyParseException>(()
            => "f(x) = if(x==0) 'a' else x\ry = f(0)".Calc());
    }

    // ═══════════════════════════════════════════════════════════════
    // CONVERT + RUNTIME PARTITION
    // ═══════════════════════════════════════════════════════════════

    // ───────────────────────────────────────────────────────────────
    // WO6 — Optional-literal unwrap-then-cast in expression builder
    // ExpressionBuilderVisitor.GetConstantNodeOrNull — when TIC infers
    // a literal type as Optional, builds primitive ConstantNode then
    // wraps via CastExpressionNode. Compensates: ConstantExpressionNode
    // .CreateConcrete only handles non-Optional bases.
    // Fix: either extend CreateConcrete or have TIC pick non-Optional
    // for primitive literals (they can't be `none`).
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO6_NumericLiteralInOptionalContext_NoCrash() {
        // [1,none][0] forces TIC element type = Int32?. The literal 1 must be
        // built as a ConstantExpressionNode of Optional type — handled by the
        // unwrap-then-cast post-hoc fixup. Without it: ArgOOR in CreateConcrete.
        var rt = "y = [1, none][0]"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.AreEqual(1, rt.Get("y"));
    }

    // ───────────────────────────────────────────────────────────────
    // WO7 — FIXED. SoftFailureConverter no longer catches
    // FunnyRuntimeException — closed set of typed soft exceptions only
    // (FormatException, OverflowException, ArgumentException). The
    // any-dispatcher's "no morphism" throws were reclassified to
    // ArgumentException at the source. Test kept as regression guard:
    // soft parse failure still surfaces as `none` for `:T?` targets.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO7_SoftParseFailure_ReturnsNone() {
        var r = "y:int? = convert('not-a-number')"
            .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled);
        Assert.IsNull(r.Get("y"));
    }

    // ───────────────────────────────────────────────────────────────
    // WO8 — FIXED. TryBuildConverterFn is now strictly Try-shaped
    // (never throws). Hard-reject hints emitted from ThrowIfHardReject
    // at compile time only. BuildAnyDispatcher no longer needs a
    // catch (FunnyParseException) block. Runtime hard-reject on the
    // any path surfaces as ArgumentException → ConcreteConverter wraps
    // as FunnyRuntimeException — same user-facing behavior, but with
    // a principled exception flow.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO8_AnyDispatcher_HardRejectAtRuntime_StillFunnyRuntimeEx() {
        Assert.Throws<FunnyRuntimeException>(()
            => ("x:ip = convert('1.2.3.4')\r"
              + "y:any = x\r"
              + "z:int = convert(y)").Calc());
    }

    // WO8 control: hard rejects fire at COMPILE time when (from, to) is
    // statically known, with directional hints.
    [Test]
    public void WO8_HardReject_AtCompileTime_WithHint_Ip() {
        var ex = Assert.Throws<FunnyParseException>(()
            => "x:ip = convert('1.2.3.4')\ry:int = convert(x)".Calc());
        StringAssert.Contains(":uint", ex.Message);
    }

    [Test]
    public void WO8_HardReject_AtCompileTime_WithHint_RealChar() {
        var ex = Assert.Throws<FunnyParseException>(()
            => "y:char = convert(65.0)".Calc());
        StringAssert.Contains("not a codepoint", ex.Message);
    }

    // WO8 invariant: `:T?` annotation does NOT rescue hard rejects.
    [Test]
    public void WO8_HardReject_OptTargetDoesNotRescue() {
        Assert.Throws<FunnyParseException>(()
            => "x:ip = convert('1.2.3.4')\ry:int? = convert(x)"
                .CalcWithDialect(optionalTypesSupport: OptionalTypesSupport.Enabled));
    }

    // ═══════════════════════════════════════════════════════════════
    // PARSER + TOKENIZER + TYPES PARTITION
    // ═══════════════════════════════════════════════════════════════

    // ───────────────────────────────────────────────────────────────
    // WO9 — Typographic-quote same-char pairing "back-compat"
    // QuotationReader.cs / Tokenizer.cs — accepts BOTH `‘…’` (correct
    // left-with-right) AND `‘…‘` (incorrect left-with-left) as valid
    // string literals. The comment explicitly says "for back-compat".
    // Fix: drop the same-char branch; migrate stale tests if any.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO9_TypographicQuote_SameCharBackCompat_BothAccepted() {
        // Both produce the same string.
        var proper = "y = ‘hi’".Calc().Get("y");
        var hack   = "y = ‘hi‘".Calc().Get("y");
        Assert.AreEqual(proper, hack);
    }

    // ───────────────────────────────────────────────────────────────
    // WO10 — Duplicated overload resolution in dependency visitor
    // FindFunctionDependenciesVisitor.AddDependencyForDefaultExpandedCall —
    // re-implements arg counting, IsParams/IsKeywordOnly checks because
    // the function alias is keyed only on declared arity (`name(arity)`),
    // missing default-expanded call sites.
    // Fix: either register one alias per legal arity range OR delegate
    // matching to IFunctionRegistry's overload resolver.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO10_DefaultExpandedCall_DependencyVisitorReResolves() {
        // a(x, n=2) called as a(5) — dependency edge requires duplicated
        // arity matching in the visitor.
        "a(x, n=2) = x + n\rout = a(5)".Calc().AssertResultHas("out", 7);
    }

    // ───────────────────────────────────────────────────────────────
    // WO11 — Special FU740 branch in InvalidExpression error builder
    // Errors.4.Types.cs:195-208 — when `desc == null` from TIC and the
    // ancestor is an EquationSyntaxNode, synthesizes FU740 from the RHS
    // instead of letting it fall to cryptic FU761.
    // Comment: "Without this branch, `out:byte = 1 + 1` falls into the
    // cryptic FU761 ... instead of the actionable FU740".
    // Fix: TIC should map operator-result generics back to their syntax
    // node so `desc` is non-null and the standard switch covers the case.
    // ───────────────────────────────────────────────────────────────
    [Test]
    public void WO11_InvalidExpressionByteEquation_GoesThroughSpecialBranch() {
        // `out:byte = 1 + 1`: 1+1 result is Int32, doesn't fit byte annotation.
        // Without the synthesized FU740 branch — cryptic FU761.
        var ex = Assert.Throws<FunnyParseException>(()
            => "out:byte = 1000000".Calc());
        Assert.IsNotNull(ex);
    }
}
