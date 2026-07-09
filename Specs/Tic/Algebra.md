# TIC Algebra — Система типовых ограничений

## Обзор: два слоя

Типовая система TIC разделена на два слоя с явным контрактом между ними:

- **АЛГЕБРА** — чистые операторы над описаниями типов. Тотальны на заявленном домене
  (частичные возвращают null как маркер противоречия, не как «исключительный случай»).
  Не принимают резолюционных решений: Preferred они только **транспортируют** по
  явным правилам (см. §Домен), но никогда не выбирают.
- **РЕЗОЛЮЦИЯ** — выбор конкретного типа из допустимого множества:
  `SolveCovariant` / `SolveContravariant`, политика Preferred, материализация
  abstract-midpoint (I48→I64, U24→U32, ...). Специфицирована в
  [TicPreferred.md](TicPreferred.md) §5–6 и [TicAlgorithm_Destruction.md](TicAlgorithm_Destruction.md)
  §Фаза 6 (Finalize) + §5b (Materialization IsOptional). Этот документ на неё только
  ссылается и фиксирует контракт слоя — правила резолюции здесь не дублируются.

**Контракт слоёв**: алгебра вычисляет и сужает множества допустимых типов; резолюция
выбирает точку из уже вычисленного множества и не может выйти за его пределы
(`результат Fit исходного CS` — контракт Finalize). Любая ветка алгебраического
оператора, которая «подглядывает» в Preferred для выбора, — утечка слоя резолюции
(текущие утечки — в §Отклонения).

### Инвентарь алгебры

**Бинарные операторы:**

| Оператор | Символ | Тотальность | Вопрос |
|---|---|---|---|
| LCA | A ∨ B | тотальный | Какой тип покроет оба? |
| GCD | A ∧ B | частичный (null) | Какой тип влезет в оба? |
| Merge | A ⊓ B | частичный (null) | Пересечение ограничений. `Unify(CS,CS) ≡ Merge` |

**Предикаты:**

| Отношение | Символ | Вопрос |
|---|---|---|
| FitsInto | A ≤ B | Подходит ли A туда, где ожидается B? |
| Convert (оптимистичное) | A ≤c⁺ B | Существует ли резолюция, при которой A конвертируется в B? |
| Convert (пессимистичное) | A ≤c⁻ B | Конвертируется ли A в B при **любой** резолюции? |

**Унарные операторы:**

| Оператор | Символ | Тотальность | Вопрос |
|---|---|---|---|
| Concretest | ↓A | тотальный | Чистая проекция на нижнюю границу |
| Abstractest | ↑A | тотальный | Чистая проекция на верхнюю границу |
| Simplify | simp(A) | частичный (null) | Канонизация состояния; null = противоречие |

**Слой резолюции** (не алгебра — по ссылке): `SolveCovariant`, `SolveContravariant`,
`ConcretestSnapshot` (↓ₛ — Destruction-снапшот: Preferred-сохраняющий вариант ↓ для
состояний, материализуемых в граф; ↓ₛ ≡ ↓ на hint-free), политика Preferred,
abstract-midpoint. См. [TicPreferred.md](TicPreferred.md) §5.4–6,
[TicAlgorithm_Destruction.md](TicAlgorithm_Destruction.md) §Фаза 6.

## Домен

Два сорта объектов (стандартная декомпозиция HM(X), Pottier 2001):

- **T — решённые типы**: Primitive (I32, Real, Bool, ...), Composite
  (Array(T), Optional(T), Fun(A→R), Struct{f:T}) с решёнными компонентами.
- **C — ограничения**: `ConstraintsState` — кортеж

```
[D..A] + IsComparable (cmp) + IsOptional (opt) + StructBound S + Preferred P
```

- **RefTo(N)** — ссылка; прозрачна для всех операторов (разыменовывается до применения).

Конкретный тип — частный случай описания: точечный интервал `[T..T]`.

**Факты домена** (по построению, не по соглашению):

- **A ∈ Primitive**. Верхняя граница интервала — всегда примитив: `TryAddAncestor`
  комбинирует границы через GCD примитивов. Это причина двух архитектурных свойств:
  ↑ не нуждается в рекурсии (см. `Algebra_Abstractest.md`), а структурные верхние
  ограничения живут на **отдельной оси S** (F-bound), не в A-слоте.
- **D ∈ любое состояние** — примитив, композит, вложенный CS. Поэтому ↓ рекурсивен.
- **P (Preferred) ∈ Primitive ∪ {∅}** — метаданные резолюции. Не «инертен»: каждый
  оператор имеет явное правило транспорта (таблица ниже), но выбор по P — только в
  слое резолюции.
- **S ∈ Struct ∪ {∅}** — F-bounded верхняя граница `T <: τ(T)`; третье независимое
  измерение, peer к cmp/opt (см. PushReform.md и теорему PT-F).

### Транспорт осей через бинарные операторы

Семантика через **множества сатисфаеров** `Sat(C) = {T : T Fit C}`:
LCA — наименьшее ограничение, покрывающее `Sat(C₁) ∪ Sat(C₂)` → остаются только
общие обязательства; Merge — ограничение для `Sat(C₁) ∩ Sat(C₂)` → обязательства
складываются.

| Ось | LCA (∨) | GCD (∧) | Merge (⊓) |
|---|---|---|---|
| cmp | cmp₁ ∧ cmp₂ | сбрасывается (результат — верхняя граница) | cmp₁ ∨ cmp₂ |
| opt | opt₁ ∨ opt₂ | сбрасывается (парная корректность с Destruction, см. `Algebra_Abstractest.md`) | opt₁ ∨ opt₂ |
| S | S₁ ∩ S₂ (пересечение полей) | S₁ ∪ S₂ (объединение полей) | S₁ ∪ S₂ (`GcdBound`, как в PT-F) |
| P | оба ∅ → ∅; один → он; различаются → P₁ ∨ P₂ если ≠ Any, иначе ∅ | сбрасывается | равны → P; один задан → он; различаются → P₁ ∨ P₂ (∅ при LCA = Any); пост-условие — P Fit результата, иначе ∅ (коммутативно, `PreferredHintLcaOrNull` — тот же хелпер, что у ∨; см. `Algebra_Merge.md`) |

Правило `opt` в LCA — дизъюнкция, `cmp` — конъюнкция: opt **расширяет** множество
значений (None входит в объединение), cmp **сужает** (обязательство пропадает из
объединения, если его нёс только один операнд).

Транспорт S реализован внутри операторов (закрытие debt #12). Граница слоёв —
одно правило: **операторы переносят S по значению (ownerless `GcdBound`
сохраняет node identity self-referential позиций), перенос владения back-edges
остаётся за стадиями** — merge-вызыватели алиасят старых владельцев на
merged-узел (`loser := RefTo(winner)`), Pull/Push зовут `GcdBound` с
owner-узлами и делают rewire. μ-позиции в пересечении ∨ выпадают (sound
weakening: чистый join не может именовать совместную рекурсивную переменную).
Законы: `AlgebraStructBoundLawsTest`.

## Фундаментальные соотношения

### Триада: LCA, GCD, FitsInto

На **T** (решённые типы) три оператора — три грани одного partial order:

```
A ≤ B   ⟺   A ∨ B = B   ⟺   A ∧ B = A     (когда A ∧ B определён)
```

Уточнение: эквивалентность `A ∧ B = A` имеет смысл только когда GCD определён (не null).
Если `A ∧ B = null`, то A и B **несравнимы** (ни A ≤ B, ни B ≤ A).

На **C** триада дословно не выполняется: `≤` для CS — это Fit («влезает ли тип в
интервал»), несимметричный вопрос; см. `Algebra_Fit.md` §2.

### Алгебраическая структура

Типы с отношением ≤ образуют **join-semilattice с top** (Any):
- Каждая пара имеет join (LCA тотален)
- Есть наибольший элемент Any
- **НЕ** lattice: meet (GCD) существует не для всех пар (Bool ∧ I32 = null)
- **НЕ** bounded below: None — bottom только для optional-подсистемы, не глобальный

Антисимметричность ≤ выполняется **по модулю отождествления `Opt(Any) = Any`**
(см. `Algebra_CanonicalForms.md`).

### Merge через LCA и GCD

Для ограничений Merge **сводится** к LCA и GCD:

```
[D₁..A₁] ⊓ [D₂..A₂] = [D₁ ∨ D₂ .. A₁ ∧ A₂]
```

Нижняя граница пересечения — LCA нижних границ (поднимает). Верхняя — GCD верхних
(опускает). Если результирующий интервал пуст — null. Оси cmp/opt/S/P — по таблице
транспорта выше.

Для решённых типов Merge = проверка структурной совместимости (equality для
примитивов, рекурсивно для composites) — третий примитив, не выражается через ∨/∧.
Полные правила: C-часть — `Algebra_Merge.md`, T-часть — `Algebra_Unify.md`.

### Convert-отношения ≤c⁺ и ≤c⁻

Вспомогательные именованные отношения алгебры (не отдельный слой):

- **≤c⁻ (пессимистичное)**: `A ≤c⁻ B` — значение A конвертируется в B при **любой**
  резолюции незакрытых границ. На примитивах совпадает с ≤ (таблица конверсий —
  та же таблица порядка). Используется для проверок, которые нельзя будет откатить.
- **≤c⁺ (оптимистичное)**: `A ≤c⁺ B` — **существует** резолюция, при которой A
  конвертируется в B. Используется в Pull/Push, пока границы ещё уточняются:
  отклонять можно только то, что не спасёт никакая резолюция.

Законы:

```
≤c⁻  ⊆  ≤c⁺                       (пессимизм строже)
на Primitive × Primitive: ≤c⁻ = ≤
T ≤ Opt(T) ⟹ T ≤c⁻ Opt(T)       (implicit lift виден обоим)
```

Interval-проверки Fit (`CanBeFitConverted`) и `SimplifyOrNull` выражаются через эти
отношения — см. `Algebra_Fit.md` §CanBeFitConverted.

### Concretest и Abstractest — чистые проекции

В целевой системе ↓ и ↑ — **чистые проекции решётки**, определяемые правилами, а не
производные от LCA/GCD:

```
↑[D..A] = A          (A ∈ Primitive — рекурсия не нужна)
↓[D..A] = ↓D         (рекурсивно: D — любое состояние)
↓T = ↑T = T          (решённый тип — точечный интервал)
```

Плюс канонические Optional-ветки (Rule B, `Algebra_CanonicalForms.md`) и
дуальность на контравариантных позициях (см. §Вариантность). Полные правила:
`Algebra_Concretest.md`, `Algebra_Abstractest.md`.

**Теорема (производность на чистом фрагменте)**. На Preferred-свободном,
non-optional фрагменте C проекции согласованы с порядком как экстремумы множества
сатисфаеров: `↑CS` — наибольший примитив в `Sat(CS)`, `↓CS` — наименьшее выразимое
состояние. В этом смысле ↓/↑ *выводимы* из ≤ — но это теорема о чистом фрагменте
(UNVERIFIED формальной проверкой), а **не определение**: определение — правила выше.
Реализация ↓ чиста (Preferred-ветки вынесены в ↓ₛ — слой резолюции, долг #19).

### Замкнутость

Операторы образуют **замкнутую систему** над доменом `T ∪ C` — с учётом полного
инвентаря (3 бинарных + 3 предиката + 3 унарных):

```
A ∨ B   : (T∪C)² → T∪C                 (всегда в домене)
A ∧ B   : (T∪C)² → T∪C | null          (null = «пусто», не тип)
A ⊓ B   : (T∪C)² → T∪C | null
A ≤ B, A ≤c⁺ B, A ≤c⁻ B : (T∪C)² → bool   (предикаты)
↓A, ↑A  : T∪C → T∪C
simp(A) : C → T∪C | null
```

Нет «внешних» объектов; null — маркер противоречия. Слой резолюции тоже замкнут
относительно домена (`Solve* : C → T∪C`), но живёт вне алгебры.

## Вариантность в composites

Все бинарные операторы обрабатывают composites **покомпонентно**, учитывая вариантность:

| Компонента | LCA (∨) | GCD (∧) | FitsInto (≤) | Merge (⊓) |
|---|---|---|---|---|
| **Ковариантная** | LCA | GCD | A ≤ B | Merge |
| **Контравариантная** | GCD | LCA | B ≤ A | Merge |

Merge игнорирует вариантность (проверяет совместимость, не порядок).

Concretest/Abstractest — дуальная пара: на контравариантных компонентах (аргументы
функций) **меняются местами**: `↓Fun(A→R) = (↑A)→(↓R)` и наоборот.

Вариантность компонент:
- **Array**: элемент — ковариантен
- **Optional**: элемент — ковариантен
- **Fun**: аргументы — контравариантны, возврат — ковариантен
- **Struct**: поля — ковариантны

## Struct — особый composite

Struct отличается от Array/Optional/Fun тем, что **набор полей** — часть типа.
Сравнение всех операторов на одном месте (правила — в per-operator файлах):

| Оператор | Что происходит с полями |
|---|---|
| LCA (∨) | **Пересечение** имён (меньше полей = общее) |
| GCD (∧) | **Объединение** имён (больше полей = конкретнее) |
| FitsInto (≤) | Подтип имеет **все поля** надтипа (и может иметь больше) |
| Merge (⊓) | **Точное совпадение** набора полей |
| ↓ / ↑ | **Identity** (поля — узлы графа; см. `Algebra_Concretest.md` §Struct) |

## Операторы на графе

Операторы применяются к **состояниям узлов** TIC Graph:

### На constraint edges (D ≤ A)

Constraint edge `D →constraint A` — это отношение `D.State ≤ A.State`. При решении
графа операторы сужают состояния:

- **Из D в A** (вверх): `A.State = A.State ∨ ↓D.State` — предок узнаёт о потомке через LCA
- **Из A в D** (вниз): `D.State = D.State ∧ ↑A.State` — потомок узнаёт о предке через GCD
- **Проверка**: `D.State ≤ A.State` — FitsInto подтверждает что ограничение выполнено

### На component links

Component link `Parent →component Child` — структурная зависимость. Операторы на
composites рекурсивно спускаются по component links:

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

Каждый закон снабжён областью действия и верификацией (имя unit-теста или UNVERIFIED).

| # | Закон | Домен | Верификация |
|---|---|---|---|
| 1 | Триада: `A ≤ B ⟺ A ∨ B = B ⟺ A ∧ B = A` | T | `Lca_FitsInto_Relationship_Primitives/_Composites`, `Gcd_FitsInto_Relationship_Primitives/_Composites` |
| 2 | Merge-интервал: `[D₁..A₁] ⊓ [D₂..A₂] = [D₁∨D₂ .. A₁∧A₂]` | C | `Unify_ConstrainsIntersection`, `Unify_DisjointConstrains` |
| 3 | Проекции: `↓[D..A] = ↓D`, `↑[D..A] = A`, `↓T = ↑T = T` | C без opt-флага (opt — по Rule B) | `MostConcreteStateTest.Constraint3/5`, `MostAbstractStateTest.Constraint3` |
| 4 | LCA + Concretest: `T ∨ CS = T ∨ ↓CS` | C: non-optional, hint-free на любой глубине | `AlgebraLatticeLawsTest.Lca_ConcretestProjection_Law4` |
| 5 | GCD + верхняя граница: `[D..A] ∧ T = A ∧ T`; `[D..∅] ∧ T = T` | C | `AlgebraGcdFitLawsTest.Gcd_BoundedCs_EntersMeetThroughAncestorOnly`, `Gcd_NeutralCs_IsIdentity_OnSolvedTypes` (+ регрессия MR5Bug3 syntax-уровня) |
| 6 | Замкнутость: результат оператора ∈ домен | всё | по построению (типы сигнатур) |
| 7 | RefTo прозрачен для всех операторов | всё | покрыто косвенно всеми suite |
| 8 | Вариантность: ковариантные компоненты — тот же оператор; контравариантные — дуальный | T | `FitsInto_Fun_Contravariance` |
| 9 | Решённость: composite решён ⟺ все компоненты решены | T | по определению IsSolved |
| 10 | Constraint edge: `D →constraint A ⟹ D.State ≤ A.State` (после решения) | граф | syntax-suite (end-to-end) |
| 11 | Канонические формы сохраняются всеми операторами | C | `CanonicalOptionalFormTest` (см. `Algebra_CanonicalForms.md`) |
| 12 | Refinability: оператор не создаёт unsolved-состояний, недостижимых по рёбрам | C | `Concretest_ArrayOfEmptyOptionalCs_NoDeadOptIsland` |
| 13 | No ambient state: операторы алгебры не имеют ambient-состояния — коиндуктивный контекст (`AlgebraCycleContext`, Amadio–Cardelli assumption set) передаётся явным параметром, лениво создаётся цикло-способными ветками и протягивается через всё взаимно-рекурсивное семейство (Lca ↔ Gcd ↔ Unify/Merge ↔ GcdBound ↔ Fit) | всё | `AlgebraStructBoundLawsTest` (hostile-μ: `*_Terminates`) + по построению (в Algebra/ нет ThreadStatic) |

Замечания к области действия:
- Закон 4 не выполняется при opt-флаге (LCA переходит в flag-form ветку, а не в ↓CS)
  и при Preferred (транспорт хинта меняет форму результата, хотя не его интервал).
- Закон 5: при пустом A meet определяется **вторым операндом целиком** (`[D..∅] ∧ T = T`),
  а не `↑CS ∧ T` — иначе для T = CS ответ ошибочно схлопнулся бы до `↑T`
  (Abstractest — это направление supremum, не meet).

## Формальный статус

### Доказано (таблица)
- Примитивы: все свойства (коммутативность, ассоциативность, идемпотентность,
  монотонность, LUB) верифицируемы исчерпывающим перебором таблицы **23×23**
  (23 примитива, включая абстрактные I96/I48/I24/I12, U48/U24/U12/U4, F32 и None).
  Exhaustive-тесты перебирают полную 23-точечную решётку (`LcaTestTools.PrimitiveTypes`;
  `AlgebraLatticeLawsTest` — LUB/GLB-точность против независимого оракула порядка,
  монотонность, поглощение, null-propagation, ассоциативность).
- Триада для конкретных типов: `A ≤ B ⟺ A ∨ B = B ⟺ A ∧ B = A`

### Доказано (структурная индукция)

**Ассоциативность LCA и GCD для composites** — взаимная индукция по глубине типа:
- База: примитивы (таблица)
- Шаг (одинаковый конструктор): по индукции для компонент
- Шаг (разные конструкторы): результат = Any → `(Any ∨ C) = Any = (A ∨ (Any))` тривиально
- Шаг (Optional lifting): `Opt(A) ∨ B = Opt(A ∨ B)`. Ассоциативность:
  `(Opt(A) ∨ B) ∨ C = Opt(A ∨ B) ∨ C = Opt((A ∨ B) ∨ C) = Opt(A ∨ (B ∨ C)) = Opt(A) ∨ (B ∨ C)` —
  по индукции для внутреннего LCA + `Opt(Any) = Any` как граничный случай
- Функции: LCA ассоциативность использует GCD для аргументов (контравариантно) и
  наоборот → взаимная индукция корректна (обе глубины < n)

**Область действия**: индукция проведена на **чистом фрагменте** — Preferred-свободные
состояния. Транспорт Preferred в `LCA(CS,CS)` использует коллапс `P₁ ∨ P₂ = Any → ∅`
и в общем случае не ассоциативен.

**Монотонность**: `A ≤ B ⟹ A ∨ C ≤ B ∨ C` — следствие LUB property (в любой
join-semilattice монотонность join следует из LUB).

**LCA — наименьшая верхняя грань**: `A ≤ C ∧ B ≤ C ⟹ A ∨ B ≤ C` — для composites с
одинаковым конструктором по индукции; для разных конструкторов C = Any (единственный
общий супертип, т.к. нет промежуточных абстрактных типов между конструкторами и Any —
**аксиома иерархии**).

### Теоремы

**Confluence**: результат решения **не зависит** от порядка применения операторов.

Доказательство (ascending chain + Diamond property, НЕ Knaster-Tarski — KT требует
complete lattice, у нас join-semilattice):
1. Каждый оператор **монотонно сужает** интервалы (inflationary в порядке сужения)
2. Домен конечен (конечное число примитивов, глубина ограничена выражением)
3. **Ascending chain condition**: монотонное сужение на конечном домене завершается
   за конечное число шагов
4. **Diamond property** (доказательство по случаям):
   - **Разные целевые узлы**: update(A) затрагивает T₁, update(B) затрагивает T₂,
     T₁ ≠ T₂ → тривиально коммутируют (модифицируют разные состояния)
   - **Один целевой узел из разных источников**: `(C.state ∨ ↓S₁) ∨ ↓S₂ = (C.state ∨ ↓S₂) ∨ ↓S₁` —
     по коммутативности и ассоциативности LCA. Аналогично для GCD:
     `(C.state ∧ ↑A₁) ∧ ↑A₂ = (C.state ∧ ↑A₂) ∧ ↑A₁`
   - **Каскадное распространение**: update(A) вызывает propagation к потомкам. Но
     каждый промежуточный шаг — применение LCA или GCD, которые идемпотентны и
     коммутативны. Порядок каскадов не влияет на конечный результат
5. Termination (3) + Diamond (4) → единственная нормальная форма (по лемме Ньюмана:
   терминирующая + локально конфлюэнтная система → конфлюэнтна)

**Оговорка о домене**: предпосылки шага 4 (коммутативность/ассоциативность ∨ и ∧)
доказаны на чистом фрагменте — интервальная часть состояний `[D..A] + cmp + opt`.
Preferred не влияет на satisfiability (только на выбор точки в слое резолюции),
поэтому confluence утверждается для **интервальной части**; результирующий Preferred
может зависеть от порядка — это принятое свойство слоя резолюции
(см. TicPreferred.md §6, инварианты P1–P3).

**Soundness** (условная): если граф **решён** (все узлы имеют конкретные состояния,
все constraint edges удовлетворены: `∀ edge D→A: D.State ≤ A.State`), то нет type
errors при исполнении.

Condition: runtime conversions корректны (A ≤ B ⟹ значение A безопасно используется
как B). Алгебраическая часть (constraint satisfaction) доказуема; runtime часть —
аксиома реализации (верифицируется тестами).

Более сильная формулировка: **constraint satisfaction гарантирует что все требуемые
conversions находятся в отношении ≤**. Корректность каждой отдельной conversion —
per-conversion лемма.

### Theorem PT-F — Principal F-Bounded Type (Push reform extension)

Для unannotated параметра `n`, тело которого использует его в выражениях с вкладами
`[Dᵢ..Aᵢ]`, `Sᵢ` (StructBound), `optᵢ`, `cmpᵢ`:

> **Principal type** = `[D ≤ A, opt, cmp, struct⊆S]` где
> - `D = ⋁ᵢ Dᵢ`  (LCA descendants — lattice join)
> - `A = ⋀ᵢ Aᵢ`  (GCD ancestors — lattice meet)
> - `S = GcdStruct(S₁,…,Sₖ)`  (field union — meet on F-bound lattice)
> - `opt = ⋁ᵢ optᵢ`, `cmp = ⋁ᵢ cmpᵢ`
>
> Интервал **non-empty** ⟺ `D ≤ A` И `D` совместим с `S` (если `S ≠ null`: либо
> `D = null`, либо `D` — struct с `Fields(D) ⊇ Fields(S)`). При non-empty principal
> type **уникален** с точностью до lattice equality.

`SimplifyOrNull` валидирует **three-way** non-emptiness `(D, A, S)`. F-bound — third
independent dimension (peer to `IsComparable`, `IsOptional`); НЕ projection в `[D..A]`.
Полное обоснование и operator extensions: `PushReform.md`.

**Decidability fragment**: ограничение F-bound covariance (Pierce 1992) даёт
**Amadio–Cardelli equirecursive subtyping with width-subtypable records** — decidable
в `O(n²)`. Без variance restriction — undecidable.

## Постулаты типовой системы

Следующие утверждения — **определяющие постулаты** (аксиомы типовой иерархии). Из них
выводятся все свойства операторов.

**Структура типов** (см. `TicGraph.md`):
- Any — top: ∀T: T ≤ Any
- Нет глобального bottom
- Примитивные типы: конечный partial order (таблица 23×23)
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

Все свойства операторов (ассоциативность, коммутативность, LUB) — **теоремы**,
доказываемые из этих постулатов. Канонические формы, вытекающие из постулатов
Optional, — в `Algebra_CanonicalForms.md`.

## Row Polymorphism в AddDescendant

`ConstraintsState.AddDescendant` комбинирует нижние границы. Для struct-on-struct:

- **Оба closed** (литералы, LCA-результаты): `LCA({a,b}, {a,c}) = {a}` — пересечение
  полей (стандартный join)
- **Хотя бы один open** (от SetFieldAccess): `Union({a,...}, {b,...}) = {a,b,...}` —
  объединение полей

Это следует из семантики: open struct `{a,...}` — constraint «имеет минимум поле a».
Комбинация двух constraints: «имеет a» И «имеет b» = «имеет a и b». Это GCD/meet в
решётке constraints, но field UNION в множестве полей.

Для closed struct: два конкретных значения `{a,b}` и `{a,c}` нуждаются в общем
супертипе = LCA = пересечение полей = `{a}`.

**Реализация**: `ConstraintsState.UnionStructFields` — создаёт НОВЫЙ struct со всеми
полями из обоих входов. Для shared полей сохраняет existing node. НЕ мутирует входные
struct (они могут быть shared в графе).

## Файлы

| Документ | Содержание |
|---|---|
| [TicGraph.md](TicGraph.md) | Граф: узлы, состояния, рёбра |
| [Algebra_LCA.md](Algebra_LCA.md) | A ∨ B — join |
| [Algebra_GCD.md](Algebra_GCD.md) | A ∧ B — meet |
| [Algebra_Fit.md](Algebra_Fit.md) | A ≤ B — partial order; ≤c⁺/≤c⁻ |
| [Algebra_Merge.md](Algebra_Merge.md) | C₁ ⊓ C₂ — Merge на ConstraintsState (C-часть ⊓) |
| [Algebra_Unify.md](Algebra_Unify.md) | A ⊓ B — Unify: T-часть ⊓ (конкретные типы, composites) |
| [Algebra_Concretest.md](Algebra_Concretest.md) | ↓A — чистая проекция на нижнюю границу |
| [Algebra_Abstractest.md](Algebra_Abstractest.md) | ↑A — чистая проекция на верхнюю границу |
| [Algebra_CanonicalForms.md](Algebra_CanonicalForms.md) | Канонические формы: Rule B, Opt(Any)=Any, flatten |
| [TicPreferred.md](TicPreferred.md) | Слой резолюции: Preferred — источники, транспорт, выбор |
| [TicAlgorithm_Destruction.md](TicAlgorithm_Destruction.md) | Слой резолюции: Destruction §5b (materialization), Finalize §6 (Solve*) |
| [PushReform.md](PushReform.md) | F-bound (ось S): контрактивность, PT-F |

## Отклонения текущей реализации (см. TicTechnicalDebt)

1. ~~**Preferred-ветки в ↓ — утечка слоя резолюции.**~~ Закрыто (долг #19, ↓-часть):
   обе ветки (выбор `Opt(Preferred)` и `ConcretestArrayElement`) вынесены в
   snapshot-оператор `ConcretestSnapshot` (↓ₛ, `StateExtensions.ConcretestSnapshot.cs`)
   слоя резолюции; ↓ — чистая проекция. Потребители ↓ₛ: `AddDescendant`-снапшоты и
   односторонние desc-проекции `LCA(CS,CS)`. Пины: `ConcretestSnapshotTest`.
   Остаток #19 — Sat/форм-меняющий коллапс в ⊓ (см. TicTechnicalDebt #19/#22).
2. ~~**Ось S сбрасывается бинарными операторами.**~~ Закрыто 2026-07-09 (#12):
   `∨ → S₁∩S₂`, `∧/⊓ → S₁∪S₂` реализованы внутри операторов (ownerless
   `GcdBound`; three-way (D,A,S) непустота в ⊓; S-discharge-гейт коллапса).
   Законы — `AlgebraStructBoundLawsTest`.
3. ~~**Две параллельные реализации ⊓.**~~ Закрыто 2026-07-09 (#13/#14):
   `UnifyOrNull(CS,CS) ≝ MergeOrNull`, P-транспорт коммутативен
   (`PreferredHintLcaOrNull`). Попутно опровергнута ассоциативность ⊓
   (TicTechnicalDebt #22, `AlgebraMergeUnifyLawsTest`).
4. ~~**Testing gap таблицы примитивов.**~~ Закрыто 2026-07-09 (#20):
   `LcaTestTools.PrimitiveTypes` расширен до полных 23 точек; property-законы
   (LUB/GLB-точность, монотонность, поглощение, null-propagation, Ref-прозрачность,
   ассоциативность) — `AlgebraLatticeLawsTest`. Остаток #20 — монотонность
   проекций ↓/↑ (см. TicTechnicalDebt #20).
