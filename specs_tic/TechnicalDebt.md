# Technical Debt

> **Scope**: known limitations of the current TIC implementation that deviate from the ideal algebra. Each entry: what's wrong, why it exists, what the proper fix is.

## Status legend

- **OPEN**: violation present in production code.
- **PARTIALLY CLOSED**: some dimensions / cases fixed, others remain.
- **ARCHITECTURAL TRADE-OFF / DESIGN TRADE-OFF**: accepted; no further fix planned.

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

## 18. TicNode.State setter solved-invariant assert: clause (4) over-wide — OPEN

**What's wrong**: the `Debug.Assert` guarding transitions out of a solved (`IsMutable=false`)
state allows clause (4) — *any* anonymous `StateStruct` re-assignment — which is wider than
the three legitimate transitions (idempotent / `StateRefTo` rewire / Optional lift over
composite). The solved-node invariant is effectively disabled for all anonymous structs.

**Why it exists**: row-poly merges + `LiftMuTypes` promotion to `CS{StructBound}` legitimately
re-assign anonymous struct states; narrowing the clause re-triggers
`BugC_LcaOfRecursiveVarsInArray`.

**Proper fix**: enumerate the legal struct transitions (width-growth, μ-promotion) explicitly
instead of exempting the whole class. Debug-only impact.

**Code**: `TicNode.cs` State setter, `// WORKAROUND: clause (4)` comment.

---

## Cleanup priority

```
#15 (Transform* identity)      ← open; worklist Pull didn't close it (different surface)
#18 (State setter clause 4)    ← enumerate legal struct transitions; debug-only
#7  (PropagatePreferred local) ← edge-local rewrite, not urgent
#6  (TwoVariableEquality)      ← SolveUselessGenerics refinement
#9  (IsMutable cascade)        ← unify immutable-merge logic
#12 (composite defaults)       ← blocked on planned mutable-struct redesign
#13 (AreSame permissive)       ← small test-infra audit
#11 (list↔array asymmetry)     ← design decision
#8  (TypeName at FunnyType)    ← major surgery; postpone
```

## Closed (removed from this file; history in git and linked specs)

- **#5 `DescendantHasOptionalLift`** — CLOSED 2026-06-27 by #10 (helpers removed, commit `3518214b`).
- **#10 worklist Pull** — CLOSED 2026-06-27 (commit `214a5778`), **hardened 2026-07-03**
  (commit `79781371`): dedicated `TicNode.EnqueueMark` (Enqueue clobbered PullRec's
  traversal `VisitMark` → SO on collection-recursive μ-types) + run-scoped coinductive
  discharged-pair memo (consumed composite edges re-emitted around μ-knots → worklist
  never drained). See [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md).
- **#16 CompCs Preferred loss** — CLOSED-UNDER-DEFAULT 2026-06-27 (gated under
  `!WorklistPullDriver.IsActive`, commit `54c264d5`).
- **#17 `LcaOrShareIdentity` cross-kind nested widening** — CLOSED 2026-06-29
  (fresh-node path (b), `StateCollection.cs`; pinned in `Stage1InvariancePin*`).
- **Identity-sharing (mutating) None+CS branches in `Lca`** — REMOVED 2026-07-03 without
  replacement: under Rule B the pure element-Lca path preserves IsOptional + Preferred
  (`CS×CS` arm + the `opt(P) ∨ CS(Pref)` rule), so the Bug#6-era side-effecting branches
  (`StateArrayLcaOrShareIdentity`, `LcaStructFields` None+CS) became dead weight. `Lca` is
  a pure join again for these pairs. The redundant Push struct-arm None-skip went with them
  (general rule `None ≤ CS?` lives in `Apply(CS, StatePrimitive)`).
- **Dead-invisible-snapshot family / canonical Optional form** — CLOSED 2026-07-03
  (commit `c5593f69`): rule "opt(τ) implies τ solved; the Optional lift of an unsolved
  [D..A] is the flag form [D..A]?" enforced in Concretest, Push (`None ≤ CS?`, covariant
  `CS{Desc=arr}×arr` descent), Destruction (RefTo short-circuit gated by descendant-covers-
  join), Pull (inner-of-Optional absorbs the Optional factor of incoming bounds). Pinned by
  `UnitTests/CanonicalOptionalFormTest`.
