# Algebra of StateMap — Two-Arg Invariant Map<K,V>

> **Status**: v1 — Map<K,V>.
> Parent: [`../Algebra/README.md`](../Algebra/README.md), [`../TypeSystem.md` §4](../TypeSystem.md#4-constructorlattice), [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md).

## 1. Domain

```
StateMap { KeyNode: TicNode, ValueNode: TicNode, Constructor = Map }
```

- **Shape**: two TicNode arguments (K + V). Mandatory both non-null.
- **Variance**: both invariant (uniform invariance).
- **Lattice position**: `Map ≤_L Enumerable ≤_L Any`. Sibling of Set, FixedArray-branch (no cross-edges; cross-kind LCA collapses to Enumerable via lattice climb).

The two-arg shape is what distinguishes StateMap from StateCollection. All other invariants apply.

## 2. Algebraic Operations

### 2.1 GetLastCommonAncestorOrNull (pure LCA, returns null on unresolved)

```
Map.GetLastCommonAncestorOrNull(b) =
    if b is not StateMap: return Any
    if (KeyState, ValueState, b.KeyState, b.ValueState) any is not ITypeState:
        return null                                   ← unresolved
    if KeyState.Equals(b.KeyState) AND ValueState.Equals(b.ValueState):
        return this                                   ← structural equality
    return Any                                        ← different K or V
```

**Invariance**: any difference in K or V (NOT subtyping — strict structural equality) widens to Any. No covariance/contravariance.

### 2.2 LcaOrShareIdentity (with side-effect)

Defined in [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md) §3. Briefly:

- Try `GetLastCommonAncestorOrNull` first.
- If null AND `b` is StateMap: MergeInplace KeyNode and ValueNode separately, return `this`.
- Otherwise null.

Two MergeInplaces in path (2) — one for K, one for V. They are independent (one may merge while the other already matches).

### 2.3 LCA dispatch

In the global LCA switch, the Map case routes through LcaOrShareIdentity:

```
case StateMap amap: return amap.LcaOrShareIdentity(b) ?? StatePrimitive.Any;
```

Falls back to Any on null. Mirrors the `StateCollection acoll => acoll.LcaOrShareIdentity(b) ?? Any` case.

### 2.4 GCD

`StateMap × StateMap` GCD:

```
Gcd(Map(k₁, v₁), Map(k₂, v₂)) =
    if k₁ ≢ k₂ OR v₁ ≢ v₂: null         ← invariance: no narrowing
    return Map(k₁, v₁)
```

Cross-kind (`Map × StateCollection`, `Map × StateArray`): null — Map is on a different lattice branch (sibling of Set/FixedArray under Enumerable, no GCD descendants in common).

### 2.5 Unify

`StateMap × StateMap` Unify: defined as the meet in the invariance lattice — same rule as GCD. If both args structurally equal, returns this; else null.

### 2.6 Fit (≤)

```
Map(k₁, v₁) ≤ Map(k₂, v₂)  iff  k₁ ≡ k₂ AND v₁ ≡ v₂        (invariance)
Map(k, v) ≤ Any                                                (top)
Map(k, v) ≤ Enumerable<pair>  iff  pair ≡ {key:k, value:v}    (via pair-struct synthesis, see §3)
```

Map ≤ Set / List / FixedArray / Array — all reject (different lattice branches).

### 2.7 Concretest / Abstractest

- `Concretest(Map(k, v))` = `Map(Concretest(k), Concretest(v))` — pointwise.
- `Abstractest(Map(k, v))` = `Map(Abstractest(k), Abstractest(v))` — pointwise.

(Pointwise per-arg because both args are invariant in the lattice — concretest/abstractest applies independently.)

## 3. Map → Enumerable Bridge (Pair-Struct Synthesis)

Map satisfies `Enumerable<{key:K, value:V}>` via a synthesized **frozen** pair-struct. The synthesis is documented in [`../TypeSystem.md`](../TypeSystem.md) §7 and realized operationally in the `CompCs × Map` Apply cells:

```
synth = StateStruct(
    fields = { "key" → sm.KeyNode, "value" → sm.ValueNode },
    isFrozen = true
)
MergeInplace(ancestor.ElementNode, structNode)
```

### Algebraic justification

The bridge is sound because:
- The synthesized struct's fields are **identity-shared** with StateMap's KeyNode and ValueNode (not fresh copies).
- Width subtyping on struct: `{key, value} ≤_struct {key, value, ...rest...}` — Enumerable's element constraint is satisfied by any struct with at least these fields.
- `isFrozen = true` prevents width-propagation onto the synthesized struct: nobody can add fields to Map's pair-shape mid-flight.

### What `Map(k, v) ≤ Enumerable<pair>` means

Formally:
```
StateMap(k, v) satisfies CompCs{Anc = Enumerable, ElementNode = e}
    ⟺ MergeInplace(e, synth(k, v)) succeeds
```

The merge succeeds when:
- `e`'s state is ConstraintsState (unresolved): trivially absorbs the struct shape.
- `e`'s state is StateStruct with compatible fields: width subtyping allows merging.
- `e`'s state is anything else: reject (Map cannot satisfy a non-struct element constraint).

## 4. Cross-Kind Reject Rules

| Map × X | Result | Reason |
|---------|--------|--------|
| Map × StateCollection(List/Array/FixedArray/Set) | reject (Any in LCA, null in GCD/Unify) | different lattice branches |
| Map × StateArray (ee-mode legacy) | reject | StateArray is single-arg, Map is two-arg |
| Map × StateFun | reject | different domain |
| Map × StateStruct (non-pair) | reject EXCEPT via pair-struct synthesis | only the synth bridge allows it |
| Map × StateOptional | reject (cross), accept via wrapping | Optional(Map(k,v)) is a separate state |
| Map × CompCs | handled via `ForwardPullCompCsStateMap` cell | the bridge |
| Map × Map | per LcaOrShareIdentity / GetLastCommonAncestorOrNull | structural equality required |

## 5. Properties

### P1. Closure

`StateMap × StateMap` operations close within {StateMap, Any}:
- LCA: StateMap (if equal) or Any (if different).
- GCD/Unify: StateMap (if equal) or null.

### P2. Variance laws

Both args strictly invariant. No covariance or contravariance in K or V.

```
Map(I32, Bool) ∨ Map(Real, Bool) = Any   ← not Map(Real, Bool) despite I32 ≤ Real
```

This is the **uniform invariance** discipline. It simplifies algebra: no per-arg variance bookkeeping, and node identity is the only path for elements to "be the same."

### P3. Identity preservation under merge

If `LcaOrShareIdentity(m₁, m₂)` fires path (2):
- `m₁.KeyNode` and `m₂.KeyNode` become reference-equal post-merge.
- Same for ValueNode.
- All future constraints on either KeyNode flow into one merged identity.
- Same for ValueNode.

This is the algebraic precondition for invariance to be tractable: once merged, "same K" is decidable by reference equality.

### P4. Pair-struct synthesis is determinate

For a given `StateMap(k, v)` with a given CompCs ancestor, the synthesized struct is uniquely determined:
- Fields: `{"key" → k, "value" → v}` (fixed names)
- isFrozen: always true
- Same StateMap → same synthesis result (modulo MergeInplace idempotence)

No nondeterminism in the bridge.

## 6. Open questions

- **Map × Optional wrapping**: `LCA(Map(I32, Bool), opt(Map(I32, Bool)))` is handled by the general Optional lift rule — `T ∨ opt(T) = opt(T)`. No Map-specific handling needed.
- **Pair-struct synthesis under Optional**: if Map is wrapped in Optional and the synth bridge fires, the result is `Optional(Enumerable<pair>)` via the standard cross-Apply Optional-wrap.

## 7. Related specs

- [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md) — the side-effecting LCA primitive
- [`CompositeConstraints.md`](CompositeConstraints.md) — CompCs algebra, including Map ↔ Enumerable cells
- [`../TypeSystem.md` §4](../TypeSystem.md#4-constructorlattice) — Map's lattice position
- [`../TypeSystem.md`](../TypeSystem.md) — overall context for the type system
