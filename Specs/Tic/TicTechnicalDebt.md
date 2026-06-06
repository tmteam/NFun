# TIC Technical Debt

Ограничения текущей реализации, которые отклоняются от идеальной алгебры.

---

## 9. IsMutable decoupling cascade — two narrow workarounds (#108 Phase 1)

**Контекст**: Phase 1 #108 поменяла `StateStruct.IsMutable => TypeName == null` на `=> !IsSolved`, чтобы привести StateStruct в соответствие с StateArray/Optional/Fun. Cascade оставил два workaround'а:

### 9a. `GetMergedStateOrNull` immutable-shortcut excludes StateStruct

**Где**: `SolvingFunctions.cs:53-60` (`stateA is not StateStruct && stateB is not StateStruct`).

**Почему**: shortcut "both immutable → equality decides" больше не подходит для struct после Phase 1 — empty struct + non-empty struct оба immutable, но Equals по shape возвращает false, что ломает row-poly union. Текущее решение — type-keyed exclusion + специальный struct↔struct case ниже.

**Правильный fix**: либо `HasNominalIdentity` свойство на ITypeState (типы с TypeName / sealed leaf), либо вообще убрать shortcut (опираться на switch-case разбиение). Оба требуют ревизии всех ITypeState callers.

### 9b. `TicNode.State` setter assertion слишком широкая для anonymous struct

**Где**: `TicNode.cs:158-170` — четвёртый disjunct `(_state is StateStruct ss && ss.TypeName == null)` пропускает ЛЮБОЙ `value` для anonymous-struct `_state`.

**Почему**: должны быть три конкретных перехода: `value is StateStruct` (struct→struct refinement), `value is ConstraintsState cs && cs.HasStructBound` (LiftMuTypes), `value is StateRefTo` (уже покрыто). Любое сужение ловит BugC_LcaOfRecursiveVarsInArray, который заассертился после Phase 1 (anonymous-solved structs стали IsMutable=false).

**Правильный fix**: сузить до трёх explicit cases после того как точно протрассированы все легитимные replace-paths. Безопасно делать с реальным debugger'ом.

---

## 5. Stale Pull snapshots (DescendantHasOptionalLift) — WORKAROUND

**Проблема**: Desc-snapshot в CS создаётся в Pull Phase 1. Phase 2 (PullNoneNode) оборачивает элементы в Optional. Snapshot устаревает — не отражает Optional wrapping.

**Текущее решение**: два специальных случая в `DestructionFunctions.Apply(CS, ICompositeState)`:
1. Actual descendant содержит Optional-элементы, snapshot — нет → принять actual через RefTo.
2. Snapshot содержит Optional-элементы, actual — нет → принять snapshot, рекурсивная Destruction.

**Почему это workaround**: Destruction компенсирует temporal gap в Pull. Добавляет complexity (два branch'а + helper `DescendantHasOptionalLift` + `HasAnyOptionalLiftedField`).

**Правильный fix**: Single-pass Pull с immediate Optional propagation (устранить двухфазность), или invalidation/refresh снапшотов после Phase 2.

---

## 6. TwoVariableEquality — unconstrained generics resolve to Any

**Проблема**: `y = a == b` (два unconstrained var). TIC резолвит a,b к Any вместо generic T.

**Причина**: `SolveUselessGenerics` не помечает named input variables как "keep generic" — только output types и explicit inputNodes (из SetFunDef). Free variables без SetFunDef попадают в covariant loop → SolveCovariant → Any.

**Правильный fix**: Named nodes без SetDef = implicit inputs. Их leaf types должны маркироваться как "keep generic" в SolveUselessGenerics. Требует осторожности: нельзя маркировать ВСЕ named nodes (сломает recursive functions где named vars должны резолвиться).

---

## 8. TypeName lost at outermost FunnyType level (Bug P) — ARCHITECTURAL TRADE-OFF

**Проблема**: `x:t = t{v=1}` resolves to `x.Type = Struct({...})` — anonymous, без TypeName. Та же беда: `tree = t{...}`, `m = identity(x)`, любой top-level named value. Только глубинные рекурсивные ссылки (inner fields) сохраняются как `NamedStruct(t)` — но обёртка теряется.

**Причина**: `FunnyType` — это tagged union; либо `Struct` (с полевой спецификацией), либо `NamedStruct` (только имя). Нельзя одновременно. `TicTypesConverter.ConvertToFunnyStruct` (lines 155-164) **намеренно** разворачивает outermost named struct в plain Struct — runtime Fit для F-bounded функций, дефолтные значения, парсинг — всё читает field spec через `StructTypeSpecification`. Внутренние ссылки (depth ≥ 1) возвращают `NamedStructOf(TypeName)`, потому что бесконечный разворот цикла невозможен.

**Эксперимент**: возврат `NamedStructOf` на ВСЕХ глубинах ломает 9 тестов (BugQ, BugR, Annotation_PartialAnonymousStruct_Fails, DefaultKeyword_* x3, RecFun_StructField_TailReturnsNone, Roadmap_GetLast_LinkedList_Generic_TwoTypes, Roadmap_NominalCarry_GenericReturnPreservesTypeName) — runtime реально зависит от depth-1 expansion для field lookup.

**Чистые альтернативы**:
1. Расширить `FunnyType.Struct` — добавить optional `TypeName` поле. Outermost expansion сохраняет `Struct({fields...}, TypeName="t")`. Backwards-compat: existing Struct остаётся с `TypeName=null`. Equality/comparison обновить для учёта TypeName.
2. NamedStruct → spec registry. NamedStruct знает только имя; field lookup ходит через `NamedTypeRegistry`. Требует прокидывания registry в runtime / API.

**Что делать сейчас**: ничего. Семантика runtime корректна — поля доступны, F-bound работает. Потеря имени на верхнем уровне — косметика рендеринга. Закрыть, когда (или если) появится потребитель API, которому нужна идентичность типа на верхнем уровне (например, сериализация с TypeName в payload).

---

## 7. PropagatePreferred — single global value

**Проблема**: PropagatePreferred собирает ОДИН глобальный Preferred (первый найденный) и broadcast'ит всем совместимым CS нодам. При mixed Preferred (e.g., hex P=U16 + int P=I32) результат зависит от порядка toposort.

**Текущее состояние**: не проблема пока все integer литералы имеют одинаковый Preferred из диалекта (I32, I64, или Real). Станет проблемой при per-literal Preferred.

**Surface (Bug Z, bug-hunt round 4)**: `fn(arr:int[]?):int = arr?.reverse().sum() ?? 0; fn([10,20,30])` крашит в runtime с `Unable to cast Int32 to UInt32`. TIC резолвит sum's T в U24 (= UInt32 runtime), хотя actual array element — Int32. Происходит только в user-fn body — top-level тот же код работает. Корень: внутри fn body есть смесь Preferred — `0` (Preferred=I32, default int), sum's T (Arithmetical → descendant U24, без явного Preferred), и declared return type I32. PropagatePreferred выбирает один глобальный Preferred (первый найденный в toposort) и broadcast'ит. Для user-fn body порядок toposort такой, что U24 (от Arithmetical) выигрывает над I32 (от литерала 0). Другие reducers (count/min/max/fold/avg) работают — `sum` специфично потому что его генерик-dispatcher разносит на разные runtime implementations по `BaseFunnyType` (UInt32Function vs Int32Function).

**Правильный fix**: Edge-local PropagatePreferred — пропагация по рёбрам графа вместо global broadcast. Каждый литерал пушит свой Preferred только в connected nodes. Bug Z — первая практическая необходимость в этой работе.

---

## 10. Pull edge-rewires violate single-pass toposort invariant — WORKAROUND

**Проблема**: Несколько `Apply` overload'ов в `PullConstraintsFunctions` делают edge rewire во время Pull: `desc → opt(T)` превращается в `desc → T` (implicit lift materialization). Новое ребро появляется ПОСЛЕ того, как toposort уже зафиксировал порядок узлов — `T` может быть уже обработан, и Pull single-pass не вернётся к нему чтобы получить descendant'а.

**Текущее решение**: общий helper `LiftDescendantToOptionalElement` в `PullConstraintsFunctions.cs` делает три шага атомарно: RemoveAncestor + AddAncestor + `StagesExtension.Invoke` (eager propagation). Два call-site'а:
1. `Apply(ICompositeState=opt, StatePrimitive)` — line 111. (MR6Bug3 family.)
2. `Apply(ConstraintsState, ICompositeState)` для opt-descendant с primitive-only inner CS — line ~278. (MR3Bug1 family.)

Без eager propagation `T` остаётся с пустой CS, и LCA-таргеты ниже по графу получают только одну сторону. Симптомы: `true ?? 1.5` → `out:Real = true` (soundness violation); `true ?? [1,2,3]` → NRE crash.

**Почему это workaround**: Pull объявлен single-pass over toposort. Любое появление новых рёбер ВНУТРИ Pull нарушает инвариант. Helper лишь гарантирует что инвариант восстанавливается immediately. Это compensation, не cure.

**Правильный fix**: Worklist-based Pull. Вместо foreach по toposort'у — очередь узлов "ready to pull". Когда rewire создаёт новое ребро, добавить target обратно в очередь. Инвариант "никакого узла не обработали с устаревшими descendants" достигается естественно. Несколько call-site'ов в PushConstraintsFunctions / DestructionFunctions также делают rewires — все ожидают такого же worklist-подхода.

**Эстимейт**: medium effort, требует переработки `PullConstraintsTwoPhase` + `PullRec` + audit всех Apply overload'ов на rewire-paths. Имеет смысл совместить с #5 (stale snapshots — другая single-pass проблема).

---

## 11. Lang-mode `list ↔ array` runtime cast is bidirectional but TIC subtyping is one-way — DESIGN TRADE-OFF

**Где**: `VarTypeConverter.cs:127-164` (`list<T> → T[]`) and `:140-164` (`T[] → list<T>`).

**Контекст**: Stage 2.5 (`950bc19f`) ввёл TIC subtyping `list<T> ≤ T[]` через `Apply(StateArray, StateCollection)` overload. Pull/Push/Destruction принимают list в arg-позиции `T[]`. Это **односторонне** — `array ≤ list` в lattice нет.

Однако `VarTypeConverter` (commit `b5fab46d`) добавил **обратный** runtime cast `T[] → list<T>` для accumulator pattern `out:list<T> = []; out = concat(out, …)`. concat возвращает `T[]`, slot — list. Reassignment runtime-cast'ит результат обратно.

**Почему workaround**: TIC не имеет edge для array→list, поэтому runtime cast — "тихая" компенсация. Unsound reassignment chains для `list<T>` slot не могут быть отвергнуты на этапе type-check — converter всегда материализует.

**Правильный fix**: ввести **assignment-edge** в TIC, отличный от arg-passing edge. Assignment ≠ subtype, и `list<T> ↔ list<T>` (invariant), `array<T> ↔ array<T>` (invariant). Cross-kind на assignment-edge — требует explicit `.toList()` / `.toArray()`. Или: расширить TIC до symmetric invariant compatibility for invariant types при reassignment, что эквивалентно требованию явного coercion.

**Эстимейт**: medium. Требует нового edge-type в TIC и parser-side tagging assignment-AST nodes как assignment (not arg-passing).

---

## 12. Default values для composite types — asymmetric across kinds

**Где**: `Runtime/IFunnyVar.cs:175-200` (`VariableSource.GetDefaultValueOrNullFor`).

**Состояние**:
- `List` → empty `MutableFunnyList` (Stage 2.2).
- `Optional`/`None` → `FunnyNone.Instance`.
- `Custom` → `CustomTypeDefinition.DefaultValue`.
- **`Struct` / `NamedStruct`** → `null`.
- `ArrayOf` → `FunnyArrayTools.CreateEmptyArray`.

**Почему workaround**: пользователь объявляющий `var x: MyStruct` (когда Stage 3 раскроет реальную мутабельность) получит `null`, тогда как `var x: list<int>` получит empty list. Этот asymmetry разочарует пользователей.

**Правильный fix**: при пуске Stage 3 (мутабельные структуры с поле-инициализацией), решить — null-default или zero-valued-shell-default. Документировать в `Specs/Statements.md` §Variable declaration. Sync с поведением reassignment.

**Эстимейт**: small but blocked on Stage 3 design decision.

---

## 13. `TestHelper.AreSame` cross-kind permissive comparison может маскировать regressions

**Где**: `src/Tests/NFun.TestTools/TestHelper.cs:97-127`.

**Контекст**: commit `b5fab46d` сделал `AreSame` сравнивающим обе стороны как `IEnumerable` element-wise если оба sequences. Это намеренно — нужно для cross-kind equality в tests (`AssertReturns("y", new[]{1,2,3})` где runtime может вернуть list).

**Почему workaround**: future bug, который случайно поменяет container kind (например, lang-mode LINQ начнёт возвращать list вместо array), не будет пойман AssertReturns assertions — они всё ещё passing.

**Правильный fix**: добавить отдельную strict variant `AreSame_StrictType` для тестов, где kind важен. Default tests могут оставаться permissive. Помечать container-kind-sensitive tests новым assertion.

**Эстимейт**: small. Requires audit of which tests legitimately need strict comparison.

---

## 14. `a[i] = v` pins target to `array<T>` — breaks `list`-alias path — **RESOLVED**

**Resolution** (commit `91328207`): replaced the hard pin
(`GetOrCreateMutableArrayNode(target_ref, …)`) with a soft upper-bound
constraint — an invisible `mutArr<elementType>` template node added as an
ancestor of the target reference. The narrower-kind merge rule
(`list ≤ array`) keeps the slot as `list` when a list flows in; the kind
survives intact, alias reference identity is preserved, and the runtime
`IFunnyMutableArray` check still rejects Set/Map/immutable shapes.

Tests un-ignored: `Stage3_LangMode_ListIndexedWrite_AliasSeesChange`.

---

## 16. CompCs cross-Apply preferred propagation loss — DESIGN TRADE-OFF

Stage 5 widened LINQ `map` first arg from `FixedArray<T0>` to `Enumerable<T0>`
so `Map<K,V>` satisfies the input contract via the synthesized pair-struct
element. The cross-cells `Apply(CompCs ancestor, StateArray descendant)` and
`ForwardPullCompCsSc` (CompCs × StateCollection) use try-MergeInplace-fallback-
to-AddAncestor on element nodes — strict identity merge for primitives
(preserves back-prop precision), loose AddAncestor edge when the element shape
isn't yet resolved (e.g. function shapes with contravariant arg slots that
can't strictly unify up-front).

**Affected (BOTH modes — initial documentation incorrectly claimed lang-mode unaffected)**:

ee-mode tests marked `[Ignore]` in this repo:
- `Closure_ArrayOfClosures_IndependentCells` — `[mk(1,2), mk(3,4), mk(5,6)]`
  with `mk(a,b) = rule(c) = a+b+c` — unconstrained `a, b` default to Real preferred
- `MR4Bug2_CorrectArityCallOn1ArgLambda_TypedAsElementReturnType` — `[rule it+1, rule it+2]`
- `MR4Bug2_ZeroArgCallOn1ArgLambda_InMapRule_SilentlyAccepted` — arity check lost
- `TwinArrayWithUpcast_lambdaConstCalculate` — `byte → real` upcast through
  nested `.map(rule it.map(...).sum()).sum()`

lang-mode mirror pinned in `MutableCollectionsContractTest.LangMirror_*`:
- 3 of 4 mirrors pass (closure cases work in lang-mode because `list<T>`
  invariant element pins back-prop tightly)
- `LangMirror_NestedByteUpcastMap_RealResult` FAILS — same nested-map+byte
  upcast regression hits lang-mode too. The numeric upcasting through
  multiple LCA layers loses precision regardless of descendant collection
  kind. Marked `[Ignore]` referencing this entry.

TIC infers the correct output type (e.g. `fixedArray<Int32>`) but runtime
materialises closure/upcast paths via Real-preferred resolution, producing
`InvalidCastException` at result extraction.

**Why some cases work and others fail**:
- Lang-mode list-of-functions + map → works (invariant collection element
  pins through MergeInplace path)
- ee-mode array-of-functions + map → fails (covariant StateArray + lambda-
  function-shape element falls through to AddAncestor → preferred-Real wins)
- BOTH modes: nested map with mixed numeric element types (byte↔int) →
  fails (multiple LCA layers introduce real-preferred default that runtime
  materialisation respects but type-check elides)

**Proper fix**: detect when the element is a function shape OR when nested
map LCA chains exist, and use strict MergeInplace with deferred-resolution
semantics (queue the element-merge to run after lambda body / nested map has
resolved). Or: worklist Pull that re-fires propagation on rewires. Both
require non-trivial TIC plumbing (TicTechnicalDebt.md #10 worklist-Pull is
the broader fix).

**Status**: 4 ee-mode + 1 lang-mode test marked `[Ignore]`. Trade-off accepted:
LINQ-over-Map functionality (m.map, m.filter, m.count) works, at the cost of
these niche numeric/closure precision losses in nested LINQ chains.

---

## 15. `TransformToArrayOrNull` / `TransformToCollectionOrNull` reuse descendant element node — WORKAROUND

`SolvingFunctions.TransformToArrayOrNull` (line 1569) and
`TransformToCollectionOrNull` (line 1623) reuse the descendant collection's
`ElementNode` directly when the inner element isn't yet solved, as a perf
optimisation (no fresh node allocation, no fresh CS, no fresh registration in
`_typeVariables`). Cascades through `TransformToMapOrNull` for KeyNode/ValueNode.

**Symptom**: chained `[]` over lang collections (`list(list(...))[i][j]`,
`fixedArray(list(...))[i][j]`) panicked with `Circular ancestor 0` in
`PullConstraintsFunctions.Apply(ICompositeState, ConstraintsState)`. The reused
ElementNode aliased the ancestor's ElementNode after element-invariance merge,
so `result.ElementNode.AddAncestor(ancestor.ElementNode)` added a self-edge.

**Current workaround**: identity guards in `Apply(ICompositeState anc, CS desc)`
for StateArray / StateCollection / StateMap branches. Mirrors the existing
guard at line 430 in `Apply(StateArray ancestor, StateCollection descendant)`.

```csharp
if (result.ElementNode != ancArray.ElementNode)
    result.ElementNode.AddAncestor(ancArray.ElementNode);
```

**Proper fix**: always allocate a fresh element node (or KeyNode/ValueNode for
Map) in `TransformTo*OrNull`. The Pull/Push Apply cells then never need
identity guards because the freshly-allocated node is never aliased with the
ancestor. Cost: extra `CreateTypeVariableNode` + `_typeVariables.Add` per
Transform call. Bench impact unknown — would need QuickBench A/B.

**Triggering scenario**: only fires when chaining `[]` over a lang collection
whose inner element type is also a lang collection AND the inner element isn't
yet solved at the time of Pull. Tests: `New_NestedList_InnerElementAccess`
covers the primary case.

**Status**: leave guards in place. Revisit if (a) more guard-bypassing cells
discover the aliasing or (b) a refactor centralises element-node allocation.

---

## Порядок устранения

```
#5 (stale snapshots) — single-pass Pull refactoring, medium effort
#10 (edge-rewire compensation) — combine with #5 — worklist Pull
#6 (unconstrained generics) — SolveUselessGenerics fix, small effort but tricky
#7 (PropagatePreferred) — edge-local rewrite, medium effort, not urgent
#11 (list↔array assignment edge) — design decision before Stage 3+
#12 (composite defaults) — blocked on Stage 3 mutable-struct design
#13 (AreSame permissive) — small audit
#15 (Transform* element-node reuse) — centralise fresh-allocation, benchmark first
```
