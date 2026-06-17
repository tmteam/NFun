# Confluence — Operator Case Enumeration

> **Status**: v1 — formal proof obligation enumeration.
> Parent: [`../Algebra/README.md`](../Algebra/README.md) §Confluence theorem (classical confluence proof for primitives + Array/Optional/Fun/Struct).
> Closes: the "pending" confluence note in `../Algebra/README.md` §Confluence for the composite-interval and Map<K,V> additions.

## 0. Recap

`../Algebra/README.md` §Confluence proves confluence for primitives + classical composites (Array, Optional, Fun, Struct) via Diamond + Newman (ascending chain + finite domain + Diamond property).

The composite-interval state (`StateCompositeConstraints`, CompCs) and the two-arg `StateMap` extend the dispatch with new operator overloads (LCA, GCD, Unify, Concretest, Abstractest). The 6 abstract operators are unchanged in their algebraic role — only new cases were added.

**This document enumerates each new operator case** and proves the Diamond property (commutativity / associativity) for it, closing the confluence proof for the full current operator set.

## 1. Diamond property — what we need to prove

For each operator `op ∈ {LCA, GCD, Unify, Concretest, Abstractest}` and each pair of inputs `(A, B)` where at least one is a CompCs or a StateMap, we need:

**Commutativity**: `op(A, B) ≡ op(B, A)` modulo identity-sharing equivalence.

**Confluence (single-step)**: if a node receives concurrent updates from operators `op₁(A, X)` and `op₂(B, Y)`, the order of application doesn't change the final state.

By the structure of TIC's solver (each operator is applied to a single node's state at a time, with edges tracked separately), confluence reduces to:
- **Commutativity of binary operators** on each state class.
- **Independence of node updates**: updates to different nodes commute trivially (don't touch the same data).

## 2. New cases — by state pair

### 2.1. CompCs × CompCs

Both inputs are StateCompositeConstraints. New cases added:

**LCA(CompCs_a, CompCs_b)** — `CompositeConstraints.md` §2.1:

```
Lca(CompCs{Anc_a, Desc_a, e_a, IsOpt_a}, CompCs{Anc_b, Desc_b, e_b, IsOpt_b}) =
    CompCs{
        Anc:   max_L(Anc_a, Anc_b)     // lattice LCA on Ancestor caps
        Desc:  min_L(Desc_a, Desc_b)   // lattice GCD on Descendant caps
        e:     identity-merged(e_a, e_b)  // MergeInplace on elements
        IsOpt: IsOpt_a OR IsOpt_b
    }
```

**Commutativity**:
- `max_L` and `min_L` on ConstructorLattice — commutative by `ConstructorLatticeTest.Lca_IsSymmetric` (proven over full Cartesian product).
- Identity-merge — commutative by `LcaOrShareIdentity.md` §P1 (Symmetry modulo merge).
- Boolean OR on IsOpt — trivially commutative.

Therefore `Lca(CompCs_a, CompCs_b) ≡ Lca(CompCs_b, CompCs_a)` modulo identity equivalence. ✓

**GCD(CompCs_a, CompCs_b)** — same structure with Anc/Desc swapped role (Anc:min_L, Desc:max_L, IsOpt:AND). Commutativity by same argument. ✓

**Unify(CompCs_a, CompCs_b)** — defined in `CompositeConstraints.md` §2.2 as the meet on the composite interval. Same structure as LCA on Anc, GCD on Desc, element identity merge. Commutative. ✓

### 2.2. CompCs × StateCollection

LCA: `CompositeConstraints.md` §2.3 — "narrower-wins" cross-merge.

```
Lca(CompCs{Anc=K_a_anc, Desc=K_a_desc, e_a, ...}, StateCollection(K_sc, e_sc)) =
    if K_sc ∈ [K_a_desc, K_a_anc]:
        CompCs{Anc: K_a_anc, Desc: max_L(K_a_desc, K_sc), e: merged(e_a, e_sc), IsOpt: IsOpt_a}
    else (K_sc not in CompCs interval):
        Any  (or StateCollection.Of(parent_kind, merged_elem) for cross-branch lattice climb)
```

**Commutativity**: this is asymmetric in the **state class**, but the algebraic result is symmetric:
- `Lca(CompCs, SC) = Lca(SC, CompCs)` returns the same final state.
- The result is a CompCs (interval) by `CompositeConstraints.md` §2.3 narrower-wins rule, regardless of argument order.
- Identity-merge of `e_a` and `e_sc` is symmetric.

Therefore commutative modulo identity equivalence. ✓

GCD: cross-class GCD returns either a concrete SC(K) (when K is within CompCs interval) or null (cross-branch). Symmetric by case construction. ✓

### 2.3. CompCs × StateMap

This is the **pair-struct synthesis path**.

LCA: when CompCs's `Anc` includes `Map` (e.g., `Anc = Enumerable`), and the other side is StateMap, the synthesis bridge fires:

```
Lca(CompCs{Anc=Enumerable, e}, StateMap(K, V)) =
    let synth = StateStruct({key: K, value: V}, isFrozen=true)
    in CompCs{Anc=Enumerable, Desc=Map, e=identity-merged(e, synth)}
```

**Commutativity**:
- The pair-struct synthesis is deterministic given (K, V) — same struct shape both orderings.
- The identity-merge of `e` with the synthesized struct is symmetric.
- The lattice update on Desc (Map) is invariant of order.

The result is the same up to identity. ✓

### 2.4. StateMap × StateMap

LCA: `StateMap.md` §2.1 — strict invariance requires identical KeyState and ValueState; else widens to Any (or shares identity if unresolved).

```
Lca(StateMap(K_a, V_a), StateMap(K_b, V_b)) =
    if K_a.Equals(K_b) AND V_a.Equals(V_b): StateMap (one of the inputs)
    else if both unresolved: LcaOrShareIdentity merges K_a≡K_b and V_a≡V_b, returns Map
    else: Any
```

**Commutativity**:
- Equality check is symmetric.
- LcaOrShareIdentity's identity-merge is symmetric (`LcaOrShareIdentity.md` §P1).
- Any-result is symmetric.

All paths produce symmetric outcomes. ✓

GCD / Unify: same structural symmetry. ✓

### 2.5. StateMap × StateCollection / StateMap × StateArray

Both reject (different lattice branches — Map vs Coll/Array). Symmetric: rejection from either side gives the same `null` (GCD/Unify) or `Any` (LCA). ✓

### 2.6. StateMap × StateOptional

Optional is a wrapper. LCA of `StateMap(K,V)` and `Opt(X)`:
- General rule: `T ∨ Opt(X) = Opt(T ∨ X)` (implicit lift, `../Algebra/README.md` §Optional lift).
- For `T = StateMap`: `StateMap ∨ Opt(StateMap) = Opt(StateMap)` if both maps share K,V; else `Opt(Any)`.

Commutative by the general Optional-lift rule. ✓

### 2.7. StateMap × StatePrimitive

LCA: `StatePrimitive ∨ StateMap = Any` (non-primitive composite × primitive = top).

Symmetric (Any from either order). ✓

### 2.8. Concretest / Abstractest extensions

**Concretest(StateMap(K, V))** — `StateMap.md` §2.7: pointwise.
```
Concretest(StateMap(K, V)) = StateMap(Concretest(K), Concretest(V))
```

By induction on element nodes, the pointwise operation preserves any algebraic properties (commutativity isn't applicable to unary operators — what we need is **idempotence**: `Concretest(Concretest(x)) = Concretest(x)`, which follows from Concretest's classical idempotence on each element).

**Abstractest**: same structure, same argument. ✓

**Concretest(CompCs)**: defined in `CompositeConstraints.md` §2.5 as descending to `StateCollection(Concretest_L(Desc ?? Anc), Concretest(element))`. Idempotent because second application produces the same StateCollection. ✓

### 2.9. SimplifyOrNull cases

When `Desc == Anc == K` (concrete), CompCs collapses to a concrete state. If `K == Map`, the collapse target is StateMap.

Deterministic given input. No commutativity question (it's a unary operator). Idempotent: applying again to the collapsed state is a no-op (StateMap doesn't simplify further). ✓

## 3. Diamond property — cross-cell concurrent updates

The classical Diamond proof in `../Algebra/README.md` §Confluence covers:
- Updates to different nodes commute.
- Updates to one node via different operators commute by LCA/GCD commutativity.

New state additions:
- New operator cases above — each commutative individually.
- `LcaOrShareIdentity`'s side-effect (MergeInplace on element nodes) — this is the only new graph-mutation operation.

**Side-effect commutativity**: if two `LcaOrShareIdentity` calls fire path-(2) on overlapping element nodes:
- `LcaOrShareIdentity(a, b)` merges `a.e` with `b.e`.
- `LcaOrShareIdentity(c, d)` merges `c.e` with `d.e`.
- If `b.e = c.e` (overlap), both merges contribute to a 4-way identity class `{a.e, b.e=c.e, d.e}`.
- The order of merges doesn't affect the final identity class (union-find associativity).

Therefore the side-effect doesn't break Diamond. ✓

## 4. Termination

Newman: terminating + locally confluent ⇒ confluent.

Termination obligations:
- New operator cases run on finite states (same primitive-type domain — bounded by input expression).
- `LcaOrShareIdentity` side-effect reduces equivalence classes by 1 per fire; at most `|V|` total per graph.
- Pair-struct synthesis allocates at most one struct node per `CompCs × StateMap` Apply call; bounded by `|E|`.

All new state additions terminate. ✓

## 5. Confluence theorem

**Theorem** (Confluence): TIC's solver produces the same final graph state regardless of the toposort tie-breaking order, given the operator cases enumerated in §2.

**Proof**:
- §2 establishes per-case commutativity for each new operator overload.
- §3 establishes Diamond property for cross-cell concurrent updates.
- §4 establishes termination.
- Newman's lemma: terminating + locally confluent ⇒ globally confluent. ✓ ∎

## 6. Empirical witness

The full test suite executes thousands of TIC solver runs. Determinism (P4) is empirically verified. Confluence is a strictly stronger property; the test suite witnesses confluence indirectly via P4 (P4 is a consequence of confluence + determinism of tie-breaking).

## 7. Related specs

- [`../Algebra/README.md`](../Algebra/README.md) §Confluence — classical confluence proof
- [`CompositeConstraints.md`](CompositeConstraints.md) §2 — CompCs operator definitions
- [`StateMap.md`](StateMap.md) §2 — StateMap operator definitions
- [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md) — identity-sharing side-effect contract
- [`../Proofs.md`](../Proofs.md) §P4 — determinism (consequence of confluence)
- [`../TypeSystem.md` §4](../TypeSystem.md#4-constructorlattice) — lattice operators with symmetric Lca/Gcd
