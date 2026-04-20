# TIC Graph — Граф ограничений

## Что это

TIC Graph — направленный граф, в котором:
- **Узлы** — типовые переменные с состоянием (описанием типа)
- **Рёбра** двух видов: constraint edges и component links

Граф строится из выражения и представляет **систему ограничений** на типы.

## Узлы

Каждый узел имеет:
- **Состояние** — описание типа (конкретный, интервал, ссылка, composite)
- **Список ancestors** — constraint edges к узлам-предкам

Три вида узлов (по происхождению):
- **Named** — переменные выражения (входы/выходы)
- **Syntax** — подвыражения AST
- **TypeVariable** — генерики и внутренние переменные

Вид влияет только на привязку к AST. Алгебраически все узлы равноценны.

## BNF — грамматика типов

Формальная грамматика типовых выражений в TIC:

```bnf
State     ::= Type | Constraints | RefTo

Type      ::= Primitive | Composite
Primitive ::= 'Bool' | 'Char' | 'Ip'
            | 'Real' | 'I96' | 'I64' | 'I48' | 'I32' | 'I24' | 'I16'
            | 'U64' | 'U48' | 'U32' | 'U24' | 'U16' | 'U12' | 'U8'
            | 'Any' | 'None'

Composite ::= Array | Optional | Fun | Struct
Array     ::= 'Array(' Node ')'
Optional  ::= 'Opt(' Node ')'
Fun       ::= '(' Node* '→' Node ')'
Struct    ::= '{' FieldDef (',' FieldDef)* '}'
FieldDef  ::= Name ':' Node

Constraints ::= '[' Desc? '..' Anc? Flags ']'
Desc      ::= Type
Anc       ::= Primitive
Flags     ::= ('opt')? ('cmp')? ('pref=' Primitive)?

RefTo     ::= 'Ref(' Node ')'

Node      ::= <узел графа, содержащий State>
Name      ::= <идентификатор поля>
```

**Примечания**:
- `Node` — не тип, а узел графа. Composite содержит ссылки на узлы, не на типы напрямую.
- `Anc` (верхняя граница) всегда `Primitive` — composites не бывают ancestor.
- `Desc` (нижняя граница) может быть `Composite` (например, `arr(I32)`).
- `Flags` в Constraints: `opt` = IsOptional, `cmp` = IsComparable, `pref` = Preferred (hint).

## Состояния узлов

### Primitive

Базовый тип, полностью определён: I32, Real, Bool, Char, None, Any.

Всегда решён.

### Constraints[D..A, opt, cmp]

Нерешённое состояние — интервал допустимых типов.

- **Desc** (D): нижняя граница. Может быть ∅.
- **Anc** (A): верхняя граница. Может быть ∅.
- **IsOptional**: тип допускает None
- **IsComparable**: тип должен поддерживать сравнение
- **Preferred**: hint для выбора при неоднозначности (не участвует в алгебре)

`[D..A]` означает: тип T такой что `D ≤ T ≤ A`.

Всегда нерешён.

### Composite состояния

Composite — состояние, содержащее **ссылки на другие узлы** графа как компоненты:

| Состояние | Компоненты | Вариантность |
|---|---|---|
| **Array(elem)** | elem — узел элемента | ковариантен |
| **Optional(elem)** | elem — узел элемента | ковариантен |
| **Fun(a₁...aₙ, ret)** | aᵢ — узлы аргументов, ret — узел возврата | args: контравариантны, ret: ковариантен |
| **Struct{f₁:n₁, ...}** | nᵢ — узлы полей | ковариантны |

**Решённость composite**: composite решён тогда и только тогда, когда **все** его компоненты решены.

Примеры:
- `Array(узел с состоянием I32)` — решён (компонент решён)
- `Array(узел с состоянием Constraints[U8..Real])` — **не решён** (компонент — интервал)
- `Struct{x: узел I32, y: узел Constraints[..]}` — **не решён** (поле y не решено)
- `Optional(узел с состоянием Constraints[..])` — **не решён**

### RefTo(N)

Ссылка: "этот узел = узел N". Прозрачна для операторов.

Не решён (разыменовывается до состояния целевого узла).

## Два вида рёбер

### 1. Constraint edges (ancestor)

`D →constraint A` означает: **D ≤ A** (тип D — подтип типа A).

Это **ограничение**, которое должно быть удовлетворено при решении.

Создаются при построении графа:
- **Определение** `y = expr`: expr →constraint y
- **Использование переменной**: usage →constraint x
- **Вызов функции** `f(arg)`: arg →constraint paramType
- **if-else**: branch →constraint result

### 2. Component links (structural)

`Parent →component Child` означает: Child — **часть** composite-состояния Parent.

Это **структурная зависимость**, не ограничение. Тип Parent определяется типами его компонент.

Создаются при установке composite-состояния:
- `Array(T)`: узел массива →component узел T
- `Optional(T)`: узел optional →component узел T
- `Fun(a₁...aₙ → r)`: узел функции →component каждый aᵢ и r
- `Struct{f₁:n₁}`: узел struct →component каждый nᵢ

### Разница

| | Constraint edge | Component link |
|---|---|---|
| Семантика | D ≤ A (ограничение) | T — часть F (структура) |
| Направление | потомок → предок | parent → child |
| Количество | узел может иметь много ancestors | composite имеет фиксированное число компонент |
| Роль | определяет **что должно быть** | определяет **из чего состоит** |

## Пример

Выражение: `y = x + 1`

```
Узлы:             Состояния:
  x (Named)       Constraints[∅..∅]
  1 (Syntax)      Constraints[U8..Real, pref=I32]
  + (Syntax)      RefTo(T)
  y (Named)       Constraints[∅..∅]
  T (TypeVar)     Constraints[U24..Real]

Constraint edges (≤):
  x →constraint T
  1 →constraint T
  + →constraint y

Component links: нет (нет composite состояний в этом примере)
```

Пример с composite: `y = [x, 1]`

```
Узлы:             Состояния:
  x (Named)       Constraints[∅..∅]
  1 (Syntax)      Constraints[U8..Real, pref=I32]
  y (Named)       Array(E)                ← composite!
  E (TypeVar)     Constraints[∅..∅]       ← элемент массива

Constraint edges:
  x →constraint E
  1 →constraint E
  [x,1] →constraint y

Component links:
  y →component E      (Array содержит узел элемента)
```

## Свойства

1. **Constraint edges ацикличны** (DAG)
2. **Component links формируют деревья** (composite → компоненты, без циклов, кроме рекурсивных struct)
3. **Состояния мутабельны**: сужаются по мере решения (Constraints → конкретный тип)
4. **RefTo прозрачен**: разыменовывается, не является ни constraint ни component
5. **Решённость рекурсивна**: composite решён ⟺ все компоненты решены
