# TIC — Type Inference Constraint Solver

> **Audience**: anyone who needs to understand, extend, or fix NFun's type inference.
>
> **Status**: production-ready specification. Formal proofs in [`Proofs.md`](Proofs.md).

## What TIC solves

NFun is an embeddable expression language with **rich static type inference**. The user writes:

```fun
fun mean(xs): return xs.sum() / xs.count()
out = mean([1, 2, 3])
```

— and TIC infers that `xs : Real[]`, `mean : Real[] → Real`, `out : Real`, without any explicit annotations. The user wrote no types; TIC derived them all.

**The problem**: given an expression's syntax tree, derive the most specific type for every sub-expression that is consistent with all of its uses. "Most specific" means: don't widen unnecessarily. "Consistent" means: respect implicit numeric widening (`I32 ≤ I64`), optional lifting (`T ≤ Opt(T)`), structural subtyping (more fields = subtype), and function variance (covariant return, contravariant args).

**The approach**: build a **constraint graph** from the AST (Phase: Build), then iteratively propagate constraints UP from descendants to ancestors (Phase: Pull) and DOWN from ancestors to descendants (Phase: Push) until each node has a precise interval `[lower_bound..upper_bound]`. Finally, pick a concrete type from each interval (Phase: Destruction + Finalize).

This document set is the formal specification of that algorithm.

## How to read this corpus

If you are…

| You want to… | Start with |
|---|---|
| **Understand what TIC does conceptually** | This README + [`TypeSystem.md`](TypeSystem.md) §Overview |
| **See the types and operators** | [`TypeSystem.md`](TypeSystem.md), [`Algebra/`](Algebra/) |
| **Understand the algorithm** | [`Algorithm.md`](Algorithm.md), [`ApplyCells.md`](ApplyCells.md) |
| **Trust the algorithm (formal proofs)** | [`Proofs.md`](Proofs.md) |
| **Know what's broken or unfinished** | [`TechnicalDebt.md`](TechnicalDebt.md) |
| **Investigate specific subsystems** | [`Advanced/`](Advanced/) |

## Corpus index

### Core (must-read for fluency)

| File | Topic | Lines |
|---|---|---|
| [`README.md`](README.md) | **This file** — intro, problem statement, index | ~100 |
| [`TypeSystem.md`](TypeSystem.md) | All types (primitives, composites, intervals) + lattice | ~400 |
| [`Graph.md`](Graph.md) | Constraint graph structure (nodes, states, edges) | ~150 |
| [`Algorithm.md`](Algorithm.md) | Build → Pull → Push → Destruction → Finalize | ~400 |
| [`ApplyCells.md`](ApplyCells.md) | Canonical Apply-cell truth tables (reference) | ~320 |
| [`Proofs.md`](Proofs.md) | P1 Soundness, P2 Termination, P3 Monotonicity, P4 Determinism, P5 Identity-sharing, P6 Push Monotonicity | ~750 |

### Algebra (operator definitions and properties)

| File | Topic |
|---|---|
| [`Algebra/README.md`](Algebra/README.md) | Algebra overview: 6 operators, fundamental relations |
| [`Algebra/BaseOperators.md`](Algebra/BaseOperators.md) | LCA, GCD, Fit, Unify, Concretest, Abstractest — per-operator rules |
| [`Algebra/CompositeConstraints.md`](Algebra/CompositeConstraints.md) | StateCompositeConstraints algebra |
| [`Algebra/StateMap.md`](Algebra/StateMap.md) | Two-arg invariant Map<K,V> algebra |
| [`Algebra/LcaOrShareIdentity.md`](Algebra/LcaOrShareIdentity.md) | Identity-sharing LCA with side-effect contract |
| [`Algebra/CanonicalForms.md`](Algebra/CanonicalForms.md) | Canonical forms + snapshot/identity discipline (Rule B, R1, join purity, coinduction scope) |
| [`Algebra/Confluence.md`](Algebra/Confluence.md) | Confluence proof (per-case Diamond enumeration) |

### Reference

| File | Topic |
|---|---|
| [`TechnicalDebt.md`](TechnicalDebt.md) | Known limitations and open issues |

### Advanced topics (niche, read on demand)

| File | Topic |
|---|---|
| [`Advanced/Preferred.md`](Advanced/Preferred.md) | Preferred metadata propagation |
| [`Advanced/PushReform.md`](Advanced/PushReform.md) | F-bounded polymorphism, iso-recursive types |
| [`Advanced/SimplePath.md`](Advanced/SimplePath.md) | SimplePrimitiveSolver perf optimization |
| [`Advanced/WorklistPull.md`](Advanced/WorklistPull.md) | Worklist Pull architecture (closes P3b) |

### Test baselines

| Folder | Purpose |
|---|---|
| [`baselines/`](baselines/) | JSON snapshots for differential testing |

## What's intentionally NOT here

- **Tutorial-style introductions for newcomers to type inference**. This is a specification, not a textbook. For background, see Pierce *Types and Programming Languages*, Damas-Milner '82, or Cardelli-Mitchell '89.
- **Implementation details** that don't bear on correctness. Pointer-level data layouts, allocation strategies, performance microoptimizations live in code comments.
- **Historical design discussions** ("why we tried X and rejected it"). Decisions live in git history. Specs describe the current design only.

## Conventions

- All examples are in NFun syntax unless otherwise marked.
- TIC state names use the prefix `State*` (e.g., `StateArray`, `StateOptional`) or the abbreviation `CS` (`ConstraintsState`) / `CompCs` (`StateCompositeConstraints`).
- Operator symbols: `∨` join (LCA), `∧` meet (GCD), `≤` subtype (Fit), `⊓` Unify, `↓` Concretest, `↑` Abstractest.
