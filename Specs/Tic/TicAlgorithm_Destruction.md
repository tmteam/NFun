# TIC Algorithm — Destruction + Finalize

## Обзор

После Pull+Push каждый узел имеет **интервал** `[D..A]` — множество допустимых типов. Destruction+Finalize **выбирают** конкретный тип из интервала.

Полная инвентаризация фаз (порядок соответствует `SolvingFunctions.Destruction` и `SolvingFunctions.Finalize`; вход — toposorted-узлы):

| Подфаза | Механизм | Что делает |
|---------|----------|------------|
| 5a. Попарная резолюция | `DestructionRec` по обратному toposort | Constraint edges → merge/RefTo/конкретизация; попутно валидация и ремонт рекурсивных типов (`ThrowIfRecursiveTypeDefinition`, `TryRepairOptSourcedCycle`, `TryPromoteCSDescendantToStructBound`) |
| 5a′. Пропагация имён | `PropagateTypeNamesAcrossSnapshots` | Штамп `TypeName` на безымянные struct-снапшоты, структурно совместимые с уже-именованным μ-корнем (только named-registry + возможная μ-рекурсия) |
| 5b. Материализация Optional | `MaterializeOptionalFlags` | Оставшиеся `CS[..]?` → `None` или `Opt(inner)` |
| 5c. Flatten | `FlattenNestedOptional` (реактивный) | `Opt(Opt(T)) → Opt(T)`; не отдельный проход — 3 точки вызова, см. §5c |
| 5d. F-bound lifting | `LiftMuTypes` | Безымянный opt-sourced μ-struct → `CS{StructBound=Sμ}`; **успешный lift форсирует Finalize** (см. ниже) |
| 6a. Дереференс | `FinalizeRecursive` | Сжатие RefTo-цепочек, валидация циклов, flatten safety net |
| 6b. Useless generics | `SolveUselessGenerics` | CS-узлы вне сигнатуры → `SolveCovariant`/`SolveContravariant` |
| 6c. Output generics | `TicResultsWithGenerics` | CS в output-типах не резолвятся — интервалы уходят в runtime |
| 6d. Абстрактные границы | `TicTypesConverter` / `ConcreteAncestor` / `ToConcrete` | Материализация абстрактных примитивов (I48, U24, …) в конкретные — см. §6d |

**Контракт LiftMuTypes → Finalize**: `SolvingFunctions.Destruction` возвращает `notSolvedCount == 0 && !anyLifted`. Успешный F-bound lift — доказательство, что тип действительно μ-полиморфен: сигнатура несёт F-bounded generic и нуждается в пути `GenericUserFunction`. Поэтому даже если все top-level узлы решены, `anyLifted=true` возвращает false и `GraphBuilder.SolveCore` обязан вызвать `Finalize`, который построит `TicResultsWithGenerics` (лифтованный CS подхватывается через reachability).

### Принципиальность vs резолюция

TIC вычисляет **principal intervals** — самые тесные интервалы `[D..A]`, совместимые со всеми ограничениями (Pull+Push). Это аналог principal types в HM.

Destruction+Finalize выполняют **non-principal resolution** — выбор конкретного типа из интервала через Preferred/Covariant/Contravariant стратегию. Это design decision языка (не логическая необходимость): литерал `1` с интервалом `[U8..Real]` разрешается в I32, не в U8 или Real.

---

## Предусловие: совместимость после Pull+Push

### Лемма совместимости (не-Optional ось)

После Pull+Push для каждого constraint edge `D →c A` **вне optional-оси**:

```
↓D ≤ ↑A    (Concretest потомка ≤ Abstractest предка)
```

**Обоснование**: Pull вычислил `A.Desc ⊇ LCA{↓Dᵢ}`. Push вычислил `D.Anc ⊆ GCD{↑Aⱼ}`. Constraint edge `D ≤ A` требует `↓D ≤ ↑A`; конфликт был бы обнаружен Pull/Push (`SimplifyOrNull = null` → ошибка типов).

**Ограничение области**: закон `↓CS ≤ ↑CS` **ложен на optional-оси** (контрпример в `Algebra_Concretest.md` §Законы): ↑ сбрасывает ось opt. Для opt-ячеек предусловие заменяется **парным контрактом стадий** (`Algebra_Abstractest.md` §Ось IsOptional): ↑ сбросил ось → Destruction обязана её восстановить (`WrapAncestorInOptional`, материализация §5b). См. §Фаза 5 / Восстановление Optional-оси.

### Фактическое поведение при нарушении

Целевое следствие леммы — «⊓ на constraint edge непусто». Реализация этого **не ассертит**:

- `DestructionFunctions.Apply(ConstraintsState, ConstraintsState)` возвращает `false`, когда `MergeOrNull` даёт null;
- `DestructionRec` **игнорирует** bool-возврат `Destruction(node, ancestor)` — silent continuation: узлы остаются нерешёнными, ребро живо. В удачном случае несовместимость всплывает позже (Finalize-валидация, конверсия); в неудачном — тихо коммитится неточный тип.

История: тот же silent-паттерн в `Apply(ICompositeState, ConstraintsState)` дал BugHunt #75 (функция `f(x) = if(x==0) 'a' else x` тихо получила тип `(int)->arr(Ch)` c коэрцией значений через `VarTypeConverter.ToText`); закрыт **точечным** throw (см. §Правила). Общий assert на null-возврат ⊓ на constraint edge — предложен как debt (внутренняя ошибка алгоритма ≠ пользовательская ошибка типов, падать надо громко).

---

## Фаза 5: Destruction

### Цель

Разрешить оставшиеся ConstraintsState в конкретные типы или RefTo, соединяя пары узлов на constraint edges.

### Порядок

По **обратному** toposort (`SolvingFunctions.Destruction`), пропуская узлы `IsMemberOfAnything` (члены composites обрабатываются через родителя). Для каждого узла `DestructionRec`:

1. VisitMark-дедупликация (свежий mark на каждый top-level вызов — узел посещается один раз; работает и на μ-циклических графах).
2. `ThrowIfRecursiveTypeDefinition` — валидация циклов component links: коиндуктивные циклы через Opt/Array валидны; struct→struct без contractive-разрыва — либо ремонт (`TryRepairOptSourcedCycle` — Optional-обёртка замыкающих рёбер + штамп TypeName; `TryPromoteCSDescendantToStructBound` — CS-internal promotion Descendant → S), либо `RecursiveTypeDefinition`-ошибка.
3. Рекурсивно Destruction **компонентов** (по component links), с дереференсом ссылочных членов (`GetNonReferenced`).
4. `FlattenNestedOptional` для Optional-узла (точка вызова 2 из трёх, см. §5c).
5. Обработка **ancestor-рёбер**: для каждого `D →c A` вызвать `Destruction(D, A)` → дереференс обеих сторон → `DestructionFunctions.Invoke(nonRefAncestor, nonRefDescendant)`; идентичные узлы — no-op.

Диспетчеризация по парам состояний — общая для всех стадий (`StagesExtension.InvokeCore`), включая смешанные Optional-ячейки composite×composite (см. §Восстановление Optional-оси).

### Правила (полная таблица)

Для каждого constraint edge `D →c A`. Правила указаны по перегрузкам `DestructionFunctions.Apply(ancestor, descendant)`.

**Оба решены:**

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive | Primitive | Нет действия (безусловный true — совместимость не перепроверяется, она обеспечена Push). |
| Opt(E) | Primitive(None) | Нет действия: `None ≤ opt(T)` для любого T. |
| Opt(E) | Primitive(non-None) | Implicit lift `T ≤ opt(T)`: `Destruction(D, E)` — примитив ограничивает элемент; ребро `D →c A` снимается. |
| Composite(non-Opt) | Primitive | `Apply(ICompositeState, StatePrimitive)` возвращает false (silent, см. §Предусловие). |
| Primitive(None) | Opt(E) | Принять silently: лифт `None ≤ opt(T)` алгебраически валиден, но прибывает сюда с обращёнными ролями через Fun-рекурсию (контравариантные позиции меняют роли местами) — исторический артефакт, задокументирован в коде `Apply(StatePrimitive, ICompositeState)`. |
| Primitive | Composite | Ошибка `IncompatibleNodes`, если `¬D.CanBePessimisticConvertedTo(A)` и D не `IsOptionalElement` (элементы Optional могут временно нести Opt-состояние до резолюции). |
| Composite | Composite(same kind) | Рекурсивная Destruction по компонентам (см. ниже). |
| Composite | Composite(diff kind, не Opt-пара) | `false` из dispatch (должна была быть обнаружена в Pull). |

**Composite ← Composite (рекурсия):**

| A.State | D.State | Правило |
|---------|---------|---------|
| Opt(E_a) | Opt(E_d) | `Destruction(E_d, E_a)` — ковариантно. |
| Arr(E_a) | Arr(E_d) | `Destruction(E_d, E_a)` — ковариантно. |
| Fun(A_a→R_a) | Fun(A_d→R_d) | **Аргументы контравариантны**: `Destruction(A_a_i, A_d_i)` — первый позиционный аргумент `Destruction(descendantNode, ancestorNode)` — сторона-подтип, поэтому вызов навязывает `A_a_i ≤ A_d_i`. **Возврат ковариантен**: `Destruction(R_d, R_a)` — `R_d ≤ R_a`. Направления зеркалят `FitsInto(StateFun, StateFun)` (`StateExtensions.Fit.cs`: возврат — `target.Ret FitsInto to.Ret`, аргументы — `to.Arg_i FitsInto target.Arg_i`). Историческая инверсия обоих направлений задокументирована комментарием в Fun-arm `DestructionFunctions` — она «работала» только потому что arg/ret-узлы после merge обычно reference-equal. |
| Struct_a | Struct_d | По общим полям: `Destruction(field_d, field_a)` ковариантно. Width subtyping: у потомка могут быть лишние поля. `MutStruct × MutStruct` — поля **инвариантны**: `MergeInplace`. `MutStruct_a ← Struct_d` → `IncompatibleNodes` (immutable не апгрейдится до mutable). Stale-None-lift: поле предка `None` (снапшот Pull Phase 1) против Optional-поля потомка → поле предка := `RefTo(descField)` (лифт `None ≤ opt(T)` тривиален; workaround #5). Если все поля совпали и разыменовались в те же узлы → `A := RefTo(D)`. |
| Arr/Fun/Struct(non-Opt) | Opt(...) | `WrapAncestorInOptional` — восстановление opt-оси, см. ниже. Для Struct-предка с `IsOptionalElement` — вместо обёртки спуск в элемент: `Invoke(A, optD.ElementNode)`. |
| Opt(...) | Arr/Fun/Struct(non-Opt) | `WrapDescendantInOptional`: solved/SyntaxNode потомок → fallback-unwrap `Invoke(E_a, D)` (implicit lift без изменения identity потомка); аналогично для composite-потомка без `IsOptionalElement` и для certified μ-cycle-head. |

**Ancestor решён, descendant — CS** (`Apply(StatePrimitive, ConstraintsState)` / `Apply(ICompositeState, ConstraintsState)`):

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive P | CS | Если `P FitsInto D.CS`: `resolved := P`, **кроме** случая `D.Preferred ≠ null ∧ D-узел.IsOptionalElement ∧ Preferred FitsInto D.CS` — тогда `resolved := Preferred` (см. §Preferred: применение Preferred в Destruction ограничено `IsOptionalElement`). Далее: `D.IsOptional ∧ resolved ≠ None` → `D := Opt(resolved)` + снятие ребра (state стал composite, повторная обработка дала бы `Apply(Prim, Composite)` → IncompatibleNodes); иначе `D := resolved`. Если ¬FitsInto — silent true (D остаётся CS; ребро живо). |
| Composite | CS(IsOptional), A = Opt | Материализация D → `Opt(inner)`, inner-CS наследует Desc/Anc/cmp/Preferred (см. §5b; **S не наследуется**). Cycle guard: если элемент предка разыменовывается в D-узел или в inner — соединение через state-копию/AddAncestor вместо рекурсивного Apply; иначе `Apply(Opt_a, Opt_d)` поэлементно. |
| Composite | CS | Если `A FitsInto D.CS`: `D := RefTo(A)`; при `D.IsOptional`: `D := Opt(inner)`, inner = `RefTo(A)`. |
| Struct_a | CS(Desc=Struct) | `TransformToStructOrNull(D.CS, Struct_a)` → D := struct-форма (при `D.IsOptional` — под Opt(inner)), затем field-by-field Destruction с предком. |
| Opt(E) | CS(Desc=Opt) | `TransformToOptionalOrNull` → D := opt-форма, затем `Apply(Opt_a, Opt_d)`. |
| Opt(E) | CS(¬opt, прочее) | Implicit lift `T ≤ opt(T)`: `Destruction(D, E)` — потомок ограничивает элемент предка; ребро снимается. |
| Composite | CS (ничего не подошло) | `IncompatibleNodes` **только если** `D.HasAncestor ∧ ¬A.FitsInto(D.Ancestor)` — конкретная верхняя граница потомка исключает composite, алгебраического решения нет (закрытие BugHunt #75). Прочие случаи — silent true (back-compat). |

**Descendant решён, ancestor — CS** (`Apply(ConstraintsState, StatePrimitive)` / `Apply(ConstraintsState, ICompositeState)`):

| A.State | D.State | Правило |
|---------|---------|---------|
| CS(IsOptional) | Primitive(None) | Нет действия: None не добавляет информации Optional-ограничению; финальный тип решит §5b. |
| CS | Primitive P | Если `P FitsInto A.CS`: `A.IsOptional` → `A := Opt(P)` (решённый Optional вокруг значения) + снятие ребра; иначе `A := P` (интервал предка схлопывается в точку-потомка). ¬FitsInto → silent true. Preferred предка **не** применяется (ср. с Prim←CS выше). |
| CS | Composite | **Covers-join gate** (см. ниже): если `D FitsInto A.CS` **и** join не несёт Optional-ось сверх потомка — `A := RefTo(D)`; при `A.IsOptional` — `A := Opt(inner)`, inner = `RefTo(D)`. Иначе — recovery-пути по снапшоту (см. §Element-by-element). |

**Оба — CS** (`Apply(ConstraintsState, ConstraintsState)`):

| A.State | D.State | Правило |
|---------|---------|---------|
| CS(opt) | CS(¬opt) | `A := StateOptional(D-узел)` — предок становится Optional **вокруг узла потомка напрямую**; собственный интервал предка (Desc/Anc/cmp/Preferred) **отбрасывается без проверки**. Ребро снимается. Зеркалит правило `CS ← Primitive` (там `Opt(P)`). Мотивация: плоский merge с последующим None-потомком схлопнул бы результат в None, потеряв Optional-семантику. Постусловие «ограничения предка удовлетворены результатом» — **открытое обязательство** (см. Лемма 2). |
| CS(¬opt) ← CS(opt), CS(opt) ← CS(opt), CS(¬opt) ← CS(¬opt) | Плоский `result := MergeOrNull(A, D)` — ⊓ с транспортом осей (`opt := opt_a ∨ opt_d`, `cmp`, `pref`, `S`; `Algebra_Merge.md`). Материализация итогового opt-флага откладывается до §5b. `result = null` → return false (игнорируется, §Предусловие). `result` — StatePrimitive (точечный коллапс) → обоим узлам. Иначе (CS либо сколлапсированный composite) → merge с выбором main (см. ниже), secondary := `RefTo(main)`, ребро снимается. |

### Covers-join gate (RefTo-легальность)

Правило для `Apply(ConstraintsState, ICompositeState)`:

```
A := RefTo(D)  легально  ⟺  D FitsInto A.CS
                             ∧ ¬( A.HasDescendant ∧ JoinCarriesOptionalBeyond(A.Descendant, D) )
```

Резолюция `A := ref(D)` отождествляет результат join'а с **одним** из вкладчиков. Если накопленный join (`A.Descendant`) несёт Optional-ось, которой у потомка нет (if-else с none-веткой: элемент join'а `[U8..]?I32!`, элемент then-ветки `[U8..]I32!`), short-circuit молча стёр бы `?` — вместо него срабатывают recovery-пути ниже.

Семантика `JoinCarriesOptionalBeyond(join, target)`:

- дереференс RefTo с обеих сторон;
- `joinOpt := join ∈ {opt(·), CS[..]?}`; `targetOpt := target ∈ {opt(·), CS[..]?, None}` — **None на стороне target считается optional** (он и есть дно optional-оси);
- `joinOpt ∧ ¬targetOpt ⇒ true`;
- иначе — парный спуск по совпадающим конструкторам (opt/opt с разворачиванием, arr/arr, struct/struct по общим полям);
- **коиндуктивный visited-pair обход по reference-identity** (`RefPairComparer`): повторно встреченная пара ⇒ false (цикл замкнулся — новых осей нет). Для μ-форм с ветвлением depth-only guard был бы экспоненциален; страховочный предел глубины 100.

### Merge: RefTo и выбор main

Когда два CS-узла сливаются, один становится **main** (хранит result-state), другой → `RefTo(main)`:

1. **TypeVariable становится main, когда ровно одна из сторон — TypeVariable** (Named- и SyntaxNode-узлы уходят в RefTo). Код: ancestor main ⟺ `ancestorNode — TypeVariable ∨ descendantNode — не TypeVariable`; иначе main — descendant.
2. Когда обе стороны TypeVariable или обе — нет: main — ancestor.
3. Ребро между ними снимается (`RemoveAncestor`); остальные рёбра **физически остаются** на своих узлах — последующие Destruction-вызовы разыменовывают secondary в main (см. Лемма 3).

Инвариант выбора: у TypeVariable нет внешних обязательств identity, поэтому он безопасно аккумулирует state; Named/SyntaxNode сохраняют разыменовываемую ссылку на него.

### Восстановление Optional-оси: WrapAncestorInOptional

Парный контракт с ↑ (`Algebra_Abstractest.md` §Ось IsOptional): проекция ↑ **сбрасывает** ось opt (`↑[D..A]? = A`) — иначе Pull/Push получили бы несравнимые composite-границы в примитивных слотах; стадии обязаны ось **восстанавливать**. Точки восстановления: материализация §5b (флаг → конструктор) и `StagesExtension.WrapAncestorInOptional` (LCA-расширение предка: `Composite ← Opt(Composite)` ⇒ предок становится `Opt(fresh inner)`, inner наследует state предка, `IsOptionalElement=true`, затем `Apply(Opt, Opt)`).

Правила WrapAncestorInOptional (общие для Pull/Push/Destruction):

- **Identity guard**: если `optB.ElementNode.GetNonReference() == nodeA` — лифт `T ≤ opt(T)` коиндуктивно тривиален (identity T совпадает с элементом opt), обёртка не нужна → true (иначе — бесконечная цепочка re-wrap).
- **Pinned-identity rejection**: обёртка отвергается (`IncompatibleNodes`), когда у узла есть внешнее обязательство identity: не-TypeVariable (SyntaxNode-литерал, Named-переменная), `IsSignatureParam` (форма сигнатуры rigid по контракту), решённый примитив (`IsSolved ∧ StatePrimitive`). TypeVariable с composite-state — НЕ pinned: identity уезжает в inner вместе с состоянием.

### Element-by-element Destruction с Desc snapshot

Когда covers-join gate не пропустил RefTo (¬FitsInto или join несёт лишнюю opt-ось), `Apply(ConstraintsState, ICompositeState)` пробует recovery-пути по порядку:

1. **`DescendantHasOptionalLift(A.Descendant, D)`** — actual-потомок содержит Optional-обёрнутые элементы, которых нет в снапшоте (транзитивно через слои composite, включая struct-поля): снапшот устарел, actual точнее → `A := RefTo(D)`.
2. **Обратный stale-случай** (только Arr): снапшот `arr(opt(·))`, actual `arr(non-opt)` → `A := снапшот`, затем поэлементная Destruction.
3. **Same-kind снапшот**: `A.Desc` — composite того же вида, что D → `A := A.Desc` (при `A.IsOptional` — под `Opt(inner)`, рекурсия в inner), затем покомпонентная Destruction снапшота с D.
4. Иначе — silent true / точечный throw (см. таблицу).

Пути 1–2 — **WORKAROUND** (TicTechnicalDebt #5, stale Pull snapshots): Desc-снапшот создаётся в Pull Phase 1, Phase 2 оборачивает элементы в Optional после — снапшот расходится с actual. Корневая причина — temporal gap двухфазного Pull; чистое решение — single-pass/worklist Pull или invalidation снапшотов.

**Связь с debt #19**: сам снапшот `A.Descendant` порождён `AddDescendant` через снапшот-оператор слоя резолюции `ConcretestSnapshot` (↓ₛ, `StateExtensions.ConcretestSnapshot.cs`) — Preferred-сохраняющий вариант чистой проекции ↓: резолюционные плечи (`Opt(Preferred)`-выбор, hint-carrier для array-элементов) живут в ↓ₛ, потому что Preferred обязан переживать Destruction-снапшоты; сам ↓ — чистая проекция (↓-часть #19 закрыта 2026-07-09, см. `Algebra_Concretest.md` и TicTechnicalDebt #19 «ОСТАТОК»). Открытым в #19 остаётся только Sat/форм-меняющий коллапс решённого композита внутри ⊓ (вместе с #22). Настоящая секция описывает текущий код; правила Destruction от остатка #19 не зависят.

---

## Materialization IsOptional (подфаза 5b)

`MaterializeOptionalFlags` — после попарной резолюции, до Finalize (пропускается, когда Optional-типов в графе нет). Для каждого оставшегося `CS(IsOptional=true)` (включая члены composites, рекурсивно):

| Состояние CS | Результат |
|-------------|-----------|
| `[∅..∅, opt]` — нет Desc, нет Anc, **и не Comparable** | `None` (тип = отсутствие значения: standalone `none`, all-none if-else) |
| `[D..A, opt, ...]` — есть хоть одно из Desc/Anc/cmp | `Opt(inner)`, inner = свежий узел с CS(Desc, Anc, cmp, Preferred), **`inner.IsOptionalElement := true`** |

Inner-CS наследует Desc, Anc, IsComparable, Preferred. **`StructBound` НЕ наследуется** (`ConstraintsState.Of` + Preferred-копия не переносят S) — для `CS{S, opt}` ось S на материализации теряется; сегодня комбинация не возникает (F-bound живёт под внешним Opt), зафиксировано как острая грань.

Comparable-случай: `[∅..∅, opt, cmp]` идёт по второй строке — `Opt([∅..∅, cmp])`; правило «пустой opt → None» требует `!IsComparable`, потому что None сам по себе не comparable (симметрично reject'у `SimplifyOrNull`).

Materialization — **не** оператор алгебры. Это **instantiation**: превращение флага в структуру (Rule B, `Algebra_CanonicalForms.md`: конструктор `opt(τ)` требует места в графе для нерешённого τ — inner-узел и есть это место).

**Redirect SyntaxNode-ов.** После `CS(opt) → Opt(innerNode)` `MaterializeOptionalNode` сканирует все узлы графа: SyntaxNode с `RefTo` на материализованный узел перенаправляется на innerNode. Цель: конкретные значения (литералы) сохраняют non-Optional тип (`42` остаётся I32, не Opt(I32)) — иначе некорректный boxing в runtime.

---

## Flatten nested Optionals (подфаза 5c)

`Opt(Opt(T)) → Opt(T)` — постулат системы типов. Реактивное поддержание канонической формы; **три точки вызова и их обоснование — в `Algebra_CanonicalForms.md` §opt(opt(T))** (установка state, Destruction-обход, Finalize-safety-net) — здесь не дублируются.

---

## Preferred — стратегия выбора

Preferred — hint происхождения (provenance), не constraint; полные правила резолюции и инварианты — **`TicPreferred.md` §5 (Резолюция) и §6 (Инварианты P1–P4)**, здесь не дублируются.

Что важно знать на уровне Destruction/Finalize:

- **Контракт**: Preferred никогда не выбирает тип вне интервала (`CanBeConvertedTo`-проверка во всех точках применения).
- **Область применения в Destruction ограничена**: `Apply(StatePrimitive, ConstraintsState)` применяет Preferred потомка **только при `descendantNode.IsOptionalElement`** — обычный CS-потомок получает примитив-предок как есть. Симметричная ячейка `Apply(ConstraintsState, StatePrimitive)` Preferred не применяет вовсе.
- В merge-ячейках Preferred переносится осью ⊓ (`Algebra_Merge.md` §Preferred); в Finalize — первый приоритет `SolveCovariant`/`SolveContravariant` (§6b); непрорезолвленные CS с Preferred добирает конверсия (`TicTypesConverter`, `TicPreferred.md` §5.3).

---

## Фаза 6: Finalize

### 6a. Дереференс и валидация

`FinalizeRecursive` для каждого узла (обратный toposort):

- **StateRefTo** → сжатие цепочки: если цель разыменовывается в ITypeState — принять state; иначе перенаправить RefTo на конец цепочки.
- **Рекурсивные типы**: повторная `ThrowIfRecursiveTypeDefinition` (циклы через Opt/Array валидны; прямые struct/fun-циклы — ремонт или ошибка).
- **Flatten** nested Optionals после финализации членов (safety net — точка 3 из `Algebra_CanonicalForms.md`).

Если после обхода нерешённых узлов нет — `TicResultsWithoutGenerics`, `SolveUselessGenerics` не вызывается.

### 6b. Резолюция generics (SolveUselessGenerics)

Три шага:

1. **Mark output**: листья output-типов получают outputTypeMark — они НЕ резолвятся (см. §6c).
2. **Inputs**: для `StateFun`-входов — `CollectNotSolvedContravariantLeafs` по аргументам, каждый непомеченный лист: `SolveContravariant`. **Не-StateFun вход** (аргумент user-функции и т.п.): помечается как сигнатурный (сохранить generic) только если несёт composite-state либо CS с Desc/Anc; неограниченный `[∅..∅]`-вход НЕ помечается и на шаге 3 резолвится в Any (известное следствие — debt #6, unconstrained generics → Any).
3. **Body-internal**: все непомеченные CS-узлы → `SolveCovariant(ignorePreferred)`.

**Ковариантная** (`ConstraintsState.SolveCovariant`) — приоритеты:

1. **F-bound материализация**: CS, у которого единственное ограничение — `StructBound` (нет Anc/Desc/Preferred/cmp) → **сама граница S** (iso-recursive packing: self-refs S уже указывают назад в владеющий CS). При IsOptional — `Opt(S)`.
2. Preferred в интервале → Preferred (пропускается при `ignorePreferred=true` — function signatures).
3. `ancestor := Anc ?? Any`; если IsComparable: ancestor не comparable → **unresolved** (остаётся CS); иначе → ancestor.
4. Desc — composite (Array, Struct, Fun, Optional) → Desc (composite информативнее Anc).
5. Anc = ∅ и Desc — абстрактный примитив → `Desc.ConcreteAncestor` (округление ВВЕРХ, см. §6d; интервалы `[abstract..abstract]` до Finalize не доживают — отвергнуты `SimplifyOrNull`).
6. Иначе → ancestor (Anc, либо Any при пустоте).

Если IsOptional: результат оборачивается в `Opt(inner)`; `Any` остаётся `Any` (фактор-отношение `Opt(Any) = Any`).

**Контравариантная** (`SolveContravariant`, аргументы функций) — приоритеты:

1. **F-bound материализация** — то же условие и результат, что и в ковариантной: S — самая узкая форма, удовлетворяющая F-bound-предикату.
2. Preferred в интервале → Preferred (**всегда**, `ignorePreferred` отсутствует — контравариантная резолюция мономорфизирует аргументы предсказуемо: I32 вместо U8).
3. Нет Desc → unresolved (остаётся CS).
4. IsComparable и Desc не comparable (comparable-примитив либо `arr(Char)`) → unresolved.
5. Иначе → Desc (самый узкий). Optional-обёртка как выше.

### 6c. Output generics

CS-узлы в output-типах **не разрешаются** — остаются интервалами (`TicResultsWithGenerics`). Runtime использует Preferred (либо Anc/дефолты конверсии, §6d) для финального выбора. Это ключевой инвариант TIC: солвер не выбирает output-типы — `y = 24` остаётся `[U8..Re]I32!` до слоя конверсии.

Замечание: маркировка «output» покрывает и **не-StateFun входы** с ограничениями (шаг 2 §6b) — их листья тоже сохраняются generic'ами, попадая в `GenericsStates` результата.

### 6d. Материализация абстрактных границ

Абстрактные примитивы (I96, I48, I24, I12, U48, U24, U12, U4) — внутренние точки решётки; наружу они выходят через **три** политики материализации, различающиеся **позицией** абстрактной точки в интервале:

| Абстрактный | (1) desc-позиция: `StatePrimitive.ConcreteAncestor` (ВВЕРХ) | (2) anc-позиция: `TicTypesConverter` (ВНИЗ, при условии Desc) | (3) голая точка: `ToConcrete` (ВВЕРХ) |
|---|---|---|---|
| I96 | Real | Int32 если `D ≤c I32`, иначе Int64 | Int64 |
| I48 | I64 | Int32 если `D ≤c I32`, иначе UInt32 | Int64 |
| I24 | I32 | Int16 если `D ≤c I16`, иначе Int32 | Int32 |
| I12 | I16 | Int8 если `D ≤c I8`, иначе Int16 | Int16 |
| U48 | U64 | UInt32 | UInt64 |
| U24 | U32 | UInt16 | UInt32 |
| U12 | U16 | UInt8 | UInt16 |
| U4  | U8  | UInt8 | UInt8 |

- **(1)** — нижняя граница `[abstract..∅]`: `SolveCovariant` приоритет 5 и no-ancestor-arm `OnlyConcreteTypesConverter` (с opt-обёрткой при IsOptional). Desc — обещание «минимум D»: конкретный результат обязан **вмещать** D → округление вверх.
- **(2)** — верхняя граница `[D..abstract]`: ConstraintsState-arm `OnlyConcreteTypesConverter` с непустым Ancestor. Anc — потолок «максимум A»: конкретный результат обязан **влезать под** A → округление вниз, с уточнением по Desc (I96/I48/I24/I12-ветки).
- **(3)** — точка `[T..T]`, сколлапсированная ⊓ в голый StatePrimitive: `TicTypesConverter.ToConcrete`. Выбрано округление вверх (тип должен вместить все значения точки).

**Позиционная дуальность**: нижние границы округляются вверх, верхние — вниз. Расхождения между политиками реальны и наблюдаемы: `U48 → UInt32` в (2), но `UInt64` в (3); `I96 → Real` в (1), но `Int64` в (3). Это не баг, а следствие разных позиций: одна и та же решёточная точка как обещание снизу и как потолок сверху материализуется в разные конкретные типы.

**Постадийная легальность абстрактной точки** (нормативная формулировка; снимает противоречие с `TicTypeSystem.md` §Абстрактные примитивы, где сказано «коллапс к абстрактному типу — ошибка»):

| Стадия | Коллапс `[T..T]`, T абстрактный |
|---|---|
| ⊓ (`MergeOrNull`, точечный коллапс) | **ЛЕГАЛЕН** — голый абстрактный StatePrimitive живёт как внутреннее состояние (комментарий у `ToConcrete` это фиксирует) |
| Simplify (`SimplifyOrNull`, канонизация Pull/Push) | **ОТВЕРГАЕТСЯ** (null → ошибка типов): абстрактная точка не может стать наблюдаемым результатом без материализации |
| Конверсия (`TicTypesConverter`) | **МАТЕРИАЛИЗУЕТСЯ** по таблице выше |

Формулировка `TicTypeSystem.md` верна только для стадии Simplify; авторитетная постадийная версия — здесь.

---

## Корректность

### Утверждение

Если Pull+Push корректны (теорема минимального интервала), то Destruction+Finalize корректны: для каждого constraint edge `D →c A` после резолюции `D.resolved ≤ A.resolved` — **по модулю открытых обязательств, явно перечисленных ниже** (Лемма 2, случай CS(opt) ← CS(¬opt)).

### Лемма 1: Совместимость (precondition)

**Формулировка**: после Pull+Push, ∀ edge `D →c A` вне optional-оси: `↓D ≤ ↑A`.

**Доказательство**: Pull вычислил `A.Desc ⊇ LCA{↓Dᵢ}`, значит `↓D ≤ A.Desc ≤ ↑A` (Desc ≤ Anc — инвариант непустого интервала, `IntervalIsNonEmpty`/`SimplifyOrNull`). Push: `D.Anc ⊆ GCD{↑Aⱼ}`, значит `↓D ≤ D.Anc ≤ ↑A`. ∎

**Optional-ось**: не покрывается (закон `↓ ≤ ↑` на ней ложен) — корректность opt-ячеек держится на парном контракте ↑/Destruction (§Фаза 5) и транспорте `opt := ∨` в ⊓.

**Следствие (целевое)**: `⊓` на constraint edge непусто. **ОПРОВЕРГНУТО эмпирически (2026-07-10, assert-попытка долга #25)**: DEBUG-panic в ячейке `Apply(CS, CS)` при merge-null вне optional-оси сработал на 6 зелёных Syntax-тестах — предпосылка леммы (полнота Pull-вкладов, теорема минимального интервала) на этих рёбрах не выполняется. Контрпримерные семейства: `[F32..Re]` vs `[U4..I96]I32!` (`avg()` — Floats), `[Re..]` vs `[U4..I96]I32!` (generic-fn `choise(0x1, 2.0, b)`, апкаст `[[0x1],[1.0],[0x1]]`), `[[..]..]` vs `[U4..Re]I32!` (fun-LCA через Fun-arm). Скрипты живут благодаря silent continuation (null → `false` из Apply, `DestructionRec` игнорирует; узлы остаются нерешёнными, Finalize добирает; история BugHunt #75). Сигнал — TraceLog на этом пути; assert возможен только после восстановления полноты Pull-вкладов (долг #25, сужен). Само доказательство леммы выше остаётся верным ПРИ её предпосылке — опровергнута применимость предпосылки, не вывод.

### Лемма 2: Резолюция (per-rule)

Для каждого правила таблицы: precondition (совместимость) → postcondition (resolved ∈ interval). Знак ∎ ставится только на доказанные случаи.

**Primitive P ← CS[D..A]**: `D := P` при `P FitsInto CS` — постусловие по определению FitsInto (единый предикат, `Algebra_Fit.md`). Вариант `D := Preferred` — только при `IsOptionalElement ∧ Preferred FitsInto CS`, постусловие то же. Вариант opt: `D := Opt(P)`, `P ≠ None`, `P ∈ [D..A]` проверено, лифт `T ≤ opt(T)` валиден. ∎

**CS[D..A] ← Primitive P**: `A := P` при `P FitsInto CS` — точечный выбор из интервала. Вариант opt: `A := Opt(P)` — восстановление оси на предке (парный контракт). ∎

**CS ← Composite (RefTo)**: `A := RefTo(D)` при `D FitsInto A.CS` — resolved(A) = resolved(D) ∈ interval(A) по FitsInto. Covers-join gate — дополнительное **необходимое** условие: без него результат терял бы Optional-ось join'а (не расширение множества решений, а сужение легальности — corrections only). ∎ (в предположении корректности `JoinCarriesOptionalBeyond`; его коиндуктивная схема — стандартная Amadio–Cardelli visited-pair, отдельного доказательства здесь нет)

**CS ← Composite (снапшот)**: `A := A.Desc` (Composite из Pull), рекурсивная покомпонентная Destruction. Индукция по глубине composite: база — Primitive-компоненты (случаи выше); шаг — по гипотезе индукции. Снапшот валиден: `A.Desc` установлен Pull как `LCA{↓Dᵢ}` и лежит под ↑A (инвариант непустого интервала). ∎ (модуль workaround-путей 1–2 §Element-by-element — они компенсируют stale-снапшоты и опираются на то, что actual/снапшот различаются только Optional-лифтом)

**Composite ← CS**: симметрично: `D := RefTo(A)` при `A FitsInto D.CS`; struct/opt-transform-пути — та же индукция. ∎

**CS(¬opt) ← CS(¬opt)** (и обе opt-пары через плоский merge): `result := A ⊓ D`. По свойству ⊓ (`Algebra_Merge.md`): `Sat(result) = Sat(A) ∩ Sat(D)` (по модулю задокументированных отклонений M-серии). Оба узла указывают на result → оба интервала соблюдены. ∎

**CS(opt) ← CS(¬opt)**: реализованное правило — `A := StateOptional(D-узел)`, интервал предка отброшен. **Постусловие НЕ доказано — открытое обязательство.** Что известно: (а) верхняя граница предка была протолкнута Push в потомка (`D.Anc ⊆ GCD{↑A}`), т.е. частично сохранена в D; (б) прочие потомки `Y →c A` сохраняют собственные рёбра и переобработаются против нового состояния `opt(D)`; (в) `cmp`/`Preferred` предка теряются безвозвратно. Пункты (а)–(б) — эвристика реализации, не доказательство; контрпример не известен, но и инвариант не выведен. Обязательство остаётся открытым до формализации (кандидат: показать, что после Push интервал предка всегда поглощён интервалом потомка на таких рёбрах).

**Fun(A_a→R_a) ← Fun(A_d→R_d)**: требуется `Fun_d ≤ Fun_a` ⟺ (∀i: `A_a_i ≤ A_d_i`) ∧ `R_d ≤ R_a` (контравариантность аргументов, ковариантность возврата — та же схема, что в `FitsInto(StateFun, StateFun)`). Код вызывает `Destruction(A_a_i, A_d_i)` — первый позиционный аргумент занимает позицию подтипа, что навязывает ровно `A_a_i ≤ A_d_i`; `Destruction(R_d, R_a)` навязывает `R_d ≤ R_a`. По индукции по компонентам. ∎

**Opt/Arr ← Opt/Arr**: ковариантный спуск `Destruction(E_d, E_a)`: `E_d ≤ E_a ⟺ F(E_d) ≤ F(E_a)`. По индукции. ∎

**Struct_a ← Struct_d**: по общим полям ковариантно; width subtyping — лишние поля потомка не нарушают; MutStruct-пары — инвариантность через `MergeInplace` (равенство ⊆ взаимное ≤). По индукции по полям. ∎

**Opt(E) ← Primitive(non-None) / CS(¬opt)**: implicit lift — достаточно `D ≤ E`, тогда `D ≤ E ≤ Opt(E)`. По индукции. ∎

**Opt(E) ← None**: `None ≤ Opt(T)` — постулат. ∎

**Opt(E) ← CS(opt)**: материализация D → Opt(inner), inner наследует интервал; далее ковариантный случай Opt ← Opt. По индукции. ∎ (наследование S — не выполняется, см. §5b)

### Лемма 3: Перенос (merge)

**Формулировка**: при merge(A, D) → main, secondary := RefTo(main) все рёбра secondary остаются удовлетворимыми.

**Механизм**: рёбра физически НЕ переносятся — они остаются на secondary; каждый последующий `Destruction(X, Y)` начинается с `GetNonReference()` обеих сторон, так что ребро secondary фактически обрабатывается против main.

**Исходящие рёбра** (`secondary →c X`): ребро требует `resolved ≤ X.resolved`. `result = A ⊓ D ⊆ interval(secondary)`, значит любой выбор из result удовлетворяет `≤ ↑secondary ≤ ↑X` (Лемма 1 для этого ребра). ∎

**Входящие рёбра** (`Y →c secondary`): после дереференса — ребро `Y →c main`; `↑main = ↑(A ⊓ D) ≤ ↑secondary` (⊓ сужает), и `↓Y ≤ ↑secondary` (Лемма 1) — но требуется `↓Y ≤ ↑main`, что из сужения НЕ следует автоматически; оно следует из того, что ⊓ включил вклад ребра `Y →c secondary` ещё на Pull (Y ∈ LCA-вкладе Desc secondary, а Desc-слоты входят в ⊓). ∎ (в предположении полноты Pull-вкладов — часть теоремы минимального интервала)

### Лемма 4: Materialization

**Формулировка**: `CS[D..A, opt, cmp?, pref?] → Opt(inner)`, inner = `CS[D..A, cmp, pref]`; `[∅..∅, opt, ¬cmp] → None`.

**Доказательство**: None — валидный тип пустого optional-ограничения (единственный сатисфаер `[∅..∅]?` без дополнительных осей). Для непустого: любой `T ∈ [D..A]` даёт валидный `Opt(T)`; cmp и pref наследуются inner-узлом, который остаётся под рёбрами (Rule B / refinability, `Algebra_CanonicalForms.md`). `IsOptionalElement=true` на inner включает Preferred-ячейку Destruction (§Preferred). ∎ (S-ось — вне леммы: не наследуется, §5b)

### Лемма 5: Finalize (SolveCovariant/Contravariant)

**Формулировка**: для CS `[D..A, cmp, pref, S]` результат Solve* либо unresolved (сам CS), либо T с FitsInto(T, CS) = true.

**Доказательство по случаям** (ковариантная; контравариантная — те же случаи с Desc вместо Anc):

1. **F-bound arm**: единственное ограничение — S → T = S. `FitStructBound(S, S)` истинен рефлексивно: width `Fields(S) ⊇ Fields(S)`; покомпонентная проверка на identical-полях замыкается коиндуктивным guard'ом пары (S, S). Opt-обёртка — валидный лифт. ∎
2. `Preferred ∈ [D..A]` → T = Pref: `D ≤ Pref ≤ A` по условию выбора (`CanBeConvertedTo` = FitsInto). ∎
3. cmp-ветка: не-comparable ancestor → **unresolved** (CS возвращается — обязательство живо, ошибки нет); comparable ancestor → T = Anc, `D ≤ A ≤ A`. ∎
4. Desc-composite → T = Desc: `D ≤ D ≤ A`. ∎
5. Абстрактный Desc без Anc → T = `ConcreteAncestor(D)`: `D ≤ ConcreteAncestor(D)` по построению (§6d, округление вверх), Anc = ∅ ≡ Any. ∎
6. Иначе T = Anc ?? Any: тривиально. ∎

Optional-обёртка сохраняет FitsInto по opt-ячейке единого предиката (лифт валиден); `Opt(Any) = Any` — фактор-отношение. ∎

### Ограничение

Корректность условна: runtime conversions корректны (`A ≤ B` → значение A безопасно используется как B). Это **аксиома реализации** — верифицируется per-conversion тестами, не доказывается в TIC-алгебре.

---

## Пример

Продолжение из `TicAlgorithm.md`: `y = if(x > 0) x else 1`.

`PropagatePreferred` (выполняется между Pull и Push, см. `GraphBuilder.SolveCore`) уже разнёс `I32!` по CS-узлам с примитивным Desc. После Pull+Push:

```
x:  [∅..Real, cmp]            (Named)
0:  [U8..Real, I32!]          (SyntaxNode)
1:  [U8..Real, I32!]          (SyntaxNode)
T': [U8..Real, cmp, I32!]     (TypeVariable, свежий из сигнатуры >)
R:  [U8..∅, I32!]             (SyntaxNode, результат if-else)
y:  [U8..∅, I32!]             (Named; Desc=U8 — примитив, тоже получил hint)

Edges: x →c T', 0 →c T', x →c R, 1 →c R, R →c y
```

**Destruction** (обратный toposort: y, R, T', x, 1, 0), обработка ancestor-рёбер каждого узла:

```
y: ancestor'ов нет → пропуск.

R: ancestor y.  Оба CS, оба ¬opt.
   ⊓([U8..∅, I32!], [U8..∅, I32!]) = [U8..∅, I32!].
   Main: y — Named, R — SyntaxNode (обе не-TypeVariable) → main = ancestor y.
   y := [U8..∅, I32!],  R := RefTo(y).

T': ancestor'ов нет → пропуск.

x: ancestors T', R.
   Destruct(x, T'): ⊓([U8..Real,cmp,I32!], [∅..Real,cmp]) = [U8..Real,cmp,I32!].
     Main: T' — TypeVariable → main = T'.  x := RefTo(T').
   Destruct(x, R): дереференс x → T', R → y.
     ⊓([U8..∅,I32!], [U8..Real,cmp,I32!]) = [U8..Real,cmp,I32!].
     Main: y — Named, T' — TypeVariable (ровно одна TypeVariable) → main = T'.
     T' := result,  y := RefTo(T').

1: ancestor R → дереференс → T'.  ⊓ без изменений.  1 := RefTo(T').
0: ancestor T'.  ⊓ без изменений.  0 := RefTo(T').
```

После Destruction: T' = `[U8..Real, cmp, I32!]`, все остальные — RefTo(T') (напрямую или через цепочку). Узлы не решены → Destruction возвращает false → Finalize.

**Finalize**: 6a сжимает цепочки (R: RefTo(y) → RefTo(T')). 6b: y — output-узел, его лист T' получает outputTypeMark → **не резолвится** (§6c: output generics остаются интервалами). Результат — `TicResultsWithGenerics`, T' = `[U8..Real, cmp, I32!]`.

**Конверсия** (`TicTypesConverter.Concrete`): CS с Preferred → I32. Итог: `x = I32, y = I32` — но выбор сделан слоем конверсии, не солвером (ключевое отличие от наивного «SolveCovariant выбрал I32»: T' помечен как output и Solve* его не касается).

---

## Сложность

| Подфаза | Сложность |
|---------|-----------|
| 5a. Попарная резолюция | O(|constraint_edges| + |component_links|) — mark-дедупликация |
| 5a′. PropagateTypeNames | O(|graph|) — два прохода + O(1)-lookup по field-индексу имён |
| 5b. Materialization | O(|nodes| + |materialized| × |nodes|) — redirect-скан SyntaxNode-ов линеен по графу на каждый материализованный узел (квадратичный уголок; materialized обычно единицы) |
| 5c. Flatten | O(|component_links|) суммарно (реактивные точки) |
| 5d. LiftMuTypes | O(|graph|) |
| 6a. Finalize dereference | O(|nodes| + |component_links|) |
| 6b. SolveGenerics | O(|generics|) |
| **Итого** | **O(|graph|)** при малом числе материализаций |
