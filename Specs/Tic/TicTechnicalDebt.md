# TIC Technical Debt

Ограничения текущей реализации, которые отклоняются от идеальной алгебры.

---

## 11. Eager Preferred lift in `LcaWithNone` — inline struct + narrowing-numeric optional field

**Где**: `src/NFun/Tic/Algebra/StateExtensions.Lca.cs` lines 61-68 (in `if (b is ConstraintsState bc2)` branch).

**Симптом**: `arr:{v:float32?}[] = [{v=1.5}, {v=none}]` → FU719 parse error. Триггерится ТОЛЬКО для комбо:
- inline anonymous struct тип-аннотация (не named type)
- narrowing numeric optional field (`float32`/`byte`/`uint16`/`int16`/`uint32`)
- смесь value + none в array literal
- Preferred литерала шире таргет-филд-тип (`Re > F32`, `I32 > U8`, etc.)

**Workaround**: обернуть в named type. `type S = {v:float32?}; arr:S[] = [S{v=1.5}, S{v=none}]` работает.

**Root cause**: Внутри `Lca(a, b)` при `a=CS[F32..Re,P=Re]`, `b=None`:
```csharp
if (b is ConstraintsState bc2) {  // wait — b=None, not CS; actually a is CS
    // Path via symmetric swap → LcaWithNone(CS) → StateOptional.Of(CS)
    // ...
    // OR: path via b == None → returns LcaWithNone(a) at line 71
    // The eager-lift block fires when the CS itself is Optional and has Preferred:
    if (bc2.Preferred != null && ... optInner.Element is StatePrimitive elemP ...
        && elemP.CanBePessimisticConvertedTo(bc2.Preferred))
        return StateOptional.Of(bc2.Preferred);  // ← ЭТО collapses opt(F32) → opt(Re)
}
```

Applying `Preferred` as a type CHOICE inside LCA violates the "Preferred is metadata, not constraint" invariant (Specs/Tic/TicPreferred.md P3). Downstream Push cannot narrow `opt(Re)` to `opt(F32)` because Re is a concrete primitive that doesn't fit F32.

**Почему нельзя просто убрать блок**: тот же путь используется для `[1,2,3] else [none]` где НЕТ таргет-аннотации — тут Preferred=I32 должен выиграть, иначе результат `opt(U8)[]` (Byte?[]) вместо intended `opt(I32)[]` (Int32?[]). Два случая требуют противоположных решений от одной LCA-функции.

**Правильный fix (не сделан)**: target-aware LCA (Pull получает контекст ancestor и применяет его до/после LCA), либо разделить преформатирование Preferred по контексту (composite ancestor known vs unknown). Обе версии требуют структурного изменения Pull, не local edit.

**Attempts (2)**:
1. Return `CS-with-IsOptional=true` вместо `opt(Preferred_primitive)` — сохраняет interval + Preferred metadata. Разрешает bug#6, ломает 7 TIC-level тестов (expected StateOptional shape).
2. Return `StateOptional(CS)` — тот же shape как раньше. Ломает 2 runtime тестов ([1,2,3] else [none] → Byte вместо Int32) потому что `ConcretestOptional` не читает Preferred из внутреннего CS.

**Регрессионный пин**: `src/Tests/NFun.SyntaxTests/BugHuntResults.cs::Bug6_InlineStructOptionalFloat32Field_WithNone_ShouldBuild` (`[Ignore]`).

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

## Порядок устранения

```
#5 (stale snapshots) — single-pass Pull refactoring, medium effort
#10 (edge-rewire compensation) — combine with #5 — worklist Pull
#6 (unconstrained generics) — SolveUselessGenerics fix, small effort but tricky
#7 (PropagatePreferred) — edge-local rewrite, medium effort, not urgent
```
