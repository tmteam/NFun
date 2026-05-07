# TIC Algebra — Система типовых ограничений

## Обзор

TIC Algebra — замкнутая система из шести операторов над типовыми ограничениями. Операторы работают на одном домене (типы + ConstraintsState) и связаны друг с другом точными соотношениями.

| Оператор | Символ | Арность | Тотальность | Вопрос |
|---|---|---|---|---|
| LCA | A ∨ B | бинарный | тотальный | Какой тип покроет оба? |
| GCD | A ∧ B | бинарный | частичный (null) | Какой тип влезет в оба? |
| FitsInto | A ≤ B | бинарный | bool | Подходит ли A для B? |
| Unify | A ⊓ B | бинарный | частичный (null) | Пересечение ограничений |
| Concretest | ↓A | унарный | тотальный | Нижняя граница |
| Abstractest | ↑A | унарный | тотальный | Верхняя граница |

## Домен

Все операторы работают над одним доменом — **описаниями типов**:

- **Primitive**: I32, Real, Bool, Char, None, Any, ...
- **Composite**: Array(T), Optional(T), Fun(A→R), Struct{f:T}
- **Constraints[D..A, opt, cmp, struct⊆S]**: интервал допустимых типов;
  опциональная структурная верхняя граница `StructBound S` (F-bounded).
  `S` может содержать `RefTo` обратно на сам Constraints — F-bound contractive
  только если каждое back-edge пересекает Optional/Array (см. PushReform.md
  §Contractivity invariant).
- **RefTo(N)**: ссылка (прозрачна — разыменовывается перед любым оператором)

Конкретный тип — частный случай описания: точечный интервал `[T..T]`.

## Фундаментальные соотношения

### Триада: LCA, GCD, FitsInto

Три оператора — три грани одного partial order:

```
A ≤ B   ⟺   A ∨ B = B   ⟺   A ∧ B = A     (когда A ∧ B определён)
```

Уточнение: эквивалентность `A ∧ B = A` имеет смысл только когда GCD определён (не null). Если `A ∧ B = null`, то A и B **несравнимы** (ни A ≤ B, ни B ≤ A).

### Алгебраическая структура

Типы с отношением ≤ образуют **join-semilattice с top** (Any):
- Каждая пара имеет join (LCA тотален)
- Есть наибольший элемент Any
- **НЕ** lattice: meet (GCD) существует не для всех пар (Bool ∧ I32 = null)
- **НЕ** bounded below: None — bottom только для optional-подсистемы, не глобальный

LCA — join в этой join-semilattice. GCD — **частичный** meet (определён когда общий подтип существует).

### Unify через LCA и GCD

Для ConstraintsState Unify **сводится** к LCA и GCD:

```
[D₁..A₁] ⊓ [D₂..A₂] = [D₁ ∨ D₂ .. A₁ ∧ A₂]
```

Нижняя граница пересечения — LCA нижних границ (поднимает). Верхняя — GCD верхних (опускает). Если результирующий интервал пуст — null.

Для конкретных типов: Unify = проверка совместимости (equality для примитивов, рекурсивный Unify для composites).

### Concretest и Abstractest — проекции

Унарные операторы извлекают границы из описания:

```
↓[D..A] = D     (нижняя граница)
↑[D..A] = A     (верхняя граница)
↓T = ↑T = T     (конкретный тип = точечный интервал)
```

LCA и GCD используют их для работы с ConstraintsState:

```
T ∨ CS = T ∨ ↓CS.Desc        (LCA с constraint → LCA с нижней границей)
CS ∧ T = ↑CS.Anc ∧ T         (GCD с constraint → GCD с верхней границей)
```

### Замкнутость

Операторы образуют **замкнутую систему**: результат каждого оператора — описание типа из того же домена. Нет "внешних" объектов.

```
A ∨ B : Type → Type                   (всегда в домене)
A ∧ B : Type → Type | null            (null = "пусто", не тип)
A ≤ B : Type → bool                   (предикат)
A ⊓ B : Type → Type | null
↓A    : Type → Type
↑A    : Type → Type
```

## Вариантность в composites

Все бинарные операторы обрабатывают composites **покомпонентно**, учитывая вариантность:

| Компонента | LCA (∨) | GCD (∧) | FitsInto (≤) | Unify (⊓) |
|---|---|---|---|---|
| **Ковариантная** | LCA | GCD | A ≤ B | Unify |
| **Контравариантная** | GCD | LCA | B ≤ A | Unify |

Unify игнорирует вариантность (проверяет совместимость, не порядок).

Concretest/Abstractest — дуальная пара: на контравариантных компонентах (аргументы функций) **меняются местами**.

Вариантность компонент:
- **Array**: элемент — ковариантен
- **Optional**: элемент — ковариантен
- **Fun**: аргументы — контравариантны, возврат — ковариантен
- **Struct**: поля — ковариантны

## Struct — особый composite

Struct отличается от Array/Optional/Fun тем, что **набор полей** — часть типа:

| Оператор | Что происходит с полями |
|---|---|
| LCA (∨) | **Пересечение** имён (меньше полей = общее) |
| GCD (∧) | **Объединение** имён (больше полей = конкретнее) |
| FitsInto (≤) | Подтип имеет **все поля** надтипа (и может иметь больше) |
| Unify (⊓) | **Точное совпадение** набора полей |

## Операторы на графе

Операторы применяются к **состояниям узлов** TIC Graph:

### На constraint edges (D ≤ A)

Constraint edge `D →constraint A` — это отношение `D.State ≤ A.State`. При решении графа операторы сужают состояния:

- **Из D в A** (вверх): `A.State = A.State ∨ ↓D.State` — предок узнаёт о потомке через LCA
- **Из A в D** (вниз): `D.State = D.State ∧ ↑A.State` — потомок узнаёт о предке через GCD
- **Проверка**: `D.State ≤ A.State` — FitsInto подтверждает что ограничение выполнено

### На component links

Component link `Parent →component Child` — структурная зависимость. Операторы на composites рекурсивно спускаются по component links:

- `Array(E₁) ∨ Array(E₂)` → рекурсивно `E₁.State ∨ E₂.State`
- `Struct{x:N₁} ⊓ Struct{x:N₂}` → рекурсивно `N₁.State ⊓ N₂.State`

### Пример на графе

Граф для `y = if(cond) x else 1`:

```
Узлы:           Состояния:              Constraint edges:
  cond          Constraints[∅..∅]
  x             Constraints[∅..∅]       x →constraint result
  1             Constraints[U8..Real]   1 →constraint result
  result        Constraints[∅..∅]       result →constraint y
  y             Constraints[∅..∅]
```

Применение операторов (снизу вверх по edges):
1. `result.State = result.State ∨ ↓x.State` → result получает нижнюю границу от x
2. `result.State = result.State ∨ ↓1.State` → result получает U8 как нижнюю границу
3. `y.State = y.State ∨ ↓result.State` → y получает ту же границу

## Инварианты системы

1. **Триада**: `A ≤ B ⟺ A ∨ B = B ⟺ A ∧ B = A`
2. **Unify = LCA + GCD**: `[D₁..A₁] ⊓ [D₂..A₂] = [D₁ ∨ D₂ .. A₁ ∧ A₂]`
3. **Проекции**: `↓[D..A] = D`, `↑[D..A] = A`, `↓T = ↑T = T`
4. **LCA + Concretest**: `T ∨ CS = T ∨ ↓CS`
5. **GCD + Abstractest**: `CS ∧ T = ↑CS ∧ T`
6. **Замкнутость**: результат оператора ∈ домен
7. **RefTo прозрачен**: для всех операторов
8. **Вариантность**: ковариантные компоненты — тот же оператор; контравариантные — дуальный
9. **Решённость**: composite решён ⟺ все компоненты решены
10. **Constraint edge**: D →constraint A ⟹ D.State ≤ A.State (после решения)

## Формальный статус

### Доказано (таблица)
- Примитивы: все свойства (коммутативность, ассоциативность, идемпотентность, монотонность, LUB) верифицированы исчерпывающим перебором таблицы 19×19
- Триада для конкретных типов: `A ≤ B ⟺ A ∨ B = B ⟺ A ∧ B = A`

### Доказано (структурная индукция)

**Ассоциативность LCA и GCD для composites** — взаимная индукция по глубине типа:
- База: примитивы (таблица)
- Шаг (одинаковый конструктор): по индукции для компонент
- Шаг (разные конструкторы): результат = Any → `(Any ∨ C) = Any = (A ∨ (Any))` тривиально
- Шаг (Optional lifting): `Opt(A) ∨ B = Opt(A ∨ B)`. Ассоциативность: `(Opt(A) ∨ B) ∨ C = Opt(A ∨ B) ∨ C = Opt((A ∨ B) ∨ C) = Opt(A ∨ (B ∨ C)) = Opt(A) ∨ (B ∨ C)` — по индукции для внутреннего LCA + `Opt(Any) = Any` как граничный случай
- Функции: LCA ассоциативность использует GCD для аргументов (контравариантно) и наоборот → взаимная индукция корректна (обе глубины < n)

**Монотонность**: `A ≤ B ⟹ A ∨ C ≤ B ∨ C` — следствие LUB property (в любой join-semilattice монотонность join следует из LUB).

**LCA — наименьшая верхняя грань**: `A ≤ C ∧ B ≤ C ⟹ A ∨ B ≤ C` — для composites с одинаковым конструктором по индукции; для разных конструкторов C = Any (единственный общий супертип, т.к. нет промежуточных абстрактных типов между конструкторами и Any — **аксиома иерархии**).

### Теоремы

**Confluence**: результат решения **не зависит** от порядка применения операторов.

Доказательство (ascending chain + Diamond property, НЕ Knaster-Tarski — KT требует complete lattice, у нас join-semilattice):
1. Каждый оператор **монотонно сужает** интервалы (inflationary в порядке сужения)
2. Домен конечен (конечное число примитивов, глубина ограничена выражением)
3. **Ascending chain condition**: монотонное сужение на конечном домене завершается за конечное число шагов
4. **Diamond property** (доказательство по случаям):
   - **Разные целевые узлы**: update(A) затрагивает T₁, update(B) затрагивает T₂, T₁ ≠ T₂ → тривиально коммутируют (модифицируют разные состояния)
   - **Один целевой узел из разных источников**: `(C.state ∨ ↓S₁) ∨ ↓S₂ = (C.state ∨ ↓S₂) ∨ ↓S₁` — по коммутативности и ассоциативности LCA. Аналогично для GCD: `(C.state ∧ ↑A₁) ∧ ↑A₂ = (C.state ∧ ↑A₂) ∧ ↑A₁`
   - **Каскадное распространение**: update(A) вызывает propagation к потомкам. Но каждый промежуточный шаг — применение LCA или GCD, которые идемпотентны и коммутативны. Порядок каскадов не влияет на конечный результат
5. Termination (3) + Diamond (4) → единственная нормальная форма (по лемме Ньюмана: терминирующая + локально конфлюэнтная система → конфлюэнтна)

**Soundness** (условная): если граф **решён** (все узлы имеют конкретные состояния, все constraint edges удовлетворены: `∀ edge D→A: D.State ≤ A.State`), то нет type errors при исполнении.

Condition: runtime conversions корректны (A ≤ B ⟹ значение A безопасно используется как B). Алгебраическая часть (constraint satisfaction) доказуема; runtime часть — аксиома реализации (верифицируется тестами).

Более сильная формулировка: **constraint satisfaction гарантирует что все требуемые conversions находятся в отношении ≤**. Корректность каждой отдельной conversion — per-conversion лемма.

### Theorem PT-F — Principal F-Bounded Type (Push reform extension)

Для unannotated параметра `n`, тело которого использует его в выражениях с вкладами `[Dᵢ..Aᵢ]`, `Sᵢ` (StructBound), `optᵢ`, `cmpᵢ`:

> **Principal type** = `[D ≤ A, opt, cmp, struct⊆S]` где
> - `D = ⋁ᵢ Dᵢ`  (LCA descendants — lattice join)
> - `A = ⋀ᵢ Aᵢ`  (GCD ancestors — lattice meet)
> - `S = GcdStruct(S₁,…,Sₖ)`  (field union — meet on F-bound lattice)
> - `opt = ⋁ᵢ optᵢ`, `cmp = ⋁ᵢ cmpᵢ`
>
> Интервал **non-empty** ⟺ `D ≤ A` И `D` совместим с `S` (если `S ≠ null`: либо `D = null`, либо `D` — struct с `Fields(D) ⊇ Fields(S)`). При non-empty principal type **уникален** с точностью до lattice equality.

`SimplifyOrNull` валидирует **three-way** non-emptiness `(D, A, S)`. F-bound — third independent dimension (peer to `IsComparable`, `IsOptional`); НЕ projection в `[D..A]`. Полное обоснование и operator extensions: `PushReform.md`.

**Decidability fragment**: ограничение F-bound covariance (Pierce 1992) даёт **Amadio–Cardelli equirecursive subtyping with width-subtypable records** — decidable в `O(n²)`. Без variance restriction — undecidable.

## Примитивные и производные операторы

**LCA (∨)** и **GCD (∧)** — примитивные операторы. Остальные четыре — производные:

| Оператор | Выражается через |
|---|---|
| FitsInto (≤) | `A ≤ B ⟺ A ∨ B = B` |
| Unify (⊓) на CS | `[D₁..A₁] ⊓ [D₂..A₂] = [D₁ ∨ D₂ .. A₁ ∧ A₂]` |
| Concretest (↓) | `↓[D..A] = D` (проекция) |
| Abstractest (↑) | `↑[D..A] = A` (проекция) |

Unify на конкретных типах = structural equality (не выражается через LCA/GCD — третий примитив).

Примечание: в литературе по type inference оператор Unify ближе всего к **constraint conjunction/merging** (Pottier 2001), а не к unification в смысле Robinson 1965.

### Постулаты типовой системы

Следующие утверждения — **определяющие постулаты** (аксиомы типовой иерархии). Из них выводятся все свойства операторов.

**Структура типов** (см. `TicGraph.md`):
- Any — top: ∀T: T ≤ Any
- Нет глобального bottom
- Примитивные типы: конечный partial order (таблица 19×19)
- Composites: Array(T), Optional(T), Fun(A→R), Struct{f:T}

**Optional**:
- `Opt(T) = T | None` (расширяет тип значением None)
- `Opt(Opt(T)) = Opt(T)` — идемпотентность (невложенный)
- `T ≤ Opt(T)` — implicit lift
- `None ≤ Opt(T)`, но `None ≰ T` для конкретных T

**Вариантность composites**:
- Array, Optional: ковариантны
- Fun args: контравариантны; Fun return: ковариантен
- Struct fields: ковариантны + width subtyping (больше полей = подтип)

**Иерархия конструкторов**:
- F\<...\> ∨ G\<...\> = Any для разных конструкторов (нет промежуточных абстрактных типов)

**Следствие**: `Opt(Any) = Any` (из `Opt(T) = T | None`, `None ⊆ Any`, идемпотентности).

Все свойства операторов (ассоциативность, коммутативность, LUB) — **теоремы**, доказываемые из этих постулатов.

### Открытый вопрос
- **Единый домен**: D = Primitive ∪ Composite ∪ Constraints ∪ RefTo. Чистая формализация: **type algebra** T(Σ) (конкретные типы с ≤) отдельно от **constraint domain** C (интервалы над T). LCA/GCD/FitsInto работают в T. Unify/Concretest/Abstractest работают в C, определяясь через операторы T. Это стандартная декомпозиция (cf. Pottier 2001, HM(X)).

## Row Polymorphism в AddDescendant

`ConstraintsState.AddDescendant` комбинирует нижние границы. Для struct-on-struct:

- **Оба closed** (литералы, LCA-результаты): `LCA({a,b}, {a,c}) = {a}` — пересечение полей (стандартный join)
- **Хотя бы один open** (от SetFieldAccess): `Union({a,...}, {b,...}) = {a,b,...}` — объединение полей

Это следует из семантики: open struct `{a,...}` — constraint "имеет минимум поле a". Комбинация двух constraints: "имеет a" И "имеет b" = "имеет a и b". Это GCD/meet в решётке constraints, но field UNION в множестве полей.

Для closed struct: два конкретных значения `{a,b}` и `{a,c}` нуждаются в общем супертипе = LCA = пересечение полей = `{a}`.

**Реализация**: `ConstraintsState.UnionStructFields` — создаёт НОВЫЙ struct со всеми полями из обоих входов. Для shared полей сохраняет existing node. НЕ мутирует входные struct (они могут быть shared в графе).

## Файлы

| Документ | Содержание |
|---|---|
| [TicGraph.md](TicGraph.md) | Граф: узлы, состояния, рёбра |
| [Algebra_LCA.md](Algebra_LCA.md) | A ∨ B — join |
| [Algebra_GCD.md](Algebra_GCD.md) | A ∧ B — meet |
| [Algebra_Fit.md](Algebra_Fit.md) | A ≤ B — partial order |
| [Algebra_Unify.md](Algebra_Unify.md) | A ⊓ B — constraint intersection |
| [Algebra_Concretest.md](Algebra_Concretest.md) | ↓A — нижняя граница |
| [Algebra_Abstractest.md](Algebra_Abstractest.md) | ↑A — верхняя граница |
