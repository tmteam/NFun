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

`none` cannot be assigned to a non-optional type, with one exception: `any` is the top type and accepts every value, including `none`

```py
x:int = none  # error. int is not optional
y:any = none  # ok. any accepts everything (it is the top of the type hierarchy)
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

The `?.` operator safely accesses a field of a struct. If the receiver is `none`, the result is `none` instead of a runtime error

```
expression?.fieldName
```

The left operand must be a struct or an optional struct. The result type is the field type wrapped in optional

```py
user = if(found) {name = 'Alice', age = 30} else none

name = user?.name      # text? — 'Alice' or none
age  = user?.age       # int?  — 30 or none
safe = user?.name ?? 'Guest'  # text — 'Alice' or 'Guest'
```

`?.` also accepts a non-optional struct receiver — the safety check is a no-op (the value can't be `none`) but the syntax stays valid. Useful when chaining onto an inline constructor or when the accessed field is itself optional

```py
type n = {v: int, next: n? = none}

# inline constructor — receiver is non-optional `n`, the field `next` is `n?`
y = n{v=1, next=n{v=2}}?.next!.v      # 2

# uniform `?.` chain works on both optional and non-optional segments
d = n{v=1, next=n{v=2, next=n{v=3}}}
y = d?.next?.next!.v                   # 3
```

`?.` on non-struct receivers (Int, Real, Text, Array, …) is rejected

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

## Type narrowing

In branches whose entry condition proves an optional is not `none`, the compiler narrows it from `T?` to `T`. Narrowing is purely syntactic — based on the shape of the condition, not on runtime data flow.

### Basic rule

`x != none` narrows `x` in the then-branch. `x == none` narrows `x` in the else-branch. Operand order is irrelevant — `none != x` works the same way

```py
x:int? = 42
y = if(x != none) x + 1 else 0    # then: x is int → 43
z = if(x == none) 0 else x + 1    # else: x is int → 43
```

A branch where narrowing doesn't apply keeps `T?`

```py
x:int? = 42
y = if(x != none) x else none     # then:int, else:none → int?
```

### Compared to `true`/`false`

`flag == true` (or `== false`) narrows in the **then-branch only** — the equality holding proves `flag` is the literal value, so not `none`. The else-branch is `flag != true`, which is satisfied by both `false` AND `none`, so no narrowing applies. Symmetric for `!=`:

| Operator         | Then-branch (T) | Else-branch (F) |
|------------------|-----------------|-----------------|
| `flag == true`   | {flag}          | ∅               |
| `flag == false`  | {flag}          | ∅               |
| `flag != true`   | ∅               | {flag}          |
| `flag != false`  | ∅               | {flag}          |

```py
flag:bool? = true
y = if(flag == true) flag else false       # then:bool, else:bool literal → bool
```

Three-way pattern match on `bool?` — each branch narrows the variable on the way in:

```py
x:bool? = true
y = if(x == true) 'yes'
    if(x == false) 'no'
    else 'unknown'                          # 'yes' | 'no' | 'unknown' (none)
```

### Boolean combinators

For each condition, narrowing tracks two sets: variables proven not-`none` when the condition is **true** (`T`) and when **false** (`F`)

| Operator | Then-branch (T) | Else-branch (F) |
|---|---|---|
| `a == none` | ∅ | {a} |
| `a != none` | {a} | ∅ |
| `a and b`   | `T(a) ∪ T(b)` | `F(a) ∩ F(b)` |
| `a or b`    | `T(a) ∩ T(b)` | `F(a) ∪ F(b)` |
| `not a`     | `F(a)`        | `T(a)` |

```py
# and: both narrowed in then-branch
y = if(x != none and z != none) x + z else 0       # x:int, z:int

# or: both narrowed in else-branch (neither is none)
y = if(x == none or z == none) 0 else x + z       # x:int, z:int

# not: swap branches
y = if(not(x == none)) x + 1 else 0                # equivalent to x != none
```

De Morgan follows naturally — `not(a == none or b == none)` is `a != none and b != none`.

### Progressive narrowing

Short-circuit `and`/`or` propagates narrowing into their right operand. The same accumulation happens across elif chains — each `WhenFalse` is visible in all subsequent branches

```py
# inside `and`
y = if(x != none and x > 0) x * 2 else 0           # right side sees x:int

# inside `or` (left WhenFalse passes through)
y = if(x == none or x < 0) 0 else x + 1            # right side sees x:int

# elif chain — WhenFalse accumulates
y = if(x == none) -1
    if(x > 10) x                                    # x:int here
    else 0
```

Short-circuit `and` also narrows in plain bool expressions, not only inside `if`

```py
y:int? = 15
x = y != none and y > 12                            # right operand sees y:int
```

### Struct fields and safe access

Direct field access `s.field` narrows the specific field. A safe-access condition `a?.field != none` extracts the root variable (`a`) and narrows it

```py
s = {v = if(...) 42 else none}
y = if(s.v == none) 0 else s.v + 1                  # else: s.v is int

a = if(...) {b = {c = 42}} else none
y = if(a?.b?.c != none) a.b.c + 1 else 0            # a is narrowed in else
```

### Collections

`filterNotNull()` narrows `T?[]` to `T[]`. There is no narrowing through `all`/`any`/`filter`/`map` — use `filterNotNull()` when you need it

```py
arr:int?[] = [1, none, 3]
y = arr.filterNotNull().map(rule it + 1)             # int[] — [2, 4]
```

### What does not narrow

| Pattern | Reason |
|---------|--------|
| `if(myCheck(x))` | Arbitrary predicates are not analysed |
| `s?.age > 18` | `s?.age` is `int?`, not comparable to `int`. Use explicit unwrap or `??` |
| `arr.all(rule it != none)` | Collection operations don't narrow element type — use `filterNotNull()` |
| `x:int? > 0` | Optional is not directly comparable — type error |
| `x != none or true` | Tautological side makes `T = ∅` (intersection) — no narrowing |

Narrowing does not escape its branch — after `if-else` the variable is `T?` again

```py
x:int? = 42
z = if(x != none) x else 0    # ok: x narrowed inside
y = x + 1                     # ERROR: x is still int? here
```

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
