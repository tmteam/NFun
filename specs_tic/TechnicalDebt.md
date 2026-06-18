# Technical Debt

> **Scope**: known limitations of the current TIC implementation that deviate from the ideal algebra. Each entry: what's wrong, why it exists, what the proper fix is.

## Status legend

- **OPEN**: violation present in production code.
- **PARTIALLY CLOSED**: some dimensions / cases fixed, others remain.
- **ARCHITECTURAL TRADE-OFF / DESIGN TRADE-OFF**: accepted; no further fix planned.

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

This also closes debt #5 (stale snapshots), debt #15 (identity guards), debt #16's Descendant axis, and the 3D+ residual of debt #17 (cross-kind nested-composite LCA): worklist re-firing lets `LcaOrShareIdentity` recompute after deep CS nodes resolve to concrete shape, removing the 1-level depth bound on path (b).

**Surface pinned for the cross-kind nested-composite residual** (closes-via-#10):
- `Bug55_Family_3D_Coalesce_WidensToAny` (`Stage1InvariancePinTests.cs`, `[Ignore]`'d)
- `Lca_CrossKind_NestedComposite_BothResolved_ShouldWiden` and `_3D_ShouldWiden` (`Stage1InvariancePinAlgebraTests.cs`, both `[Ignore]`'d)

Formally diagnosed in [`Proofs.md`](Proofs.md) P3 Monotonicity: same-identity short-circuit alone closes an artificially-constructed sub-family (where prior calls pre-merged element identities) but does not converge at first-time 3D/4D entries because the recursive descent through `LcaOrShareIdentity` sees physically distinct ElementNodes at each layer until the innermost primitive. Cross-kind `MergeInplace` of composite elements is precluded by the 0832 LeetCode narrowing trap (routes through `NarrowerArrayBranchOrNull`). Worklist re-fire is the only mechanism that lets the outer LCA recompute after the inner CS resolves.

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

**Proper fix**: always allocate fresh element nodes in `Transform*`. Trade-off: extra allocations + node registration per Transform call. Closed by worklist Pull (debt #10).

---

## 16. CompCs cross-Apply Preferred propagation loss — PARTIALLY CLOSED

> **Preferred axis CLOSED**. **Descendant axis CONFIRMED OPEN** — Finalize-time counterexample reproduced (Bug hunt #47).
>
> Formal identification: [`Proofs.md`](Proofs.md) §3 (P3 Monotonicity, axes P3a / P3b).

**What's wrong**: CompCs cross-Apply cells (`ForwardPullCompCsSc`, `ForwardCompCsStateArray`, `ReverseCompCsStateArray`) use try-MergeInplace-fallback-to-AddAncestor on element nodes. The fallback path historically did not propagate Preferred metadata, violating P3 Monotonicity on the Preferred axis.

**What's closed (P3a)**: `PropagatePreferredAcrossFallback` helper restores P3 on the Preferred axis at the AddAncestor fallback boundary.

**What remains OPEN (P3b — confirmed counterexample)**: the Descendant axis at the AddAncestor fallback AND at the empty-CS deferred-accept (`PullConstraintsFunctions.cs:660-690`, the no-positively-forbidding-bound branch). Bug hunt #47 (`data:list<list<int>>; data.map(rule it.first())` widens output element to `Any`) is a clean Finalize-time TIC counterexample: `first`'s generic `T₀` carries no Preferred (no Arithmetical constraint, unlike `sum`), and its CS stays empty until the lambda's `it` resolves via map's outer binding — by which time streaming Pull has already moved past the relevant edge and never re-fires the element unification. Two narrow heuristic fixes (RefTo-promote with and without single-ancestor guard) regressed 41 and 8 working tests respectively because they could not distinguish "must re-fire on tightening" from "already resolved, no re-fire needed" — exactly the discrimination worklist Pull encodes for free.

**Closed-runtime side-effect**: `LangMirror_NestedByteUpcastMap_RealResult` was previously cited as a P3b counterexample. Trace analysis showed TIC infers the correct outer-map element with the Preferred fix; the residual failure was **runtime materialization** of the inner `.map()`'s lambda — TIC widens the inner element type along the outer chain (e.g. `byte → Real` or `Int32 → Real`), but the concrete collection still stores narrower values, so each element arrived at the lambda call site at its original CLR type. Closed by per-element coercion in `MapFunction.ConcreteMap.Calc` / `MapEnumerableFunction.ConcreteMap.Calc`, mirroring the existing pattern in `SumIter.As<T>`.

**Path to closure**: worklist Pull (debt #10) — the principled fix, spec'd in [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md) — routes `desc.elem.D` through the standard CS×CS path with all safety guarantees of Lemma 3.1. Pinned by `Bug47_MapItFirstOnNestedList_WidensToAny` (`[Ignore]`'d) and `Bug47_Workaround_MapFunctionReference_Works` (passes — function-reference path resolves correctly via StateFun unification).

---

## 17. `LcaOrShareIdentity` widens to `Any` for cross-kind nested-composite elements — CLOSED (2D depth); 3D+ residual rolled into debt #10

> **CLOSED for the 2D-depth surface family** via path (b) in `LcaOrShareIdentity`. 3D+ residual is a worklist-Pull manifestation — pinned under debt #10.

**What was wrong**: `StateCollection.LcaOrShareIdentity` historically gated its cross-kind identity-share branch on `Element is not ICompositeState && xKindOther.Element is not ICompositeState`. When both elements were themselves composite (e.g. `list<list<I32>>` vs `array<array<I32>>`), the guard fired and the caller widened to `Any` — even though the algebraic answer is well-defined per spec `BaseOperators.md:27` ("climb lattice via ConstructorLattice.Lca") extended by recursive element LCA per LUB-proof Case 4 induction.

**What's closed (2D depth — landed)**: New path (b) in `LcaOrShareIdentity` (`StateCollection.cs:240-280`):
1. Recursively computes `elemLca = elemA.Lca(elemB)` via standard dispatch (no MergeInplace at the composite — sidesteps the 0832 LeetCode narrowing trap).
2. Couples the literal's CS element node with the LCA result via `AddDescendant` — uses the CS-side as the canonical so Push propagation finds matching identity.

Pinned by 4 syntax surfaces in `Stage1InvariancePinTests.cs` and 1 algebra-level pin (`Lca_SameKindOuter_CrossKindInner_AlreadyWorks`) in `Stage1InvariancePinAlgebraTests.cs`.

**3D+ residual — rolled into debt #10**: Path (b) is bounded to 1-level depth via a guard (`xKindOther.Element is StateCollection deeperOther && deeperOther.Element is ICompositeState`). Professorial review (commit aftermath) traced the root cause to first-time-entry recursion seeing physically distinct ElementNodes at each layer until the innermost primitive — a same-identity short-circuit closes only an artificial sub-family (prior calls pre-merged identities) and re-introduces FU758 / Confluence-P3 violation risk. Principled closure requires re-firing LCA after deep CS resolution, which IS debt #10's worklist Pull mechanism. The 3D residual surfaces now live under debt #10's "also closes" list above.

**Instrumentation retained**: `StateCollection.cs:310` emits a `TraceLog` marker at every widening-to-null event in `LcaOrShareIdentity`. Enables measurement of real-world hit-rate via `-t` flag — useful when worklist Pull lands to verify the residual surfaces close.

---

## Cleanup priority

```
#10 (worklist Pull)            ← also closes #5, #15, #16's Descendant axis, #17 3D+ residual
#7  (PropagatePreferred local) ← edge-local rewrite, not urgent
#6  (TwoVariableEquality)      ← SolveUselessGenerics refinement
#9  (IsMutable cascade)        ← unify immutable-merge logic
#12 (composite defaults)       ← blocked on planned mutable-struct redesign
#13 (AreSame permissive)       ← small test-infra audit
#11 (list↔array asymmetry)     ← design decision
#8  (TypeName at FunnyType)    ← major surgery; postpone
```
