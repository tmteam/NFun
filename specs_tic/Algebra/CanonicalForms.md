# Canonical Forms and the Snapshot/Identity Discipline

> **Status**: normative since 2026-07-03 (commits `c5593f69`, `7eeabc65`).
> **Origin**: the dead-invisible-snapshot family — `if(true) [[1]] else [[none]]` → FU710,
> `[1,2,3].map(rule if(it>1) [it] else [none])` → FU710, `OptionalArrayFieldMapSum` → FU719 —
> all shared one root that no local Lca/Push patch could fix (flat and nested cases
> ping-ponged under every attempt). Professor analysis 2026-07-03.

## 1. The problem class

TIC states carry two kinds of information:

- **Structure** — constructors: `arr(·)`, `SC(K, ·)`, `opt(·)`, `{fields}`, `(args)->ret`.
- **Constraints** — unresolved intervals `[D..A]` with axis FLAGS (`IsOptional`,
  `IsComparable`, `IsClearable`, `StructBound`) and hints (`Preferred`).

A **snapshot** is any state stored away from its live nodes: a `ConstraintsState.Descendant`,
an ancestor cap, a Concretest result. The failure class arises when a snapshot **changes the
representation** of constraint information mid-solve — most destructively, when a *flag* on an
unresolved interval is materialized as a *constructor* around a fresh unsolved copy:

```
Concretest( CS[..]? )  =  opt( fresh-empty-CS )     // the pre-Rule-B behavior
```

The fresh copy is a **dead island**: no edge reaches it, nothing ever refines it, and the two
representations of the same Optional axis (flag vs constructor) no longer join through the
CS×CS interval arm. Every downstream stage then needs a special case to reconcile them — and
each special case is a workaround by the project's own definition.

This is not specific to Optional. **Any axis flag** (`IsClearable` today; any future flag)
hits the same schism if an operation materializes it structurally around an unsolved bound.

## 2. Rule B — canonical Optional form

> **In any stored state, `opt(τ)` implies τ is solved. The Optional lift of an unsolved
> constraint `[D..A]` is the flag form `[D..A]?`. Every operation — join, snapshot,
> transfer — preserves this canonical form.**

Consequences enforced in code:

| Operation | Canonical behavior | Where |
|---|---|---|
| `Concretest(CS[D..A]?)`, inner unsolved | returns flag-form CS (interval + Preferred kept), NOT `opt(fresh-CS)` | `StateExtensions.Concretest.cs` |
| `Concretest(CS[g..]?)`, ground g | `opt(g)` — materialization of solved states is unchanged | same |
| join `x ∨ CS` (bc2 arm) | `opt(P) ∨ CS[D..A](Pref) = CS[P..A]?(Pref)` — stays interval while a hint is alive; the flat rule is the ground instance, nested reduces to it by structural induction | `StateExtensions.Lca.cs` |
| Pull into inner-of-Optional | the wrapper consumes the Optional factor of incoming bounds: an `opt(X)` bound on an `IsOptionalElement` node (or on the inner created by CS→opt wrap) enters as `X`. `opt(opt(X))` is non-canonical and dies in interval checks (`[opt(I32)..Re]`) | `PullConstraintsFunctions.cs` (CS×CS unwrap, WrapOptionalInner) |
| Push `None ≤ CS?` | always valid — None is the bottom of the Optional axis and does not participate in the interval. ONE general cell rule (`Apply(CS, StatePrimitive)`), not per-call-site skips | `PushConstraintsFunctions.cs` |
| `ToString` of any state | delegates to depth-guarded `PrintState` — naive interpolation recursed forever on μ-recursive states | all `SolvingStates/*` |

The flag→constructor conversion happens exactly ONCE, in `MaterializeOptionalFlags` /
Destruction / Finalize — after constraint collection, per the original design intent
("StateOptional is materialized before Destruction, after constraint collection").

## 3. R1 — monotone refinable lower bounds

> **A stored descendant bound over a state with unresolved members must remain a function of
> those members: it may contain ground types, or identity-shared live nodes — it must never
> EVALUATE an unresolved member.**

`Concretest` is a *valuation* (evaluates every member at the bottom of its interval,
fabricates fresh nodes). Using a valuation as a *storage form* freezes `D(⊥)` and severs
refinement. Rule B is the Optional-axis instance of R1; the general split is:

- **Valuation use** (fit checks, ancestor comparisons): Concretest is fine — the result is
  consumed immediately and never stored.
- **Storage use** (`AddDescendant`, Lca results that become `Descendant`): must be
  canonical-form-preserving. Unsolved members stay interval-form; flags stay flags.

The full storage/valuation split of Concretest into two entry points is FUTURE work — today
Rule B covers the axis that was bleeding. Treat any new
`Concretest`-result-stored-as-Descendant path as suspect.

## 4. Join purity

> **`Lca` is a pure lattice join: no operand mutation, no live-node aliasing into results.**

History: the Bug#6-era identity-share branches (`StateArrayLcaOrShareIdentity`,
`LcaStructFields` None+CS field reuse) mutated an operand (`AddDescendant(None)` on a live
node) and aliased that node into the join result. They existed only because pre-Rule-B
snapshots died with their flags; under Rule B the pure element-Lca carries
`IsOptional`/`Preferred` itself, and the branches were REMOVED (commit `7eeabc65`).
`Lca` is called from speculative probes (`CanMergeStates` → `MergeOrNull`) — an impure join
leaves permanent graph mutations on failed probes.

Known remaining exception: `StateCollection.LcaOrShareIdentity` (documented in
`LcaOrShareIdentity.md`) — its side effects follow the debt-#17 discipline (fresh node in the
RESULT, `AddDescendant` on the live side for narrowing). If a future axis flag needs the same
treatment, use that discipline, not raw node aliasing.

## 5. Resolution short-circuits must cover the join

> **`anc := ref(descendant)` at Destruction is legal only when the descendant's state covers
> the accumulated join (`anc.Descendant`). A short-circuit that equates the result with ONE
> contributor silently discards what the other contributors added.**

Instance: `if-else` with a none-branch — join element `[U8..]?I32!`, then-branch element
`[U8..]I32!`. `y := ref(then)` erased the `?`. Gate: `JoinCarriesOptionalBeyond`
(`DestructionFunctions.cs`) — a coinductive reference-identity walk (visited-pair set, NOT a
depth guard: μ-recursive trees with branching make depth-only guards exponential — the
LeetCode binary-tree suite hung 30 s/test on that mistake).

## 6. Coinduction scope = derivation scope

> **The assumption set of a coinductive μ-subtyping derivation must live as long as the
> derivation.** Under streaming Pull a derivation is one `Invoke` (the in-stack visited-pair
> guard in `StagesExtension` suffices). Under worklist Pull the decomposition of a μ-knot is
> spread across drains — the assumption set is the run-scoped discharged-pair memo
> (`WorklistPullDriver.MarkDischarged`/`IsDischarged`). See
> `Advanced/WorklistPull.md` §Post-closure hardening.

Corollary (worklist termination): the edge set must be monotone modulo discharged pairs —
re-emitting a consumed composite edge is coinductively redundant and, on μ-knots, the source
of non-terminating add/remove oscillation.

## 7. Checklist for adding a new axis flag or composite state

1. The flag lifts an unsolved interval? → flag form only; materialize once, late (Rule B).
2. Any operation stores a state containing your flag? → verify it stores the flag, not a
   constructor around a fresh copy (R1).
3. Lca/Gcd arms: flag joins as `∨`/`∧` on the interval arm; hints survive one-sided.
4. Push: define what descends. Axis flags usually do NOT descend into non-flagged
   branches (implicit lift `T ≤ opt(T)`-style: conversion happens at the value boundary).
5. Destruction short-circuits: does `ref(desc)` cover your axis? Extend the covers-join gate.
6. Printing: `ToString => PrintState(0)`; add a depth/visited guard if you add recursion.
7. μ-types: every new traversal needs a coinductive guard keyed by reference identity.

## 8. Related

- `TicPreferred.md` P3 — Preferred is metadata; joins keep it while representable.
- `Advanced/WorklistPull.md` — worklist hardening (EnqueueMark, discharged pairs).
- `TechnicalDebt.md` §Closed — the closure history of this family.
- `Algebra/LcaOrShareIdentity.md` — the sanctioned identity-share discipline (debt #17).
