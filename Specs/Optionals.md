# NFun Optional Types (experimental feature)



Optional types allow values to represent either a concrete value or the absence of a value (`none`).

**NOTE**
The feature is experimental and disabled by default. There is no guarantee that its syntax, semantics, or type inference will not change in the future.

## Optional type `T?`

Any type can be made optional by appending `?` to its type identifier.
An optional type `T?` can hold either a value of type `T` or `none`

```py
x:int? = 42        # optional int with a value
y:int? = none      # optional int with no value
z:real? = 1.5      # optional real
t:text? = 'hello'  # optional text
```

Optional can be applied to composite types as well:

```py
a:int?[] = [1, none, 3]    # array of optional ints
b:int[]? = [1, 2, 3]       # optional array of ints
c:int[]? = none             # optional array with no value
d:int?[]? = [1, none, 3]   # optional array of optional ints
```

Note: the `?` binds tightly to the preceding type. `int?[]` means "array of `int?`", not "optional `int[]`".
To make the array itself optional, write `int[]?`

## The `none` literal

`none` is a literal that represents the absence of a value. It can be assigned to any optional type

```py
x:int? = none
y:text? = none
z:bool? = none
```

`none` cannot be assigned to a non-optional type

```py
x:int = none  # error. int is not optional
```

`none` can appear in array literals. The resulting element type becomes optional

```py
a = [1, none, 3]     # int?[] (type inferred)
b:int?[] = [1, none]  # int?[] (type annotated)
```

## Default value

The default value for any optional type `T?` is `none`

```py
y:int? = default  # none
```

## Type conversion rules

### Implicit lift: `T` to `T?`

A value of type `T` can be implicitly converted to `T?`. This means that wherever an optional is expected, a non-optional value can be used

```py
x:int? = 42       # int implicitly lifted to int?
y:int?[] = [1,2]  # each int element implicitly lifted to int?
```

### No implicit unwrap: `T?` to `T`

An optional type `T?` cannot be implicitly converted to `T`.
To extract the value, use the `??` operator or the `!` operator

```py
x:int? = 42
y:int = x     # error. int? is not convertable to int
y:int = x!    # ok. force unwrap
y:int = x??0  # ok. coalesce with default
```

### Covariance

Optional types are covariant: if `A` is convertable to `B`, then `A?` is convertable to `B?`

```py
a:int? = 42
b:real? = a   # int? is convertable to real?
```

## Null coalesce operator `??`

The `??` operator returns the left operand if it is not `none`, otherwise returns the right operand

```
expression ?? fallback
```

The left operand is of type `U?` (non-optional values are implicitly lifted via `U → U?`).
The right operand can be any type `V`. The result type is `LCA(U, V)` — the lowest common ancestor of the unwrapped left element type and the right operand type

```py
a:int? = 42
b = a ?? 0          # int — 42

c:int? = none
d = c ?? 0          # int — 0

e = none ?? 'hello' # text — 'hello'
```

The right operand can itself be optional:

```py
a:int? = 42
b:int? = none
c = a ?? b          # int? — 42
d = b ?? a          # int? — 42

e:real? = 1.5
f = a ?? e          # real? — LCA(int, real?) = real?
```

The `??` operator is short-circuit and right-associative. The fallback is only evaluated if the left side is `none`

```py
a:int? = 42
b = a ?? ___throwError()  # 42 — fallback never evaluated

c:int? = none
d:int? = none
e = c ?? d ?? 42  # equivalent to: c ?? (d ?? 42)
                  # d ?? 42 : int, then c ?? int : int
                  # result: 42
```

## Force unwrap operator `!`

The postfix `!` operator extracts the value from an optional. If the value is `none`, a runtime exception is thrown

```
expression!
```

The operand is of type `T?` (non-optional values are implicitly lifted via `T → T?`).
The result type is `T` (non-optional)

```py
a:int? = 42
b = a!           # 42

c:int? = none
d = c!           # runtime error: Force unwrap of none value
```

## Safe field access `?.`

The `?.` operator safely accesses a field of an optional struct. If the struct is `none`, the result is `none` instead of a runtime error

```
expression?.fieldName
```

The left operand must be of optional struct type. The result type is the field type wrapped in optional

```py
user = if(found) {name = 'Alice', age = 30} else none

name = user?.name      # text? — 'Alice' or none
age  = user?.age       # int?  — 30 or none
safe = user?.name ?? 'Guest'  # text — 'Alice' or 'Guest'
```

After `?.`, `none` propagates through the entire chain — both field accesses and method calls. You only need one `?.` at the beginning (TypeScript-style)

```py
data = if(found) {inner = {value = 42}} else none

x = data?.inner.value        # int? — 42 or none
y = data?.inner.value ?? 0   # int  — 42 or 0
```

Method calls also propagate:
```py
arr:int[]? = if(hasData) [3,1,2] else none

arr?.sort().reverse() ?? []        # int[] — [3,2,1] or []
arr?.sort().reverse().count() ?? 0 # int   — 3 or 0
arr?.count() ?? 0                  # int   — 3 or 0
```

Explicit `?.` on every level (`data?.inner?.value`) also works but is not required

## Safe array access `?[`

The `?[` operator safely indexes into an optional array. Returns `none` if the array is `none` OR if the index is out of bounds

```
expression?[index]
```

```py
arr:int[]? = if(hasData) [10, 20, 30] else none

arr?[0]          # int? — 10 or none (if arr is none)
arr?[99]         # int? — none (out of bounds)
arr?[-1]         # int? — none (negative index)
arr?[1] ?? 0     # int  — 20 or 0
```

This is fully safe — `?[` never throws a runtime error. Compare with regular `[]` which throws on `none` array or out-of-bounds index.

Non-optional arrays can also use `?[` for bounds-safe access:

```py
arr = [10, 20, 30]
arr?[99] ?? 0    # int — 0 (out of bounds returns none, coalesced to 0)
```

## Type Narrowing (сужение типов)

Компилятор анализирует условия проверки на `none` и автоматически сужает optional-тип переменной в соответствующей ветке. Если условие гарантирует, что переменная не `none`, то в теле ветки переменная имеет тип `T` вместо `T?`.

### Базовое правило

`x != none` сужает `x` в then-ветке. `x == none` сужает `x` в else-ветке.

```py
x:int? = 42

y = if(x != none) x + 1 else 0    # then: x это int → 43
z = if(x == none) 0 else x + 1    # else: x это int → 43
```

В ветке, где сужение НЕ применяется, переменная остаётся `T?`:

```py
x:int? = 42
y = if(x != none) x else none     # then: int, else: none → результат int?
```

Порядок `none` не важен — `none != x` эквивалентно `x != none`:

```py
x:int? = 42
y = if(none != x) x + 1 else 0   # то же самое, x сужается в then-ветке
```

### Распознаваемые паттерны

`NarrowingAnalyzer` — чисто синтаксический анализ условий. Для каждого условия вычисляются два множества: `WhenTrue` (переменные, гарантированно не-`none` при истинности условия) и `WhenFalse` (при ложности).

#### `x == none` / `x != none`

```
x == none  →  WhenTrue={},  WhenFalse={x}
x != none  →  WhenTrue={x}, WhenFalse={}
```

```py
x:int? = 42
y = if(x != none) x + 1 else 0       # WhenTrue={x} → then-ветка: x это int
z = if(x == none) 0 else x + 1       # WhenFalse={x} → else-ветка: x это int
```

#### `x == true` / `x == false` (bool?)

Сравнение с `true` или `false` доказывает, что переменная не `none` (потому что `none != true` и `none != false`). Сужение применяется в **обеих** ветках:

```
x == true   →  WhenTrue={x}, WhenFalse={x}
x != true   →  WhenTrue={x}, WhenFalse={x}
x == false  →  WhenTrue={x}, WhenFalse={x}
x != false  →  WhenTrue={x}, WhenFalse={x}
```

```py
flag:bool? = true
y = if(flag == true) flag else false        # обе ветки: flag это bool
z = if(flag == false) not flag else false   # обе ветки: flag это bool
```

Трёхсторонний pattern match для `bool?`:

```py
x:bool? = true
y = if(x == true) 'yes'
    if(x == false) 'no'
    else 'unknown'                          # 'yes', 'no', или 'unknown' (none)
```

#### `and` — объединение WhenTrue, пересечение WhenFalse

```
(a and b)  →  WhenTrue  = union(a.WhenTrue, b.WhenTrue)
              WhenFalse = intersect(a.WhenFalse, b.WhenFalse)
```

Если оба условия проверяют `!= none`, то в then-ветке все переменные сужены:

```py
x:int? = 3
z:int? = 4
y = if(x != none and z != none) x + z else 0   # then: оба int → 7
```

**Прогрессивное сужение внутри `and`**: `and` является short-circuit — после проверки `x != none` правая часть условия уже видит `x` как `int`:

```py
x:int? = 42
y = if(x != none and x > 0) x * 2 else 0
#                   ^^^^^
#                   x уже int, сравнение с int допустимо
```

Три переменные:

```py
a:int? = 1
b:int? = 2
c:int? = 3
y = if(a != none and b != none and c != none) a + b + c else 0  # 6
```

#### `or` — пересечение WhenTrue, объединение WhenFalse

```
(a or b)  →  WhenTrue  = intersect(a.WhenTrue, b.WhenTrue)
             WhenFalse = union(a.WhenFalse, b.WhenFalse)
```

В then-ветке сужаются только переменные, проверяемые в **обеих** частях `or` (пересечение). В else-ветке (ни одно из условий не выполнено) сужаются все упомянутые переменные (объединение):

```py
# or: else-ветка гарантирует, что ОБА условия ложны → оба не-none
a:int? = 3
b:int? = 4
y = if(a == none or b == none) 0 else a + b    # else: оба int → 7
```

```py
# or: then-ветка — только x (присутствует в обеих частях)
x:int? = 42
z:int? = 1
y = if(x != none or x != none) x else 0
# z НЕ сужается — упомянут только в одной стороне
```

**Прогрессивное сужение внутри `or`**: `or` является short-circuit — если левая часть `x == none` ложна (то есть `x` не `none`), правая часть видит `x` как сужённый:

```py
x:int? = 42
y = if(x == none or x < 0) 0 else x + 1
#                   ^^^^^
#                   x уже int (left-side WhenFalse={x})
```

#### `not` — инверсия (swap WhenTrue/WhenFalse)

```
not(a)  →  WhenTrue = a.WhenFalse,  WhenFalse = a.WhenTrue
```

```py
x:int? = 42
y = if(not(x == none)) x + 1 else 0    # эквивалент x != none → 43
z = if(not(x != none)) 0 else x + 1    # эквивалент x == none → 43
```

Двойное отрицание:

```py
x:int? = 42
y = if(not(not(x != none))) x else 0   # 42 (отрицания уничтожаются)
```

#### De Morgan

Комбинация `not` + `and`/`or` работает по законам Де Моргана:

```py
# not(a == none or b == none) = a != none and b != none
x:int? = 10
z:int? = 20
y = if(not(x == none or z == none)) x + z else 0   # 30

# not(a != none and b != none) = a == none or b == none
y = if(not(x != none and z != none)) 0 else x + z  # 30

# De Morgan + progressive comparison
a:int? = 5
b:int? = 3
y = if(not(a == none or b == none) and a > b) a - b else 0  # 2
```

### Multi-elif: прогрессивное сужение

При нескольких `if`/`elif`-ветках `WhenFalse` каждого условия **накапливается**. Если первая ветка проверяет `x == none`, то все последующие ветки (и else) видят `x` как сужённый тип:

```py
x:int? = 42
y = if(x == none) -1       # WhenFalse={x} → далее x это int
    if(x > 10) x           # x уже int, сравнение допустимо
    else 0
```

Множественные none-проверки сужают разные переменные:

```py
x:int? = 3
z:int? = 4
y = if(x == none) -1       # WhenFalse={x}
    if(z == none) -2       # WhenFalse={x,z} (накопление)
    else x + z             # оба int → 7
```

Длинные цепочки elif с вычислениями над сужёнными переменными:

```py
x:int? = 75
y = if(x == none) 0
    if(x < 25) 1           # x уже int
    if(x < 50) 2
    if(x < 100) 3          # → 3
    else 4
```

### Сужение полей структур

Анализатор распознаёт прямой доступ к полю `s.field` и сужает конкретное поле:

```py
s = {v = if(true) 42 else none}
y = if(s.v == none) 0 else s.v + 1     # else: s.v это int → 43
```

Multi-elif с полями:

```py
s = {v = if(true) 42 else none}
y = if(s.v == none) -1
    if(s.v > 100) 100       # s.v уже int
    else s.v                 # → 42
```

Два поля структуры:

```py
s = {a = if(true) 1 else none, b = if(true) 2 else none}
y = if(s.a == none or s.b == none) 0 else s.a + s.b    # 3
```

### Сужение через safe access (`?.`)

Если условие содержит safe access (`a?.field != none`), анализатор извлекает **корневую переменную** (`a`) и сужает её:

```py
a = if(true) {b = {c = 42}} else none
y = if(a?.b?.c != none) a.b.c + 1 else 0    # a сужён → 43

user = if(true) {name = 'hello world'} else none
y = if(user?.name != none) user.name.count() else 0   # 11
```

После safe access + bool-литерал:

```py
s = if(true) {flag = true} else none
y = if(s?.flag == true) 1 else 0    # s сужён → 1
```

### Прогрессивное сужение внутри `and` (вне if)

Short-circuit `and` обеспечивает сужение и в обычных bool-выражениях (не только в if-условиях):

```py
y:int? = 15
x = y != none and y > 12   # y > 12 видит y как int → true
```

### Сужение коллекций

`filterNotNull()` (синоним: `compact()`) сужает `T?[]` в `T[]`. После вызова элементы гарантированно не-`none`:

```py
arr:int?[] = [1, none, 3]
cleaned = arr.filterNotNull()              # int[] — [1, 3]
y = cleaned.map(rule it + 1)              # int[] — [2, 4]
```

```py
arr:int?[] = [none, 3, none, 1, none, 2]
y = arr.filterNotNull().sort()             # int[] — [1, 2, 3]
```

Полный конвейер:

```py
arr:int?[] = [none, 1, none, 2, none, 3]
y = arr.filterNotNull()
       .map(rule it * it)
       .fold(rule it1 + it2)               # 1 + 4 + 9 = 14
```

Фильтрация с сужением внутри лямбды (паттерн `not(it == none or ...)`):

```py
items:int?[] = [1, none, -2, 3, none, -1]
y = items.filter(rule not(it == none or it < 0))   # [1, 3]
```

### Что НЕ сужает

| Паттерн | Причина |
|---------|---------|
| `if(myCheck(x))` | Произвольные предикаты не анализируются |
| `s?.age > 18` | `s?.age` возвращает `int?`, а `int?` несравним с `int`. Нужен explicit unwrap или `??` |
| `arr.all(rule it != none)` | Коллекционные операции (`all`, `any`, `filter`, `map`) не сужают тип элемента. Используйте `filterNotNull()` |
| `x:int? > 0` | Optional нельзя сравнивать напрямую — ошибка типов |
| `x != none or true` | `or true` делает условие всегда истинным, `WhenTrue = intersect({x},{}) = {}` — нет сужения |
| Scope leak | Сужение не выходит за пределы ветки. После if-else переменная снова `T?` |

```py
x:int? = 42
z = if(x != none) x else 0    # ok: x сужён внутри
y = x + 1                     # ОШИБКА: x всё ещё int? за пределами if
```

### Механизм реализации

1. **`NarrowingAnalyzer`** — чисто синтаксический анализ условия, возвращает `Result(WhenTrue, WhenFalse)` — множества имён переменных (или путей полей `s.age`), гарантированно не-`none` при истинности/ложности условия
2. **TIC: `SetNarrowedVariable`** — создаёт алиас с развёрнутым типом. Для переменной `x: opt(T)` создаётся узел `x': T` и ограничение `merge(x, opt(x'))`
3. **ExpressionBuilder** — в сужённой области использует `TypeOverrideNode`, подменяющий тип переменной на сужённый

For operator precedence of `?.`, `??`, `!` see **Operators.md**


## Complete example

```py
# Working with optional data
scores:int?[] = [95, none, 82, none, 71]

# Replace missing scores with zero using map and coalesce
filled = scores.map(rule it ?? 0)   # int[] — [95, 0, 82, 0, 71]

# Optional struct from conditional
user = if(loggedIn) {name = userName, level = 5} else none

greeting = user?.name ?? 'Guest'    # text — user name or 'Guest'
level    = user?.level ?? 0         # int  — level or 0

# Force unwrap when you are certain the value exists
config:int? = 42
value = config!                     # int — 42 (throws if none)
```
