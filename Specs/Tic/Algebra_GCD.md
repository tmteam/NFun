# GCD — Greatest Common Descendant

## Определение

**GCD(A, B)** — наибольший (самый общий) тип, который является подтипом и A, и B.

Обозначение: **A ∧ B** (meet в решётке типов).

Интуиция: "наиболее широкий тип, значения которого принимаются и там где ожидается A, и там где ожидается B".

GCD — оператор слоя **алгебры**: чистый, частичный. Поведение слоя **резолюции**
(SolveContravariant, политика Preferred, материализация) — см. `TicPreferred.md` и
`TicAlgorithm_Destruction.md` §6.

## Частичность

В отличие от LCA, GCD **может не существовать** (возвращает null). Два типа могут не иметь общего подтипа.

Например: `Bool ∧ I32 = null` — нет типа, который одновременно Bool и I32.

## Домены законов

**T** — решённые типы (`IsSolved`); **C** — constraints (`ConstraintsState`).
Полный набор законов meet-полурешётки выполняется на T. На C-фрагменте CS вносит в meet
только свой Ancestor (Desc и флаги — обязательства узла, не верхняя граница), из-за чего
идемпотентность не выполняется. Коммутативность на C выполняется с точностью до
⊤-класса на клетке «оба операнда нейтральны» (см. раздел ConstraintsState).

## Алгебраические свойства

| Свойство | Формула | Домен | Верификация |
|---|---|---|---|
| Идемпотентность | `A ∧ A = A` | T | `AlgebraInvariantsTest.Gcd_Idempotent` |
| Коммутативность | `A ∧ B = B ∧ A` | T ∪ C (на клетке «оба нейтральны» — с точностью до ⊤-класса) | `Gcd_Symmetry` (T); `AlgebraGcdFitLawsTest.Gcd_Commutative_IncludingConstraintsFragment` (T ∪ C) |
| Ассоциативность | `(A ∧ B) ∧ C = A ∧ (B ∧ C)` где определено | T | `Gcd_Associativity_Composites` (выборочно); примитивы — `AlgebraLatticeLawsTest.Gcd_Associativity_Primitives_FullLattice` (все 23 точки, 23³) |
| Монотонность | `A ≤ B ⟹ A ∧ C ≤ B ∧ C` где определено | T | `AlgebraLatticeLawsTest.Gcd_Monotone_FullLattice` (23³; A∧C≠null ⟹ B∧C≠null ∧ A∧C ≤ B∧C) |
| Top-нейтральность | `A ∧ Any = A` | T ∪ C | `Gcd_AnyIsIdentity_ConcreteTypes`, `Gcd_OptAnyIsIdentity`; на нерешённых операндах — `AlgebraGcdFitLawsTest.Gcd_AnyIsIdentity_UnsolvedComposites` |
| Null-propagation | `A ∧ B = null ⟹ ∀C≤A: C ∧ B = null` | T | `AlgebraLatticeLawsTest.Gcd_NullPropagation_FullLattice` (23³) |
| Ref-прозрачность | `Ref(A) ∧ B = A ∧ B` | T ∪ C | `AlgebraLatticeLawsTest.Gcd_RefTransparent` |

## Дуальность с LCA

| LCA (∨) | GCD (∧) |
|---|---|
| Наименьший **супер**тип | Наибольший **под**тип |
| Всегда определён | Может быть null |
| `A ∨ Any = Any` | `A ∧ Any = A` (на T) |
| CS вносит Descendant, Ancestor теряется | CS вносит Ancestor, Descendant теряется |

Связь: `A ≤ B` ⟺ `A ∨ B = B` ⟺ `A ∧ B = A`.
Сэндвич: `GCD(A,B) ≤ A ≤ LCA(A,B)` (`AlgebraInvariantsTest.Lca_Gcd_Duality_ForPrimitives`).

## Правила по категориям типов

### Примитивы

Решётка — **23 точки** (см. `Algebra_LCA.md` и `TicTypeSystem.md`). Кросс-таблица
signed × unsigned задаётся формулой замыкания (m, n — битовые ширины):

```
GCD(U_m, I_n) = наибольший U_j,  j ≤ min(m, n−1),  j ∈ {64,48,32,24,16,12,8}, иначе U4
```

(−1 — бит знака). U4 — низ числовой решётки (номинальная ширина 7 = общий поднабор I8 и U8),
поэтому внутри числовой семьи GCD всегда определён.

Например: `I64 ∧ Real = I64`, `I32 ∧ U32 = U24`, `Bool ∧ I32 = null`.

Реализация: предвычисленная таблица 23×23, O(1). Null-записи = нет общего подтипа.

### None

None — bottom для optional типов, но НЕ bottom для всех типов:

```
None ∧ None   = None
None ∧ Any    = None         (None ≤ Any)
None ∧ Opt(T) = None         (None ≤ Opt(T))
None ∧ T      = null         (None ≰ конкретным не-optional типам)
```

### Optional

- `Opt(A) ∧ Opt(B) = Opt(A ∧ B)` — ковариантно по inner; null пробрасывается.
- `Opt(A) ∧ Any = Opt(A)` — Opt(A) ≤ Any (при A = Any — коллапс к Any: `Opt(Any) = Any`).
- `Opt(A) ∧ B = A ∧ B` — для B ∉ {Any, Optional}. Почему: если C ≤ Opt(A) и C ≤ B,
  то C ≠ None (т.к. None ≰ B), значит C ≤ A. Поэтому meet — без обёртки.

### Составные (composite) типы — общее правило

Дуально к LCA. Для `F<T₁, T₂, ...>`:

| Вариантность | Компонента результата | Оператор |
|---|---|---|
| **Ковариантная** (out) | `Tᵢ' = Tᵢ_a ∧ Tᵢ_b` | GCD |
| **Контравариантная** (in) | `Tᵢ' = Tᵢ_a ∨ Tᵢ_b` | LCA |

Разные конструкторы → null: `F<...> ∧ G<...> = null` (F ≠ G).
Если GCD ковариантной компоненты = null → весь GCD = null.

- **Array**: `A[] ∧ B[] = (A ∧ B)[]`. Если `A ∧ B = null` → null.
- **Function**: `(A→R₁) ∧ (B→R₂) = (A ∨ B) → (R₁ ∧ R₂)`. Аргументы — LCA (тотален,
  null-источником быть не может), возврат — GCD. Если argsCount различен — null.

### Struct — width subtyping

Дуально к LCA: struct GCD = **объединение полей** (union). Больше полей = конкретнее:

```
{x:A, y:B} ∧ {x:C, z:D} = {x: A ∧ C, y:B, z:D}
{x:A}       ∧ {y:B}      = {x:A, y:B}
```

Для общих полей — GCD типов; null общего поля → весь GCD = null.

**Мутабельность**: GCD допустим кросс-семейно (MutStruct <: Struct).
- Если **хотя бы одна** сторона mutable — общие поля инвариантны: `UnifyOrNull` (точное
  совпадение); null → весь GCD = null.
- Вид результата: `Mut ∧ Mut = Mut`; `Struct ∧ Mut = Mut` (meet — конкретнее, mutable
  конкретнее immutable); `Struct ∧ Struct = Struct`.

**Метаданные**:
- `IsOptionalSourced = os₁ OR os₂` — маркер распространяется через каждый meet
  (асимметрия с LCA, где он не распространяется вовсе — сознательно).
- `TypeName`: null поглощается к имени другой стороны; оба равны → имя; оба различны → null.
  **Шире** правила LCA (там имя выживает только при совпадении с обеих сторон): meet
  конкретнее обеих сторон, поэтому вправе принять идентичность одной из них.

**Identity-sharing**: односторонние поля вставляются в результат **алиасом узла операнда** —
результирующий struct разделяет живые узлы с операндами, а не снимает snapshot. Дальнейшее
сужение поля-операнда видно через результат.

**Рекурсия и терминация** — коиндуктивное правило, зеркальное LCA: `GCD(μX.T, μX.T) = μX.T`.
Для именованных μ-типов цикл срезается по совпадению имени. Без среза многопутевые
рекурсивные типы (self-ссылка через `?` и через `rule`) расходятся. Механизм — коиндуктивный
контекст (явный параметр `AlgebraCycleContext`, не ambient-состояние; инвариант №13
Algebra.md), протянутый и через мост `GcdBound → MergeFieldStateGcd → ∧`.

### Пользовательские примитивы

`StatePrimitiveCustom` — изолированные точки: `custom ∧ custom = custom` (то же имя),
`custom ∧ Any = custom`, иначе null.

## ConstraintsState

Правило раскрытия: сторона-CS вносит в meet **только свой Ancestor**; при его отсутствии
сторона нейтральна — результат равен второму операнду **без изменений**.

```
[D..A] ∧ T = A ∧ T           (Ancestor определён — сужаем по нему)
[D..∅] ∧ T = T               (нейтральная сторона: T возвращается как есть)
```

Нейтральность проверяется у **обеих** сторон до какого-либо поднятия через Ancestor —
иначе результат зависит от порядка операндов. Следствия для CS ∧ CS:

```
[D₁..∅] ∧ [D₂..A₂] = [D₂..A₂]    (второй операнд целиком: Desc, флаги, Preferred выживают)
[D₁..A₁] ∧ [D₂..∅] = [D₁..A₁]    (симметрично: нейтральная сторона нейтральна в любой позиции)
[D₁..A₁] ∧ [D₂..A₂] = A₁ ∧ A₂
[D₁..∅] ∧ [D₂..∅]  = нейтральный CS (оба операнда — представители одного ⊤-класса;
                      результат определён с точностью до ⊤-класса, реализация возвращает
                      второй операнд)
```

**Transport-правило**: сторона, раскрытая через Ancestor, **сбрасывает** свои Desc,
IsOptional, IsComparable и Preferred — они принадлежат узлу (нижняя граница, обязательства
резолюции), а не верхней границе, по которой идёт meet. Нейтральная сторона (`[D..∅]`)
сбрасывает свои оси, но второй операнд выживает целиком, включая его Preferred.
**Исключение — ось S**: StructBound — структурная верхняя граница (upper-bound
информация, как Ancestor), поэтому она переживает и раскрытие, и нейтральность —
транспорт `S = S₁ ∪ S₂` применяется поверх ядра (см. §StructBound ниже).
Верификация: `AlgebraGcdFitLawsTest.Gcd_NeutralCs_ReturnsOtherOperandWhole_BothOrders`,
`Gcd_NeutralCs_IsIdentity_OnSolvedTypes`, `Gcd_BoundedCs_EntersMeetThroughAncestorOnly`.

**StructBound (S)**: `S = S₁ ∪ S₂` (union полей через ownerless `GcdBound`, рекурсивный GCD
на общих; null поглощается к другому). Семантика по satisfier-множествам: meet — более
сильное ограничение, значит объединение обязательных полей (Theorem PT-F, см. `Algebra.md`).
Union применяется поверх S-free ядра; результат ядра определяет форму:

```
core = CS         → CS + S-union (three-way (D,A,S) непустота, иначе null)
core = struct T   → GcdBound(T, S) — struct-≤ и есть F-bound-предикат,
                    решённый struct — просто ещё один вклад в bound
core = Opt(X)     → как meet(S, X) — ни один struct-satisfier не None
core = Any        → CS{S} — Any не несёт информации
core = иное решён → null — под не-struct нет struct-satisfier'а
```

Тесты: `AlgebraStructBoundLawsTest.Gcd_BothBounds_FieldUnion`,
`Gcd_OneSidedBound_SurvivesNeutralOtherSide_BothOrders`, `Gcd_CommutativeOnSAxis`,
`Gcd_BoundVsAny_ReturnsBoundConstraint`, `Gcd_BoundVsNonAnyPrimitive_ReturnsNull`,
`Gcd_BoundVsPrimitiveAncestorCs_ReturnsNull`, `Gcd_BoundVsSolvedStruct_AbsorbsViaUnion`,
`Gcd_BoundVsArray_ReturnsNull`.

## Граничные случаи

| Случай | Результат | Почему |
|---|---|---|
| `A ∧ A` | `A` | Идемпотентность (T) |
| `A ∧ Any` | `A` | Any не сужает (T) |
| `Bool ∧ I32` | `null` | Нет общего подтипа |
| `A[] ∧ {x:B}` | `null` | Разные конструкторы |
| `None ∧ I32` | `null` | None ≰ I32 |
| `None ∧ Opt(I32)` | `None` | None ≤ Opt(T) |
| `Opt(A) ∧ Any` | `Opt(A)` | Opt(A) ≤ Any |

## Связь с другими операторами

- **LCA** (`∨`) — дуальный, тотальный. См. `Algebra_LCA.md`.
- **Fit** — `A ≤ B` ⟺ `A ∧ B = A`. См. `Algebra_Fit.md`.
- **Merge** (`⊓` на constraints) — использует GCD для пересечения верхних границ.
  См. `Algebra_Unify.md`.
- **TryAddAncestor** — сужение верхней границы CS: `Anc' = Anc ∧ new_anc`.
  Если null — интервал пуст (ошибка типа).

## Сложность

Аналогична LCA: Primitive × Primitive — O(1), Composite × Composite — O(components × depth),
рекурсивные struct — + O(nesting) коиндуктивный контекст (явный параметр).

## Инварианты

1. **Нижняя грань** (если определена): A ∧ B ≤ A, A ∧ B ≤ B (`Gcd_ResultIsDescendantOfBoth_Primitives`; таблица примитивов = точная GLB независимого оракула — `AlgebraLatticeLawsTest.Gcd_PrimitiveTable_IsGreatestLowerBound`, 23²)
2. **Наибольшая нижняя грань**: C ≤ A ∧ C ≤ B ⟹ C ≤ A ∧ B
3. **Null-propagation**: A ∧ B = null ⟹ ¬∃T: T ≤ A ∧ T ≤ B (`AlgebraLatticeLawsTest.Absorption_LcaOverGcd_FullLattice` — definedness ∧ сверен с оракулом «существует общая нижняя грань»; `Gcd_NullPropagation_FullLattice`)
4. **Связь с Fit**: `A ≤ B` ⟺ `A ∧ B = A` на T (`Gcd_FitsInto_Relationship_Primitives`, `_Composites`)

## Отклонения текущей реализации (см. TicTechnicalDebt)

Закрыто 2026-07-09 (#20): монотонность, null-propagation и Ref-прозрачность покрыты
property-тестами `AlgebraLatticeLawsTest` (полная 23-точечная решётка, GLB-точность —
против независимого оракула порядка); `LcaTestTools.PrimitiveTypes` расширен до полных
23 точек — ассоциативность проверяется на полной таблице (см. §Инварианты).
Открытых отклонений нет.
