# Technical Debt

> **Scope**: known limitations of the current TIC implementation that deviate from the ideal algebra. Each entry: what's wrong, why it exists, what the proper fix is.

## Status legend

- **OPEN**: violation present in production code.
- **PARTIALLY CLOSED**: some dimensions / cases fixed, others remain.
- **ARCHITECTURAL TRADE-OFF / DESIGN TRADE-OFF**: accepted; no further fix planned.

---

## 5. `DescendantHasOptionalLift` workaround — CLOSED (2026-06-27)

**Closed by**: debt #10 worklist Pull. Helpers (`DescendantHasOptionalLift`,
`IsOptionalLiftBetween`, `HasAnyOptionalLiftedField`) and their two caller
blocks in `DestructionFunctions` removed (commit `3518214b`). Worklist re-fires
on edge addition; by Destruction time the snapshot and actual shapes agree.

**Historical context**: Pull Phase-1 snapshots could become stale by Phase 2
(None-absorption order-of-operations: Phase 1 sets `IsOptional` flags, Phase 2
processes non-None constraints). The `DescendantHasOptionalLift` check in
Destruction redirected stale snapshots to the actual (lifted) form via a
`StateRefTo` patch.

---

## 6. `TwoVariableEquality` — unconstrained generics resolve to Any — OPEN

**What's wrong**: `f(x, y) = x == y` infers `x : Any, y : Any` instead of leaving them as unresolved generics. The equality operator has signature `(T, T) → Bool`, which should make `x` and `y` share a generic `T`. Without other constraints, `T` should remain a free type variable.

**Why it exists**: `SolveUselessGenerics` heuristic doesn't recognize this pattern. It resolves the generic to `Any` because no other anchor exists.

**Proper fix**: refine `SolveUselessGenerics` to detect when a generic is constrained only by an equality / comparison, and leave it generic.

---

## 7. PropagatePreferred — single global value — OPEN

**What's wrong**: `PropagatePreferred` collects a single global Preferred from the graph and broadcasts to all compatible CS nodes. In mixed scenarios (hex literal + decimal literal), the order matters: which Preferred is picked depends on toposort.

**Why it exists**: the broadcast pass is simpler than edge-local propagation.

**Proper fix**: edge-local PropagatePreferred — each node receives Preferred only from its directly-connected descendants, not transitively from disconnected sources.

See [`Advanced/Preferred.md`](Advanced/Preferred.md) §6.

---

## 8. TypeName lost at outermost FunnyType level — ARCHITECTURAL TRADE-OFF

**What's wrong**: at the FunnyType level (output of TIC, input to runtime), a struct can carry either `TypeName` OR field types, not both. The tagged union representation doesn't support both at the outermost level.

**Why it exists**: `FunnyType` was designed before named structs; the tagged-union architecture is hard to retrofit.

**Status**: design trade-off accepted. Workarounds in `FunnyConverter` reconstruct TypeName from the named-type registry when needed.

**Path to fix**: redesign `FunnyType` to be a proper struct (not tagged union) with optional TypeName field. Major surgery.

---

## 9. IsMutable decoupling cascade — two narrow workarounds — OPEN

**What's wrong**: changing `StateStruct.IsMutable` semantics from `=> TypeName == null` to `=> !IsSolved` aligned StateStruct with StateArray / Optional / Fun, but left two workarounds in cross-cut handlers.

**9a**: `GetMergedStateOrNull` immutable-shortcut excludes StateStruct from a generic "both solved → equal-merge" path. Reason: StateStruct's mutability semantics differ.

**9b**: `MergeStructs` width-rule has a special case for the same. Reason: field-set width propagation depends on mutability.

**Proper fix**: unify the immutable-merge handling across all composite states.

---

## 10. Pull edge-rewires violate single-pass toposort — CLOSED (2026-06-27)

**Closed by**: worklist Pull implementation. `GraphBuilder.UseWorklistPull = true`
by default since commit `214a5778`. Convergence achieved through Path B (gated
approach — every Apply-cell mutation gated on `WorklistPullDriver.IsActive`,
streaming behavior preserved exactly):

- **Wave-1**: 6 wrap-inner allocation sites memoized via `TicNode.WrapOptionalInner` cache (commits `0c040300`, `942f3463`).
- **Wave-2**: `TransformTo{Optional,Array,Collection}OrNull` accept `TicNode descNode`, memoize via `WrapOptionalInner` / new `TransformElementInner` field (commits `ce9eaf93`, `177f2e3c`).
- **Wave-2.6**: `TicNode.AddAncestor` hook skips Enqueue when `this.IsOptionalElement` — prevents tower-of-wraps cycle through freshly-allocated inners (commit `f58c84f9`).
- **Wave-2.7**: `Apply(F, CS{IsOptional})` morphs ancestor into `Opt(inner)` via `WrapOptionalInner` memo when worklist active (commit `0a7bafe8`).
- **Phase 2 cleanup**: CompCs eager re-Pull + `PropagatePreferredAcrossFallback` gated under `!IsActive` (commit `54c264d5`); `DescendantHasOptionalLift` family removed (commit `3518214b`).

**Closures via debt #10**:
- Debt #5 `DescendantHasOptionalLift`: CLOSED (helpers removed).
- Debt #16 Preferred axis: CLOSED-UNDER-DEFAULT (workaround gated under `!IsActive`).

**Allocation oracle** (`DiagAllocProbeTest`): streaming 10 / worklist 11 on getLast
recursive-struct graph. Near-baseline.

**Pinned tests that DID close** (5/5 originally failing):
- `GetLast_Call_WithConcreteArg`, `GetLast_Cycle_ResolvesAtTicLevel`, `GetLast_TwoCallSites_NamedStructs`
- `IfElseNone_AsStructField_ChainedSafeAccess`, `…_Coalesce`

**Pinned tests that did NOT close** (different surface, kept `[Ignore]`'d):
- `Bug55_Family_3D_Coalesce_WidensToAny` — 3D LCA algebra extension, independent of Pull driver.
- `Lca_CrossKind_NestedComposite_*` — pure-algebra `Lca` composite-element widening.
- `Bug47_MapItFirstOnNestedList_WidensToAny` family — generic T resolution through `.map(rule it.G())`.

**Spec references**:
- [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md) — design.
- [`Advanced/WorklistPull_ExecPlan.md`](Advanced/WorklistPull_ExecPlan.md) — implementation diary (Path A failure + Path B success).
- [`Advanced/WorklistPull_ConvergenceAnalysis.md`](Advanced/WorklistPull_ConvergenceAnalysis.md) — professor diagnosis of tower-of-wraps cycle.

---

## 11. `list ↔ array` runtime cast is bidirectional but TIC subtyping is one-way — DESIGN TRADE-OFF

**What's wrong**: at the lang-mode level, `list<T>` and `array<T>` are runtime-convertible in both directions (a list can be passed where array is expected, and vice versa via materialization). But TIC's `ConstructorLattice` has `List ≤ Array` (one-way subtype), not bidirectional.

**Why it exists**: TIC's uniform-invariance discipline doesn't permit symmetric subtyping without breaking confluence.

**Status**: trade-off accepted. The bidirectional runtime cast handles the practical cases; TIC's one-way subtyping handles type inference.

---

## 12. Composite-type default values asymmetric — OPEN

**What's wrong**: default values for `List<T>`, `Optional<T>`, `Struct{…}`, `Custom` types are computed asymmetrically. Some use a centralized helper; others have ad-hoc construction logic.

**Why it exists**: composite types were added at different stages, each with its own default-value convention.

**Proper fix**: unified default-value protocol. Blocked on planned mutable-struct redesign.

---

## 13. `TestHelper.AreSame` cross-kind permissive — OPEN

**What's wrong**: `TestHelper.AreSame` (test infrastructure) considers a `StateArray(I32)` "the same" as a `StateCollection(Array, I32)`. This permissive comparison masks container-kind regressions: a test would pass even if the wrong concrete container was inferred.

**Why it exists**: legacy from before the StateCollection / StateArray distinction.

**Proper fix**: tighten `AreSame` to require exact state class match.

---

## 15. `Transform*OrNull` element-node reuse + identity guards — OPEN (closes via worklist Pull)

**What's wrong**: `TransformToArrayOrNull`, `TransformToCollectionOrNull`, `TransformToMapOrNull` reuse the descendant collection's `ElementNode` directly (perf optimization). When the descendant's element identity aliases the ancestor's element (chained `[]` over lang collections), `AddAncestor(self)` panics.

**Current mitigation**: identity guards in `Apply(ICompositeState ancestor, CS descendant)` cells.

**Proper fix**: always allocate fresh element nodes in `Transform*`. Trade-off: extra allocations + node registration per Transform call.

**Status (2026-06-27)**: NOT closed by debt #10 worklist Pull (originally predicted to). The identity guards prevent `AddAncestor(self)` panic specifically, not the streaming P3b gap that worklist closed. Wave-2 memoization landed for the Transform helpers' element creation (gated on `WorklistPullDriver.IsActive`) but doesn't change the aliasing semantics. Standalone fix still required.

---

## 16. CompCs cross-Apply Preferred propagation loss — CLOSED-UNDER-DEFAULT (2026-06-27)

**Closed by**: debt #10 worklist Pull. The fallback path's eager
`PullConstraintsForNode` and `PropagatePreferredAcrossFallback` calls are now
gated under `!WorklistPullDriver.IsActive` (commit `54c264d5`) — dead code
under default worklist mode. Worklist re-fires the source through standard
`Apply(CS, CS)` which copies Preferred via the normal Lemma 3.1 path.

**Bug47/49 residuals are NOT this debt** — see TicDebt10_WorklistPullTests.cs.
`data.map(rule it.first())` widens to `Any` due to generic-T resolution in
`.map(rule it.G())` failing, not P3b. Tracked separately; pinned `[Ignore]`'d.

**Historical context**: CompCs cross-Apply cells (`ForwardPullCompCsSc`,
`ForwardCompCsStateArray`, `ReverseCompCsStateArray`) used try-MergeInplace-
fallback-to-AddAncestor on element nodes. The fallback path historically did
not propagate Preferred metadata, violating P3 Monotonicity on the Preferred
axis. The mitigation patched Preferred at the fallback boundary; the Descendant
axis remained open until worklist Pull made the cross-Apply re-firing routine.

**Closed-runtime side-effect** (preserved): `LangMirror_NestedByteUpcastMap_RealResult`
was a P3b counterexample whose TIC behavior matched expectations; the residual
failure was **runtime materialization** of the inner `.map()`'s lambda — TIC
widens inner element types but the concrete collection stores narrower values.
Closed by per-element coercion in `MapFunction.ConcreteMap.Calc` /
`MapEnumerableFunction.ConcreteMap.Calc`, mirroring `SumIter.As<T>`.

---

## 17. `LcaOrShareIdentity` widens to `Any` for cross-kind nested-composite elements — CLOSED (2D depth); 3D+ residual rolled into debt #10

> **CLOSED for the 2D-depth surface family** via path (b) in `LcaOrShareIdentity`. 3D+ residual is a worklist-Pull manifestation — pinned under debt #10.

**What was wrong**: `StateCollection.LcaOrShareIdentity` historically gated its cross-kind identity-share branch on `Element is not ICompositeState && xKindOther.Element is not ICompositeState`. When both elements were themselves composite (e.g. `list<list<I32>>` vs `array<array<I32>>`), the guard fired and the caller widened to `Any` — even though the algebraic answer is well-defined per spec `BaseOperators.md:27` ("climb lattice via ConstructorLattice.Lca") extended by recursive element LCA per LUB-proof Case 4 induction.

**What's closed (2D depth — landed)**: New path (b) in `LcaOrShareIdentity` (`StateCollection.cs:240-280`):
1. Recursively computes `elemLca = elemA.Lca(elemB)` via standard dispatch (no MergeInplace at the composite — sidesteps the 0832 LeetCode narrowing trap).
2. Couples the literal's CS element node with the LCA result via `AddDescendant` — uses the CS-side as the canonical so Push propagation finds matching identity.

Pinned by 4 syntax surfaces in `Stage1InvariancePinTests.cs` and 1 algebra-level pin (`Lca_SameKindOuter_CrossKindInner_AlreadyWorks`) in `Stage1InvariancePinAlgebraTests.cs`.

**3D+ residual — NOT closed by debt #10** (correction 2026-06-27): Originally
predicted that worklist Pull would close the 3D residual, but the actual
mechanism is pure `Lca` algebra on composite elements — independent of when
Pull re-fires. `Bug55_Family_3D_Coalesce_WidensToAny` and
`Lca_CrossKind_NestedComposite_*` still `[Ignore]`'d after debt #10 closure;
their proper fix is an `Lca` algebra extension recursively widening composite
elements at arbitrary depth.

**Instrumentation retained**: `StateCollection.cs:310` emits a `TraceLog` marker at every widening-to-null event in `LcaOrShareIdentity`. Enables measurement of real-world hit-rate via `-t` flag — useful when worklist Pull lands to verify the residual surfaces close.

---

## Cleanup priority

```
#10 (worklist Pull)            CLOSED 2026-06-27 — closed #5, closed-under-default #16
#15 (Transform* identity)      ← still open; debt #10 didn't close it (different surface)
#17 3D+ residual (Lca algebra) ← still open; not Pull-related, needs Lca extension
#7  (PropagatePreferred local) ← edge-local rewrite, not urgent
#6  (TwoVariableEquality)      ← SolveUselessGenerics refinement
#9  (IsMutable cascade)        ← unify immutable-merge logic
#12 (composite defaults)       ← blocked on planned mutable-struct redesign
#13 (AreSame permissive)       ← small test-infra audit
#11 (list↔array asymmetry)     ← design decision
#8  (TypeName at FunnyType)    ← major surgery; postpone
```
