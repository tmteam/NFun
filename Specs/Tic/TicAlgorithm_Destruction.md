# TIC Algorithm — Destruction + Finalize

## Обзор

После Pull+Push каждый узел имеет **интервал** `[D..A]` — множество допустимых типов. Destruction+Finalize **выбирают** конкретный тип из интервала.

Две фазы:
1. **Destruction** — попарная резолюция constraint edges + materialization IsOptional
2. **Finalize** — очистка ссылок, резолюция generics, валидация

### Принципиальность vs резолюция

TIC вычисляет **principal intervals** — самые тесные интервалы `[D..A]` совместимые со всеми ограничениями (Pull+Push). Это аналог principal types в HM.

Destruction+Finalize выполняют **non-principal resolution** — выбор конкретного типа из интервала через Preferred/Covariant/Contravariant стратегию. Это design decision языка (не логическая необходимость): литерал `1` с интервалом `[U8..Real]` разрешается в I32, не в U8 или Real.

---

## Предусловие: совместимость после Pull+Push

### Лемма совместимости

После Pull+Push для каждого constraint edge `D →c A`:

```
↓D ≤ ↑A    (Concretest потомка ≤ Abstractest предка)
```

**Следствие**: `Unify(D.CS, A.CS) ≠ null` — пересечение интервалов непусто.

**Обоснование**: Pull вычислил `A.Desc ⊇ LCA{↓Dᵢ}`. Push вычислил `D.Anc ⊆ GCD{↑Aⱼ}`. Constraint edge `D ≤ A` требует `↓D ≤ ↑A`. Если бы это не выполнялось, Pull или Push обнаружили бы конфликт (SimplifyOrNull = null → ошибка типов).

**Значение для Destruction**: Unify не может вернуть null на constraint edge — интервалы уже совместимы. Если Unify вернёт null — это внутренняя ошибка алгоритма, не пользовательская ошибка типов.

---

## Фаза 5: Destruction

### Цель

Разрешить оставшиеся ConstraintsState в конкретные типы или RefTo.

### Порядок

По **обратному** toposort. Для каждого узла D:
1. Рекурсивно Destruction **компонентов** (по component links). Cycle guard для рекурсивных типов.
2. Flatten nested Optionals (`Opt(Opt(T)) → Opt(T)`).
3. Обработка **ancestor-рёбер** D: для каждого `D →c A`, вызвать `Destruct(A, D)`.

### Правила (полная таблица)

Для каждого constraint edge `D →c A`:

**Оба решены:**

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive | Primitive | Нет действия. |
| Composite | Composite(same) | Рекурсивная Destruction по компонентам (см. ниже). |
| Composite | Composite(diff) | Ошибка (должна была быть обнаружена в Pull). |

**Ancestor решён, descendant — CS:**

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive P | CS | Если `P ∈ [D.Desc..D.Anc]`: `D := P`. Если `D.IsOptional` и `P ≠ None`: `D := Opt(P)`. С учётом Preferred (см. ниже). |
| Composite | CS | 1) Если FitsInto и `¬D.IsOptional`: `D := RefTo(A)`. 2) Если FitsInto и `D.IsOptional`: `D := Opt(inner)`, inner = RefTo(A). 3) Если ¬FitsInto и `D.Desc` — Composite того же вида: element-by-element destruct с Desc snapshot (см. ниже). |
| Opt(E) | CS(IsOptional) | Материализация D → Opt(inner), inner получает CS constraints. Далее `Destruct(Opt(E), Opt(inner))`. |
| Opt(E) | CS(not IsOptional) | Implicit lift: `Destruct(E, D)` (D ≤ E через T ≤ Opt(T)). |
| Opt(E) | Primitive(non-None) | Implicit lift: `Destruct(E, D)`. |
| Opt(E) | Primitive(None) | Нет действия (None ≤ Opt(T) для любого T). |

**Descendant решён, ancestor — CS:**

| A.State | D.State | Правило |
|---------|---------|---------|
| CS | Primitive P | Если `A.IsOptional` и `P ≠ None`: `A := Opt(P)`. Если `A.IsOptional` и `P = None`: нет действия (materialization позже). Иначе: `A := P`. |
| CS | Composite | 1) Если FitsInto и `¬A.IsOptional`: `A := RefTo(D)`. 2) Если FitsInto и `A.IsOptional`: `A := Opt(inner)`, inner = RefTo(D). 3) Если ¬FitsInto и `A.Desc` — Composite того же вида: element-by-element destruct с Desc snapshot. |
| CS(IsOptional) | Opt(E) | Материализация A → Opt(inner_A). Далее `Destruct(Opt(inner_A), Opt(E))`. |

**Оба — CS:**

| A.State | D.State | Правило |
|---------|---------|---------|
| CS(¬opt) | CS(¬opt) | `result := Unify(A, D)`. Если result — Primitive: обоим. Иначе: merge (main получает result, secondary → RefTo). |
| CS(opt) | CS(¬opt) | Материализация A → Opt(inner_A). `inner_A := Unify(inner_A, D)`. D → RefTo(inner_A). |
| CS(¬opt) | CS(opt) | Материализация D → Opt(inner_D). `inner_D := Unify(A, inner_D)`. A → RefTo(inner_D). |
| CS(opt) | CS(opt) | `result := Unify(A, D)`. `result.opt := true`. Merge. |

**Composite ← Composite (рекурсия):**

| A.State | D.State | Правило |
|---------|---------|---------|
| Opt(E_a) | Opt(E_d) | `Destruct(E_a, E_d)` |
| Arr(E_a) | Arr(E_d) | `Destruct(E_a, E_d)` |
| Fun(args_a→R_a) | Fun(args_d→R_d) | `Destruct(args_a_i, args_d_i)` для каждого arg + `Destruct(R_a, R_d)` |
| Struct_a | Struct_d | Для каждого общего поля: `Destruct(field_a, field_d)`. Width subtyping: descendant может иметь лишние поля. |

### Merge: RefTo и выбор main

Когда два узла сливаются, один становится **main** (хранит state), другой → RefTo(main):

1. TypeVariable уступает Named и Syntax
2. При равных типах: ancestor → main
3. Рёбра secondary переносятся на main

### Лемма переноса

При merge(A, D) → main=A, secondary=D: рёбра D переносятся на A. `Unify(A, D)` вычисляет пересечение интервалов: `result ⊆ A.interval ∩ D.interval`. Так как result ⊆ D.interval, main-узел (с result) удовлетворяет все рёбра, ранее принадлежавшие D (каждое ребро требовало совместимость с D.interval; result — его подмножество).

### Element-by-element Destruction с Desc snapshot

Когда CS-ancestor имеет `Desc = Composite` (snapshot из Pull), а descendant — Composite того же вида:

```
A:[CS, Desc=F<S₁,...>] ← D:[F<C₁,...>]
⟹  A.State := F<S₁,...> (snapshot), затем Destruct(F<S₁,...>, F<C₁,...>) покомпонентно
```

Если `A.IsOptional`: `A := Opt(inner)`, inner получает snapshot. Рекурсивная Destruction inner vs D.

---

## Materialization IsOptional (подфаза 5b)

После попарной резолюции, оставшиеся `CS(IsOptional=true)`:

| Состояние CS | Результат |
|-------------|-----------|
| `[∅..∅, opt]` (нет ограничений) | `None` (тип = отсутствие значения) |
| `[D..A, opt, ...]` (есть ограничения) | `Opt(inner)` где inner = CS(Desc=D, Anc=A, cmp, pref) |

Inner CS наследует **все** ограничения оригинала: Desc, Anc, IsComparable, Preferred.

Materialization — **не** оператор алгебры. Это **instantiation**: превращение флага в структуру. Аналог: структурная унификация (CS → Composite) превращает CS в composite-форму. Materialization превращает `CS(opt)` в `Opt(CS)`.

---

## Flatten nested Optionals (подфаза 5c)

`Opt(Opt(T)) → Opt(T)`. Постулат системы типов (см. `TicTypeSystem.md`).

Может возникнуть когда:
- Element-узел Optional изменился через другое ребро (стал Optional сам)
- Materialization вложила Optional в уже-Optional

Применяется рекурсивно к composite-членам после Destruction и после Finalize дереференса.

---

## Preferred — стратегия выбора

Preferred — **hint** от литерала или контекста. Не участвует в Pull/Push (не алгебраический). Используется в Destruction/Finalize для выбора конкретного типа из интервала.

**Правило**: если `D ≤ P ≤ A` (P внутри интервала): выбираем P. Иначе: P игнорируется.

**Примеры**:
- Литерал `1`: `[U8..Real, pref=I32]` → I32 (P в интервале)
- `x:i64 = 1`: литерал `[U8..Real, pref=I32]`, ancestor I64. Push → `[U8..I64, pref=I32]`. I32 в интервале → I32.
- `x:u8 = 1`: Push → `[U8..U8]`. P=I32 вне интервала → U8.

**Контракт**: Preferred никогда не выбирает тип вне интервала.

---

## Фаза 6: Finalize

### 6a. Дереференс и валидация

Для каждого узла:
- **StateRefTo** → разыменовать цепочку до конкретного state
- **Рекурсивные типы**: валидация что циклы в component links проходят через Opt или Array (коиндуктивные типы валидны; прямые struct/fun циклы — ошибка)
- **Flatten** nested Optionals (safety net)

### 6b. Резолюция generics

CS-узлы не в output-типах — "useless generics", не видимые пользователю. Стратегия:

**Ковариантная** (`SolveCovariant`): выбирает тип "по умолчанию":
1. Preferred в интервале → Preferred
2. Anc определён → Anc (самый широкий допустимый)
3. Только Desc → Desc (самый узкий)
4. Пусто → Any

**Контравариантная** (`SolveContravariant`, аргументы функций): выбирает самый **широкий** тип (Abstractest). Обоснование: функция безопаснее когда принимает более широкий тип.

**Контракт**: результат FitsInto исходного CS. Обоснование по случаям:
- Preferred: `D ≤ P ≤ A` — по условию выбора
- Anc: `D ≤ A ≤ A` — тривиально
- Desc: `D ≤ D ≤ A` — тривиально
- Any: CS = `[∅..∅]` → FitsInto(Any, [∅..∅]) = true

**IsComparable**: если CS.cmp=true и выбранный тип не comparable → ошибка. SolveCovariant/Contravariant проверяют comparable-совместимость результата.

### 6c. Output generics

CS-узлы в output-типах **не разрешаются** — остаются как интервалы. Runtime использует Preferred для финального выбора. Результат: `TicResultsWithGenerics`.

---

## Корректность

### Утверждение

Если Pull+Push корректны (теорема минимального интервала), то Destruction+Finalize корректны: для каждого constraint edge `D →c A`, после Destruction выполняется `D.resolved ≤ A.resolved`.

### Лемма 1: Совместимость (precondition)

**Формулировка**: после Pull+Push, ∀ edge `D →c A`: `↓D ≤ ↑A`.

**Доказательство**: Pull вычислил `A.Desc ⊇ LCA{↓Dᵢ}`, значит `↓D ≤ A.Desc ≤ ↑A` (Desc ≤ Anc — инвариант непустого интервала, проверяется SimplifyOrNull). Аналогично Push: `D.Anc ⊆ GCD{↑Aⱼ}`, значит `↓D ≤ D.Anc ≤ ↑A`.

**Следствие**: `Unify(D, A) ≠ null` — пересечение `[LCA(D.Desc, A.Desc) .. GCD(D.Anc, A.Anc)]` непусто.

### Лемма 2: Резолюция (per-rule correctness)

Для каждого правила таблицы: precondition (совместимость) → postcondition (resolved type ∈ interval).

**Primitive P → CS[D..A]**: `D := P` при `P ∈ [D.Desc..D.Anc]` (проверяется FitsInto). С Preferred: `D := Pref` при `Pref ∈ [D.Desc..D.Anc]`, иначе `D := P`. Postcondition: `D.Desc ≤ result ≤ D.Anc`. ∎

**Primitive P → CS[D..A, opt]**: `D := Opt(P)` при `P ∈ [D.Desc..D.Anc]`, `P ≠ None`. Postcondition: `Opt(P)` — валидный тип, `P ≤ Opt(P)` (implicit lift), `P ∈ [D..A]` проверено. ∎

**CS[D_a..A_a] → Primitive P**: `A := P` при `P ∈ [A.Desc..A.Anc]`. С IsOptional: `A := Opt(P)`. Аналогично предыдущему. ∎

**Composite → CS**:
- Случай FitsInto: `D := RefTo(A)`. FitsInto(Composite, CS) = `Composite ∈ [D.Desc..D.Anc]` — проверено. ∎
- Случай ¬FitsInto + snapshot: `A.State := A.Desc` (snapshot = Composite из Pull). Рекурсивная Destruction: `Destruct(snapshot, D)` по компонентам.
  **Индукция по глубине composite**: база — Primitive компоненты (случай Prim→CS, доказан выше). Шаг — composite компоненты: глубина(component) < глубина(parent), по гипотезе индукции component-level Destruction корректен. Snapshot валиден: `A.Desc` установлен Pull-ом как `LCA{↓Dᵢ}`, значит `A.Desc ≤ ↑A` (инвариант непустого интервала). ∎

**CS → Composite**: симметрично: `A := RefTo(D)` при FitsInto, иначе snapshot `D.Desc` + рекурсия. Та же индукция. ∎

**CS(¬opt) → CS(¬opt)**: `result := Unify(A, D) = [LCA(D_a, D_d) .. GCD(A_a, A_d)]`. По Лемме 1: пересечение непусто. `result ⊆ A.interval ∩ D.interval` — по определению Unify. ∎

**CS(opt) → CS(¬opt)**: Материализация: `A := Opt(inner_A)`. `inner_A := Unify(inner_A, D)` — inner_A наследует A constraints без opt. По Лемме 1 + inner_A.interval = A.interval: непусто. ∎

**CS(¬opt) → CS(opt)**: Симметрично. ∎

**CS(opt) → CS(opt)**: `result := Unify(A, D)`. `result.opt := true`. По Лемме 1: непусто. ∎

**Opt(E_a) ← Opt(E_d)**: `Destruct(E_a, E_d)` рекурсивно. По ковариантности: `E_d ≤ E_a` ⟺ `Opt(E_d) ≤ Opt(E_a)`. По индукции. ∎

**Arr(E_a) ← Arr(E_d)**: аналогично Opt, ковариантность. ∎

**Fun(A_a→R_a) ← Fun(A_d→R_d)**: `Destruct(A_a_i, A_d_i)` (args) + `Destruct(R_a, R_d)` (return). Args контравариантны: `A_d_i ≤ A_a_i`. Return ковариантен: `R_d ≤ R_a`. По индукции. ∎

**Struct_a ← Struct_d**: поле-по-полю, ковариантно. Width subtyping: descendant может иметь лишние поля (не нарушает). По индукции по каждому полю. ∎

**Opt(E) ← Primitive(non-None)**: implicit lift `P ≤ E` (через `T ≤ Opt(T)`). `Destruct(E, P)`. По индукции. ∎

**Opt(E) ← None**: `None ≤ Opt(T)` для любого T — тривиально. ∎

**Opt(E) ← CS(IsOptional)**: Материализация D → Opt(inner). inner наследует CS constraints. `Destruct(Opt(E), Opt(inner))` → `Destruct(E, inner)`. По индукции. ∎

**Opt(E) ← CS(¬opt)**: implicit lift: `Destruct(E, D)`. `D ≤ E` достаточно для `D ≤ Opt(E)`. По индукции. ∎

### Лемма 3: Перенос (merge correctness)

**Формулировка**: при merge(A, D) → main=A, secondary=D, все рёбра D удовлетворены main-узлом.

**Доказательство**:

**Исходящие рёбра** (`D →c X`): ребро требует `D.resolved ≤ X.resolved`. `result = Unify(A, D) ⊆ D.interval`. Любой тип T выбранный из result удовлетворяет `T ∈ D.interval`, значит `T ≤ ↑D ≤ ↑X` (по Лемме 1 для ребра D→X). ∎

**Входящие рёбра** (`Y →c D`): ребро требует `Y.resolved ≤ D.resolved`. После merge, D → RefTo(main). Destruction **обработает** ребро `Y →c main` при обработке Y (Destruction итерирует ancestor-рёбра каждого узла). Корректность этого ребра следует из Леммы 1 для пары (Y, main): `↓Y ≤ ↑main`. `↑main = ↑Unify(A,D) ≤ ↑D` (Unify сужает). `↓Y ≤ ↑D` (Лемма 1 для Y→D). Значит `↓Y ≤ ↑main`. ∎

### Лемма 4: Materialization

**Формулировка**: `CS[D..A, opt] → Opt(inner)` где inner = `CS[D..A]`. Результат корректен.

**Доказательство**:
- Случай `[∅..∅, opt]` → None. None — валидный тип для optional без ограничений. ∎
- Случай `[D..A, opt, ...]` → `Opt(inner)`, inner = `CS[D..A, cmp, pref]`. Inner наследует все ограничения. Любой `T ∈ [D..A]` → `Opt(T)` валиден. `T ≤ Opt(T)` (implicit lift). Comparable: inner проверяет cmp — наследовано. Preferred: inner наследует pref — resolution корректна. ∎

### Лемма 5: Finalize (SolveCovariant/Contravariant)

**Формулировка**: для CS `[D..A, cmp, pref]`, SolveCovariant возвращает T такой что FitsInto(T, CS) = true.

**Доказательство по случаям**:
1. `Preferred ∈ [D..A]` → T = Pref. `D ≤ Pref ≤ A` — по условию выбора. cmp: если CS.cmp, Preferred проверяется на comparable. ∎
2. `Anc ≠ ∅` → T = Anc. `D ≤ A ≤ A` — тривиально. cmp: Anc проверяется. ∎
3. Только Desc → T = Desc. `D ≤ D ≤ A` — тривиально (A=∅ ≡ Any, `D ≤ Any`). cmp: Desc проверяется. ∎
4. Пусто → T = Any. `FitsInto(Any, [∅..∅]) = true`. cmp: CS.cmp=false — **инвариант**: если cmp=true, то существует constraint edge от comparable-функции (оператор `>`, `<` и т.д.), который через Push устанавливает Anc ≠ ∅. Следовательно `[∅..∅, cmp]` не достигает Finalize. ∎

**SolveContravariant**: T = Abstractest(CS) = Anc (или Any если ∅). `D ≤ T ≤ A` — аналогично случаю 2. ∎

### Ограничение

Корректность условна: runtime conversions корректны (A ≤ B → значение A безопасно используется как B). Это **аксиома реализации** — верифицируется per-conversion тестами, не доказывается в TIC алгебре.

---

## Пример

Продолжение из `TicAlgorithm.md`: `y = if(x > 0) x else 1`

После Pull+Push:
```
x:  [∅..Real, cmp]
0:  [U8..Real, pref=I32]
1:  [U8..Real, pref=I32]
T': [U8..Real, cmp]
R:  [U8..∅]
y:  [U8..∅]

Edges: x →c T', 0 →c T', x →c R, 1 →c R, R →c y
```

**Destruction** (reverse toposort: y, R, T', x, 1, 0):

Обработка ancestor-рёбер каждого узла:

```
y: ancestor нет → пропуск

R: ancestor y.
  Destruct(y, R): оба CS. Unify([U8..∅], [U8..∅]) = [U8..∅].
  y := RefTo(R).

T': ancestor нет → пропуск

x: ancestors T', R.
  Destruct(T', x): оба CS. Unify([U8..Real,cmp], [∅..Real,cmp]) = [U8..Real,cmp].
  x := RefTo(T'). T' сохраняет [U8..Real, cmp].
  Destruct(R, x): x теперь RefTo(T'). Дереференс → T':[U8..Real,cmp].
  Destruct(R, T'): оба CS. Unify([U8..∅], [U8..Real,cmp]) = [U8..Real,cmp].
  R := RefTo(T'). T' → [U8..Real, cmp].

1: ancestor R.
  Destruct(R, 1): R = RefTo(T'). Дереференс → T':[U8..Real,cmp].
  Destruct(T', 1): оба CS. Unify([U8..Real,cmp], [U8..Real,pref=I32]) = [U8..Real,cmp,pref=I32].
  1 := RefTo(T'). T' → [U8..Real, cmp, pref=I32].

0: ancestor T'.
  Destruct(T', 0): оба CS. Unify([U8..Real,cmp,pref=I32], [U8..Real,pref=I32]) = [U8..Real,cmp,pref=I32].
  0 := RefTo(T').
```

После Destruction: все → RefTo(T'). T': `[U8..Real, cmp, pref=I32]`.

**Finalize**: SolveCovariant(T'): Preferred=I32, I32 ∈ [U8..Real], I32 comparable → **I32**.

Итог: `x = I32, y = I32`. Все узлы → I32 через RefTo(T').

---

## Сложность

| Подфаза | Сложность |
|---------|-----------|
| Попарная резолюция | O(|constraint_edges| + |component_links|) |
| Materialization | O(|nodes|) |
| Flatten | O(|component_links|) |
| Finalize dereference | O(|nodes| + |component_links|) |
| SolveGenerics | O(|generics|) |
| **Итого** | **O(|graph|)** |
