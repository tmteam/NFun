# Push Reform — Iso-Recursive Type Inference for Named Structs

> **Status**: F-bounded polymorphism + StructBound dimension on ConstraintsState (named recursive types).
> .

**Issue**: [#121](https://github.com/tmteam/NFun/issues/121) — unannotated recursive functions on declared named types.

## Motivating example

```nfun
type node = {v: int, next: node? = none}

listSum(n) = if(n == none) 0 else n.v + listSum(n?.next)
```

`listSum` has no return-type annotation. Its principal type is `node? → int` — but TIC must derive that without the annotation. The body's recursive call `listSum(n?.next)` plus the field accesses `n.v` and `n?.next` create a struct→struct cycle on `n`'s parameter type. Without intervention, TIC throws *Recursive type definition*.

The principal type — `μX. opt(struct{v:int, next:X})` — is **iso-recursive and contractive**: every back-edge crosses an Optional constructor (`opt`), so the cycle is well-founded. Push reform restores that Optional break during solving and (when the lifted body has no uniquely-matching named type) generalizes it as an F-bounded generic.

## Algorithm

### Phase A — Cycle detection

`SolvingFunctions.ThrowIfRecursiveTypeDefinition` walks a node's full state subgraph and counts back-edges. A back-edge through an Optional or Array breaks the cycle (those are constructors). A back-edge between two structs without an Optional/Array break is invalid — *unless* the subgraph carries the **opt-sourced** marker.

### Phase B — Opt-sourced gate

A struct's `IsOptionalSourced` flag is set when the struct originated from a `?.` access (`SetSafeFieldAccess` emits `opt(struct{field:T})` and stamps the inner struct). The marker is preserved through every algebraic combination (Pull, Lca, Gcd, Unify, Concretest, MergeStructs, UnionStructFields) by the rule `MergedIsOptionalSourced(a, b) = a || b`.

When cycle detection fires, the reform consults `HasReachableOptSourcedStruct` and `StructSubgraphIsOptSourced`. If either is true, the cycle is *contractive* (originated through `?.`) and the recursion check attempts repair instead of throwing. Declared `type t = {self:t}` never sets the marker and still errors.

### Phase C — Cycle repair

Two repair paths cover the cycle topologies that arise:

**(C-struct) `TryRepairOptSourcedCycle`** wraps **every** closing edge of the cycle struct in `StateOptional`:

```
struct{v:int, next:struct{v:int, next:...}}
                ↓
struct{v:int, next:opt(struct{v:int, next:opt(...)})}
```

Multi-self-field structs (e.g., `tree{left:tree, right:tree}`) need **both** edges wrapped. Wrapping only the first leaves the others as struct→struct cycles that trip the recursion check on the next pass.

After all closing edges are wrapped, the cycle struct is stamped with `TypeName` from the registry via `FindUniqueMatchingNamedType` — match by field-set superset. The stamp is set **last**: setting it earlier flips `IsMutable=false` (named structs are solved) and blocks the Optional wrap assignment.

**(C-cs) `TryPromoteCSDescendantToStructBound`** handles the case where the cycle root is a `ConstraintsState` whose `Descendant` carries the cycle struct. When `StructDescendantClosesContractively` confirms every closing field path crosses an Optional or Array, the struct is rebound: it moves from the `Descendant` slot to the `StructBound` slot on the same CS. No state-class change — strict refinement in the F-bound calculus, per Cardelli–Mitchell '89 / Pierce TAPL §20 (the lower bound of a recursive shape IS the F-bound). Subsequent recursion walks short-circuit at the CS via the `StructBound != null` branch.

### Phase D — TypeName propagation across snapshots

`PropagateTypeNamesAcrossSnapshots` runs after Destruction. Cycle repair stamps `TypeName` on **one** struct instance — but TIC algebraic operations (Pull, Lca, Concretest, Apply) create many independent struct snapshots throughout the body's graph. They all denote the same μ-type and must agree on identity for runtime dispatch (operator generic resolution, variable types, named-struct casts).

Two-pass:

1. **Collect** every TypeName already stamped on a struct (`presentNames`). Detect whether the body contains any opt-sourced struct (`anyOptSourced`).
2. **Skip the pass entirely** when no opt-sourced struct exists — without one, every declared named type came from explicit annotation and unnamed snapshots are local field-access constraints (`struct{v}` from `n.v`), not μ-type tags.
3. **Stamp** every unnamed struct whose field-set is a *unique* subset of one collected name's declared fields. The "exactly one" rule prevents over-tagging when several named types share a common field prefix.

### Phase E — F-bound lifting

`LiftMuTypes` runs after `PropagateTypeNamesAcrossSnapshots`. It finds the canonical post-cycle-rescue shape — `StateOptional(elem)` where `elem.State is StateStruct{IsOptionalSourced=true, TypeName=null}` with a verified self-ref — and replaces the inner element's state with `ConstraintsState{StructBound = struct}`. The original struct becomes the F-bound body `Sμ`, owned by the new CS; back-edges already RefTo the elem node, so they now semantically denote "back to the bounded variable T". `Sμ.IsFrozen` is set at lift to forbid further width extension at call sites (analogous to TAPL §22.6 let-generalization closing a row variable).

Existing TypeName stamps survive the lift: F-bound and TypeName coexist; runtime picks TypeName when present (nominal), falls back to F-bound (structural).

A second sub-pass, `TryRedirectDegenerateOptCycle`, rewrites degenerate `opt(opt(...))` chains that closed without struct content to a `StateRefTo` onto a sibling F-bound holder (Pottier-Rémy '05 §10.6 graph-as-witness; Amadio-Cardelli '93 §4.2).

## TypeName algebraic rules

Two distinct rules live in `StateStruct`:

| Operation | TypeName rule | Helper | Why |
|---|---|---|---|
| `Lca` | equal → keep, anything else → null | `LcaTypeName(a, b)` | LCA is the common ancestor — must NOT invent a name the other side never carried. |
| Merge (Pull, Gcd, Unify, MergeStructs, UnionStructFields, Apply(Struct,Struct)) | one null → other; both equal → that name; both differ → null *(caller rejects)* | `MergedTypeName(a, b)` | Merge is the most-specific consistent identity — one side carrying a name plus the other being anonymous is no conflict. |

`Apply(StateStruct ancestor, StateStruct descendant)` rejects with `return false` on TypeName conflict (both named, differ) — that's a real type error (`type a` ≤ `type b`), and Pull must signal incompatibility.

## Identity short-circuit

`StateStruct.Equals` short-circuits on `TypeName` equality:

```csharp
if (TypeName != null && other.TypeName != null
    && TypeName.Equals(other.TypeName, StringComparison.OrdinalIgnoreCase))
    return true;
```

Two named structs with the same declared TypeName are the same type by definition. The short-circuit avoids descending into the cyclic field graph (which would stack-overflow without a separate cycle guard).

## Threading model

`INamedTypeFieldRegistry` is threaded explicitly as a parameter through:

```
GraphBuilder.SolveCore
  → SolvingFunctions.Destruction(nodes, hasOpt, namedTypeRegistry)
      → DestructionRecursive(node, namedTypeRegistry)
          → ThrowIfRecursiveTypeDefinition(node, namedTypeRegistry)
              → TryRepairOptSourcedCycle(node)
              → TryPromoteCSDescendantToStructBound(node)
      → PropagateTypeNamesAcrossSnapshots(nodes, namedTypeRegistry)
      → LiftMuTypes(nodes, namedTypeRegistry)
  → SolvingFunctions.Finalize(..., namedTypeRegistry)
      → FinalizeRecursive(node, namedTypeRegistry)
          → ThrowIfRecursiveTypeDefinition(node, namedTypeRegistry)
```

No globals. Concurrent solves on different graphs are isolated.

## F-bounded polymorphism — algebraic shape

`ConstraintsState` carries an additional optional dimension:

```
RecursiveBound : RecBound?      // F-bound `T <: τ(T)`; currently τ is always StateStruct
StructBound    : StateStruct?   // shim: RecursiveBound.Body as StateStruct
```

The interval becomes `[D..A, cmp, opt, struct⊆S]`. `StructBound = null` ⇒ "no structural upper bound" — semantics identical to non-recursive code. The new dimension is **independent** of `[D..A]`, exactly as `IsComparable` and `IsOptional` are independent flag-dimensions.

### Operator rules on StructBound

| Op    | Rule on StructBound                                  | Justification                  |
|-------|------------------------------------------------------|--------------------------------|
| Lca   | `LcaStruct(S₁,S₂)`; null absorbs (drops bound)       | join — wider bound (∩ fields)  |
| Gcd   | `GcdBound(S₁,S₂)`; null absorbs to other             | meet — richer bound (∪ fields) |
| Unify | `UnifyStruct(S₁,S₂)`; null absorbs; same width req'd | exact width                    |
| Fit   | `T ≤ S` iff `T:Struct`, `Fields(T)⊇Fields(S)`, pointwise covariant ≤ | width subtyping |
| ↓     | `D` if `D≠∅` else `[..,cmp]` else `∅` (S is NOT a default lower) | upper-bound, not lower |
| ↑     | `A` if `A≠∅` else `[..,cmp]` else `Any` (S is NOT a default upper, though structural) | F-bound is independent dimension, not collapsed into [D..A] |

`Lca` widens (intersection of fields) because the result must accept anything either input accepts. `Gcd` narrows (union of fields) because the result is the strongest constraint compatible with both. Symmetric to existing `StateStruct` operators in `Algebra/BaseOperators.md` / `Algebra/BaseOperators.md`.

`S` is exposed only via the dedicated accessor `StructBound(CS)`, used by `Fit` (`FitStructBound`) and call-site dispatch. It is NOT projected into Concretest/Abstractest. Reason: the algebraic invariant `T ∨ CS = T ∨ ↓CS` (Algebra.md §Теоремы) would be violated if `↓CS = S` for `T:primitive` — the result `T ∨ struct = Any` would silently lose the F-bound and poison subsequent LCA chains. F-bound is a third independent dimension orthogonal to `[D..A]` (peer to `IsComparable`, `IsOptional`), not a fallback projection.

### Ownership

`StructBound = S` is owned by exactly one `ConstraintsState` instance. `S.Fields` may contain `RefTo` back to its owner, but `S` itself is never shared between two CS objects. Merges that combine two CSs create a fresh `S` (via `GcdBound`) with self-refs redirected to the new owner.

## Contractivity invariant

For any `CS` with `StructBound = S`: every cycle from `S.Fields` reaching `CS` MUST cross a `StateOptional` or `StateArray` constructor, AND every occurrence of `RefTo(CS)` inside `S.Fields` MUST be at a strictly **covariant** position (i.e., NOT under a function-argument constructor).

**(C1) Optional/Array break** — every back-edge must traverse `StateOptional` or `StateArray`. Without this the type is non-contractive (e.g., `T <: {self:T}`). `StructDescendantClosesContractively` and `StructHasSelfRef` enforce this at lift time and slot-promotion time. Array recursion is supported on both annotated and unannotated forms: `case StateArray arr: if (fromStruct) break;` in `ThrowIfRecursiveReq` treats Array as a contractive constructor when reached from inside a struct.

**(C2) Covariance restriction** — the `T` in `S.Fields` must appear only at covariant positions. Concretely: forbid `op: (T) -> R` (function-argument is contravariant — a negative occurrence of `T`). Without this, F-bounded subtype check is undecidable in worst case (Pierce, *Bounded Polymorphism is Undecidable*, POPL 1992). With covariance-only, the fragment is **Amadio–Cardelli equirecursive subtyping** (1993) — decidable in `O(n²)`.

Contractivity is checked at the moment `StructBound` is first formed (during cycle-rescue lifting), not deferred. Deferring would let Pull/Push iterate on a non-contractive bound and potentially diverge (lattice height becomes unbounded if a back-edge has no Optional/Array break).

### Sufficiency theorem — contractivity checks guarantee termination

**Theorem (C1 + C2 sufficiency)**: if a struct bound `S` passes both
contractivity checks (C1: every back-edge crosses Optional/Array; C2: every
self-reference at covariant position), then Pull/Push iteration on `S`
terminates in `O(depth(S))` steps.

**Proof** (potential function argument).

Define a potential function `Φ : TicNode → ℕ` over the StructBound's reachable
subgraph:

```
Φ(node) = (number of Optional/Array boundaries on the longest path from node
           back to the cycle root WITHOUT visiting any boundary twice)
```

For a contractive bound:
- `Φ(cycle_root) = 0`.
- Each Optional/Array boundary crossing increases the path depth.
- Since every back-edge crosses Optional/Array (by C1), traversal "descends"
  through these boundaries; `Φ` is well-defined and finite.

**Termination by potential decrease**: each Pull/Push step on a node `n` in
the StructBound subgraph operates on `n.State` and possibly propagates
constraints to children of `n`. Children at the **next composite level** have
strictly smaller `Φ` (they've crossed one fewer Optional/Array boundary from
the current position).

The iteration can re-enter a node only if some external edge brings new
information AND the node's potential allows progress. By monotonicity of the
algebraic operators (P3 Monotonicity from TicProofs.md), state can only
narrow. On a finite domain (bounded by depth(S) Optional/Array crossings),
narrowing terminates.

**Why C2 (covariance) is needed**: at contravariant positions (function arg),
the algebraic direction reverses — subtyping flips. Without C2, a recursive
type appearing at contravariant position would require resolving an
unbounded chain of supertype constraints, which is undecidable in general
(Pierce '92). C2 restricts the fragment to Amadio–Cardelli decidable
equirecursive subtyping (1993).

Therefore: C1 ensures finiteness of the depth-descent argument; C2 ensures
decidability of the underlying subtype check. Together, they guarantee
termination in `O(depth(S))` Pull/Push steps. ∎

### Empirical witness

The full test suite includes recursive named types (`type t = {v:int, next:t? = none}`,
mutually-recursive structures, optional-chained access on recursive types).
All such tests pass without timeout or non-termination. The contractivity
checks have never been observed to allow a non-contractive bound through;
when they reject (e.g., `type bad = {self:bad}` without Optional break),
TIC produces a clean "Recursive type definition" error rather than diverging.

## LiftMuTypes algorithm

Runs after Destruction has stamped TypeName on uniquely-matchable cycle roots, before Finalize.

1. For each toposorted node, walk into `StateOptional` and other composite states (visit-mark dedup).
2. For each `StateOptional(elem)` encountered, examine `elem.State`:
   - Must be `StateStruct` with `TypeName == null` (otherwise it's already nominally sealed),
   - `IsOptionalSourced == true` (originated from `?.`),
   - `elem.IsMutable == true` (not already locked),
   - `StructHasSelfRef(s, elem) == true` (C1: the struct actually self-references),
   - `StructFieldsSubsetOfAnyRegistered(s, registry)` if a registry is present (gate against over-lifting).
3. If all hold: set `s.IsFrozen = true`, build `ConstraintsState.Empty` with `StructBound = s`, replace `elem.State` with that CS.
4. Collect every node that now holds a `StructBound`. For each remaining degenerate opt-only cycle that points at no struct content, redirect its head to a sibling F-bound holder via `StateRefTo` (`TryRedirectDegenerateOptCycle`).

`SolveCore` treats a successful lift as proof that the function signature carries an F-bounded generic — even if all top-level node `IsSolved` flags are set, `Destruction` returns `false` to force the `Finalize` path that builds `TicResultsWithGenerics`.

## Principal-type theorem

> **(Theorem PT-F).** For an unannotated parameter `n` whose body uses it in expressions that contribute primitive interval `[Dᵢ..Aᵢ]`, structural constraint `Sᵢ`, and flag-bits `(opt_i, cmp_i)`, the **principal type** of `n` is `[D ≤ A, opt, cmp, struct⊆S]` where:
> - `D = ⋁ᵢ Dᵢ` (LCA of descendants, lattice join)
> - `A = ⋀ᵢ Aᵢ` (GCD of ancestors, lattice meet)
> - `S = GcdBound(S₁,…,Sₖ)` (field union — meet on the structural bound lattice)
> - `opt = ⋁ᵢ optᵢ`, `cmp = ⋁ᵢ cmpᵢ`
>
> The interval is **non-empty** iff `D ≤ A` AND no element of `D` is excluded by `S` (`D` is either struct with `Fields(D) ⊇ Fields(S)` or null/primitive-with-S=null). When non-empty, this principal type is **unique** up to lattice equality.

The theorem makes explicit that:
- D and S are independent dimensions that may both be present;
- non-emptiness is a three-way predicate on `(D, A, S)`, not just `D ≤ A`;
- inference is complete (the principal type always exists when the body is well-typed).

## Theoretical references

- Cardelli, Wegner. *On Understanding Types, Data Abstraction, and Polymorphism.* CSUR 1985.
- Canning, Cook, Hill, Olthoff, Mitchell. *F-Bounded Polymorphism for Object-Oriented Programming.* FPCA 1989.
- Pierce. *Bounded Polymorphism Is Undecidable.* POPL 1992.
- Amadio, Cardelli. *Subtyping Recursive Types.* TOPLAS 1993.
- Hosoya, Pierce. *Regular Expression Pattern Matching.* TOPLAS 2003.
- Pottier, Rémy. *The Essence of ML Type Inference.* In ATTAPL, 2005.
- Pierce. *Types and Programming Languages.* MIT Press 2002 (TAPL §20, §26, §28).
