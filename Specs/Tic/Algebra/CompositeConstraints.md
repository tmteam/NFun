# StateCompositeConstraints — Composite-Interval Algebra

> **What this is**: a composite analogue of `ConstraintsState`, used when a function signature requires "some collection kind" (e.g. `Enumerable<T>`) without committing to which one.
>
> **When created**: at function-signature slot setup time (e.g. `count(arr: Enumerable<T>)`).
>
> **When eliminated**: collapses to a concrete `StateCollection` / `StateMap` once the descendant and ancestor caps converge on a single ConstructorKind.

## 1. Domain

```
StateCompositeConstraints {
    Ancestor    : ConstructorKind?    // upper bound in lattice (may be null = unconstrained)
    Descendant  : ConstructorKind?    // lower bound in lattice (may be null)
    ElementNode : TicNode             // mandatory; shared identity for the element
    IsOptional  : bool                // absorbed-None flag
}
```

Subtyping: a CompCs represents the set `{ SC(K, e) | Descendant ≤_L K ≤_L Ancestor AND e satisfies ElementNode constraints }` ∪ (if `IsOptional`) Optional wrappers.

## 2. Layer 0 — symmetric content operators

These are pure functions on state pairs without graph mutation.

### 2.1. LCA

```
Lca(CompCs{Anc_a, Desc_a, e_a, IsOpt_a}, CompCs{Anc_b, Desc_b, e_b, IsOpt_b}) =
    CompCs{
        Anc:   max_L(Anc_a, Anc_b)        // lattice LCA on upper bounds
        Desc:  min_L(Desc_a, Desc_b)      // lattice GCD on lower bounds
        e:     element_node_identity(e_a, e_b)
        IsOpt: IsOpt_a OR IsOpt_b
    }
```

Where `max_L` / `min_L` are `ConstructorLattice.Lca` / `ConstructorLattice.Gcd`. Element nodes are merged via `LcaOrShareIdentity` (see [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md)) when both are unresolved.

### 2.2. GCD / Unify (same formula)

```
Gcd(CompCs_a, CompCs_b) = Unify(CompCs_a, CompCs_b) =
    CompCs{
        Anc:   min_L(Anc_a, Anc_b)        // narrower upper bound
        Desc:  max_L(Desc_a, Desc_b)      // narrower lower bound
        e:     merged identity
        IsOpt: IsOpt_a AND IsOpt_b
    }
    null if min_L(Anc_a, Anc_b) < max_L(Desc_a, Desc_b)  // empty interval
```

(In CompCs, same-class GCD and Unify coincide because the interval is between ConstructorKinds, not between general types.)

### 2.3. Cross-class with StateCollection

```
Lca(CompCs{Anc, Desc, e, IsOpt}, StateCollection(K, e_sc)) =
    if Desc ≤_L K ≤_L Anc:
        CompCs{
            Anc:   Anc                     // unchanged
            Desc:  max_L(Desc, K)          // tightened by K (narrower-wins)
            e:     identity_merge(e, e_sc)
            IsOpt
        }
    else:
        Any (K outside interval)
```

The narrower-wins rule: the resulting CompCs has K as a tightened Descendant, not collapsed to `StateCollection(K)`. Reason: there may be other concurrent CompCs constraints; keep the interval form until convergence.

### 2.4. Cross-class with StateMap

```
Lca(CompCs{Anc=Enumerable, Desc, e, IsOpt}, StateMap(K, V)) =
    let synth = StateStruct({key: K, value: V}, isFrozen=true)
    in CompCs{
        Anc:   Enumerable
        Desc:  max_L(Desc, Map)
        e:     identity_merge(e, synth)
        IsOpt
    }
```

This is the **Map → Enumerable bridge**. The synthesized struct's fields ARE the StateMap's K and V (shared identity); constraints on the struct fields propagate to K and V. See [`StateMap.md`](StateMap.md) §3 and [`../TypeSystem.md`](../TypeSystem.md) §6.

### 2.5. Concretest / Abstractest

```
Concretest(CompCs{Anc, Desc, e, IsOpt}) =
    let K = Concretest_L(Desc ?? Anc)         // canonical concrete kind
    in StateCollection(K, Concretest(e))
       or StateMap(...) if K = Map
       (wrapped in StateOptional if IsOpt)

Abstractest(CompCs{Anc, Desc, e, IsOpt}) =
    let K = Anc ?? Enumerable
    in StateCollection(K, Abstractest(e))
       (wrapped if IsOpt)
```

## 3. Layer 1 — symmetric commit (MergeInplace)

`GetMergedStateOrNull(stateA, stateB)` returns the algebraic merge result without graph mutation; `MergeInplace(node_a, node_b)` commits the merge by updating node states and ancestor lists.

### 3.1. Per-class merge results

For CompCs × CompCs: returns the Unify result. The two ElementNodes are merged via MergeInplace.

For CompCs × StateCollection: returns the collapsed `StateCollection(K, merged_element)` when the interval narrows to a single K. Element nodes merged.

For CompCs × StateMap: returns `StateMap(K, V)` after pair-struct synthesis.

### 3.2. Cycle-guard contract

`MergeInplace(a, b)` may be invoked transitively on element nodes whose states already reference `a` or `b` (recursive composite types — e.g., `list<t>` where `t = list<t>`). Without a guard, the recursion would not terminate.

The contract:

```
MergeInplace(a, b):
    if a is currently being merged with anyone:    // re-entry guard
        return                                      // existing call closes the cycle
    mark a as in-progress
    perform merge: state union via GetMergedStateOrNull; ancestor-list union
    clear in-progress mark
```

**Termination**: each `MergeInplace` invocation either
- returns immediately (re-entry guard fires), or
- runs to completion and decreases the equivalence-class count by 1.

Since the number of equivalence classes is bounded by `|V|` (graph nodes), and the in-progress mark is per-node, the total number of completed `MergeInplace` calls is bounded by `|V|`. Re-entries are at most one per in-flight call. Cumulative cost is `O(|V|)`.

**Soundness**: the re-entry guard does not lose information. When `MergeInplace(a, b)` re-enters `MergeInplace(a, …)` transitively, the outer call still completes its union of constraint sets after the inner call returns. The inner call's contribution is realized as identity (already established by the outer call's mark), so no edge or constraint is dropped.

This contract is invoked by `LcaOrShareIdentity` (see [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md) §EC1 recursive-type edge case) and by every operator that calls `MergeInplace` on element nodes of cyclic composites.

## 4. Layer 2 — directional commit (Apply cells)

The Pull and Push cells in TIC's solver are documented exhaustively in [`../ApplyCells.md`](../ApplyCells.md). Each cell uses Layer-0 operators internally; the directional nature comes from the Pull/Push distinction (which side's state is updated).

Key cells for CompCs:
- **Forward Pull `Apply(CompCs anc, StateCollection desc)`**: LCA-update Desc on ancestor; element merge; no change to Anc.
- **Forward Push `Apply(CompCs anc, StateCollection desc)`**: precondition only; no state mutation.
- **Reverse Pull `Apply(StateCollection anc, CompCs desc)`**: collapse CompCs to `StateCollection(K)` if K matches anc; element merge.
- **Reverse Push `Apply(StateCollection anc, CompCs desc)`**: GCD-update Anc on descendant.
- **Map variants**: analogous, via pair-struct synthesis.

### Element propagation strategy

For element-axis propagation (the invariant element):
1. Try `MergeInplace(anc.ElementNode, sc.ElementNode)` — strict identity merge.
2. If `CanMergeStates` returns false (function-shape elements that can't unify up-front), fall back to `AddAncestor(sc.ElementNode, anc.ElementNode)` — subtyping edge.
3. In the fallback path, `PropagatePreferredAcrossFallback` restores P3 Monotonicity on the Preferred axis (see [`../Proofs.md`](../Proofs.md) §3).

## 5. Collapse to concrete state

When `Descendant = Ancestor = K` (interval reduces to a point), CompCs collapses to:

- `StateCollection(K, ElementNode)` if K is a single-arg kind.
- `StateMap(KeyNode, ValueNode)` if K = Map (requires interpreting ElementNode as a pair-struct).
- Wrapped in `StateOptional(...)` if `IsOptional` is true.

## See also

- Lattice operations: [`../TypeSystem.md`](../TypeSystem.md) §4 ConstructorLattice
- Identity-sharing for unresolved elements: [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md)
- StateMap algebra: [`StateMap.md`](StateMap.md)
- Confluence proof: [`Confluence.md`](Confluence.md)
- Apply cells in algorithmic context: [`../ApplyCells.md`](../ApplyCells.md)
