# TIC Preferred — Формальная спецификация

## 1. Что такое Preferred

**Preferred** — hint для резолюции типов, **не** constraint. Это metadata на `ConstraintsState`, несущая информацию о **происхождении** (provenance) значения.

**Ключевое свойство**: Preferred не изменяет множество допустимых решений — только выбирает, КАКОЕ решение из интервала `[Desc..Anc]` будет выбрано при финализации.

Один и тот же constraint-интервал `[U24..Re]` может иметь разный Preferred в зависимости от происхождения:
- Целочисленный литерал `42` → `[U8..Re, P=I32]` (integer provenance)
- Вещественный литерал (в тесте) → `[U8..Re, P=Re]` (real provenance)
- Hex-литерал `0xFF` → `[U8..I96, P=I32]` (hex provenance, ancestor I96 вместо Re)

Preferred — это `StatePrimitive?` (nullable). Большинство CS-нод не имеют Preferred (null).

**Код**: `ConstraintsState.Preferred` — публичное свойство типа `StatePrimitive`.

---

## 2. Источники Preferred

Preferred создаётся **только** при построении графа (фаза Build). Три источника:

### 2.1. Целочисленные литералы

`TicSetupVisitor.Visit(GenericIntSyntaxNode)` (строки 1025-1107) создаёт generic constraint с Preferred из диалекта:

```
SetGenericConst(id, desc, anc: Real, preferred: GetPreferredIntConstantType())
```

`GetPreferredIntConstantType()` (строки 1112-1118) возвращает:
- `IntegerPreferredType.I32` → `StatePrimitive.I32` (default)
- `IntegerPreferredType.I64` → `StatePrimitive.I64`
- `IntegerPreferredType.Real` → `StatePrimitive.Real`

Примеры:
- `42` → `[U8..Re, P=I32]` (помещается в byte, может быть до Real)
- `-5` → `[I16..Re, P=I32]` (минимум I16 для отрицательных)
- `100000` → `[U24..Re, P=I32]` (не помещается в U16)

### 2.2. Hex/bin литералы

`TicSetupVisitor.Visit(GenericIntSyntaxNode)` при `node.IsHexOrBin` (строки 1029-1067):

```
SetGenericConst(id, desc, anc: I96, preferred: ...)
```

Отличие от обычных целых: ancestor = `I96` (не `Real`), потому что hex/bin литералы используются для битовых операций и не должны расширяться до вещественных. Preferred тот же (из диалекта), но для больших значений фиксируется:
- `0x7FFFFFFF` → `[U24..I96, P=I32]`
- Значения > I32 → `P=I64`

### 2.3. Вещественные литералы (тестовый API)

`GraphBuilderExtensions.SetIntConst()` (строки 46-50) — используется в TIC-тестах:

```csharp
public static void SetIntConst(this GraphBuilder b, int id, StatePrimitive desc)
    => b.SetGenericConst(id, desc: desc, anc: StatePrimitive.Real, preferred: StatePrimitive.Real);
```

Устанавливает `P=Real`. В production-коде вещественные константы (double) устанавливаются как `SetConst(id, Real)` — конкретный тип без constraint, поэтому Preferred не нужен.

### 2.4. API: SetGenericConst

`GraphBuilderExtensions.SetGenericConst()` (строки 52-63) — общий API для создания CS-ноды с Preferred:

```csharp
public static void SetGenericConst(this GraphBuilder b, int id,
    StatePrimitive desc = null, StatePrimitive anc = null, StatePrimitive preferred = null)
```

Устанавливает `desc`, `anc` и `Preferred` на CS-ноде. Единственная точка входа для задания Preferred.

---

## 3. Правила распространения (Propagation)

Preferred распространяется через 4 механизма, каждый на своей фазе.

### 3.1. Pull CS←CS: двунаправленное копирование

**Файл**: `PullConstraintsFunctions.cs`, строки 28-34.

При Pull от descendant к ancestor (CS←CS), Preferred копируется **в обе стороны**:

```
if (ancestor.Preferred == null && descendant.Preferred != null)
    ancestor.Preferred = descendant.Preferred;   // upward: desc → anc
if (descendant.Preferred == null && ancestor.Preferred != null)
    descendant.Preferred = ancestor.Preferred;    // downward: anc → desc
```

**Upward** (desc→anc): целочисленная константа в массиве `[1,2,3]` передаёт `P=I32` элементу массива, а оттуда — array-ноде.

**Downward** (anc→desc): struct field chain `s.m.n` — Preferred от литерала передаётся обратно через цепочку полей.

### 3.2. SetCallArgument: копирование при вызове функции

**Файл**: `GraphBuilder.cs`, строки 196-198.

При связывании аргумента функции с generic type parameter:

```
if (argCs.Preferred != null && genCs.Preferred == null)
    genCs.Preferred = argCs.Preferred;
```

Preferred от фактического аргумента копируется в generic-параметр функции. Это обеспечивает: `sum([1,2,3])` — generic `T` от `T[] → T` получает `P=I32` от элементов массива.

### 3.3. IntersectIntervalsOrNull: сохранение через пересечение

**Файл**: `ConstraintsState.cs`, строки 166-181.

При пересечении двух CS-интервалов (MergeOrNull, Pull с composites):

```
result.Preferred = this.Preferred ?? other.Preferred;
if (this.Preferred != null && other.Preferred != null && !this.Preferred.Equals(other.Preferred))
    result.Preferred = result.CanBeConvertedTo(this.Preferred) ? this.Preferred : other.Preferred;
if (result.Preferred != null && !result.CanBeConvertedTo(result.Preferred))
    result.Preferred = null;
```

Правила:
1. Если только одна сторона имеет Preferred — берётся он
2. Если оба одинаковые — берётся общий
3. Если оба разные — выбирается тот, что подходит к результирующему интервалу
4. Финальная проверка: если Preferred не вписывается в результирующий интервал — сбрасывается в null

### 3.4. LCA: сохранение через LCA двух CS

**Файл**: `StateExtensions.Lca.cs`, строки 27-34.

При вычислении LCA двух ConstraintsState:

```
var preferred = (ac.Preferred, bc.Preferred) switch {
    (null, null)  => null,
    (null, var p) => p,
    (var p, null) => p,
    var (pa, pb)  => pa.Equals(pb) ? pa : null
};
```

Правила: если оба совпадают — сохраняется; если один есть — берётся он; если различаются — теряется (null). Это консервативная стратегия: при конфликте Preferred лучше потерять hint, чем выбрать неверный.

---

## 4. PropagatePreferred pass — глобальный broadcast

### 4.1. Когда выполняется

Между фазами Push и Destruction. Последовательность фаз:
```
Build → Toposort → Pull → Push → PropagatePreferred → Destruction → Finalize
```

### 4.2. Зачем нужен

Constraint-алгебра **теряет provenance** на границах composite-типов. Пример:

```
s = { m = 42 }    # 42 → [U8..Re, P=I32]
x = s.m            # struct field access создаёт CS-посредник для 'm'
y = x + 1          # '+' создаёт generic T c constraint [U8..Re], но P=null
```

Узел `y` получает интервал `[U8..Re]` через Pull/Push, но Preferred теряется при структурной декомпозиции (snapshot struct → element node). PropagatePreferred восстанавливает `P=I32` на `y`.

### 4.3. Алгоритм

**Файл**: `SolvingFunctions.cs`, строки 327-367.

Два прохода по toposorted-нодам:

**Pass 1 — Collect**: обход всех нод (включая рекурсию в composite-члены). Ищет первую CS-ноду с непустым Preferred. Сохраняет в `commonPreferred`.

```csharp
if (nr.State is ConstraintsState cs && cs.Preferred != null)
    preferred ??= cs.Preferred;
```

Если ни одна нода не имеет Preferred — pass прерывается (нечего распространять).

**Pass 2 — Apply**: обход всех нод (с рекурсией в composites). Для каждой CS-ноды без Preferred проверяет совместимость и устанавливает:

```csharp
if (nr.State is ConstraintsState cs && cs.Preferred == null
    && cs.HasDescendant && cs.Descendant is StatePrimitive
    && cs.CanBeConvertedTo(preferred))
    cs.Preferred = preferred;
```

**Guard-условия для Apply**:
1. `cs.Preferred == null` — не перезаписывать существующий Preferred
2. `cs.HasDescendant` — должна иметь нижнюю границу (иначе интервал слишком абстрактен)
3. `cs.Descendant is StatePrimitive` — descendant должен быть примитивом (для composites Preferred не имеет смысла)
4. `cs.CanBeConvertedTo(preferred)` — Preferred должен лежать в интервале `[Desc..Anc]`

### 4.4. Единый глобальный Preferred

Текущая реализация собирает **один** `commonPreferred` для всего графа. Это корректно при условии, что все литералы в выражении имеют один Preferred-тип (что верно для текущего дизайна: все целочисленные литералы одного диалекта дают одинаковый Preferred).

---

## 5. Резолюция — как Preferred влияет на выходной тип

### 5.1. SolveCovariant (ковариантная резолюция)

**Файл**: `ConstraintsState.cs`, строки 258-281.

Preferred — **первый приоритет** (перед ancestor):

```
if (!ignorePreferred && Preferred != null && CanBeConvertedTo(Preferred))
    inner = Preferred;
else {
    inner = ancestor ?? Any;
    // ... comparable, composite desc cases
}
```

Порядок приоритетов:
1. **Preferred** (если `!ignorePreferred` и подходит к интервалу)
2. **Ancestor** (самый широкий тип в интервале)
3. **ConcreteAncestor** (для абстрактных desc без ancestor)

Параметр `ignorePreferred`: при `true` — Preferred пропускается. Используется для **function signatures** — функция `f(x) = x + 1` должна оставаться generic, а не резолвиться в I32.

### 5.2. SolveContravariant (контравариантная резолюция)

**Файл**: `ConstraintsState.cs`, строки 289-305.

Preferred — **первый приоритет** (безусловно, ignorePreferred отсутствует):

```
if (Preferred != null && CanBeConvertedTo(Preferred))
    inner = Preferred;
else if (!HasDescendant)
    return this; // unresolved
else
    inner = Descendant;
```

Порядок приоритетов:
1. **Preferred** (если подходит к интервалу)
2. **Descendant** (самый узкий тип в интервале)

### 5.3. TicTypesConverter: преобразование CS в FunnyType

**Файл**: `TicTypesConverter.cs`, строки 111-114.

Нерезолвленный CS с Preferred конвертируется напрямую:

```csharp
case ConstraintsState constrains when constrains.Preferred != null:
    if (constrains.HasDescendant && constrains.Descendant is StateOptional)
        return FunnyType.OptionalOf(ToConcrete(constrains.Preferred.Name));
    return ToConcrete(constrains.Preferred.Name);
```

Это fallback для случаев, когда CS не был резолвлен через SolveCovariant/SolveContravariant (например, при конвертации промежуточных нод).

### 5.4. ConcretestArrayElement: сохранение Preferred

**Файл**: `StateExtensions.Concretest.cs`, строки 30-42.

При вычислении Concretest для элемента массива, если CS имеет Preferred и desc != Preferred:

```csharp
if (element is ConstraintsState cs && cs.Preferred != null
    && cs.HasDescendant && cs.Descendant is StatePrimitive desc
    && !desc.Equals(cs.Preferred) && desc.CanBePessimisticConvertedTo(cs.Preferred)) {
    var result = ConstraintsState.Of(desc, isComparable: cs.IsComparable, isOptional: cs.IsOptional);
    result.Preferred = cs.Preferred;
    return result;
}
```

Вместо стандартного Concretest (который бы выбрал desc), создаётся новый CS с тем же desc но **без ancestor**, сохраняя Preferred. Это предотвращает потерю Preferred при конкретизации массива.

---

## 6. Формальные инварианты

### P1 — Safety (безопасность)

```
Preferred ∈ [Desc..Anc]  ∨  Preferred = null
```

Preferred всегда лежит внутри допустимого интервала, либо отсутствует. Гарантируется:
- При создании: `SetGenericConst` устанавливает Preferred совместимый с `[desc..anc]`
- При пересечении: `IntersectIntervalsOrNull` проверяет `CanBeConvertedTo` (строки 179-180)
- При PropagatePreferred: Apply проверяет `CanBeConvertedTo` (строка 365)
- При резолюции: `SolveCovariant`/`SolveContravariant` проверяют `CanBeConvertedTo`

### P2 — Idempotence (идемпотентность)

```
PropagatePreferred(PropagatePreferred(G)) = PropagatePreferred(G)
```

Второй вызов PropagatePreferred не изменяет граф:
- Collect находит тот же `commonPreferred` (Preferred не стирается)
- Apply пропускает ноды, уже имеющие Preferred (`cs.Preferred == null` guard)

### P3 — Monotonicity (монотонность)

```
∀ node: node.Desc(before) = node.Desc(after) ∧ node.Anc(before) = node.Anc(after)
```

Preferred **не сужает** интервал `[Desc..Anc]`. Установка Preferred не влияет на Pull/Push, не изменяет FitsInto, LCA, GCD. Preferred — metadata, ортогональная constraint-алгебре.

### P4 — Determinism (детерминизм)

При фиксированном topological order, PropagatePreferred детерминирован: первый найденный Preferred в Collect-порядке становится `commonPreferred`. Поскольку все литералы одного диалекта дают одинаковый Preferred, результат не зависит от порядка обхода.

---

## 7. Почему Preferred нельзя убрать

**Эксперимент**: заменить Preferred на resolution-time defaulting (выбирать I32 для всех числовых CS при финализации).

**Провал**: интервал `[U24..Re]` возникает в двух различных контекстах:

1. `[1,2,3].sum()` — должен резолвиться в `I32` (integer provenance)
2. `3.14 * 2` — `2` должен резолвиться в `Real` (real context)

Без Preferred оба случая неразличимы: одинаковый интервал `[U8..Re]`, но правильные ответы разные. Preferred несёт provenance, который constraint-алгебра теряет при структурной декомпозиции.

**Формально**: Preferred — это морфизм из пространства «литерал + контекст» в пространство «допустимый тип». Constraint-алгебра (LCA, GCD) оперирует множествами типов, теряя информацию об источнике. Preferred восстанавливает эту информацию.

---

## 8. Ограничения и будущая работа

### 8.1. Единый глобальный Preferred

PropagatePreferred собирает **одно** значение для всего графа. Если в выражении два литерала с разным Preferred (например, hex `0xFF` с `P=U16` и decimal `42` с `P=I32`), один из них будет потерян.

**Текущее состояние**: это не проблема, т.к. все целочисленные литералы одного диалекта дают одинаковый Preferred. Hex/bin литералы также используют `GetPreferredIntConstantType()`.

### 8.2. Edge-local propagation

Альтернатива глобальному broadcast — распространение Preferred по рёбрам графа (как Pull/Push). Каждая нода получала бы Preferred от своих прямых связей, а не от глобального значения. Это решило бы проблему 8.1.

**Не реализовано**: пока все литералы одного диалекта имеют одинаковый Preferred, edge-local propagation избыточен. При добавлении новых источников Preferred (typeclasses, user annotations) потребуется пересмотр.

### 8.3. Preferred для composite-типов

Текущий Preferred — всегда `StatePrimitive`. Нет механизма для «preferred array type» или «preferred struct type». PropagatePreferred Apply пропускает CS с composite descendant (`cs.Descendant is StatePrimitive` guard).

Для текущей системы типов NFun это достаточно: composite-типы не имеют ambiguity при резолюции (массив — это массив, struct — это struct). Preferred нужен только для числовых примитивов, где интервал `[U8..Re]` содержит десятки допустимых типов.
