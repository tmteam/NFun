# Worklist Pull — Execution Plan

> Parent: [`WorklistPull.md`](WorklistPull.md) — design spec. This doc is code-level.
> Branch: `worklist-pull`.

## Baseline (recorded 2026-06-26 on `worklist-pull` HEAD)

QuickBench V1 Precise on MacBook M4 (clean build), one commit above `bf5626f8` (bench `set`→`setAt` rename to bypass `set`-keyword conflict introduced by Mutable collections).

```
V1 Calibration:  92.8μs  (corridor 92.3 ± 0.5μs — slightly hot CPU)
Simple   Parse=3.0μs Build=11.8μs  TIC+Asm=8.8μs   Alloc=10.2kb
Medium   Parse=9.4μs Build=52.7μs  TIC+Asm=43.3μs  Alloc=35.6kb
Complex  Parse=26.5μs Build=171.1μs TIC+Asm=144.6μs Alloc=114.5kb

V2 Calibration:  92.7μs
Simple   Parse=3.6μs Build=15.2μs  TIC+Asm=11.5μs  Alloc=12.1kb
Medium   Parse=9.9μs Build=59.3μs  TIC+Asm=49.4μs  Alloc=37.5kb
Complex  Parse=26.8μs Build=192.3μs TIC+Asm=165.6μs Alloc=117.1kb
```

Snapshots: `/tmp/baseline_worklist_pull_v1.json`, `/tmp/baseline_worklist_pull_v2.json`. Compare after each Phase.

Acceptance corridor:
- **Phase 1** (worklist behind feature flag, OFF by default): ≤2% regression on Build vs baseline.
- **Phase 2** (workarounds removed): no perf budget — measure and decide.
- **Phase 3** (worklist ON by default + un-ignore tests): ≤10% on Build (spec §3.1 predicts ~5%).

### Final perf (2026-06-27 Path B + Phase 2 cleanup + default ON)

```
V1 Calibration: 91.9μs (Δ=-1.0% vs baseline)
Simple   Build=11.8μs  Δ=-0.15%   Alloc Δ=+2.21%
Medium   Build=52.8μs  Δ=+0.25%   Alloc Δ=+2.37%
Complex  Build=172.9μs Δ=+1.06%   Alloc Δ=+2.09%

V2 Calibration: 92.0μs (Δ=-0.8% vs baseline)
Simple   Build=15.0μs  Δ=-1.48%   Alloc Δ=+2.64%
Medium   Build=58.4μs  Δ=-1.54%   Alloc Δ=+2.73%
Complex  Build=189.0μs Δ=-1.73%   Alloc Δ=+2.42%
```

Build performance within noise corridor across both V1 and V2; some categories
even slightly faster (likely cache effects from restructured hot path).
Allocation overhead 2-3% — driver Queue + per-node memo fields (WrapOptionalInner,
TransformElementInner). Far below acceptance corridor (≤10%).

For comparison — failed Path A attempt (Phase 2-3 default ON pre-revert) was
Simple +3.06% / Medium +4.81% / Complex +6.13%. The Path B gated approach
(every Apply-cell mutation gated on `IsActive`) is ~4-6× lower overhead.

## Phase 1 — Infrastructure (no behavioural change at default)

### 1.1. Files to touch (minimum)

| File | Change |
|---|---|
| `src/NFun/Tic/SolvingFunctions.cs` | Add `WorklistPullDriver` nested class with `[ThreadStatic]` worklist + `PullConstraintsWorklist(nodes)` entry point. |
| `src/NFun/Tic/TicNode.cs` | Hook `AddAncestor` / `RemoveAncestor` to enqueue source if `WorklistPullDriver.IsActive`. |
| `src/NFun/Tic/SolvingFunctions.cs` `MergeInplace` | Same hook: enqueue both sides + their descendants on merge. |
| `src/NFun/Tic/GraphBuilder.cs` | Add `UseWorklistPull` flag (default false). When set, route the Pull phase through `PullConstraintsWorklist` instead of streaming. |
| `src/Tests/NFun.Tic.Tests/UnitTests/WorklistPullSmokeTests.cs` (new) | Smoke tests: build a known-failing case (Bug47, Bug55 3D); assert worklist driver runs to fixed point and produces the same/better result. |

### 1.2. WorklistPullDriver design

```csharp
internal static class WorklistPullDriver {
    [ThreadStatic] private static Queue<TicNode> _worklist;
    [ThreadStatic] private static bool _isActive;
    [ThreadStatic] private static int _drainCount; // for diagnostics + termination guard

    public static bool IsActive => _isActive;

    public static void Enqueue(TicNode node) {
        if (!_isActive || node == null) return;
        // Dedup-on-enqueue via VisitMark (single int field, no HashSet alloc)
        if (node.VisitMark == _enqueueMark) return;
        node.VisitMark = _enqueueMark;
        _worklist.Enqueue(node);
    }

    public static void RunPull(TicNode[] toposortedNodes) {
        _isActive = true;
        _worklist = new Queue<TicNode>(toposortedNodes.Length * 2);
        _drainCount = 0;
        try {
            // Seed in toposort order
            foreach (var n in toposortedNodes)
                if (!n.IsMemberOfAnything)
                    Enqueue(n);

            while (_worklist.Count > 0) {
                if (++_drainCount > MaxDrains)
                    throw new InvalidOperationException("Worklist Pull failed to converge");
                var node = _worklist.Dequeue();
                // Mark as 'in-process' so re-enqueue from inside PullRec is allowed
                node.VisitMark = _processedMark; // see fixed-point check below
                PullConstraintsForNode(node);
            }
        } finally {
            _isActive = false;
            _worklist = null;
        }
    }
}
```

Open question: **fixed-point detection**. Three options:
- (A) Re-enqueue only on edge addition (current spec §1.2/1.3). Simple but may over-iterate.
- (B) Snapshot state hash before `PullConstraintsForNode`, compare after. Re-enqueue only if changed. Avoids redundant re-Pulls but adds per-node overhead.
- (C) Per-dimension dirty bits (spec §3.1 mitigation). Lowest re-Pull rate but biggest refactor.

**Phase 1 pick**: (A) for simplicity. Measure perf. If >5% regression on Build with flag ON, switch to (B).

### 1.3. Hook strategy

`TicNode.AddAncestor`:
```csharp
public void AddAncestor(TicNode node) {
    if (node == this) AssertChecks.Panic("Circular ancestor 0");
    for (int i = 0; i < _ancestors.Count; i++)
        if (_ancestors[i] == node) return;
    _ancestors.Add(node);
    WorklistPullDriver.Enqueue(this); // ← new
}
```

`SolvingFunctions.MergeInplace`:
- Already a centralized merge. Add `WorklistPullDriver.Enqueue(primary)` and `Enqueue(secondary)` + their descendants after the merge body.

**Risk**: edge additions during graph BUILD (before solve starts) would still enqueue if `_isActive=true`. Mitigation: `_isActive` flips on only inside `RunPull`. Build-time `AddAncestor` calls are no-ops in `Enqueue`.

### 1.4. GraphBuilder integration

```csharp
public bool UseWorklistPull { get; set; } = false; // Phase 1: opt-in

// In Solve(), replace:
SolvingFunctions.PullConstraintsTwoPhase(sorted);
// with:
if (UseWorklistPull)
    SolvingFunctions.WorklistPullDriver.RunPull(sorted);
else
    SolvingFunctions.PullConstraintsTwoPhase(sorted);
```

### 1.5. Phase 1 exit criteria

- Build succeeds, all 14k tests pass with `UseWorklistPull=false` (no behavioural change). **HARD requirement.**
- New `WorklistPullSmokeTests` (set `UseWorklistPull=true`):
  - One trivial test (no cycles, primitive arithmetic) passes — worklist converges in 1 drain, same result as streaming.
  - One pinned debt-#10 test (Bug47_MapItFirstOnNestedList_WidensToAny) passes — was Ignore, now un-ignored under the flag.
- QuickBench V1 Precise with flag OFF: within ±2% of baseline.

## Phase 2 — Remove workarounds (with flag ON)

**Status (2026-06-26): BLOCKED.** Flipping `UseWorklistPull` default to `true` produces 5
TIC-test convergence failures on cyclic Optional + recursive-struct graphs:

- `GetLast_Call_WithConcreteArg`, `GetLast_Cycle_ResolvesAtTicLevel`,
  `GetLast_TwoCallSites_NamedStructs` — getLast (recursive function with `T? → Any?`).
- `IfElseNone_AsStructField_ChainedSafeAccess`, `…_Coalesce` — chained `?.` through
  Optional struct field with None branch.

Mechanism: streaming Pull walks each node once per top-level call (`PullConstraintsForNode`),
so new edges added INSIDE Apply cells aren't re-traversed. Worklist Pull's
`AddAncestor` hook re-fires Pull on the edge source, which on a cyclic graph triggers a
cascade of re-fires that never reaches a fixed point — `WorklistPullDriver failed to
converge after 1025 drains`. Monotonicity (state can only narrow) doesn't save us
because `WrapAncestorInOptional` and the cross-Apply cells perform shape-mutating
operations (T → Opt(T), AddAncestor between not-yet-stable composite elements) that
unfold differently under re-entry.

Detailed professor analysis: [`WorklistPull_ConvergenceAnalysis.md`](WorklistPull_ConvergenceAnalysis.md) — top-3 culprit Apply cells + 3 refactor principles + 5-step migration sequence.

Two paths forward (pick before Phase 2 work resumes):

- **Selective worklist** — narrow the hook from `AddAncestor` (every edge) to only
  `MergeInplace` and explicit enqueues at the CompCs cross-Apply fallback. This is
  effectively the §4 alternative from `WorklistPull.md`: a targeted P3b fix without
  the cyclic-graph convergence risk. Smaller blast radius; closes Bug47 / Bug55 3D
  and the four ee-mode closures; leaves debt #10 partially open.
- **Convergence-proof refactor** — execute the 5-step migration from
  `WorklistPull_ConvergenceAnalysis.md`: idempotency of shape mutation (innerNode
  caching), branch determinism in CompCs cross-Apply, driver-level state-hash skip,
  then long-term Apply-cell decision/edit split. Multi-week.

Workarounds slated for removal — DO NOT TOUCH until convergence is resolved:

1. **Eager `PullConstraintsForNode`** calls in `CompCsApply.ForwardPullCompCsSc`, `ForwardCompCsStateArray`, `ReverseCompCsStateArray` — worklist handles re-Pull automatically.
2. **`PropagatePreferredAcrossFallback`** in `CompCsApply` — Apply(CS, CS) on the now-enqueued source will copy Preferred via standard path.
3. **`DescendantHasOptionalLift` / `IsOptionalLiftBetween` / `HasAnyOptionalLiftedField`** in `DestructionFunctions` (debt #5 stale-snapshot Optional lift). Re-firing Pull post-Phase-2-Optional-wrapping eliminates the stale-snapshot window.
4. **Identity guards in `Transform*OrNull`** (debt #15) — worklist re-firing on the alias-shared element node converges to the same fixed point without the guard.

Each step:
1. Remove the workaround.
2. Run TIC + Unit + API (~10s).
3. Run Syntax (15-18 min) in background.
4. Commit only if green; otherwise diagnose which test broke, decide if it's a worklist-Pull blind spot or a regression bug.

## Phase 3 — Turn ON by default, un-ignore tests

1. Flip `UseWorklistPull` default to `true`.
2. Un-ignore the 5 debt-#10 tests + 4 ee-mode P3b tests + 1 lang-mode (`LangMirror_NestedByteUpcastMap_RealResult`) + 5 Stage1 tests (Bug55 3D family).
3. Full suite + QuickBench V1.
4. If perf within corridor (~10%), commit. If not, switch to fixed-point detection option (B) and re-measure.

## Phase 4 — Update specs + close debts

- `specs_tic/Algorithm.md` §Pull: replace streaming description with worklist semantics; reference `WorklistPull.md`.
- `specs_tic/Proofs.md` §3: merge P3a/P3b/P3c → unified P3.
- `specs_tic/TechnicalDebt.md`: mark #10, #15, #16's Descendant axis, #17 3D residual as CLOSED.
- `specs_tic/ApplyCells.md`: remove "Eager re-Pull?" column.
- `CLAUDE.md`: update debt list in Known Technical Debt.

## Risk register

| Risk | Mitigation |
|---|---|
| Perf regression >10% on Build | Switch fixed-point detection from option A to option B (state hash). If still bad, evaluate option C (dirty bits) — bigger refactor. |
| Infinite loop from oscillating MergeInplace | `MaxDrains` cap (e.g. 10× node count); throw to catch in CI. Spec §1.4 termination argument relies on monotonicity. |
| ScCClosurePass duplicates work | Worklist Pull runs BEFORE Push, ScCClosurePass runs AFTER Push. They operate on different phases — keep both for now. Re-evaluate in Phase 4 whether ScCClosurePass can be subsumed. |
| Hook in `MergeInplace` mis-fires during graph BUILD | `_isActive` flag scoped to `RunPull` only. Build-time merges are no-ops in `Enqueue`. |
| Stale ancestor edges from RefTo dereference | `NodeToposort.OptimizeTopology` already dereferences post-Pull. Worklist runs on toposorted nodes (already deref'd). |

## Out of scope for this branch

- Removing two-phase Pull (None Phase 1 + non-None Phase 2). Worklist subsumes the temporal ordering, but the None-first ordering is currently load-bearing for `IsOptional` flag setup. Keep two-phase entry; worklist runs within Phase 2.
- Replacing `ScCClosurePass`. Separate effort.
- `SimplePrimitiveSolver` path — not affected (primitives only).
