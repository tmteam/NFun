# Merge — Пересечение ограничений на ConstraintsState

## Определение

**Merge(C₁, C₂)** — пересечение двух ограничений на одну переменную: наиболее общее ограничение, которому тип удовлетворяет тогда и только тогда, когда он удовлетворяет обоим входам. Если совместных типов нет — null.

Обозначение: **C₁ ⊓ C₂** (meet на домене ограничений C).

Семантика — через множества satisfier'ов. Для ограничения C обозначим `Sat(C) = { T : T ≤ C }` — множество конкретных типов, удовлетворяющих C (≤ — см. `Algebra_Fit.md`). Определяющее свойство:

```
Sat(C₁ ⊓ C₂) = Sat(C₁) ∩ Sat(C₂)
C₁ ⊓ C₂ = null   ⟺   Sat(C₁) ∩ Sat(C₂) = ∅
```

В алгебре есть **ровно один** оператор пересечения ограничений — Merge. `Unify(CS, CS)` **определяется** как Merge; T-часть ⊓ (конкретные типы, composites) — в `Algebra_Unify.md`. Merge — тот оператор, который фактически выполняется при слиянии узлов в Destruction (см. `TicAlgorithm_Destruction.md`).

## Домен

ConstraintsState — кортеж `[D..A, cmp, opt, S, P]`:

| Компонента | Смысл | Роль |
|---|---|---|
| D | нижняя граница (descendant) | ограничение |
| A | верхняя граница (ancestor, примитив) | ограничение |
| cmp | IsComparable | ограничение (type class) |
| opt | IsOptional | ограничение (тип обязан покрывать None) |
| S | StructBound (F-bound) | ограничение |
| P | Preferred | **метаданные** (hint для resolution, Sat не сужает) |

## Интервальное ядро

```
[D₁..A₁] ⊓ [D₂..A₂] = [D₁ ∨ D₂ .. A₁ ∧ A₂]
```

- **D = D₁ ∨ D₂** (LCA): satisfier обязан лежать выше обоих нижних вкладов, т.е. выше их join.
- **A = A₁ ∧ A₂** (GCD): satisfier обязан лежать ниже обеих верхних границ, т.е. ниже их meet. Если GCD не определён — результат null.
- **Непустота**: результат null, если интервал пуст:

```
IntervalIsNonEmpty([D..A, cmp]):
    D = ∅ ∨ A = ∅   → непуст (открытый интервал)
    D = A            → непуст ⟺ ¬cmp ∨ A ∈ Comparable
    иначе            → непуст ⟺ D ≤c A     (пессимистическая конвертация)
```

Условие пустоты — именно `¬(D ≤c A)`, а не «D выше A»: **несравнимость** D и A тоже опустошает интервал.

## Оси

Транспорт остальных компонент (каждая ось — своё правило):

| Ось | Правило | Обоснование |
|---|---|---|
| cmp | `cmp₁ ∨ cmp₂` (OR) | ограничения на meet накапливаются |
| opt | `opt₁ ∨ opt₂` (OR) | флаг «None внесён»: вклад любой стороны обязывает результат |
| S | `S₁ ∪ S₂` — объединение полей (ownerless `GcdBound`, теорема PT-F); после транспорта — three-way (D, A, S) непустота (`StructBoundIsSatisfiable`) | meet на решётке F-bound'ов; больше полей = строже. Self-referential позиции сохраняют node identity — rewire владения принадлежит стадиям (merge-вызыватели алиасят старых владельцев на merged-узел) |
| P | коммутативное правило ниже | метаданные; переносятся, но Sat не меняют |

### Preferred — транспорт

Правило обязано быть **коммутативным**:

```
P₁ = P₂                 → P = P₁
ровно одно задано       → P = заданное
P₁ ≠ P₂, оба заданы    → P = LCA(P₁, P₂); LCA = Any → P = ∅ (хинты из разных
                          семейств не несут общей информации)
пост-условие            → P влезает в результат (CanBeConvertedTo), иначе P = ∅
```

Реализация: единый хелпер `StateExtensions.PreferredHintLcaOrNull` — тот же, что
у P-транспорта LCA (∨); пост-условие применяется в `IntersectIntervalsOrNull`.
Правило коммутативно, но **не ассоциативно** (drop-order, TicTechnicalDebt #22).

## Коллапс-правила

Merge канонизирует результат — вырожденное ограничение сводится к типу:

**Точечный коллапс** `[T..T]`:

```
[T..T]        → T
[T..T, opt]   → Opt(T)
[T..T, cmp]   → null, если T ∉ Comparable
[T..T, S]     — недостижимо: точка примитивна (A ∈ Primitive), а примитив никогда
                не удовлетворяет struct bound — three-way непустота уже вернула null
                (Merge_PointCollapseWithBound_ReturnsNull)
```

**Коллапс решённого композита**: если A = ∅, ¬cmp и D — решённый Struct или Optional:

```
[D..]        → D
[D.., opt]   → Opt(D)
```

**Исключение (Preferred-survival)**: если D — Optional и P ≠ ∅, коллапс не выполняется. Hint обязан дожить до resolution (`TicPreferred.md`); коллапс в решённый тип уничтожил бы его.

**Исключение (S-discharge)**: при S ≠ ∅ коллапс пиннит узел к D, что легально только если
D сам удовлетворяет bound (`FitStructBound(D, S)` — S погашен решённым типом); иначе
constraint-форма сохраняется — пропуск канонизации всегда Sat-нейтрален
(`Merge_SolvedStructDescendant_FittingBound_CollapsesAndDischarges`,
`Merge_SolvedStructDescendant_NotFittingBoundByType_KeepsConstraintForm`).

Канонические формы — легитимная часть алгебры, а не костыли представления:

- **Rule B**: у флаг-формы `[D..A]?` descendant opt-free — Optional-ось живёт во флаге, не внутри D;
- **Opt(Any) = Any** — фактор-тождество домена (см. `Algebra_Fit.md` §Антисимметричность);
- **FlattenNestedOptional** — поддержание канонической формы `Opt(Opt(T)) = Opt(T)` при асинхронном уточнении элемента.

## Связь с Fit — закон поглощения

Определяющее свойство meet, прямое следствие Sat-семантики:

```
x ≤ (C₁ ⊓ C₂)   ⟺   x ≤ C₁  ∧  x ≤ C₂
```

На comparable-ячейках (бывшее отклонение F2 — comparable-примитив проходил `≤` без
descendant-проверки) закон верифицирован property-тестом
`AlgebraGcdFitLawsTest.MergeFit_Absorption_ComparableCells`. Единый
satisfaction-предикат закрыт (F1/#16, `Algebra_Fit.md`); полная развёртка закона по
всем ячейкам C — тестовый остаток (планируется).

## Связь с Simplify

`SimplifyOrNull` — канонизация представления, не самостоятельный алгебраический оператор: Sat(C) он не меняет, только приводит C к канонической форме или обнаруживает `Sat(C) = ∅` (→ null). Коллапс-правила Merge — подмножество канонизации Simplify. Композиция (реализована с #13):

```
⊓  =  интервальное ядро + транспорт осей  →  canonicalize
```

где canonicalize оператора ⊓ — **Sat-нейтральное подмножество** Simplify: точечный
коллапс, коллапс решённого композита (с flatten `Opt(Opt(T)) = Opt(T)`), cmp-правило
(numeric-D ограничен Real; для D вне {numeric, Char, arr(≤Char)} Sat пуст → null;
членство домена — единый предикат `IsComparableDomain`, долг #31 закрыт).
Полный `SimplifyOrNull` в ⊓ НЕ входит: он остаётся стадийной канонизацией состояний
узлов и содержит whole-value правило `[D..A≠Any]? → null` (opt-значение не влезает в
примитивный ancestor — MR2Bug3), которое конфликтует с flag-формой `[D..A]?`,
производимой join-правилом LCA (там A ограничивает ЭЛЕМЕНТ). Эта двойственность
прочтения ancestor-оси на opt-флагованных CS — открытая напряжённость слоя стадий;
затащить whole-value правило внутрь ⊓ нельзя (ломает `if-else`-join'ы с none-ветками:
семейство StaleSnapshot-тестов).

## Законы

Домен законов — C (ограничения). Законы для конкретных типов — `Algebra_Unify.md`.

| Закон | Формула | Статус |
|---|---|---|
| Коммутативность (ядро) | `C₁ ⊓ C₂ = C₂ ⊓ C₁` на [D..A, cmp, opt] | `Intersect_IsCommutative` (ConstraintsAlgebraTest); `Intersect_SymmetricOnDisjoint_BothNull`, `Intersect_IsSymmetric_*` (BugRegression_ConstraintsStateInvariantsTests) |
| Коммутативность (P-ось) | транспорт P симметричен | `Merge_Commutative_IncludingPreferredAxis`, `Merge_PreferredHints_Equal/OneSided/DifferFitting/DifferUnfitting/UnrelatedFamilies_*` (AlgebraMergeUnifyLawsTest) |
| Ассоциативность | `(C₁ ⊓ C₂) ⊓ C₃ = C₁ ⊓ (C₂ ⊓ C₃)` | **ОПРОВЕРГНУТА по модулю хинтов и collapse** (TicTechnicalDebt #22): (1) drop-order P-оси — `([∅..∅] ⊓ [U8..I64]Re!) ⊓ [U8..Re]I32! = [U8..I64]I32!` но правая свёртка даёт `[U8..I64]`; (2) Sat-меняющий коллапс решённого композита `[D..∅] → D` (D — Struct/Optional) сужает Sat с `{T ≥ D}` до `{D}` mid-fold (#19 spirit). Бывший корень «недо-принимающая ячейка T ⊓ CS» устранён #16 (единый предикат: −64 контрпримера, +0 новых). Полный тест `Merge_Associative_OverCsTriples` [Ignore]-pinned; hint-free non-collapsing фрагмент (примитивные и array D, все cmp/opt-комбинации) ассоциативен — `Merge_Associative_HintFree_NonCollapsing_OverCsTriples`; фикс ячейки закреплён `Merge_TCsCell_OptionalLift_MidFoldCollapse_Associates` |
| Unify(CS,CS) ≡ Merge | один оператор ⊓ | `Unify_OnConstraints_EqualsMerge_Matrix` (AlgebraMergeUnifyLawsTest) |
| Выживание осей через ⊓ | opt и P переживают унификацию | `Unify_OptionalAxis_SurvivesUnification`, `Unify_Preferred_SurvivesUnification`, `Unify_OptionalAndPreferred_SurviveTogether` (AlgebraMergeUnifyLawsTest) |
| Идемпотентность | `C ⊓ C = C` | НЕВЕРИФИЦИРОВАНА (планируется) |
| Пустота | `C₁ ⊓ C₂ = null ⟺ Sat(C₁) ∩ Sat(C₂) = ∅` | `Merge_IncompatibleAncestors_ReturnsNull`, `NonEmpty_*` (ConstraintsAlgebraTest); `Intersect_DisjointIntervals_ReturnsNull`, `IntervalIsNonEmpty_*` (BugRegression_ConstraintsStateInvariantsTests) |
| Нейтральность пустого C | `CS(∅) ⊓ C = C` | `Intersect_OneEmpty_OneNarrow_ReturnsNarrow` (BugRegression_ConstraintsStateInvariantsTests) |
| Точечный коллапс | `[T..T] → T`, `[T..T, opt] → Opt(T)` | `Merge_SamePoint_CollapsesToPrimitive`, `Merge_SamePoint_BothOptional_ReturnsOptional` (ConstraintsAlgebraTest) |
| Коллапс композита | `[D..] → D` для решённого D | `Merge_SolvedStruct_NoAncestor_ReturnsStruct` (ConstraintsAlgebraTest) |
| P-транспорт (one-sided) | одно заданное P переживает ⊓ | `Merge_PreferredFromFirst_Preserved`, `Merge_PreferredFromSecond_Preserved` (ConstraintsAlgebraTest) |
| opt-транспорт | `opt = opt₁ ∨ opt₂` | `Intersect_OptionalORs`, `Intersect_BothOptional_IsOptional`, `Intersect_NeitherOptional_NotOptional` (ConstraintsAlgebraTest) |
| cmp-транспорт | `cmp = cmp₁ ∨ cmp₂` | `Intersect_ComparableORs` (ConstraintsAlgebraTest) |
| S-транспорт | `S = S₁ ∪ S₂`, коммутативен; one-sided S выживает | `Merge_OneSidedBound_Survives_BothOrders`, `Merge_BothBounds_FieldUnion`, `Merge_SharedBoundField_MeetsPerGcdBound`, `Merge_CommutativeOnSAxis`, `Unify_BoundSurvivesUnification` (AlgebraStructBoundLawsTest) |
| S-непустота (three-way) | `⊓ = null` при S + не-Any примитивном A, S + cmp, S + struct-D без required-полей | `Merge_BoundVsNonAnyPrimitiveAncestor_ReturnsNull`, `Merge_BoundVsComparable_ReturnsNull`, `Merge_SolvedStructDescendant_MissingBoundField_ReturnsNull`, `Merge_PointCollapseWithBound_ReturnsNull` (AlgebraStructBoundLawsTest) |
| S identity-preservation | self-referential позиции bound'а сохраняют node identity через ⊓ | `Merge_SelfReferentialBound_PreservesBackEdgeIdentity`, `Merge_SharedSelfReferentialField_KeepsSelfRefSide`; cycle-merge репродукция — `MergeGroup_CycleWithBoundCarrier_PreservesStructBound`, `MergeGroup_CycleBoundCarrierWithEmptyPeer_KeepsBound` (AlgebraStructBoundLawsTest) |
| Поглощение с Fit | `x ≤ (C₁ ⊓ C₂) ⟺ x ≤ C₁ ∧ x ≤ C₂` | `AlgebraGcdFitLawsTest.MergeFit_Absorption_ComparableCells` (comparable-ячейки; предикат унифицирован #16, полная развёртка покрытия — тестовый остаток) |

## Отклонения текущей реализации (см. TicTechnicalDebt)

| # | Отклонение | Текущее поведение | Целевое правило |
|---|---|---|---|
| M4 | Пост-проверка интервала асимметрична | MergeOrNull повторно проверяет D **receiver'а** против merged-A; симметричный вклад аргумента этим путём не проверяется | непустота проверяется один раз, на merged `[D..A]` |

Закрыто 2026-07-09 (#13/#14): M1 (три реализации ⊓ — `Unify(CS,CS)` теперь
делегирует MergeOrNull, P-транспорт живёт в одном месте) и M2 (некоммутативный
P-tie-break — заменён коммутативным LCA-правилом `PreferredHintLcaOrNull`).
Закрыто 2026-07-09 (#12): M3 (S-транспорт внутри ⊓ — `S = S₁ ∪ S₂` через
ownerless `GcdBound`; законы в `AlgebraStructBoundLawsTest`).
