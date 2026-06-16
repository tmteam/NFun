# Preferred — Metadata propagation

> **What this is**: an optional metadata hint attached to ConstraintsState nodes that biases the Destruction-time type resolution toward a preferred concrete type, without constraining the set of acceptable types.
>
> **Status**: production component. Known limitation (single global Preferred) tracked as debt #7 in [`../TechnicalDebt.md`](../TechnicalDebt.md).

## 1. What Preferred is

`Preferred` is a `StatePrimitive?` field on `ConstraintsState`. It carries **provenance information**: where did this constraint interval come from, and what concrete type would be most natural?

**Key property**: Preferred does not change the **set of acceptable types** for the constraint — it only changes **which type is picked** at resolution time when multiple are valid.

A CS interval `[U24..Real]` can have any of `Preferred = I32`, `Preferred = Real`, or `Preferred = null`. All three represent the same set of solutions, but resolve differently:
- `Preferred = I32` → picks `I32`.
- `Preferred = Real` → picks `Real`.
- `Preferred = null` → picks based on default resolution rules (Concretest for covariant position).

## 2. Sources of Preferred

Preferred is set at Build time only. Three sources:

### 2.1. Integer literal constants

Integer literals (`1`, `42`, `0`) create a CS node with:
- `Descendant = U8` (smallest representable type)
- `Ancestor = Real` (largest in numeric hierarchy)
- `Preferred = I32` (or per dialect's `IntegerPreferredType` setting)

This makes integer literals resolve to `I32` by default (unless wider use forces upcast).

### 2.2. Hex / binary literal constants

Hex (`0xFF`) and binary (`0b1010`) literals use:
- `Descendant = U8`, `Ancestor = I96` (not Real — hex/bin imply bit-level use)
- `Preferred = I32` (or dialect setting)

Different `Ancestor` than decimal literals because hex/bin are typically used for bitwise operations, which don't extend to Real.

### 2.3. Real literal constants

`Real` literals (`1.5`, `3.14e10`) produce `StatePrimitive.Real` directly — no CS, no Preferred needed.

## 3. Propagation mechanisms

### 3.1. Bidirectional copy in CS × CS Pull (CRITICAL — P3 Monotonicity rule)

When `Apply(CS ancestor, CS descendant)` fires in Pull:

```
if ancestor.Preferred == null AND descendant.Preferred != null:
    ancestor.Preferred = descendant.Preferred           // upward propagation
if descendant.Preferred == null AND ancestor.Preferred != null:
    descendant.Preferred = ancestor.Preferred           // downward propagation
```

This is the **P3 Monotonicity rule** ([`../Proofs.md`](../Proofs.md) §3): Preferred information is preserved bidirectionally across constraint edges and never lost.

**Upward** (descendant → ancestor): integer literals propagate their `I32` preference up to generic function-parameter slots.

**Downward** (ancestor → descendant): a field-access chain `s.m.n` propagates the field's Preferred back through the chain.

### 3.2. Generic argument propagation in function calls

When a generic function `f(arg: T) -> T` is called with a concrete-Preferred argument, the function's generic slot receives the Preferred:

```
if argCs.Preferred != null AND genericSlot.Preferred == null:
    genericSlot.Preferred = argCs.Preferred
```

Example: `sum([1, 2, 3])` — the generic `T` of `sum: T[] → T` gets `Preferred = I32` from the array element type.

### 3.3. PropagatePreferred phase

After Pull and Push complete, a global broadcast pass:
- For each CS node with `Preferred = null`, find a connected node with a compatible Preferred and copy it.

This handles edge cases where Pull/Push didn't traverse a particular path.

### 3.4. LCA preservation

`Lca` on two CS states preserves Preferred:

```
result.Preferred = a.Preferred ?? b.Preferred
```

(First non-null wins. If both have Preferred, the LHS wins — order-dependent in this one case; debt #7.)

## 4. Resolution at Destruction

`SolveCovariant` (covariant positions, e.g., function return types):
1. If `Preferred` is set AND compatible with the constraint interval: pick `Preferred`.
2. Else if `Descendant` is set: pick `Descendant`.
3. Else: pick type-class default (`Char` for Comparable, etc.) or `Any`.

`SolveContravariant` (contravariant positions, e.g., function arg types):
1. If `Preferred` is set AND compatible: pick `Preferred`.
2. Else pick `Ancestor` (or `Any`).

## 5. Invariants

| # | Invariant |
|---|---|
| P1 | Preferred is metadata, not a constraint. Two CS intervals `[U8..Real]` with different Preferred have the same set of acceptable solutions. |
| P2 | Preferred never widens a constraint. If Preferred is incompatible with `[D..A]`, it is dropped at resolution. |
| P3 | **Monotonicity**: Preferred is preserved bidirectionally during Pull (§3.1). Loss across the AddAncestor fallback in CompCs cross-Apply is the known violation (debt #16), closed by `PropagatePreferredAcrossFallback`. |
| P4 | LCA-preserved: `Lca(a, b).Preferred = a.Preferred ?? b.Preferred`. |
| P5 | At most one Preferred per CS at any time. Mixed Preferred sources collapse on first encounter (debt #7 — order-dependent). |
| P6 | Resolution uses Preferred only when compatible. Incompatible Preferred is dropped. |

## 6. Known limitations

### Debt #7 — Single global Preferred

PropagatePreferred uses a **single global broadcast pass** that collects the first encountered Preferred and propagates it widely. In mixed scenarios (e.g., `hex_lit | int_lit`), the order matters: hex's `U16` Preferred wins over int's `I32` depending on toposort order.

**Path to fix**: edge-local PropagatePreferred (each node receives Preferred only from its directly-connected descendants, not transitively from disconnected sources). Tracked as debt #7.

### Debt #16 — Preferred loss across CompCs fallback (closed)

The `Apply(CompCs, *)` cells previously did not propagate Preferred across the AddAncestor fallback edge. Closed via the `PropagatePreferredAcrossFallback` helper. See [`../Proofs.md`](../Proofs.md) §3.5.

## See also

- Algorithm overview: [`../Algorithm.md`](../Algorithm.md) §5 PropagatePreferred phase
- P3 Monotonicity proof: [`../Proofs.md`](../Proofs.md) §3
- Debt #16 (closed Preferred axis): [`../TechnicalDebt.md`](../TechnicalDebt.md)
