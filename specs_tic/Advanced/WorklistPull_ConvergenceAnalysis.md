# Worklist Pull — Convergence Analysis (2026-06-26)

> Parent: [`WorklistPull.md`](WorklistPull.md) design + [`WorklistPull_ExecPlan.md`](WorklistPull_ExecPlan.md) Phase 2 BLOCKED entry.
> Source: professor analysis after flipping `UseWorklistPull` default to true and observing 5 TIC-test convergence failures.

## Numeric oracle

On the GetLast graph (18 nodes built):

| Pull strategy | Nodes allocated during Solve | Outcome |
|---|---|---|
| Streaming | 10 | converges |
| Worklist | 1016 | diverges at 1025 drains |

~100x allocation ratio. Apply cells emit fresh innerNodes per drain when state
propagates through the recursive μX. struct{v, next:Opt(X)} cycle. The numeric
gap is the signal: each refactor step should drop the worklist count toward
streaming's baseline. Oracle test:
`src/Tests/NFun.Tic.Tests/UnitTests/DiagAllocProbeTest.cs`.

## Failure surface

Five tests diverge at `WorklistPullDriver failed to converge after 1025 drains (seed=16 nodes)`:

1. `GetLastCanaryTest.GetLast_Call_WithConcreteArg`
2. `GetLastCanaryTest.GetLast_Cycle_ResolvesAtTicLevel`
3. `GetLastCanaryTest.GetLast_TwoCallSites_NamedStructs`
4. `IfElseNoneSyntaxTest.IfElseNone_AsStructField_ChainedSafeAccess`
5. `IfElseNoneSyntaxTest.IfElseNone_AsStructField_ChainedSafeAccess_Coalesce`

Common shape: cyclic graph involving recursive struct (μX. struct{…, next: Opt(X)}) + `SetCoalesce` / `SetSafeFieldAccess` planting CS-state nodes at the cycle boundary.

## Root cause

Streaming Pull walks each node ONCE in toposort order. Many Apply cells perform operations that are not strictly bounded-narrowing — they tolerate this because the same `(ancestor, descendant)` pair is never re-considered. Worklist Pull breaks the single-visit assumption: any `AddAncestor` re-enqueues the source. Cells that mutate node shape (T → Opt(T), CS → Opt(innerCS)) or add new edges between not-yet-stable composite elements get re-invoked on the new shape and do work again — producing new edges, new innerNodes, new enqueues. No fixed point.

The driver's monotonicity contract (`WorklistPull.md` §1.4 termination) treats `Info(c)` as a 6-dimension lattice element that can only narrow. Where cells violate this is where they **mutate `State` shape kind** (composite-kind transition) and/or **emit fresh allocations per fire** (innerNode constructors).

## Top 3 culprits

### 1. `Apply(CS, ICompositeState{StateOptional})` ancestor-wrap branch — `PullConstraintsFunctions.cs:60-90`

Allocates a fresh `innerNode = TicNode.CreateTypeVariableNode("e" + ancestorNode.Name + "'", innerCs)` on every fire. The gate at line 64 (`ancestorNode.IsOptionalElement`) handles one re-entry shape but not all: when a μ-recursive cycle returns to the inner CS through a different outer path, the cell fires again with a different `ancestorNode` and allocates yet another inner. Cycle pump.

Effect: each loop closure of the μ-struct cycle inflates the graph by O(depth) nodes; the worklist keeps re-enqueueing them all.

### 2. `CompCsApply.ForwardPullCompCsSc` MergeInplace-vs-AddAncestor branch flip — `CompCsApply.cs:13-43, 152-160, 171-179`

The cell first tries `MergeInplace(ancestor.ElementNode, sc.ElementNode)`; on failure, `AddAncestor` and eager `PullConstraintsForNode`. The branch decision depends on `CanMergeStates`, which depends on element state. Under worklist, element state narrows iteratively, so the branch SWITCHES between re-entries:

- First call: `CanMergeStates = false` → AddAncestor path, eager re-Pull.
- Second call (after eager re-Pull narrowed it): `CanMergeStates = true` → MergeInplace path, which itself enqueues both endpoints, which kicks ancestor migrations, which re-fires the cell, …

Combined: MergeInplace ↔ AddAncestor flip-flopping along the cycle adds an unbounded number of distinct edges. Driver invariant ("fresh edges bounded by O(|V|·arity)") broken.

### 3. `LiftDescendantToOptionalElement` + multiple `innerNode` allocations — `PullConstraintsFunctions.cs:124-132, 236-291, 297-317`

Each fire creates fresh nodes, re-adds ancestor edges, sets `IsOptionalElement`. Not hash-consed: two firings on the same `(nodeA, nodeB)` produce two different innerNodes, each grafted into the graph with its own ancestor list. Cycle hits this on every loop closure.

The `WORKAROUND` comment at line 126-127 acknowledges the design: "rewire-after-toposort breaks single-pass invariant; proper fix is worklist Pull." But the proper fix as currently scaffolded LOSES the single-pass invariant *without* compensating for the fresh-allocation semantics that the workaround assumed.

## Honorable mentions

- **Struct width propagation** in `Apply(StateStruct, StateStruct)` re-fires `descField.AddAncestor(ancField)` even on duplicate edges. `AddAncestor` dedups the list but still enqueues. For `μX. {v, next: Opt(X)}` graphs, a single outer-level edge produces N field-level edges per worklist tick.
- **`MergeInplace` ancestor migration** (`SolvingFunctions.cs:428-429`) emits one `Enqueue(main)` per migrated edge — `O(|secondary.Ancestors|)` enqueues. Bounded but multiplicative.
- **`Invoke` visited-pair guard** (`StagesExtension.cs:19-46`) is scoped per Invoke entry (`pairs.Remove(pair)` in `finally`). Does not persist across worklist drains — coinductive cycle protection lost between top-level dequeues.

## Refactor principles (in priority order)

### Principle 1 — Idempotency of shape mutation

A cell that turns `S` into `Opt(S')` must be trigger-once-per-node. Cache `innerNode` on the wrapped node (e.g. `nodeA.OptionalWrapperInner`), so the second invocation returns the same node and skips re-emitting ancestor edges that already exist. Fixes culprits #1, #3 and the analogous `Apply(StateArray, StateArray)` element-wrap branch (`PullConstraintsFunctions.cs:297-325`).

### Principle 2 — Branch determinism in cross-Apply

`CompCsApply.Forward*` must not switch between `MergeInplace` and `AddAncestor` across re-entries on the same pair. Pick one deterministically (likely AddAncestor + deferred MergeInplace at Destruction) and commit; remove the eager `PullConstraintsForNode` since the worklist drains that anyway. Fixes culprit #2.

### Principle 3 — State-hash fixed-point detection (driver-level)

Even with idempotent shape and deterministic branching, struct width propagation and ancestor migration can re-enqueue nodes whose lattice dimensions haven't changed. Snapshot `Info(c)` before `PullConstraintsForNode`; skip the visit if the snapshot hasn't changed since the last fire on that node. Directly enforces the P3 narrowing contract at the driver level instead of trusting every cell. Spec ExecPlan §1.2 option B.

### Deeper architectural answer

Split each Apply cell into:
- **decision function**: pure, idempotent, monotone — answers "is the constraint satisfied, and what would narrow if applied?"
- **graph-edit function**: non-idempotent — fires only when the decision strictly narrows the lattice.

Driver calls decision first; only on strict-narrowing fires the edit. This is the Pottier-Rémy '05 §3 bounded-narrowing form. Multi-week refactor touching ~22 Apply cells across Pull/Push/Destruction.

## Migration sequence (proposed)

| Step | Scope | Pinned test signal |
|---|---|---|
| 1 | Principle 3 driver-level — snapshot+skip | Likely reduces drain count but does NOT close the 5 failures (allocations still happen) |
| 2 | Principle 1 on culprit #1 — innerNode caching at line 80 | 2-3 of the 5 GetLast tests may converge |
| 3 | Principle 1 on culprit #3 — Lift family + StateArray element-wrap | IfElseNone tests should converge |
| 4 | Principle 2 on culprit #2 — CompCs cross-Apply | Phase 2 workarounds become removable |
| 5 | Apply-cell split (decision vs edit) — long term | Driver simplification, formal monotonicity proof |

Each step needs its own commit + full test pass (TIC + Unit + API + Syntax) before moving on. Performance check after every step that touches a hot Apply cell (overhead corridor 92.3 ± 0.5μs V1 Calibration).

## Out of scope here

- ScCClosurePass interaction — unchanged for now; worklist runs in Pull only.
- `SimplePrimitiveSolver` path — primitive-only, no composite shape mutation, unaffected.
- Removing two-phase Pull (None-first / non-None-second) — already discussed in ExecPlan; convergence work doesn't depend on it.
