# Push Reform — изо-рекурсивный вывод типов для именованных структур

**Issue**: [#121](https://github.com/tmteam/NFun/issues/121) — неаннотированные рекурсивные функции над объявленными именованными типами.

Каждая секция помечена статусом:
- **РЕАЛИЗОВАНО** — описывает текущий код (проверено против HEAD); нормативно.
- **ЦЕЛЬ** — проектная рамка, до конца не доказанная/не реализованная.
- **ИСТОРИЯ** — как было; сохранено для контекста, кода не описывает.

---

## Мотивирующий пример — РЕАЛИЗОВАНО

```nfun
type node = {v: int, next: node? = none}

listSum(n) = if(n == none) 0 else n.v + listSum(n?.next)
```

`listSum` не имеет аннотации возврата. Её principal type — `node? → int`, и TIC обязан
вывести его сам. Рекурсивный вызов `listSum(n?.next)` вместе с доступами `n.v` и
`n?.next` создают struct→struct цикл на типе параметра `n`. Без вмешательства TIC
бросает *Recursive type definition*.

Principal type — `μX. opt(struct{v:int, next:X})` — **изо-рекурсивен и контрактивен**:
каждое замыкающее ребро проходит через конструктор Optional, цикл well-founded. Push
reform восстанавливает этот Optional-разрыв при решении и (когда лифтованное тело не
матчится однозначно на именованный тип) обобщает его как F-bounded generic.

---

## Алгоритм

### Фаза A — обнаружение циклов — РЕАЛИЗОВАНО

`SolvingFunctions.ThrowIfRecursiveTypeDefinition(node, namedTypeRegistry, isRecursion)`
вызывается из `DestructionRec` (на каждой ноде обратного toposort-прохода) и из
`FinalizeRecursive`. Пре-скан `HasReachableOptSourcedStruct` (полный walk state-графа
ноды) выполняется только при `isRecursion` — без `?.` / registry / рекурсивных функций
opt-sourced структур не бывает, и не-рекурсивный код не платит за обход.

Сам обход (`ThrowIfRecursiveReq` / `ThrowIfNodeReq`) — НЕ «подсчёт back-edges», а
walk с жёсткими точками останова:

- **`case StateOptional: break`** — обход ОСТАНАВЛИВАЕТСЯ на Optional. Ничто за
  Optional'ом не исследуется вовсе (none — база индукции; всё под конструктором
  контрактивно по построению).
- **`case StateArray` при `fromStruct`** — останов: `struct → arr → struct` —
  валидная μ-форма (`type t = {kids: t[]}`). Standalone-цепочка массивов (`fromStruct
  = false`) продолжает обход — `arr(arr(...self...))` без структуры невалиден.
- **`case ConstraintsState` с `HasStructBound`** — F-bound сам является контрактивной
  границей: обход спускается в Descendant/Ancestor, но НЕ в S (повторный вход в
  bounded-переменную T аналогичен Optional-разрыву).

Повторный вход по `VisitMark` (`ThrowIfNodeReq`) разбирается в порядке:
1. `fromStruct` и нода — `StateArray` → принять (замыкающее ребро пересекло
   Array-конструктор; симметрия к останову выше — иначе VisitMark сработал бы ДО
   ветки `case StateArray`).
2. Цикл opt-sourced (`optSeen` с пути ИЛИ `StructSubgraphIsOptSourced` самой ноды) →
   `TryRepairOptSourcedCycle` (фаза C-struct).
3. `TryPromoteCSDescendantToStructBound` (фаза C-cs).
4. Иначе — `FindRecursionTypeRoute` и `TicErrors.RecursiveTypeDefinition`.

### Фаза B — opt-sourced gate — РЕАЛИЗОВАНО

Маркер `StateStruct.IsOptionalSourced` ставится конструкторами `?.`-форм:
`GraphBuilderExtensions.SetSafeFieldAccess` И `SetSafeMethodCall` (обе помечают граф
`IsRecursion = true`). Сохраняется через merge-операции правилом
`StateStruct.MergedIsOptionalSourced(a, b) = a || b` (MergeStructs, UnionStructFields,
GcdBound, Unify, Gcd, Pull `Apply(Struct,Struct)`). Исключение: через `Lca`
(`LcaStructFields`) маркер НЕ переносится — join строит структурно-наиболее-узкого
общего предка, чья идентичность не должна наследовать односторонний маркер.

Консультируются `HasReachableOptSourcedStruct` (пре-скан фазы A) и
`StructSubgraphIsOptSourced` (сама нода цикла — на случай, когда width-propagation
скопировала состояние без маркера). Объявленный `type t = {self:t}` маркера не
получает нигде и по-прежнему ошибка.

### Фаза C — ремонт цикла — РЕАЛИЗОВАНО

Два пути ремонта под разные топологии цикла:

**(C-struct) `TryRepairOptSourcedCycle`** — цикл замыкается на struct-ноде. Оборачивает
**каждое** замыкающее ребро (поле, для которого `ReachesStructWithoutOptionalBreak`
достигает узла цикла) в `StateOptional`:

```
struct{v:int, next:struct{v:int, next:...}}
                ↓
struct{v:int, next:opt(struct{v:int, next:opt(...)})}
```

Мульти-self-field структуры (`tree{left:tree, right:tree}`) требуют обёртки ОБОИХ
рёбер — ремонт одного оставил бы второй struct→struct цикл на следующем проходе.
Уже-Optional ребро считается отремонтированным; немутабельное поле принимается,
только если это именованная структура той же идентичности (прошлый проход уже разорвал
ребро) — любой иной немутабельный state означает исходную ошибку.

После обёртки всех рёбер структура штампуется `TypeName` из реестра через
`FindUniqueMatchingNamedType` (матч по field-set superset, ровно один кандидат).
Штамп ставится **последним**: ранняя установка перевела бы структуру в
`IsMutable = false` и заблокировала Optional-wrap присваивания.

**(C-cs) `TryPromoteCSDescendantToStructBound`** — корень цикла — CS-нода, чей
`Descendant` несёт цикловую структуру. Когда `StructDescendantClosesContractively`
подтверждает, что КАЖДЫЙ замыкающий путь полей пересекает Optional/Array (вердикты
`ClosingPathVerdict`: `NoClose` / `ClosesContractively` / `ClosesNonContractively`,
любой неконтрактивный — отказ), структура перепривязывается внутри той же CS:
`cs.StructBound = s; cs.ClearDescendant()`. Очистка Descendant обязательна —
обязательство поглощено F-bound'ом (`T <: S`); оставить Descendant значило бы
переограничить `T = S` в точности, убив F-bounded полиморфизм. Смены класса состояния
нет — строгое уточнение в F-bound-исчислении (Cardelli–Mitchell '89 / Pierce TAPL §20:
нижняя граница рекурсивной формы И ЕСТЬ F-bound). Последующие обходы рекурсии
коротко замыкаются на этой CS через ветку `HasStructBound` (фаза A).

### Фаза D — распространение TypeName по снапшотам — РЕАЛИЗОВАНО

`PropagateTypeNamesAcrossSnapshots` выполняется внутри
`SolvingFunctions.Destruction` после пофазового `DestructionRec`, под гейтом
`namedTypeRegistry != null && isRecursion`. Ремонт штампует `TypeName` на ОДНОМ
экземпляре структуры, но алгебраические операции (Pull, Lca, Concretest, Apply)
плодят независимые снапшоты — все они обозначают один μ-тип и обязаны сойтись в
идентичности для runtime-диспетчеризации.

Два прохода:
1. **Collect** (`CollectStampedTypeNames`) — все уже проштампованные имена
   (`presentNames`), включая структуры в Descendant-слотах CS; попутно — есть ли в
   теле хоть одна opt-sourced структура (`anyOptSourced`).
2. **Пропуск целиком**, если opt-sourced структур нет (мы не в μ-теле: все имена —
   от явных аннотаций, безымянные снапшоты — локальные field-access constraints) или
   имён не собрано.
3. **Stamp** (`StampUnnamedStructsByPresentNames` через предвычисленный
   `BuildNameFieldIndex`) — безымянная структура получает имя, если её field-set —
   подмножество объявленных полей РОВНО ОДНОГО собранного имени («exactly one»
   предотвращает over-tagging при общем префиксе полей у нескольких типов).

### Фаза E — F-bound lifting (`LiftMuTypes`) — РЕАЛИЗОВАНО

Порядок внутри `Destruction`: `DestructionRec`* → `PropagateTypeNamesAcrossSnapshots`
→ `MaterializeOptionalFlags` → **`LiftMuTypes`** (лифт видит уже материализованные
Optional-формы).

`TryLiftElement(elemNode, registry)` лифтует элемент Optional-ноды при ВСЕХ условиях
(фактический список; проверяется в этом порядке):

1. `elem.State is StateStruct s`;
2. `s.TypeName == null` — номинально запечатанные не заменяются;
3. `s.IsOptionalSourced` — форма родом из `?.`;
4. `StructHasSelfRef(s, elem)` — C1: структура реально самоссылочна (замыкание
   детектится и через `RefTo` на владельца, и через шаринг того же экземпляра
   `StateStruct` — паттерн обёртки из C-struct; `FieldReachesOwner` идёт через
   Optional/Array/RefTo, но НЕ спускается во вложенные структуры);
5. `StructFieldsSubsetOfAnyRegistered(s, registry)` — если реестр есть (гейт против
   over-lifting).

Проверки `IsMutable` в списке НЕТ. Прежняя редакция спеки требовала
`elem.IsMutable == true`; условие ушло из кода — наиболее вероятно, при декаплинге
#108 (Phase 1 перевела anonymous-solved структуры в `IsMutable = false`, и гейт стал
бы блокировать легитимные лифты решённых анонимных μ-структур).

Действие лифта: `s.IsFrozen = true` (F-bound обобщён — field-set ЗАКРЫТ; width-merge
на call-site валидирует, но не расширяет; аналог закрытия row-переменной при
let-generalization, TAPL §22.6), затем `elem.State = ConstraintsState{StructBound=s}`.
Back-edges внутри `Sμ` уже RefTo'ят elem-ноду — теперь это семантически «назад к
bounded-переменной T». Штампы TypeName переживают лифт: F-bound и TypeName
сосуществуют; runtime предпочитает TypeName (номинально), падает в F-bound
(структурно).

Успешный лифт (`anyLifted`) — доказательство, что сигнатура несёт F-bounded generic:
`Destruction` возвращает `false` даже при всех решённых top-level нодах, форсируя в
`GraphBuilder.SolveCore` путь `Finalize` → `TicResultsWithGenerics`.

Под-проход **`TryRedirectDegenerateOptCycle`** выполняется только при `anyLifted`:
`CollectFBoundHolders` собирает CS-держателей StructBound; для каждой Optional-ноды
walk по спайну opt/RefTo — если спайн замыкается сам на себя без struct-контента и
без собственного F-bound, голова opt-цикла (мутабельная, `StateOptional`)
переписывается в `StateRefTo(первый F-bound-держатель)` (Pottier-Rémy '05 §10.6
graph-as-witness; Amadio-Cardelli '93 §4.2).

### Смежные механизмы вне Destruction — РЕАЛИЗОВАНО

Механика ремонта из фаз A-C и E получила «сестринские» пути на других стадиях
пайплайна — до и после Destruction.

**1. Auto-wrap в setter'е `TicNode.State` (пятый путь ремонта).** Присваивание
opt-sourced структуры, замыкающее неконтрактивный цикл, чинится В МОМЕНТ мутации
графа (до всякого Destruction): если `value is StateStruct ns`,
`StructSubgraphIsOptSourced(ns)` и `StructHasFieldReaching(ns, this)` — state
оборачивается в `StateOptional(inner)` прямо в setter'е. Порядок гейтов —
перф-осознанный: дешёвый opt-sourced предикат раньше дорогого поиска замыкания.
Это же место содержит и flatten `opt(opt(T)) → opt(T)` при присваивании.

**2. Сертификация `TicNode.IsContractiveCycleHead` на toposort'е.**
`NodeToposort.Visit`: когда цикл замыкается через composite-member ребро, инициатор
цикла помечается `IsContractiveCycleHead = true`, и toposort продолжает работу — цикл
НЕ мержится и НЕ бросается (composite-member ребро контрактивно по построению,
Cardelli-Mitchell '89 §3). Флаг — witness: даунстрим-проверки
(`GraphBuilder.ReturnContainsContractiveCycle`, `HasAnyRecursiveCandidate`, runtime
Fit, коиндуктивный Equals) трактуют такую ноду как контрактивную границу, эквивалент
`cs.HasStructBound`.

**3. `TarjanScc` + `ScCClosurePass` — Kleene-fixpoint по циклическим SCC.**
`GraphBuilder.SolveCore` после `PushConstraints` (под гейтами `IsRecursion` и
`HasAnyRecursiveCandidate` — O(n) проба на `IsContractiveCycleHead` /
`CS{StructBound}` / `StructIsRecursiveCycle`) запускает `ScCClosurePass`:
`TarjanScc.ComputeSccs` по syntax-, named- и type-нодам (рёбра — RefTo,
composite-member, Ancestors; порядок совпадает с `NodeToposort.Visit`); для каждой
SCC с `IsCyclicScc` и `IsContractive` — `PushUntilFixpoint(scc, maxIterations: 10)`.
Однопроходный Push оставляет дегенеративные ref-цепочки в самоссылочных
return-позициях ко-рекурсивных функций; итерация Push до неподвижной точки
прогоняет F-bound по циклу до канонического регулярного дерева (монотонный dataflow
на решётке конечной высоты — Kildall '73 / Cousot '77). Ациклические синглтоны SCC
пропускаются — ноль оверхеда для простого кода.

**4. Двухъярусная контрактивность.** Ярусы применяют РАЗНЫЕ критерии:
- **Слабый критерий** (toposort / `TarjanScc.IsContractive`): достаточно ЛЮБОГО
  composite-member ребра внутри цикла — включая ребро через поле структуры. Дёшев,
  допускает кандидатов в дальнейшую обработку.
- **Строгий критерий** (Destruction-время: `ThrowIfRecursiveReq`,
  `ClosingPathVerdict`, `ReachesStructWithoutOptionalBreak`): разрыв засчитывается
  ТОЛЬКО за Optional/Array. Финальный судья: struct→struct цикл без Optional/Array,
  прошедший слабый ярус, здесь либо ремонтируется (фаза C), либо ошибка.

---

## Алгебраические правила TypeName — РЕАЛИЗОВАНО

Два правила в `StateStruct`:

| Операция | Правило TypeName | Хелпер | Почему |
|---|---|---|---|
| `Lca` | равны → сохранить; иначе → null | `LcaTypeName(a, b)` | Join — общий предок, не вправе выдумать имя, которого другая сторона не несла. |
| Merge (Gcd, Unify, MergeStructs, UnionStructFields, GcdBound) | один null → другой; равны → имя; различаются → null | `MergedTypeName(a, b)` | Merge — наиболее специфичная согласованная идентичность. |

Поведение при конфликте (оба именованы, имена различны) РАЗНОЕ по путям:

- **Pull `Apply(StateStruct, StateStruct)` и struct-ветка
  `Apply(ICompositeState, ConstraintsState)`** — НЕ отклоняют: обе стороны
  **даунгрейдятся до анонимных** (`TypeName = null`), полевые Pull-рёбра ниже ловят
  настоящие несовпадения формы. Основание: именованные типы структурны
  (`Specs/NamedTypes.md` — «transparent alias», «no runtime distinction»; Bug HH).
- **`GetMergedStateOrNull` (struct×struct)** — nominal short-circuit
  `StateStruct.NominalEquals`: одинаковые имена → любая сторона без спуска в поля;
  разные → `null` (merge-отказ). Асимметрия с Pull-путём — осознанная: merge требует
  тождества значений, Pull — лишь совместимости по ≤.

---

## Identity short-circuit — РЕАЛИЗОВАНО

`StateStruct.Equals` останавливается на двух уровнях:

1. **Номинальный**: `NominalEquals` — два именованных struct'а с одним TypeName равны
   по определению (спуска в циклический field-граф нет).
2. **Коиндуктивный** (для анонимных циклических форм): visited-pair guard
   (`Amadio-Cardelli '93 §4.2 бисимуляция`) — повторно вошедшая пара считается
   равной. Прежняя редакция полагалась только на TypeName-short-circuit; лифтованные
   анонимные F-bounds его не имеют, поэтому guard обязателен (см. также
   `ConstraintsState.StructBoundsEqual` — отдельная циклобезопасная структурная
   проверка равенства S-оси).

---

## Threading model — РЕАЛИЗОВАНО

`INamedTypeFieldRegistry` и флаг `IsRecursion` протаскиваются явными параметрами
(никаких глобалов; конкурентные solve изолированы — единственное исключение,
thread-static broadcast `StagesExtension._isRecursion`, устанавливается per-solve):

```
GraphBuilder.SolveCore(ignorePrefered)
  → Toposort (+fused Pull | PullConstraintsTwoPhase при None)
  → SolvingFunctions.PropagatePreferred(sorted)           // ДО Push — TicPreferred.md §4.1
  → SolvingFunctions.PushConstraints(sorted)
  → ScCClosurePass(sorted)                                // при IsRecursion
  → SolvingFunctions.Destruction(sorted, hasOptionalTypes, NamedTypeRegistry, IsRecursion)
      → DestructionRec(node, namedTypeRegistry, mark, isRecursion)
          → ThrowIfRecursiveTypeDefinition(node, namedTypeRegistry, isRecursion)
              → TryRepairOptSourcedCycle / TryPromoteCSDescendantToStructBound
      → PropagateTypeNamesAcrossSnapshots(sorted, namedTypeRegistry)   // registry ∧ isRecursion
      → MaterializeOptionalFlags(sorted)                               // при hasOptionalTypes
      → LiftMuTypes(sorted, namedTypeRegistry)                         // при isRecursion
  → SolvingFunctions.Finalize(sorted, outputNodes, inputNodes, syntaxNodes,
                              namedNodes, ignorePreferred, namedTypeRegistry, isRecursion)
      → FinalizeRecursive(node, namedTypeRegistry, mark, isRecursion)
          → ThrowIfRecursiveTypeDefinition(...)
```

На Build-стороне (`GraphBuilder.SetCall(StateFun)`) при `IsRecursion` работают
`SignatureHasRecursiveShape` → `CreatePerSiteCloneMap` (per-call-site инстанцирование
F-bounded сигнатур, Damas-Milner let-полиморфизм; RetNode намеренно не пре-клонируется)
и `ReturnContainsContractiveCycle` → возврат через `StateRefTo` на RetNode функции
(цикл остаётся внутренним для сигнатуры).

---

## F-bounded полиморфизм — алгебраическая форма

### Слот — РЕАЛИЗОВАНО

`ConstraintsState` несёт третье независимое измерение:

```
StructBound : StateStruct   // F-bound `T <: τ(T)`; τ — всегда StateStruct
HasStructBound : bool       // стабильный read-API (точка миграции #108 Phase 3)
```

Интервал становится `[D..A, cmp, opt, struct⊆S]`. `StructBound = null` ⇒ «нет
структурной верхней границы» — семантика идентична нерекурсивному коду. Измерение
**независимо** от `[D..A]`, ровно как `IsComparable` и `IsOptional`.

### RecBound — ИСТОРИЯ

Промежуточная конструкция `RecursiveBound : RecBound?` (обёртка над телом bound'а, к
которой `StructBound` был шимом) ликвидирована: `StructBound : StateStruct` — И ЕСТЬ
слот, никакой обёртки за ним нет. Упоминание «RecBound elimination Phase 1» осталось
только в комментарии `StateStruct.IsMutable`.

### Правила операторов на S — РЕАЛИЗОВАНО (пост-#12/#13)

Нормативный дом этих правил — Algebra-спеки; здесь сводка и указатели. Транспорт S
внутри операторов закрыт долгом #12 (законы — `AlgebraStructBoundLawsTest`).

| Оп | Правило на S | Реализация | Нормативно |
|---|---|---|---|
| ∨ (Lca) | `S = S₁ ∩ S₂` — пересечение по именам полей, LCA на общих; μ-позиции выпадают (sound weakening); null поглощает (bound дропается) | `StateExtensions.IntersectBoundsOrNull` | `Algebra_LCA.md` |
| ∧ (Gcd) | `S = S₁ ∪ S₂` поверх S-free ядра; решённый struct поглощает bound тем же meet'ом (struct-≤ и есть F-bound-предикат); `Any → CS{S}`; Optional разворачивается; прочие решённые → null | `StateExtensions.Gcd` → ownerless `GcdBound` + `ApplyBoundToMeet` | `Algebra_GCD.md` |
| ⊓ (Merge) | `S = S₁ ∪ S₂` (ownerless `GcdBound`) + three-way непустота `(D, A, S)` + S-discharge-гейт коллапса решённого композита (`FitStructBound`) | `ConstraintsState.IntersectIntervalsOrNull` / `MergeOrNull`, `StructBoundIsSatisfiable` | `Algebra_Merge.md` |
| Unify(CS,CS) | ≡ ⊓. Фикция `UnifyStruct` с exact-match правилом на S устранена | `StateExtensions.UnifyOrNull(CS,CS)` → `MergeOrNull` | `Algebra_Unify.md` |
| Fit | `T ≤ S` ⟺ `T:Struct`, `Fields(T) ⊇ Fields(S)`, попарно ковариантно; коиндуктивный in-progress guard. Часть ЕДИНОГО предиката `FitsInto` | `ConstraintsState.FitStructBound` (internal) | `Algebra_Fit.md` |
| ↓ / ↑ | S НЕ проецируется (иначе ломается умбрелла-закон `T ∨ CS = T ∨ ↓CS`) | `Concretest`/`Abstractest` S не читают | `Algebra_Concretest.md` / `Algebra_Abstractest.md` |

Этот файл дальше описывает только F-bound-специфику, которой нет в Algebra-спеках:
режимы владения, `RewireStructBoundOwnership`, контрактивность.

### Владение (ownership) — РЕАЛИЗОВАНО

`StructBound = S` принадлежит ровно одной `ConstraintsState`. `S.Fields` может
содержать `RefTo` назад на владельца — F-bound self-reference; сам `S` между двумя CS
не шарится. `GcdBound(a, b, resultOwner, otherOwner)` работает в двух режимах:

- **Owner mode** (стадии — Pull/Push): self-refs переписываются на нового владельца
  (`RewireFieldNode` / `RewireState`); при односторонней передаче bound'а —
  `RewireStructBoundOwnership(s, oldOwner, newOwner)` (ленивая проверка
  `StateContainsSelfRef`, копия только при необходимости). Optional-wrap ancestor'а
  мигрирует bound на inner-CS с rewire self-refs на inner-ноду
  (`PullConstraintsFunctions.Apply(CS, ICompositeState)`).
- **Ownerless mode** (алгебра, `resultOwner = null` — правило границы слоёв, долг
  #12): оператор переносит S ПО ЗНАЧЕНИЮ и СОХРАНЯЕТ node identity self-referential
  позиций (поле с recursion-переменной — `ContainsRecursionVariable` — берётся
  узлом-как-есть, без rewire). Передача владения остаётся за стадиями: merge-вызыватели
  алиасят проигравшего владельца (`loser := RefTo(winner)`), и старые back-edges
  прозрачно разыменовываются в merged-переменную.

**Width-rigidity замороженного bound'а** (`GcdFrozenAndOpen`): field-set
замороженного (обобщённого) F-bound'а ЗАКРЫТ — meet с кандидатом валидирует
`fields(candidate) ⊇ fields(frozen)` (недостающее обязательное поле — отказ), общие
поля меет-ятся рекурсивно, лишние поля кандидата — width-slack, в bound не
поглощаются. Результат сохраняет идентичность замороженного bound'а.

`MergeFieldStateGcd` — правило self-ref позиций: оба self → `RefTo(resultOwner)`
(канонический меет), один self + один конкретный → self-ref сохраняется
(инстанцирование: bound — generic-форма, конкретное значение — её обитатель;
Cardelli-Mitchell '89 §3, TAPL §20.2 fold/unfold). Позиция recursion-переменной
после лифта распознаётся и в CS-кодировке (`IsBoundCarrierCs` —
`CS{HasStructBound}`), не только как прямой `RefTo`.

---

## Инвариант контрактивности

### C1 — Optional/Array-разрыв — РЕАЛИЗОВАНО

Для любой `CS{StructBound = S}`: каждый цикл из `S.Fields`, достигающий CS, ОБЯЗАН
пересечь конструктор `StateOptional` или `StateArray`. Enforcement в момент
формирования bound'а, не отложенно (отложенность позволила бы Pull/Push итерировать
по неконтрактивному bound'у и разойтись — высота решётки станет неограниченной):

- при slot-promotion — `StructDescendantClosesContractively` (все замыкающие пути
  контрактивны, любой неконтрактивный — отказ);
- при лифте — `StructHasSelfRef` (существование цикла) на уже отремонтированной
  фазой C форме (все замыкающие рёбра уже обёрнуты в Optional);
- Array-рекурсия поддержана на аннотированных и неаннотированных формах — останов
  `case StateArray` при `fromStruct` в `ThrowIfRecursiveReq` (фаза A).

Плюс слабый ярус на toposort/Tarjan — см. «Двухъярусная контрактивность» выше.

### C2 — ковариантность позиций — РЕАЛИЗОВАНО консервативно (over-approximation)

Формальное требование: `T` в `S.Fields` — только в ковариантных позициях (негативное
вхождение `op: (T) → R` делает F-bounded subtype check неразрешимым в худшем случае —
Pierce, *Bounded Polymorphism is Undecidable*, POPL 1992; ковариантный фрагмент —
Amadio–Cardelli '93, разрешим за O(n²)).

Фактический enforcement — НЕ анализ позиций, а отказ смотреть внутрь Fun:
`ClosingPathVerdict` и `FieldReachesOwner` не спускаются в `StateFun` вообще
(default-ветки). Следствия:

- негативное вхождение `T` через аргумент функции невидимо для анализа замыканий ⇒
  замыкающий путь не найден ⇒ promotion/lift отказывают ⇒ цикл уходит в ошибку —
  требование C2 выполняется;
- НО невидима и рекурсия через return-позицию функции (ковариантную, теоретически
  допустимую) — она отвергается точно так же. Это честная переаппроксимация:
  guard от неразрешимости куплен ценой полноты на Fun-опосредованных μ-типах.

---

## Теорема о principal type — ЦЕЛЬ (three-way непустота — РЕАЛИЗОВАНО)

> **(Теорема PT-F).** Для неаннотированного параметра `n`, чьё тело даёт примитивные
> интервалы `[Dᵢ..Aᵢ]`, структурные ограничения `Sᵢ` и флаги `(optᵢ, cmpᵢ)`,
> **principal type** `n` есть `[D ≤ A, opt, cmp, struct⊆S]`, где
> `D = ⋁ᵢ Dᵢ`, `A = ⋀ᵢ Aᵢ`, `S = GcdBound(S₁,…,Sₖ)` (объединение полей — meet на
> решётке структурных bound'ов), `opt = ⋁ᵢ optᵢ`, `cmp = ⋁ᵢ cmpᵢ`.
> Интервал непуст ⟺ `D ≤ A` И `D` не исключён `S`. При непустоте principal type
> **единствен** с точностью до решёточного равенства.

Статус по частям:
- three-way предикат непустоты `(D, A, S)` — в коде: `StructBoundIsSatisfiable`,
  разделяемый `SimplifyOrNull` (стадийная канонизация) и `IntersectIntervalsOrNull`
  (ядро ⊓);
- независимость осей D и S, S-транспорт операторами — в коде;
- ПОЛНОТА вывода (principal type существует всегда, когда тело well-typed) — не
  доказана и заведомо нарушается переаппроксимацией C2 (Fun-опосредованные μ-типы
  отвергаются). Остаётся проектной рамкой.

---

## Пирамида тестов — РЕАЛИЗОВАНО

- **TIC unit** (`src/Tests/NFun.Tic.Tests/Structs/RecursiveFunctionPrincipalTypeTest.cs`)
  - `RecursiveSafeAccess_SingleField_ProducesMuType` — μ-вывод без реестра
  - `RecursiveSafeAccess_TwoFields_ProducesMuType` — оба замыкающих ребра обёрнуты
    (regression-sentinel правила «wrap EVERY edge»)
  - `RowPolymorphic_NotRecursive_StaysOpenStruct` — нерекурсивная структурная функция
    остаётся open struct (без μ-обёртки)
  - `NonStructRecursion_StaysGeneric` — `recurse(x) = recurse(x)` остаётся α → α
- **Законы S-оси** (`NFun.Tic.Tests/UnitTests/AlgebraStructBoundLawsTest`) — 32 теста:
  законы ∨/∧/⊓ на S, identity-preservation ownerless-режима,
  `MergeGroup_CycleWithBoundCarrier_PreservesStructBound` (репродукция латентного
  бага потери S при cycle-merge).
- **Syntax integration** (`src/Tests/NFun.SyntaxTests/NamedTypes/RecursiveTypeTest.cs`)
  - `RecursiveFunction_LinkedListSum_Unannotated` — односвязный список, полный pipeline
  - `RecursiveFunction_DirectFieldAccess_TreeSum_Unannotated` — двухполевое дерево
  - `RowPolymorphic_LengthFunction_StaysGeneric` — sentinel против over-tagging фазы D

---

## Теоретические ссылки

- Cardelli, Wegner. *On Understanding Types, Data Abstraction, and Polymorphism.* CSUR 1985.
- Canning, Cook, Hill, Olthoff, Mitchell. *F-Bounded Polymorphism for Object-Oriented Programming.* FPCA 1989.
- Pierce. *Bounded Polymorphism Is Undecidable.* POPL 1992.
- Amadio, Cardelli. *Subtyping Recursive Types.* TOPLAS 1993.
- Hosoya, Pierce. *Regular Expression Pattern Matching.* TOPLAS 2003.
- Pottier, Rémy. *The Essence of ML Type Inference.* In ATTAPL, 2005.
- Pierce. *Types and Programming Languages.* MIT Press 2002 (TAPL §20, §22.6, §26).
- Tarjan. *Depth-First Search and Linear Graph Algorithms.* SIAM J. Comput. 1972.
- Kildall '73 / Cousot & Cousot '77 — монотонный dataflow / неподвижные точки (ScCClosurePass).
