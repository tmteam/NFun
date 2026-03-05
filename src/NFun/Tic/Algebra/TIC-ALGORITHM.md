# TIC Algorithm — Type Inference Constraint Solver

## Part 1: Type Universe

### 1.1 Types

Types form a lattice:

```
T ::= P                        -- primitive (Bool, Char, U8, U16, ..., I16, I32, ..., Real, Ip, Any)
    | Array(T)                 -- array with element type T
    | Fun(T₁...Tₙ → T)        -- function with arg types and return type
    | Struct{f₁:T₁...fₙ:Tₙ}   -- struct with named fields
    | C[desc, anc, cmp, pref]  -- constraint interval (unsolved type variable)
    | Ref(node)                -- reference to another node (alias)
```

Primitives, Array, Fun, Struct are **type states** (ITypeState).
Array, Fun, Struct are **composite states** (ICompositeState).
ConstraintsState is a bounded type variable, not a concrete type.

### 1.2 Subtyping (≤)

For primitives: pre-defined partial order (U8 ≤ U16 ≤ U32 ≤ ... ≤ Real ≤ Any, etc.)

For composites:
- `Array(A) ≤ Array(B)` iff `A ≤ B` (covariant)
- `Fun(A₁...Aₙ→R₁) ≤ Fun(B₁...Bₙ→R₂)` iff `Bᵢ ≤ Aᵢ` and `R₁ ≤ R₂` (args contravariant, return covariant)
- `Struct{...f:A...} ≤ Struct{f:B}` iff `A ≤ B` for each field f in B (width + depth subtyping, covariant)
- `T ≤ Any` for all T

Structs are immutable, therefore fields are covariant.

### 1.3 Constraint Interval

A constraint `C[desc, anc, cmp, pref]` (ConstraintsState) represents an unsolved type variable with:
- **desc** (descendant/lower bound): most concrete type known. Can be a primitive or composite.
- **anc** (ancestor/upper bound): most abstract type allowed. Always a primitive or null.
- **cmp** (comparable flag): if true, type must support comparison operators (numeric, Char, or Array(Char)).
- **pref** (preferred): resolution hint (e.g., I32 for integer constants).

**Invariant**: if both desc and anc are set, then `desc ≤ anc`.

---

## Part 2: Type Algebra

Four core operations on the type lattice. All are defined recursively for composite types.

### 2.1 Lca(A, B) — Least Common Ancestor (Join, ⊔)

Smallest type C such that `A ≤ C` and `B ≤ C`.
Used for: if-else result type, array element type.

| A | B | Lca(A, B) |
|---|---|-----------|
| P₁ | P₂ | Pre-computed 18×18 matrix |
| Array(A) | Array(B) | Array(Lca(A, B)) |
| Fun(A₁..Aₙ→R₁) | Fun(B₁..Bₙ→R₂) | Fun(Gcd(A₁,B₁)..Gcd(Aₙ,Bₙ) → Lca(R₁,R₂)), Any if any Gcd is null |
| Struct{fᵢ:Aᵢ} | Struct{fⱼ:Bⱼ} | Struct{fₖ:Lca(Aₖ,Bₖ)} where fₖ ∈ fields(A) ∩ fields(B) |
| C[dA,..] | C[dB,..] | C[Lca(dA,dB), null, cmpA ∧ cmpB] or desc directly if solved |
| different kinds | | Any |

Note: Struct LCA keeps only **common** fields (intersection). Each common field type is Lca'd recursively. This mirrors width subtyping: fewer fields = more general.

### 2.2 Gcd(A, B) — Greatest Common Descendant (Meet, ⊓)

Largest type C such that `C ≤ A` and `C ≤ B`. Returns null if no such C exists.
Used for: function argument types in Lca (contravariance), ancestor narrowing.

| A | B | Gcd(A, B) |
|---|---|-----------|
| P₁ | P₂ | Pre-computed 18×18 matrix (or null) |
| Array(A) | Array(B) | Array(Gcd(A, B)) or null |
| Fun(A₁..Aₙ→R₁) | Fun(B₁..Bₙ→R₂) | Fun(Lca(A₁,B₁)..→Gcd(R₁,R₂)) or null |
| Struct{fᵢ:Aᵢ} | Struct{fⱼ:Bⱼ} | Struct{union of fields, Gcd on shared} or null |
| different kinds | | null |

Note: Struct Gcd keeps the **union** of all fields (dual to Lca). Shared fields are Gcd'd recursively. More fields = more specific = lower in the lattice.

### 2.3 Unify(A, B) — Unification

Find a type satisfying BOTH A and B simultaneously. Returns null if impossible.
Used for: struct field Lca with unsolved constraints, node merging.

| A | B | Unify(A, B) |
|---|---|-------------|
| Any | X | Any |
| P | P (same) | P |
| P₁ | P₂ (different) | null |
| P | C[d, a, cmp] | P if P fits C, else null |
| C₁ | C₂ | C[Lca(d₁,d₂), Gcd(a₁,a₂), cmp₁∨cmp₂].Simplify() |
| Array(A) | Array(B) | Array(Unify(A,B)) or null |
| Struct{same fields} | Struct{same fields} | Struct{Unify per field} or null |
| different kinds | | null |

### 2.4 Fits(A, B) — Constraint Satisfaction

"Does type A fit where B is expected?"

For concrete types this is subtyping: A ≤ B.
For ConstraintsState B = C[desc, anc, cmp]: checks that `desc ≤ A ≤ anc` **and** comparable constraint is met. This is a two-sided check (not just subtyping).

Recursive for composites:
- Array: element Fits element
- Fun: return Fits return (covariant), args reversed (contravariant)
- Struct: A must have all fields of B, each field Fits (width + depth)

### 2.5 Supporting Operations

**Concretest(A)** — most specific type representable by A:
- Primitive → itself
- C[desc, ..] → Concretest(desc), or C[cmp] if no desc
- Array → Array(Concretest(element))
- Fun → Fun(Abstractest(args)→Concretest(return)) — contravariant args
- Struct → Struct with dereferenced field nodes

**Abstractest(A)** — most general type representable by A:
- Primitive → itself
- C[.., anc, cmp] → anc if present, else Any (unless comparable)
- Array → Array(Abstractest(element))
- Fun → Fun(Concretest(args)→Abstractest(return)) — contravariant args

**Simplify(C)** — simplify a constraint after modification:
- If desc = anc → return the primitive
- If comparable, validate against desc
- If desc is nested ConstraintsState, flatten
- Returns null if constraint is inconsistent

**Merge(C₁, C₂)** — merge two constraint intervals (on ConstraintsState):
- desc = Lca(d₁, d₂), anc = Gcd(a₁, a₂), cmp = cmp₁ ∨ cmp₂
- Handles preferred type resolution
- Returns null if inconsistent

### 2.6 Algebraic Properties

All verified by unit tests:

| Property | Lca | Gcd | Unify | Fits |
|----------|-----|-----|-------|------|
| Symmetry | ✅ | ✅ | ✅ | n/a |
| Idempotent | ✅ | ✅ | ✅ | n/a |
| Reflexive | n/a | n/a | n/a | ✅ |
| Transitive | ✅ primitives | n/a | n/a | ✅ primitives |
| Top (Any) | Lca(A,Any)=Any | Gcd(A,Any)=A | Unify(A,Any)=Any | Fits(A,Any)=true |
| Bottom (⊥=C[]) | Lca(A,⊥)=A | n/a | n/a | Fits(A,⊥)=true |

Cross-operation invariants (tested for primitives):
- `Gcd(A,B) ≤ A ≤ Lca(A,B)`
- `A fits Lca(A,B)`
- `Gcd(A,B) fits A`

---

## Part 3: Constraint Graph

### 3.1 Graph Structure

A directed graph G = (N, E) where:
- **N**: set of typed nodes. Each node has a mutable `state ∈ T`.
- **E**: ancestor edges. `(D, A) ∈ E` means "D ≤ A" (D's type must be convertible to A's type).

Node types:
- **Named**: input/output variables of the expression (`x`, `y`)
- **SyntaxNode**: intermediate expression nodes (constants, function calls)
- **TypeVariable**: generated during graph building (array elements, function args, struct fields)

### 3.2 Edge Semantics

Two kinds of relationships:

**Ancestor edge** (D ≤ A): "D is convertible to A". Created by `AddAncestor`.
Example: `y = if(cond) a else b` → both `a` and `b` get ancestor edge to `y`.

**Identity** (A ≡ B): "A and B are the same type". Created by `MergeInplace`.
One node becomes `StateRefTo` of the other. Used for cycle resolution and equivariant positions.

### 3.3 Composite Decomposition

When Pull encounters a composite-to-composite ancestor edge, it **decomposes** it into member-level edges:

**Array**: `Array(dE) ≤ Array(aE)` → replace with `dE ≤ aE` (one edge for element)
**Fun**: `Fun(dA→dR) ≤ Fun(aA→aR)` → replace with `dR ≤ aR` and `aA ≤ dA` (args reversed)
**Struct**: `Struct{f:dF} ≤ Struct{f:aF}` → replace with `dF ≤ aF` per common field.
  - Missing fields: added to descendant if not frozen.
  - Extra fields in descendant: allowed (width subtyping).

After decomposition, member nodes are processed by subsequent solver passes automatically.

---

## Part 4: Solving Algorithm

```
SOLVE(G):
    1. TOPOSORT(G)
    2. PULL(sorted)
    3. PUSH(sorted)
    4. DESTRUCT(sorted)
    5. FINALIZE(sorted)
```

### 4.1 TOPOSORT

Topologically sort nodes by ancestor edges via DFS.
If cycle detected (A ≤ B ≤ ... ≤ A), merge all nodes in cycle into one representative.
All other nodes in cycle become Ref(representative).

Post-condition: sorted array is acyclic.

### 4.2 PULL (bottom-up)

For each node in toposort order, for each ancestor edge (descendant → ancestor):
Propagate descendant information **upward** to tighten ancestor's lower bound.

Core operation: `ancestor.desc = Lca(ancestor.desc, descendant.desc)`

Dispatch matrix (ancestor state × descendant state):

| anc \ desc | Primitive | Constraints | Composite |
|---|---|---|---|
| **Primitive** | check compat | optimistic check | check compat |
| **Constraints** | AddDescendant + Simplify | AddDescendant + Simplify | AddDescendant + Simplify |
| **Composite** | fail | transform desc to composite, add member edges | decompose into member edges |

Key behaviors:
- **Composite ← Constraints**: transforms the constraint node into a matching composite (Array/Fun/Struct) and adds member-level ancestor edges. The composite-level edge is removed.
- **Struct ← Struct**: field-level decomposition with `AddAncestor` per field. Missing fields added to descendant.

### 4.3 PUSH (top-down)

For each node in **reverse** toposort order, for each ancestor edge:
Propagate ancestor information **downward** to tighten descendant's upper bound.

Core operation: `descendant.anc = Gcd(descendant.anc, ancestor.anc)`

| anc \ desc | Primitive | Constraints | Composite |
|---|---|---|---|
| **Primitive** | check compat | AddAncestor + Simplify | pass |
| **Constraints** | check ancestor | AddAncestor + Simplify | struct field propagation |
| **Composite** | fail | transform desc, push members | push per member |

Key behaviors:
- **Constraints ← Composite (Struct)**: if ancestor's constraint has a struct descendant, propagate field types via MergeInplace.
- **Struct ← Struct**: extend descendant with missing fields (width subtyping), then push per field.

### 4.4 DESTRUCT (bottom-up)

For each remaining unsolved ancestor-descendant pair, resolve to concrete types.

| anc \ desc | Primitive | Constraints | Composite |
|---|---|---|---|
| **Primitive** | pass | Fits → assign | pass |
| **Constraints** | CanBeConvertedTo → assign | MergeOrNull → assign+ref | Fits → ref; else fallback decompose |
| **Composite** | fail | Fits → ref; else struct transform+decompose | decompose members |

Key behaviors:
- **Constraints ← Constraints**: merge both intervals via MergeOrNull, assign result and create Ref.
- **Constraints ← Composite**: if composite fits constraints, create ref. Otherwise, for matching descendant types, decompose into member-level destruction.
- **Composite ← Constraints**: if composite ancestor fits descendant constraints, create ref. For structs, transform constraint to struct and destruct field-by-field.
- **Struct ← Struct**: destruct each field recursively. Only redirect ancestor→descendant if all fields are equivalent.

### 4.5 FINALIZE

Resolve remaining generics (unsolved ConstraintsState nodes):

1. Replace all Ref chains with actual states.
2. Identify output types and contravariant input types.
3. **Output** (covariant): solve via SolveCovariant — prefer ancestor (most general). Use preferred type if available and valid.
4. **Input** (contravariant): solve via SolveContravariant — prefer descendant (most concrete). Use preferred type if available and valid.
5. **Other**: solve covariantly.

---

## Part 5: Dispatch Pattern

Each stage (Pull, Push, Destruct) implements the IStateFunction interface with 11 Apply overloads:

```
3×3 for (Primitive, Constraints, Composite) × (Primitive, Constraints, Composite)
+3  for (Array×Array, Fun×Fun, Struct×Struct)
-1  because Composite×Composite dispatches to the specific 3
= 11 methods total
```

The dispatcher (StagesExtension.Invoke) resolves RefTo chains, then dispatches to the correct Apply overload based on runtime types of both states.

---

## Part 6: Variance Summary

| Context | Position | Direction | Operation |
|---|---|---|---|
| Array element | covariant | Lca for join, AddAncestor in Pull | desc ≤ anc |
| Fun return | covariant | Lca for join, AddAncestor in Pull | desc ≤ anc |
| Fun arguments | contravariant | Gcd for join, reversed AddAncestor | anc ≤ desc |
| Struct fields | covariant | Lca for join, AddAncestor in Pull | desc ≤ anc |
| If-else branches | covariant | Lca of both branches | branch ≤ result |

Structs are immutable → fields are covariant. If mutable structs/arrays are added, fields/elements would need to be invariant (both directions).

---

## Part 7: Key Invariants

Maintained throughout solving:

1. **Constraint interval valid**: if both desc and anc are set, then desc ≤ anc.
2. **Ancestor edges are acyclic** after toposort (cycles merged).
3. **Composite decomposition preserves semantics**: replacing Array≤Array with elem≤elem is equivalent.
4. **Ref chains terminate**: GetNonReference always finds a non-Ref state.
5. **Comparable propagation**: comparable flag propagates through Lca (AND), Merge (OR), and is checked in Fits and Simplify.
6. **Preferred type validity**: after merge, preferred is cleared if it doesn't fit the resulting constraint.

---

## Part 8: Terminology Reference

| TIC Term | Standard (type theory) | Description |
|---|---|---|
| Lca | Join, LUB (⊔) | Least common ancestor in type lattice |
| Gcd | Meet, GLB (⊓) | Greatest common descendant in type lattice |
| Unify | Unification | Find type satisfying both constraints |
| Fits | Constraint satisfaction | Does type A fit where B is expected? (two-sided for constraints) |
| Merge | Constraint combination | Combine two constraint intervals |
| Simplify | Normalization | Reduce constraint to simplest form |
| Concretest | Lower bound projection | Most specific type representable |
| Abstractest | Upper bound projection | Most general type representable |
| Ancestor edge | Subtyping constraint (≤) | D ≤ A: descendant convertible to ancestor |
| MergeInplace | Equate | Make two nodes the same type (identity) |
| Pull | Forward propagation | Bottom-up: tighten lower bounds |
| Push | Backward propagation | Top-down: tighten upper bounds |
| Destruct | Resolution | Resolve constraint intervals to concrete types |
| Finalize | Generic resolution | Solve remaining type variables by variance |
