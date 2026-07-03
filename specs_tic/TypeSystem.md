# Type System

> **Scope**: complete enumeration of TIC types — primitives, composites, and inference-time interval states. Includes the ConstructorLattice (collection kinds) and pair-struct synthesis bridge.

## 1. Two classes of state

Every TIC graph node has one of these states:

| State | Solved? | Role |
|---|---|---|
| `StatePrimitive` | yes | concrete primitive (I32, Bool, …) |
| `StateArray`, `StateCollection`, `StateMap`, `StateOptional`, `StateFun`, `StateStruct` | yes | concrete composite |
| `ConstraintsState` (CS) | no | primitive interval `[D..A]` + dimension flags |
| `StateCompositeConstraints` (CompCs) | no | composite interval over `ConstructorKind` |
| `StateRefTo` | n/a | transparent alias (dereferenced before any operator) |

Solved states represent a final type. Unsolved states represent a set of acceptable types and are reduced during Pull/Push/Destruction.

## 2. Primitive lattice

### 2.1. Concrete primitives

| Type | Numeric | Comparable | Notes |
|---|---|---|---|
| `Bool` | no | no | |
| `Char` | no | yes | |
| `Ip` | no | no | string-interpolation context |
| `Real` | yes | yes | IEEE 754 double |
| `I64`, `I32`, `I16` | yes | yes | signed integers |
| `U64`, `U32`, `U16`, `U8` | yes | yes | unsigned integers |
| `None` | no | no | absence of value |
| `Any` | no | no | top of lattice |

### 2.2. Abstract primitives

Intermediate types existing **only during inference**. They cannot be the final type of an expression.

| Type | Role |
|---|---|
| `I96` | LCA of large signed/unsigned: `I64 ∨ U64 = I96` |
| `I48` | LCA: `I32 ∨ U32 = I48` |
| `I24` | LCA: `I16 ∨ U16 = I24` |
| `I12` | LCA: `I8 ∨ U8 = I12` |
| `U48`, `U24`, `U12` | intermediate GCD results |
| `U4`  | GCD: `I8 ∧ U8 = U4` (lattice bottom of the unsigned chain, nominal 0..127) |

If a constraint interval collapses to an abstract type, that is a type error.

### 2.3. Numeric hierarchy

```
Unsigned: U4 ≤ U8 ≤ U12 ≤ U16 ≤ U24 ≤ U32 ≤ U48 ≤ U64 ≤ F32 ≤ Real
Signed:   I8 ≤ I12 ≤ I16 ≤ I24 ≤ I32 ≤ I48 ≤ I64 ≤ I96 ≤ F32 ≤ Real
```

A signed type contains an unsigned only if its bit width is strictly larger. Otherwise the LCA lifts to the next abstract level (I12, I24, I48, I96).

**Floats** (master, 2026-07): `F32 ≤ Real` (`Real ≡ float64`). F32 is the least common
supertype of ALL integers, so `int + f32 → f32` needs no explicit cast. `I8`/`Float32`
are concrete runtime types (sbyte/float); math builtins are generic over the Floats
constraint `[F32..Real]`, pinned to `[Real..Real]` when `FloatFamilySupport = None`.

Cross-table example: `U16 ∨ I16 = I24` (intermediate abstract), `U16 ∨ I32 = I32`.

### 2.4. Non-numeric primitive interactions

`Bool`, `Char`, `Ip` are mutually incompatible and incompatible with numerics: any cross LCA produces `Any`.

`None` is special: `None ∨ T = Opt(T)` for `T ∉ {Any, None}` (Optional lift).

## 3. Composite types

### 3.1. Array(T) — ee-mode covariant

Used in ee-mode (`Funny.Hardcore.Build`) for array literals `[1, 2, 3]`. **Covariant** in element: `Array(A) ≤ Array(B) ⟺ A ≤ B`.

- `Array(A) ∨ Array(B) = Array(A ∨ B)`
- `Array(A) ∧ Array(B) = Array(A ∧ B)`

Sits **outside** the ConstructorLattice (§4) — preserves expression-mode semantics unchanged.

### 3.2. Optional(T)

- **Covariant**: `Opt(A) ≤ Opt(B) ⟺ A ≤ B`
- **Implicit lift**: `T ≤ Opt(T)` for all T
- **None lift**: `None ∨ T = Opt(T)` for T ∉ {Any, None}
- **Flatten**: `Opt(Opt(T)) = Opt(T)` (postulate)
- **Top collapse**: `Opt(Any) = Any`

### 3.3. Fun(A₁, ..., Aₙ → R)

- **Arguments contravariant**: `Fun(A→R) ≤ Fun(B→R) ⟺ B ≤ A`
- **Return covariant**: `Fun(A→R₁) ≤ Fun(A→R₂) ⟺ R₁ ≤ R₂`
- Different arities are incompatible: `Fun(A→R) ∨ Fun(B,C→R) = Any`
- `Fun(A→R₁) ∨ Fun(B→R₂) = Fun(A ∧ B → R₁ ∨ R₂)`

### 3.4. Struct{f₁:T₁, …}

Structural typing with width subtyping. Each field has covariant depth.

- **Width**: `{x:A, y:B} ≤ {x:C} ⟺ A ≤ C` (more fields = subtype).
- **Depth**: `{x:A} ≤ {x:B} ⟺ A ≤ B`.
- **LCA**: field intersection. `{x:A, y:B} ∨ {x:C, z:D} = {x: A ∨ C}`
- **GCD**: field union. `{x:A} ∧ {x:B, y:C} = {x: A ∧ B, y:C}`
- **Frozen vs Mutable**: literals are frozen (field set fixed); field-access (`s.x`) creates open mutable structs that can absorb new fields during Pull/Push.
- **Named structs**: `TypeName` is an optional identifier for declared types; anonymous structs have `TypeName = null`.

#### Row polymorphism (IsOpen)

- **Closed struct** `{a:T}` — exact field set (literals, LCA, GCD results).
- **Open struct** `{a:T, ...}` — minimum these fields, possibly more (created by field-access).

| Existing | Incoming | Combination |
|---|---|---|
| Closed × Closed | LCA → intersection | Closed |
| Open × any | Field union | Open if either is open |

### 3.5. StateCollection (lang-mode invariant single-arg collections)

Unified state for List, MutableArray, FixedArray, Set (and future Queue/Stack). Data-driven on `ConstructorKind` — one class instance distinguishes its kind via an enum field, not subclassing.

- **Shape**: one element + `ConstructorKind` discriminator.
- **Variance**: **invariant** in element (uniform invariance rule).
- **Allowed kinds**: List, MutableArray, FixedArray, Set.
- **Rejected**: Any (sentinel), Enumerable (constraint-only), Map (separate StateMap class).

Cross with ee-mode StateArray uses the narrower-wins rule (Array-branch kinds widen to the array; see Algebra/CompositeConstraints.md §3.10).

### 3.6. StateMap

```
StateMap { KeyNode, ValueNode, Constructor = Map }
```

- **Shape**: two elements (K + V). Both mandatory.
- **Variance**: invariant in BOTH key and value.
- **Pure LCA**: requires `Equals(ak, bk) AND Equals(av, bv)`; else widens to `Any`.
- **Identity sharing**: `LcaOrShareIdentity` merges KeyNode/ValueNode identities when pure LCA would widen due to unresolved elements (see [`Algebra/LcaOrShareIdentity.md`](Algebra/LcaOrShareIdentity.md)).

### 3.7. StateCompositeConstraints (CompCs)

Composite analogue of CS. Used for function-signature slots typed `Enumerable<T>` etc.

```
CompCs { Ancestor: ConstructorKind?, Descendant: ConstructorKind?, ElementNode: TicNode, IsOptional: bool }
```

- **Subtyping**: a CompCs represents `{ SC(K, e) | Descendant ≤_L K ≤_L Ancestor }`.
- **Collapse**: when Descendant = Ancestor = K (concrete), CompCs collapses to a concrete `StateCollection(K, ElementNode)` or `StateMap(K, V)`.

Full algebra in [`Algebra/CompositeConstraints.md`](Algebra/CompositeConstraints.md).

## 4. ConstructorLattice

The 7-node lattice over collection constructors:

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

| Kind | Concrete | Instantiable via |
|---|---|---|
| `Any` | n/a | universal top |
| `Enumerable` | n/a | constraint-only (function signatures) |
| `FixedArray` | yes | `fixedArray(1,2,3)` |
| `Array` (mutable) | yes | lang-mode `int[]` literal |
| `List` | yes | `list(1,2,3)`, lang-mode literal |
| `Set` | yes | `set(1,2,3)` |
| `Map` | yes | `__mkMap(...)` + pair-struct synthesis to Enumerable |

### Lattice operations

- `Lca(a, b)` — climb both chains to root, return deepest shared node. Symmetric.
- `Gcd(a, b)` — return common descendant or `null`.
- `IsSubtypeOf(child, parent)` — walk `child`'s chain to root; true if `parent` is on the way.
- `Concretest(k)` — descend abstract constructor to canonical concrete (`Enumerable → List`, `FixedArray → Array`).

### Cross-kind merge identity

For `StateArray × StateCollection(List)` (ee-mode + lang-mode meet), the merge keeps the **narrower** state. This guarantees stable downstream pattern-matching on collection kind.

## 5. ConstraintsState — six orthogonal dimensions

| Dim | Field | Domain | Role |
|---|---|---|---|
| 1 | `Descendant` | `ITicNodeState?` | lower bound: T must be at least D |
| 2 | `Ancestor` | `StatePrimitive?` | upper bound: T must be at most A |
| 3 | `IsOptional` | bool | absorbed-None flag (materializes to `opt(inner)` pre-Destruction) |
| 4 | `IsComparable` | bool | type-class constraint |
| 5 | `StructBound` | `StateStruct?` | F-bound (see [`Advanced/PushReform.md`](Advanced/PushReform.md)) |
| 6 | `Preferred` | `StatePrimitive?` | metadata hint for resolution (not constraint) |

Each dimension is independent. NoConstrains predicate is true iff all six are absent (Preferred alone does not "constrain").

**Per-dimension operator behavior**:

| Operator | D / A | IsOpt | IsCmp | SB | P |
|---|---|---|---|---|---|
| `AddDescendant(T)` | `D ← Lca(prev, T)` | set true if `T = None` | inherited | unchanged | bidirectional preserved |
| `AddAncestor(T)` | `A ← Gcd(prev, T)` | unchanged | unchanged | unchanged | preserved |
| `Lca` | element-wise (LCA on D, GCD on A) | OR | AND | `GcdBound` | LCA-preserved |
| `Gcd` | element-wise (GCD on D, LCA on A) | AND | OR | n/a | metadata copy |
| `Unify` | `[Lca(D₁,D₂) .. Gcd(A₁,A₂)]` | OR | AND | `GcdBound` | preserved |

## 6. Map → Enumerable bridge — pair-struct synthesis

A `StateMap(K, V)` satisfies `CompCs{Anc=Enumerable, e}` via a **synthesized frozen pair-struct**:

```
synth = StateStruct(
    fields = { "key" → KeyNode, "value" → ValueNode },
    isFrozen = true
)
MergeInplace(CompCs.ElementNode, synth)
```

**Critical identity property**: the synthesized struct's field nodes ARE the StateMap's `KeyNode` and `ValueNode` (reference-equal). Constraints on the synthesized struct fields propagate directly to K and V; the bridge is structurally sound (see [`Algebra/StateMap.md`](Algebra/StateMap.md) §3).

The synthesized struct is `isFrozen=true` to prevent width-propagation: Map's pair-shape has exactly `{key, value}` fields.

## 7. Identity discipline

TIC tracks two equality notions:

- **Reference equality** (`ReferenceEquals` on TicNode pointers): used by MergeInplace short-circuit, LcaOrShareIdentity, cross-Apply circular-ancestor guard.
- **Structural equality** (`Equals` on state values): used by LCA/GCD computation and final type comparisons.

**Why identity matters for invariance**: uniform invariance requires that "two collections of the same kind with the same element" be the **same type**. Without identity sharing, two structurally-equal-but-distinct nodes would split constraints onto two graphs that should be one. `LcaOrShareIdentity` enforces this by merging element-node identities when pure LCA would widen.

## 8. Function-signature slots

When a generic function is called, TIC creates per-call slots:

| Signature type | TIC slot state |
|---|---|
| `Generic(i)` | StateRefTo to a fresh CS |
| `Generic(i)[]` | StateArray (ee-mode) or StateCollection (lang-mode) wrapping the slot |
| `EnumerableOf<T>` | CompCs `{Ancestor=Enumerable, ElementNode=T-slot}` |
| `MapOf<K,V>` | StateMap |
| `(A,B)→R` | StateFun |

Slots are wrapped with `IsSignatureParam=true` to prevent Optional-wrapping or other shape-changing transformations.

### Dialect split for `map`

Lang-mode and ee-mode use different `map` signatures (only function with a dialect split):

- ee-mode `MapFunction`: `(FixedArray<T0>, (T0)→T1) → FixedArray<T1>` — strict back-prop precision.
- Lang-mode `MapEnumerableFunction`: `(Enumerable<T0>, (T0)→T1) → FixedArray<T1>` — accepts Map<K,V> via pair-struct synthesis.

This is the only LINQ split — other LINQ functions either preserve element type (`filter`, `count`, `any`) or collapse to scalars (`sum`), so they have no precision concerns.

## 9. Subtyping summary

| Rule | Formula |
|---|---|
| Numeric widening | `U8 ≤ U16 ≤ ... ≤ Real`, `I16 ≤ I32 ≤ ...` |
| Optional lift | `T ≤ Opt(T)` |
| Array covariance | `A ≤ B ⟹ A[] ≤ B[]` |
| Optional covariance | `A ≤ B ⟹ Opt(A) ≤ Opt(B)` |
| Fun covariance (ret) | `R₁ ≤ R₂ ⟹ (A→R₁) ≤ (A→R₂)` |
| Fun contravariance (args) | `B ≤ A ⟹ (A→R) ≤ (B→R)` |
| Struct width | `{f₁:T₁,…,fₙ:Tₙ,…} ≤ {f₁:T₁,…,fₙ:Tₙ}` |
| Struct depth | `A ≤ B ⟹ {f:A} ≤ {f:B}` |
| Collection (invariant) | `StateCollection(K, A) = StateCollection(K, B) ⟺ A ≡ B` |
| Map (invariant) | `StateMap(K, V) = StateMap(K', V') ⟺ K ≡ K' ∧ V ≡ V'` |
| None lift | `None ≤ Opt(T)` |
| Top | `T ≤ Any` |

## 10. Postulates

1. `Opt(Opt(T)) = Opt(T)` — Optional does not nest.
2. `Opt(Any) = Any` — Any already contains all values.
3. Abstract types do not materialize.
4. No intermediate abstract composites.
5. Element invariance for StateCollection and StateMap (uniform invariance).
6. Map and StateCollection are NOT subtypes of each other; the only bridge is via CompCs Enumerable through pair-struct synthesis.
7. `LcaOrShareIdentity`'s identity-merge side-effect is part of the algebraic contract (P5 Identity-Sharing Soundness, [`Proofs.md`](Proofs.md) §5).

## See also

- Operator algebra: [`Algebra/`](Algebra/)
- Graph structure: [`Graph.md`](Graph.md)
- Algorithm: [`Algorithm.md`](Algorithm.md)
- Formal proofs: [`Proofs.md`](Proofs.md)
