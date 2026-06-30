# TIC Type System — Система типов

## Обзор

Система типов NFun — **структурная** (не номинативная), с **неявными числовыми расширениями** и **optional lift**. Типы образуют join-полурешётку с `Any` наверху.

Два класса типов: **примитивы** (атомарные, решённые) и **композиты** (содержат ссылки на другие типы).

## Примитивы

### Конкретные примитивы

| Тип | Описание | Numeric | Comparable |
|-----|----------|---------|------------|
| `Bool` | Логический | нет | нет |
| `Char` | Символ | нет | да |
| `Ip` | IP-адрес (IPv4) | нет | нет |
| `Real` | Вещественное число (IEEE 754 double, ≡ `float64`) или decimal по dialect | да | да |
| `F32` | Float32 (IEEE 754 single, System.Single) | да | да |
| `I64` | Знаковое 64-бит | да | да |
| `I32` | Знаковое 32-бит | да | да |
| `I16` | Знаковое 16-бит | да | да |
| `I8`  | Знаковое 8-бит | да | да |
| `U64` | Беззнаковое 64-бит | да | да |
| `U32` | Беззнаковое 32-бит | да | да |
| `U16` | Беззнаковое 16-бит | да | да |
| `U8` | Беззнаковое 8-бит | да | да |
| `None` | Отсутствие значения | нет | нет |
| `Any` | Top решётки (∀T: T ≤ Any) | нет | нет |

### Абстрактные примитивы

Промежуточные типы, существующие **только во время вывода типов**. Не могут быть финальным типом выражения.

| Тип | Роль |
|-----|------|
| `I96` | LCA знаковых с большими беззнаковыми (`I64 ∨ U64 = I96`) |
| `I48` | LCA: `I32 ∨ U32 = I48` |
| `I24` | LCA: `I16 ∨ U16 = I24` |
| `I12` | LCA: `I8 ∨ U8 = I12` |
| `U48` | GCD: промежуточный беззнаковый |
| `U24` | GCD: промежуточный беззнаковый |
| `U12` | GCD: `I16 ∧ U16 = U12` |
| `U4`  | GCD: `I8 ∧ U8 = U4` (наименьший общий беззнаковый) |

Абстрактные типы **сохраняют информацию** при вычислении LCA/GCD для пар signed/unsigned. Если constraint-интервал коллапсирует к абстрактному типу — это ошибка (типы несовместимы).

### Числовая иерархия

Частичный порядок на числовых типах (A ≤ B = неявная конверсия A → B):

**Беззнаковые**: `U4 ≤ U8 ≤ U12 ≤ U16 ≤ U24 ≤ U32 ≤ U48 ≤ U64 ≤ F32 ≤ Real`

**Знаковые**: `I8 ≤ I12 ≤ I16 ≤ I24 ≤ I32 ≤ I48 ≤ I64 ≤ I96 ≤ F32 ≤ Real`

**Floats**: `F32 ≤ Real` (`Real ≡ float64`). F32 — наименьший общий супер-тип для всех целых, что позволяет `int + f32 → f32` без явного приведения.

**Кросс-конверсии** (unsigned → signed через промежуточные):

| Беззнаковый | Знаковый | LCA |
|-------------|----------|-----|
| `U8` | `I16` | `I16` |
| `U16` | `I16` | `I24` |
| `U32` | `I32` | `I48` |
| `U64` | `I64` | `I96` |
| `U8` | `I32` | `I32` |
| `U16` | `I32` | `I32` |
| `U32` | `I16` | `I48` |

**Принцип**: signed тип вмещает unsigned только если его битовая ширина строго больше. Иначе LCA поднимается на следующий уровень (I24, I48, I96).

### Не-числовые примитивы

`Bool`, `Char`, `Ip` — **несовместимы** друг с другом и с числовыми:

```
Bool ∨ Char = Any
Bool ∨ I32  = Any
Char ∨ Real = Any
```

`None` — специальный: `None ∨ T = Opt(T)` для T ∉ {Any, None}. См. раздел Optional.

## Композитные типы

### Array(T)

Массив с элементами типа T.

- **Ковариантен**: `Array(A) ≤ Array(B)` ⟺ `A ≤ B`
- Пример: `I32[] ≤ I64[]` (потому что `I32 ≤ I64`)
- `Array(A) ∨ Array(B) = Array(A ∨ B)`
- `Array(A) ∧ Array(B) = Array(A ∧ B)`

### Optional(T)

Optional — тип допускающий None. Значение: либо T, либо None.

- **Ковариантен**: `Opt(A) ≤ Opt(B)` ⟺ `A ≤ B`
- **Implicit lift**: `T ≤ Opt(T)` для всех T
- **None lift**: `None ∨ T = Opt(T)` для T ∉ {Any, None}
- **Flatten**: `Opt(Opt(T)) = Opt(T)` (постулат — вложенные optional коллапсируют)
- **Top collapse**: `Opt(Any) = Any`
- `Opt(A) ∨ Opt(B) = Opt(A ∨ B)`
- `Opt(A) ∨ B = Opt(A ∨ B)` (неявный lift B в Optional)

### Fun(A₁, ..., Aₙ → R)

Функциональный тип с n аргументами и возвратом R.

- **Аргументы контравариантны**: `Fun(A→R) ≤ Fun(B→R)` ⟺ `B ≤ A`
- **Возврат ковариантен**: `Fun(A→R₁) ≤ Fun(A→R₂)` ⟺ `R₁ ≤ R₂`
- Функции с **разной арностью несовместимы**: `Fun(A→R) ∨ Fun(B,C→R) = Any`
- `Fun(A→R₁) ∨ Fun(B→R₂) = Fun(A ∧ B → R₁ ∨ R₂)`

### Struct{f₁:T₁, ..., fₙ:Tₙ}

Структурный тип (record) с именованными полями. Типизация — **структурная** (width subtyping).

- **Width subtyping**: `{x:A, y:B} ≤ {x:C}` ⟺ `A ≤ C` (больше полей = подтип)
- **Depth subtyping**: поля ковариантны — `{x:A} ≤ {x:B}` ⟺ `A ≤ B`
- **LCA = пересечение полей**: `{x:A, y:B} ∨ {x:C, z:D} = {x: A ∨ C}`
- **GCD = объединение полей**: `{x:A} ∧ {x:B, y:C} = {x: A ∧ B, y:C}`
- **Пустой struct** `{}` — супертип всех struct
- **Frozen/Mutable**: два режима при выводе типов:
  - **Frozen** (литерал `{x:1, y:true}`): набор полей фиксирован при создании. AddField → ошибка. Pull/Push могут только сужать типы существующих полей.
  - **Mutable** (переменная `s.x`): набор полей расширяемый. Pull/Push добавляют поля при width subtyping (ancestor с полем y → descendant получает поле y). Переход mutable→frozen происходит при Destruction (все поля определены).
  - **Инвариант**: frozen struct отвергает AddField. Mutable struct принимает AddField только в Pull/Push, не в Destruction.
- **Named structs**: `TypeName` — опциональное имя для объявленных типов. Анонимные литералы: `TypeName = null`.

#### Row Polymorphism (IsOpen)

Структурные типы различаются по **открытости**:

- **Closed struct** `{a:T}` (IsOpen=false): точный набор полей. Литералы, LCA-результаты, GCD-результаты. При комбинировании двух closed struct в AddDescendant — поля пересекаются (LCA, стандартное поведение).

- **Open struct** `{a:T, ...}` (IsOpen=true): минимум эти поля, возможно больше. Создаётся SetFieldAccess (`x.a`) и SetSafeFieldAccess (`x?.a`). При комбинировании с другим struct в AddDescendant — поля объединяются (union).

**Правила комбинирования в AddDescendant:**

| Existing | Incoming | Операция | Результат IsOpen |
|----------|----------|----------|------------------|
| Closed | Closed | LCA (пересечение полей) | Closed |
| Open | any | Field union | Closed (если incoming closed) или Open |
| any | Open | Field union | Closed (если existing closed) или Open |

**Мотивация**: `x.a + x.b` означает "x имеет минимум поля a и b". Это два open constraint. Их объединение: "x имеет a И b" = union {a, b}. Массив `[{a=1}, {a=2, b=3}]` — два конкретных значения. Их общий тип: LCA = intersection {a}.

**Где создаются open structs:**
- `SetFieldAccess` — `new StateStruct(isOpen: true)`
- `SetSafeFieldAccess` — `new StateStruct(fields, isFrozen: false, isOpen: true)`

**Где создаются closed structs:**
- Литералы (`SetStructInit`) — `isOpen: false` (default)
- LCA — `isOpen: false` (всегда closed)
- GCD — `isOpen: false` (всегда closed)

**Пропагация IsOpen:**
- `TransformToStructOrNull` — наследует от входного struct
- `MergeStructs` — OR обоих входов
- `GetNonReferenced` — копирует
- `Concretest` — копирует

## Comparable

Единственный type class в системе. Тип comparable если поддерживает `<`, `>`, `<=`, `>=`, `==`, `!=`.

**Comparable типы**:
- Все числовые примитивы (U8...Real, включая абстрактные)
- `Char`
- `Array(Char)` — текст (единственный comparable composite)

**Не comparable**: `Bool`, `Ip`, `None`, `Any`, `Array(T)` (кроме `Array(Char)`), `Opt(T)`, `Fun(...)`, `Struct{...}`.

Comparable — **constraint**, не свойство. Выражается флагом `IsComparable` в ConstraintsState. При LCA: `cmp₁ AND cmp₂` (constraint сужается). При Unify: `cmp₁ OR cmp₂` (constraint объединяется).

## Неявные конверсии

В системе два вида неявных конверсий:

### 1. Числовое расширение (Numeric Widening)

Следует иерархии: если `A ≤ B` в числовой решётке, то значение A неявно конвертируется в B.

Примеры: `U8 → I32`, `I32 → I64`, `U16 → I24`, `I64 → Real`.

Реализация: предвычисленная таблица LCA/GCD, O(1) для любой пары.

### 2. Optional Lift

`T → Opt(T)` — любое значение неявно оборачивается в Optional.

Примеры: `42 → Opt(42)`, `"hello" → Opt("hello")`.

**Обратное невалидно**: `Opt(T) → T` (implicit unwrap) запрещён. Для извлечения значения из Optional нужен явный `??` или `?.`.

## Подтипирование — сводка

| Правило | Формула |
|---------|---------|
| Числовое расширение | `U8 ≤ U16 ≤ ... ≤ Real`, `I16 ≤ I32 ≤ ...` |
| Optional lift | `T ≤ Opt(T)` |
| Array covariance | `A ≤ B ⟹ A[] ≤ B[]` |
| Optional covariance | `A ≤ B ⟹ Opt(A) ≤ Opt(B)` |
| Fun covariance (ret) | `R₁ ≤ R₂ ⟹ (A→R₁) ≤ (A→R₂)` |
| Fun contravariance (args) | `B ≤ A ⟹ (A→R) ≤ (B→R)` |
| Struct width | `{f₁:T₁,...,fₙ:Tₙ,...} ≤ {f₁:T₁,...,fₙ:Tₙ}` |
| Struct depth | `A ≤ B ⟹ {f:A} ≤ {f:B}` |
| None lift | `None ≤ Opt(T)` для всех T |
| Top | `T ≤ Any` для всех T |

## Постулаты

1. **Opt(Opt(T)) = Opt(T)** — Optional не вкладывается
2. **Opt(Any) = Any** — Any уже содержит все значения
3. **Абстрактные типы не материализуются** — промежуточные типы (I24, U12, ...) существуют только при выводе
4. **Нет промежуточных абстрактных composites** — не существует "абстрактного массива" или "абстрактной функции"

## Связь с алгеброй

Операторы (LCA, GCD, FitsInto, Unify, Concretest, Abstractest) работают над этой системой типов. Их определения — в `Algebra.md` и отдельных документах `Algebra_*.md`.
