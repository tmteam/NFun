# Technical Debt

> **Scope**: known limitations of the current TIC implementation that deviate from the ideal algebra. Each entry: what's wrong, why it exists, what the proper fix is.

## Status legend

- **OPEN**: violation present in production code.
- **PARTIALLY CLOSED**: some dimensions / cases fixed, others remain.
- **CLOSED**: resolved; entry retained for historical context until next cleanup pass.

---

## 5. `DescendantHasOptionalLift` workaround — OPEN

**What's wrong**: Pull Phase-1 snapshots can become stale by Phase 2. A snapshot captured before None-absorption may show a non-Optional struct field that becomes `opt(T)` later. Destruction must reconcile.

**Why it exists**: two-phase Pull was introduced to handle None-absorption order-of-operations. Phase 1 sets `IsOptional` flags; Phase 2 processes non-None constraints with those flags already set.

**Current mitigation**: `DescendantHasOptionalLift` check in Destruction redirects stale snapshots to the actual (lifted) form.

**Proper fix**: single-pass Pull refactor — eliminate the two-phase split and the snapshot drift it creates. Related to debt #10.

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

## 10. Pull edge-rewires violate single-pass toposort — OPEN (closes via worklist Pull)

**What's wrong**: Pull is a streaming single-pass over toposorted nodes. When an Apply cell adds a new edge (`AddAncestor`) after the source was already visited, the new edge would not propagate. Current code mitigates with `PullConstraintsForNode` eager re-Pull calls in specific cells.

**Why it exists**: streaming Pull is faster than a worklist algorithm in the common case.

**Proper fix**: worklist Pull architecture spec'd in [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md). Implementation pending.

This also closes debt #5 (stale snapshots), debt #15 (identity guards), and debt #16's Descendant axis.

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

## 14. `a[i] = v` pin to `array<T>` — RESOLVED

The indexed-write operation was pinned to `array<T>` (mutable array kind), breaking the list-alias path. Resolved by routing through the proper StateCollection slot.

---

## 15. `Transform*OrNull` element-node reuse + identity guards — OPEN (closes via worklist Pull)

**What's wrong**: `TransformToArrayOrNull`, `TransformToCollectionOrNull`, `TransformToMapOrNull` reuse the descendant collection's `ElementNode` directly (perf optimization). When the descendant's element identity aliases the ancestor's element (chained `[]` over lang collections), `AddAncestor(self)` panics.

**Current mitigation**: identity guards in `Apply(ICompositeState ancestor, CS descendant)` cells.

**Proper fix**: always allocate fresh element nodes in `Transform*`. Trade-off: extra allocations + node registration per Transform call. Closed by worklist Pull (debt #10).

---

## 16. CompCs cross-Apply Preferred propagation loss — PARTIALLY CLOSED

> **Preferred axis CLOSED**. **Descendant axis CONJECTURED OPEN** (no proven TIC-level counterexample post-fix).
>
> Formal identification: [`Proofs.md`](Proofs.md) §3 (P3 Monotonicity, axes P3a / P3b).

**What's wrong**: CompCs cross-Apply cells (`ForwardPullCompCsSc`, `ForwardCompCsStateArray`, `ReverseCompCsStateArray`) use try-MergeInplace-fallback-to-AddAncestor on element nodes. The fallback path historically did not propagate Preferred metadata, violating P3 Monotonicity on the Preferred axis.

**What's closed (P3a)**: `PropagatePreferredAcrossFallback` helper restores P3 on the Preferred axis at the AddAncestor fallback boundary.

**What remains conjectured open (P3b)**: the Descendant axis at the AddAncestor fallback. The streaming-toposort gap (eager re-Pull walks ancestors of `anc.elem`, not the new incoming edge from `desc.elem`) is a plausible mechanism, but no test exhibits a TIC-level (Finalize-time) violation post-PropagatePreferredAcrossFallback. See [`Proofs.md`](Proofs.md) §3.6.

**Closed-runtime side-effect**: `LangMirror_NestedByteUpcastMap_RealResult` was previously cited as a P3b counterexample. Trace analysis showed TIC infers the correct outer-map element with the Preferred fix; the residual failure was **runtime materialization** of the inner `.map()`'s lambda — TIC widens the inner element type along the outer chain (e.g. `byte → Real` or `Int32 → Real`), but the concrete collection still stores narrower values, so each element arrived at the lambda call site at its original CLR type. Closed by per-element coercion in `MapFunction.ConcreteMap.Calc` / `MapEnumerableFunction.ConcreteMap.Calc`, mirroring the existing pattern in `SumIter.As<T>`.

**Path to closure**: worklist Pull (debt #10) — the principled fix, spec'd in [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md) — routes `desc.elem.D` through the standard CS×CS path with all safety guarantees of Lemma 3.1.

---

## Cleanup priority

```
#10 (worklist Pull)            ← also closes #5, #15, #16's Descendant axis
#7  (PropagatePreferred local) ← edge-local rewrite, not urgent
#6  (TwoVariableEquality)      ← SolveUselessGenerics refinement
#9  (IsMutable cascade)        ← unify immutable-merge logic
#12 (composite defaults)       ← blocked on planned mutable-struct redesign
#13 (AreSame permissive)       ← small test-infra audit
#11 (list↔array asymmetry)     ← design decision
#8  (TypeName at FunnyType)    ← major surgery; postpone
```
