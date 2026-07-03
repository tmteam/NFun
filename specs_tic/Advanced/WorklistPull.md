# Worklist Pull — Architecture Spec (closes debt #10 + P3b)

> **Status**: v1 — design spec for worklist Pull refactor.
> Parent: [`../Algorithm.md`](../Algorithm.md) §Pull, [`../Proofs.md`](../Proofs.md) §3.6 (P3b).
> Closes: debt #10 (edge-rewire compensation), debt #16 P3b (Descendant axis), and any future "stale snapshot" issues caused by streaming toposort.

## 0. Problem statement

Current TIC Pull uses **streaming toposort**: nodes are processed in topological order, each visited exactly once. When an Apply cell adds a new edge (via `AddAncestor`) after the source node has already been processed, the new edge is **not retroactively propagated**.

Workarounds in current code:
- **Eager re-Pull** (`PullConstraintsForNode`) — fires after specific cross-Apply cells.
- **Identity guards** in `Transform*OrNull` (debt #15) — prevent self-edge panic.
- **`PropagatePreferredAcrossFallback`** (debt #16 Phase 5) — closes P3a.

These workarounds are localized and incomplete. The principled fix is a **worklist-based Pull**: every edge addition triggers re-processing of affected nodes until a fixed point is reached.

## 1. Algorithmic design

### 1.1. Data structures

```
type Worklist = Queue<TicNode>
type EdgeAddedEvent = (source: TicNode, target: TicNode)

global state:
    worklist: Worklist           // nodes pending re-processing
    visited:  HashSet<TicNode>   // nodes already processed at least once in current iteration
    iteration_mark: int           // incremented per worklist drain to support multi-pass
```

### 1.2. Main loop

```python
def WorklistPull(graph):
    worklist = Queue()
    
    # Seed: enqueue all nodes in toposort order.
    for node in toposort(graph):
        worklist.enqueue(node)
    
    # Drain the worklist; re-enqueue on edge additions.
    while worklist not empty:
        node = worklist.dequeue()
        if already_at_fixpoint(node): continue
        
        previous_state = snapshot(node)
        PullOnce(node)                     # walk ancestors, fire Apply cells
        
        if node.state changed from previous_state:
            # Re-enqueue all descendants that depend on this node's state
            for desc in node.descendants:
                worklist.enqueue(desc)
        
        # Edge-addition hook: any AddAncestor / MergeInplace during PullOnce
        # adds the affected nodes to the worklist (see §1.3).

def PullOnce(node):
    for ancestor in node.ancestors:
        PullConstrains(node, ancestor)      # may call Apply cells
    # No recursion into composite members here; the worklist handles that.
```

### 1.3. Edge addition hook

Apply cells currently call `AddAncestor(source, target)` directly. Under worklist Pull, this call also enqueues `source` (so its new ancestor edge gets re-processed):

```python
def AddAncestor_hooked(source, target):
    source.ancestors.add(target)
    worklist.enqueue(source)                # ← the key change
```

Similarly for `MergeInplace(a, b)` — both `a` and `b` (and their descendants) are enqueued.

### 1.4. Termination

**Termination invariant**: each `(node, edge)` pair is processed at most a bounded number of times.

**Bound argument**:
- State changes during Pull are **monotone** (per P3): each Pull step narrows the constraint interval or fills in null Preferred/flags. Strict monotonicity on a finite domain (bounded by the input expression's type structure).
- Edge additions during Pull are also bounded: each composite cell can add at most O(arity) edges per visit; total edges added bounded by O(|V| × arity_max) which is O(|E|) plus a constant per cycle re-entry.
- A node is re-enqueued only when its state changes OR it gains a new edge. Both events are bounded.
- Total worklist drains: O((|V| + |E|) × dimensions-changing-events) = O(|V|² × 6) in the worst case (each of 6 dimensions can change at most once per node before it's stable). In practice O(|V| × D(expr)) — similar to current streaming Pull.

### 1.5. Equivalence to current Pull

**Theorem (Worklist-vs-Streaming equivalence)**: for any input graph G where current streaming Pull terminates without violating P3 (i.e., no AddAncestor fallback fires), worklist Pull produces the same result.

**Proof sketch**:
- In G without AddAncestor fallback, all edges exist at the start of Pull and no new edges are added.
- Streaming Pull visits each node once in toposort order.
- Worklist Pull starts with all nodes enqueued in toposort order. The first drain visits each node once. No new edges added → no re-enqueueing.
- Both produce the same node states. ∎

**Theorem (P3b closure)**: worklist Pull restores P3 (full Monotonicity) on the Descendant axis.

**Proof**:
- The P3b violation in `ForwardPullCompCsSc` Path 2 (AddAncestor fallback) is: `source.D ≠ null` exists, but `target.D` (the ancestor element) doesn't receive it because eager re-Pull on `target` walks `target.ancestors`, not the newly-added `source` edge.
- Under worklist Pull: `AddAncestor(source, target)` enqueues `source`. When `source` is dequeued, `PullOnce(source)` walks `source.ancestors` (including `target`), firing `Apply(target, source)`.
- `Apply(target's-state, source's-state)` (e.g., `Apply(CS, CS)` if both are CS) propagates Descendant via the standard CS×CS rule (Lemma 3.1 dimension D).
- Therefore `target.D ⊒ source.D` after the worklist iteration. ✓ P3b holds. ∎

## 2. Migration plan

### 2.1. Phase 1 — Add worklist infrastructure

Implementation tasks:
1. Add `Queue<TicNode>` to `GraphBuilder` or `SolvingFunctions`.
2. Wrap `AddAncestor` / `MergeInplace` with edge-addition hooks that enqueue affected nodes.
3. Modify `PullConstraintsRecursive` to drain the worklist instead of (or in addition to) the streaming toposort.
4. Add `previous_state_snapshot` checks (or hashes) to detect fixed-point.

### 2.2. Phase 2 — Remove workarounds

Once worklist Pull is verified:
- Remove `PropagatePreferredAcrossFallback` calls in the CompCs cross-Apply cells (no longer needed).
- Remove eager `PullConstraintsForNode` calls in cross-Apply helpers (worklist handles propagation automatically).
- Remove identity guards in `TransformTo*OrNull` (debt #15) — worklist tolerates new edges naturally.

### 2.3. Phase 3 — Un-ignore tests

- `LangMirror_NestedByteUpcastMap_RealResult`
- Closure_ArrayOfClosures_IndependentCells (ee-mode)
- MR4Bug2_CorrectArityCallOn1ArgLambda_TypedAsElementReturnType (ee-mode)
- MR4Bug2_ZeroArgCallOn1ArgLambda_InMapRule_SilentlyAccepted (ee-mode)
- TwinArrayWithUpcast_lambdaConstCalculate (ee-mode)

Verify all pass under worklist Pull.

### 2.4. Phase 4 — Update specs

- TicAlgorithm.md §Pull: replace streaming-toposort description with worklist semantics.
- TicProofs.md §3: collapse P3a/P3b/P3c back into unified P3 (all axes proven).
- TicTechnicalDebt.md: mark debt #10 and debt #16 as RESOLVED. Mark debt #15 as RESOLVED if identity guards are removed.
- ApplyCells.md: remove "Eager re-Pull?" column (no longer needed).

## 3. Risk assessment

### 3.1. Performance impact

Streaming Pull is O(|V| + |E|) per pass — one visit per node.
Worklist Pull is O(|V|² × dimensions) worst-case but O(|V| × D(expr)) in practice.

For typical expressions (D ≈ 5, |V| ≈ 100): worklist visits each node ~5 times vs ~1 for streaming. **~5× slowdown** on Pull phase.

Mitigation:
- Fixed-point detection via state hash: nodes that haven't changed don't trigger Apply re-firing.
- Per-dimension "dirty bits" instead of full re-Pull.
- For primitive-only expressions, `SimplePrimitiveSolver` (Advanced/SimplePath.md) short-circuits the full TIC entirely — no impact.

Expected production impact: <5% on Build phase for typical expressions; negligible on Run phase.

### 3.2. Correctness risks

- **Race conditions** with worklist drain: none — TIC is single-threaded.
- **Infinite loops** from cycle re-entry: prevented by monotonicity (state can only narrow); when no node changes state on a worklist drain, fixed point reached.
- **Order-dependent results**: worklist Pull is deterministic given a fixed queue ordering (FIFO + stable enqueue on edge addition). P4 Determinism preserved.

### 3.3. Backwards compatibility

The 4 `[Ignore]`'d ee-mode tests + the 1 lang-mode test currently fail because P3b is violated. Under worklist Pull they would PASS. No regression in currently-passing tests expected (P3a results are a subset of P3 full).

## 4. Alternative — incremental fix vs full refactor

If worklist Pull's performance impact is unacceptable, a smaller fix exists:

**Extended `PropagatePreferredAcrossFallback`**: at the AddAncestor fallback site, propagate Descendant as well (with safety checks):

```csharp
private static void PropagateConstraintsAcrossFallback(TicNode source, TicNode target) {
    if (source.GetNonReference().State is ConstraintsState sourceCs
        && target.GetNonReference().State is ConstraintsState targetCs)
    {
        // Preferred (P3a)
        if (targetCs.Preferred == null && sourceCs.Preferred != null)
            targetCs.Preferred = sourceCs.Preferred;
        
        // Descendant (P3b candidate)
        if (sourceCs.HasDescendant) {
            // Safe path: use AddDescendant API (does LCA with safety simplification)
            // Risk: may over-narrow if target has other implicit constraints.
            // Only apply when target has no concurrent edges with conflicting constraints.
            if (!HasConflictingPendingEdges(target))
                targetCs.AddDescendant(sourceCs.Descendant);
        }
    }
}
```

This is **localized but heuristic**: `HasConflictingPendingEdges` is a guess; we may miss cases or wrongly propagate. Worklist Pull is the principled answer.

Recommendation: use the heuristic as a **temporary debt mitigation** if performance of worklist Pull is unacceptable. Otherwise implement worklist Pull directly.

## 5. Post-closure hardening (2026-07-03, commit `79781371`)

The 2026-06-27 closure was incomplete: any **collection-recursive named type**
(`type t = {v:int, kids:t[]}`) sent the solver into a StackOverflow, which crashed
the test host at Syntax test ~768/10846 and masked itself as a green run. Two
independent termination defects, both fixed:

1. **Mark clash.** `Enqueue` dedup'd via `TicNode.VisitMark` — the same field
   `PullRec` uses as its traversal visited-set. An Enqueue fired from inside an
   Apply cell erased "visited" on μ-cycle members, so the traversal re-entered the
   knot forever. Rule: *a traversal's visited-set must be isolated from any other
   subsystem's node marking.* Fix: dedicated `TicNode.EnqueueMark` field.

2. **Edge-set oscillation.** Composite×composite Pull cells consume their edge
   (`RemoveAncestor`) and emit member edges; on a μ-knot each member's
   decomposition re-emits the other member's already-consumed edge, so the edge
   set never reaches a fixpoint and the worklist never drains (`maxDrains`
   "Monotonicity violation suspected"). Rule (Amadio-Cardelli): *the coinductive
   assumption set of a μ-subtyping derivation must live as long as the derivation
   — under the worklist, that is the whole run.* Fix: run-scoped discharged-pair
   memo (`WorklistPullDriver.MarkDischarged` / `IsDischarged`, registered in
   `PullConstrains` when the edge was consumed, checked in `TicNode.AddAncestor`).
   Streaming Pull is unaffected — a single pass never re-emits a consumed pair.

Pinned by `RecursiveStructTest.CollectionRecursiveStruct_*_PullConverges` (TIC level,
builds the 2-node μ-knot directly) and `BugHuntStatementsResults_Round4.StmtBug49`.

## 6. Related specs

- [`../Algorithm.md`](../Algorithm.md) §Pull — current streaming Pull design
- [`../Proofs.md`](../Proofs.md) §3.6 — P3b violation that this closes
- [`../TechnicalDebt.md`](../TechnicalDebt.md) — #10/#15/#16 history (closed entries live in its "Closed" section)
- [`../ApplyCells.md`](../ApplyCells.md) — cells affected by the refactor
