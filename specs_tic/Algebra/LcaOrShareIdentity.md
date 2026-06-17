# LcaOrShareIdentity — Identity-Sharing LCA with Side-Effect

> **Scope**: a side-effecting LCA variant for invariant composites (StateCollection, StateMap) used when pure LCA would widen to Any due to unresolved element nodes.
> Parent: [`README.md`](README.md), [`BaseOperators.md`](BaseOperators.md) §LCA.

## 1. Motivation

Pure LCA on invariant composites widens to `Any` when element nodes aren't structurally equal:

```
StateCollection(List, e₁) ∨ StateCollection(List, e₂) =
    e₁ ≡ e₂ ? StateCollection(List, e₁) : Any
```

For **nested literal scenarios** this is too aggressive. Example:

```fun
fs = list(list(10,20), list(30,40), list(50,60))
```

The three inner lists each create their own fresh element node `e_inner_i` (initially `ConstraintsState[U8..]`). The outer list's element type T must LCA all three: `list(e₁) ∨ list(e₂) ∨ list(e₃)`.

- Pure LCA: `e₁ ≠ e₂ ≠ e₃` (distinct nodes) → result `Any` → outer list element widens to Any → cascade type loss.
- Desired: recognize all three are "the same shape with unresolved elements" and **merge their identities** so future constraints land on a shared node.

`LcaOrShareIdentity` is the partial-function LCA variant with the explicit side-effect of identity-merging when pure LCA would widen.

## 2. Definition (StateCollection)

```
LcaOrShareIdentity_SC : (a: StateCollection, b: ITicNodeState) → ITypeState?

LcaOrShareIdentity_SC(a, b) =
    pure ← a.GetLastCommonAncestorOrNull(b)
    if pure ≠ null:
        return pure                                          (1)
    if b is StateCollection AND b.Constructor = a.Constructor:
        if NOT ReferenceEquals(a.ElementNode, b.ElementNode):
            MergeInplace(a.ElementNode, b.ElementNode)        ← side-effect
        return a                                              (2)
    return null                                               (3)
```

Cases:
- **(1)**: pure LCA succeeded — both sides resolved to concrete structurally-equal elements, or one side widens to Any cleanly. No side-effect.
- **(2)**: same-kind with unresolved element nodes — merge identities, return `a` (one of the two states becomes the shared identity).
- **(3)**: cross-kind or non-collection — null, caller decides.

## 3. Definition (StateMap)

Mirror of StateCollection but for two-arg StateMap:

```
LcaOrShareIdentity_Map : (a: StateMap, b: ITicNodeState) → ITypeState?

LcaOrShareIdentity_Map(a, b) =
    pure ← a.GetLastCommonAncestorOrNull(b)
    if pure ≠ null:
        return pure
    if b is StateMap:
        if NOT ReferenceEquals(a.KeyNode, b.KeyNode):
            MergeInplace(a.KeyNode, b.KeyNode)
        if NOT ReferenceEquals(a.ValueNode, b.ValueNode):
            MergeInplace(a.ValueNode, b.ValueNode)
        return a
    return null
```

Pointwise identity-share on both K and V.

## 4. Side-Effect Contract

`LcaOrShareIdentity` is **not a pure function**. Callers MUST be aware that calling it may mutate the graph (via MergeInplace on element/key/value nodes).

### Caller obligations

1. **Don't assume idempotence on the graph**: calling `LcaOrShareIdentity(a, b)` twice changes the graph only on the first call (second is no-op because identities now match).
2. **Don't compose with pure operators in mixed expressions** assuming algebraic laws hold pointwise on the graph state — only on the abstract type level.
3. **The result is one of the input states** when path (2) fires: the returned ITypeState is either `a` (the receiver) or, by symmetry of the merge, equivalent to `b` post-merge. After call, `a ≡ b` on the graph.

### Where called

- The global LCA dispatch — fires for any `StateCollection × *` and `StateMap × *` pair.

## 5. Algebraic Properties

Despite the side-effect, `LcaOrShareIdentity` satisfies key algebraic laws **modulo identity merging**:

### P1. Symmetry (modulo merge)

```
LcaOrShareIdentity(a, b) ≡ LcaOrShareIdentity(b, a)
```

After both calls, the graph state is identical: same merged identities. The returned ITypeState pointer may differ (one returns `a`, the other returns `b`), but they are reference-equal modulo MergeInplace's idempotence.

### P2. Idempotence

```
LcaOrShareIdentity(a, a) = a
```

`ReferenceEquals(a.ElementNode, a.ElementNode)` is trivially true → no merge fires. Pure LCA returns `a`. Trivially idempotent.

### P3. Convergence under iteration

For a sequence `a₁, a₂, …, aₙ` of same-kind same-element-shape states:

```
LcaOrShareIdentity(LcaOrShareIdentity(a₁, a₂), a₃) ≡
LcaOrShareIdentity(a₁, LcaOrShareIdentity(a₂, a₃))
```

Both orderings converge to the same merged-identity result. Proof sketch: MergeInplace is associative on the graph (creates union-find structures); the order of merges doesn't change the final equivalence class.

### P4. Monotonicity preservation

If `a ≤_T b` (a's element constraints are weaker than b's), and `LcaOrShareIdentity(a, b)` fires identity-merge, then post-merge:

```
a.ElementNode.State == b.ElementNode.State (literally the same state)
```

The merge propagates constraints from both sides into one node. No constraint is lost; the merge is monotone in the "information gain" sense.

## 6. Edge Cases

### EC1. Cycle in element nodes

If `a.ElementNode` and `b.ElementNode` are at μ-positions of a recursive type (e.g., `list<t> where t = list<t>`), MergeInplace's re-entry guard (`CompositeConstraints.md` §3.2 cycle-guard contract) terminates without infinite recursion. The merge folds the cycle into a single equivalence class.

### EC2. Element node already merged elsewhere

If `a.ElementNode` was previously merged with some other node X, and now we merge it with `b.ElementNode`, MergeInplace transparently handles RefTo chains. Result: a, b, and X all share one root identity.

### EC3. Cross-class with different constructors

E.g., `LcaOrShareIdentity_SC(SC(List, e₁), SC(Set, e₂))`. Constructor mismatch → falls through to path (3), returns null. Caller (StateExtensions.Lca dispatch) treats null as "fall through to Any-collapse" or "try CompCs interval LCA next". No identity merge fires.

### EC4. b is not a Collection / Map at all

E.g., `LcaOrShareIdentity_SC(SC(List, e), StatePrimitive.I32)`. Path (3), returns null.

## 7. Why "Side-Effect" is Acceptable Here

Pure-functional algebra is the ideal but TIC's graph-based model already uses MergeInplace pervasively (cycle resolution, generic substitution, F-bound contractivity repair). `LcaOrShareIdentity`'s side-effect is no novel violation — it's a deliberate exposure of MergeInplace at the algebraic layer to enable invariance preservation across unresolved-element scenarios.

The alternative — pure LCA + Any-collapse — would force programmers to write redundant type annotations on every nested literal. The trade-off (algebraic purity vs. ergonomic inference) is decided in favor of inference.

## 8. Relation to Other Operations

| Operation | Relation |
|-----------|----------|
| `GetLastCommonAncestorOrNull` (StateCollection / StateMap) | Strict structural LCA; basis for path (1). |
| `MergeInplace` | The mutating primitive that path (2) calls. |
| Global LCA dispatch | Routes StateCollection × * and StateMap × * to LcaOrShareIdentity. |
| Unify | Doesn't use LcaOrShareIdentity. Unify on CompCs has its own identity-merge mechanism. |

## 9. Known issues

None specific to LcaOrShareIdentity. The related debt #15 (`TransformTo*` reusing descendant element nodes) is a separate identity-aliasing concern handled by guard checks in Apply cells, not by this operation.
