# FitsInto — Subtype Relation

## Определение

**FitsInto(A, B)** — предикат: "тип A является подтипом B" (A можно безопасно использовать там, где ожидается B).

Обозначения:

- **A ≤ B** — Fit;
- **A ≤c B** — пессимистическая конвертация (CanBeConvertedPessimisticTo): преобразование корректно в ЛЮБОМ случае. Для примитивов ≤ и ≤c совпадают (таблица).

Интуиция: "значение типа A можно присвоить переменной типа B без потери информации".

Домен: A — описание типа; B — либо конкретный тип (домен T), либо ConstraintsState (домен C). Для B ∈ T предикат — частичный порядок; для B ∈ C — satisfaction: `A ≤ C ⟺ A ∈ Sat(C)` (см. `Algebra_Merge.md`).

## Единый satisfaction-предикат (#16 закрыт 2026-07-09)

**Правило одним предложением**: `T satisfies C` — ЕДИНСТВЕННЫЙ авторитетный предикат
(CS-ячейка `FitsInto(target, ConstraintsState)`, StateExtensions.Fit.cs) = Optional-ось
(`None ≤ C?`; `opt(X) ≤ C? ⟺ X` проходит плоские ячейки; плоский `T ≤ C?` — implicit
lift) ∧ интервал `[D..A′]` ∧ cmp ∧ **S** (FitStructBound) — все прочие точки проверки
(`ConstraintsState.CanBeConvertedTo`, клетка `T ⊓ CS` в UnifyOrNull, fast-path мержа
узлов `GetMergedStateOrNull`) делегируют ему.

Следствия унификации:

- **CanBeConvertedTo** — однострочная делегация; прежняя МЕЛКАЯ проверка композитов
  (сравнение по конструктору, элементы игнорировались:
  `CS[arr(I64)..].CanBeConvertedTo(arr(U8)) = true`) заменена глубокой структурной
  (`CanBeFitConverted`). Fast-path мержа узлов (SolvingFunctions.cs) получил глубокую
  проверку через ту же делегацию — без аллокаций, рекурсия ограничена глубиной типа.
  Пины: `CanBeConvertedTo_CompositeDesc_*`, `CanBeConvertedTo_OptionalLift_*`
  (BugRegression_ConstraintsStateInvariantsTests).
- **Optional-ось** теперь ЧАСТЬ предиката (была «не проверяется»): флаг `C?` читается
  как opt-лифт интервала — `opt(X)`-target разворачивается до X, плоский target идёт
  через implicit lift. Обязательство «узел обязан принять None» остаётся стадийным
  (Destruction `WrapAncestorInOptional`), поэтому lift-принятие плоского T Sat-корректно.
- **Unsolved-target ячейка ancestor**: нерешённый target с Descendant обязан
  `D_t ≤c A′`; без Descendant — верхние границы обязаны пересекаться
  (`GCD(A_t ?? Any, A′) ≠ ∅`). Прежняя ячейка была пессимистична
  (`Fit(⊥, [∅..A′]) = false` при `Fit(⊥, A′) = true` — несогласованность с ячейкой
  решённого target). Пин: `Concretest_FitsBackInto_AncestorOnlyCs`.
- **Лифт в примитивной ветке** `CanBeFitConverted`: `D ≤ opt(X) ⟺ D ≤ X` — зеркало
  композитной ветки (снятая асимметрия: `[arr([U8..])..]?` проходил, `[U8..]?` — нет).
  Пин: `Concretest_FitsBackInto_OptionalPrimitiveDescCs`.

**Preferred** по-прежнему не проверяется НАМЕРЕННО: это метаданные для материализации
(Preferred-policy, см. `TicPreferred.md`), а не ограничение совместимости.

## Связь с LCA и GCD

FitsInto, LCA и GCD — три грани одного отношения порядка:

```
A ≤ B  ⟺  A ∨ B = B  ⟺  A ∧ B = A      (последняя эквивалентность — когда A ∧ B определён)
```

Если `A ∧ B = null` — A и B **несравнимы** (ни A ≤ B, ни B ≤ A). FitsInto — проверка, LCA/GCD — вычисление. FitsInto дешевле: возвращает bool без аллокаций.

## Алгебраические свойства

Свойства сформулированы для домена T (решённые типы):

| Свойство | Формула | Статус |
|---|---|---|
| Рефлексивность | `A ≤ A` | примитивы — таблица; composites — НЕВЕРИФИЦИРОВАНО (планируется) |
| Транзитивность | `A ≤ B ∧ B ≤ C ⟹ A ≤ C` | НЕВЕРИФИЦИРОВАНО (планируется) |
| Антисимметричность | `A ≤ B ∧ B ≤ A ⟹ A = B` **по модулю Opt(Any) = Any** | НЕВЕРИФИЦИРОВАНО (планируется) |
| Top | `∀A: A ≤ Any` | `Lca_AnyAbsorbs_ConcreteTypes` (AlgebraInvariantsTest, через триаду) |
| Ref-прозрачность | `Ref(A) ≤ B ⟺ A ≤ B` | НЕВЕРИФИЦИРОВАНО (планируется) |

Антисимметричность: `Any ≤ Opt(Any)` (implicit lift) и `Opt(Any) ≤ Any` выполняются одновременно, поэтому антисимметрия верна на фактор-множестве `T / {Opt(Any) = Any}`. Тождество `Opt(Any) = Any` — определяющий постулат домена (см. `Algebra.md` §Постулаты), а не нарушение порядка.

Это **частичный порядок** (по модулю фактор-тождества) — не все пары сравнимы (`Bool` и `I32` несравнимы).

## Два контекста проверки

### 1. `A ≤ ConcreteType` — проверка конкретного типа

**Примитивы:** `A ≤ B` по таблице (CanBePessimisticConvertedTo). Например: `I32 ≤ Real`, `U8 ≤ I16`.

**None:**
```
None ≤ None
None ≤ Any
None ≤ Opt(T)    для любого T
None ≰ T          для других конкретных T
```

**Optional** — ковариантна + implicit lift:
```
Opt(A) ≤ Opt(B)   ⟺  A ≤ B           (ковариантность)
A ≤ Opt(B)         ⟺  A ≤ B           (implicit lift: T ≤ Opt(T))
Opt(A) ≤ B         только если B = Any  (нельзя "опустить" optional в non-optional)
```

Implicit lift `T ≤ Opt(T)` — ключевое правило: non-optional значение всегда принимается там, где ожидается optional.

**Array** — ковариантна:
```
A[] ≤ B[]  ⟺  A ≤ B
A[] ≤ B    только если B = Any
```

**Struct** — width subtyping + ковариантность полей:
```
{f₁:A₁, ..., fₙ:Aₙ, extra...} ≤ {f₁:B₁, ..., fₙ:Bₙ}
⟺  ∀i: Aᵢ ≤ Bᵢ
```

Подтип **имеет все поля** надтипа (и может иметь дополнительные). Каждое поле ковариантно.

**MutableStruct** — подсемейство Struct; направление мутабельности и вариантность полей:

```
Struct    ≤ MutStruct   = false                              (immutable нельзя повысить до mutable)
MutStruct ≤ Struct      ⟺  ∀f ∈ Struct: Mut.f ≤ Struct.f   (read-only view: ковариантно + width)
MutStruct ≤ MutStruct   ⟺  Fields ⊇ (width) ∧ ∀f: A.f ⊓ B.f ≠ null   (поля ИНВАРИАНТНЫ)
```

Закон **«инвариантный field-subtyping = ⊓-совместимость»**: mutable-поле одновременно читается (ковариантная позиция) и пишется (контравариантная); пересечение требований — инвариантность. Алгебраически она выражается не двусторонним `≤` (это была бы equality решённых типов), а совместимостью по Merge/Unify: `A.f ⊓ B.f ≠ null` допускает нерешённые поля, чьи ограничения ещё могут сойтись. Статус: НЕВЕРИФИЦИРОВАНО unit-тестом (планируется).

**Function** — контравариантна по аргументам, ковариантна по возврату:
```
(A₁,...,Aₙ → R₁) ≤ (B₁,...,Bₙ → R₂)
⟺  ∀i: Bᵢ ≤ Aᵢ  ∧  R₁ ≤ R₂
⟺  argsCount равен
```

Контравариантность: функция-подтип **принимает более широкие** аргументы.

### 2. `A ≤ ConstraintsState` — проверка интервала

```
A ≤ [D..A′, cmp, S]?   ⟺  A = None                                  (opt-ось: None-лифт)
A ≤ [D..A′, cmp, S]?   ⟺  X ≤ [D..A′, cmp, S]  для A = opt(X)      (opt-ось: развёртка)
A ≤ [D..A′, cmp, S]?   ⟺  A ≤ [D..A′, cmp, S]  для прочих A        (implicit lift)

A ≤ [D..A′, cmp, S]    ⟺     (cmp ⟹ A ∈ Comparable)
                           ∧ (D задан  ⟹ D ≤c A)
                           ∧ (A′ задан ⟹ ancestor-ячейка, см. ниже)
                           ∧ (S задан  ⟹ A решён ⟹ FitStructBound(A, S);
                              A нерешён — S откладывается до merge-фазы)

ancestor-ячейка:  A решён      ⟹ A ≤c A′
                  A = CS с D_t ⟹ D_t ≤c A′
                  A = CS без D ⟹ GCD(A_t ?? Any, A′) ≠ ∅
```

1. **Comparable check**: Comparable-типы — числовые примитивы, Char, arr(Char). Optional, Struct, Fun, None, Bool — НЕ comparable. Единый предикат домена — `IsComparableDomain` (долг #31 закрыт): для нерешённой части вопрос «может ли ещё стать членом домена» (≤-форма) — `arr([..Ch])` проходит cmp-ячейку (может уточниться до arr(Ch)), `arr(U8)` — нет; unsolved CS принимается консервативно. Comparable-проверка **не отменяет** descendant-проверку: `U8 ≤ [I64.., cmp]` — false (I64 ≰c U8), хотя U8 comparable. Верификация: `AlgebraGcdFitLawsTest.FitsInto_ComparableCell_DescendantViolation_False`, `FitsInto_ComparableCell_ConjunctiveRule_AllPrimitives`, `ConstraintsAlgebraTest.FitsInto_ComparableCell_*`.
2. **Descendant check**: `D ≤c A` — см. CanBeFitConverted ниже.
3. **Ancestor check**: `A ≤c A′`.
4. **StructBound check** (F-bound): A обязан быть `StateStruct`, `Fields(A) ⊇ Fields(S)`, `∀f ∈ S: A.f ≤ S.f` (ковариантно, width subtyping). Coinductive cycle-guard для self-referencing bound'ов (Amadio–Cardelli). Часть единого предиката (см. §Единый satisfaction-предикат). Полная семантика F-bound — `PushReform.md`.

**IsOptional** — часть предиката (opt-ось, правила выше); **Preferred** НЕ проверяется (см. §Единый satisfaction-предикат).

### CanBeFitConverted(D, A)

Вспомогательный предикат descendant-проверки: "существует ли тип T такой что D ≤ T и T ≤ A" — совместим ли descendant D с целью A.

| D | A | Проверка |
|---|---|---|
| Primitive | Primitive | `D ≤c A` по таблице (в частности `Any ≤c A` только при A = Any) |
| Primitive | CS | `CS.Desc ≠ ∅ ∧ D ≤c CS.Desc` |
| Primitive | Optional target | implicit lift: `D ≤ Opt(X)` → `D ≤ X` (зеркало композитной ветки, #16) |
| CS | любой | рекурсивно по `CS.Descendant` (если есть), иначе true |
| Composite | Composite (тот же конструктор) | покомпонентно |
| Composite | Optional target | implicit lift: `D ≤ Opt(X)` → `D ≤ X` |
| Composite | CS | через `CS.Descendant`, если он composite; иначе false |
| Composite | другой конструктор | false |

Примечания:

- для struct-полей используется `≤c` (пессимистическая конвертация), а не рекурсивный CanBeFitConverted;
- `≤c` для struct-целей **инвариантна по глубине** на struct-типизированных полях: набор полей — width subtyping (`Fields(from) ⊇ Fields(to)`), но значения общих полей сравниваются через **Equals**, без рекурсии по `≤c`.

## Общее правило для composites

Дуально к LCA/GCD: FitsInto вычисляется **покомпонентно** с учётом вариантности:

| Вариантность | Проверка компоненты |
|---|---|
| **Ковариантная** (out) | `Aᵢ ≤ Bᵢ` |
| **Контравариантная** (in) | `Bᵢ ≤ Aᵢ` (обратный порядок) |
| **Инвариантная** (MutStruct поля) | `Aᵢ ⊓ Bᵢ ≠ null` |

Если типы структурно несовместимы (разные конструкторы) — false (кроме Any).

## Граничные случаи

| Случай | Результат | Почему |
|---|---|---|
| `A ≤ A` | true | Рефлексивность |
| `A ≤ Any` | true | Any — top |
| `None ≤ Opt(T)` | true | None допустим в Optional |
| `None ≤ I32` | false | None не является числом |
| `Opt(A) ≤ A` | false | Нельзя "опустить" optional (кроме A=Any) |
| `A ≤ Opt(A)` | true | Implicit lift |
| `{x:A,y:B} ≤ {x:C}` | A ≤ C | Доп. поля OK (width subtyping) |
| `{x:A} ≤ {x:B,y:C}` | false | Не хватает поля y |
| `CS(∅) ≤ T` (T конкретный, разыменованный) | true | Неограниченный CS может стать любым конкретным T |
| `CS(∅) ≤ [..A′]` | true | Unsolved-target ячейка (#16): ⊥ может сузиться под A′ — согласовано с `Fit(⊥, A′) = true` |
| `CS[..A_t] ≤ [..A′]` | `GCD(A_t, A′) ≠ ∅` | Существует общий satisfier под обеими верхними границами |
| `CS[D_t..] ≤ [..A′]` | `D_t ≤c A′` | Committed lower bound обязан пройти под A′ |
| `CS(∅) ≤ [D..]`, D задан | false | Descendant-проверка требует descendant у левого операнда |

## Связь с другими операторами

- **LCA** (`∨`): `A ≤ B ⟺ A ∨ B = B`. См. `Algebra_LCA.md`.
- **GCD** (`∧`): `A ≤ B ⟺ A ∧ B = A` (когда определён). См. `Algebra_GCD.md`.
- **Merge** (`⊓`): закон поглощения `x ≤ (C₁ ⊓ C₂) ⟺ x ≤ C₁ ∧ x ≤ C₂`. См. `Algebra_Merge.md`; на comparable-ячейках верифицирован `AlgebraGcdFitLawsTest.MergeFit_Absorption_ComparableCells`.
- **CanBePessimisticConvertedTo**: примитив ≤ примитив через LCA-таблицу (`A ≤ B ⟺ LCA(A,B) = B`). Только для StatePrimitive.
- **CanBeConvertedPessimisticTo** (`≤c`): общий рекурсивный ≤ для любых типов. Используется в descendant/ancestor-проверках и для struct-полей.

## Сложность

| Входные типы | Сложность |
|---|---|
| Primitive × Primitive | O(1) |
| Composite × Composite | O(components × depth) |
| Target × ConstraintsState | O(1) + CanBeFitConverted |
| F-bound (FitStructBound) | O(fields × depth) + cycle guard |

## Инварианты

1. **Рефлексивность**: A ≤ A
2. **Транзитивность**: A ≤ B ∧ B ≤ C ⟹ A ≤ C — НЕВЕРИФИЦИРОВАНО (планируется)
3. **Антисимметричность**: A ≤ B ∧ B ≤ A ⟹ A = B, по модулю `Opt(Any) = Any`
4. **Top**: ∀A: A ≤ Any
5. **None lift**: None ≤ Opt(T), None ≤ Any, None ≤ None — `None_FitsInto_OptI32/OptBool/OptArray/OptStruct` (ConstraintsFitsTest)
6. **Implicit lift**: T ≤ Opt(T)
7. **Ref-прозрачность**: Ref(A) ≤ B ⟺ A ≤ B
8. **Согласованность с ∨/∧**: A ≤ B ⟺ A ∨ B = B ⟺ A ∧ B = A (когда ∧ определён) — примитивы: `Lca_FitsInto_Relationship_Primitives`, `Gcd_FitsInto_Relationship_Primitives` (AlgebraInvariantsTest)
9. **CS-режим**: правила интервала — `PrimitiveFits_*`, `ArrayFits_*`, `FunFits_*`, `StructFits_*`, `TextFits_into_Comparable_returnsTrue` (ConstraintsFitsTest); comparable-ячейка (конъюнктивное правило) — `AlgebraGcdFitLawsTest.FitsInto_ComparableCell_*`, `FitsInto_NonComparable_IntoComparableCell_False`; полный предикат — `CanBeConvertedTo_*` (BugRegression_ConstraintsStateInvariantsTests)

## Отклонения текущей реализации (см. TicTechnicalDebt)

Открытых отклонений нет.

Закрыто 2026-07-09 (#16): F1 (раздвоение предиката — `FitsInto(_, CS)` теперь единый
авторитетный satisfaction-предикат с S/opt-осями, CanBeConvertedTo и клетка `T ⊓ CS`
делегируют ему) и F3 (RefTo-target разыменовывается до классификации ячеек —
Ref-прозрачность структурная).
