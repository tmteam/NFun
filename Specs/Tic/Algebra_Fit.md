# FitsInto — Subtype Relation

## Определение

**FitsInto(A, B)** — предикат: "тип A является подтипом B" (A можно безопасно использовать там, где ожидается B).

Обозначение: **A ≤ B**

Интуиция: "значение типа A можно присвоить переменной типа B без потери информации".

## Связь с LCA и GCD

FitsInto, LCA и GCD — три грани одного отношения порядка:

```
A ≤ B  ⟺  A ∨ B = B  ⟺  A ∧ B = A
```

FitsInto — проверка, LCA/GCD — вычисление. FitsInto дешевле: возвращает bool без аллокаций.

## Алгебраические свойства

| Свойство | Формула |
|---|---|
| Рефлексивность | `A ≤ A` |
| Транзитивность | `A ≤ B ∧ B ≤ C ⟹ A ≤ C` |
| Антисимметричность | `A ≤ B ∧ B ≤ A ⟹ A = B` |
| Top | `∀A: A ≤ Any` |
| Ref-прозрачность | `Ref(A) ≤ B ⟺ A ≤ B` |

Это **частичный порядок** — не все пары сравнимы (`Bool` и `I32` несравнимы).

## Два контекста проверки

FitsInto используется в двух режимах:

### 1. `A ≤ ConcreteType` — проверка конкретного типа

A fits into конкретный тип B если:

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

**Function** — контравариантна по аргументам, ковариантна по возврату:
```
(A₁,...,Aₙ → R₁) ≤ (B₁,...,Bₙ → R₂)
⟺  ∀i: Bᵢ ≤ Aᵢ  ∧  R₁ ≤ R₂
⟺  argsCount равен
```

Контравариантность: функция-подтип **принимает более широкие** аргументы.

### 2. `A ≤ ConstraintsState` — проверка интервала

A fits into CS `[D..Anc, cmp, S]` если:

1. **Ancestor check**: `A ≤ Anc` (если Anc определён)
2. **Comparable check**: если `cmp=true` — A должен быть comparable. Comparable типы: числовые примитивы, Char, arr(Char). Optional, Struct, Fun, None, Bool — НЕ comparable. Если A comparable и примитивен — **достаточно** (descendant check пропускается, т.к. comparable примитив гарантированно лежит в числовом интервале).
3. **Descendant check**: если `D` определён — `CanBeFitConverted(D, A)` (см. ниже)
4. **StructBound check**: если `S` определён — `A` должен быть `StateStruct`, `Fields(A) ⊇ Fields(S)`, и `∀f ∈ S: A.f ≤ S.f` (covariant width subtyping). Это **F-bound check** — call-site arg должен структурно удовлетворять bound'у. См. PushReform.md.

**IsOptional** НЕ проверяется в FitsInto. IsOptional — информация для materialization (Destruction/Finalize), не для проверки совместимости. Preferred — аналогично.

### CanBeFitConverted(D, A)

Вспомогательный предикат: "существует ли тип T такой что D ≤ T и T ≤ A" — совместим ли descendant D с верхней границей A.

| D | A | Проверка |
|---|---|---|
| Primitive | Primitive | `D ≤ A` по таблице |
| Primitive | CS | `CS.Desc != null ∧ D ≤ CS.Desc` |
| CS | любой | рекурсивно по `CS.Descendant` (если есть), иначе true |
| Composite | Composite (тот же конструктор) | покомпонентно |
| Composite | Composite (Optional target) | implicit lift: `D ≤ Opt(X)` → `D ≤ X` |
| Composite | CS | через `CS.Descendant` если composite |
| Composite | другой конструктор | false |
| Any | любой | `Abstractest(A)` |

Примечание: для struct полей используется `CanBeConvertedPessimisticTo` (общий рекурсивный метод), а не `CanBeFitConverted` — это разные функции с разной семантикой.

## Общее правило для composites

Дуально к LCA/GCD: FitsInto вычисляется **покомпонентно** с учётом вариантности:

| Вариантность | Проверка компоненты |
|---|---|
| **Ковариантная** (out) | `Aᵢ ≤ Bᵢ` |
| **Контравариантная** (in) | `Bᵢ ≤ Aᵢ` (обратный порядок) |

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
| `CS(∅) ≤ T` | true | Unconstrained CS fits anything |

## Связь с другими операторами

- **LCA** (`∨`): `A ≤ B ⟺ A ∨ B = B`. См. `Algebra_LCA.md`.
- **GCD** (`∧`): `A ≤ B ⟺ A ∧ B = A`. См. `Algebra_GCD.md`.
- **CanBePessimisticConvertedTo**: примитив ≤ примитив через LCA-таблицу (`A ≤ B ⟺ LCA(A,B) = B`). Только для StatePrimitive.
- **CanBeConvertedPessimisticTo**: общий рекурсивный ≤ для любых типов. Используется в CanBeFitConverted для struct полей.
- **CanBeFitConverted**: проверка существования типа в интервале [D..A].

## Сложность

| Входные типы | Сложность |
|---|---|
| Primitive × Primitive | O(1) |
| Composite × Composite | O(components × depth) |
| Target × ConstraintsState | O(1) + CanBeFitConverted |

## Инварианты

1. **Рефлексивность**: A ≤ A
2. **Транзитивность**: A ≤ B ∧ B ≤ C ⟹ A ≤ C
3. **Антисимметричность**: A ≤ B ∧ B ≤ A ⟹ A = B
4. **Top**: ∀A: A ≤ Any
5. **None lift**: None ≤ Opt(T), None ≤ Any, None ≤ None
6. **Implicit lift**: T ≤ Opt(T)
7. **Ref-прозрачность**: Ref(A) ≤ B ⟺ A ≤ B
8. **Согласованность**: A ≤ B ⟺ A ∨ B = B ⟺ A ∧ B = A
