# StateCompositeConstraints — Алгебра composite-constraint интервалов

> **Status:** design **v6.1 FINAL** — Stage C.0 (pre-code spec, converged + v6 audit fixes applied).
> Parent: [`/Specs/Collections.md`](../Collections.md), [`Algebra.md`](Algebra.md), [`ConstructorLattice.md`](ConstructorLattice.md).
> Closes: ad-hoc `list ≤ array` subtype shortcuts (Stage 2.5 / B.3) + tech-debt **#14** (`a[i]=v` pin to array). **Не закрывает** debt #11.

## Changelog

- **v6.1** (this version) — applied 4 algebra blockers + 1 regression + 8 nits from v6 dual audit:
  - **§3.1 LCA**: null-Desc/Anc on one side now treated as **identity** (other side wins), mirror `StateExtensions.Lca.cs:19-24`.
  - **§3.2 GCD same-class**: dropped — same formula as §3.3 Unify; cross-class GCD rules retained (different from Unify cross-class).
  - **§3.10 narrower-wins regression**: `CompCS{Anc=K} ∧ StateArray` returns `StateCollection(K, e)` when K is in lattice, not `StateArray(e)`. Mirror `SolvingFunctions.cs:74-78`.
  - **§4.1.4 null-guard**: explicit reject when `GCD_L(Anc, K) == null` (cross-branch K).
  - **§3.8.1**: extended mark constants to 7 (added cross-class + Abstractest).
  - Nits: §3.7 `ConstructorKind?` value equality note; §4.0.3 reject IsComparable+composite non-comparable; §3.6 hashability emission in TicSetupVisitor note; §3.2 cross StateOptional pseudocode; §4.0.1 ConstraintsState row routing explicit; §3.9 citation `ConstructorLattice.md:140-141`; §6.2 step 2 `Visit(FunCallSyntaxNode)`; §10.4 list split EXISTING/FUTURE.
  - Roadmap C.4a: 2 → 2.5d; baseline JSON listed as C.1 prerequisite.
- **v6** — converged design after professor's theoretical analysis (Layer-0 symmetric content vs Layer-2 directional commit) + tree-lattice simplification (predicate axes collapse to interval refinements):
  - **§1**: CompCS = `{Anc, Desc, IsOptional, ElementNode}` — **4 fields** (was 7 in v5). Predicate axes removed:
    - `IsIndexedMutable` ≡ `Anc ≤ array` (interval refinement, not predicate)
    - `IsHashableElement` → ElementNode propagation (existing CS `IsHashable` predicate)
    - `IsMutableCollection` → Stage D (separate per-container methods, no typeclass)
  - **§1.1** Predicate × Constructor satisfaction matrix → **removed** (no predicates)
  - **§3** reorganized into two layers per professor's decomposition:
    - **Layer 0** (symmetric content): LCA / GCD / Unify / Concretest / Abstractest / SimplifyOrNull
    - **Layer 1** (symmetric commit): `GetMergedStateOrNull` / `MergeInplace` — collapse fires here
  - **§4** rewritten as **Layer 2 directional commit** with 4 explicit Apply cells per professor (mirror `ConstraintsState.Apply` pattern):
    - Forward Pull `Apply(CompCS_a, SC(K))`: LCA on Desc only, Anc untouched (Pull invariant)
    - Forward Push `Apply(CompCS_a, SC(K))`: only preconditions + element merge, no state mutation
    - Reverse Pull `Apply(SC(K), CompCS_d)`: collapse CompCS_d to concrete SC(K)
    - Reverse Push `Apply(SC(K), CompCS_d)`: GCD on Anc only, Desc untouched (Push invariant)
  - **§10.4** Stage D mutation API decided — separate per-container methods (list/set/queue/map), **no typeclass machinery**. Honest semantic asymmetry (list.add total/void, set.tryAdd partial/bool, map.set returns prior value).
  - **§13** acceptance criteria updated for v6.

- **v5** — addressed 6 v4 blockers; A-B2 / A-B3 partially closed (this v6 corrects fully via layered decomposition).
- **v4** — addressed 4 v3 algebra nits.
- **v3** — addressed 11 v2 blockers (LegacyArray regression, SimplifyOrNull enforcement, cross-class cycle guards, etc.).
- **v2** — addressed 21 v1 issues (predicate semantics, nullable Ancestor, CLR boundary section, F-bound deferral).
- **v1** — initial draft.

## 0. Мотивация

Существующая TIC-алгебра асимметрична по доменам:

| Домен | Concrete state | Interval state |
|---|---|---|
| Primitives (I32, Bool, Char, …) | `StatePrimitive` | `ConstraintsState` (`[D..A]`, IsComparable, IsOptional, …) |
| Composites — Array-branch (List, Array, FixedArray) | `StateCollection` | **(отсутствует)** |
| Composites — Set/Queue/Map (Stage D) | n/a (Stage D) | n/a (Stage D) |
| Composites — Fun | `StateFun` | n/a |
| Composites — Struct | `StateStruct` | частично через `ConstraintsState.StructBound` (см. §8) |

Пробел маскируется shortcut-механизмами (`Apply(StateArray, StateCollection)` cross-edges, asymmetric `VarTypeConverter`). Они работают для Stage B, но не масштабируются на Stage D (Set/Map) и не позволяют пользовательские LINQ сигнатуры через inference.

**Решение** — `StateCompositeConstraints` (CompCS) как полноправный гражданин TIC-алгебры, симметричный `ConstraintsState`, но над `ConstructorLattice` вместо `PrimitiveLattice`.

## 1. Определение state

```csharp
public sealed class StateCompositeConstraints : ITicNodeState
{
    // Immutable. Constructed via Create(...) factory; mutation via copy-on-write With*.
    private readonly ConstructorKind? _ancestor;
    private readonly ConstructorKind? _descendant;
    private readonly bool _isOptional;

    private StateCompositeConstraints(
        TicNode elementNode,
        ConstructorKind? ancestor,
        ConstructorKind? descendant,
        bool isOptional)
    {
        ElementNode = elementNode;
        _ancestor = ancestor;
        _descendant = descendant;
        _isOptional = isOptional;
    }

    /// Lattice upper bound (narrowest constructor допустимый). `null` ≡ "нет cap".
    public ConstructorKind? Ancestor => _ancestor;
    public bool HasAncestor => _ancestor.HasValue;

    /// Lattice lower bound. `null` ≡ unconstrained interval.
    public ConstructorKind? Descendant => _descendant;
    public bool HasDescendant => _descendant.HasValue;

    /// Element type variable. Always present.
    /// Под invariance — `↑Element = ↓Element = Element` (identity).
    public TicNode ElementNode { get; }

    /// LCA: OR. Unify: OR. (расширения сохраняются)
    public bool IsOptional => _isOptional;

    // ──────────────────────────────────────────────────────────────────────
    // Factory: SimplifyOrNull-enforced. Single entry point.
    public static StateCompositeConstraints? Create(
        TicNode elementNode,
        ConstructorKind? ancestor = null,
        ConstructorKind? descendant = null,
        bool isOptional = false)
    {
        var candidate = new StateCompositeConstraints(elementNode, ancestor, descendant, isOptional);
        return SimplifyOrNull(candidate);
    }

    // Copy-on-write transformers. Return:
    //   this — if no change
    //   new StateCompositeConstraints — if accepted
    //   StateCollection (via TryCollapseToPoint internally) — if interval collapsed to point
    //   StateOptional(StateCollection) — if collapsed and IsOptional
    //   null — if rejected (interval became empty)
    public ITicNodeState? WithDescendant(ConstructorKind value);
    public ITicNodeState? WithAncestor(ConstructorKind value);
    public ITicNodeState? WithIsOptional(bool value);

    // Internal helper used by all With*. Builds candidate, runs SimplifyOrNull,
    // then TryCollapseToPoint. Returns flat ITicNodeState — collapse is centralised here.
    private ITicNodeState? With(ConstructorKind? a, ConstructorKind? d, bool isOpt)
    {
        var candidate = new StateCompositeConstraints(ElementNode, a, d, isOpt);
        var simplified = SimplifyOrNull(candidate);
        if (simplified == null) return null;
        return simplified.TryCollapseToPoint() ?? (ITicNodeState)simplified;
    }

    /// Trivial collapse — pure function, never mutates anything.
    /// Returns StateCollection (or wrapped StateOptional) when interval is a point.
    public ITicNodeState? TryCollapseToPoint();
}
```

### Алгебраическая интерпретация

CompCS — **интервал** `[Descendant .. Ancestor]` над `ConstructorLattice` + element type variable (invariant) + IsOptional flag.

```
ConstraintsState[Desc..Anc, IsComparable, IsOptional, Preferred, StructBound]
   = интервал PrimitiveLattice + предикаты + опц. структурная граница

StateCompositeConstraints[Desc..Anc, IsOptional, ElementNode]
   = интервал ConstructorLattice + IsOptional + element type variable
```

**Почему нет predicate осей** (vs CS которая имеет IsComparable, Preferred):

1. **Container-shape "предикаты" (IsIndexedMutable, IsEnumerable, IsFixed)** — в lattice дерева коллекций **все они эквивалентны interval refinement'у над `Anc`**:

| "Предикат" | Equivalent interval |
|---|---|
| Enumerable | `Anc=enumerable` (top of lattice — нет cap) |
| IndexedRead | `Anc ≤ fixedArray` |
| IndexedMutable | `Anc ≤ array` |
| Mutable list-only | `Anc=list` |

Поэтому **отдельная ось не нужна** — interval over ConstructorLattice уже несёт эту информацию.

2. **Element-axis "предикаты" (Hashable)** — push'атся на `ElementNode` где они становятся CS predicates (`IsHashable`). CompCS не несёт; механизм propagation тривиален.

3. **Cross-branch "предикаты" (IsMutableCollection — `add` для list ИЛИ set ИЛИ map)** — выглядят как настоящий type class, но в Stage C/D мы **не унифицируем** mutation API между ветками дерева (см. §10.4). Поэтому ось не нужна.

### 1.1 ConstructorLattice (recap)

Дерево (см. [`ConstructorLattice.md`](ConstructorLattice.md)):

```
              any
               |
          enumerable
       /     |     |     \
fixedArray  fixedSet queue fixedmap
    |         |              |
  array      set            map
    |
   list
```

Subtype edges (`≤_L`):
- `list ≤_L array ≤_L fixedArray ≤_L enumerable`
- `set ≤_L fixedSet ≤_L enumerable`
- `queue ≤_L enumerable`
- `map ≤_L fixedmap ≤_L enumerable`

**Element invariance**: `SC(K, T₁)` and `SC(K, T₂)` are unrelated unless `T₁ = T₂` (по unification). Element handling — всегда через `MergeInplace`, не через LCA/GCD.

### 1.2 Hashable propagation (через ElementNode)

Когда signature требует hashable element (Set, Map keys, hash-based collections):
- Constraint emit'ится на ElementNode TIC: `CS{IsHashable=true}` — **existing CS predicate**.
- CompCS не несёт никакой Hashable axis.

Hashability per type — за CS layer (existing `ConstraintsState.IsHashable` check). Lookup table для справки:

| Type | Hashable? |
|---|---|
| Bool, I8-I64, U8-U64 | ✓ |
| Real | ✓ (with NaN caveats — CLR `double.GetHashCode`) |
| Char, Text, Ip | ✓ |
| Any, None | ✓ |
| Optional(T) | ✓ iff T hashable |
| Struct, NamedStruct | ✓ iff all fields hashable (recursive) |
| Fun | ✗ |
| StateCollection (List/Array/Set/Queue/Map) | ✗ (mutable; identity hash unsafe) |
| StateArray (ee-mode immutable) | ✓ iff element hashable |

Эти правила применяются в CS hashability check; **CompCS не участвует**.

## 2. Покрытие сценариев

| Сценарий | State |
|---|---|
| Untyped fn arg `fun foo(xs): xs.count()` | `CompCS{Anc=enumerable, Desc=null, Elem=fresh CS}` |
| Concrete value through Pull (`list<int>`) | refines to `CompCS{Anc=enumerable, Desc=list, Elem=Int}` then Destruction picks Desc → `StateCollection(List, Int)` |
| Signature `fun count<T>(xs: Enumerable<T>)` | `CompCS{Anc=enumerable, Desc=null, Elem=T}` |
| Signature `fun get<T>(xs: IndexedRead<T>, i: int)` | `CompCS{Anc=fixedArray, Desc=null, Elem=T}` |
| `a[i] = v` (closes debt #14) | target slot: `CompCS{Anc=array, Desc=null, Elem=T}` |
| Optional `xs: Enumerable<T>?` | `CompCS{Anc=enumerable, Desc=null, Elem=T, IsOpt=true}` |

## 3. Layer 0 — Symmetric content operators

Pure operations on state values. No nodes, no commit side-effects. Defined in `StateExtensions.*.cs` style, callable from both Layer 1 (`GetMergedStateOrNull`) and Layer 2 (Pull/Push/Destruction Apply).

### 3.1 LCA — `A ∨ B` (join, widens)

#### Same class: `CompCS ∨ CompCS`

**Identity convention** (mirror `StateExtensions.Lca.cs:19-24`): null Desc/Anc on one side means "no information"; other side is identity element. NOT "admits anything" (which would force null on join).

```
LCA(CompCS_A, CompCS_B) under _compCsLcaInProgress:
    # Widen lower bound. null-Desc = identity (other side wins).
    newDesc =
        match (A.Desc, B.Desc):
            (null, null) → null
            (null, D)    → D                          # identity: B's floor survives
            (D, null)    → D                          # identity: A's floor survives
            (D1, D2)     → LCA_L(D1, D2)              # both present → join
    # Widen upper bound. Same identity convention.
    newAnc =
        match (A.Anc, B.Anc):
            (null, null) → null
            (null, A2)   → A2
            (A1, null)   → A1
            (A1, A2)     → LCA_L(A1, A2)
    newElem = MergeInplace(A.ElementNode, B.ElementNode)  # invariance → unify
    newIsOpt = A.IsOpt || B.IsOpt                     # OR (extension preserved)
    return StateCompositeConstraints.Create(newElem, newAnc, newDesc, newIsOpt)
```

#### Cross: `CompCS ∨ StateCollection(K, elemK)`

`SC(K)` — это точка `[K..K]` в lattice. Identity convention same as same-class — null on CompCS side means "use the point K".

```
under _compCsXCollLcaInProgress:
    newDesc =
        if A.Desc == null → K          # identity: K-floor survives
        else → LCA_L(A.Desc, K)
    newAnc =
        if A.Anc == null → K           # identity: K-cap survives
        else → LCA_L(A.Anc, K)
    newElem = MergeInplace(A.ElementNode, elemK)
    return StateCompositeConstraints.Create(newElem, newAnc, newDesc, A.IsOpt)
```

#### Cross: `CompCS ∨ StatePrimitive`

```
case StatePrimitive.Any → StatePrimitive.Any (wrap in StateOptional if A.IsOpt)
case other → StatePrimitive.Any (composite ∨ primitive non-Any → Any top)
```

#### Cross: `CompCS ∨ StateOptional(inner)`

```
return new StateOptional(LCA(A, inner))  # IsOpt forced true on result
```

### 3.2 GCD — `A ∧ B` (meet, narrows; may collapse to point)

Symmetric meet — interval intersection. **May collapse to `StateCollection`** when result is a point.

#### Same class: `CompCS ∧ CompCS`

**Same formula as §3.3 Unify** — interval meet `[LCA(D₁,D₂) .. GCD(A₁,A₂)]` per Algebra.md identity. To avoid duplication: same-class GCD on CompCS **delegates to §3.3 Unify** (mirrors CS precedent where `ConstraintsState.MergeOrNull` is the only meet — no separate same-class GCD).

```
GCD(CompCS_A, CompCS_B) = Unify(CompCS_A, CompCS_B)   # see §3.3
```

(Cross-class GCD rules below are distinct from cross-class Unify: cross-class GCD intersects an interval with a point/concrete state and may collapse; this is different from same-class symmetric interval meet.)

#### Cross: `CompCS ∧ StateCollection(K, elemK)`

`SC(K)` — точка `[K..K]`. Intersection с интервалом — точка K, если K попадает в интервал.

```
under _compCsXCollGcdInProgress:
    if A.HasDescendant && !IsSubtypeOrEqual_L(A.Desc, K): return null   # K below floor
    if A.HasAncestor && !IsSubtypeOrEqual_L(K, A.Anc): return null      # K above cap
    newElem = MergeInplace(A.ElementNode, elemK)
    sc = new StateCollection(K, newElem)
    return A.IsOpt ? new StateOptional(sc) : sc
```

#### Cross: `CompCS ∧ StatePrimitive`

```
case StatePrimitive.Any → return A
case other → return null    # primitive shape ≠ composite shape
```

#### Cross: `CompCS ∧ StateOptional(inner)`

```
inner_meet = GCD(A, inner)
if inner_meet == null: return null
# Result always wraps in StateOptional — RHS is Optional, narrowing preserves wrapping.
return new StateOptional(inner_meet)
```

### 3.3 Unify — `A ⊓ B`

Per [`Algebra.md`](Algebra.md) identity `[D₁..A₁] ⊓ [D₂..A₂] = [LCA(D₁,D₂) .. GCD(A₁,A₂)]`:

```
Unify(CompCS_A, CompCS_B) under _compCsUnifyInProgress:
    newDesc = same as GCD same-class (LCA of floors)
    newAnc  = same as GCD same-class (GCD of caps)
    if newDesc != null && newAnc != null && !IsSubtypeOrEqual_L(newDesc, newAnc):
        return null
    newElem = MergeInplace(A.ElementNode, B.ElementNode)
    newIsOpt = A.IsOpt || B.IsOpt    # Unify accumulates OR
    result = StateCompositeConstraints.Create(newElem, newAnc, newDesc, newIsOpt)
    return result?.TryCollapseToPoint() ?? result
```

**Cross-class Unify** delegates to §3.2 cross-rules (which may collapse).

### 3.4 Concretest — `↓A`

Resolution к конкретному state. **Without dialect smuggling**.

```
Concretest(A) under _compCsConcretestInProgress:
    if A.HasDescendant:
        K = A.Descendant            # use floor
    elif A.HasAncestor && A.Ancestor != Any:
        K = ConstructorLattice.Concretest(A.Ancestor)
    else:
        return A   # abstract residual — dialect default handles (§5)

    elem = A.ElementNode    # invariance: identity
    sc = new StateCollection(K, elem)
    return A.IsOpt ? new StateOptional(sc) : sc
```

**Contract**: never returns null. Returns self-as-residual when unresolvable.

### 3.5 Abstractest — `↑A`

Widen — drop floor, preserve cap.

```
result = StateCompositeConstraints.Create(
    elementNode = A.ElementNode,
    ancestor    = A.Ancestor,    # preserve cap
    descendant  = null,           # drop floor (widest)
    isOptional  = A.IsOptional)
# Widening always succeeds when input A was valid by construction.
assert(result != null)
return result
```

### 3.6 SimplifyOrNull — interval validity check

Single responsibility: confirm interval `[Desc..Anc]` is non-empty.

```
SimplifyOrNull(A): StateCompositeConstraints?
    if A.HasDescendant && A.HasAncestor
        && !IsSubtypeOrEqual_L(A.Descendant, A.Ancestor):
        return null    # empty interval
    return A
```

**Что НЕ делает (vs v5)**:
- Не refine'ит Anc от предикатов (нет предикатов).
- Не push'ит hashability на ElementNode (механизм через CS, не CompCS).
- Не collapse'ит к StateCollection (это делает caller через TryCollapseToPoint).

Это тривиальная функция; она существует чтобы Create / With* имели единый чек.

**Где hashability emit'ится**: `TicSetupVisitor.InitializeGenericType` (line 1797 per §9 C.4a) — при signature requiring `Hashable<T>`, emit `CS{IsHashable=true}` on ElementNode TIC. Apply cells (§4.1) **не** push'ят hashability — они trust что ElementNode уже carry CS predicate when signature demands.

### 3.7 TryCollapseToPoint

Pure function — без мутаций. Возвращает collapsed state или null.

```
TryCollapseToPoint(A): ITicNodeState?
    if A.HasDescendant && A.HasAncestor && A.Descendant == A.Ancestor:
        sc = new StateCollection(A.Descendant, A.ElementNode)
        return A.IsOpt ? new StateOptional(sc) : sc
    return null
```

`A.Descendant == A.Ancestor` — value equality on `ConstructorKind?` (C# nullable enum). Safe because `ConstructorKind` is an enum and nullable equality short-circuits on hasValue mismatch.

**Centralized**: вызывается **только** из (1) `With*` методов (§1), (2) §3.2/§3.3 same-class GCD/Unify. Callers `node.State = compCS.WithX(...)` автоматически получают collapsed state — никакой fan-out.

### 3.8 Cycle guards

Recursive types (`t = list<t>`, `t = enumerable<t>`) — torture-test (TIC debt #5 reprise).

#### 3.8.1 Reserved mark constants

New constants live in `StateCompositeConstraints` class (per-class ownership convention, mirroring `StateCollection.CycleGuard = -57600` in `StateCollection.cs:42` and `IsMutableCycleMark = -58600` / `IsSolvedCycleMark = -58700` in `StateComposite.cs:93,96`):

```csharp
// Range -59000 to -59600 reserved for StateCompositeConstraints (Stage C.0).
public const int CompCsLcaMark           = -59000;
public const int CompCsGcdMark           = -59100;   // cross-class GCD only (same-class delegates to Unify)
public const int CompCsUnifyMark         = -59200;
public const int CompCsConcretestMark    = -59300;
public const int CompCsAbstractestMark   = -59400;
public const int CompCsXCollMark         = -59500;   // cross-class StateCollection guards (LCA/GCD/Unify share)
public const int CompCsXArrayMark        = -59600;   // cross-class StateArray guards (LCA/GCD/Unify share)
```

#### 3.8.2 Per-pair guards (same-class)

```csharp
[ThreadStatic] private static HashSet<(CompCS, CompCS)> _compCsLcaInProgress;
[ThreadStatic] private static HashSet<(CompCS, CompCS)> _compCsGcdInProgress;
[ThreadStatic] private static HashSet<(CompCS, CompCS)> _compCsUnifyInProgress;
```

#### 3.8.3 Per-pair guards (cross-class)

Cross-class operators recurse через Element edges (которые могут содержать другие CompCS):

```csharp
[ThreadStatic] private static HashSet<(CompCS, StateCollection)> _compCsXCollLcaInProgress;
[ThreadStatic] private static HashSet<(CompCS, StateCollection)> _compCsXCollGcdInProgress;
[ThreadStatic] private static HashSet<(CompCS, StateCollection)> _compCsXCollUnifyInProgress;
[ThreadStatic] private static HashSet<(CompCS, StateArray)>      _compCsXArrayLcaInProgress;
[ThreadStatic] private static HashSet<(CompCS, StateArray)>      _compCsXArrayGcdInProgress;
[ThreadStatic] private static HashSet<(CompCS, StateArray)>      _compCsXArrayUnifyInProgress;
```

Cross-class Unify guard catches re-entry that bypasses the GCD entry path (e.g. через StateRefTo aliasing).

#### 3.8.4 Per-node guards (unary)

```csharp
[ThreadStatic] private static HashSet<TicNode> _compCsConcretestInProgress;
[ThreadStatic] private static HashSet<TicNode> _compCsAbstractestInProgress;
```

#### 3.8.5 Guard pattern (mirror StateStruct cycle-rescue)

```csharp
ITicNodeState Lca(CompCS a, CompCS b) {
    var guard = _compCsLcaInProgress ??= new();
    if (!guard.Add((a, b)) && !guard.Add((b, a)))
        return a;    // coinductive: cycle hit → return one side
    try { return LcaCore(a, b); }
    finally { guard.Remove((a, b)); guard.Remove((b, a)); }
}
```

### 3.9 StateArray (legacy ee) — НЕ в lattice

`LegacyArray` deliberately outside `ConstructorLattice` per `ConstructorLattice.md:140-141`:

> *"The legacy `StateArray`, `StateFun`, and `StateStruct` are deliberately outside the constructor lattice — their internals (covariant element / arg+ret split / named-field dict) don't benefit from uniform `CompositeArg[]` representation."*

Reasons:
- Variance mismatch: `StateArray` is **covariant**; all `ConstructorLattice` members are **invariant**.
- Existing `LCA(StateArray, StateCollection)` widens to `StateArray.Of(elemLca)` — adding LegacyArray as lattice sibling would silently change this.
- Lang-mode kinds and ee-mode array are semantically distinct (mutability + variance).

### 3.10 CompCS ↔ StateArray dedicated cross-rules

Since StateArray не в lattice, cross-rules dedicated:

**LCA** (`CompCS ∨ StateArray(elemArr)`):
- Symmetric upward widening. Since `StateArray` is covariant и не имеет lattice-position, результат — Any (top) для interval, либо StateArray(elemArr) если CompCS already constrained к ee-array-compatible kind.
- Element unifies (under invariance vs covariance — мы выбираем invariance для consistency).
- Practical default: `LCA(CompCS, StateArray) = StateArray(LCA(elemA, elemArr))` matches existing `StateExtensions.Lca` behavior.

**GCD** (`CompCS ∧ StateArray(elemArr)`) — **narrower-wins rule** (mirror `SolvingFunctions.cs:74-78`):

```
under _compCsXArrayGcdInProgress:
    # Verify element compatibility
    newElem = MergeInplace(A.ElementNode, elemArr)
    if newElem == null: return null
    # Narrower-wins: if CompCS has a lattice cap (A.Anc is non-null), it
    # represents a NARROWER kind than StateArray's nominal position.
    # Collapse to StateCollection(A.Anc, e) — lang-mode kind survives.
    if A.HasAncestor:
        return new StateCollection(A.Ancestor, newElem)
    # Otherwise CompCS is unconstrained — fall back to ee-array (legacy default).
    return new StateArray(newElem)
```

This preserves existing TIC behavior where cross-merge `StateArray × StateCollection(List)` keeps `StateCollection(List)` identity (lang-mode kind narrower than legacy ee-array). v5 §3.10 unilaterally returned StateArray — v6.1 fixes regression.

- No predicate axes to check (v6 simplification).

**Apply Pull/Push/Destruction**: dispatch via §4 table; calls LCA/GCD above.

**Runtime conversion**: `VarTypeConverter` retains cross-paths (§7).

## 4. Layer 2 — Directional Apply operators

Per-stage Pull / Push / Destruction. Each cell is **directional** with one-sided commit — distinct from §3 symmetric operators. Mirrors existing TIC pattern (`PullConstraintsFunctions.ApplyAncestorConstrains` at `:570-579`).

**Critical distinction**: §3 GCD/Unify computes a meet that may collapse to point (symmetric). §4 Pull/Push refines one side's interval bound and writes to one node's state slot (directional). These are **different operations** — §4 cells do **not** route to §3.3 Unify or §3.2 cross-GCD as primary dispatch.

**Layer 0 reuse**: §4 cells DO use Layer-0 primitives (`LCA_L` / `GCD_L` on lattice kinds, `MergeInplace` on element nodes) and Layer-0 helpers (`SimplifyOrNull`, `TryCollapseToPoint` via `With*`). What distinguishes Layer 2 from Layer 0/1 is the **one-sided commit** (`ancNode.State = ...` or `descNode.State = ...`) and **stage-specific refinement rule** (Pull touches Desc only, Push touches Anc only), not avoidance of Layer-0 helpers.

### 4.0 Apply tables

#### 4.0.1 Forward direction — `Apply(ancestor: CompCS, descendant: RHS, ancNode, descNode)`

| RHS | Pull (desc→anc) | Push (anc→desc) | Destruction |
|---|---|---|---|
| `StateCollection(K, e_K)` | **§4.1.1** | **§4.1.2** | §3.4 Concretest both, MergeInplace |
| `StateArray(e)` | §3.10 cross dedicated | §3.10 cross dedicated | §3.10 cross dedicated |
| `StatePrimitive Any` | no-op | no-op | §3.4 Concretest, prim merge |
| `StatePrimitive other` | **reject** | **reject** | **reject** |
| `ConstraintsState` | coerce CS→`SC(K)` when CS.Desc is `SC`, route to §4.1.1 cell. Else reject. | same routing | same |
| `StateFun` | **reject (explicit)** | **reject (explicit)** | **reject (explicit)** |
| `StateStruct` | **reject (explicit)** | **reject (explicit)** | **reject (explicit)** |
| `StateOptional(inner)` | unwrap, recurse, set IsOpt=true | wrap descendant if anc.IsOpt | wrap/unwrap |
| `CompCS` | §3.3 Unify, install via §4.2 | §3.3 Unify | §3.3 Unify |

#### 4.0.2 Reverse direction — `Apply(ancestor: RHS, descendant: CompCS, ancNode, descNode)`

| LHS | Pull (desc→anc) | Push (anc→desc) | Destruction |
|---|---|---|---|
| `StateCollection(K, e_K)` | **§4.1.3** | **§4.1.4** | Same as forward Destruction |
| `StateArray(e)` | §3.10 reverse | §3.10 reverse | §3.10 reverse |
| `StatePrimitive Any` | no-op | no-op | §3.4 Concretest |
| `StatePrimitive other` | **reject** (non-Any prim не accept composite) | **reject** | **reject** |
| `ConstraintsState` | coerce CS→CompCS-view, recurse | same | same |
| `StateFun` | **reject (explicit)** | **reject (explicit)** | **reject (explicit)** |
| `StateStruct` | **reject (explicit)** | **reject (explicit)** | **reject (explicit)** |
| `StateOptional(inner)` | wrap/unwrap | wrap/unwrap | wrap/unwrap |

#### 4.0.3 CS↔CompCS coercion

Triggered when CS.Descendant is a `StateCollection` (composite-kind concrete state). StateStruct's `StructBound` doesn't participate — different shape.

```
# Reject early if CS carries a real constraint that composites cannot satisfy:
if CS.IsComparable && !IsComparable_K(CS.Desc.Constructor):
    return reject     # e.g. CS{IsComparable=true, Desc=SC(List, ...)} — list not Comparable

CompCS_view = StateCompositeConstraints.Create(
    elementNode = CS.Desc.ElementNode,       # StateCollection has ElementNode
    ancestor    = null,                       # CS.Anc is StatePrimitive — not lattice; drop
    descendant  = CS.Desc.Constructor,
    isOptional  = CS.IsOptional)
# CS predicates IsComparable (after the reject check above), Preferred dropped — primitive-axis,
# not applicable to composites.
```

If CS.Desc is `StateStruct` → coercion not applicable; primitive-vs-composite reject.

`IsComparable_K`: per §1.2 lookup — Comparable kinds in NFun are scalar/text primitives; collections (List/Array/Set/Queue/Map) are not Comparable.

### 4.1 The four critical Apply cells (per professor's rules)

Format mirrors `ConstraintsState.Apply` family: preconditions / content / element merge / commit.

#### 4.1.1 Forward Pull `Apply(CompCS_a anc, StateCollection(K, e_K) desc)`

Pull: фактическое значение `desc` (concrete `SC(K)`) inform'ит ancestor's CompCS slot.

```
preconditions:
    if CompCS_a.HasAncestor && !IsSubtypeOrEqual_L(K, CompCS_a.Ancestor):
        return false                         # K above ancestor's cap

content (LCA on Desc slot — descendant floor raised, ancestor cap UNTOUCHED):
    newDesc = CompCS_a.HasDescendant
        ? LCA_L(CompCS_a.Descendant, K)
        : K
    newState = CompCS_a.WithDescendant(newDesc)   # may collapse to SC via With*
    if newState == null: return false

element merge (invariance):
    if !MergeInplace(CompCS_a.ElementNode, e_K): return false

commit (one-sided to ancestor):
    ancestorNode.State = newState                  # may be CompCS or collapsed SC
    return true
```

**Pull invariant**: `CompCS_a.Ancestor` **never refined** — Pull flows desc→anc; descendants don't carry information about ancestor's upper bound. (Mirror: `ApplyAncestorConstrains` calls `AddDescendant`, never `TryAddAncestor`.)

#### 4.1.2 Forward Push `Apply(CompCS_a anc, StateCollection(K, e_K) desc)`

Push: ancestor's facts pushed down. Push validates descendant satisfies ancestor's cap. **No state mutation.**

```
preconditions (the only checks):
    if CompCS_a.HasAncestor && !IsSubtypeOrEqual_L(K, CompCS_a.Ancestor):
        return false

element push (propagate element-axis constraints down):
    if !StagesExtension.Invoke(this, CompCS_a.ElementNode, e_K):
        return false

commit:
    NO mutation on ancestor (Push never widens ancestor).
    NO mutation on descendant (SC(K) already satisfies the cap; no narrowing needed).
    return true
```

#### 4.1.3 Reverse Pull `Apply(StateCollection(K, e_K) anc, CompCS_d desc)`

Pull в reverse: ancestor concretised to SC(K); descendant CompCS должен concretise к SC(K) downward (collapse).

```
preconditions:
    if CompCS_d.HasAncestor && !IsSubtypeOrEqual_L(K, CompCS_d.Ancestor):
        return false                              # K above descendant's cap
    if CompCS_d.HasDescendant && !IsSubtypeOrEqual_L(CompCS_d.Descendant, K):
        return false                              # K below descendant's floor

content (collapse CompCS_d to concrete SC(K)):
    newState = new StateCollection(K, CompCS_d.ElementNode)
    if CompCS_d.IsOptional: newState = new StateOptional(newState)

element merge (invariance):
    if !MergeInplace(CompCS_d.ElementNode, e_K): return false

commit (one-sided to descendant):
    descendantNode.State = newState
    return true
```

#### 4.1.4 Reverse Push `Apply(StateCollection(K, e_K) anc, CompCS_d desc)`

Push в reverse: ancestor SC(K) — descendant must accept K downward. Narrow descendant's Anc cap.

```
preconditions:
    if CompCS_d.HasDescendant && !IsSubtypeOrEqual_L(CompCS_d.Descendant, K):
        return false                              # K below descendant's floor

content (GCD on Anc — descendant cap narrowed, descendant floor UNTOUCHED):
    if CompCS_d.HasAncestor:
        narrowedAnc = GCD_L(CompCS_d.Ancestor, K)
        if narrowedAnc == null: return false       # cross-branch K (e.g. list vs set)
        newAnc = narrowedAnc
    else:
        newAnc = K
    newState = CompCS_d.WithAncestor(newAnc)       # may collapse via With*
    if newState == null: return false

element merge (invariance — convention: CompCS's element first, mirror §4.1.3):
    if !MergeInplace(CompCS_d.ElementNode, e_K): return false

commit (one-sided to descendant):
    descendantNode.State = newState
    return true
```

**Push invariant**: `CompCS_d.Descendant` **never refined** — Push flows anc→desc; ancestors don't carry information about descendant's lower bound.

### 4.2 GetMergedStateOrNull — symmetric merge identity (Layer 1)

Used by `MergeInplace` (coalesce two nodes into one slot). **Symmetric** — caller does not pick direction.

```csharp
ITicNodeState? GetMergedStateOrNull(ITicNodeState a, ITicNodeState b) {
    // Same-class CompCS: §3.3 Unify (which calls TryCollapseToPoint internally).
    // Cross-class: §3.2 cross-GCD rules (which may collapse to StateCollection).
    // All routes return possibly-collapsed state; no extra check needed by caller.
    if (a is CompCS && b is CompCS) return Unify(a, b);
    if (a is CompCS && b is SC sc) return Gcd_CompCsXColl(a, sc);
    if (a is SC sc2 && b is CompCS cc) return Gcd_CompCsXColl(cc, sc2);
    if (a is CompCS && b is StateArray sa) return Gcd_CompCsXArray(a, sa);
    // ... existing routing for other (a, b) combinations
}
```

**Centralized collapse**: §3.3 Unify and §3.2 cross-GCD both internally call `TryCollapseToPoint` before returning. `GetMergedStateOrNull`'s callers receive either CompCS or StateCollection — no caller-side bookkeeping.

### 4.3 Total dispatch surface

- §4.1: 4 explicit Apply cells (`StateCollection` × Pull/Push × forward/reverse) — the only cells specific to CompCS↔SC at Layer 2.
- §4.0.1/§4.0.2: cells for other RHS classes (~7 × Pull/Push/Destruction × 2 directions = ~42 cells, most explicit reject or trivial).
- §3 Layer 0/Layer 1 operators: ~10 functions (LCA × 4 cross, GCD × 4 cross, Unify same-class, Concretest, Abstractest).

Forward+reverse Apply cells share core logic via private helpers parameterised by direction enum.

## 5. CLR boundary

`fun count(xs: Enumerable<int>): int` — slot должен принимать любую iterable.

### 5.1 `BaseFunnyType.Enumerable = 24`

Новый base type (после уже занятых 0-23, см. `BaseFunnyType.cs:21-59`).

- **CLR input converter**: принимает любой `System.Collections.IEnumerable`, оборачивает в `IFunnyEnumerable` view.
- **CLR output converter**: rejected — function must resolve to concrete via §3.4. Defensive: in normal operation §3.4 maps `Enumerable → List` per `ConstructorLattice.cs:127`.

### 5.2 `FunnyConverter` + `TypeBehaviour` updates

C.4b deliverables:
1. `FunnyConverter.cs`: add `BaseFunnyType.Enumerable` case в `GetInputConverterFor`/`GetOutputConverterFor`.
2. `TypeBehaviour.cs`: append `null` entries at index 24 в `FunToClrTypesMap` (line 133+) и `DefaultPrimitiveValues` (line 160+). Startup assert `FunToClrTypesMap.Length == enum length` fail-fast'ит (per `TypeBehaviour.cs:123-131`).
3. `BaseFunnyType.cs`: add `Enumerable = 24` enum value.

### 5.3 `IFunnyEnumerable` runtime

Pull-up `FunnyType ElementType` from `IFunnyArray` / `IFunnyFixedArray` / `IFunnyMutableArray` to `IFunnyEnumerable` parent interface. All current in-repo implementers already declare ElementType — pull-up non-breaking in-repo.

**Binary-compat caveat**: `IFunnyEnumerable` is public NFun API. External implementors (host apps with custom enumerable types) will need to add `ElementType`. C.4b CHANGELOG note required.

## 6. Runtime interface mapping

| TIC interval | Runtime interface | Required ops | LINQ functions |
|---|---|---|---|
| `Anc=enumerable` (CompCS) | `IFunnyEnumerable` | iterate, Count | count, contains, any, all, first, last, reverse, fold, map, filter, sum, min, max, range (~22) |
| `Anc≤fixedArray` (CompCS) | `IFunnyFixedArray` | + GetElementOrNull | slice, get/`[i]`, chunk, take, skip, find (~10) |
| `Anc≤array` (CompCS) | `IFunnyMutableArray` | + SetAt | `[i] = v` syntax |

**No type class** — single-overload + structural dispatch via TIC (см. §6.2). Runtime polymorphism через `IFunnyEnumerable` hierarchy.

### 6.2 Dispatch design — single overload + TIC structural dispatch

Closes CLAUDE.md tech-debt #1 ("Stage 2 dispatch path for Enumerable<T>").

**Problem**: existing `IFunctionRegistry` keys by `(name, arity)` — no runtime-type dispatch. Call `count(myList)` должен resolve к единственному `count<T>(xs: Enumerable<T>): int` независимо от runtime kind.

**Decision**: registry stays `(name, arity)`-keyed. Constraint travels via `GenericConstrains.CompositeConstraint` (§9 C.4a). Apply (§4) accepts any RHS satisfying the constraint via §3 cross-rules. Dispatch — **structural**, resolved at TIC time, executed via interface call at runtime.

**Rejected**: N concrete overloads per kind — would inflate registry to ~120 entries (22 functions × 5+ kinds), regress inference for nested generic calls.

**Concrete dispatch flow** for `count(myList)` where `myList: list<int>`:

1. Parser emits `FunCallSyntaxNode("count", [myList])`.
2. `TicSetupVisitor.Visit(FunCallSyntaxNode)` (at `TicSetupVisitor.cs:938`) looks up `count/1` in `IFunctionRegistry` → returns single `count<T>` signature.
3. `TicSetupVisitor.InitializeGenericType` (line 1797) reads `T`'s `GenericConstrains.CompositeConstraint = Enumerable` → emits `CompCS{Anc=enumerable, Desc=null, Element=fresh CS}` for `xs`'s TIC node.
4. Pull pushes `myList`'s `StateCollection(List, int)` into `xs`'s CompCS slot.
5. Forward Pull `Apply(CompCS{Anc=enumerable}, StateCollection(List, int))` (§4.1.1): narrows `Desc=list` via LCA; `xs.State` = `CompCS{Anc=enumerable, Desc=list, Element=int}`.
6. Interval `[list..enumerable]` — not a point, no collapse.
7. Destruction (§3.4): pick `Desc=list` → `xs` resolved to `StateCollection(List, int)`.
8. Runtime expression node receives `IFunnyEnumerable` view (since `IFunnyList : IFunnyMutableArray : IFunnyEnumerable`). `count` reads `IFunnyEnumerable.Count`.

**Key insight**: runtime polymorphism via `IFunnyEnumerable` уже polymorphic — все lang-mode collection runtime classes implement it. Dispatch is structural at TIC time, executed via interface call at runtime. No per-kind dispatch needed.

**Migration risk**: если функция требует kind-specific dispatch (e.g. `reverse` O(1) для array vs O(n) для list), implement через runtime branching `if (xs is IFunnyMutableArray a)` внутри single function body — не via TIC-level dispatch.

## 7. `VarTypeConverter` cleanup scope

Verified against `src/NFun/Types/VarTypeConverter.cs`. Total **9 collection-related arms**:

| Arm | Lines | Status after Stage C |
|---|---|---|
| `List → ArrayOf` | 129-144 | **removed** (LINQ Enumerable migration) |
| `MutableArray → ArrayOf` | 147-163 | **removed** (LINQ Enumerable migration) |
| `List → MutableArray` | 169-184 | **removed** (cross-lang lattice; CompCS handles) |
| `ArrayOf → MutableArray` | 188-203 | **retained** (legacy LINQ result into lang slot) |
| `MutableArray → List` | 207-223 | **retained** (lang reassignment, debt #11 accumulator) |
| `List → FixedArray` | 229-243 | **retained** (intra-lang lattice subtype) |
| `MutableArray → FixedArray` | 244-259 | **retained** (intra-lang lattice subtype) |
| `FixedArray → ArrayOf` | 260-275 | **removed** (LINQ Enumerable migration) |
| `ArrayOf → List` | 282-297 | **retained** (debt #11 accumulator) |

**Net: 4 removed, 5 retained.**

**Debt #11 NOT closed by Stage C**: rows `ArrayOf↔List` retain accumulator pattern. CompCS removes forward direction (lang→ee LINQ), reverse direction preserved для compat. Закрытие #11 потребует assignment-edge в TIC — вне Stage C scope.

**§C.4b net delta**: +1 input converter (`ClrEnumerableInputFunnyConverter`). Total: `-4 + 1 = -3` arms.

## 8. F-bound migration — out of scope

`ConstraintsState.StructBound: StateStruct` carries structural shape, F-bound contractivity, cycle guards, width-subtyping — это не subsume'ятся `StateCompositeConstraints.Ancestor: ConstructorKind?` (nominal lattice).

**Decision**: Stage C **не трогает** StructBound. Migration → future stage с own spec.

## 9. Implementation roadmap

| Sub-stage | Scope | Deliverables | Effort |
|---|---|---|---|
| **C.0** | This document (v6) | Final spec, dual audit passed | 1 day |
| **C.1** | `StateCompositeConstraints` class + cycle-guard infrastructure | **Prerequisite**: commit `Specs/Tic/baselines/master-pre-stageC.json` (QuickBench V1 Precise baseline before any C.1 code). Then: class skeleton (§1), 7 mark constants (§3.8.1), HashSet harnesses (§3.8.2/3/4), private ctor + Create factory + With* methods | 1.5 days |
| **C.2** | Layer 0 algebra: LCA/GCD/Unify/Concretest/Abstractest/SimplifyOrNull/TryCollapseToPoint + unit tests | Pure-algebra operators (§3.1-§3.7), cross-class (§3.10) | 3 days (simpler than v5 due to no predicate complexity) |
| **C.3** | Layer 2 Apply overloads (Pull/Push/Destruction) + StagesExtension switch + unit tests | 4 explicit Apply cells (§4.1.1-4), other RHS routing, cross-class cycle guards (§3.8.3), `GetMergedStateOrNull` extension (§4.2) | 3 days |
| **C.4a** | `GenericConstrains` carrier mechanism + integration tests | `GenericConstrains` extends с nullable `CompositeConstraint` field (struct extension touches 8+ static factories); `TicSetupVisitor.InitializeGenericType` (line 1797) emits CompCS for `Enumerable`-constrained generics; `Constrains.Enumerable` static factory | 2.5 days |
| **C.4b** | CLR boundary: `BaseFunnyType.Enumerable = 24` + tables + converter | `BaseFunnyType.Enumerable = 24`, `TypeBehaviour.cs` table extension (`FunToClrTypesMap`, `DefaultPrimitiveValues` index 24), `ClrEnumerableInputFunnyConverter`, `FunnyConverter` case (§5.2), `IFunnyEnumerable.ElementType` pull-up, CHANGELOG note re binary-compat | 2 days |
| **C.5** | Migrate LINQ functions to single-overload `Enumerable<T>` / `IndexedRead<T>` signatures | Family 1-6 per LINQ category; 1 commit per family. ~22 functions migrated. Runtime branching for kind-specific perf if needed | 10 days (simpler than v5: no predicate setup needed) |
| **C.6a** | `IndexedMutable<T>` = `Anc≤array` interval — `a[i]=v` сохраняет target identity (closes debt #14) | Replace `TicSetupVisitor.cs:2465-2468` `GetOrCreateMutableArrayNode` pin with CompCS constraint `{Anc=array, Desc=null}` on target. Un-ignore `Stage3_LangMode_ListIndexedWrite_AliasSeesChange` | 2 days |
| **C.7** | Remove B-stage shortcuts | Remove 4 `VarTypeConverter` arms per §7, cross-LCA widening shortcuts в `StateExtensions.Lca.cs`, cross-merge identity arms в `SolvingFunctions.cs:74-95` | 2 days |
| **C.8** | Cleanup + un-ignore tests + perf gate | Un-ignore tests per §10 matrix, QuickBench Simple/Build ≤ +5% gate, docs sync, baseline JSON committed to `Specs/Tic/baselines/master-pre-stageC.json` | 1 day |

**Total: ~28 days** (v6 reduces from v5's 34d due to simpler algebra, removed predicate machinery, and deferred mutation typeclass; v6.1 +0.5d for C.4a carrier struct extension).

**Stage D** (separate effort, future): mutation API per container — list/set/queue/map specific methods. **NOT in Stage C scope.**

## 10. Test re-enabling matrix

### 10.1 Stage 2 LINQ tests

20 Stage-2-ignored tests в `MutableCollectionsContractTest.cs`: 18 contiguous in lines 84-266 + 2 outliers at 604, 628. Все `[Ignore("Stage 2 — …")]`. **Un-ignored at C.5** as LINQ migration progresses; **acceptance gate at C.8**.

Subset breakdown:
- `Stage2_*Count*`: un-ignored after C.5 Family 1.
- `Stage2_*Map*` / `Stage2_*Filter*`: un-ignored after C.5 Family 3.
- `Stage2_*Slice*` / `Stage2_*Get*`: un-ignored after C.5 Family 5.

### 10.2 Stage 3 mutator tests

| Test | Currently | After Stage | Trace |
|---|---|---|---|
| `Stage3_LangMode_ListIndexedWrite_AliasSeesChange` | Ignored (debt #14) | **passes after C.6a** | See §10.3 below |
| `Stage3_LangMode_IntArrayIndexedWrite_RebindsSlot` | Ignored (annotation syntax) | **stays Ignored** | Requires `int[]` annotation parser support (deferred to master) |
| `Narrowing_AcrossContinue_StructDirectFieldAccess` | Ignored (B.3.3 fallout) | **requires separate audit** | Not algebra-driven |
| `StmtBug49_RecFn_RecStruct_HOFArg_CircularAncestor` | Ignored (B.3.3 fallout) | **requires separate audit** | Recursive named type + `t[]` field |

### 10.3 Alias test trace (inferred + declared)

**Inferred target trace** (`a = list(1,2,3); a[0] = 99`):

1. `a = list(1,2,3)` — `a` slot inferred as `StateCollection(List, int)`.
2. `a[0] = 99` — `TicSetupVisitor.VisitIndexedAssignment` (post-C.6a):
   - Emit constraint on `a`'s slot: `CompCS{Anc=array, Desc=null, Element=int}` ("must be IndexedMutable").
   - Pull `Apply(CompCS{Anc=array}, StateCollection(List, int))` (§4.1.1):
     - Check `IsSubtypeOrEqual_L(list, array)` → ✓ (list ⊆ array branch).
     - `newDesc = LCA_L(null, list) = list`.
     - GCD with point → `CompCS{Anc=array, Desc=list, Element=int}`.
     - **List identity preserved** (Anc=array doesn't override; final concretise via Destruction picks Desc=list).
3. Destruction (§3.4): pick `Desc=list` → `StateCollection(List, int)`.
4. Runtime: `IndexedAssignExpressionNode.Calc()` casts to `IFunnyMutableArray` (implemented by `MutableFunnyList` per `IFunnyList.cs:20`). SetAt mutates in-place.
5. `a[0]` reads through `IFunnyEnumerable.GetElementOrNull(0)` → 99. ✓

**Declared-target trace** (hypothetical `b:list<int>` annotation):

1. `b:list<int>` — `b` slot pinned to `StateCollection(List, int)` via `LangTiHelper`.
2. `b[0] = 99` — `Apply(CompCS{Anc=array}, StateCollection(List, int))` succeeds (same as inferred).
3. Result identical. ✓

(Note: `list<T>` annotation parser deferred to master per §12 Q11; trace included for completeness.)

### 10.4 Stage D mutation API (deferred — separate effort)

Per-container methods, **no typeclass machinery**. These signatures are **forward design** — Stage C **does not deliver** them. Set/Queue/Map runtime classes do not exist yet; their introduction is **Stage D-1 prerequisite** (~3-5 days runtime work per container) before mutation API.

**Existing list mutation API (Stage 3 / B.1 — already shipped)**:
```
fun add(l: list<T>, x: T): void                    # BaseFunctions.cs / ListMutationFunctions
fun addAll(l: list<T>, xs: list<T>): void          # currently typed list<T>, NOT Enumerable<T>
fun remove(l: list<T>, x: T): bool                 # search-and-remove-by-value
fun removeAt(l: list<T>, idx: int): T?
fun removeLast(l: list<T>): T?
fun clear(l: list<T>): void
```

**Future list extensions (Stage D-1)**:
```
fun append(l: list<T>, x: T): void                 # alias for add (sugar)
fun addAll(l: list<T>, xs: Enumerable<T>): void    # widen second arg to Enumerable post Stage C
```

**Future set API (Stage D-1, requires Set runtime class first)**:
```
fun tryAdd(s: set<T>, x: T): bool                  # false if duplicate
fun tryRemove(s: set<T>, x: T): bool               # false if absent
fun contains(s: set<T>, x: T): bool
```

**Future queue API (Stage D-1, requires Queue runtime class first)**:
```
fun enqueue(q: queue<T>, x: T): void
fun tryDequeue(q: queue<T>): T?
fun tryPeek(q: queue<T>): T?
```

**Future map API (Stage D-1, requires Map runtime class first)**:
```
fun set(m: map<K,V>, k: K, v: V): V?               # returns previously stored
fun tryAdd(m: map<K,V>, k: K, v: V): bool          # false if key existed
fun tryRemove(m: map<K,V>, k: K): V?
fun get(m: map<K,V>, k: K): V?
fun containsKey(m: map<K,V>, k: K): bool
```

**Rationale**: semantic asymmetry между mutation operations is real (list `add` total/void, set `tryAdd` partial/bool, map `set` returns prior value, queue `tryDequeue` returns Optional). Unified API (Java-style `add: bool`) loses information. Generic mutation across containers — rare in scripting use cases; deferred to Stage E+ if ever needed.

## 11. Trade-offs

### 11.1 Performance budget

Baseline (Macbook M4, branch master pre-Stage-C, QuickBench Precise V1): **92.3 ± 0.5μs Simple Build, weighted-mean 18.75μs**.

| Metric | Budget | Abort gate (at C.8) |
|---|---|---|
| Simple Build (mean) | ≤ +5% (≤ 13.05μs from 12.43μs SPS-on) | Hard fail if > +8% — revert C.5+ batches |
| Medium Build | ≤ +5% | Hard fail if > +8% |
| Complex Build | ≤ +5% | Hard fail if > +8% |
| Weighted-mean | ≤ +3% (≤ 19.32μs from 18.75μs) | Hard fail if > +5% |
| CompCS allocations per Simple parse | 0 (no Simple-tier expression uses Enumerable typeclass) | Hard fail if > 0 |
| CompCS allocations per Medium parse | ≤ 2 per inferred `Enumerable<T>` arg | Hard fail if > 4 |

**Measurement protocol**: clean build, V1 Precise (60s), 3-run median. Reference baseline JSON committed at `Specs/Tic/baselines/master-pre-stageC.json` before C.1 starts. Each C.5 family commit re-runs the gate.

**Allocation measurement**: `[MemoryDiagnoser]` harness in `QuickBench` (extension, C.1 deliverable) OR instrumented build counter.

**Mitigation tools available before abort**:
1. `this`-return shortcut in `With*` when no field changes (already in §1).
2. Sentinel singletons for common shapes (`Empty`, `EnumerableUnconstrained`, `IndexedReadConstrained`, `IndexedMutableConstrained`) — `~0.5 days` at C.2.
3. Cycle-guard HashSets remain `ThreadStatic` as today; **no new perf-only ThreadStatic fields** (per `feedback_threadstatic_for_perf.md`).

If post-mitigation budget still misses → abort; CompCS allocation pressure exceeds value.

### 11.2 Trade-offs (recap)

| Cost | Magnitude |
|---|---|
| Lines of code | ~+1800-2200 (v6 less than v5 due to no predicate machinery) |
| Dispatch surface | ~14 explicit Apply cells (§4.1 × 2 + §4.0 routing) |
| Documentation | Per-cell rules |
| Learning curve | One more TIC state class |
| Allocation per refine | 1 alloc per accepted `With*` (mitigated by `this`-return) |

| Benefit | |
|---|---|
| Algebraic clarity | CompCS = CS-for-composites, no impedance mismatch |
| Closes debt #14 | `a[i]=v` pin removed |
| Enables typeclass-style LINQ | Single signature per function vs N overloads |
| Removes ad-hoc shortcuts | `list↔array` Stage 2.5 / B.3 shortcuts retired |

## 12. Decisions

| Question | Decision |
|---|---|
| Q1 `Ancestor` nullable vs `Any` sentinel | **Nullable** (consistent with CS) |
| Q2 `Concretest(Any, Desc=null)` | Returns CompCS unchanged; dialect default in §5 |
| Q3 Predicate axes | **Removed** (v6) — fold into interval (IsIndexedMutable=`Anc≤array`) or ElementNode propagation (IsHashableElement→CS IsHashable). No CompCS-level predicates |
| Q4 `IsOptional` rules | LCA=OR, Unify=OR (preserved from CS precedent) |
| Q5 Element invariance | Identity (`↑Element = ↓Element = Element`) |
| Q6 Cycle guard mark constants | -59000..-59300 reserved (§3.8.1) |
| Q7 F-bound migration | Out of scope (§8) |
| Q8 Performance | QuickBench Simple ≤ +5%, Build ≤ +5%, allocations per parse ≤ N (gate at C.8) |
| Q9 Map deferral | Stage D (§10.4) — separate methods, no unified API |
| Q10 `enumerable<T>` parser annotation | No parser work in C; deferred to master |
| Q11 `list<T>` declared-annotation parser | Deferred to master |
| Q12 `IFunnyMutableCollection` interface | **Removed** (v6) — Stage D uses per-container interfaces (no shared interface) |
| Q13 `BaseFunnyType.Enumerable` | New in C.4b |
| Q14 ee-mode `StateArray` lattice position | **NOT in lattice**; dedicated cross-rules §3.10 |
| Q15 `IsOptional` debt #5 duplication | Accept; fix #5 separately |
| Q16 Carrier mechanism for `Enumerable<T>` constraint in TIC | `GenericConstrains` extends с nullable `CompositeConstraint` field. `TicSetupVisitor.InitializeGenericType` (line 1797) emits CompCS nodes |
| Q17 Dispatch surface | **Single overload + TIC structural dispatch** via CompCS (§6.2). Registry stays `(name, arity)`-keyed |
| Q18 State immutability | **Copy-on-write** (`readonly` fields + `With*` returning ITicNodeState or null) |
| Q19 Apply directionality | **Pull/Push/Destruction explicit per-stage** (§4.0). Reverse-direction `Apply(prim non-Any, CompCS)` rejects |
| Q20 Collapse ownership | **Centralised in `With*` and §3 Layer 1 operators** — caller never sees raw point-CompCS |
| Q21 Layer decomposition | §3 = symmetric content + Layer 1 commit; §4 = Layer 2 directional commit. **§4 Apply cells never route to §3.2 GCD** (different ops) |
| Q22 Stage D mutation API | **Separate per-container methods** (list.add, set.tryAdd, map.set, queue.enqueue) — no shared interface, no typeclass. Honest semantic asymmetry |
| Q23 IsHashableElement axis | **Removed** (v6) — push to ElementNode where it becomes CS `IsHashable` |
| Q24 IsIndexedMutable axis | **Removed** (v6) — fold into `Anc ≤ array` interval |
| Q25 IsMutableCollection axis | **Removed** (v6) — Stage D doesn't need it (separate methods); Stage E+ if/when generic mutation needed |

## 13. Acceptance criteria для C.0 review (v6.1)

Algebra design (Layer 0 + Layer 1):
- ✅ CompCS = `{Anc, Desc, IsOptional, ElementNode}` — 4 fields, no predicate axes.
- ✅ §3 organised as symmetric content layer (LCA / GCD / Unify / Concretest / Abstractest / SimplifyOrNull / TryCollapseToPoint).
- ✅ §3 cross-rules for StateCollection / StateArray / StatePrimitive / StateOptional.
- ✅ Element invariance — MergeInplace at every cross-rule.
- ✅ Cycle guards: same-class + cross-class + unary (§3.8).
- ✅ StateArray **NOT in lattice** (regression fix); dedicated cross-rules §3.10.
- ✅ Copy-on-write factory + readonly fields (v5 A-B1 carried).

Apply design (Layer 2):
- ✅ §4 Apply table split into explicit Pull / Push / Destruction columns (forward + reverse).
- ✅ 4 critical cells (§4.1.1-4) stated per professor's rules (preconditions / content / element merge / commit) — Pull invariant preserved (Anc untouched), Push invariant preserved (Desc untouched).
- ✅ §4 cells **do not route to §3.2 GCD** (Layer 0/Layer 2 separation).
- ✅ Reverse-direction primitive non-Any rejects.
- ✅ Collapse centralised in `With*` + §3 Layer 1 operators (`GetMergedStateOrNull`); callers never see raw point-CompCS.

CLR + runtime:
- ✅ §5.1-5.3 CLR boundary; line citations corrected (`TypeBehaviour.cs:123-131`, `160+`).
- ✅ §6 Runtime interface mapping table.
- ✅ §6.2 Dispatch design — single overload + TIC structural dispatch. Closes debt #1.
- ✅ §7 VarTypeConverter cleanup; 9 arms classified; debt #11 NOT closed (acknowledged).

Testing + scope:
- ✅ §10 test re-enabling matrix accurate.
- ✅ §10.3 alias test trace (inferred + declared).
- ✅ §10.4 Stage D mutation API decided (separate per-container, no typeclass).
- ✅ Pin removal at `TicSetupVisitor.cs:2465-2468` in C.6a deliverables (closes debt #14).

Implementation:
- ✅ Roadmap C.1-C.8 with effort estimate ~27.5 days (down from v5 34d due to simpler algebra).
- ✅ §11.1 quantified perf budget (Simple/Build/allocation gates).
- ✅ Carrier mechanism: `GenericConstrains` extends with `CompositeConstraint`; emission at `TicSetupVisitor.InitializeGenericType:1797`.

v6.1 audit-driven fixes:
- ✅ §3.1 LCA null-as-identity (mirror `StateExtensions.Lca.cs:19-24`).
- ✅ §3.2 GCD same-class delegated to §3.3 Unify (no duplication).
- ✅ §3.10 StateArray narrower-wins (mirror `SolvingFunctions.cs:74-78`) — regression fix.
- ✅ §4.1.4 explicit null-guard on `GCD_L(Anc, K)`.
- ✅ §3.8.1 7 mark constants (was 4).
- ✅ §4.0.3 reject IsComparable + composite non-comparable kind.
- ✅ §3.6 hashability emission point note (TicSetupVisitor, not Apply cells).
- ✅ §3.7 ConstructorKind? value equality note.
- ✅ §3.2 cross StateOptional pseudocode fixed.
- ✅ §4.1.4 MergeInplace direction consistent with §4.1.3.
- ✅ §3.9 citation `ConstructorLattice.md:140-141`.
- ✅ §6.2 step 2 `Visit(FunCallSyntaxNode)` correct method name.
- ✅ §10.4 split EXISTING (B.1) vs FUTURE (Stage D-1), correct list mutation API.
- ✅ §9 C.1 baseline JSON prerequisite; C.4a 2 → 2.5d.

Pending:
- ⏳ C.1 implementation start.
