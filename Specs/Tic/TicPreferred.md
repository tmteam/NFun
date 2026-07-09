# TIC Preferred — Формальная спецификация

## 1. Что такое Preferred

**Preferred** — hint для резолюции типов, **не** constraint. Это metadata на `ConstraintsState`, несущая информацию о **происхождении** (provenance) значения.

**Ключевое свойство**: Preferred не изменяет множество допустимых решений — только выбирает, КАКОЕ решение из интервала `[Desc..Anc]` будет выбрано при финализации.

Один и тот же constraint-интервал может иметь разный Preferred в зависимости от происхождения:
- Целочисленный литерал `42` → `[U4..Re, P=I32]` (integer provenance)
- Вещественный литерал (в TIC-тесте) → `[U8..Re, P=Re]` (real provenance)
- Hex-литерал `0xFF` → `[U8..I96, P=I32]` (hex provenance, ancestor I96 вместо Re)

Preferred — это `StatePrimitive` (nullable). Большинство CS-нод не имеют Preferred (null).

**Код**: `ConstraintsState.Preferred` — публичное свойство типа `StatePrimitive` с публичным setter'ом.

---

## 2. Источники Preferred

Preferred создаётся на ТРЁХ стадиях жизни графа: при построении (Build), во время
broadcast'а (`PropagatePreferred` — override-правило §4.3) и во время Destruction
(синтез в `TransformToOptionalOrNull`). Утверждение «Preferred создаётся только при
Build» неверно для текущего кода.

### 2.1. Целочисленные литералы (Build)

`TicSetupVisitor.Visit(GenericIntSyntaxNode)` создаёт generic constraint через
`GraphBuilder.SetGenericConst` с Preferred из диалекта.

`TicSetupVisitor.GetPreferredIntConstantType()` возвращает:
- `IntegerPreferredType.I32` → `StatePrimitive.I32` (default)
- `IntegerPreferredType.I64` → `StatePrimitive.I64`
- `IntegerPreferredType.Real` → `StatePrimitive.Real`

**Бины десятичных литералов** (descendant по величине значения; ancestor всегда `Real`):

| Значение | Desc | Preferred |
|---|---|---|
| `0..127` | `U4` (низ решётки — общее подмножество I8 ∩ U8) | диалект |
| `128..255` | `U8` | диалект |
| `..32767` | `U12` | диалект |
| `..65535` | `U16` | диалект |
| `..int.MaxValue` | `U24` | диалект |
| `..uint.MaxValue` | `U32` | **I64** (промоушен, MR4Bug1) |
| `..long.MaxValue` | `U48` | **I64** |
| `> long.MaxValue` | конкретный `U64` (не generic) | — |
| `-128..-1` | `I8` | диалект |
| `..short.MinValue` | `I16` | диалект |
| `..int.MinValue` | `I32` | диалект |
| `< int.MinValue` | `I64` | **I64** |

Промоушен Preferred к `I64` для значений вне диапазона Int32 (в обе стороны) нужен,
чтобы неограниченная резолюция выбирала Int64, а не проваливалась в Ancestor (Real):
`out = 4294967295` должен быть `Int64`, не `Real` (MR4Bug1). Ancestor остаётся `Real`,
чтобы generic-контексты по-прежнему принимали Real-операнды.

Примеры: `42 → [U4..Re, P=I32]`; `-5 → [I8..Re, P=I32]`; `100000 → [U24..Re, P=I32]`.

### 2.2. Hex/bin литералы (Build)

Тот же `Visit(GenericIntSyntaxNode)` при `node.IsHexOrBin`. Отличие: ancestor —
`I96` для положительных (hex/bin — битовые литералы, не расширяются до вещественных)
и `I64` для отрицательных. Бины по desc те же (`U4..U48`), Preferred: `I32` до
`int.MaxValue`, `I64` выше; значения > `long.MaxValue` — конкретный `U64`.
Отрицательные ниже `int.MinValue` — конкретный `I64`.

### 2.3. GraphBuilder.SetDef — seeding от конкретного выражения (Build)

`GraphBuilder.SetDef(name, rightNodeId)`: если выражение уже решено в конкретный
примитив, а нода определения ещё CS — примитив записывается как Preferred:

```csharp
if (exprNode.State is StatePrimitive primitive && defNode.State is ConstraintsState constrains)
    constrains.Preferred = primitive;
```

Так `x = 1.0; y = x + 1` переносит Real-происхождение на `y` даже там, где сам
литерал не CS.

### 2.4. Прочие Build-источники

- **`TicNode.TrySetAncestor`** (путь `SetCallArgument(StatePrimitive)`): при
  добавлении примитивного ancestor'а от конкретной сигнатуры вызова CS-нода получает
  `Preferred = anc` — параметр функции и есть намеренный тип аргумента.
- **`TicSetupVisitor.PropagateReturnOnlyPreferred`**: у user-функций generic `T`
  получает Preferred из `GenericConstrains.Preferred` (снятого с body-solve через
  `GenericConstrains.FromTicConstrains`), но ТОЛЬКО когда `T` встречается лишь в
  return-позиции либо когда фактический аргумент — литерал `none` (GH #126:
  `s(none)` возвращал Real вместо Int). Обычные call-site generic'и Preferred из
  сигнатуры НЕ получают (комментарий в `TicSetupVisitor.InitializeGenericType`) —
  hint приходит естественно через `TryConvertConstToRef` (§3.2).
- **Тестовый API**: `GraphBuilderExtensions.SetIntConst` — `[desc..Re, P=Re]`;
  `GraphBuilderExtensions.SetGenericConst` — общая точка входа `(desc, anc, preferred)`.

### 2.5. Destruction-время: синтез в TransformToOptionalOrNull

`SolvingFunctions.TransformToOptionalOrNull` (вызывается из Pull и Destruction при
материализации `CS{IsOptional}` → `opt(inner)`) не только переносит существующий
hint, но и **синтезирует** новый из конкретного примитивного Descendant:

```csharp
innerCs.Preferred = descendant.Preferred
    ?? (descendant.HasDescendant && descendant.Descendant is StatePrimitive dp ? dp : null);
```

Это зеркалит поведение `SetDef` (Preferred от литерала) для optional-пути. Заметим:
синтезированный hint reference-равен Descendant'у — ровно тот класс «auto-init»
hint'ов, который broadcast имеет право перезаписать (§4.3).

### 2.6. Override при broadcast'е

`SolvingFunctions.ApplyPreferred` может ПЕРЕЗАПИСАТЬ существующий Preferred (§4.3) —
т.е. PropagatePreferred тоже мутирует ось P, а не только заполняет пустоты.

---

## 3. Правила распространения (Propagation)

Preferred распространяется четырьмя механизмами, каждый на своей фазе.

### 3.1. Pull CS←CS: двунаправленное копирование

**Код**: `PullConstraintsFunctions.Apply(ConstraintsState ancestor, ConstraintsState descendant)`.

При Pull через ребро desc → anc (обе стороны CS) Preferred копируется **в обе стороны**
(только в пустой слот):

```
if (ancestorCopy.Preferred == null && descendant.Preferred != null)
    ancestorCopy.Preferred = descendant.Preferred;   // upward: desc → anc
if (descendant.Preferred == null && ancestor.Preferred != null)
    descendant.Preferred = ancestor.Preferred;       // downward: anc → desc
```

**Upward** (desc→anc): целочисленная константа в `[1,2,3]` передаёт `P=I32` элементу
массива, а оттуда — array-ноде.

**Downward** (anc→desc): struct field chain `s.m.n` — Preferred от литерала передаётся
обратно через цепочку полей.

### 3.2. TryConvertConstToRef: копирование при вызове функции

**Код**: `GraphBuilder.TryConvertConstToRef` (медленный путь
`GraphBuilder.SetCallArgument(StateRefTo, argId)`).

Когда constraint-нода аргумента (свежая, без предков) полностью поглощается диапазоном
generic'а и конвертируется в прямую ссылку, её Preferred переносится в generic:

```csharp
if (argCs.Preferred != null && genCs.Preferred == null)
    genCs.Preferred = argCs.Preferred;
```

Типичный случай: `x * 2` — константа `2` (свежая CS `[U4..Re, P=I32]`) как аргумент
арифметического generic'а `[U24..Re]`: диапазон generic'а поглощается диапазоном
константы, нода конвертируется в RefTo, hint переезжает на `T` умножения.
Предусловия конверсии: у обеих CS заданы примитивные Descendant и Ancestor, и
comparable-флаг аргумента не строже generic'ового.

### 3.3. IntersectIntervalsOrNull (⊓): коммутативный hint-LCA

**Код**: `ConstraintsState.IntersectIntervalsOrNull` → `StateExtensions.PreferredHintLcaOrNull`.

С закрытием долга #14 обе стороны ⊓ обрабатываются ЕДИНЫМ коммутативным правилом
(история «receiver-wins / argument-wins» из кода ушла):

```csharp
result.Preferred = PreferredHintLcaOrNull(Preferred, other.Preferred);
if (result.Preferred != null && !result.CanBeConvertedTo(result.Preferred))
    result.Preferred = null;
```

Правила `PreferredHintLcaOrNull(P₁, P₂)`:
1. Оба null → null.
2. Только один задан → берётся он.
3. Оба равны → общий.
4. Различаются → `LCA(P₁, P₂)` на примитивной решётке: более широкий hint сохраняет
   намерение резолюции (hint-LCA `I32` и `Real` = `Real` — int поднимается без потерь,
   а real-литерал в одной из веток означает намерение Real).
5. Если `LCA = Any` — hint'ы из несвязанных семейств, информация нулевая → drop (null).

Пост-условие (только у ⊓): hint обязан вписываться в объединённый интервал
(`CanBeConvertedTo`), иначе сбрасывается. Поскольку `Unify(CS,CS) ≝ MergeOrNull`
(Unify(CS,CS) ≡ Merge), это же правило действует и в Unify.

### 3.4. LCA (∨): тот же коммутативный hint-LCA

**Код**: `StateExtensions.Lca`, ветка CS×CS — `PreferredHintLcaOrNull(ac.Preferred, bc.Preferred)`.

С волны #14 у ∨ и ⊓ ОДНА транспортная функция для P (различие только в
пост-условии fit-drop, которое есть лишь у ⊓ — у чистого join'а без Ancestor'а
дропать не по чему). Кроме ячейки CS×CS, hint выживает и в смешанных ветках join'а:

- `LCA(T, CS{opt})` — результат-CS наследует `bc2.Preferred` («hints survive the join»);
- `opt(P) ∨ CS[D..A](Pref) = CS[P..A]?(Pref)` — join остаётся нерешённым интервалом
  с сохранённым hint'ом; прежний eager `StateOptional.Of(Preferred)` запекал hint в
  конкретный примитив (нарушение P3) и блокировал Push-сужение (FU719, Bug#6;
  долг #11 — закрыт).

---

## 4. PropagatePreferred pass — глобальный broadcast

### 4.1. Когда выполняется

**Между Pull и Push** (`GraphBuilder.SolveCore`):

```
Build → Toposort(+fused Pull | two-phase Pull) → PropagatePreferred → Push
      → [ScCClosurePass] → Destruction → Finalize
```

Почему именно до Push (MR2Bug4): `PushConstraintsFunctions.Apply(StatePrimitive, ConstraintsState)`
схлопывает CS литерала `[U8..Re]I32!` в голый примитив, когда ancestor пиновал его в
одну точку (`y:byte = 5` → литерал форсируется в `U8`). Если бы broadcast шёл ПОСЛЕ
Push, `CollectPreferred` не нашёл бы ни одного hint'а — и `byte+byte` с отрицательными
литералами резолвился бы в Real вместо I32. Запуск между Pull и Push захватывает hint,
пока он ещё живёт на CS. Это корректно по P3: Preferred — metadata, на Pull/Push не
влияет, поэтому перестановка фазы безопасна.

### 4.2. Зачем нужен

Constraint-алгебра **теряет provenance** на границах composite-типов. Пример:

```
s = { m = 42 }     # 42 → [U4..Re, P=I32]
x = s.m            # struct field access создаёт CS-посредник для 'm'
y = x + 1          # '+' создаёт generic T c constraint [U24..Re], но P=null
```

Узел `y` получает интервал через Pull/Push, но Preferred теряется при структурной
декомпозиции (snapshot struct → element node). PropagatePreferred восстанавливает
`P=I32` на `y`.

### 4.3. Алгоритм

**Код**: `SolvingFunctions.PropagatePreferred` → `CollectPreferred` + `ApplyPreferred`.
Оба прохода идут по toposorted-нодам с рекурсией в composite-члены (dedup через
`VisitMark`/`NextMark`, деref через `GetNonReference`).

**Pass 1 — Collect** (`CollectPreferred`): первый найденный непустой `cs.Preferred`
становится `commonPreferred` (`preferred ??= cs.Preferred`). Если hint'ов нет — pass
завершается.

**Pass 2 — Apply** (`ApplyPreferred`): для каждой CS-ноды, удовлетворяющей guard'ам:

1. `cs.HasDescendant` — есть нижняя граница (иначе интервал слишком абстрактен);
2. `cs.Descendant is StatePrimitive` — для composite-descendant'ов Preferred не имеет смысла;
3. `cs.CanBeConvertedTo(preferred)` — hint вписывается в интервал —

выполняется:

```csharp
if (cs.Preferred == null)
    cs.Preferred = preferred;                     // заполнить пустой слот
else if (!cs.Preferred.Equals(preferred)
      && ReferenceEquals(cs.Preferred, descPrim))
    cs.Preferred = preferred;                     // OVERRIDE auto-init hint'а
```

**Override-правило**: существующий Preferred ПЕРЕЗАПИСЫВАЕТСЯ broadcast-значением,
если он reference-равен Descendant'у. Reference-равенство здесь — маркер
происхождения: hint, который является тем же объектом, что и нижняя граница, был
проинициализирован «от бедности» (auto-init от границы constraint'а или синтез §2.5),
а не от литерала — он не несёт информации сверх самого интервала, и литеральный
`commonPreferred` должен доминировать. (Примитивы решётки — статические синглтоны
`StatePrimitive.I32` и т.п., поэтому reference-равенство совпадает с
«hint = Descendant по значению» для всех нод, созданных штатными путями.)

### 4.4. Единый глобальный Preferred

Текущая реализация собирает **один** `commonPreferred` на весь граф. Это корректно,
пока в графе фактически один hint (или все совпадают); при смеси hint'ов результат
зависит от порядка toposort — долг #7, наблюдаемая поверхность — Bug Z (см. §8.1).

---

## 5. Резолюция — как Preferred влияет на выходной тип

### 5.1. SolveCovariant (ковариантная резолюция)

**Код**: `ConstraintsState.SolveCovariant(bool ignorePreferred = false)`.

Порядок ветвей:

1. **F-bound арм**: если единственный constraint — `StructBound` (нет Ancestor,
   Descendant, Preferred, IsComparable) — материализуется сам bound (iso-recursive
   `fold`), с opt-обёрткой при `IsOptional`. Preferred в этот арм не попадает по
   построению (условие требует `Preferred == null`).
2. **Preferred** — если `!ignorePreferred`, hint задан и `CanBeConvertedTo(Preferred)`
   — выбирается hint.
3. Иначе `ancestor = Ancestor ?? Any`, затем:
   - `IsComparable` и ancestor несравним → нода остаётся нерешённой (возврат `this`);
   - `Descendant is ICompositeState` → Descendant;
   - открытый интервал `[abstract..null]` → `StatePrimitive.ConcreteAncestor`
     абстрактного descendant'а (абстрактные типы — TIC-internal, наружу не выходят);
   - иначе ancestor.
4. **Постобработка** `WrapOptional`: при `IsOptional` результат оборачивается в
   `StateOptional.Of(inner)`; `opt(Any)` схлопывается в `Any`.

Параметр `ignorePreferred = true` пропускает шаг 2 — используется для function
signatures (`f(x) = x + 1` должна остаться generic, а не резолвиться в I32);
прокидывается из `GraphBuilder.Solve(ignorePrefered)` через `SolvingFunctions.Finalize`
→ `SolveUselessGenerics`.

### 5.2. SolveContravariant (контравариантная резолюция)

**Код**: `ConstraintsState.SolveContravariant()`.

1. **F-bound арм** — тот же, что в SolveCovariant.
2. **Preferred** — первый приоритет, безусловно (параметра ignorePreferred нет).
3. Нет Descendant → нерешённая (возврат `this`).
4. `IsComparable` и Descendant несравним (не comparable-примитив и не `arr(Char)`)
   → нерешённая.
5. Иначе Descendant.
6. Постобработка `WrapOptional` — как в §5.1.

### 5.3. TicTypesConverter: конвертация CS в FunnyType

**Код**: `TicTypesConverter`, ветка `case ConstraintsState constrains when constrains.Preferred != null`.

Нерезолвленный CS с Preferred конвертируется напрямую в `ToConcrete(Preferred.Name)`;
если `Descendant is StateOptional` — в `FunnyType.OptionalOf(...)`. Это fallback для
CS, не прошедших через SolveCovariant/SolveContravariant (промежуточные ноды).

### 5.4. ConcretestSnapshot: снапшот-оператор слоя резолюции (↓-часть долга #19 — ЗАКРЫТА)

**Статус**: разделение выполнено — ↓ (`StateExtensions.Concretest`) теперь чистая
проекция (Preferred только транспортируется во флаг-форме, никогда не выбирается);
две резолюционные ветки вынесены в **`ConcretestSnapshot`** (↓ₛ,
`StateExtensions.ConcretestSnapshot.cs`) — оператор ЭТОГО слоя (семейство Solve),
вызываемый там, где результат материализуется в хранимое состояние графа
(`ConstraintsState.AddDescendant`, односторонние desc-проекции LCA):

1. **`SnapshotArrayElement`**: если элемент массива — CS с Preferred, примитивным
   Descendant ≠ Preferred и `desc ≤ Preferred`, вместо коллапса в голый примитив
   возвращается НОВЫЙ CS `[desc..∅]` с тем же Preferred (без ancestor'а). Desc-снапшоты
   массивов проносят hint через Destruction.
2. **`SnapshotConstraints`, Optional-ветка**: для `CS{IsOptional}` при
   `inner ≤ Preferred` выбирается Preferred вместо inner (↓ₛ даёт `opt(Preferred)`,
   чистый ↓ — `opt(↓D)`). Rule B действует в обоих операторах: лифт нерешённой
   границы остаётся во флаг-форме `[D..A]?` с перенесённым Preferred.

Контракт ↓ₛ: на Preferred-свободных состояниях ↓ₛ ≡ ↓; идемпотентен; interval-часть
результата Fit-удовлетворяет исходному CS. Пины: `ConcretestSnapshotTest`.

Оставшаяся родственная примесь в ⊓ (остаток #19): `MergeOrNull` НЕ схлопывает решённый
Optional-descendant `[opt(D)..∅] → opt(D)`, если задан Preferred («Preferred-survival
exception») — hint должен дожить до резолюции, коллапс бы его уничтожил.

---

## 6. Формальные инварианты

### P1 — Safety (безопасность)

```
Preferred ∈ [Desc..Anc]  ∨  Preferred = null
```

Preferred всегда лежит внутри допустимого интервала, либо отсутствует. Гарантируется:
- При создании: `SetGenericConst` задаёт Preferred, совместимый с `[desc..anc]`
  (бины §2.1-2.2 по построению).
- При пересечении: пост-условие в `IntersectIntervalsOrNull` — hint-LCA, не влезающий
  в объединённый интервал, сбрасывается (`CanBeConvertedTo`).
- При broadcast'е: guard `cs.CanBeConvertedTo(preferred)` в `ApplyPreferred`.
- При резолюции: `SolveCovariant`/`SolveContravariant` проверяют `CanBeConvertedTo`
  перед выбором hint'а — даже «протёкший» неподходящий hint не станет типом.

### P2 — Idempotence (идемпотентность)

```
PropagatePreferred(PropagatePreferred(G)) = PropagatePreferred(G)
```

Аргумент — с учётом override-правила §4.3:

- **Collect**: pass не стирает hint'ы, только добавляет/заменяет; первым в
  toposort-порядке остаётся тот же `commonPreferred` (первый источник не перезаписывался
  — override меняет значение на `commonPreferred`, что не меняет результат Collect).
- **Apply**, по классам нод после первого прогона:
  1. `Preferred == null` → был заполнен `commonPreferred` в первом прогоне; во втором
     ветка не срабатывает.
  2. `Preferred = commonPreferred` (заполненные и перезаписанные) — обе ветки либо
     не срабатывают (`Equals`-guard), либо записали бы то же значение.
  3. Собственный hint, НЕ reference-равный Descendant'у — не трогается.
  4. Hint, reference-равный Descendant'у: перезаписан в первом прогоне на
     `commonPreferred`. После перезаписи он либо не reference-равен Descendant'у
     (override больше не применим), либо `commonPreferred` и есть объект-Descendant —
     тогда повторная запись того же объекта. Фикс-поинт достигается за один прогон.

### P3 — Monotonicity / ортогональность

```
∀ node: Sat(node)(before) = Sat(node)(after)
```

Установка Preferred **не сужает** интервал `[Desc..Anc]` и не меняет исходы Pull/Push:
FitsInto, LCA, GCD на ось P не смотрят. Именно P3 делает безопасным перенос
PropagatePreferred между Pull и Push (§4.1).

**Известные отклонения** (не от Sat, а от формы представления) — остаток долга #19:
- ~~`ConcretestArrayElement` и Opt-выбор в `ConcretestConstraints`~~ — закрыто:
  ↓ чист; hint-зависимость живёт в ↓ₛ (`ConcretestSnapshot`, §5.4) — оператор слоя
  резолюции, для которого зависимость от P — контракт, а не отклонение.
- `MergeOrNull`: Preferred подавляет коллапс решённого Optional-descendant'а —
  каноническая форма результата ⊓ зависит от hint'а.

Отклонение Sat-нейтрально (множество допустимых типов не меняется), но нарушает
ортогональность оси P к оператору ⊓. Статус: остаток #19 (вынос Sat/форм-меняющего
коллапса из ⊓ в стадийную канонизацию).

### P4 — Determinism (детерминизм)

При фиксированном topological order PropagatePreferred детерминирован: первый
найденный Preferred в Collect-порядке становится `commonPreferred`. Оговорка: при
СМЕШАННЫХ hint'ах (а они возможны — §8.1) результат детерминирован, но зависит от
порядка toposort, т.е. от несемантических деталей выражения (долг #7).

---

## 7. Почему Preferred нельзя убрать

**Эксперимент**: заменить Preferred на resolution-time defaulting (выбирать I32 для
всех числовых CS при финализации).

**Провал**: интервал `[U24..Re]` возникает в двух различных контекстах:

1. `[1,2,3].sum()` — должен резолвиться в `I32` (integer provenance)
2. `3.14 * 2` — `2` должен резолвиться в `Real` (real context)

Без Preferred оба случая неразличимы: одинаковый интервал, но правильные ответы разные.
Preferred несёт provenance, который constraint-алгебра теряет при структурной
декомпозиции.

**Формально**: Preferred — это морфизм из пространства «литерал + контекст» в
пространство «допустимый тип». Constraint-алгебра (LCA, GCD) оперирует множествами
типов, теряя информацию об источнике. Preferred восстанавливает эту информацию.

---

## 8. Ограничения и будущая работа

### 8.1. Единый глобальный Preferred

PropagatePreferred собирает **одно** значение на весь граф. Смешанные hint'ы — не
гипотетика: они возникают уже сейчас (литерал `> int.MaxValue` даёт `P=I64` рядом с
дефолтным `P=I32`; `SetDef` сеет hint от конкретного выражения; `TrySetAncestor` —
от сигнатуры). Какой из них выиграет — определяется порядком toposort.

**Наблюдаемая поверхность — Bug Z** (bug-hunt round 4, долг #7):
`fn(arr:int[]?):int = arr?.reverse().sum() ?? 0; fn([10,20,30])` падал в runtime с
`Unable to cast Int32 to UInt32` — внутри тела fn смесь hint'ов (`0` с P=I32 против
auto-init от Arithmetical-descendant'а U24), и toposort-порядок для user-fn body
выбирал U24. Override-правило §4.3 закрывает конкретно класс auto-init hint'ов, но
не общую проблему конкурирующих ЛИТЕРАЛЬНЫХ hint'ов. Полный fix — edge-local
propagation (§8.2). Реестр: `Specs/Tic/TicTechnicalDebt.md` #7.

### 8.2. Edge-local propagation

Альтернатива глобальному broadcast'у — распространение Preferred по рёбрам графа (как
Pull/Push): каждая нода получает hint от прямых связей, а не от глобального значения.
Решает §8.1 (каждый литерал пушит свой hint только в connected-компоненту влияния).

**Не реализовано**: пока конфликтующие hint'ы редки, а override-правило гасит самый
частый класс (auto-init), edge-local избыточен. При добавлении новых источников
Preferred (typeclasses, user annotations) потребуется пересмотр.

### 8.3. Preferred для composite-типов

Текущий Preferred — всегда `StatePrimitive`. Нет механизма «preferred array type» или
«preferred struct type»; guard `cs.Descendant is StatePrimitive` в `ApplyPreferred`
пропускает composite-descendant'ы.

Для текущей системы типов этого достаточно: composite-типы не имеют ambiguity при
резолюции (массив — это массив, struct — это struct). Preferred нужен только числовым
примитивам, где интервал `[U4..Re]` содержит десятки допустимых типов.
