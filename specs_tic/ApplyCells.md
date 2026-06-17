# TIC Apply Cells — Canonical Truth Tables

> **Status**: v1 — Apply-cell reference for Pull, Push, Destruction.
> Parent: [`Algorithm.md`](Algorithm.md), [`Algorithm.md` §Destruction](Algorithm.md#7-phase-6--destruction), [`Algebra/CompositeConstraints.md`](Algebra/CompositeConstraints.md), [`TypeSystem.md`](TypeSystem.md).
>
> **Purpose**: a canonical truth table for every `Apply(X ancestor, Y descendant)` cell in Pull / Push / Destruction phases. Replaces having to read inline code comments to know "what happens when CompCs meets StateMap in Pull."

## 0. Reading guide

Each table row is one Apply cell, identified by the `(ancestor state, descendant state)` pair. Columns:
- **Direction**: Pull (descendant → ancestor) / Push (ancestor → descendant) / Destruction.
- **Precondition**: returns false if violated.
- **State mutation**: what changes on `ancestor.State` or `descendant.State`.
- **Edge mutation**: AddAncestor / RemoveAncestor / MergeInplace operations.
- **Eager re-Pull?**: whether `PullConstraintsForNode` is called on a node (Pull only).
- **Notes**: cite TicTechnicalDebt # if relevant.

States used in the tables:
- `Prim` = StatePrimitive
- `CS` = ConstraintsState (primitive interval)
- `CompCs` = StateCompositeConstraints (composite interval)
- `Arr` = StateArray (ee-mode covariant)
- `Coll` = StateCollection (lang-mode invariant single-arg)
- `Map` = StateMap
- `Fun` = StateFun, `Struct` = StateStruct, `Opt` = StateOptional
- `RefTo` = StateRefTo (transparent alias)

### 0.1 RefTo preamble — universal dereference

For every Apply cell in every direction, the dispatch first calls `GetNonReference()` on each side. RefTo is transparent: the cell is then dispatched on the **underlying state**, never on RefTo itself. As a consequence:

- No cell is parameterized on `RefTo` in either position.
- `Apply(RefTo, *)` and `Apply(*, RefTo)` rows are absent by design — they reduce to the underlying-state row.
- Identity of two `RefTo` chains is decided by walking each chain to its terminal owner and comparing owner identities.

The dereference is the only transformation; no state mutation or edge effect is performed at the RefTo layer.

## 1. Pull cells

### 1.1 Pull: Prim × \*

| Ancestor | Descendant | Precondition | State mutation | Edge effect | Re-Pull |
|---|---|---|---|---|---|
| Prim | Prim | `CanBePessimisticConvertedTo` | none | none | — |
| Prim | CS | `CanBeConvertedOptimisticTo` | none | — | — |
| Prim | Composite | `CanBePessimisticConvertedTo` | none | — | — |

### 1.2 Pull: CS × \*

| Ancestor | Descendant | Precondition | State mutation | Edge effect | Re-Pull |
|---|---|---|---|---|---|
| CS | Prim | — | `ApplyAncestorConstrains`: ancCopy.AddDescendant(desc); SimplifyOrNull | — | — |
| CS | CS | — | ancCopy.AddDescendant(desc.Descendant); **Preferred bidirectional copy**; AddDescendant(None) if desc.IsOptional; StructBound merge via GcdBound | RemoveAncestor | — |
| CS | Opt | (none — special path) | Wrap ancestor in Optional; create inner CS node | RemoveAncestor; AddAncestor(innerNode) on element | **Yes** (eager) |
| CS | Struct (HasStructBound) | — | GcdBound merge on descStruct; SimplifyOrNull | — | — |
| CS | Other composite | — | ApplyAncestorConstrains | — | — |

**Preferred propagation in CS × CS** is bidirectional in the algebra (Advanced/Preferred.md §3 P3 Monotonicity). This is the cell #16 fails to replicate when it falls back to AddAncestor — see §5 below.

### 1.3 Pull: Composite × Prim

| Ancestor | Descendant | Precondition | State mutation | Edge effect | Re-Pull |
|---|---|---|---|---|---|
| Opt | None | — | none (None ≤ opt(T) is reflexive) | RemoveAncestor | — |
| Opt | Prim (non-None) | — | `LiftDescendantToOptionalElement`: redirect desc→opt.ElementNode | RemoveAncestor; AddAncestor(opt.ElementNode) | **Yes** |
| Composite (non-Opt) | Prim | — | false (composite cannot accept primitive descendant) | — | — |

### 1.4 Pull: Composite × CS (the dispatcher)

**Guard rule** : non-Opt composite ancestor + IsOptional CS descendant → **return false** (the `opt(T) ≤ T` rejection).

| Ancestor | Descendant | Precondition | State mutation | Edge effect | Re-Pull |
|---|---|---|---|---|---|
| Arr | CS | desc.HasStructBound → false | `TransformToArrayOrNull(name, desc)`; install result | RemoveAncestor; **identity guard**: AddAncestor(ancArr.elem) only if not aliased | — |
| Coll | CS | desc.HasStructBound → false | `TransformToCollectionOrNull(kind, name, desc)` | RemoveAncestor; AddAncestor on elem (identity-guarded) | — |
| Map | CS | desc.HasStructBound → false | `TransformToMapOrNull(name, desc)` | RemoveAncestor; AddAncestor on key/value (identity-guarded) | — |
| Fun | CS | desc.HasStructBound → false | `TransformToFunOrNull`; install | RemoveAncestor; AddAncestor on ret/args | — |
| Struct | CS | — | `TransformToStructOrNull`; TypeName merge; width propagation | RemoveAncestor; optional AddField; field nodes AddAncestor | — |
| Opt | CS | (transform path or implicit lift) | TransformToOptionalOrNull OR explicit opt-wrap | RemoveAncestor; AddAncestor on elem | sometimes |

**Identity guards** (debt #15): `TransformTo*` may reuse descendant's element node; the guard `if (result.ElementNode != ancArray.ElementNode) result.ElementNode.AddAncestor(ancArray.ElementNode)` prevents self-edge panic.

### 1.5 Pull: same-shape composite × same-shape composite

| Ancestor | Descendant | Precondition | State mutation | Edge effect | Re-Pull |
|---|---|---|---|---|---|
| Arr | Arr | — | (optional: Optional-element propagation during re-pull after PullNoneNode) | AddAncestor on elem (if ≠) | — |
| Coll | Coll | Constructor equal OR (IsArrayBranchKind ∧ desc ⊆ anc) | none | AddAncestor on elem (if ≠); RemoveAncestor | — |
| Arr | Coll | desc.Constructor in Array-branch | none | AddAncestor on elem; RemoveAncestor | — |
| Coll | Arr | anc.Constructor in Array-branch | none | AddAncestor on elem; RemoveAncestor | — |
| Map | Map | — | none | AddAncestor on key (if ≠); AddAncestor on value (if ≠); RemoveAncestor | — |
| Fun | Fun | ArgsCount equal | none | AddAncestor desc.RetNode → anc.RetNode; **anc.ArgNodes[i].AddAncestor(desc.ArgNodes[i])** (contravariance); RemoveAncestor | — |
| Struct | Struct | — | TypeName merge; IsOptionalSourced propagate; width propagation on mutable anc; per-field merge | RemoveAncestor; optional AddField; field nodes AddAncestor | — |
| Opt | Opt | — | none | AddAncestor on elem if not μ-cycle tautology; RemoveAncestor | — |

### 1.6 Pull: CompCs cells

The 4 critical Apply cells dispatched from Pull for CompCs:

| Ancestor | Descendant | Precondition | State mutation | Edge effect | Re-Pull |
|---|---|---|---|---|---|
| CompCs | CompCs | — | `ApplySameClass`: UnifyCompCs | RemoveAncestor | — |
| CompCs | Prim (None) | — | WithIsOptional(true) | RemoveAncestor | — |
| CompCs | Prim (non-None) | — | false | — | — |
| CompCs | CS | desc.HasDesc(Coll or Arr) | route to `ForwardPullCompCsSc` / `ForwardCompCsStateArray` | RemoveAncestor (downstream) | **Yes** |
| CS | CompCs | — | `ConcretestCompCs` → install or defer | RemoveAncestor if deferred | — |
| **CompCs** | **Coll** | K ⊆ anc.Anc (lattice) | `ForwardPullCompCsSc`: newDescKind = Lca(Desc, K); try `MergeInplace(anc.elem, sc.elem)`; fallback `AddAncestor` | RemoveAncestor | **Yes (anc.elem)** |
| **CompCs** | **Arr** | — | `ForwardCompCsStateArray`: try MergeInplace; fallback AddAncestor | RemoveAncestor | **Yes (anc.elem)** |
| **CompCs** | **Map** | Map ⊆ anc.Anc | `ForwardPullCompCsStateMap`: **synthesize pair-struct** `{key: sm.KeyNode, value: sm.ValueNode}`; MergeInplace(anc.elem, structNode) | RemoveAncestor | **Yes (anc.elem)** |
| CompCs | Opt | — | unwrap; AddAncestor on elem | RemoveAncestor; AddAncestor on elem | — |
| Coll | CompCs | K ⊆ desc.Anc | `ReversePullScCompCs`: collapse CompCs to SC(K); MergeInplace on elem | RemoveAncestor | — |
| Arr | CompCs | — | `ReverseCompCsStateArray` (isPull=true): MergeInplace on elem | RemoveAncestor | **Yes** |
| Map | CompCs | Map ⊆ desc.Anc | `ReversePullStateMapCompCs`: synth pair-struct; MergeInplace; collapse to StateMap | RemoveAncestor | — |
| Opt | CompCs | — | unwrap via implicit lift; recurse on elem | RemoveAncestor; AddAncestor on elem | — |
| CompCs | Fun | — | false (CompCs constrains to collection-shape; Fun is not in the lattice) | — | — |
| CompCs | Struct | desc is the pair-struct from prior synth | route via `Apply(CompCs, Coll/Arr/Map)` element-axis MergeInplace; otherwise false | — | — |
| Fun | CompCs | — | false (mirror of CompCs × Fun) | — | — |
| Struct | CompCs | — | false (mirror of CompCs × Struct, modulo pair-struct synth case handled at element axis) | — | — |

**The pair-struct synthesis** in the `CompCs × Map` cell is the bridge that lets Map satisfy `Enumerable<{key, value}>`. See §7 of [`TypeSystem.md`](TypeSystem.md) and §3 of [`Algebra/StateMap.md`](Algebra/StateMap.md).

## 2. Push cells

Push is dual to Pull: it propagates ancestor constraints DOWN to descendant. Generally simpler — Push doesn't add edges, only validates compatibility and propagates state.

### 2.1 Push: Composite × CS (the dispatcher)

**Guard rule** : non-Opt composite ancestor + IsOptional CS descendant → **wrap desc in Optional** (Push version, contrasts Pull's false-return).

| Ancestor | Descendant | Precondition | State mutation | Notes |
|---|---|---|---|---|
| Arr | CS | — | TransformToArrayOrNull; AddAncestor on elem; PushConstraints on elem | — |
| Coll | CS | — | TransformToCollectionOrNull; same | — |
| Map | CS | — | TransformToMapOrNull; AddAncestor key/value; PushConstraints both | — |
| Fun | CS | — | TransformToFunOrNull; PushFunTypeArgumentsConstraints | — |
| Struct | CS | — | TransformToStructOrNull; TryMergeStructFields (MergeInplace/PushConstraints per field) | — |
| Opt | CS | various | Materialize opt(inner); AddAncestor; PushConstraints; OR implicit lift | several sub-cases |

### 2.2 Push: same-shape composite × same-shape composite

| Ancestor | Descendant | State mutation |
|---|---|---|
| Opt × Opt | PushConstraints(desc.elem, anc.elem) |
| Arr × Arr | PushConstraints(desc.elem, anc.elem) |
| Coll × Coll | PushConstraints(desc.elem, anc.elem) — constructor check first |
| Arr × Coll, Coll × Arr | cross-family: PushConstraints on elements |
| Map × Map | PushConstraints on both key+value |
| Fun × Fun | PushFunTypeArgumentsConstraints (contra on args, co on ret) |
| Struct × Struct | per-field merge: TryMergeStructFields + open-row width propagation |

### 2.3 Push: CompCs cells

| Ancestor | Descendant | Precondition | State mutation |
|---|---|---|---|
| CompCs | CompCs | — | UnifyCompCs |
| CompCs | CS | desc.HasDesc(Coll or Arr) | `ForwardPushCompCsSc` / `ForwardCompCsStateArray` (precondition-only) |
| CompCs | Coll | K ⊆ anc.Anc | `ForwardPushCompCsSc`: precondition + element invariance check |
| CompCs | Arr | — | `ForwardCompCsStateArray` (isPull=false): precondition |
| CompCs | Map | Map ⊆ anc.Anc | `ForwardPushCompCsStateMap`: precondition |
| Coll | CompCs | precondition | `ReversePushScCompCs`: GCD on CompCs.Anc; MergeInplace on elem |
| Arr | CompCs | — | `ReverseCompCsStateArray` (isPull=false) |
| Map | CompCs | precondition | `ReversePushStateMapCompCs`: GCD on CompCs.Anc; MergeInplace on elem |

## 3. Destruction cells

By Destruction time:
- Two-phase Pull is complete (Phase 1: None nodes set IsOptional flags; Phase 2: non-None nodes).
- Push is complete.
- All ConstraintsState IsOptional flags are finalized.
- The graph is fully constrained; Destruction does final resolution.

Destruction-specific concerns:
- **WrapAncestorInOptional**: when ancestor needs to be wrapped in Optional to absorb an Optional descendant.
- **WrapDescendantInOptional**: dual, when descendant needs wrapping.
- **FlattenNestedOptional**: post-destruct pass on Opt nodes to remove `opt(opt(T))` nesting.
- **MaterializeOptionalFlags**: convert CS.IsOptional=true to actual StateOptional state (Destruction wrapper).

### 3.1 Destruction: critical CompCs collapse paths

| Ancestor | Descendant | State mutation |
|---|---|---|
| CompCs | CompCs | `ApplySameClass` (UnifyCompCs) |
| CompCs | Prim (non-Any) | `ConcretiseAndRetryForward`: collapse CompCs to concrete (StateCollection / StateMap), retry |
| Prim (non-Any) | CompCs | `ConcretiseAndRetryReverse`: collapse descendant CompCs, retry |
| CompCs | Coll | `ForwardPullCompCsSc` (same as Pull path) |
| CompCs | Arr | `ForwardCompCsStateArray` (same as Pull) |
| **CompCs** | **Map** | `ForwardPullCompCsStateMap`: synth pair-struct, MergeInplace |
| Coll | CompCs | `ReversePullScCompCs`: collapse to concrete |
| Arr | CompCs | `ReverseCompCsStateArray` (isPull=true) |
| **Map** | **CompCs** | `ReversePullStateMapCompCs`: synth pair-struct, collapse to StateMap |

### 3.2 Destruction: Optional wrapping rules

`WrapAncestorInOptional` :
- Triggered when LCA(Composite, Opt(Composite)) = Opt.
- Wraps ancestor's composite in StateOptional, absorbing the Optional descendant.
- **Guards**: no wrap if ancestor already opt-wrapped, or pinned (SyntaxNode / Named / IsSignatureParam / IsSolved Primitive).
- Creates inner TypeVariable node to preserve composite state.

`WrapDescendantInOptional` :
- Dual: when LCA(Opt(Composite), Composite) = Opt.
- Guards similar.

`FlattenNestedOptional` (in SolvingFunctions, called from `DestructionRecursive` and the StateOptional setter):
- Post-destruct pass on `StateOptional` nodes.
- Removes redundant `opt(opt(T)) → opt(T)`.
- Also fires in State setter and Destruction itself.

`MaterializeOptionalFlags` (in SolvingFunctions, post-Destruction pass):
- Post-Destruction pass.
- Converts CS with `IsOptional=true` to actual `StateOptional(innerCsNode)`.
- Special case: pure Optional with no other constraints → `StatePrimitive.None`.

### 3.3 Destruction: same-shape pairs

| Ancestor | Descendant | State mutation |
|---|---|---|
| Opt × Opt | Destruction(desc.ElementNode, anc.ElementNode) |
| Arr × Arr | Destruction(desc.ElementNode, anc.ElementNode) (covariant) |
| Coll × Coll | constructor check; MergeInplace on elements |
| Arr × Coll, Coll × Arr | cross-family element destruct |
| **Map × Map** | **MergeInplace on both KeyNode and ValueNode** |
| Fun × Fun | recursive args (contravariant) + return (covariant) |
| Struct × Struct | field-by-field destruct; stale-None-vs-Optional snapshot handling |

### 3.4 Destruction: stale snapshot handling

A Pull Phase 1 snapshot may have captured a non-Optional struct field that Phase 2 later wrapped to opt(T). Destruction handles this:

- **Mutable struct, stale None vs Optional field**: redirect None field to `StateRefTo(opt field)`.
- **Immutable struct (width-subtyped), stale snapshot**: same redirect (None ≤ opt(T) algebraic lift).

The `DescendantHasOptionalLift` workaround (debt #5) handles the related case where ancestor.Descendant snapshot is non-Optional but the actual descendant is now opt-wrapped.

## 4. Key design patterns

### 4.1 The guard rule (Pull vs Push divergence)

```
Pull:  Composite-non-Opt anc × CS-IsOptional desc → return false  (rejects opt(T) ≤ T)
Push:  same case             → wrap descendant in Optional, recurse  (legacy behavior)
Destr: same case             → WrapAncestorInOptional               (materialization)
```

The asymmetry is intentional: Pull is strict (no implicit unwrap), Push is permissive (the wrap is always sound during Push direction).

### 4.2 Eager re-Pull

When a Pull cell mutates state of a node AFTER the streaming toposort has already visited it, the new constraints would not propagate without an explicit re-Pull. Cells calling `PullConstraintsForNode`:

- `LiftDescendantToOptionalElement` — on descendantNode (rewired)
- `ForwardPullCompCsSc` — on ancestor.ElementNode (after MergeInplace)
- `ForwardCompCsStateArray` — on ancestor.ElementNode
- `ForwardPullCompCsStateMap` — on ancestor.ElementNode (after struct synth)
- `ReverseCompCsStateArray` (isPull=true) — on descendant.ElementNode

**Eager re-Pull does NOT traverse newly-added AddAncestor edges from descendant's element to ancestor's element** when those edges connect to nodes outside the re-Pull starting point. This is the root cause of debt #16.

### 4.3 Identity guards (debt #15)

`TransformToArrayOrNull / TransformToCollectionOrNull / TransformToMapOrNull` reuse the descendant's existing ElementNode when not solved (perf optimization). To prevent self-edge panic in chained `[]` access:

```
if (result.ElementNode != ancArray.ElementNode)
    result.ElementNode.AddAncestor(ancArray.ElementNode);
```

The proper fix (always allocate fresh) is tracked in debt #15.

### 4.4 MergeInplace vs AddAncestor — the precision trade-off (debt #16)

`CompCsApply.ForwardPullCompCsSc` and `ForwardCompCsStateArray` use try-MergeInplace-fallback-to-AddAncestor for element propagation:

```
if (CanMergeStates(ancestor.ElementNode, sa.ElementNode))
    MergeInplace(ancestor.ElementNode, sa.ElementNode);
else
    sa.ElementNode.AddAncestor(ancestor.ElementNode);
PullConstraintsForNode(ancestor.ElementNode);
```

**MergeInplace** is the strict path: it preserves element-node identity. Element constraints from both sides land on the merged node. Preferred metadata is propagated as part of GetMergedStateOrNull.

**AddAncestor** is the loose path: when MergeInplace would fail (e.g., not-yet-resolved composite shapes like function types), it adds a subtyping edge. The asymmetry is:
- Constraints flow from descendant's element UP to ancestor's element via the new edge.
- But Preferred metadata propagation requires bidirectional copy ([`Algebra/BaseOperators.md`](Algebra/BaseOperators.md) §LCA; [`Advanced/Preferred.md`](Advanced/Preferred.md) §3 P3).
- The current AddAncestor path does NOT propagate Preferred — this is the formally-identified violation of P3 Monotonicity (proven in [`Proofs.md`](Proofs.md)).

**See §5 for the formal identification.**

## 5. Debt #16 precise location

The fallback lives in `ForwardPullCompCsSc` and `ForwardCompCsStateArray` (the try-MergeInplace-fallback-AddAncestor block).

```csharp
if (CanMergeStates(ancestor.ElementNode, sa.ElementNode))
    SolvingFunctions.MergeInplace(ancestor.ElementNode, sa.ElementNode);
else
    sa.ElementNode.AddAncestor(ancestor.ElementNode);
SolvingFunctions.PullConstraintsForNode(ancestor.ElementNode);
```

The **AddAncestor fallback** adds a directed edge `sa.ElementNode → ancestor.ElementNode` (subtyping: sa's element is a subtype of ancestor's element).

Streaming Pull's invariant: when an edge is added after the source has been processed, eager re-Pull on the affected nodes propagates. But:
- `PullConstraintsForNode(ancestor.ElementNode)` re-fires Pull on ancestor.elem.
- ancestor.elem's ancestors are walked. But sa.elem is a DESCENDANT of ancestor.elem now, not an ancestor.
- Pull only walks UP (descendant → ancestor). The new edge from sa.elem (descendant) is processed when **sa.elem's** Pull fires — but sa.elem has already been processed in the streaming toposort.

So sa.elem's constraints (including its Preferred metadata) never propagate up the new edge. ancestor.elem stays as its pre-Apply state.

This is the formal violation of P3 Monotonicity documented in [`Proofs.md`](Proofs.md) §3, closed for the Preferred axis by `PropagatePreferredAcrossFallback`. The Descendant axis remains open — see [`TechnicalDebt.md`](TechnicalDebt.md) #16.

## 6. Related specs

- [`Algorithm.md`](Algorithm.md) — Pull / Push phases, two-phase Pull, streaming toposort
- [`Algorithm.md` §Destruction](Algorithm.md#7-phase-6--destruction) — Destruction phase, materialization rules
- [`Algebra/CompositeConstraints.md`](Algebra/CompositeConstraints.md) — CompCs Layer 0/1/2 algebra
- [`Algebra/StateMap.md`](Algebra/StateMap.md) — StateMap algebra
- [`Algebra/LcaOrShareIdentity.md`](Algebra/LcaOrShareIdentity.md) — identity-sharing LCA
- [`TypeSystem.md`](TypeSystem.md) — type system overview
- [`TechnicalDebt.md`](TechnicalDebt.md) — debt #15 (Transform* identity), debt #16 (Preferred precision)
- Forthcoming: `Proofs.md` — P3 Monotonicity formal proof of debt #16
