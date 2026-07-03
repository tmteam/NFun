# NFun Named Types

Named types give names to existing types. A named type is a transparent alias — fully interchangeable with its underlying type.

## Type Alias

Any type can be given a name. Aliases chain and compose freely:

```py
type age      = int
type nums     = int[]
type matrix   = int[][]
type maybeInt = int?
type user     = {name: text, age: int}      # struct
type binop    = rule(int, int)->int         # function

type id  = int
type ids = id[]           # int[]

type a = int
type b = a
type c = b                # c → b → a → int
```

An alias works exactly like its target type — in expressions, annotations, function signatures:

```py
type age = int
x: age = 42
y = x + 1                  # 43

type user = {name: text, age: int}
u = user{name = 'Alice', age = 30}
u.name                     # 'Alice'

type transform = rule(int)->int
apply(f: transform, x: int) = f(x)
y = apply(rule it * 2, 21) # 42
```

## Struct field formats

| Format | Example | Required? |
|--------|---------|-----------|
| `name: type` | `{a: int}` | yes |
| `name: type = expr` | `{a: int = 0}` | no |
| `name = expr` | `{a = 42}` | no (type inferred) |
| `name` | `{a}` | yes (type inferred) |

Field names may be primitive-type keywords (`int`, `real`, `bool`, `text`, `char`, etc.) — position disambiguates them from the type

```py
type complex = {real: real, imag: real}

c = complex{real = 1.0, imag = 2.0}
y = c.real                          # 1.0
```

## Construction and defaults

`TypeName{...}` creates an instance, filling defaults for missing optional fields:

```py
type user = {name: text, age: int = 0, active: bool = true}

user{name = 'Alice'}              # age = 0, active = true
user{name = 'Bob', age = 25}      # active = true
user{}                             # ERROR: 'name' is required
user{name = 'Alice', z = 1}       # ERROR: unknown field 'z'
```

Default expressions must be constant: literals, operators on literals, array/struct literals.

Annotation is a type constraint — all fields must be present, no defaults applied:

```py
type user = {name: text, age: int = 0}

a = user{name = 'Alice'}                # OK: construction fills age = 0
b: user = {name = 'Alice', age = 0}     # OK: anonymous struct matches
c: user = {name = 'Alice'}              # ERROR: missing field 'age'
```

## Recursive types

A struct field can reference its own type through `?`, `[]`, or `rule(...)->...?`:

```py
type node = {v: int, next: node? = none}

n = node{v = 1, next = node{v = 2, next = node{v = 3}}}
n.next?.next?.v ?? -1              # 3
```

```py
type tree = {v: int, children: tree[] = []}

t = tree{v = 0, children = [tree{v = 1}, tree{v = 2}]}
t.children.count()                 # 2
```

```py
type stream = {head: int, tail: rule()->stream?}    # lazy stream
```

Direct self-reference (`type t = {f: t}`) is not allowed — the recursion must cross an Optional, Array, or function-arrow constructor.

### Mutual Recursion

```py
type a = {x: int, b: b? = none}
type b = {y: int, a: a? = none}

v = a{x = 1, b = b{y = 2, a = a{x = 3}}}
v.b?.a?.x ?? -1                    # 3
```

## Rules

- Type names are case-insensitive
- Duplicate type names are not allowed
- Circular non-struct aliases (`type a = b; type b = a`) are not allowed
- Types can reference types declared later in the script (forward references)
- Named types are structural — no runtime distinction from the underlying type
