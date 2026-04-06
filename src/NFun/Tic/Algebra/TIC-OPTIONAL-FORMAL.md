# TIC Optional Formal Model — Synthesis

## 1. Type System

Bounded join-semilattice (T, ≤) with top=Any, no universal bottom. Partial meet (GCD can be null).

**None** is sui generis: bottom of optional sublattice `{None} ∪ {opt(T)} ∪ {Any}`, NOT bottom of full lattice. `None ≤ opt(T)` for all T. `None ≤ Any`. None is NOT ≤ any other primitive.

**Critical**: `LCA(None, T) = opt(T)` escapes primitive sublattice — by design.

## 2. ConstraintsState Algebra

Product of three orthogonal dimensions:
```
C = IntervalAlgebra × OptionalityAlgebra × ComparabilityAlgebra
C = (D: ITicNodeState?, A: StatePrimitive?, opt: bool, cmp: bool)
```

- **Interval** [D..A]: D = lower bound (any type), A = upper bound (primitive only)
- **Optionality** opt: bool with OR-join semantics
- **Comparability** cmp: bool with OR-join semantics

Operations:
- AddDescendant(T): D' = LCA(D, T), except None → opt := true
- AddAncestor(P): A' = GCD(A, P)
- Merge(C1, C2): D = LCA(D1,D2), A = GCD(A1,A2), opt = opt1∨opt2, cmp = cmp1∨cmp2
- SolveCovariant: inner = A ∨ composite_D ∨ Any; return opt ? wrap(inner) : inner
- SolveContravariant: inner = D ∨ preferred; return opt ? wrap(inner) : inner

## 3. Pipeline

```
PULL(forward)  → tighten lower bounds (D), set IsOptional from None
PUSH(reverse)  → tighten upper bounds (A), skip None nodes
DESTRUCT(reverse) → collapse intervals to concrete types
MATERIALIZE    → convert remaining IsOptional ConstraintsState → StateOptional
FINALIZE(reverse) → resolve generics
```

4-phase + materialization bridge is sufficient. No 5th propagation phase needed.

## 4. Key Invariant

**INV: If C.IsOptional = true, then C.Descendant is NOT StateOptional.**

This prevents dual representation (flag + structural wrapper = double-wrapping).

Enforcement: in AddDescendant, after LCA, if result is StateOptional — unwrap:
```
if (_descendant is StateOptional opt) {
    IsOptional = true;
    _descendant = opt.Element;  // unwrap inner
}
```

## 5. Concretest Invariant

When a ConstraintsState with IsOptional=true is used as input to another constraint's AddDescendant, `Concretest()` materializes opt into StateOptional so LCA sees it:

```
Concretest(C[D..A, opt=true]) = opt(Concretest(D))
```

This ensures `LCA(U8, Concretest(C[..opt=true])) = LCA(U8, opt(...)) = opt(U8)`.

Already implemented via `type.Concretest()` call in AddDescendant's LCA branch.

## 6. Error Detection for Non-Optional Args

`f(x:i32); f(none)` — func arg has C[I32..], None sets IsOptional=true → C[I32.., opt=true].

After MaterializeOptionalFlags: node becomes StateOptional(inner=C[I32..]).
Destruction: StateOptional vs function's I32 constraint → incompatible → error.

Requires: MaterializeOptionalFlags runs BEFORE Destruction for error detection.
Risk: may break other tests (tested earlier, caused 42 regressions).

Alternative: in Destruction's Apply(ConstraintsState, StatePrimitive), when fitting P into C[D..A, opt=true] — the fit succeeds but result is opt(P). Then when opt(P) propagates through function call edges, structural incompatibility is detected.

## 7. Remaining Issues (7 failing tests)

All stem from:
1. **INV not enforced**: LCA can produce StateOptional descendant without unwrapping
2. **Concretest timing**: AddDescendant's LCA branch must Concretest incoming type (already done)
3. **Error detection**: None in non-optional context must eventually produce error
