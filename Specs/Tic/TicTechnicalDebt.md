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

## Порядок устранения

```
#5 (stale snapshots) — single-pass Pull refactoring, medium effort
#6 (unconstrained generics) — SolveUselessGenerics fix, small effort but tricky
#7 (PropagatePreferred) — edge-local rewrite, medium effort, not urgent
```
