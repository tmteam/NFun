# Constructor Lattice

> Stage 1 deliverable of the mutable collections feature.
> See parent spec: [`/Specs/Collections.md`](../Collections.md).

A 7-node lattice over collection composite type constructors. Used by TIC's
algebraic operators (LCA, GCD, Concretest) when reasoning about uniformly
positional ordered collections (lists, sets, arrays, maps).

```
                Any
                 │
            Enumerable
            /    │    \
   FixedArray   Set    Map
        │
     Array
        │
      List
```

## Members

| Ordinal | Kind         | Status                                | Instantiable via                  |
|---------|--------------|---------------------------------------|-----------------------------------|
| 0       | `Any`        | universal top                         | n/a (lattice sentinel)            |
| 1       | `Enumerable` | constraint-only                       | n/a (used in fn signatures)       |
| 2       | `FixedArray` | concrete, read-only                   | `fixedArray(1,2,3)` factory       |
| 3       | `Array`      | concrete, mutable fixed-size          | lang-mode `int[]` literal         |
| 4       | `List`       | concrete, mutable growable            | `list(1,2,3)`, lang-mode literal  |
| 5       | `Set`        | concrete, mutable unordered           | factory TBD (Stage 0 OQ #9)       |
| 6       | `Map`        | concrete, mutable key→value           | syntax TBD (Stage 5+)             |

`Enumerable` is the only **constraint-only** member — it never produces a
runtime value. Every other constructor has at least one factory or literal
that materialises it.

## Operations

### `Lca(a, b) → ConstructorKind`

Least common ancestor. Climb both chains to root, return deepest shared node.

| a            | b          | Lca          |
|--------------|------------|--------------|
| `List`       | `List`     | `List`       |
| `List`       | `Array`    | `Array`      |
| `List`       | `Set`      | `Enumerable` |
| `Set`        | `Map`      | `Enumerable` |
| `Array`      | `Set`      | `Enumerable` |
| `Enumerable` | `List`     | `Enumerable` |
| anything     | `Any`      | `Any`        |

**Symmetric:** `Lca(a, b) == Lca(b, a)` for all inputs (proven by
`ConstructorLatticeTest.Lca_IsSymmetric` over the full Cartesian product).

### `Gcd(a, b) → ConstructorKind?`

Greatest common descendant, or `null` if none. Symmetric to LCA.

| a            | b            | Gcd          |
|--------------|--------------|--------------|
| `List`       | `Array`      | `List`       |
| `Enumerable` | `Set`        | `Set`        |
| `FixedArray` | `Enumerable` | `FixedArray` |
| `List`       | `Set`        | `null`       |
| `Array`      | `Map`        | `null`       |

### `IsSubtypeOf(child, parent) → bool`

Walk `child`'s chain to root. True if `parent` appears along the way.
Reflexive: `IsSubtypeOf(X, X) == true`.

`Any` is the universal supertype: `IsSubtypeOf(X, Any) == true` for all X.

### `Concretest(kind) → ConstructorKind`

Descend an abstract constructor to its preferred concrete representative.

| Input        | Output  | Reason                                                       |
|--------------|---------|--------------------------------------------------------------|
| `Enumerable` | `List`  | spec-defined: List is the canonical mutable Enumerable       |
| `FixedArray` | `Array` | only concrete descendant in the lattice's FixedArray branch  |
| anything else| itself  | already concrete                                             |

**Dialect-independent** at the TIC level. The lang-vs-ee literal default
choice (lang `[1,2,3]` → List vs ee `[1,2,3]` → existing `StateArray`)
happens at the parser, where the parser hard-codes which state class the
literal binds to. The algebra is pure.

### `IsConstraintOnly(kind) → bool`

True iff `kind == Enumerable`. Used to distinguish "appears only in
constraints" from "abstract in the lattice shape".

### `RequiresConcretestDescent(kind) → bool`

True iff `kind ∈ {Enumerable, FixedArray}` — the two members for which
`Concretest` would descend further. Used by resolution-time code to decide
"should I look for a concrete descendant?"

### `ElementVariance(kind) → Variance`

All members return `Invariant`. The Stage 0 design accepts the simplification
of uniform invariance across all new collections; element-LCA collapses to
`Any` on mismatch.

The legacy ee-mode `StateArray` is **covariant** in element but is **not**
in this lattice. It stays outside the new infrastructure to preserve
expression-mode semantics unchanged.

## Implementation notes

- Pure static operations on a hand-written parent map. No dialect dependency,
  no allocation, no global state. Confluence of TIC's algebraic operators
  (see [`Algebra.md`](Algebra.md)) survives the addition.
- O(depth) climb; depth ≤ 4. Lookup against a 7-element `ConstructorKind[]`
  parent table.
- `Lca` uses a 32-bit `int` bitmask; guarded by `Debug.Assert` for the
  ≤32-member ceiling. Promote to `long` if the lattice grows past 32 members.

## State class membership

| State class                | Kind(s)                           | Variance  | Mutability    | Stage |
|----------------------------|-----------------------------------|-----------|---------------|-------|
| `StateCollection`          | List / FixedArray / Array / Set / future Queue, Stack | Invariant | per-kind (see Collections.md) | 1 (scaffold), 2-4 (light up per kind) |
| `StateMap` (future)        | `Map`                             | Invariant | hash-mutable  | 5+ (full)    |
| `StateArray` (legacy)      | n/a                               | Covariant | immutable     | unchanged    |
| `StateFun`                 | n/a                               | mixed     | n/a           | unchanged    |
| `StateStruct`              | n/a                               | invariant | mutable in lang | unchanged  |

`StateCollection` is data-driven: a single C# class instance distinguishes its
collection kind via the <see cref="ConstructorKind"/> enum carried as a field,
not via subclassing. This collapses what would be N×M Apply overloads
(IStateFunction / StagesExtension) into a single dispatch.

Two-arg collections (Map) keep a separate state class because their shape
(key + value) differs structurally from the single-arg invariant pattern.

The legacy `StateArray`, `StateFun`, and `StateStruct` are deliberately
outside the constructor lattice — their internals (covariant element /
arg+ret split / named-field dict) don't benefit from uniform
`CompositeArg[]` representation. See [`Collections.md`](../Collections.md)
§Scope of the refactor.

## Cross-kind merge identity

When the same TIC node ends up with both a `StateArray` snapshot and a
`StateCollection(List)` snapshot during constraint propagation, the merge
keeps **one** identity for that node. The rule:

> **Narrower-Constructor wins.** Across the lattice edge `List ⊆ Array`, the
> merge of a `StateArray × StateCollection(List)` pair returns the
> `StateCollection(List)` instance regardless of which side called
> `GetMergedStateOrNull`. The array's element node is merged into the list's
> element node via `MergeInplace`.

This is the analogue of `MergeOrNull`'s general "narrower bound wins" rule
for primitives. It guarantees that downstream code which pattern-matches
on collection kind sees a stable result:

- If a graph node ever held a list snapshot, after the merge it is a list.
- If a graph node only ever saw arrays, the merge keeps the array.
- `Specs/Tic/Algebra_LCA.md` documents the symmetric LCA case
  (`Array × List → Array(elemLCA)`) — LCA widens up the lattice, merge
  narrows down.

Implementation: `SolvingFunctions.GetMergedStateOrNull` (the
`StateArray × StateCollection(List)` / `StateCollection(List) × StateArray`
arms) and `MergeCollectionsWithCycleGuard` (the visited-pair guard that
keeps the recursion termination intact for self-referential lists). Same
rule applies for future cross-edge merges as new collection kinds land.
