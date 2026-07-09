# Concretest — Чистая проекция на нижнюю границу

## Определение

**Concretest(A)**, обозначение **↓A** — наиболее конкретное (узкое) состояние,
которое описание A допускает.

↓ — **чистая проекция решётки**: тотальный унарный оператор алгебры, без
резолюционных решений. Preferred он **транспортирует** (сохраняет в
CS-результатах), но никогда не **выбирает** — выбор по хинту принадлежит слою
резолюции (`SolveContravariant`, TicPreferred.md §5). Бывшие Preferred-ветки
реализации вынесены (долг #19, закрыт в ↓-части) в **снапшот-оператор**
`ConcretestSnapshot` (↓ₛ) слоя резолюции — он сохраняет/выбирает Preferred для
Destruction-снапшотов и используется там, где результат материализуется в хранимое
состояние графа (`AddDescendant`, односторонние desc-проекции LCA). На
Preferred-свободных состояниях ↓ₛ ≡ ↓. См. TicPreferred.md §5.4 и
`ConcretestSnapshotTest`.

Дуален к Abstractest (см. `Algebra_Abstractest.md`); на контравариантных позициях
они меняются местами.

## Интуиция

Для ConstraintsState `[D..A]`: ↓ проецирует на нижнюю границу — **рекурсивно**,
потому что D (в отличие от A) может быть любым состоянием, включая композит с
вложенными CS (факт домена, см. `Algebra.md` §Домен).

Для решённого типа T: ↓T = T (точечный интервал `[T..T]`).

## Правила (нормативные, целевая система)

### Примитивы

```
↓P = P
```

### ConstraintsState — non-optional

```
↓[D..A]        = ↓D              (рекурсивно; внешний A отбрасывается)
↓[∅..A, cmp]   = [∅..∅, cmp]     (comparable-обязательство сохраняется как CS)
↓[∅..∅]        = [∅..∅]          (identity на пустом CS)
```

Рекурсивность первого правила существенна: `↓[arr([U24..Real])..Any] = arr(U24)` —
проекция протаскивается сквозь композитную нижнюю границу
(тест `MostConcreteStateTest.Constraint5`).

**`S` (StructBound) НЕ участвует в Concretest.** F-bound — **верхняя** граница
(upper bound), не нижняя. Подставлять `S` как default lower сломало бы инвариант
`T ∨ CS = T ∨ ↓CS`: для `T = primitive` и `CS = [∅..∅, S=struct{...}]` получили бы
`T ∨ struct = Any`, отравляя последующие LCA. F-bound ортогонален интервалу `[D..A]`
и доступен только через отдельный accessor `StructBound(CS)` для Fit и call-site
dispatch. См. PushReform.md.

↓ и ↓ₛ **не транспортируют** S и в CS-результатах: S — graph-carried обязательство
(μ-back-edges ссылаются на узел-владельца), копирование его в спроецированный снапшот
создало бы тот же класс мёртвых островов, который запрещает Rule B (refinability).
Оговорка о минимальном представителе (`↓[∅..∅,S] = [∅..∅]` сам по себе не
удовлетворяет S) разряжается тем, что узел-владелец сохраняет свой bound — результат
↓ отвечает на интервальные вопросы и не заменяет bound-carrier. Если найдётся путь,
где результат ↓/↓ₛ ЗАМЕЩАЕТ bound-carrier — это дыра, фиксировать в TicTechnicalDebt.

### ConstraintsState — optional (Rule B)

Optional-лифт результата подчиняется канонической форме
(Rule B, `Algebra_CanonicalForms.md`): `opt(τ)` требует **решённого** τ;
лифт нерешённой границы остаётся во **flag form** `[..]?`. В коде ветка —
общий helper `LiftOptional` (StateExtensions.Concretest.cs), разделяемый ↓ и ↓ₛ:
закон делегации на этом плече структурный (долг #30 закрыт).

```
inner = ↓D                (если D определён)
inner = [∅..∅, cmp]       (если D = ∅)

↓[D..A]? =
    Any                       если inner = Any            (Opt(Any) = Any)
    opt(inner)                если inner — решённый тип
    [d..a, c ∨ cmp]?          если inner = [d..a, c]      (flag form; Preferred переносится)
    [inner..∅, cmp]?          если inner — нерешённый тип (flag form)
```

Следствия:

```
↓[∅..∅]?           = [∅..∅]?          (flag form; НЕ opt(fresh-CS))
↓[∅..A, cmp]?      = [∅..∅, cmp]?
↓[Bool..A]?        = opt(Bool)
↓[arr([u8..])..]?  = [arr([u8..])..]? (композит с нерешёнными членами — flag form)
```

Правило «нерешённое остаётся flag form» — прямое требование закона refinability
(«каждая нерешённая граница остаётся достижимой по рёбрам», см.
`Algebra_CanonicalForms.md`): материализация `opt(копия-CS)` создала бы мёртвый
остров, который никакое ребро не уточнит. Тесты:
`CanonicalOptionalFormTest.Concretest_EmptyOptionalCs_StaysFlagForm`,
`Concretest_SolvedOptionalCs_MaterializesOpt`, `Concretest_OptionalAny_CollapsesToAny`,
`Concretest_ArrayOfEmptyOptionalCs_NoDeadOptIsland`.

### Composites

Ковариантно по компонентам, **кроме аргументов функций** (контравариантных —
используют дуальный Abstractest):

```
↓(A[])              = (↓A)[]
↓Opt(A)             = Opt(↓A)                    (Opt(Any) = Any)
↓((A₁,...,Aₙ) → R)  = (↑A₁,...,↑Aₙ) → ↓R
↓Struct             = identity                    (см. ниже)
↓Ref(N)             = ↓N.State                   (ref-прозрачность)
```

Функция: наиболее конкретная функция **принимает** самые широкие аргументы
(Abstractest) и **возвращает** самый узкий результат (Concretest).

### Struct — identity, а не покомпонентная проекция

```
↓{f₁:N₁, ...} = {f₁:N₁, ...}     (identity, по модулю RefTo path-compression;
                                  TypeName и IsOptionalSourced сохраняются)
```

Покомпонентное правило `↓{f:A} = {f:↓A}` **некорректно**, по двум причинам:

1. **Поля — узлы графа, не значения.** Поле struct — `TicNode`, разделяемый с другими
   путями вывода. Покомпонентная проекция создала бы snapshot состояния поля вне
   узла — нерешённую копию, недостижимую по рёбрам (нарушение refinability, тот же
   класс ошибок, что запрещает Rule B).
2. **«Самый конкретный struct» не определяется по полям.** При width subtyping
   подтип struct имеет **больше** полей: множество подтипов `{f:T}` не имеет
   наименьшего элемента, выразимого перечислением полей. Проекция по полям отвечает
   на другой вопрос и не даёт нижней грани.

Единственное, что делает ↓ со struct — path-compression: `RefTo`-поля
разыменовываются до корневых узлов (identity на уровне семантики, нормализация на
уровне представления). Тесты: `MostConcreteStateTest.Struct1-3`.

## Связь с другими операторами

- **Abstractest**: дуальный — проекция на верхнюю границу. См. `Algebra_Abstractest.md`.
- **LCA**: `T ∨ CS = T ∨ ↓CS` — закон 4 умбреллы (`Algebra.md`), область действия:
  non-optional, hint-free **на любой глубине** (LCA транспортирует вложенные hint'ы
  через join — использует ↓ₛ на односторонних desc-проекциях, — а чистый ↓ их
  срезает, поэтому при вложенном Preferred формы расходятся).
- **Merge**: `CS ⊓ CS` вычисляет desc через LCA, который рекурсивно использует ↓.
- **Слой резолюции**: `SolveContravariant` — резолюционный аналог ↓ (выбирает
  descendant + Preferred); `ConcretestSnapshot` (↓ₛ) — Destruction-снапшот,
  Preferred-сохраняющий вариант ↓ для материализуемых состояний. ↓ проецирует,
  Solve/↓ₛ — выбирают; роли разделены (долг #19, ↓-часть закрыта).

## Законы

| Закон | Домен | Верификация |
|---|---|---|
| Identity: `↓T = T` | T (решённые) | `MostConcreteStateTest.Primitive1`, `Array1`, `Struct1` |
| Идемпотентность: `↓↓A = ↓A` | весь | `AlgebraLatticeLawsTest.Concretest_Idempotent` (примитивы 23, CS-зоопарк с флагами/Preferred, композиты с CS-внутренностями) |
| Fit-совместимость: `↓CS Fit CS` | **весь C** | `AlgebraLatticeLawsTest.Concretest_FitsBackInto_Cs` (полный sweep). Два ранее опровергнутых фрагмента закрыты #16 (единый satisfaction-предикат): (a) ancestor-only `[∅..A]` — unsolved-target ячейка ancestor согласована (`Fit(⊥,[∅..A]) = Fit(⊥,A) = true`); (b) opt-флаг с примитивным Desc `[P..]?` — Fit несёт opt-ось + лифт `T ≤ opt(T)` в примитивной ветке `CanBeFitConverted`. Пины (активны): `Concretest_FitsBackInto_AncestorOnlyCs`, `Concretest_FitsBackInto_OptionalPrimitiveDescCs` |
| Порядок границ: `↓CS ≤ ↑CS` | C **non-optional** | `AlgebraLatticeLawsTest.BoundsOrder_ConcretestLeqAbstractest_NonOptionalCs` |
| Opt collapse: `↓Opt(Any) = Any` | T | `Opt_Any_CollapseToAny` |
| Rule B ветки | C optional | `CanonicalOptionalFormTest.*` |
| Ref-прозрачность: `↓Ref(A) = ↓A` | всё | косвенно всеми suite |
| Fun-дуальность: args → ↑, return → ↓ | T | `MostConcreteStateTest.Fun1-4` |

**Закон `↓CS ≤ ↑CS` на optional-ветке ЛОЖЕН** — область действия ограничена
non-optional CS сознательно. Контрпример: `[I32..Real]?` даёт `↓ = opt(I32)`,
`↑ = Real` (↑ сбрасывает ось opt, см. `Algebra_Abstractest.md`), а `opt(I32) ≰ Real`.
Пара (↓, ↑) на optional-оси корректна только совместно с Destruction-обёрткой
(`WrapAncestorInOptional`) — это парный контракт стадий, не свойство проекций.

Монотонность (`A ≤ B ⟹ ↓A ≤ ↓B`) — закрыта 2026-07-10 (долг #20, последний остаток):
на **T** (решённые типы, где Fit — порядок подтипирования) закон ВЕРИФИЦИРОВАН —
`AlgebraLatticeLawsTest.Concretest_Monotone_SolvedTypes` (23-точечная решётка +
решённый композитный зоопарк, включая fun-вариантность). На **C** закон при ≤ = Fit
ОПРОВЕРГНУТ — [Ignore]-пин `Concretest_Monotone_CsFragment` (323 контрпримера на
ProjectionZoo²), два класса: (1) C справа — `Re Fit [U8..Re]`, но `↓Re = Re ≰ U8`:
Fit(X, C) — отношение satisfaction (X ∈ Sat(C)), не информационный порядок, а ↓
проецирует C на НИЖНЮЮ границу, так что любой сатисфаер строго выше D опровергает;
(2) opt-флаг слева — `[U8..]? Fit Re`, но `↓[U8..]? = opt(U8) ≰ Re` (парный контракт
↓/Destruction). «Подходящий смысл для C» требует порядка Sat-включения, которого
алгебра как предиката не определяет — до его появления C-версия закона не формулируема.

## Отклонения текущей реализации (см. TicTechnicalDebt)

1. ~~**Выбор `Opt(Preferred)`**~~ и ~~**`ConcretestArrayElement`**~~ — закрыты
   (долг #19, ↓-часть): обе резолюционные ветки вынесены в `ConcretestSnapshot` (↓ₛ),
   ↓ — чистая проекция по нормативным правилам выше. Чистые пины:
   `ConcretestSnapshotTest.Concretest_OptionalCsWithPreferred_IgnoresHint_PurePin`,
   `Concretest_ArrayElementCsWithPreferred_ProjectsToBareElement_PurePin`,
   `CanonicalOptionalFormTest.Concretest_OptionalCsWithPreferred_MaterializesLowerBoundAndDropsHint`.
   Контракт ↓ₛ — `ConcretestSnapshotTest` (делегация ↓ₛ ≡ ↓ на hint-free,
   идемпотентность, Fit-совместимость, оба резолюционных плеча).
2. ~~**Testing gap (остаток)**: монотонность ↓ не покрыта unit-тестами~~ — закрыт
   2026-07-10 (долг #20 закрыт целиком): на T верифицирована, на C опровергнута при
   ≤ = Fit и [Ignore]-запинена с контрпримерами (см. §Законы). Идемпотентность
   и Fit-совместимость закрыты `AlgebraLatticeLawsTest` (Fit-совместимость — по всему C
   с 2026-07-09, оба бывших контрпримерных фрагмента закрыты #16, см. таблицу законов;
   зоопарк расширен hint-несущими формами при закрытии ↓-части #19).
