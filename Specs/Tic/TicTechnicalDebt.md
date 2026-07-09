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

## Долги алгебраического слоя (ревью 2026-07-10, фаза 1)

Найдены при сверке Algebra*-спек с кодом. Спеки переписаны на целевую систему
(двухслойная архитектура: чистая алгебра + слой резолюции); ниже — расхождения
кода с целевыми правилами. Каждая запись продублирована в секции «Отклонения»
соответствующего Algebra*-файла.

## 19. Резолюционные примеси в Concretest — ОСТАТОК (↓-часть закрыта 2026-07-09)

**Закрыто**: `Opt(Preferred)`-выбор и `ConcretestArrayElement` вынесены из ↓ в
снапшот-оператор слоя резолюции `ConcretestSnapshot` (↓ₛ,
`StateExtensions.ConcretestSnapshot.cs`); ↓ — чистая проекция по нормативным
правилам `Algebra_Concretest.md`. Потребители ↓ₛ: `ConstraintsState.AddDescendant`
(оба снапшот-пути) и односторонние desc-проекции `LCA(CS,CS)` (результат хранится
как Descendant join'а — transport-правило Preferred требует выживания вложенных
hint'ов). Чистый ↓ остался в: bc2-arm `LCA(T, CS)` (`↓T`), fun-args ↑ (дуальность),
`CanBeConvertedOptimisticTo` (предикат). Контракт и пины — `ConcretestSnapshotTest`
(делегация ↓ₛ ≡ ↓ на hint-free, идемпотентность, Fit-совместимость, оба плеча);
закон 4 уточнён: домен — hint-free на любой глубине. Sweep ассоциативности ⊓ не
сдвинулся (322 до/после; в записи #22 фигурировало 321 — пере-замер на 66016a29
даёт 322, расхождение зафиксировано ДО этого изменения). QuickBench — замеряется
отдельно после приземления.

**Остаток**: Sat/форм-меняющий коллапс решённого композита `[D..∅] → D` внутри ⊓
(корень 2 долга #22 + «Preferred-survival exception» в `MergeOrNull`) — резолюционная
примесь в ⊓; лечится выносом коллапса из ⊓ в стадийную канонизацию. Под QuickBench.

## 22. Ассоциативность ⊓ ОПРОВЕРГНУТА (2026-07-09)

Property-тест `AlgebraMergeUnifyLawsTest.Merge_Associative_OverCsTriples`
([Ignore]-pinned). Из трёх корней первоначального анализа корень «ячейка `T ⊓ CS`
недо-принимает» УСТРАНЁН единым satisfaction-предикатом FitsInto (opt-лифт; sweep-diff против
c6cd0edc — −64 контрпримера, +0 новых; пин
`Merge_TCsCell_OptionalLift_MidFoldCollapse_Associates`). Остаются два ВРОЖДЁННЫХ
корня (321 контрпример на CsShapes³; пере-замер 2026-07-09 на 66016a29 даёт 322 —
до и после ↓-части #19, т.е. split не сдвинул sweep):

1. **P-ось (drop-order)**: правило «hint-LCA, затем drop-if-unfit» коммутативно,
   но не ассоциативно. Контрпример: `a=[∅..∅], b=[U8..I64]Re!, c=[U8..Re]I32!` →
   `(a⊓b)⊓c = [U8..I64]I32!` (Re дропнут рано, I32 выживает), но
   `a⊓(b⊓c) = [U8..I64]` (hint-LCA(Re,I32)=Re не влез — информация I32 уничтожена).
   Ядра интервалов совпадают — расходятся только метаданные. Закон формулируется
   «modulo hints».
2. **Sat-меняющий коллапс решённого композита** `[D..∅] → D` (D — solved
   Struct/Optional): mid-fold сужает Sat с `{T ≥ D}` до `{D}`, теряя «комнату вверх»
   над D. Контрпример: `a=[∅..∅], b=[{a:I32}..], c=[U8..]` → left = null
   (struct ∉ Sat([U8..]) — корректно для collapsed struct), right = `[Any..]`
   (LCA(struct, U8) = Any в CS-домене — корректно для constraint). Побочная форма —
   потеря opt-метаданных: `(a⊓b)⊓c = {F|a:I32}` vs `a⊓(b⊓c) = opt({F|a:I32})` при
   `c=[∅..∅]?`. Резолюционная примесь в ⊓ — духа #19; предикатом не лечится,
   лечится выносом коллапса из ⊓ в стадийную канонизацию (вместе с #19).

Верифицированный фрагмент РАСШИРЕН: hint-free non-collapsing —
примитивные И array-descendant'ы, все cmp/opt-комбинации
(`Merge_Associative_HintFree_NonCollapsing_OverCsTriples`, активный; заменил старый
`Merge_Associative_PrimitiveIntervalFragment`).

## 25. Лемма 1 Destruction ОПРОВЕРГНУТА как инвариант: ⊓-null на constraint-ребре достижим на зелёных скриптах (сужен 2026-07-10)

Было: «`DestructionRec` игнорирует bool-возврат; null от ⊓ на constraint-ребре —
внутренняя ошибка по Лемме 1; Fix: assert». Assert-попытка 2026-07-10 дала
НАХОДКУ: DEBUG-panic в ячейке `DestructionFunctions.Apply(CS, CS)` (merge-null,
вне optional-оси — ровно документированно-невозможный класс) сработал на 6
зелёных Syntax-тестах, независимо от прочих изменений пакета. Контрпримеры
(все non-opt CS × CS): `[F32..Re]` vs `[U4..I96]I32!` (`[0x1,0x3].avg()` —
Floats-desc против integer-range-desc), `[Re..]` vs `[U4..I96]I32!`
(generic-функция `choise(0x1, 2.0, b)`; апкаст `[[0x1],[1.0],[0x1]]`),
`[[..]..]` vs `[U4..Re]I32!` (fun-LCA: FunLca3, ObviousFails — через
Destruction Fun-arm). Т.е. предпосылка Леммы 1 (полнота Pull-вкладов, теорема
минимального интервала) на этих рёбрах НЕ выполняется; скрипты живут именно
благодаря silent-continue (узлы остаются нерешёнными, Finalize добирает).

Сделано: сигнал переведён в TraceLog на этом пути (комментарий-находка в ячейке);
«Следствие (целевое)» Леммы 1 в `TicAlgorithm_Destruction.md` помечено
опровергнутым как assert-кандидат. Честный fix — не assert, а восстановление
полноты Pull-вкладов на этих семействах (родня #5/#10); до тех пор assert
невозможен, отличение «легитимного» null от бага не имеет решающего предиката.

## 26. Открытое постусловие Apply(CS(opt), CS(¬opt)) в Destruction

Правило оборачивает узел-потомок (`StateOptional(descendantNode)`), выбрасывая
собственный интервал предка (Desc/Anc/cmp/Preferred). Контрпример неизвестен,
но инвариант «ограничения предка поглощены интервалом потомка после Push» не
доказан. Кандидатная формализация — в Лемме 2 TicAlgorithm_Destruction.md.

## 27. Асимметрия TypeName-конфликта: Pull деградирует, Merge отвергает

Pull `Apply(Struct,Struct)` при конфликте имён деградирует до анонимной
структуры (Bug HH), `GetMergedStateOrNull` возвращает null — два разных ответа
на «тип a против типа b». Сегодня осознанно (≤ против =), но следующее
изменение NamedTypes должно решить это сознательно.

## 28. TrySetAncestor безусловно перезаписывает Preferred

`TrySetAncestor` пишет `Preferred = anc` без null-гарда — в отличие от всех
остальных точек пропагации. Конкретный аргумент сигнатуры может затереть хинт
литерала; кандидат-вкладчик в семейство смешанных хинтов (долг #7).

## 29. Тихая потеря S при материализации CS{S, opt}

При материализации IsOptional-CS в §5b StructBound не наследуется внутренним
узлом. Форма `CS{S, opt}` сегодня не возникает, но потеря молчалива, если
когда-нибудь возникнет. Отмечено в Destruction-спеке §5b как sharp edge.

Вторая точка той же потери — Push opt-материализация
(`PushConstraintsFunctions.Apply(ICompositeState, CS)`): в отличие от
Pull-близнецов там нет ни S-транспорта с `RewireStructBoundOwnership`, ни
переноса Preferred. Недостижимость для S-несущего CS проверена probe'ом по
полной батарее (2026-07, ревью item 5); инвариант и требуемый fix записаны
комментарием на месте. Смежная слепота Push к S в composite-ancestor-ветках
(`case StateArray/StateFun` без S-конфликт-гарда, `TransformToStructOrNull`
без S) — кандидат на общий рефакторинг 6x-дублированного opt-материализатора.

## 32. Convert-мост `CanBeConvertedOptimisticTo(Struct,Struct) → UnifyOrNull` не несёт коиндуктивный контекст — ДОКАЗАН НЕДОСТИЖИМЫМ ИЗ СЕМЕЙСТВА

При переводе алгебры на явный `AlgebraCycleContext` (инвариант №13 Algebra.md)
все мосты взаимно-рекурсивного семейства протянуты контекстом, кроме одного:
`StateExtensions.Convert.cs` — struct×struct-ячейка `CanBeConvertedOptimisticTo`
вызывает `UnifyOrNull` с ctx=null (свежее assumption set).

**Аргумент недостижимости** (статический, по call-graph): единственные вызовы
`CanBeConvertedOptimisticTo` ИЗНУТРИ семейства — cmp-канонизация
`MergeOrNull`/`SimplifyOrNull` с целью `StatePrimitive.Char`; struct-ячейка
требует `to is StateStruct` — недостижима по типам аргументов. Остальные
вызовы — stage-уровень (свежий вход, как любой top-level вызов).
Runtime-тест не конструируем (недостижимость типовая); стресс-μ-тесты
протянутых мостов: `AlgebraStructBoundLawsTest.*_Terminates`.

**Fix при инвалидации аргумента** (новый вызов из семейства с не-примитивной
целью): протянуть ctx через Convert-цепочку тем же паттерном, что Fit.

## 34. Открытые вопросы резолюционного хвоста (TicResolution.md §9, O2-O6)

O2: три правила для `[D..∅]` без Preferred (конвертер→Any / SPS rule 8→d /
SolveCovariant→композитный D) — расхождение латентно (Destruction поглощает),
нормативное правило не выбрано. O3: IsOptional дропается на границе
(FromTicConstrains + Preferred/Ancestor-ветки конвертера) — контрпример не
построен, proof obligation. O4: композитный Descendant молча дропается
проекцией сигнатур. O5: struct-generic матчинг по superset имён полей,
first-wins, значения игнорируются — вложенные наборы неразличимы.
O6: четвёртое ad-hoc правило резолюции вне слоя — RuntimeBuilder
(PrecomputeDefaultValues) резолвит CS как `Preferred ?? Descendant`.

## Порядок устранения


```
Алгебра (остатки ревью, по возрастанию риска):
#19 — вынос Sat-меняющего коллапса решённого композита из ⊓ в стадийную
      канонизацию (вместе с #22; последним, под QuickBench)

Верификация/страховка:
#25 — восстановить полноту Pull-вкладов на контрпримерных семействах
      (Floats/generic-fn/fun-LCA), затем вернуть assert (родня #5/#10)
#26 — доказать/опровергнуть постусловие Apply(CS(opt), CS(¬opt))

Дизайн-решения (при следующем заходе в область):
#27 — TypeName-конфликт: Pull деградирует vs Merge отвергает (решить при NamedTypes)
#28 — null-гард Preferred в TrySetAncestor (вместе с #7)
#29 — наследование S при материализации CS{S, opt} (когда форма возникнет)

Алгоритм (легаси):
#5 (stale snapshots) — single-pass Pull refactoring, medium effort
#10 (edge-rewire compensation) — combine with #5 — worklist Pull
#6 (unconstrained generics) — SolveUselessGenerics fix, small effort but tricky
#7 (PropagatePreferred) — edge-local rewrite, medium effort, not urgent
(#8 — architectural trade-off, ждёт API-потребителя; #9 — два narrow
workaround'а Phase 1 #108, сужать только с debugger-трассировкой.)
```
