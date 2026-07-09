# TIC Simple Path (SimplePrimitiveSolver)

Fast-path вывода типов для выражений, состоящих только из примитивов. Когда composite-типов
нет, полный TIC вырождается в монотонный dataflow на конечной решётке — SPS
(`SimplePrimitiveSolver`) решает его интервальной арифметикой на ординалах за O(N).

Контракт: SPS либо возвращает результат, СОВПАДАЮЩИЙ с полным TIC (§8), либо
воздерживается (`null`) — и caller проваливается в полный TIC. Воздержание всегда
безопасно; ложный результат — никогда не допустим.

---

## 1. Решётка

### 1.1 Примитивная решётка L

L — конечная решётка из **23 элементов** с частичным порядком ≤ (subtyping).
Ординалы НЕ захардкожены в SPS — они производные от `StatePrimitive.Order`
(`(int)Name >> 6`); `PRIM_COUNT = 23` обязан совпадать с `StatePrimitive.LatticeSize`.

| Order | Тип | Флаги |
|-------|-----|-------|
| 0 | Any | top |
| 1 | Char | comparable |
| 2 | Bool | — |
| 3 | Ip | — |
| 4 | Real | numeric, comparable |
| 5 | F32 | numeric, comparable |
| 6 | I96 | numeric, comparable, **abstract** |
| 7 | I64 | numeric, comparable |
| 8 | I48 | numeric, comparable, **abstract** |
| 9 | I32 | numeric, comparable |
| 10 | I24 | numeric, comparable, **abstract** |
| 11 | I16 | numeric, comparable |
| 12 | I12 | numeric, comparable, **abstract** |
| 13 | I8 | numeric, comparable |
| 14 | U64 | numeric, comparable |
| 15 | U48 | numeric, comparable, **abstract** |
| 16 | U32 | numeric, comparable |
| 17 | U24 | numeric, comparable, **abstract** |
| 18 | U16 | numeric, comparable |
| 19 | U12 | numeric, comparable, **abstract** |
| 20 | U8 | numeric, comparable |
| 21 | U4 | numeric, comparable, **abstract**, низ числовой решётки |
| 22 | None | частичный bottom |

Абстрактные типы (I96, I48, I24, I12, U48, U24, U12, U4) существуют только как
внутренние границы constraint'ов; runtime-значение такого типа не бывает. Их
конкретизация — `StatePrimitive.ConcreteAncestor`: I96→Real, I48→I64, I24→I32,
I12→I16, U48→U64, U24→U32, U12→U16, U4→U8.

### 1.2 Диаграмма Хассе

```
                     Any (0)
                /   |    |    \
           Real(4) Bool(2) Char(1) Ip(3)
             |
           F32 (5)
             |
           I96 (6)
          /       \
      I64(7)      U64(14)
        |            |
      I48(8)      U48(15)
        |            |
      I32(9)      U32(16)
        |            |
      I24(10)     U24(17)
        |            |
      I16(11)     U16(18)
        |            |
      I12(12)     U12(19)
        |            |
      I8(13)      U8(20)
          \        /
           U4 (21)
```

**Цепи**:
- Знаковая: `U4 ≤ I8 ≤ I12 ≤ I16 ≤ I24 ≤ I32 ≤ I48 ≤ I64 ≤ I96 ≤ F32 ≤ Real ≤ Any`
- Беззнаковая: `U4 ≤ U8 ≤ U12 ≤ U16 ≤ U24 ≤ U32 ≤ U48 ≤ U64 ≤ I96`
- `F32` лежит между целочисленной вершиной и Real: `∀ int-тип T: T ≤ F32`,
  `F32 ≤ Real` (`LCA(int, F32) = F32`, `GCD(int, F32) = int`).

**Кросс-связи signed×unsigned** задаются формулой по битовой ширине
(`StatePrimitive.FillLcaGcdMaps`), а не перечислением:
- `LCA(U_m, I_n)` = наименьший знаковый `I_k` с `k ≥ max(n, m+1)` (плюс бит на знак);
- `GCD(U_m, I_n)` = наибольший беззнаковый `U_j` с `j ≤ min(m, n−1)` (общее безопасное
  подмножество).

`U4` — низ числовой решётки с номинальной шириной **7** (диапазон 0..127 = I8 ∩ U8):
в формуле width=7 кодирует «влезает и в I8, и в U8». Благодаря U4 литерал `100`
подходит и для `int8 = 100`, и для `uint8 = 100` из одного бина.

**Не-числовые**: `Bool ≤ Any`, `Char ≤ Any`, `Ip ≤ Any` — попарно несравнимы между
собой и с числами. **None**: `None ≤ Any` и только (частичный bottom — несравним со
всеми остальными).

### 1.3 LCA (join) и GCD (meet)

`LCA(a,b)` — наименьший общий предок; `GCD(a,b)` — наибольший общий потомок, `null`
(в SPS — `OPEN`) если общего потомка нет.

Реализация: предвычисленные плоские таблицы `s_lcaTable` / `s_gcdTable`
(`byte[23 × 23]` = 529 байт каждая). Таблицы строятся в статическом конструкторе
SPS **брутфорсом по авторитетным методам решётки** — `GetLastCommonPrimitiveAncestor`,
`GetFirstCommonDescendantOrNull`, `CanBePessimisticConvertedTo` для всех пар из
`s_allPrimitives` — а не дублируют определение решётки. Расхождение SPS-таблиц с
полной решёткой исключено по построению.

Свойства (закреплены property-тестами `AlgebraLatticeLawsTest` на полной 23-точечной
решётке — точность LUB/GLB против независимого оракула, монотонность, поглощение):
коммутативность, ассоциативность, монотонность, идемпотентность, `LCA(a,Any)=Any`,
`GCD(a,Any)=a`.

**Несравнимые пары (GCD = OPEN)**: любая пара из разных не-числовых ветвей
(Bool/Char/Ip между собой и против чисел), `None` против всего, кроме Any и None.

Примеры: `GCD(Bool, I32) = OPEN`; `LCA(U16, I32) = I32`; `GCD(U16, I32) = U16`;
`LCA(U32, I16) = I48`; `GCD(U32, I16) = U12`; `LCA(I8, U8) = I12`; `GCD(I8, U8) = U4`.

### 1.4 Предикаты Fits и Comparable

`Fits(a, b)` = «a ≤ b» = неявная конверсия допустима ⟺ `LCA(a,b) = b`. Таблица
`s_fitsTable` (`bool[23 × 23]`), брутфорс из `CanBePessimisticConvertedTo`.
`Fits(None, Any) = true`; `Fits(None, x) = false` для прочих x ≠ None.

`s_comparableTable` (`bool[23]`) — снимок `StatePrimitive.IsComparable`
(numeric ∪ {Char}); используется на фазе Resolve (§6.1, паритет-фикс §8.1).

---

## 2. Constraint-язык

### 2.1 Переменные

V = {g₀, …, g_{n-1}} — множество **групп**. Каждый AST-узел и каждая именованная
переменная приписаны к группе. Группы объединяются union-find'ом (`Find` с path
compression, `Unite` по рангу).

### 2.2 Состояние группы

Каждая группа несёт интервал и метаданные (struct `Group`, ~12 байт):

- `Desc ∈ L ∪ {OPEN}` — нижняя граница (OPEN = 0xFF — не ограничена);
- `Anc ∈ L ∪ {OPEN}` — верхняя граница;
- `Pref ∈ L ∪ {OPEN}` — hint резолюции (мягкий, на выполнимость не влияет);
- `Comparable ∈ {true,false}` — группа обязана резолвиться в comparable-тип;
- `Parent`, `Rank` — union-find.

### 2.3 Формы constraint'ов

1. **Тождество**: `gᵢ ≡ gⱼ` — `Unite(i, j)`.
2. **Конкретный тип**: `SetConcrete(g, t)` — интервал-точка `[t..t]`.
3. **Интервал**: `SetInterval(g, d, a, p)`.
4. **Subtyping-ребро**: `gᵢ ≤ gⱼ` — `AddEdge`, обрабатывается в Propagate.
5. **Generic constraint**: `ApplyGenericConstraint(g, c)` — границы и comparable из
   `GenericConstrains` сигнатуры.
6. **Preferred**: `pref[g] = p` — hint.

### 2.4 Выполнимость

Группа выполнима ⟺ `Desc = OPEN ∨ Anc = OPEN ∨ Fits(Desc, Anc)`. Нарушение
где бы то ни было ⇒ флаг `_failed` (fail-fast, §5).

---

## 3. Gate (предусловие)

Корректность SPS требует, чтобы в constraint-систему не попал ни один composite-тип.
Gate — двухуровневый фильтр плюс условие на call-site.

### 3.0 Call-site

`RuntimeBuilderHelper.SolveBodyOrThrow` вызывает `SimplePrimitiveSolver.SolveOrNull`
только при `namedTypeFieldRegistry == null && syntaxTree.IsSimpleBody`. Наличие
объявленных именованных типов сразу отключает SPS.

### 3.1 Уровень 1: AST-gate (`IsSimpleBody`)

Определяется на проходе нумерации (`SetNodeNumberVisitor.CheckSimple`) — ноль
дополнительной стоимости. Первый неподходящий узел сбрасывает `IsSimpleBody = false`.

**Принимаются** (исчерпывающий список из `CheckSimple`): `GenericIntSyntaxNode`,
`IpAddressConstantSyntaxNode`, `NamedIdSyntaxNode`, `BinOperatorSyntaxNode`,
`UnaryOperatorSyntaxNode`, `FunCallSyntaxNode`, `IfThenElseSyntaxNode`,
`IfCaseSyntaxNode`, `ComparisonChainSyntaxNode`, `EquationSyntaxNode`,
`VarDefinitionSyntaxNode`, `SyntaxTree`; `ConstantSyntaxNode` — только если
`OutputType` numeric / Bool / Real / Char / Ip.

**Отвергается** всё остальное (default-ветка): `ArraySyntaxNode`,
`StructInitSyntaxNode`, `AnonymFunctionSyntaxNode`, `SuperAnonymFunctionSyntaxNode`,
`StructFieldAccessSyntaxNode`, `DefaultValueSyntaxNode`, `TypeDeclarationSyntaxNode`
и любой не перечисленный тип узла; текстовые/массивные константы.

### 3.2 Уровень 2: семантический gate (внутри `SolveOrNull` / `WalkAndCheck`)

Даже при `IsSimpleBody = true` SPS возвращает `null`, если:

1. **Apriori-типы**: `AllAprioriTypesArePrimitive` — все внешние типы переменных
   numeric / Bool / Char / Ip / Any / Empty.
2. **Аннотации типов**: `IsSimpleTypeSyntax` — синтаксис типа пуст или именованный
   примитив. Распознавание ДЕЛЕГИРОВАНО `TypeSyntaxResolver.TryResolvePrimitiveKeyword`
   — единый реестр ключевых слов с алиасами (`int ≡ int32`, `byte ≡ uint8`,
   `sbyte ≡ int8`, `float64 ≡ real`, плюс `int8/int16/int64`, `uint*`, `real`,
   `bool`, `char`, `ip`, `any`, `float32`, `text`). Оговорка: `text` проходит
   keyword-проверку, но `ToPrimitiveOrd(Text) = OPEN` ⇒ `SetConcreteChecked`
   выставляет `_failed` — SPS воздерживается корректно.
3. **Float-family gate диалекта**: при
   `FloatFamilySupport = AccordingToRealBehaviour` аннотации `float32`/`float64`
   (`IsFloatFamilyKeyword`) заставляют SPS вернуть `null` ДО резолюции типа — чтобы
   полный TIC выбросил штатную parse-ошибку о недоступности float-семейства в этом
   диалекте, а не SPS молча преуспел или упал не тем кодом.
4. **Сигнатуры функций**: `IsSimpleSignature` — либо `PureGenericFunctionBase` ровно
   с одним constraint'ом, либо `IConcreteFunction` со всеми типами из
   `IsPrimitiveBaseType` (Bool, Char, Ip, **Any**, Real, **Float32**, Int8..Int64,
   UInt8..UInt64).
5. **Top-level**: `UserFunctionDefinitionSyntaxNode` и `TypeDeclarationSyntaxNode` —
   немедленный отказ.
6. **Неизвестные узлы**: default-ветка `WalkAndCheck` возвращает false.

### 3.3 Аргумент полноты gate'а

Gate — необходимое условие корректности: composite-типы требуют вариантности и
структурной декомпозиции, которых SPS не моделирует, а bidirectional-семантика
`Unite` корректна только для примитивов (§7). Уровень 1 отсекает все
composite-порождающие AST-узлы; уровень 2 — composite-типы, приходящие через
сигнатуры, аннотации и apriori-типы. Вместе они гарантируют: в систему попадают
только элементы L.

---

## 4. Фаза Build

Единый слитый AST-проход `WalkAndCheck`: gate-проверка и генерация constraint'ов
одновременно; первый неподходящий узел — немедленный `null` без лишней работы.
Побочный эффект: резолвленные сигнатуры кэшируются на syntax-узлах
(`ResolvedSignature`) — полный TIC при fallback'е их переиспользует.

### 4.1 Операции

**NewGroup() → g** — свежая группа: Desc=Anc=Pref=OPEN, Comparable=false.

**GetOrCreateNodeGroup(nodeId) / GetOrCreateVarGroup(name)** — группа AST-узла /
переменной (имена case-insensitive).

**SetConcrete(g, t)**: валидация против существующих границ
(`Fits(Desc,t)`, `Fits(t,Anc)`, иначе `_failed`), затем `Desc = Anc = t`. Pref не
трогается — конкретный тип сам себе резолюция. **SetConcreteChecked** дополнительно
фейлит на `t = OPEN` (непримитивный `FunnyType`).

**SetInterval(g, d, a, p)**: `Desc = d, Anc = a`; Pref — только в пустой слот.
Предусловие: вызывается на свежих группах (литералы), кросс-валидации нет.

**Unite(a, b)**: union-find merge по рангу + `MergeInto(root, other)`:
- `Desc := LCA(Desc_root, Desc_other)` (OPEN — нейтраль);
- `Anc := GCD(Anc_root, Anc_other)`; GCD = OPEN ⇒ **`_failed`** (несовместимые
  верхние границы, например I96 против Bool);
- пост-проверка `Fits(Desc, Anc)` ⇒ иначе **`_failed`**;
- `Comparable := OR`; `Pref` — first-wins (пустой слот заполняется).

**ApplyGenericConstraint(g, c)**: `Desc := LCA(·, c.Descendant)`,
`Anc := GCD(·, c.Ancestor)` (GCD-null ⇒ `_failed`), `Comparable |= c.IsComparable`.

**AddEdge(from, to)** — направленное subtyping-ребро, обрабатывается в Propagate.

### 4.2 Генерация constraint'ов по узлам

**GenericIntSyntaxNode** — бины идентичны полному TIC
(`TicSetupVisitor.Visit(GenericIntSyntaxNode)`, см. `TicPreferred.md` §2.1-2.2):

*Десятичные* (`SetupDecimal` / `SetupDecimalPositive`): ancestor Real; desc
`U4`(≤127) / `U8` / `U12` / `U16` / `U24`(≤int.Max) / `U32` / `U48`; Pref — диалект
(`_preferredIntOrd`), с промоушеном в **I64** для значений > int.MaxValue;
`> long.MaxValue` — конкретный U64. Отрицательные: desc `I8` / `I16` / `I32`
(Pref диалект) / `I64` (Pref I64 — ниже int.MinValue).

*Hex/bin* (`SetupHexBin` / `SetupHexBinPositive`): ancestor `I96` (не Real —
битовые литералы); те же desc-бины `U4..U48`, Pref I32/I64 по величине;
отрицательные — `[I8|I16|I32 .. I64, P=I32]`, ниже int.MinValue — конкретный I64.

**IpAddressConstantSyntaxNode**: `SetConcrete(g, Ip)`.

**ConstantSyntaxNode**: `SetConcreteChecked(g, ToPrimitiveOrd(OutputType))` (уровень 1
уже проверил примитивность).

**NamedIdSyntaxNode** (`SetupNamedId`): имя уже известно как переменная →
`Unite(varGroup, nodeGroup)`; иначе константа из реестра → `SetConcreteChecked`;
иначе новая переменная → `Unite`. Классификация пишется в `node.IdType`.

**BinOperatorSyntaxNode / UnaryOperatorSyntaxNode / FunCallSyntaxNode**: gate
`IsSimpleSignature`, дети обходятся первыми. Для `PureGenericFunctionBase` —
`ApplyGenericConstraint(resultGroup, Constrains[0])` + `Unite` всех аргументов с
result-группой (result-группа служит generic-группой — без аллокаций); группа
записывается в `_callGenericGroup[orderNumber]`. Для `IConcreteFunction` —
`SetConcreteChecked` на все аргументы и результат. Pow — спецслучай (§10).

**IfThenElseSyntaxNode**: условия — `SetConcrete(Bool)`; каждая ветвь и else —
`AddEdge(branchGroup, resultGroup)`.

**ComparisonChainSyntaxNode** — §9.

**EquationSyntaxNode**: аннотация (если есть) → `SetConcreteChecked(varGroup, тип)`
(float-family gate §3.2 — до резолюции); **SetDef-seeding Preferred**: если
expr-группа конкретна (`Desc = Anc ≠ OPEN`), её тип пишется в `Pref` var-группы
(пустой слот) — зеркало `GraphBuilder.SetDef`; затем `AddEdge(exprGroup, varGroup)`.

**VarDefinitionSyntaxNode**: аннотация → `SetConcreteChecked(varGroup, тип)`.

---

## 5. Фаза Propagate

Итерация до неподвижной точки по направленным рёбрам (`Propagate`). Модель отказа —
**fail-fast**: любое противоречие немедленно выставляет `_failed`, и `Propagate`
прерывается (`if (_failed) return`) — недостижимые состояния не досчитываются.

### 5.1 Transfer-функция

Для ребра (from, to), приведённого к union-find-корням (совпали — skip):

**T1. Push desc (from → to)**: `NarrowDesc(to, Desc_from)` —
`Desc_to := LCA(Desc_to, Desc_from)`; пост-проверка `Fits(Desc_to, Anc_to)`,
нарушение ⇒ `_failed`.

**T2. Pull anc (to → from)**: `NarrowAnc(from, Anc_to)` —
`Anc_from := GCD(Anc_from, Anc_to)`; **GCD = OPEN ⇒ `_failed`** (несовместимые
ancestors); пост-проверка `Fits(Desc_from, Anc_from)` ⇒ иначе `_failed`.

**T3. Concrete desc pull (to → from)**: если to конкретна (`Desc_to = Anc_to ≠ OPEN`)
— `NarrowDesc(from, Desc_to)`. Зеркалит Destruction полного TIC
(конкретный ancestor уточняет constraint-descendant).

**T4. Propagate anc (from → to)**: `NarrowAnc(to, Anc_from)`. Прямого аналога в TIC
нет — правило схлопывает Push + Destruction в один шаг; корректно для примитивов
(нет вариантности и структурных компонент).

*Честная оговорка о полноте (T2/T4 + fail-fast)*: GCD верхних границ двух ветвей может
быть пуст там, где полный TIC успешно решает выражение через LCA в точке join'а
(if-else с несравнимыми ancestors ветвей). SPS в этом случае фейлится и воздерживается
— результат КОРРЕКТЕН (fallback в полный TIC), но не бесплатен: ложное воздержание
оплачивается полным прогоном TIC и держится на том, что fallback существует. Это
задокументированная потеря полноты, а не корректности.

**T5. Comparable (двунаправленно)**: флаг OR-ится в обе стороны (constraint обязан
дойти до переменных).

**T6. Preferred (двунаправленно)**: hint переливается в пустой слот в обе стороны.

*Замечание о посылке эквивалентности T6*: полный TIC распространяет Preferred ОДНИМ
глобальным broadcast'ом (`PropagatePreferred`, первый найденный hint), SPS — локально
по рёбрам. Стратегии совпадают, только пока в выражении фактически один hint
(или все совпадают) — что для примитивных выражений сегодняшних источников hint'ов
верно, но эродирует с ростом числа источников (`Specs/Tic/TicTechnicalDebt.md` #7,
единый глобальный Preferred). При расхождении стратегий первым сломается §8-инвариант
на выражениях со смешанными hint'ами — паритетные пины §8.3 обязаны это поймать.

### 5.2 Инвариант цикла

- `Desc` монотонно не убывает (заменяется только LCA-ом, который ≥ обоих входов);
- `Anc` монотонно не возрастает (только GCD);
- `Comparable` — false → true, обратно никогда;
- `Pref` — write-once (OPEN → значение).

Интервалы только сужаются.

### 5.3 Терминация

**Потенциал**: Φ = Σ_g (rank(Anc_g) − rank(Desc_g)), где rank — высота элемента в L,
rank(OPEN_anc) = **22** (максимум — |L|−1), rank(OPEN_desc) = 0.

- Каждая продуктивная итерация (`changed = true`) уменьшает Φ минимум на 1
  (либо выставляет `_failed`, что тоже терминально).
- Φ ≥ 0; Φ_initial ≤ |G| · 22.
- Высота решётки h = **11** (длиннейшая цепь
  `U4→I8→I12→I16→I24→I32→I48→I64→I96→F32→Real→Any`).
- Число итераций ограничено монотонностью; инженерного cap'а НЕТ (`do … while (changed)`)
  — терминация чисто структурная. На практике 1-3 итерации.

### 5.4 Модель отказа

Fail-fast: `_failed` выставляется в `MergeInto`, `SetConcrete`, `SetConcreteChecked`,
`ApplyGenericConstraint`, `NarrowDesc`, `NarrowAnc` — и на GCD-null, и на нарушение
`Fits` после трансфера. `Propagate` проверяет флаг после каждого правила и прерывается.
`SolveOrNull` после Propagate: `_failed ⇒ null` (fallback в полный TIC, который
воспроизведёт ошибку со штатным сообщением). Прежняя модель «отложенного отказа»
(несовместимость терпится до Resolve) больше не действует.

---

## 6. Фаза Resolve

После неподвижной точки каждая группа резолвится в один конкретный тип.

### 6.1 Процедура решения

`ResolveGroup(g)` = `ResolveRoot(Find(g))` + comparable-проверка (ниже).

`ResolveRoot`: d = Desc, a = Anc, p = Pref. Первое подходящее правило:

| # | Условие | Результат | Обоснование |
|---|---------|-----------|-------------|
| 1 | d,a ≠ OPEN, d = a | d | Интервал-точка. |
| 2 | d,a ≠ OPEN, ¬Fits(d,a) | **null** | Невыполнимый интервал (страховка — fail-fast §5.4 обычно ловит раньше). |
| 3 | d,a ≠ OPEN, p ≠ OPEN, Fits(d,p) ∧ Fits(p,a) | p | Hint внутри интервала. |
| 4 | d,a ≠ OPEN | `RefineAncestor(a, d)` | Ковариантная резолюция к ancestor'у с конкретизацией абстрактного. |
| 5 | a ≠ OPEN, p ≠ OPEN, Fits(p,a) | p | Нет desc; hint под ancestor'ом. |
| 6 | a ≠ OPEN | `RefineAncestor(a, OPEN)` | Нет desc и подходящего hint'а. |
| 7 | p ≠ OPEN, (d = OPEN ∨ Fits(d,p)) | p | Нет anc; hint сам по себе (в т.ч. ЧИСТЫЙ hint без границ — p-return при d = OPEN). |
| 8 | d ≠ OPEN | d | Нет anc и hint'а. |
| 9 | Comparable | Real | Дефолтный comparable-тип. |
| 10 | иначе | Any | Полностью неограничена. |

**RefineAncestor(a, d)** — конкретизация абстрактного ancestor'а, desc-обусловленная:
`I96` (единственный абстрактный, реально доходящий сюда — из
`GenericConstrains.Integers`, constraint `&`/`|`/`^`/`//`/`~`) резолвится в **I32,
если `Fits(d, I32)`, иначе I64**. Прочие абстрактные (I48, I24, I12, U48, U24, U12)
ни один встроенный оператор не объявляет как Anc — до Resolve они не доживают; при
появлении `Integers3264`/`Integers32` у встроенных операторов метод расширить.
Соответствует политике 2 конкретизации абстрактных на выходе полного TIC —
см. `TicAlgorithm_Destruction.md` §6d.

**Comparable-паритет** (`ResolveGroup`): если группа помечена Comparable, а
резолвленный тип не comparable (`s_comparableTable[Order]`), возвращается **null** —
SPS воздерживается, и полный TIC сообщает ошибку семейства FU783 (вместо прежнего
молчаливого `max(true,false) → Bool`, ловившегося только библиотечным backstop'ом
FU777). Пин: `CompareOperatorsTest.MinMax_NonComparable_SameErrorOnBothSolverPaths`.

### 6.2 Ковариантный уклон

При обеих границах и неодноточечном интервале выбирается **ancestor** (широчайший
тип), не descendant — семантика `SolveCovariant` полного TIC: выходные переменные
резолвятся в самый широкий тип, приемлемый для потребителя. Исключение — Preferred
(правило 3).

### 6.3 Конструирование результата (выходной контракт)

Два независимых выхода; отказ ЛЮБОГО из них — воздержание целиком.

**`BuildResults` → `TypeInferenceResults`**:
- по `_nodeGroup` каждый AST-узел резолвится и мапится на flyweight-синглтон
  `s_syntaxNodeSingletons[Order]` (статические `TicNode`; ноль аллокаций на вызов);
- по `_callGenericGroup` каждая generic-группа вызова регистрируется через
  `RememberGenericCallArguments` как одноэлементный массив `s_singleRefToArrays[Order]`
  (`StateRefTo` на flyweight type-variable);
- результаты оборачиваются в **`SpsTicResults`** — облегчённый `ITicResults` БЕЗ
  словаря именованных нод: `GetVariableNodeOrNull` возвращает **null**,
  `GetVariableNode` **бросает** (не должен вызываться при `typesApplied = true`),
  `GetSyntaxNodeOrNull` читает массив синглтонов, generics пусты.

**`ApplyTypesToSyntaxTree` / `ApplyTypesRecursive`** — SPS проставляет типы В ДЕРЕВО
сам (в полном TIC это делает `ApplyTiResultEnterVisitor` через namedNodes):
`OutputType` каждого узла и `VariableType` каждого `NamedIdSyntaxNode` пишутся
напрямую из резолвленных групп (`s_ordToFunnyType[Order]`). Успех обоих шагов
выставляет out-флаг **`typesApplied = true`** — сигнал дальнейшему пайплайну, что
визитор применения типов не нужен. Любая нерезолвленная группа ⇒ `null` и
`typesApplied = false`.

---

## 7. Лемма Unite (ссылки на переменные)

### 7.1 Формулировка

**Лемма.** Для примитивных выражений без шэдоуинга переменных `Unite(var, ref)` даёт
то же назначение типов, что TIC-путь `SetVarRef` (двунаправленные рёбра + унификация
в Destruction).

### 7.2 Набросок доказательства

1. В TIC чтение переменной создаёт RefTo-ребро; после Pull и Push установлено
   `x_var ≤ x_ref` и `x_ref ≤ x_var`.
2. Для примитивов (без вариантности) двунаправленное ≤ — это равенство.
3. `Unite` устанавливает равенство (одна группа) немедленно — единственная неподвижная
   точка двунаправленного constraint'а.
4. Следовательно назначения типов совпадают. ∎

### 7.3 Предусловия

- **Нет composite-типов** — там вариантность: `Array(A) ≤ Array(B)` требует `A ≤ B`,
  не `A = B`.
- **Нет шэдоуинга** — имя ↦ ровно одна группа.
- **Нет мутаций** — все использования суть чтения (определения идут через
  `AddEdge`-путь SetDef, не через Unite).

---

## 8. Инвариант безопасности

### 8.1 Формулировка

Для всех выражений e, принятых gate'ом:

```
SPS(e) ≠ null  ⇒  ∀ узла n из e: SPS(e).type(n) = TIC(e).type(n)
```

Когда SPS возвращает результат, он совпадает с полным TIC на каждом узле. Направление
«воздержаться» свободно: SPS(e) = null всегда допустимо.

Comparable-ось этого инварианта обеспечена резолюционным предикатом §6.1
(comparable-группа, резолвящаяся в не-comparable тип ⇒ null): обе трассы —
SPS-eligible и форсированный полный TIC — дают одну и ту же ошибку FU783.

### 8.2 Обязательства доказательства

1. **Полнота gate'а** — ни один composite не попадает в систему (§3.3).
2. **Эквивалентность Build** — constraint'ы SPS совпадают с ограничением
   `TicSetupVisitor` на L: `Unite(var,ref)` = `SetVarRef` (лемма §7);
   `AddEdge(expr,var)` = ребро `SetDef` (включая Preferred-seeding);
   `SetConcrete` = `SetConst`; `SetInterval` = `SetGenericConst` с теми же бинами;
   `ApplyGenericConstraint + Unite` = `SetCall(PureGeneric)` для 1-constraint
   generic'ов; конкретные функции = `SetCall(ConcreteFunction)`.
3. **Fixed point Propagate = Pull+Push+Destruction на L** — T1 = Push desc,
   T2 = Pull anc, T3 = Destruction конкретного ancestor'а в constraint-descendant,
   T4 = прямое распространение ancestor'а (валидно только для примитивов);
   T6-посылка — см. оговорку §5.1.
4. **Resolve = композиция резолюции полного TIC на L** — процедура §6 обязана
   совпадать не с голым `SolveCovariant`, а с композицией
   **`SolveCovariant` ∘ `TicTypesConverter`**: полный TIC сперва резолвит CS
   (Preferred → ancestor → …), а затем конвертер конкретизирует абстрактные типы на
   выходе. `RefineAncestor` — это второй член композиции, втянутый в Resolve
   (`I96 → I32|I64`); правило 7 (p-return) — зеркало ветки
   `ConstraintsState when Preferred != null` конвертера.

### 8.3 Эмпирическая верификация

Отдельного массива differential-тестов НЕТ (прежнее упоминание «37 differential
tests» не соответствовало ни одному существующему файлу). Паритет фактически
пинуется двумя механизмами:

1. **Весь syntax-suite** (`NFun.SyntaxTests`): SPS включён прозрачно на call-site
   (§3.0), поэтому каждый примитивный тест прогоняет SPS-путь, а каждый тест с
   composite-типами — полный TIC; расхождение типов или пропущенная ошибка ломает
   существующие ассерты.
2. **Явные паритетные пины**:
   `CompareOperatorsTest.MinMax_NonComparable_SameErrorOnBothSolverPaths` — пары
   выражений одинаковой семантики, где первая форма SPS-eligible, а вторая (с
   composite-привязкой `z = [1]`) форсирует полный TIC; ассертится идентичный код
   ошибки на обеих трассах.

---

## 9. Особенности comparison chain

Цепочки сравнений (`a < b == c >= d`) — сахар над попарными сравнениями
(`SetupCompareChain`).

### 9.1 Классификация операторов

- **Равенство (==, !=)**: `GenericConstrains.Any` — без границ и comparable.
- **Порядок (<, >, <=, >=)**: `GenericConstrains.Comparable` — comparable = true.

### 9.2 Генерация constraint'ов

Для операторов [op₀..op_{k-1}] и операндов [o₀..o_k]: на каждой позиции i comparable
(если реляционный op) ставится на группу операнда i, затем
`Unite(group(oᵢ), group(o_{i+1}))`; в конце `SetConcrete(resultGroup, Bool)`.
Generic-группой служит группа первого операнда пары — без аллокаций. Через
транзитивность union-find все операнды цепочки оказываются в одной группе;
comparable-флаг от любого реляционного оператора накрывает всех.

### 9.3 Результат

Тип цепочки — всегда Bool (конкретно); тип операндов — интервальная резолюция
объединённой группы.

---

## 10. Спецслучай Pow

### 10.1 Предикат IsPowForcedReal

`IsPowForcedReal(exp)` = true ⟺ показатель НЕ является `GenericIntSyntaxNode` с
неотрицательным целым значением (т.е. не литерал, или отрицательный литерал).

### 10.2 Forced Real

При `IsPowForcedReal`: `SetInterval(resultGroup, Real, Real, OPEN)` +
`Unite` всех операндов с result-группой; generic-группа записывается. Действует и для
операторной формы (`x ** y`), и для функциональной (`pow(x, y)` —
`call.Id == CoreFunNames.Pow`).

### 10.3 Обычный путь

Неотрицательный целый показатель — целосохраняющая степень (повторное умножение):
стандартный PureGeneric-путь с constraint'ом функции (обычно Arithmetical
`[U24..Real]`).

---

## 11. Модель производительности

### 11.1 Сложность

- **Build**: O(N) — один проход, union-find ~O(α(N)).
- **Propagate**: O(E · k), k ограничено высотой решётки (h = 11), практически 1-3.
- **Resolve**: O(G).
- **Итого**: O(N + E·k + G) ≈ O(N) для типичных выражений.

### 11.2 Память

- Массив групп: G × 12 байт (struct `Group`: 3 байта ординалов + int Parent +
  Rank + Comparable, с паддингом).
- `_nodeGroup`, `_callGenericGroup`: N × 4 байта каждый (int, −1 = пусто).
- Словарь переменных: O(V).
- Рёбра: E × 8 байт, стартовая ёмкость 16, удвоение.

### 11.3 Кэш

- Таблицы: LCA 529 Б + GCD 529 Б + Fits 529 Б + comparable 23 Б ≈ 1.6 КБ — целиком
  в L1.
- Группа 12 байт ⇒ ~5 групп на cache line; типичное выражение — 1-4 линии.
- Flyweight-синглтоны выхода: 23 syntax-ноды + 23 type-variable RefTo-массива +
  23 `FunnyType` (`s_ordToFunnyType`) — статическая инициализация, ноль аллокаций
  на вызов при конструировании результата.
