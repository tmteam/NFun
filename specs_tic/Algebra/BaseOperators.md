# Base Operators — LCA, GCD, Fit, Unify, Concretest, Abstractest

Per-operator rules for the 6 base operators of TIC algebra. See [`README.md`](README.md) for context.

## LCA (∨) — join

**Definition**: smallest type that is a supertype of both A and B.

**Totality**: total. Worst case `A ∨ B = Any`.

### Rules by type

| Inputs | Result |
|---|---|
| `T ∨ T` | `T` (idempotence) |
| `T ∨ Any` | `Any` |
| `None ∨ T` (T ∉ {Any, None}) | `Opt(T)` (None lift) |
| primitive × primitive | precomputed LCA table (O(1)) |
| `Array(A) ∨ Array(B)` | `Array(A ∨ B)` (covariant) |
| `Opt(A) ∨ Opt(B)` | `Opt(A ∨ B)` |
| `Opt(A) ∨ B` | `Opt(A ∨ B)` (implicit B lift) |
| `Opt(Any) ∨ T` | `Any` (top collapse) |
| `Fun(A→R₁) ∨ Fun(B→R₂)` | `Fun(A ∧ B → R₁ ∨ R₂)` (contra args, co ret) |
| Different-arity Fun | `Any` |
| `{x:A, y:B} ∨ {x:C, z:D}` | `{x: A∨C}` (field intersection) |
| StateCollection same-kind × same-kind | per-element merge or `Any` (invariant) |
| StateCollection cross-kind × cross-kind | climb lattice via `ConstructorLattice.Lca` |
| `CompCs × CompCs` | per-dimension join (Anc:max_L, Desc:min_L, IsOpt:OR, element merge) |
| `StateMap × StateMap` | strict equality on K,V else `Any` (see [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md) for unresolved case) |

### Properties

The properties below hold up to the equivalence relation `≡_id` defined in §"Identity equivalence" at the end of this file. For pure types (primitives, covariant composites without unresolved invariant elements, function and struct shapes whose internals are themselves pure) `≡_id` is structural equality; for invariant composites (`StateCollection`, `StateMap`, `CompCs`) whose element nodes may be merged in-place by `LcaOrShareIdentity`, `≡_id` quotients by the merge-induced node-identity equivalence.

- **Commutative**: `A ∨ B ≡_id B ∨ A` (proven by enumeration / structural induction).
- **Associative**: `(A ∨ B) ∨ C ≡_id A ∨ (B ∨ C)`.
- **Idempotent**: `A ∨ A ≡_id A`.
- **Monotone**: `A ≤ A' ⟹ A ∨ B ≤ A' ∨ B`.

For primitives, covariant composites, Fun and Struct shapes, `≡_id` coincides with `=`, so the laws read as ordinary equalities. For invariant composites whose element nodes are unresolved at the time of operation, `LcaOrShareIdentity`'s side-effect may rewire node identity — see [`LcaOrShareIdentity.md`](LcaOrShareIdentity.md) §5 for the formal contract. Proofs in [`../Proofs.md`](../Proofs.md) that depend on these laws state explicitly whether they rely on `=` or `≡_id`.

### LCA as least upper bound

**Lemma (LCA-as-LUB)**. For all types `A, B, C`: if `A ≤ C` and `B ≤ C`, then `A ∨ B ≤ C`.

**Proof** by structural induction on the pair `(A, B)`.

**Top / bottom axioms**: `T ∨ Any = Any` and `Any ≤ Any` close immediately. `None ∨ T` cases are handled by the Optional-lift inductive step below.

**Case 1 — same primitive on both sides** (`A, B ∈ StatePrimitive`, same primitive hierarchy): `A ∨ B` is the join in the primitive lattice. If `A ≤ C` and `B ≤ C`, then by lattice property `A ∨ B ≤ C`. ✓

**Case 2 — primitives in distinct branches**: `A ∨ B = Any` only if no common supertype exists below Any; but then any `C` with `A ≤ C ∧ B ≤ C` must itself be `Any`. So `A ∨ B = Any ≤ Any = C`. ✓

**Case 3 — covariant composites match** (`A = Array(A')`, `B = Array(B')`): `A ≤ C` forces `C = Array(C')` with `A' ≤ C'`; similarly `B' ≤ C'`. By IH on the element pair, `A' ∨ B' ≤ C'`. Therefore `A ∨ B = Array(A' ∨ B') ≤ Array(C') = C`. ✓ (Same argument applies to `Opt`.)

**Case 4 — invariant single-arg composites match** (`A = StateCollection(K, e_A)`, `B = StateCollection(K, e_B)`): per `LcaOrShareIdentity.md` §5 P4, the result is `StateCollection(K, e)` where `e` carries the merged element constraints. `A ≤ C` forces `C = StateCollection(K, e_C)` with `e_A ≡ e_C` (invariance — strict identity). Same for B: `e_B ≡ e_C`. After identity merge, the result's element is identified with both, hence with `e_C`. Result `≤ C`. ✓

**Case 5 — invariant two-arg composites match** (`A = StateMap(k_A, v_A)`, `B = StateMap(k_B, v_B)`): pointwise application of Case 4 on both axes (K and V). ✓

**Case 6 — composites in distinct branches** (`A = Array`, `B = Fun`, etc.): `A ∨ B = Any`. Any `C` with `A ≤ C ∧ B ≤ C` must satisfy both supertype constraints from different lattice branches, forcing `C = Any`. ✓

**Case 7 — Fun-on-Fun** (mixed variance — `A = Fun(A_arg → A_ret)`, `B = Fun(B_arg → B_ret)`): `A ∨ B = Fun(A_arg ∧ B_arg → A_ret ∨ B_ret)`. `A ≤ C` requires `C = Fun(C_arg → C_ret)` with `C_arg ≤ A_arg` (contra) AND `A_ret ≤ C_ret` (co); same for B: `C_arg ≤ B_arg`, `B_ret ≤ C_ret`. By GCD-as-GLB (dual lemma below) on the args: `C_arg ≤ A_arg ∧ B_arg`. By IH on the rets: `A_ret ∨ B_ret ≤ C_ret`. Therefore the LCA Fun fits into `C` by Fun-Fit's contravariant/covariant rule. ✓

**Case 8 — Struct-on-Struct** (width subtyping — `A = {f₁:A₁,…}`, `B = {f₁:B₁,…}`): `A ∨ B = {f_i : A_i ∨ B_i | f_i ∈ keys(A) ∩ keys(B)}` (field intersection). `A ≤ C` forces `keys(C) ⊆ keys(A)` and per-shared-field `A_i ≤ C_i`; same for B: `keys(C) ⊆ keys(B)` and `B_i ≤ C_i`. So `keys(C) ⊆ keys(A) ∩ keys(B) = keys(A ∨ B)`. By IH on each shared field: `A_i ∨ B_i ≤ C_i`. Therefore `A ∨ B ≤ C`. ✓

**Case 9 — Optional lift** (`A = Opt(A')`, `B` non-Opt): `A ∨ B = Opt(A' ∨ B)`. `A ≤ C` requires `C = Opt(C')` with `A' ≤ C'`; `B ≤ C = Opt(C')` requires either `B ≤ C'` (lift) or `B = None` (trivial). By IH on the wrapped pair: `A' ∨ B ≤ C'`. Therefore `Opt(A' ∨ B) ≤ Opt(C') = C`. ✓

**Case 10 — CompCs on either side**: see `CompositeConstraints.md` §2.1 for the per-dimension formula; the LCA-as-LUB property is preserved per-dimension by the lattice / boolean-OR / element-identity-merge structure (each dimension independently satisfies its dimension-local LUB property, proven in `Confluence.md` §2).

**Termination**: the induction is on the pair `(A, B)`; each recursive call descends into structurally smaller components (element, arg, ret, field). Base cases (primitives, top) close immediately.

This proves `A ∨ B ≤ C` for all type pairs. ∎

**Dual lemma (GCD-as-GLB)** is proven by the dual argument with variances swapped: GCD widens contravariant positions and narrows covariant ones. Used in Case 7 above.

## GCD (∧) — partial meet

**Definition**: largest type that is a subtype of both A and B, or `null` if none.

**Partiality**: `Bool ∧ I32 = null`, `Bool ∧ Char = null`. Non-null iff A and B have a common subtype.

### Rules by type

| Inputs | Result |
|---|---|
| `T ∧ T` | `T` |
| `T ∧ Any` | `T` |
| `None ∧ T` | `None` if `T ∈ {None, Opt(_)}`; `null` else |
| primitive × primitive | precomputed GCD table (returns null for incompatible) |
| `Array(A) ∧ Array(B)` | `Array(A ∧ B)` if defined |
| `Opt(A) ∧ Opt(B)` | `Opt(A ∧ B)` |
| `Opt(A) ∧ B` (B ≠ Opt) | `A ∧ B` (unwrap then meet) |
| `Fun(A→R₁) ∧ Fun(B→R₂)` | `Fun(A ∨ B → R₁ ∧ R₂)` (dual to LCA) |
| `{x:A, y:B} ∧ {x:C, z:D}` | `{x:A∧C, y:B, z:D}` (field union) |
| StateCollection cross-kind | `ConstructorLattice.Gcd` → null on cross-branch |

### Properties

- Commutative, associative, idempotent on the comparable subset.
- **Width-subtyping dual**: LCA narrows field sets, GCD widens them.

## Fit (≤) — partial order

**Definition**: `A ≤ B` iff A's values are all valid B values.

**Bool**: `true`/`false`. Equivalent to `A ∨ B = B` (or, when GCD is defined, to `A ∧ B = A`).

### Rules

| Pair | Holds when |
|---|---|
| `T ≤ T` | always (reflexive) |
| `T ≤ Any` | always (top) |
| `None ≤ Opt(T)` | always |
| `T ≤ Opt(T)` | always (Optional lift) |
| primitive × primitive | per numeric / non-numeric hierarchy |
| `Array(A) ≤ Array(B)` | `A ≤ B` (covariant) |
| `Fun(A→R) ≤ Fun(B→S)` | `B ≤ A` AND `R ≤ S` |
| `{f₁,…,fₙ,extra…} ≤ {f₁,…,fₙ}` | each shared field's depth: `A_i ≤ B_i` |

### Properties

- **Reflexive**: `T ≤ T`.
- **Transitive**: `A ≤ B ∧ B ≤ C ⟹ A ≤ C`.
- **Antisymmetric**: `A ≤ B ∧ B ≤ A ⟹ A ≡ B` (where ≡ is structural equality).

Together, Fit makes the type domain a **partial order**.

## Unify (⊓) — constraint intersection

**Definition**: For two ConstraintsState intervals `[D..A]` and flag dimensions:

```
[D₁..A₁, opt₁, cmp₁, S₁, P₁] ⊓ [D₂..A₂, opt₂, cmp₂, S₂, P₂] =
    [D₁ ∨ D₂  ..  A₁ ∧ A₂,
     opt₁ OR opt₂,
     cmp₁ AND cmp₂,           (intersection of comparable constraints)
     GcdBound(S₁, S₂),
     P-preserved bidirectionally]
```

Non-empty iff `D₁ ∨ D₂ ≤ A₁ ∧ A₂`.

For struct LCA on non-CS fields, Unify is the underlying operator.

### Partiality

Returns null when:
- `A₁ ∧ A₂ = null` (ancestor GCD undefined)
- `D₁ ∨ D₂ > A₁ ∧ A₂` (empty interval)

## Concretest (↓) — lower bound extraction

**Definition**: most concrete type satisfying a constraint state.

For ConstraintsState `[D..A]`: returns `D` if defined, else `A`'s Concretest, else type-class default (Comparable → `Char`, etc.), else `None` for Optional, else `Any`.

For composites: pointwise. E.g., `Concretest(Array(c)) = Array(Concretest(c))`.

For StateMap: pointwise on KeyNode and ValueNode.

**Idempotent**: `↓(↓A) = ↓A`.

## Abstractest (↑) — upper bound extraction

**Dual to Concretest**: most abstract type satisfying a constraint.

For ConstraintsState `[D..A]`: returns `A` if defined, else `D`'s Abstractest, else `Any`.

Pointwise on composites.

**Idempotent**: `↑(↑A) = ↑A`.

**F-bound exclusion**: Concretest and Abstractest explicitly **exclude** the StructBound dimension — F-bound is a third independent dimension orthogonal to `[D..A]` interval extraction. See [`../Advanced/PushReform.md`](../Advanced/PushReform.md).

## Identity equivalence `≡_id`

The algebraic laws on LCA / GCD / Unify / Concretest / Abstractest hold up to the equivalence relation `≡_id` defined as follows.

Let `~` be the smallest equivalence relation on TicNode generated by `MergeInplace`: `a ~ b` whenever `MergeInplace(a, b)` has been invoked during the current solve. `~` extends to states by congruence: two states are `~`-related iff they have the same constructor and their component nodes are pointwise `~`-related.

**Definition**: two types `T, T'` are **identity-equivalent**, written `T ≡_id T'`, iff `T ~ T'`.

For pure types (no invariant composite with `MergeInplace`-able element nodes in the subterm), `~` reduces to syntactic equality, so `T ≡_id T'` iff `T = T'`.

For invariant composites (`StateCollection`, `StateMap`, `CompCs`), `~` may identify two structurally-distinct snapshots whose element nodes have been merged. The algebraic laws then hold modulo this identification.

**Properties of `≡_id`**:
- Reflexive, symmetric, transitive (an equivalence by construction).
- Monotone in time: once `a ~ b`, the relation persists for the rest of the solve.
- Preserved by every operator in this file: `T₁ ≡_id T₂ ∧ U₁ ≡_id U₂ ⟹ op(T₁, U₁) ≡_id op(T₂, U₂)` for `op ∈ {∨, ∧, ⊓, ↓, ↑}`. Proven case-by-case in [`Confluence.md`](Confluence.md) §3.

**Where `≡_id` matters in proofs**:
- [`../Proofs.md`](../Proofs.md) P4 (Determinism) — states the property up to `≡_id` because LcaOrShareIdentity's side-effect chooses an arbitrary representative of the equivalence class.
- [`../Proofs.md`](../Proofs.md) P5 (Identity-sharing Soundness) — soundness preservation under `~`-quotient.
- [`Confluence.md`](Confluence.md) — confluence is stated as "same final graph state modulo `≡_id`".
