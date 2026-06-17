# Algebra — Overview

TIC algebra is a closed system of **six operators** over the type domain ([`../TypeSystem.md`](../TypeSystem.md)):

| Operator | Symbol | Arity | Totality | Question |
|---|---|---|---|---|
| LCA | `A ∨ B` | binary | total | What type covers both? |
| GCD | `A ∧ B` | binary | partial (null) | What type fits inside both? |
| Fit | `A ≤ B` | binary | bool | Does A fit in B? |
| Unify | `A ⊓ B` | binary | partial | Intersect two constraint intervals. |
| Concretest | `↓A` | unary | total | Lower bound. |
| Abstractest | `↑A` | unary | total | Upper bound. |

## Fundamental relations

```
A ≤ B   ⟺   A ∨ B = B   ⟺   A ∧ B = A    (when GCD defined)
```

If `A ∧ B = null`, A and B are **incomparable** (neither A ≤ B nor B ≤ A).

## Algebraic structure

Types under ≤ form a **join-semilattice with top** (Any):
- Every pair has a join (LCA is total).
- Any is the greatest element.
- **Not a lattice**: meet (GCD) is partial — e.g., `Bool ∧ I32 = null`.
- **No global bottom**: None is bottom only within the Optional subsystem, not for all types.

## Unify reduces to LCA + GCD

For ConstraintsState intervals:

```
[D₁..A₁] ⊓ [D₂..A₂] = [D₁ ∨ D₂  ..  A₁ ∧ A₂]
```

The interval is non-empty iff `D₁ ∨ D₂ ≤ A₁ ∧ A₂`.

## Per-operator details

| File | Content |
|---|---|
| [`BaseOperators.md`](BaseOperators.md) | LCA, GCD, Fit, Unify, Concretest, Abstractest — rules + tables |
| [`CompositeConstraints.md`](CompositeConstraints.md) | StateCompositeConstraints algebra |
| [`StateMap.md`](StateMap.md) | StateMap (two-arg invariant Map<K,V>) algebra |
| [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md) | Side-effecting LCA for invariant composites with unresolved elements |
| [`Confluence.md`](Confluence.md) | Confluence proof (per-case Diamond enumeration) |

## Theorems (proven in [`../Proofs.md`](../Proofs.md))

- **Confluence**: solution result is order-independent (Newman: terminating + locally confluent).
- **Soundness**: if a graph is solved, no type errors at runtime.
- **Termination**: monotone narrowing on a finite domain.
- **Determinism**: same input expression ⇒ same final types.
