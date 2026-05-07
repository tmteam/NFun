using NFun.Exceptions;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Structs;

using static StatePrimitive;

/// <summary>
/// North-star tests for #121: TIC must infer the principal type of a recursive
/// function on an opt-broken struct WITHOUT requiring a declared named type.
///
/// The principal type for `recurse(t) = recurse(t?.left)` is
///   μX. opt(struct{left: X})
/// — a contractive iso-recursive type whose every back-edge crosses an
/// Optional constructor. Absent the Push-reform fix, TIC would throw
/// "Recursive type definition" because Push merges the recursive field as a
/// struct→struct self-loop without restoring the Optional break.
///
/// These tests do NOT use NamedTypeRegistry — they prove the algebra is
/// self-sufficient. Row-polymorphic regression sentinels live alongside.
/// </summary>
public class RecursiveFunctionPrincipalTypeTest {

    [SetUp] public void Initialize() => TraceLog.IsEnabled = true;
    [TearDown] public void Deinitialize() => TraceLog.IsEnabled = false;

    [Test(Description = "recurse(t) = recurse(t?.left) — should infer μX. opt(struct{left:X}). After M1.3 lift the inner element is CS{StructBound=Sμ} not StateStruct.")]
    public void RecursiveSafeAccess_SingleField_ProducesMuType() {
        // node ids: 0 = t, 1 = t?.left, 2 = recurse(t?.left)
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("recurse", returnId: 2, returnType: null, "t");
        graph.SetVar("t", 0);
        graph.SetSafeFieldAccess(0, 1, "left");
        graph.SetCall(fun, 1, 2);

        var result = graph.Solve();

        // Push reform M1.3 lifted form (PushReform.md §Concrete TIC-graph):
        // param `t` = StateOptional(elem)
        //   where elem.State = ConstraintsState{ StructBound = Sμ }
        //   where Sμ.fields["left"] is a node whose state carries the back-edge
        //   to elem (closing the μ-cycle through Optional).
        var paramNode = fun.ArgNodes[0].GetNonReference();
        Assert.IsInstanceOf<StateOptional>(paramNode.State,
            "Param 't' principal type must be opt(...). Actual: " + paramNode.State);

        var inner = ((StateOptional)paramNode.State).ElementNode.GetNonReference();
        Assert.IsInstanceOf<ConstraintsState>(inner.State,
            "After M1.3 lift: Optional's element is CS{StructBound}. Actual: " + inner.State);

        var cs = (ConstraintsState)inner.State;
        Assert.IsNotNull(cs.StructBound,
            "Lifted CS must carry StructBound (the μ-shape). Actual: " + cs);

        var innerStruct = cs.StructBound;
        var leftField = innerStruct.GetFieldOrNull("left");
        Assert.IsNotNull(leftField, "Sμ must have field 'left'");

        // The crucial assertion: 'left' field must be Optional-wrapped — that's
        // what makes the recursive type contractive (C1 invariant).
        Assert.IsInstanceOf<StateOptional>(leftField.GetNonReference().State,
            "Field 'left' must be opt(...) so the cycle is contractive. Actual: "
            + leftField.GetNonReference().State);
    }

    /// <summary>
    /// Regression sentinel for TryRepairOptSourcedCycle's "wrap EVERY closing
    /// edge" rule. The old implementation wrapped only the FIRST closing field
    /// (e.g. `left`) and left `right` as a struct→struct cycle, tripping the
    /// "Recursive type definition" check on the next pass. This test asserts
    /// BOTH fields are Optional-wrapped — only true when the repair iterates
    /// over all closing edges.
    /// </summary>
    [Test(Description = "Two-field recurse(t) = recurse(t?.left); recurse(t?.right) — μX. opt(struct{left:X, right:X}). Both closing edges must be wrapped — see TryRepairOptSourcedCycle.")]
    public void RecursiveSafeAccess_TwoFields_ProducesMuType() {
        // node ids: 0 = t (for ?.left), 1 = t?.left, 2 = recurse(t?.left)
        //           3 = t (for ?.right), 4 = t?.right, 5 = recurse(t?.right)
        //           6 = body (= 5, last expression)
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("recurse", returnId: 5, returnType: null, "t");
        graph.SetVar("t", 0);
        graph.SetSafeFieldAccess(0, 1, "left");
        graph.SetCall(fun, 1, 2);
        graph.SetVar("t", 3);
        graph.SetSafeFieldAccess(3, 4, "right");
        graph.SetCall(fun, 4, 5);

        var result = graph.Solve();

        var paramNode = fun.ArgNodes[0].GetNonReference();
        Assert.IsInstanceOf<StateOptional>(paramNode.State);
        var inner = ((StateOptional)paramNode.State).ElementNode.GetNonReference();
        // Push reform M1.3 lifted form: inner is CS{StructBound=Sμ}.
        Assert.IsInstanceOf<ConstraintsState>(inner.State,
            "After M1.3 lift: Optional's element is CS{StructBound}. Actual: " + inner.State);
        var cs = (ConstraintsState)inner.State;
        Assert.IsNotNull(cs.StructBound, "Lifted CS must carry StructBound");
        var str = cs.StructBound;

        var leftField = str.GetFieldOrNull("left");
        var rightField = str.GetFieldOrNull("right");
        Assert.IsNotNull(leftField, "Sμ must have 'left'");
        Assert.IsNotNull(rightField, "Sμ must have 'right'");
        Assert.IsInstanceOf<StateOptional>(leftField.GetNonReference().State,
            "'left' must be opt(...) for contractive μ");
        Assert.IsInstanceOf<StateOptional>(rightField.GetNonReference().State,
            "'right' must be opt(...) for contractive μ");
    }

    [Test(Description = "REGRESSION SENTINEL: row-polymorphic length(p) = p.x*p.x + p.y*p.y stays open struct, NOT μ")]
    public void RowPolymorphic_NotRecursive_StaysOpenStruct() {
        // length(p) = p.x * p.x + p.y * p.y
        // node ids: 0=p, 1=p.x, 2=p, 3=p.x, 4=p.x*p.x
        //           5=p, 6=p.y, 7=p, 8=p.y, 9=p.y*p.y
        //           10=sum
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("length", returnId: 10, returnType: null, "p");
        graph.SetVar("p", 0);
        graph.SetFieldAccess(0, 1, "x");
        graph.SetVar("p", 2);
        graph.SetFieldAccess(2, 3, "x");
        graph.SetArith(1, 3, 4);
        graph.SetVar("p", 5);
        graph.SetFieldAccess(5, 6, "y");
        graph.SetVar("p", 7);
        graph.SetFieldAccess(7, 8, "y");
        graph.SetArith(6, 8, 9);
        graph.SetArith(4, 9, 10);

        var result = graph.Solve();

        // Param 'p' MUST stay an open struct (no μ, no Optional wrapping).
        var paramNode = fun.ArgNodes[0].GetNonReference();
        Assert.IsInstanceOf<StateStruct>(paramNode.State,
            "Row-poly param must stay struct (no μ-recursive wrap). Actual: " + paramNode.State);
        Assert.IsNotInstanceOf<StateOptional>(paramNode.State);

        var str = (StateStruct)paramNode.State;
        Assert.IsTrue(str.IsOpen, "Row-poly param must be open struct (subtypable)");
        Assert.IsNotNull(str.GetFieldOrNull("x"));
        Assert.IsNotNull(str.GetFieldOrNull("y"));
    }

    [Test(Description = "REGRESSION SENTINEL: identity-style recurse(x) = recurse(x) stays α → α (no μ, no struct)")]
    public void NonStructRecursion_StaysGeneric() {
        // recurse(x) = recurse(x) — pure α → α, no struct, no opt-source
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("recurse", returnId: 1, returnType: null, "x");
        graph.SetVar("x", 0);
        graph.SetCall(fun, 0, 1);

        var result = graph.Solve();

        // Should be a generic — neither struct nor optional should appear.
        var paramNode = fun.ArgNodes[0].GetNonReference();
        Assert.IsNotInstanceOf<StateStruct>(paramNode.State,
            "Non-struct recursion must not produce struct param");
        Assert.IsNotInstanceOf<StateOptional>(paramNode.State,
            "Non-struct recursion must not produce opt-wrapped param");
    }

    // ════════════════════════════════════════════════════════════════════════
    // GH #126 follow-up — F-bound contractivity through anonymous struct cycle
    // ════════════════════════════════════════════════════════════════════════
    // TIC-level companion to BugHunt300Test.KnownBug_RevHelp_FBoundWidthRejects.
    //
    // The original 5-line script
    //
    //   type n = {v: int = 0, next: n? = none}
    //   loop(x, acc) = if(x==none) acc else loop(x?.next, n{next=acc})
    //   go(x) = loop(x, none)
    //   r = go(n{v=1})
    //   y = r!.v
    //
    // succeeds at TIC level (with named-type registry) and fails at RUNTIME
    // inside `FunnyTypeFitsStructBound`. The TIC-level part is fine here —
    // the F-bound emerges, gets stamped with TypeName="n", and call sites
    // accept it. The runtime bug is in `FunnyTypeFitsStructBound` — it does
    // not fold the candidate through the named-type registry before comparing
    // with the recursive bound. Tracked in tasks #82/#83.
    //
    // But strip the named-type registry away and the same script reveals a
    // **TIC-level** sibling bug: the inferred F-bound's contractivity proof
    // disappears, and the cycle becomes a forbidden struct→struct self-loop.
    // The test below isolates that to the minimal trigger: a 1-arg recursive
    // function whose body builds a struct with itself as a field.

    [Test(Description = @"
        loop(x) = loop({next=x})
        — minimum TIC-level trigger for non-contractive F-bound cycle.
        Anonymous struct constructor `{next=x}` makes the cycle struct→struct
        (no Optional break in between), so TIC rejects it as a recursive type
        definition. With a named-type registry whose entry matches {next: self?},
        the lift would re-introduce the Optional break and the same script
        solves cleanly (CLI: `type n=...; loop(x)=loop(n{next=x})` works).
        Documents the current behavior; flip the assertion to `graph.Solve()`
        when the lift can fire without registry help.")]
    public void GH126_StructCycleWithoutOptional_Throws() {
        // 4 graph ops, mirrors `loop(x) = loop({next=x})`.
        var graph = new GraphBuilder();
        var loopFun = graph.SetFunDef("loop", returnId: 100, returnType: null, "x");

        graph.SetVar("x", 1);                                  // x
        graph.SetStructInit(new[] { "next" }, new[] { 1 }, 2); // {next=x}
        graph.SetCall(loopFun, 2, 100);                        // loop({next=x}) → body

        Assert.Throws<TicRecursiveTypeDefinitionException>(() => graph.Solve(),
            "Without named-type registry, anonymous struct cycle through itself is non-contractive.");
    }

    [Test(Description = @"
        Design diagnostic — not a bug. Faithful 1:1 translation of
            loop(x, acc) = if(x==none) acc else loop(x?.next, {next=acc})
            go(x) = loop(x, none)
        without a named-type registry. Throws TicRecursiveTypeDefinitionException.

        Professor consult verdict (formal type theory):

        The body of `loop` produces only `S ⊇ struct{next: S}` for lacc. Per
        Amadio–Cardelli '93 §3 and Pierce TAPL §20.2 under NFun's equirecursive-
        with-Optional discipline (every back-edge must cross Optional/Array),
        `S = struct{next:S}` and `S = struct{next:opt(S)}` are observably
        DISTINCT μ-fixpoint types, not one. The body alone supplies no
        algebraic constraint that selects between them. Picking either is
        invention, not inference — violates principal-type soundness
        (Damas–Milner: inferred scheme must be most general).

        The two existing rescues are correctly scoped:
          - TryRepairOptSourcedCycle checks the cycle's OWN subgraph. The
            `x?.next` opt-source lives on lx's subgraph and contributes to
            lx — extending its reach to lacc would conflate provenance with
            constraint (textbook false positive).
          - TryPromoteCSDescendantToStructBound requires Optional in the
            closing path; absent here.

        The user MUST supply an algebraic anchor:
          (1) named-type registry declaring `next: n?` (explicit Optional)
              — used by BugHunt300 KnownBug_RevHelp_FBoundWidthRejects
                (now passes via runtime Fit-check fix), OR
          (2) explicit `:t?` annotation on a parameter (forces Optional).

        Without either, this test correctly throws. Stays [Ignore]'d as
        documentation of the algebraic boundary; flip to Assert.Throws if
        we ever want a stable diagnostic test.")]
    [Ignore("Design diagnostic — TIC's rejection here is formally correct per Amadio-Cardelli '93 §3 (contractivity) and Damas-Milner principal-type soundness. See test description for the professor-consult verdict. Not a bug to fix at TIC level; must be unblocked by user via named-type registry or explicit Optional annotation.")]
    public void GH126_FullScriptShape_FBoundThroughWrapper() {
        // loop(x, acc) = if(x==none) acc else loop(x?.next, {next=acc})
        var graph = new GraphBuilder();
        var loopFun = graph.SetFunDef("loop", returnId: 109, returnType: null, "lx", "lacc");

        graph.SetVar("lx", 100);
        graph.SetConst(101, None);
        graph.SetEquality(100, 101, 102);

        graph.SetVar("lacc", 103);

        graph.SetVar("lx", 104);
        graph.SetSafeFieldAccess(104, 105, "next");
        graph.SetVar("lacc", 106);
        graph.SetStructInit(new[] { "next" }, new[] { 106 }, 107);
        graph.SetCall(loopFun, 105, 107, 108);

        graph.SetIfElse(new[] { 102 }, new[] { 103, 108 }, 109);

        // go(x) = loop(x, none)
        var goFun = graph.SetFunDef("go", returnId: 202, returnType: null, "gx");
        graph.SetVar("gx", 200);
        graph.SetConst(201, None);
        graph.SetCall(loopFun, 200, 201, 202);

        graph.Solve();

        var loopArg0 = loopFun.ArgNodes[0].GetNonReference();
        Assert.IsInstanceOf<StateOptional>(loopArg0.State,
            $"loop's first arg must be opt(...) for cycle contractivity. Actual: {loopArg0.State}");
        var goArg0 = goFun.ArgNodes[0].GetNonReference();
        Assert.IsInstanceOf<StateOptional>(goArg0.State,
            $"go's arg must be opt(...). Actual: {goArg0.State}");
    }
}
