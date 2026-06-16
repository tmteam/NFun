# NFun Collections

**Status:** partially shipped (see `## Implementation status` below).
**Scope:** new mutable collection types for lang-mode. Backward-compatible for expression-mode.
**Mode policy:** `int[]` and friends behave **differently** in lang-mode vs expression-mode. This is intentional and mirrors the existing struct-mutability split.

---

## Implementation status

| What | Shipped? | Notes |
|---|---|---|
| `BaseFunnyType.List` + runtime (`IFunnyList`, `MutableFunnyList`) | yes (Stage 2.2) | `List<object>`-backed, value equality |
| CLR ↔ list converter (`System.Collections.Generic.List<T>` ↔ `MutableFunnyList`) | yes (Stage 2.2) | Both directions |
| `list(...)` factory function | yes (Stage 2.3) | Arities 1..8 registered as overloads |
| Lang-mode `[1,2,3]` literal → `list<T>` | yes (Stage 2.3) | ee-mode literal stays `T[]` |
| `[]` empty literal in lang-mode | yes | Bare `out = []` resolves to `list<Any>` (Stage 2 default). With annotation (`out:int[] = []`) or first-usage context (`a = []; a.concat([1,2,3])`) infers the element type. For other empty collections use `default` (`out:set<int> = default`). |
| LINQ on lists (`count`, `map`, `filter`, `fold`, `reverse`, `slice`, `concat`, indexing, …) | yes — via subtyping | TIC rule `list<T> ≤ T[]` lets existing array-typed LINQ accept lists. No per-function overloads, no `Enumerable<T>` typeclass yet. |
| For-loop `for x in <list>` | yes | `ForExpressionNode` iterates `IFunnyArray`/`IFunnyList`/`IEnumerable` |
| Cross-kind equality `list == array` | yes | `TypeHelper.AreEqual` treats both as one equivalence class, element-wise |
| Reassignment `out:list<T> = <array result>` | yes | `VarTypeConverter` handles array→list cast at write site |
| `list<T>` / `array<T>` / `fixedArray<T>` / `enumerable<T>` **type annotation syntax** | **no — deferred** | Parser changes intentionally postponed; type annotations still use `int[]` |
| `Array<T>` (mutable, lang-mode `int[]`) — write `a[i]=v`, `add`, `remove` | no — Stage 3 | TIC state class (`StateCollection(Array)`) exists but is not yet user-reachable |
| `FixedArray<T>`, `Set<T>`, `Map<K,V>` | no — Stages 3-5 | Lattice slots reserved |
| Cross-mode user-function call diagnostic (D3) | no — Stage 3+ | Not pinned today |
| `Enumerable<T>` typeclass / `ConstructorBound` field | no | Superseded by the `list<T> ≤ T[]` shortcut for Stage 2. Will be revisited when `Set`/`Map` arrive |

### Where the implementation diverged from the original plan

1. **LINQ migration is via TIC subtyping, not via `Enumerable<T>` typeclass.** The plan was to migrate every LINQ signature to `Enumerable<T>` (D1 in `Specs/Stage2Plan.md`). Instead Stage 2.5 added a single algebraic rule `list<T> ≤ T[]` in Pull/Push/Destruction (`Apply(StateArray, StateCollection)`), and lang-mode lists flow into existing `T[]`-keyed LINQ functions transparently. Pros: no per-function rewrite, no `ConstraintsState.EnumerableArgNode` field. Cons: LINQ results still come back as `T[]` (not `list<T>`); `Set`/`Map` will need the proper typeclass when they arrive.
2. **Asymmetric runtime cast.** TIC subtyping is one-way (`list ≤ array`), but `VarTypeConverter` also handles `array → list` for the lang-mode mutable-variable accumulator pattern (`out:list<T> = [] ; out = concat(out, …)`).
3. **Empty literal `[]` defaults to `list<Any>` (lang-mode) without context.** Bare `out = []` resolves cleanly. With annotation or first-usage context the element type narrows: `out:int[] = []` → `int[]`; `a = []; b = a.concat([1,2,3])` → `a:list<Int32>`. For non-list empty collections use `default`: `out:int[] = default` → `[]`, future `out:set<int> = default` → empty set. `[]` itself binds to ee-mode `StateArray` or lang-mode `StateCollection(List)` based on the `IsLangMode` dialect flag.
4. **`list<T>` type-annotation syntax skipped.** User direction — annotations stay on `int[]` for now. The TIC state and runtime container both work; only the parser surface for `list<T>` / `array<T>` / `fixedArray<T>` / `enumerable<T>` is deferred.

---

## Design constraints (taken as given)

1. **Lang-mode `int[]` is mutable Array** — `a:int[] = [1,2,3]; a[1] = 42` must work.
2. **All lang-mode collections are invariant** in their element type for **function-parameter Liskov substitution** (a `list<int>` is not a `list<real>` at a call site, even though `int ≤ real`). For **LCA in expression position**, the implementation does element-wise widening: `LCA(list<A>, list<B>) = list<LCA(A, B)>`, which collapses the element to `Any` when A and B are incompatible (e.g. `int` vs `text`) — the container kind itself stays. This is a deliberate trade-off: strict invariance would collapse the whole container to `Any` and lose the ability to iterate the result; element-wise widening keeps the container shape (you can still `.count()` / `.toArray()` etc.) at the cost of an `Any`-typed element.
3. **Expression-mode is unchanged.** `int[]` stays read-only and covariant there. The 13 983 existing tests must continue to pass.

## Type hierarchy

Subtype relation (subtype on the right, supertype on the left):

```
Enumerable<T>    iteration only           (no instances; abstract)
   ↑
FixedArray<T>   + indexed read a[i]       (no instances; abstract)
   ↑
Array<T>        + indexed write a[i]=v    (mutable, fixed-length)
   ↑
List<T>         + add/remove/clear        (mutable, growable)
```

Plus orthogonal:

```
Enumerable<T>
   ↑
Set<T>          + contains O(1)           (mutable, no duplicates, no order)
```

```
Enumerable<{key:K, value:V}>
   ↑
Map<K,V>        + getKey, setKey methods  (mutable, hashed; no [k] syntax)
```

`queue`, `stack` — postponed to Stage 5+. Possibly thin wrappers over `List<T>` with restricted API.

### Constructor lattice

A single ordinal lattice of "what kind of collection" — independent of element type:

```
Enumerable
  ├── FixedArray
  │     └── Array
  │           └── List
  └── Set
  └── Map
```

`Enumerable` — **abstract** constructor. Cannot be instantiated directly; used as a constraint in function signatures. Like `I96` in the primitive lattice.

`FixedArray` — **concretely instantiable** via the factory `fixedArray(1, 2, 3, ...)` (parallels `list(...)`, `set(...)`). Cannot satisfy `MutableCollection` or `IndexedMutable` constraints — read-only after creation.

Concretest descent rule for `Enumerable`:
- lang-mode preferred concrete → **`List`**.
- expression-mode preferred concrete → **`Array`** (the existing immutable backed-by-CLR-array).

### Variance table

Per constructor, per type argument:

| Constructor    | Element variance | Why                          |
|----------------|------------------|------------------------------|
| Enumerable<T>  | invariant        | uniform rule (lang-mode)     |
| FixedArray<T>  | invariant        | uniform rule (lang-mode)     |
| Array<T>       | invariant        | mutation requires invariance |
| List<T>        | invariant        | mutation requires invariance |
| Set<T>         | invariant        | hash + uniform rule          |
| Map<K,V>       | invariant, invariant | mutation + hash          |

Expression-mode `int[]` continues to use the existing `StateArray` (covariant) — separate state class, separate code path. No conflict.

## Lang-mode key semantics

### Syntax for type annotations

- `int[]`  ≡  `Array<int>`   (mutable fixed-size).
- `list<int>`, `set<int>`, `map<int, text>` — explicit.
- `enumerable<int>`, `fixedArray<int>` — usable in function signatures (parameters can be abstract); cannot be a literal target.

### Literal default

- `[1, 2, 3]` — generic literal with **preferred constructor = List** in lang-mode (= **Array** in expression-mode). Element preferred = `Int32` (per current dialect logic).
- The literal binds to whatever the context demands:
  ```
  a:int[]      = [1,2,3]   # binds as Array<int>
  a:list<int>  = [1,2,3]   # binds as List<int>
  a:set<int>   = [1,2,3]   # binds as Set<int>      (via .toSet() conversion under the hood)
  a            = [1,2,3]   # no context → List<int> (lang-mode preferred)
  ```
- This makes `[…]` symmetric to `42`: a generic-constant resolvable by context, with a sensible default.

### Per-stage defaults

| Stage | lang-mode `[1,2,3]` default | ee-mode `[1,2,3]` default |
|---|---|---|
| 2 (List lands) | **List<T>** | Array<T> (unchanged) |
| 3+ | List<T> | Array<T> (unchanged) |

ee-mode literal default never changes — it stays `Array<T>` (the existing immutable backed-by-CLR-array) to preserve backward compatibility with all expression-mode code.

### Empty literal

`[]` — generic empty collection literal, binds to the dialect's preferred constructor:
- With context `a:list<int> = []` → `List<int>`. Works.
- With context `a:int[] = []` → `Array<int>` in lang-mode, `Int32[]` in ee-mode.
- Without context — use the **`default` keyword** instead: `out:int[] = default` resolves to `[]`. `default` follows the declared type and avoids the TIC eager-resolution problem.

`[]` itself never gets deferred-inference machinery — the `default` keyword covers the no-context case cleanly without changes to TIC's resolution model.

### Indexed write

```
a:int[] = [1, 2, 3]
a[1] = 42                # rebinds slot
```

Parser change: extend the existing assignment grammar (`s.field = v` already works for mutable struct) to accept `expr[index] = v` when expr's type is `Array<T>` or `List<T>` (anything satisfying `IndexedMutable<T>` typeclass).

### Mutation methods

On `Array<T>`:
- `a[i]` — read.
- `a[i] = v` — write.
- `count()`, `contains(x)`, all LINQ — via `Enumerable<T>`.
- Length is fixed. No `add`/`remove`.

On `List<T>` (in addition to Array's API):
- `a.add(x)`, `a.addAll(xs)`, `a.remove(x)→bool`, `a.removeAt(i)→T?`, `a.removeLast()→T?`, `a.clear()`.

On `Set<T>`:
- `s.add(x)→bool`, `s.remove(x)→bool`, `s.contains(x)→bool`.
- No indexed access.

On `Map<K,V>` — **method-only**, no `[...]` syntax (neither read nor write):
- `m.get(k) → V?`, `m.tryGet(k) → {value:V, success:bool}` — read.
- `m.setKey(k, v)`, `m.tryAddKey(k, v) → bool` — write.
- `m.removeKey(k) → V?`, `m.tryRemoveKey(k) → {value, success}` — remove.
- `m.containsKey(k) → bool` — membership.
- `m.keys() → Enumerable<K>`, `m.values() → Enumerable<V>`, iteration yields `{key:K, value:V}`.

The `Key` suffix on mutating methods is deliberate: avoids the `(name, arity)` registry collision with `set.tryRemove(item) → bool` (Set has the same arity but operates on element, not key). Uniform `Key` suffix across `setKey / tryAddKey / removeKey / containsKey / tryRemoveKey` keeps the surface predictable.

Rationale for no `[...]` syntax: `a[i] = v` keeps its strict TIC signature `target ≤ mutArr<T>`, `i : Int32` — only mutable Array/List satisfy. Extending to Map<K,V> would require relaxing the index type and adding a Map branch to the upper bound, which conflicts with the project's "no special cases" rule without the full typeclass-dispatch infrastructure. Map's keys can be any Hashable type (text, named-struct, …) — they don't read as "index". Symmetry: if write is out then read is out too — asymmetry is worse than method verbosity.

### LINQ via typeclasses (constraint predicates)

`count`, `contains`, `map`, `filter`, `fold`, `first`, `reverse`, `sort` etc. are written **once** with constraint `T : Enumerable<E>`. NFun's TIC matches the constraint at the call site; the function registry picks the concrete impl based on the runtime type of the first argument.

This is path (b) from the prior review — typeclass-as-constraint, **not** vtable dispatch.

### Iteration

`for x in xs:` works for any `xs: Enumerable<T>` (so: array, list, set, map keys, etc.).

`for kv in m:` for a map yields `{key:K, value:V}`. Destructuring `for k, v in m:` — Stage 5+.

### Conversions

- `T[].toList()` — copies into a new `List<T>`.
- `List<T>.toArray()` — copies into a new `Array<T>` (lang-mode mutable) or fixed array (ee).
- `Enumerable<T>.toSet()` — drops duplicates and order.
- Implicit conversion only via subtyping (List → Array → FixedArray → Enumerable). The other direction requires an explicit `.toX()`.

### Alias semantics

Lang-mode collections are reference types (backing `System.Collections.Generic.List<T>`, `HashSet<T>`, `Dictionary<K,V>`). `b = a; b.add(x)` → `a` sees the mutation. Same model as mutable struct field assignment already in lang-mode.

---

## TIC implementation sketch

### Scope of the refactor

`StateStruct` keeps its existing shape — named-field `Dictionary<string,TicNode>`, open-row width subtyping, coinductive equality. It is structurally different from positional collections and gets no benefit from sharing a base class with them. Migrating it under `CompositeArg[]` would erase the field names that `MergeStructs` / `UnionStructFields` rely on. Struct stays as-is.

`StateFun` likewise stays as-is. Its position/arity machinery is specialised enough that the marginal sharing doesn't pay off.

The refactor scope is **positional ordered collections**: existing `StateArray` plus new `StateList`, `StateSet`, `StateMap` (Stages 2-4). They share constructor, ordinal positional args, and uniform variance — that's where a base class actually consolidates code.

### StateComposite

Introduce abstract `StateComposite` in `src/NFun/Tic/SolvingStates/` for positional collections only:

```csharp
abstract class StateComposite : ICompositeState {
    public abstract ConstructorKind Constructor { get; }
    public abstract CompositeArg[] Arguments { get; }
}
record CompositeArg(TicNode Node, Variance Variance);
enum Variance { Invariant, Covariant }   // Contravariant not needed at this stage
```

### Two `Array` states — ee vs lang

There are **two distinct state classes** for what the user sees as `int[]`:

- `StateArray` — existing expression-mode immutable array. Element is **covariant**. All existing TIC machinery (LCA, decomposition, Pull/Push) keeps working unchanged. No mode flag inside the algebra.
- `StateCollection` with `ConstructorKind.Array` — new lang-mode mutable fixed-size array. Element is **invariant**. Extends `StateComposite`. (Pre-Stage-2.1b: this was a separate `StateMutableArray` class. After 2.1b: data-driven via the unified `StateCollection`.)

The parser chooses which state to emit based on the dialect at parse time. From TIC's perspective they are different types, just as `StateCollection(Array)` and `StateCollection(List)` are distinguished by their `Constructor` field in Stage 2. No branching on dialect inside algebraic operators.

`StateArray` does NOT migrate under `StateComposite`. It stays as-is to guarantee Stage 1 is truly behaviour-preserving for expression mode.

All single-arg lang-mode collections (`ConstructorKind` ∈ {`List`, `FixedArray`, `Array`, `Set`, future `Queue`, `Stack`}) live in the unified `StateCollection` class — Stage 2.1b refactor collapsed N would-be-subclasses into one to avoid combinatorial blow-up in `IStateFunction` / `StagesExtension`. Two-arg `Map` keeps a separate future class because its structural shape differs.

### Constructor lattice

Mirror the existing primitive lattice machinery. `Specs/Tic/ConstructorLattice.md` (new) will describe LCA/GCD over constructor ordinals.

```
Ord 0: Enumerable      (abstract)
Ord 1: FixedArray      (abstract)
Ord 2: Array           (concrete)
Ord 3: List            (concrete)
Ord 4: Set             (concrete)
Ord 5: Map             (concrete)
Ord 6: Fun             (concrete, separate branch — not Enumerable)
Ord 7: Struct          (concrete, separate branch)
```

`Concretest(Enumerable)` returns `List` — there is no dialect branching inside the algebra. The dialect choice between "lang creates List by default" and "ee creates Array by default" happens at the **parser** level (which state class the literal binds to), not at TIC-resolution time. From TIC's perspective the literal arrives as a concrete `StateList` or `StateArray` already; abstract constructors are only used in function signatures (`Enumerable<T>`), not in literals.

### LCA decomposition rule (simplified for this stage)

```
LCA(F<A1, A2, ...>, G<B1, B2, ...>):
    C = ConstructorLCA(F, G)             # lattice climb
    if C == Any: return Any
    if F == G:
        # same constructor — element-wise rules from variance table
        result[i] = LCA(Ai, Bi)           if covariant
                  = Ai                    if Ai == Bi and invariant
                  = Any                   if Ai != Bi and invariant
        return C<result...>
    else:
        # different constructors landed on a common ancestor — we cannot
        # compose elements without variance climbing on each argument.
        # Stage-0 decision: return abstract ancestor with element=Any.
        return C<Any, Any, ...>
```

This is **strictly simpler** than full variance-climbing. We accept that some LCAs collapse to `Any` where a fuller system would preserve element info. That's the trade for tractable implementation.

### GenericConstrains updates

Add to `src/NFun/Interpretation/Functions/GenericConstrains.cs`:

```csharp
public static readonly GenericConstrains Enumerable    = new(...);  // satisfied by all collection states
public static readonly GenericConstrains FixedArray    = new(...);  // satisfied by Array, List
public static readonly GenericConstrains IndexedMutable = new(...); // satisfied by Array, List
public static readonly GenericConstrains MutableCollection = new(...); // satisfied by List, Set, Map
public static readonly GenericConstrains Hashable      = new(...);  // for set/map element types
```

Each constraint becomes a predicate on `StateComposite.Constructor`. No vtable, no abstract instantiation. Constraint satisfaction is a lattice-fit check.

---

## Open questions (decide before Stage 1)

1. **Mutable struct alignment.** **Decided: stay separate.** `s.field = v` (`FieldAssignExpressionNode`) and `a[i] = v` (`IndexedAssignExpressionNode`) keep their dedicated parser+runtime paths. Rationale: (a) field name is a compile-time string statically resolved by TIC; indexed key is a generic `int` validated only at runtime — different layers of the type system; (b) error messages need to be specific ("struct not initialized" vs "index out of range"); (c) only 2 cases — premature abstraction. The TIC-level `IndexedMutable<K,V>` typeclass (when introduced in Stage 4+) is **collection-only**; structs do not participate.

2. **`a == b` for collections.** **By value.** Two collections are equal iff their constructor matches and their elements pairwise equal (using element's own `==`). Concretely:
   - `Array<T>` / `List<T>` / `FixedArray<T>` — same length AND same elements in same order.
   - `Set<T>` — same cardinality AND same element set (order-independent).
   - `Map<K,V>` — same key set AND value equal per key.
   - Cross-constructor `==` is allowed when LCA fits (e.g. `list<int> == array<int>` compares element-wise). When LCA collapses to `Any`, `==` falls back to reference equality.
   - **Mutation invalidates prior equality.** `a = [1,2,3]; b = [1,2,3]; a == b → true; a.add(4); a == b → false`. This matches user intuition for mutable value comparison.

3. **Default values for `a:list<int>` (no initializer).** **Decided: `[]` (empty collection).** Applies uniformly: `list` → empty `MutableFunnyList`; `array` → empty mutable array (Stage 3); `set` → empty `HashSet` (Stage 4); `map` → empty `Dictionary` (Stage 5). Already implemented for `List` (`IFunnyVar.GetDefaultValueOrNullFor`); future composites follow the same rule. Pairs with debt #12 — when the unified default-value protocol lands, the rule is "empty collection per constructor".

4. **Hash/equality for element types of Set/Map.** **Decided: Immutable typeclass** (renamed from Hashable to capture the fundamental property — hashing follows from immutability). **Initial scope: primitives only** — `bool`, all numeric primitives (`U8..I64`, `Real`), `char`, `text` (runtime-immutable string backing), `IPAddress`. Everything else is rejected at the call site of `set(...)` / `__mkMap(...)` with FU580. **Recursive extension** to `FixedArray<T>`, `Fun(...)` (identity hash), `Optional<T>`, future frozen-struct — tracked in [issue #129](https://github.com/tmteam/NFun/issues/129). Rationale: deep immutability is the correct invariant for hash-based containers (mutation of any inner part breaks the hash); the recursive predicate is a focused follow-up that doesn't block the primitive use case.

5. **CLR interop.** External users (Sonica) consume `IFunnyVar` via converters. `BaseFunnyType.List`, `.Set`, `.Map` — new enum values. Migration: existing switches throw on unknown; document the upgrade procedure.

6. **`for k, v in map:` destructuring.** Stage 5+. Until then `for kv in map: kv.key … kv.value`.

7. **Iteration mutation.** **Decided: runtime error.** `for x in a: a.add(y)` throws `FunnyRuntimeException("collection modified during iteration")`. Rationale: (a) backing `System.Collections.Generic.List<T>` already throws `InvalidOperationException` on enumerator invalidation — don't mask it; (b) snapshot semantics hide bugs (the obvious intent `for x in a: a.add(x)` would otherwise terminate silently and produce surprising results); (c) "fail loudly" is the project default. When the user genuinely wants to mutate during iteration they write `for x in a.toList(): a.add(x)` explicitly.

8. **Performance budget.** Named types added 8-16% Build regression per memory. Each new BaseFunnyType + each new state class adds dispatch cost. Set a budget: ≤5% Simple-Build regression per stage, measured via QuickBench.

9. **`set` identifier collision.** `ArrayGenericFunctions.cs:347` already registers `set` as the immutable update built-in `set(arr, index, value)`. Stage 4's `set(1,2,3)` factory collides. Decision needed before Stage 4: rename the existing built-in (candidate: `updated`) OR pick a different factory name (candidate: `setOf`). Stage 0 picks one.

10. **Typeclass-as-constraint with structural T.** `Enumerable<T>`, `IndexedMutable<T>`, `MutableCollection<T>`, `Hashable<T>` all carry a type variable. Stage 0 must spec how `GenericConstrains` stores the inner T — either a new `TicNode` carrier (real TIC change) or a constructor-only predicate with T recovered post-hoc from the matched composite state. Choice affects every LINQ signature in Stage 2.

11. **`Map<K,V>` literal syntax.** Deliberately undecided. No literal token is reserved in advance — when Stage 5 lands, we evaluate options against the actual parser surface at that time. Could be `[k => v, ...]`, `#{k: v}`, factory-only `map([k,v], [k,v])`, or something else. Don't paint a corner that doesn't exist yet.

---

## Staging summary (revised)

| Stage | Scope | Effort | User-visible |
|---|---|---|---|
| **0** | Design (this doc), specs draft, no code | 1-2 weeks | nothing |
| **1** | `StateComposite` base for positional collections; variance table; constructor lattice scaffolding. StateStruct and StateFun stay as-is. `StateArray` (ee, covariant) stays as-is. New `StateList` defined and ready for Stage 2. No new public API. | 3-4 weeks | nothing |
| **2** | `List<T>` end-to-end (read-only API): TIC state, parser, factory `list(1,2,3)` and `fixedArray(1,2,3)`, type syntax `list<T>`, Enumerable<T> constraint, LINQ rewritten through Enumerable. Lang-mode `[1,2,3]` default → List. ee-mode unchanged. | 4-5 weeks | `list<int>` works, LINQ on lists |
| **3** | Mutation: introduce `StateMutableArray` (lang-mode `int[]`); parser change for `a[i] = v`; list `add`/`remove`/`removeAt`/`clear`. IndexedMutable + MutableCollection typeclasses. | 4-6 weeks | `int[]` mutation in lang, list methods |
| **4** | `Set<T>`. Hashable typeclass. Set factory (name TBD — collides with existing `set(arr,i,v)` built-in; resolve in Stage 0). | 3-4 weeks | sets |
| **5+** | `Map<K,V>` (syntax under discussion — literal token NOT reserved in advance), deferred empty-literal `[]` inference, `for k,v in m:` destructuring, queue/stack as sugar, possible HKT first-class. | look later | maps, ergonomics |

**Total to Stage 4 (sets working): ~16-21 weeks of focused work.** Map is its own multi-week project.

---

## Risks

1. **Performance regression on Simple-Build.** Each new BaseFunnyType adds branches to hot paths. Mitigation: QuickBench gate at end of each stage.
2. **Cross-mode user confusion.** "Why is `a[i] = v` rejected in ee-mode?" Mitigation: clear error message routing this to mode docs.
3. **External converters break.** New enum values → external `switch` falls through. Mitigation: enum values are additive; document in release notes; add a tested conversion helper for Sonica-style consumers.
4. **TIC complexity.** TicTechnicalDebt.md will grow. Mitigation: each stage adds at most one new debt item (`// WORKAROUND:`) with documented plan.

## Acceptance gate for Stage 0

- This document reviewed and approved.
- `Specs/Tic/ConstructorLattice.md` drafted (LCA/GCD tables).
- `Specs/Statements.md` updated to reflect "all lang-mode collections invariant; `int[]` mutable in lang-mode".
- `Specs/Arrays.md` updated for the constructor hierarchy.
- No code changes.
