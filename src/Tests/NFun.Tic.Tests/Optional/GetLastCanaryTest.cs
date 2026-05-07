using NFun.Tic.SolvingStates;
using NUnit.Framework;

namespace NFun.Tic.Tests.Optional;

/// <summary>
/// TIC-level canary for Push reform M2.A Phase B (ε).
/// Reproduces the SetCoalesce composite-member cycle that makes
/// Roadmap_GetLast_LinkedList_Generic_TwoTypes throw at toposort.
///
/// Function: getLast(n) = getLast(n?.next) ?? n
///   (the if/else around it is irrelevant to the cycle topology;
///    the cycle is entirely within the SetCoalesce subexpression.)
/// </summary>
public class GetLastCanaryTest {

    [Test(Description = "Phase B (ε): getLast body resolves cycle. Inspects signature.")]
    public void GetLast_Cycle_ResolvesAtTicLevel() {
        // Mirror node ids from runtime trace:
        //   12 = VAR n (passed to ?.next)
        //   11 = n?.next  (SafeFieldAccess result)
        //   10 = recursive call result getLast(n?.next)
        //   13 = VAR n  (right operand of ??)
        //    9 = ??-result (Coalesce)
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        graph.SetVar("n", 12);
        graph.SetSafeFieldAccess(12, 11, "next");
        graph.SetCall(fun, 11, 10);                  // getLast(n?.next) -> 10
        graph.SetVar("n", 13);
        graph.SetCoalesce(10, 13, 9);                // 10 ?? 13 -> 9

        // After Phase B (ε): expected to NOT throw at toposort.
        // What happens after toposort (Pull/Push/Destruction/Finalize) is
        // the next diagnosis question.
        graph.Solve();
        // Solve succeeds. Inspect the resulting signature.
        var argNode = fun.ArgNodes[0].GetNonReference();
        TestContext.WriteLine($"argNode.State = {argNode.State}");
        var retNode = fun.RetNode.GetNonReference();
        TestContext.WriteLine($"retNode.State = {retNode.State}");

        // The signature should expose:
        //   arg : opt(struct{next: opt(self_ref_to_arg_elem)})
        //   ret : opt(self_ref_to_arg_elem)
        // Where self_ref is RefTo back to the bound owner.
        Assert.IsInstanceOf<StateOptional>(argNode.State, "arg should be opt-flavored");
        Assert.IsInstanceOf<StateOptional>(retNode.State, "ret should be opt-flavored");
    }

    [Test(Description = "Phase B (ε): TWO-PHASE — solve getLast first, then solve script call (mirrors syntax pipeline)")]
    [Ignore("M2.B Cycle 7: superseded by degenerate-opt-cycle redirect. Roadmap_GetLast_LinkedList_Generic_TwoTypes now passes; this canary verified pre-redirect intermediate state.")]
    public void GetLast_TwoPhase_SyntaxLike() {
        // Phase 1: Build and Solve getLast separately (like RuntimeBuilder does)
        var getLastGraph = new GraphBuilder();
        var fun = getLastGraph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        getLastGraph.SetVar("n", 12);
        getLastGraph.SetSafeFieldAccess(12, 11, "next");
        getLastGraph.SetCall(fun, 11, 10);
        getLastGraph.SetVar("n", 13);
        getLastGraph.SetCoalesce(10, 13, 9);

        getLastGraph.Solve();
        TestContext.WriteLine($"After Phase 1: arg state = {fun.ArgNodes[0].GetNonReference().State}");
        TestContext.WriteLine($"After Phase 1: ret state = {fun.RetNode.GetNonReference().State}");

        // Phase 2: NEW graph for script body, calls fun with concrete arg
        var bodyGraph = new GraphBuilder();
        var fields = new System.Collections.Generic.Dictionary<string, TicNode>();
        fields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        fields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        var concreteStruct = new StateStruct(fields, isFrozen: true);
        bodyGraph.GetOrCreateStructNode(100, concreteStruct);
        bodyGraph.SetCall(fun, 100, 101);  // Use the SOLVED fun

        // Forceunwrap
        var u = bodyGraph.InitializeVarNode();
        bodyGraph.SetCall(
            new ITicNodeState[] { StateOptional.Of(u.Node), u },
            new[] { 101, 102 });

        // .v field access
        bodyGraph.SetFieldAccess(102, 103, "v");

        try {
            bodyGraph.Solve();
            var v = bodyGraph.GetOrCreateNode(103).GetNonReference();
            TestContext.WriteLine($"v access result state = {v.State}");
        } catch (System.Exception ex) {
            TestContext.WriteLine($"Two-phase failure: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    [Test(Description = "Phase B (ε): full pipeline — call + forceUnwrap + .v access")]
    public void GetLast_Call_ForceUnwrap_FieldAccess() {
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        graph.SetVar("n", 12);
        graph.SetSafeFieldAccess(12, 11, "next");
        graph.SetCall(fun, 11, 10);
        graph.SetVar("n", 13);
        graph.SetCoalesce(10, 13, 9);

        // Concrete struct {v: int, next: none}
        var fields = new System.Collections.Generic.Dictionary<string, TicNode>();
        fields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        fields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        var concreteStruct = new StateStruct(fields, isFrozen: true);
        graph.GetOrCreateStructNode(100, concreteStruct);
        graph.SetCall(fun, 100, 101);

        // Node 102 = forceUnwrap(101). forceUnwrap : (T?) -> T.
        // Wire: 101 ≤ opt(U); 102 = U for fresh U.
        var u = graph.InitializeVarNode();
        graph.SetCall(
            new ITicNodeState[] { StateOptional.Of(u.Node), u },
            new[] { 101, 102 });

        // Node 103 = .v field access on 102.
        // SetFieldAccess: 102 must be struct{v: ...}; 103 = field type.
        graph.SetFieldAccess(102, 103, "v");

        try {
            graph.Solve();
            var v = graph.GetOrCreateNode(103).GetNonReference();
            TestContext.WriteLine($"v access result state = {v.State}");
        } catch (System.Exception ex) {
            TestContext.WriteLine($"Failure: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    [Test(Description = "M2.B diagnostic: getLast called with TWO structurally-different structs")]
    [Ignore("M2.B Cycle 7: superseded by degenerate-opt-cycle redirect. Roadmap_GetLast_LinkedList_Generic_TwoTypes now passes; this canary verified pre-redirect intermediate state.")]
    public void GetLast_TwoCallSites_DifferentStructs() {
        // Phase 1: Build and Solve getLast separately
        var getLastGraph = new GraphBuilder();
        var fun = getLastGraph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        getLastGraph.SetVar("n", 12);
        getLastGraph.SetSafeFieldAccess(12, 11, "next");
        getLastGraph.SetCall(fun, 11, 10);
        getLastGraph.SetVar("n", 13);
        getLastGraph.SetCoalesce(10, 13, 9);
        getLastGraph.Solve();

        TestContext.WriteLine($"After body solve: arg state = {fun.ArgNodes[0].GetNonReference().State}");
        TestContext.WriteLine($"After body solve: ret state = {fun.RetNode.GetNonReference().State}");

        // Phase 2: TWO different concrete structs flow into the SAME funState
        var bodyGraph = new GraphBuilder();

        // Struct A: {v: int, next: opt(none)} — like type a
        var fieldsA = new System.Collections.Generic.Dictionary<string, TicNode>();
        fieldsA["v"] = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        fieldsA["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        var structA = new StateStruct(fieldsA, isFrozen: true);
        bodyGraph.GetOrCreateStructNode(100, structA);
        bodyGraph.SetCall(fun, 100, 101);

        // Struct B: {v: real, next: opt(none), label: arr(char)} — like type b
        var fieldsB = new System.Collections.Generic.Dictionary<string, TicNode>();
        fieldsB["v"] = TicNode.CreateInvisibleNode(StatePrimitive.Real);
        fieldsB["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        fieldsB["label"] = TicNode.CreateInvisibleNode(StateArray.Of(StatePrimitive.Char));
        var structB = new StateStruct(fieldsB, isFrozen: true);
        bodyGraph.GetOrCreateStructNode(200, structB);
        bodyGraph.SetCall(fun, 200, 201);

        try {
            bodyGraph.Solve();
            var n101 = bodyGraph.GetOrCreateNode(101).GetNonReference();
            var n201 = bodyGraph.GetOrCreateNode(201).GetNonReference();
            TestContext.WriteLine($"Call 1 result (a) state = {n101.State}");
            TestContext.WriteLine($"Call 2 result (b) state = {n201.State}");

            // Drill deeper: get the inner element state
            if (n101.State is StateOptional opt101) {
                var inner = opt101.ElementNode.GetNonReference();
                TestContext.WriteLine($"  Call 1 element state = {inner.State}");
            }
            if (n201.State is StateOptional opt201) {
                var inner = opt201.ElementNode.GetNonReference();
                TestContext.WriteLine($"  Call 2 element state = {inner.State}");
            }

            // Now test FORCEUNWRAP + .v access on each result (mirrors Syntax test)
            var u101 = bodyGraph.InitializeVarNode();
            bodyGraph.SetCall(
                new ITicNodeState[] { StateOptional.Of(u101.Node), u101 },
                new[] { 101, 102 });
            bodyGraph.SetFieldAccess(102, 103, "v");

            var u201 = bodyGraph.InitializeVarNode();
            bodyGraph.SetCall(
                new ITicNodeState[] { StateOptional.Of(u201.Node), u201 },
                new[] { 201, 202 });
            bodyGraph.SetFieldAccess(202, 203, "v");

            bodyGraph.Solve();
            var v103 = bodyGraph.GetOrCreateNode(103).GetNonReference();
            var v203 = bodyGraph.GetOrCreateNode(203).GetNonReference();
            TestContext.WriteLine($"After !.v: 103 (a-result.v) state = {v103.State}");
            TestContext.WriteLine($"After !.v: 203 (b-result.v) state = {v203.State}");
        } catch (System.Exception ex) {
            TestContext.WriteLine($"Two-call failure: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    [Test(Description = "M2.B: getLast with TWO NamedStructs (mirrors syntax test failure)")]
    public void GetLast_TwoCallSites_NamedStructs() {
        // Phase 1: Build and Solve getLast separately
        var getLastGraph = new GraphBuilder();
        var fun = getLastGraph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        getLastGraph.SetVar("n", 12);
        getLastGraph.SetSafeFieldAccess(12, 11, "next");
        getLastGraph.SetCall(fun, 11, 10);
        getLastGraph.SetVar("n", 13);
        getLastGraph.SetCoalesce(10, 13, 9);
        getLastGraph.Solve();

        // Phase 2: TWO NamedStruct call sites — mimics
        //   ra = getLast(a{...})  where a = {v:int, next:a?}
        //   rb = getLast(b{...})  where b = {v:real, next:b?, label:text}
        var bodyGraph = new GraphBuilder();

        // NamedStruct A: a = {v:int, next:a?}
        var aFields = new System.Collections.Generic.Dictionary<string, TicNode>();
        var aSelfNode = TicNode.CreateInvisibleNode(StatePrimitive.Any); // placeholder
        aFields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        aFields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(aSelfNode));
        var structA = new StateStruct(aFields, isFrozen: true) { TypeName = "a" };
        // Update self-ref to point to the struct itself (post-construction)
        bodyGraph.GetOrCreateStructNode(100, structA);
        bodyGraph.SetCall(fun, 100, 101);

        // NamedStruct B: b = {v:real, next:b?, label:char[]}
        var bFields = new System.Collections.Generic.Dictionary<string, TicNode>();
        var bSelfNode = TicNode.CreateInvisibleNode(StatePrimitive.Any);
        bFields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.Real);
        bFields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(bSelfNode));
        bFields["label"] = TicNode.CreateInvisibleNode(StateArray.Of(StatePrimitive.Char));
        var structB = new StateStruct(bFields, isFrozen: true) { TypeName = "b" };
        bodyGraph.GetOrCreateStructNode(200, structB);
        bodyGraph.SetCall(fun, 200, 201);

        try {
            bodyGraph.Solve();
            var n101 = bodyGraph.GetOrCreateNode(101).GetNonReference();
            var n201 = bodyGraph.GetOrCreateNode(201).GetNonReference();
            TestContext.WriteLine($"PASS: NamedStruct two-call works.");
            TestContext.WriteLine($"  101 state: {n101.State}");
            TestContext.WriteLine($"  201 state: {n201.State}");
        } catch (System.Exception ex) {
            TestContext.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    [Test(Description = "M2.B: full syntax-like setup — TWO NamedStructs + !.v on each")]
    [Ignore("M2.B Cycle 7: superseded by degenerate-opt-cycle redirect. Roadmap_GetLast_LinkedList_Generic_TwoTypes now passes; this canary verified pre-redirect intermediate state.")]
    public void GetLast_TwoCallSites_FullPipeline() {
        // Mirror syntax test:
        //   ra = getLast(a{...})!.v
        //   rb = getLast(b{...})!.v
        var getLastGraph = new GraphBuilder();
        var fun = getLastGraph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        getLastGraph.SetVar("n", 12);
        getLastGraph.SetSafeFieldAccess(12, 11, "next");
        getLastGraph.SetCall(fun, 11, 10);
        getLastGraph.SetVar("n", 13);
        getLastGraph.SetCoalesce(10, 13, 9);
        getLastGraph.Solve();

        var bodyGraph = new GraphBuilder();

        // NamedStruct A
        var aFields = new System.Collections.Generic.Dictionary<string, TicNode>();
        aFields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        aFields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        var structA = new StateStruct(aFields, isFrozen: true) { TypeName = "a" };
        bodyGraph.GetOrCreateStructNode(100, structA);
        bodyGraph.SetCall(fun, 100, 101);
        // ! force unwrap
        var u1 = bodyGraph.InitializeVarNode();
        bodyGraph.SetCall(
            new ITicNodeState[] { StateOptional.Of(u1.Node), u1 },
            new[] { 101, 102 });
        // .v
        bodyGraph.SetFieldAccess(102, 103, "v");

        // NamedStruct B
        var bFields = new System.Collections.Generic.Dictionary<string, TicNode>();
        bFields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.Real);
        bFields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        bFields["label"] = TicNode.CreateInvisibleNode(StateArray.Of(StatePrimitive.Char));
        var structB = new StateStruct(bFields, isFrozen: true) { TypeName = "b" };
        bodyGraph.GetOrCreateStructNode(200, structB);
        bodyGraph.SetCall(fun, 200, 201);
        var u2 = bodyGraph.InitializeVarNode();
        bodyGraph.SetCall(
            new ITicNodeState[] { StateOptional.Of(u2.Node), u2 },
            new[] { 201, 202 });
        bodyGraph.SetFieldAccess(202, 203, "v");

        try {
            bodyGraph.Solve();
            var v103 = bodyGraph.GetOrCreateNode(103).GetNonReference();
            var v203 = bodyGraph.GetOrCreateNode(203).GetNonReference();
            TestContext.WriteLine($"PASS: full pipeline works.");
            TestContext.WriteLine($"  103 (a-result.v): {v103.State}");
            TestContext.WriteLine($"  203 (b-result.v): {v203.State}");
        } catch (System.Exception ex) {
            TestContext.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    [Test(Description = "M2.B: TWO NamedStructs with proper self-recursive next:Self?")]
    [Ignore("M2.B Cycle 7: superseded by degenerate-opt-cycle redirect. Roadmap_GetLast_LinkedList_Generic_TwoTypes now passes; this canary verified pre-redirect intermediate state.")]
    public void GetLast_TwoCallSites_RecursiveStructs() {
        var getLastGraph = new GraphBuilder();
        var fun = getLastGraph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        getLastGraph.SetVar("n", 12);
        getLastGraph.SetSafeFieldAccess(12, 11, "next");
        getLastGraph.SetCall(fun, 11, 10);
        getLastGraph.SetVar("n", 13);
        getLastGraph.SetCoalesce(10, 13, 9);
        getLastGraph.Solve();

        var bodyGraph = new GraphBuilder();

        // NamedStruct A: a = {v:int, next:a?} — TRUE recursion via TypeName
        // Build the recursive structure: next references a's own struct (via RefTo to its node)
        var aStructNode = bodyGraph.GetOrCreateNode(100);
        var aFields = new System.Collections.Generic.Dictionary<string, TicNode>();
        aFields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        aFields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(aStructNode));
        var structA = new StateStruct(aFields, isFrozen: true) { TypeName = "a" };
        aStructNode.State = structA;
        bodyGraph.SetCall(fun, 100, 101);
        var u1 = bodyGraph.InitializeVarNode();
        bodyGraph.SetCall(
            new ITicNodeState[] { StateOptional.Of(u1.Node), u1 },
            new[] { 101, 102 });
        bodyGraph.SetFieldAccess(102, 103, "v");

        // NamedStruct B: b = {v:real, next:b?, label:char[]}
        var bStructNode = bodyGraph.GetOrCreateNode(200);
        var bFields = new System.Collections.Generic.Dictionary<string, TicNode>();
        bFields["v"] = TicNode.CreateInvisibleNode(StatePrimitive.Real);
        bFields["next"] = TicNode.CreateInvisibleNode(StateOptional.Of(bStructNode));
        bFields["label"] = TicNode.CreateInvisibleNode(StateArray.Of(StatePrimitive.Char));
        var structB = new StateStruct(bFields, isFrozen: true) { TypeName = "b" };
        bStructNode.State = structB;
        bodyGraph.SetCall(fun, 200, 201);
        var u2 = bodyGraph.InitializeVarNode();
        bodyGraph.SetCall(
            new ITicNodeState[] { StateOptional.Of(u2.Node), u2 },
            new[] { 201, 202 });
        bodyGraph.SetFieldAccess(202, 203, "v");

        try {
            bodyGraph.Solve();
            var v103 = bodyGraph.GetOrCreateNode(103).GetNonReference();
            var v203 = bodyGraph.GetOrCreateNode(203).GetNonReference();
            TestContext.WriteLine($"PASS: recursive named two-call");
            TestContext.WriteLine($"  103 = {v103.State}");
            TestContext.WriteLine($"  203 = {v203.State}");
        } catch (System.Exception ex) {
            TestContext.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    [Test(Description = "Phase B (ε): script-body call to getLast with concrete arg")]
    public void GetLast_Call_WithConcreteArg() {
        // Step 1: Build getLast (same as canary above)
        var graph = new GraphBuilder();
        var fun = graph.SetFunDef("getLast", returnId: 9, returnType: null, "n");
        graph.SetVar("n", 12);
        graph.SetSafeFieldAccess(12, 11, "next");
        graph.SetCall(fun, 11, 10);
        graph.SetVar("n", 13);
        graph.SetCoalesce(10, 13, 9);

        // Step 2: Build a "script body" that creates a concrete struct
        // and calls getLast on it. Mirrors the syntax test's
        //   ra = getLast(a{v=1, next=a{v=2}})!.v
        // but skips the !.v field access (would test downstream).
        // Concrete struct {v: int, next: opt(self)}.

        // Node 100 = literal struct argument {v: 1, next: none}
        var fields = new System.Collections.Generic.Dictionary<string, TicNode>();
        var vField = TicNode.CreateInvisibleNode(StatePrimitive.I32);
        var nextField = TicNode.CreateInvisibleNode(StateOptional.Of(TicNode.CreateInvisibleNode(StatePrimitive.None)));
        fields["v"] = vField;
        fields["next"] = nextField;
        var concreteStruct = new StateStruct(fields, isFrozen: true);
        // Wrap in node 100
        var node100 = graph.GetOrCreateStructNode(100, concreteStruct);

        // Node 101 = getLast call result
        graph.SetCall(fun, 100, 101);

        // Now solve. If signature substitution works correctly,
        // node 101's type should be opt(concrete struct).
        try {
            graph.Solve();
            var resultNode = graph.GetOrCreateNode(101).GetNonReference();
            TestContext.WriteLine($"call result state = {resultNode.State}");
            // Should be opt-flavored
            Assert.IsInstanceOf<StateOptional>(resultNode.State,
                "Call result must be opt-flavored (T?)");
        } catch (System.Exception ex) {
            TestContext.WriteLine($"Failure: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
