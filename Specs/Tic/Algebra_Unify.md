# Unify — Пересечение ограничений

## Определение

**Unify(A, B)** — наиболее общее описание типа, подходящее и для A, и для B одновременно. Если совместного описания нет — null.

Обозначение: **A ⊓ B**

В алгебре есть **ровно один** оператор пересечения ограничений: на домене C (ConstraintsState) `Unify(CS, CS)` **определяется** как Merge — полные правила в `Algebra_Merge.md`. Настоящий файл задаёт T-часть ⊓ (конкретные типы и composites) и смешанные клетки.

Все четыре бинарных оператора работают на **одном домене** (типы + ограничения), но задают разные вопросы:

| Оператор | Вопрос |
|---|---|
| LCA (∨) | Какой тип **покроет** оба? |
| GCD (∧) | Какой тип **влезет** в оба? |
| FitsInto (≤) | **Подходит** ли A туда, где ожидается B? |
| Unify (⊓) | **Пересечение ограничений** для одной переменной |

## Пример: четыре оператора на ConstraintsState

Дано: `C₁ = [U8..Real]`, `C₂ = [I16..I64]`

```
                        Real ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ Anc C₁
                         |
                        I64 ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  Anc C₂
                         |
                    ┌────┤
                    │   I32
                    │    │      ← область C₁ ∩ C₂
                    │   I16 ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  Desc C₂
                    │    │
                    └────┤
                         │
                        U8 ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ Desc C₁
```

| Оператор | Результат | Что делает |
|---|---|---|
| `C₁ ∨ C₂` (LCA) | `[I16..] ` | desc = LCA(U8, I16) = I16. Расширяет нижнюю границу |
| `C₁ ∧ C₂` (GCD) | `[..I64]` | anc = GCD(Real, I64) = I64. Сужает верхнюю границу |
| `C₁ ≤ C₂` (Fit) | false | U8 ≤ I16? нет. C₁ не подтип C₂ |
| `C₁ ⊓ C₂` (Unify) | `[I16..I64]` | desc = LCA(U8,I16) = I16, anc = GCD(Real,I64) = I64. Пересечение |

Unify использует **LCA для нижней границы** и **GCD для верхней** — получает пересечение интервалов. Если пересечение пусто (`¬(desc ≤c anc)` — несравнимость тоже опустошает интервал) → null.

## Отличие от LCA и GCD

| | LCA (∨) | GCD (∧) | Unify (⊓) |
|---|---|---|---|
| Вопрос | "что вмещает оба?" | "что влезает в оба?" | "одна переменная для обоих?" |
| Для интервалов | Расширяет desc | Сужает anc | Пересекает: desc=∨, anc=∧ |
| Для concrete | Наименьший супертип | Наибольший подтип | Совместимость |
| `I32 op I64` | `I64` | `I32` | `null` |
| `{x,y} op {x}` | `{x}` | `{x,y}` | `null` |

Почему `I32 ⊓ I64 = null` (хотя `I32 ∧ I64 = I32`): одна переменная не может **быть** одновременно I32 и I64. GCD даёт наибольший общий **подтип** — это другой вопрос.

Почему `{x,y} ⊓ {x} = null` (хотя `{x,y} ≤ {x}`): struct с 2 полями и struct с 1 полем — **разные типы**. Subtyping говорит что один можно использовать **вместо** другого, но это не один тип.

## Частичность

Unify может вернуть **null** — ограничения несовместимы. Тотальность не гарантирована (в отличие от LCA).

## Алгебраические свойства

| Свойство | Формула | Статус |
|---|---|---|
| Идемпотентность | `A ⊓ A = A` | `Unify_Idempotent` (AlgebraInvariantsTest) |
| Коммутативность | `A ⊓ B = B ⊓ A` | `Unify_Symmetry` (AlgebraInvariantsTest); P-ось — `Merge_Commutative_IncludingPreferredAxis` (AlgebraMergeUnifyLawsTest, #14); TypeName — НЕВЕРИФИЦИРОВАНА (планируется) |
| Ассоциативность | `(A ⊓ B) ⊓ C = A ⊓ (B ⊓ C)` | **ОПРОВЕРГНУТА** на C-фрагменте (TicTechnicalDebt #22): drop-order P-оси + mid-fold collapse через ячейку T ⊓ CS (U2/#16). `Merge_Associative_OverCsTriples` [Ignore]-pinned; фрагмент без хинтов/композитных D — `Merge_Associative_PrimitiveIntervalFragment` (активен) |
| Top | `Any ⊓ A = A` | `Unify_AnyIsCompatibleWithAll` (AlgebraInvariantsTest) |

## Правила

### Any

Any совместим с любым типом — не ограничивает:

```
Any ⊓ A = A
```

### Примитивы

Одна переменная не может быть двумя разными примитивами:

```
P ⊓ P = P        (один тип)
P ⊓ Q = null      (P ≠ Q: разные примитивы несовместимы)
```

Тесты: `Unify_SamePrimitiveReturnsSelf`, `Unify_DifferentPrimitivesReturnNull` (AlgebraInvariantsTest).

### None и Optional

None — единственное значение, принадлежащее любому Optional типу:

```
None ⊓ None     = None
None ⊓ Opt(T)   = None         (None ∈ Opt(T) → совместимы, переменная = None)
None ⊓ T        = null          (T non-optional: None ∉ T)
Opt(A) ⊓ Opt(B) = Opt(A ⊓ B)  (рекурсивно по элементу)
Opt(A) ⊓ B      = null          (B non-optional, B ≠ None, B ≠ Any)
```

### Составные типы

Конструкторы должны совпадать. Компоненты — рекурсивный Unify:

```
F<A₁,...> ⊓ F<B₁,...> = F<A₁ ⊓ B₁, ...>
F<...>    ⊓ G<...>    = null                  (разные конструкторы)
```

Если Unify любой компоненты = null → весь результат null.

Вариантность **не применяется** (в отличие от LCA/GCD): Unify проверяет совместимость, а не подтиповое отношение. Каждая компонента должна быть совместима сама с собой.

Примеры:

- **Array**: `A[] ⊓ B[] = (A ⊓ B)[]` — `Unify_ArrayRecursive` (AlgebraInvariantsTest)
- **Function**: `(A→R₁) ⊓ (B→R₂) = (A ⊓ B) → (R₁ ⊓ R₂)`. ArgsCount различен → null.

### Struct

Наборы полей должны **совпадать** (exact match). Типы полей — рекурсивный Unify:

```
{x:A, y:B} ⊓ {x:C, y:D} = {x: A ⊓ C, y: B ⊓ D}
{x:A, y:B} ⊓ {x:C}      = null     (разные наборы полей)
{a:A, b:B} ⊓ {b:C, c:D} = null     (наборы {a,b} ≠ {b,c})
```

Тесты: `Unify_StructSameFields`, `Unify_StructDifferentFieldTypes` (AlgebraInvariantsTest).

**MutableStruct** — другой конструктор:

```
MutStruct ⊓ Struct    = null                              (разные конструкторы)
MutStruct ⊓ MutStruct = пополевой ⊓ при exact match набора полей
```

**Идентичность результата** (метаданные struct, транспорт-правила):

- `TypeName`: `∅ → имя другого; равные → имя; разные → ∅` (анонимный struct). Коммутативность на этой оси НЕВЕРИФИЦИРОВАНА (планируется);
- `IsOptionalSourced`: OR.

Сравнение struct через все операторы:

| | LCA (∨) | GCD (∧) | Unify (⊓) |
|---|---|---|---|
| Поля | Пересечение имён | Объединение имён | Точное совпадение |
| `{x,y} op {x}` | `{x: ∨}` | `{x: ∧, y}` | `null` |
| Интуиция | "что общего?" | "всё вместе" | "один тип?" |

Exact match — правило для **решённых** struct-типов. Для struct как **ограничения** (StructBound S на CS) meet — объединение полей (теорема PT-F): «минимум поля S₁» ∧ «минимум поля S₂» = «минимум поля S₁ ∪ S₂». Это ось S оператора Merge, см. `Algebra_Merge.md` §Оси — не путать с ⊓ решённых struct'ов.

## ConstraintsState

```
CS₁ ⊓ CS₂  ≝  Merge(CS₁, CS₂)
```

Сводка (полные правила, коллапс и Preferred-транспорт — `Algebra_Merge.md`):

```
[D₁..A₁, cmp₁, opt₁, S₁, P₁] ⊓ [D₂..A₂, cmp₂, opt₂, S₂, P₂] =
    D   = D₁ ∨ D₂            (LCA — поднимает нижнюю границу)
    A   = A₁ ∧ A₂            (GCD — опускает верхнюю; не определён → null)
    cmp = cmp₁ OR cmp₂
    opt = opt₁ OR opt₂
    S   = S₁ ∪ S₂            (объединение полей, PT-F)
    P   = коммутативное LCA-правило hint'ов
    null ⟺ интервал пуст: ¬(D ≤c A)
```

Интуиция для desc = LCA: нижняя граница — наиболее **конкретный** допустимый тип. LCA двух нижних границ даёт более **общую** (высокую) нижнюю границу → интервал сужается снизу.

Тесты: `Unify_ConstrainsIntersection`, `Unify_DisjointConstrains` (AlgebraInvariantsTest); интервальное ядро — `Intersect_*` (ConstraintsAlgebraTest).

### ConcreteType ⊓ CS

Правило (#16, единый satisfaction-предикат — `Algebra_Fit.md`):

```
T ⊓ C = T      если T satisfies C: opt-ось (None / opt(X)-развёртка / implicit lift),
                 интервал [D..A′], cmp, S — предикат FitsInto(T, C)
        null    иначе
```

Opt-ось: lift-принятие плоского T (`T ⊓ C? = T`) Sat-корректно, потому что
обязательство «узел обязан принять None» — стадийное (Destruction
`WrapAncestorInOptional` оборачивает при материализации); метаданные C (opt-флаг на
решённом T) этим путём не переживают ⊓ — остаток #22 (collapse-семейство).

Тесты: `Unify_PrimitiveFitsInConstrains`, `Unify_PrimitiveOutsideConstrains`, `Unify_EmptyConstrainsAcceptsAll` (AlgebraInvariantsTest); opt-lift в ячейке — `Merge_TCsCell_OptionalLift_MidFoldCollapse_Associates` (AlgebraMergeUnifyLawsTest).

Частный случай: `None ⊓ C` идёт тем же путём — `None ∈ Sat(C)` → None (например `None ⊓ CS(∅) = None`); при заданном D или cmp → null.

## Граничные случаи

| Случай | Результат | Почему |
|---|---|---|
| `A ⊓ A` | `A` | Идемпотентность |
| `A ⊓ Any` | `A` | Any не ограничивает |
| `I32 ⊓ I64` | `null` | Разные примитивы |
| `I32[] ⊓ I64[]` | `null` | Элементы несовместимы |
| `{x,y} ⊓ {x}` | `null` | Разные наборы полей |
| `MutStruct{x} ⊓ Struct{x}` | `null` | Разные конструкторы |
| `None ⊓ Opt(T)` | `None` | None ∈ Opt(T) |
| `None ⊓ CS(∅)` | `None` | None ∈ Sat(CS(∅)) |
| `[U8..Real] ⊓ [I16..I64]` | `[I16..I64]` | Пересечение непусто |
| `[U8..I32] ⊓ [I64..Real]` | `null` | Пересечение пусто |

## Связь с другими операторами

- **Merge**: `Unify(CS, CS) ≝ Merge` — см. `Algebra_Merge.md`
- **LCA** (`∨`): ⊓ на CS использует LCA для desc
- **GCD** (`∧`): ⊓ на CS использует GCD для anc
- **FitsInto** (`≤`): `T ⊓ C ≠ null ⟺ T ∈ Sat(C)` (satisfaction-предикат)

## Инварианты

1. **Идемпотентность**: A ⊓ A = A — `Unify_Idempotent`
2. **Коммутативность**: A ⊓ B = B ⊓ A — `Unify_Symmetry` (оси P/TypeName — см. §Свойства)
3. **Ассоциативность**: (A ⊓ B) ⊓ C = A ⊓ (B ⊓ C) — на C-домене ОПРОВЕРГНУТА по модулю хинтов и collapse-семейства (см. `Algebra_Merge.md` §Законы, #22); hint-free non-collapsing фрагмент верифицирован — `Merge_Associative_HintFree_NonCollapsing_OverCsTriples`
4. **Top**: Any ⊓ A = A — `Unify_AnyIsCompatibleWithAll`
5. **Совместимость**: если A ⊓ B = C и C конкретен, то C ≤ A и C ≤ B (в constraint-sense)
6. **Struct**: A ⊓ B = null если наборы полей различаются — `Unify_StructSameFields`, `Unify_StructDifferentFieldTypes`
7. **Примитивы**: P ⊓ Q = null для P ≠ Q — `Unify_DifferentPrimitivesReturnNull`

## Отклонения текущей реализации (см. TicTechnicalDebt)

Открытых отклонений нет.

Закрыто 2026-07-09 (#13): U1 — `UnifyOrNull(CS,CS)` теперь ≝ `MergeOrNull`
(opt = OR, P = коммутативное правило; оси переживают унификацию —
`Unify_*_SurvivesUnification`, `Unify_OnConstraints_EqualsMerge_Matrix`).
Закрыто 2026-07-09 (#16): U2 — клетка `T ⊓ CS` использует единый
satisfaction-предикат (S + opt-лифт, `Algebra_Fit.md`); соответствующий корень
опровержения ассоциативности устранён (64 контрпримера, sweep-diff против c6cd0edc).
Метаданные-остаток (opt-флаг не переживает возврат решённого T) — #22.
Закрыто 2026-07-09 (#12/#13/#16): U3 — S-ось транспортируется `Merge`
(`S = S₁ ∪ S₂`, ownerless GcdBound) и проверяется в ячейке `T ⊓ CS` предикатом.
