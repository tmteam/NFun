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

The left operand must be of optional type `T?`. The right operand must be of type `T`.
The result type is `T` (non-optional)

```py
a:int? = 42
b = a ?? 0          # 42

c:int? = none
d = c ?? 0          # 0

e = none ?? 'hello' # 'hello'
```

The `??` operator is short-circuit and right-associative. The fallback is only evaluated if the left side is `none`

```py
a:int? = 42
b = a ?? ___throwError()  # 42 — fallback never evaluated

c:int? = none
d:int? = none
e = c ?? d ?? 42  # equivalent to: c ?? (d ?? 42)
                  # result: 42
```

## Force unwrap operator `!`

The postfix `!` operator extracts the value from an optional. If the value is `none`, a runtime exception is thrown

```
expression!
```

The operand must be of optional type `T?`. The result type is `T` (non-optional)

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

After `?.`, the `none` propagates through subsequent `.` field accesses automatically. You only need one `?.` at the beginning of the chain

```py
data = if(found) {inner = {value = 42}} else none

x = data?.inner.value        # int? — 42 or none (propagates through .inner and .value)
y = data?.inner.value ?? 0   # int  — 42 or 0
```

Explicit `?.` on every level (e.g. `data?.inner?.value`) also works, but is not required when only the root is optional

## Conditional expressions with `none`

The `if-else` expression naturally supports optional types when one or both branches return `none`

```py
y = if(condition) 42 else none       # int?
z = if(condition) none else 'hello'  # text?
w = if(condition) none else none     # none
```

This is the standard way to produce optional values from conditions.
The `else` clause is always required — there is no implicit `else none`

```py
# Multiple branches
result =
    if(x > 100) 'high'
    if(x > 0)   'positive'
    else none                  # text?
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
