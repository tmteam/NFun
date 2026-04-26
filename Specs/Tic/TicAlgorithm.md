# TIC Algorithm — Построение и решение графа ограничений

## Фазы

1. **Build** — построение constraint graph из AST
2. **Toposort** — топологическая сортировка + слияние циклов (может быть fused с Pull при отсутствии None)
3. **Pull** — распространение нижних границ (desc → anc), оператор LCA
4. **Push** — распространение верхних границ (anc → desc), оператор GCD
5. **PropagatePreferred** — broadcast Preferred hint по всем совместимым CS нодам (см. ниже)
6. Destruction + Finalize — см. `TicAlgorithm_Destruction.md`

Pull и Push — ядро алгоритма. Все операции в них выражаются через:
- **6 алгебраических операторов** (LCA, GCD, FitsInto, Unify, Concretest, Abstractest) — см. `Algebra.md`
- **Структурная декомпозиция** — следствие правил подтипирования для composites: `F<A₁,...> ≤ F<B₁,...>` раскладывается на компонентные ограничения по вариантности (см. `TicTypeSystem.md`)
- **Implicit lift** — постулат подтипирования `T ≤ Opt(T)` (см. `TicTypeSystem.md`)

Результат Pull+Push **не зависит** от выбора конкретного topological order (следствие confluence, доказанной в `Algebra.md` через коммутативность/ассоциативность LCA/GCD и лемму Ньюмана).

---

## Соглашения

### Пустые границы (∅)

`∅` в Desc или Anc означает **отсутствие ограничения**:
- `Desc = ∅` — нижняя граница не установлена. Эквивалент: "любой тип допустим снизу."
- `Anc = ∅` — верхняя граница не установлена. Эквивалент: "любой тип допустим сверху."

Операторы с ∅ (∅ — нейтральный элемент при **комбинировании однородных границ**):
```
LCA(∅, T) = T          (комбинирование нижних границ: ∅ не ограничивает)
LCA(∅, ∅) = ∅
GCD(∅, T) = T          (комбинирование верхних границ: ∅ не ограничивает)
GCD(∅, ∅) = ∅
↓[∅..A] = ∅            (Concretest: нет нижней границы)
↑[D..∅] = ∅            (Abstractest: нет верхней границы)
FitsInto(T, ∅) = true  (нет ограничения — всё допустимо)
FitsInto(∅, T) = true
```

**Область применения**: ∅ как нейтральный элемент используется **только** при комбинировании границ одного вида: Pull комбинирует Desc (нижние) через LCA, Push комбинирует Anc (верхние) через GCD. Операторы LCA/GCD с ∅ **не вызываются** для проверки совместимости между Desc и Anc разных интервалов — для этого используется FitsInto.

`∅` — **не тип**, а отсутствие информации. Семантически: `[∅..∅]` = "неограниченная переменная", `[D..∅]` = "известна нижняя граница", `[∅..A]` = "известна верхняя граница".

### Флаги ConstraintsState

Помимо интервала `[D..A]`, ConstraintsState содержит два boolean-флага:

- **IsOptional** — тип допускает None. Устанавливается через None absorption в Pull. Propagation: `opt_result := opt_a ∨ opt_d` (OR — расширяет множество допустимых значений).
- **IsComparable** — тип должен поддерживать сравнение. Устанавливается из сигнатуры функции (операторы `>`, `<`, `==` и т.д.). Propagation: при **Pull** — не пропагируется (comparable на descendant не требует comparable на ancestor: `max(a,b)` возвращает comparable, но if-else может объединить comparable и non-comparable). При **Push** — передаётся от ancestor к descendant (`D.cmp := D.cmp ∨ A.cmp`): если ancestor comparable, descendant тоже должен быть. При **Merge** (Destruction): `cmp_result := cmp_a ∨ cmp_d` (OR). Валидация: SimplifyOrNull отвергает интервал если IsComparable=true но тип не comparable.

Оба флага **не участвуют в LCA/GCD** напрямую. Они propagate по своим правилам при обработке CS←CS и CS→CS.

**Preferred** — hint для resolution (pref=I32 для целочисленных литералов). **Не участвует** в алгебраических операциях (LCA, GCD, FitsInto). Preferred — metadata, не constraint. Влияет только на `SolveCovariant` при финальном выборе типа из интервала. Полная спецификация: `TicPreferred.md`.

**Правила propagation Preferred:**

1. **Pull(CS←CS)**: если `A.pref = ∅ ∧ D.pref ≠ ∅`: `A.pref := D.pref` (upward). Также: если `D.pref = ∅ ∧ A.pref ≠ ∅`: `D.pref := A.pref` (downward, для struct field chains).

2. **IntersectIntervalsOrNull**: при пересечении двух CS берётся первый ненулевой Preferred, проверяется `CanBeConvertedTo`. Если оба имеют разный Preferred — выбирается тот что подходит к результату.

3. **PropagatePreferred pass** (после Push, перед Destruction): отдельный проход по всем узлам графа.
   - **Collect**: рекурсивно обходит все CS-ноды (включая вложенные в composites). Собирает первый найденный Preferred.
   - **Apply**: рекурсивно обходит все CS-ноды. Если CS не имеет Preferred, desc — StatePrimitive, и CS может быть сконвертирован в collected Preferred → устанавливает `cs.Preferred = collected`.
   - Это решает struct field access: `s.m` создаёт CS-посредники без Preferred. PropagatePreferred раздаёт I32 (от литералов) всем совместимым CS-нодам в графе.
   - Безопасность: Preferred — metadata, не constraint. Установка Preferred на совместимый CS не меняет корректность — только улучшает разрешение.

**Инвариант**: Preferred никогда не СОЗДАЁТ constraint — он только ВЫБИРАЕТ из существующих валидных решений. Поэтому propagation Preferred безопасен: любой результат в интервале `[Desc..Anc]` корректен, Preferred лишь выбирает "лучший".

---

## Фаза 1: Build

**Вход**: AST + реестр функций. **Выход**: TIC Graph (см. `TicGraph.md`).

### Правила построения

Каждая конструкция языка порождает узлы и рёбра:

| Конструкция | Constraint edges | Component links | Состояние |
|-------------|-----------------|-----------------|-----------|
| Литерал `42` | — | — | `[U8..Real, pref=I32]` |
| Переменная `x` | `usage →c x` | — | `[∅..∅]` |
| Определение `y = e` | `e →c y` | — | y: output |
| `if(c) a else b` | `a →c R`, `b →c R` | — | R: `[∅..∅]` |
| `try a catch b` | `a →c R`, `b →c R` | — | R: `[∅..∅]` |
| `try a catch(e) b` | `a →c R`, `b →c R` | — | R: `[∅..∅]`, e: `{message:text, data:any}` (scoped) |
| `[a, b, c]` | `a →c E`, `b →c E`, `c →c E` | `Arr →comp E` | Arr: Array(E) |
| `{x:a, y:b}` | `a →c Nₓ`, `b →c Nᵧ` | `S →comp Nₓ`, `S →comp Nᵧ` | S: Struct (frozen) |
| `s.field` | — | `s →comp N` | s: Struct{field:N} (mutable) |
| `s?.field` | — | `res →comp inner` | res: Opt(inner) |
| `a ?? b` | `a →c Opt(U)`, `U →c R`, `b →c R` | `Opt(U) →comp U` | U: fresh `[∅..∅]`, R: `[∅..∅]` |
| `a?[i]` | `a →c Opt(Arr(E))`, `i →c I32` | `res →comp inner` | res: Opt(inner), inner связан с E |
| `x != none` narrowing | `orig →c Opt(T)`, `T` merged с narrowed | — | narrowed: unwrapped тип внутри if-branch |

### Инстанциация вызовов функций

Вызов `f(a₁,...,aₙ)` с generic-сигнатурой `(P₁,...,Pₙ) → R` (где Pᵢ, R могут содержать типовые переменные T₁,...,Tₘ):

1. Для каждой типовой переменной Tⱼ из сигнатуры создаётся **свежий узел** Tⱼ' с ограничениями из сигнатуры. Например, `(T,T)→T` где T:[U8..Real,cmp] → узел T' с состоянием `[U8..Real,cmp]`.
2. Для каждого аргумента: `aᵢ →c Pᵢ'` (constraint edge к инстанциированному параметру).
3. Для composite-параметров (Array, Fun, Struct): создаются component links и узлы-компоненты по структуре параметра.
4. Результат: merge узла возврата R' с узлом выражения вызова.

**Пример**: `count(arr)` с сигнатурой `(T[]) → I32`:
- Узел E':[∅..∅] (свежий, для T), узел P':Array(E') (параметр)
- `arr →c P'`, component link `P' →comp E'`
- Результат вызова: I32

**Пример**: `x + 1` с сигнатурой `(T,T) → T` где T:[U8..Real]:
- Узел T':[U8..Real] (свежий)
- `x →c T'`, `1 →c T'`
- Результат: RefTo(T')

### Constraint generation rules (формально)

Для каждой синтаксической формы — правило генерации ограничений:

```
[[lit: P]]           = node(lit) : [P..P]
[[lit: N, pref=P]]   = node(lit) : [N..P_anc, pref=P]         (числовой литерал)
[[x]]                = node(usage) →c node(x)                  (использование переменной)
[[y = e]]            = [[e]] →c node(y);  output(y)            (определение)
[[if(c) a else b]]   = [[c]] →c Bool;  [[a]] →c R;  [[b]] →c R;  R : [∅..∅]
[[try a catch b]]    = [[a]] →c R;  [[b]] →c R;  R : [∅..∅]
[[try a catch(e) b]] = [[a]] →c R;  scope(e:{message:text, data:any}){ [[b]] } →c R;  R : [∅..∅]
[[ [a₁,...,aₙ] ]]    = ∀i: [[aᵢ]] →c E;  Arr : Array(E);  Arr →comp E
[[{f₁:a₁,...}]]      = ∀i: [[aᵢ]] →c Nᵢ;  S : Struct{f₁:N₁,...} (frozen);  S →comp Nᵢ
[[e.f]]              = [[e]] becomes Struct{f:N} (mutable);  result = N
[[e?.f]]             = [[e.f]];  result = Opt(inner)
[[f(a₁,...,aₙ)]]    = instantiate(sig(f));  ∀i: [[aᵢ]] →c Pᵢ';  result = merge(R', call_node)
[[a ?? b]]           = U fresh; [[a]] →c Opt(U); U →c R; [[b]] →c R;  R : [∅..∅]   (SetCoalesce)
```

Instantiate: для каждой типовой переменной T в сигнатуре — свежий узел T'. Composite-параметры: shape-rigid (IsSignatureParam). Результат: подстановка `σ = {T₁↦T'₁, ...}`, применённая к сигнатуре.

### Инвариант

После Build:
- Каждое `D →c A` означает `D ≤ A`.
- Каждое `P →comp C` означает C — структурная часть P.
- Constraint edges формируют DAG (возможны циклы — устраняются в Toposort).

---

## Фаза 2: Toposort

**Вход**: граф из фазы 1. **Выход**: линейный порядок `N₁,...,Nₖ`.

**Инвариант порядка**: `Nᵢ →c Nⱼ` ⟹ `i < j`.

DFS обходит **оба** вида рёбер (constraint + component). Порядок посещения: компоненты → ancestors → сам узел.

**Оптимизация: fused Toposort+Pull.** Если в графе нет ни одного None-узла (`hasNone=false`), Toposort и Pull объединяются в один проход: Pull выполняется inline для каждого узла в момент его emit-а из toposort. Если `hasNone=true` — Toposort и Pull выполняются раздельно (двухфазный Pull требует предварительного полного toposort-порядка). Семантика идентична; это чисто перформансная оптимизация.

**Циклы**: при обнаружении цикла узлы сливаются в один (состояния объединяются через **Unify**). Циклы возникают из взаимных ссылок (`x = f(y); y = g(x)`). После слияния граф — DAG.

**Member-узлы** (компоненты composites) включены в порядок, но помечены — в основных циклах Pull/Push пропускаются, обрабатываются рекурсивно через component links.

---

## Базовые операции

### Структурная декомпозиция

Следствие правил подтипирования composites (см. `TicTypeSystem.md`). Если `D ≤ A` и оба — composites одного конструктора, ограничение раскладывается на компонентные:

| Конструктор | Декомпозиция `D ≤ A` |
|-------------|---------------------|
| Array | `D.elem ≤ A.elem` |
| Optional | `D.elem ≤ A.elem` |
| Fun | `D.ret ≤ A.ret`, `A.argᵢ ≤ D.argᵢ` |
| Struct | `D.fₖ ≤ A.fₖ` для каждого поля fₖ ∈ A |

Fun: аргументы **контравариантны** (направление инвертируется). Struct: потомок должен иметь **все** поля предка (width subtyping) — недостающие поля добавляются. Если предок mutable — лишние поля потомка также добавляются к предку (Pull вычисляет most specific bound, больше полей = more specific).

Разные конструкторы несовместимы: `Array(...) ≤ Fun(...)` → ошибка типов.

### Структурная унификация

Если `D ≤ A`, D — ConstraintsState, A — Composite `F<C₁,...,Cₙ>`:

1. D **принимает форму** A: `D.State := F<C'₁,...,C'ₙ>`
2. Новые компоненты C'ᵢ:
   - Если CS.Desc — composite того же конструктора F: C'ᵢ наследуют соответствующие компоненты Desc
   - Если CS.Desc — composite другого конструктора: **ошибка типов** (Array ≤ Struct несовместимы)
   - Если CS.Desc = ∅ или Primitive: C'ᵢ := `[∅..∅]` (свежие неограниченные)
3. Исходное ребро `D →c A` **заменяется** на компонентные рёбра (декомпозиция)
4. Новые component links: `D →comp C'ᵢ`
5. **Все остальные** constraint edges D (к другим ancestors) остаются. При их обработке D уже имеет composite-форму → срабатывает Composite←Composite или Composite←CS по стандартным правилам

**Инвариант**: структурная унификация создаёт рёбра только к **потомкам** A в toposort (компоненты A расположены раньше A в toposort). Поэтому новые рёбра обрабатываются **в том же проходе** рекурсивно, без необходимости повторного обхода.

### Optional wrapping (расширение ancestor)

Если `D ≤ A`, D — `Opt(F<...>)`, A — `F<...>` (тот же конструктор, но без Optional):

```
A:[F<C₁,...>]  ≤  D:[Opt(F<C'₁,...>)]
⟹  A := Opt(inner),  inner.State := F<C₁,...>,  затем декомпозиция inner ≤ D.elem
```

Это **LCA**: `LCA(F<...>, Opt(F<...>)) = Opt(F<...>))` — ancestor расширяется чтобы вместить Optional-descendant. Аналогично тому как структурная унификация сужает CS до Composite, Optional wrapping расширяет Composite до Opt(Composite).

| Операция | Направление | Когда |
|----------|-------------|-------|
| Структурная унификация | CS → Composite (сужение) | CS встречает Composite ancestor |
| Optional wrapping | Composite → Opt(Composite) (расширение) | Composite ancestor встречает Opt(Composite) descendant |

Обе операции — **смена состояния узла**, не монотонное сужение интервала. Обе алгебраически обоснованы: унификация следует из `CS ≤ Composite`, wrapping следует из `LCA(T, Opt(T)) = Opt(T)`.

**Shape-rigid узлы**: Optional wrapping применяется только к **inferred** composites (результаты if-else, переменные). Composites из **сигнатур функций** являются shape-rigid — их форма задана (given), не выводится (inferred). Wrapping shape-rigid узла — ошибка типов: `Opt(T) ≤ T` невалиден, и расширение сигнатуры до Optional изменило бы контракт функции.

Аналогия с HM: shape-rigid ≈ rigid (skolem) type variables. Форма конструктора фиксирована, компоненты внутри — flexible.

**Реализация: WrapAncestorInOptional / WrapDescendantInOptional.** Обе функции в `StagesExtension` — единый dispatch для Optional wrapping, используемый всеми фазами (Pull, Push, Destruction):
- **WrapAncestorInOptional**: отвергает SyntaxNode (ошибка типов — литерал не может стать Optional) и IsSignatureParam (shape-rigid). Создаёт inner-узел с текущим состоянием ancestor, устанавливает `IsOptionalElement`.
- **WrapDescendantInOptional**: для SyntaxNode — fallback: unwрапливает Optional ancestor и рекурсивно применяет функцию к element (литерал не оборачивается, а проверяется как element). Для TypeVariable — аналогично WrapAncestor: создаёт inner-узел, устанавливает `IsOptionalElement`.

Все остальные constraint edges A остаются. При их обработке A уже имеет Optional-форму → срабатывает Opt←Opt, Opt←Primitive или Opt←CS по стандартным правилам.

### Implicit lift (Optional)

Постулат `T ≤ Opt(T)` — правило подтипирования (см. `TicTypeSystem.md`). Применяется в трёх ситуациях:

1. **Unwrap**: `D:[non-Opt] ≤ A:[Opt(E)]` → ограничение переписывается как `D ≤ E`
2. **Wrap**: `D:[CS, IsOptional] ≤ A:[non-Opt Composite]` → D принимает форму `Opt(inner)`, inner получает ограничения CS без IsOptional, `inner ≤ A`
3. **None absorption**: `D:None ≤ A:[CS]` → `A.IsOptional := true` (не меняет Desc/Anc)

**Обратное направление запрещено**: `Opt(T) ≤ T` — ошибка типов (implicit unwrap невалиден).

---

## Фаза 3: Pull

**Цель**: для каждого `D →c A` передать нижнюю границу D в A.

**Порядок**: по toposort (потомки раньше предков). Для каждого узла: сначала обработка constraint edges (ancestor edges), затем рекурсивно Pull компонентов (по component links). Обоснование: Pull может изменить состояние узла (CS → Composite через TransformToXxx), после чего composite members нужно рекурсивно обработать.

### Определение

```
Pull(A, D) — обновить A, учитывая D ≤ A
```

### None: двухфазная обработка

Если в графе есть хотя бы один узел с состоянием None, Pull выполняется в два прохода **по тому же toposort-порядку**:

**Проход 1** — только None-узлы:
- Итерация по toposort порядку. Для каждого узла D: если `D.State = None`, обработать все constraint edges D (каждое `D →c A`).
- Единственное действие: `A.IsOptional := true` (правило None absorption).
- Всё остальное пропускается.

**Проход 2** — все не-None-узлы:
- Итерация по toposort порядку. Для каждого узла D: если `D.State ≠ None`, обработать по стандартным правилам Pull.
- К этому моменту IsOptional уже установлен на всех ancestors None-узлов. Concretest и LCA корректно учитывают optional-природу.

**Зачем**: None absorption устанавливает IsOptional — **флаг**, а не значение в решётке. Другие правила Pull (LCA, структурная унификация) зависят от этого флага. Без двухфазности: если не-None потомок обработан до None-потомка, ancestor может получить Desc без учёта IsOptional, что приводит к ложным конфликтам.

Если None-узлов нет — один проход, все узлы.

### Правила

Для каждого constraint edge `D →c A`:

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive | Primitive | Проверка `D ≤ A` (FitsInto). Ошибка если false. |
| Primitive | CS | Проверка `↓D ≤ A` (FitsInto). Ошибка если false. |
| Primitive | Composite | Проверка `D ≤ A` (FitsInto). Только `Any` совместим. |
| CS | Primitive | `A.Desc := LCA(A.Desc, D)`. Проверка: `A.Desc ≤ A.Anc`. Preferred: если `A.pref = ∅`, копировать из контекста (литерал → ancestor). |
| CS | CS | `A.Desc := LCA(A.Desc, ↓D)`. `A.opt := A.opt ∨ D.opt`. `A.cmp := A.cmp ∧ D.cmp`. Preferred: если `A.pref = ∅ ∧ D.pref ≠ ∅`: `A.pref := D.pref`. |
| CS | Composite(non-Opt) | `A.Desc := LCA(A.Desc, D)`. |
| CS | Optional | Implicit lift (wrap): A принимает форму Opt(inner) — inner получает ограничения A, `D.elem ≤ inner`. |
| Composite | CS | Структурная унификация D → форма A, затем декомпозиция. |
| Composite | Composite(same) | Структурная декомпозиция → Pull для каждой пары компонент. |
| Opt(E) | Primitive(non-None) | Implicit lift (unwrap): `D ≤ E`. |
| Opt(E) | None | Нет действия (None ≤ Opt(T) для любого T). |
| non-Opt Comp(F) | Opt(F) (same, A inferred) | Optional wrapping: `A := Opt(inner)`, `inner.State := A.old_state`, декомпозиция `inner ≤ D.elem`. |
| non-Opt Comp(F) | Opt(F) (same, A shape-rigid) | **Ошибка**: signature-given shape не может быть расширена до Optional. |
| non-Opt Comp | Opt(different constructor) | **Ошибка**: разные конструкторы несовместимы. |
| Composite | Composite(diff, non-Opt) | **Ошибка**: разные конструкторы несовместимы. |

### Результат Pull

```
∀ узел A:  A.Desc ⊇ LCA{↓Dᵢ | Dᵢ →c A}
```

Каждый ancestor впитал LCA нижних границ всех потомков.

---

## Фаза 4: Push

**Цель**: для каждого `D →c A` передать верхнюю границу A в D.

**Порядок**: по **обратному** toposort (предки раньше потомков). Для каждого узла: сначала рекурсивно Push компонентов (cycle guard для рекурсивных типов), затем обработка constraint edges.

### Определение

```
Push(A, D) — обновить D, учитывая D ≤ A
```

### Правила

Для каждого constraint edge `D →c A`:

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive | Primitive | Проверка `D ≤ A` (FitsInto). |
| Primitive | CS | `D.Anc := GCD(D.Anc, A)`. Проверка: `D.Desc ≤ D.Anc`. |
| Primitive | Composite | Нет действия (`D ≤ Any` тривиально). |
| CS | Primitive | Если `A.Anc ≠ ∅`: проверка `D ≤ A.Anc`. Если `A.Anc = ∅`: пропуск (нет верхней границы). |
| CS | CS | Если `A.Anc ≠ ∅`: `D.Anc := GCD(D.Anc, A.Anc)`. Если `A.Anc = ∅`: пропуск. `D.cmp := D.cmp ∨ A.cmp`. |
| CS(Desc=Struct) | Composite(Struct) | Поле-ограничения из CS.Desc передаются в D по полям через Push. |
| CS(Anc≠∅,≠Any) | Composite | **Ошибка**: composite не подтип конкретного примитива. |
| Composite | CS | Структурная унификация D → форма A, затем декомпозиция + рекурсивный Push для компонент. |
| Composite | Composite(same) | Структурная декомпозиция → рекурсивный Push для каждой пары компонент. |
| Opt(E) | Primitive(non-None) | Implicit lift (unwrap): `D ≤ E`, рекурсивный Push. |
| Opt(E) | None | Нет действия. |
| Opt(E) | CS(IsOptional) | Структурная унификация D → Opt(inner), `inner ≤ E`, рекурсивный Push. |
| non-Opt Comp | CS(IsOptional) | Implicit lift (wrap): D := Opt(inner), `inner ≤ A`, рекурсивный Push. |
| non-Opt Comp(F) | Opt(F) (same, A inferred) | Optional wrapping + рекурсивный Push `inner ≤ D.elem`. |
| non-Opt Comp(F) | Opt(F) (same, A shape-rigid) | **Ошибка**: signature-given shape не может быть расширена. |
| non-Opt Comp | Opt(different constructor) | **Ошибка**: разные конструкторы несовместимы. |
| Composite | Composite(diff, non-Opt) | **Ошибка**: разные конструкторы несовместимы. |

### Отличие от Pull

Pull вычисляет `A.Desc := LCA(...)`. Push вычисляет `D.Anc := GCD(...)` и **рекурсивно проталкивает** ограничения в компоненты через декомпозицию. Pull устанавливает composite-структуру узлов, Push наполняет её верхними границами.

**Push не передаёт Desc**: нижние границы двигаются только вверх (Pull). Верхние границы двигаются только вниз (Push). Если ancestor A имеет только Desc без Anc — Push ничего не передаёт (пропуск).

### Результат Push

```
∀ узел D:  D.Anc ⊆ GCD{↑Aⱼ | D →c Aⱼ}
```

Каждый потомок впитал GCD верхних границ всех предков.

---

## Результат Pull + Push

### Теорема (минимальный интервал)

После обеих фаз Pull (None absorption + основной проход) и одного прохода Push, каждый узел N с состоянием `[D..A]` удовлетворяет:

```
D = LCA{↓Dᵢ | Dᵢ →c N}     (LCA нижних границ потомков)
A = GCD{↑Aⱼ | N →c Aⱼ}     (GCD верхних границ предков)
D ≤ A                         (непустой интервал, иначе ошибка типов)
```

Любой тип T ∈ [D..A] — валидное решение: все потомки конвертируются в T, T конвертируется во все предки.

### Идемпотентность

Pull+Push **идемпотентны**: повторный проход Pull не изменит ни одного Desc (LCA идемпотентен: `LCA(D, D) = D`). Повторный проход Push не изменит ни одного Anc (GCD идемпотентен). Следствие: один проход оптимален.

### Зачем нужен Destruction

Pull+Push вычисляют **интервал**, не выбирают тип. Destruction+Finalize выполняют **резолюцию** — см. `TicAlgorithm_Destruction.md`.

**Граница Pull/Push и Destruction**: узлы с `IsOptional=true` остаются ConstraintsState после Pull+Push (IsOptional — флаг на CS, не composite-состояние). Materialization `CS(IsOptional) → StateOptional` происходит в Destruction. Optional wrapping **composites** (Composite → Opt(Composite)) происходит в Pull.

---

## Сходимость

### Утверждение

Pull и Push завершаются за **один проход** каждый.

### Мера завершения

Определим потенциал графа:

```
Φ(G) = |E_c| + |E_comp|
```

где `E_c` — множество constraint edges, `E_comp` — множество component links **в финальном графе** (после всех структурных унификаций).

`Φ(G)` конечен (ограничен глубиной AST: каждый узел AST порождает O(1) рёбер, каждая структурная унификация заменяет 1 constraint edge на O(k) component links + constraint edges, где k — арность composite, k ≤ |AST|).

### Доказательство

**Pull**: обход по toposort. Для каждого узла D:
1. Рекурсивный Pull компонентов D (по component links) — обрабатывает ≤ |comp_links(D)| рёбер.
2. Для каждого ancestor-ребра `D →c A`: применяется одно правило из таблицы.
   - Если правило алгебраическое (LCA/FitsInto): O(1) работы, ребро обработано.
   - Если структурная унификация: ребро `D →c A` заменяется на k компонентных рёбер. Каждое обрабатывается рекурсивно. Суммарно: k рёбер вместо 1, но суммарный потенциал Φ не растёт (замена 1-на-k учтена в Φ).
   - Если Optional wrapping: создаётся inner-узел + 1 component link + 1 constraint edge. Каждый composite-узел wraps не более одного раза (необратимо). Суммарно: O(|composite_nodes|) дополнительных рёбер, ограничено O(|AST|).
3. При обработке узла D все потомки D **уже финальны** (обработаны раньше в toposort).

Каждое ребро из Φ(G) обрабатывается **ровно один раз**. Итого: O(Φ(G)).

**Push**: обход в обратном toposort. Аналогичный аргумент; каждое ребро обработано один раз. Рекурсивный Push в компоненты обрабатывает component links, каждый — один раз.

**Инвариант структурной унификации**: новые компоненты C'ᵢ расположены **раньше** узла A в toposort (компоненты composite всегда раньше parent). Поэтому при Pull (toposort order) они уже обработаны к моменту когда A встречается. При Push (reverse toposort) — обрабатываются рекурсивно в том же вызове.

**Монотонность**:
- Pull: Desc только растёт (LCA = join, монотонен). `∅ ≤ LCA(∅, T) = T ≤ LCA(T, T') = T∨T'`.
- Push: Anc только убывает (GCD = meet, монотонен). `∅ ≥ GCD(∅, T) = T ≥ GCD(T, T') = T∧T'`.
- Структурная унификация необратима (CS → Composite, обратное невозможно).
- Интервал `[D..A]` только **сужается**.

**Сложность**: O(Φ(G)) = O(|AST|).

### Достаточные свойства системы типов

| Свойство | Зачем |
|----------|-------|
| **Конечная высота решётки** (ACC) | LCA/GCD вычисляются за конечное число шагов |
| **Тотальность LCA** (Any — top) | ∨ определён для любой пары типов |
| **Монотонность LCA и GCD** | Нет осцилляций; идемпотентность; однопроходность |
| **Конечная глубина вложенности типов** | Рекурсивная декомпозиция завершается |
| **DAG constraint graph** (после toposort) | Однопроходный обход без циклических зависимостей |

Свойства 1-3 — свойства **системы типов**. Свойства 4-5 — свойства **программ** (конечное AST порождает конечный граф; циклы устраняются toposort-ом).

Вариантность composites (ковариантность/контравариантность) — не отдельное свойство, а часть определения подтипирования для каждого конструктора (см. `TicTypeSystem.md`).

---

## Пример

Выражение: `y = if(x > 0) x else 1`

### Build

Оператор `>` имеет сигнатуру `(T,T) → Bool` где T:[U8..Real, cmp]. Инстанциация создаёт свежий узел T'.

```
Узлы и начальные состояния:
  x:  [∅..∅]
  0:  [U8..Real, pref=I32]
  1:  [U8..Real, pref=I32]
  T': [U8..Real, cmp]          ← свежий из сигнатуры >
  R:  [∅..∅]                    ← результат if-else
  y:  [∅..∅]

Constraint edges:
  x  →c T'    (аргумент >)
  0  →c T'    (аргумент >)
  x  →c R     (ветка if)
  1  →c R     (ветка else)
  R  →c y     (определение)
```

None-узлов нет → Pull в один проход.

### Pull (порядок: 0, 1, x, T', R, y)

```
Обработка 0: ancestor T'. Pull(T', 0):
  T'.Desc := LCA(U8, U8) = U8.  T' → [U8..Real, cmp]  (без изменений)

Обработка 1: ancestor R. Pull(R, 1):
  R.Desc := LCA(∅, U8) = U8.    R → [U8..∅]

Обработка x: ancestors T', R.
  Pull(T', x):  T'.Desc := LCA(U8, ∅) = U8  (без изменений, ∅ нейтрален)
  Pull(R, x):   R.Desc  := LCA(U8, ∅) = U8  (без изменений)

Обработка T': ancestor нет (T' — внутренний узел оператора)

Обработка R: ancestor y. Pull(y, R):
  y.Desc := LCA(∅, U8) = U8.    y → [U8..∅]
```

### Push (порядок: y, R, T', x, 1, 0)

```
Обработка y: ancestors нет → пропуск

Обработка R: ancestor y.
  Push(y, R): y.Anc = ∅ → пропуск (нет верхней границы)

Обработка T': ancestors нет → пропуск

Обработка x: ancestors T', R.
  Push(T', x): ↑T' = Real. x.Anc := GCD(∅, Real) = Real. cmp от T'.  x → [∅..Real, cmp]
  Push(R, x):  ↑R = ∅ → пропуск

Обработка 1: ancestor R.
  Push(R, 1): ↑R = ∅ → пропуск.  1 → [U8..Real, pref=I32] (без изменений)

Обработка 0: ancestor T'.
  Push(T', 0): ↑T' = Real. 0.Anc := GCD(Real, Real) = Real. (без изменений)
```

### Результат (интервалы после Pull+Push)

```
x:  [∅..Real, cmp]
0:  [U8..Real, pref=I32]
1:  [U8..Real, pref=I32]
T': [U8..Real, cmp]
R:  [U8..∅]
y:  [U8..∅]
```

Destruction (см. `TicAlgorithm_Destruction.md`) выберет конкретные типы из интервалов.

---

## Пример 2: Optional (двухфазный Pull)

Выражение: `y = if(c) 42 else none`

### Build

```
Узлы:
  c:   [∅..∅]
  42:  [U8..Real, pref=I32]
  none: None
  R:   [∅..∅]                ← результат if-else
  y:   [∅..∅]

Constraint edges:
  42   →c R    (ветка if)
  none →c R    (ветка else)
  R    →c y    (определение)
```

None-узел есть → двухфазный Pull.

### Pull Phase 1 (только None)

```
none →c R: правило None absorption. R.IsOptional := true.
R → [∅..∅, opt]
```

### Pull Phase 2 (не-None, toposort: c, 42, R, y)

```
42 →c R: правило CS ← Primitive.
  R.Desc := LCA(∅, U8) = U8.  R → [U8..∅, opt]

R →c y: правило CS ← CS.
  y.Desc := LCA(∅, ↓R) = LCA(∅, U8) = U8.
  y.opt := false ∨ true = true.
  y → [U8..∅, opt]
```

### Push (reverse: y, R, 42, c)

```
Всё ∅ ancestors — пропуск (нет верхних границ).
```

### Результат (интервалы после Pull+Push)

```
42:  [U8..Real, pref=I32]
R:   [U8..∅, opt]
y:   [U8..∅, opt]
```

### Destruction (кратко)

Materialization (подфаза 5b): R и y имеют `IsOptional=true`.
`R: [U8..∅, opt] → Opt(inner)`, inner = `[U8..∅, pref=I32]` (Preferred от 42 через Pull).
Finalize: inner → I32 (Preferred в интервале). y → `Opt(I32)`.

**Итог: y = int?** (Opt(I32)). Значение: 42 → Opt(42), none → None.

---

## Связь с алгеброй

| Фаза | Оператор | Что вычисляет | Направление |
|------|----------|---------------|-------------|
| Pull | LCA (∨), Concretest (↓) | Desc = LCA{↓потомков} | desc → anc |
| Push | GCD (∧), Abstractest (↑) | Anc = GCD{↑предков} | anc → desc |
| Оба | FitsInto (≤) | Проверка совместимости | — |
| Оба | Структурная декомпозиция | Composite ≤ Composite → компоненты | рекурсивно |
| Toposort | Unify (⊓) | Слияние циклов | — |
