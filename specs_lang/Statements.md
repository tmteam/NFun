# NFun Statements

Statement mode is an **extension** of expression mode. Everything that works in expression mode  as an expression — works here unchanged. 

This document covers only what statement mode **adds**: indented blocks, control flow, mutable variables and structs, multi-line function bodies, pattern matching, error-handling block forms, type narrowing and I/O. 

## Blocks

`:` starts a block. An expression on the same line — single-line block. A newline — multi-line indented block. Both forms appear everywhere a block is expected (function body, `if`/`for`/`while`/`when` body, `try`/`catch`/`anyway`).

```
if x > 0: print('positive')

if x > 0:
    print('positive')
    x += 1
```

Indentation rules: see `IndentRules.md`.

## Scoping

Every block opens a new lexical scope: function body, `if`/`elif`/`else` body, `for`/`while` body, `when` arm, `try`/`catch`/`anyway` body, lambda body. Variables declared inside a block do not leak out.

```
fun f():
    if true:
        local = 1            # only visible inside this branch
    return local             # ERROR — `local` not in scope
```

The iterator of `for x in xs:` is bound for the body only. `catch e:` binds `e` only inside the catch body.

Reassignment to an outer-scope variable writes through to the outer variable:

```
total = 0
for x in [1,2,3]:
    total += x
# total == 6
```

## Variables

### Reassignment

Variables are reassignable. The variable's type is the widest type across all assignments in its scope (LCA over assigned expressions).

```
x = 42
x = x + 1
```

`x = 1` then `x = 1.5` makes `x:real`. Reassigning a value that doesn't widen into a single type is a parse error.

### Compound assignment

`x op= expr` is sugar for `x = x op expr`. Same form applies to fields: `s.field op= expr`.

| Operator | Meaning                                  |
|----------|------------------------------------------|
| `+=`     | add                                      |
| `-=`     | subtract                                 |
| `*=`     | multiply                                 |
| `/=`     | real division                            |
| `//=`    | integer division                         |
| `%=`     | remainder                                |

```
x += 5
x //= 2
p.y -= 3
```

### Mutable structs

Statement mode adds in-place field assignment via `s.field = expr`. The struct is mutated; references to the same struct see the new value. This is the only form of mutation beyond reassignment.

```
p = {x = 0, y = 0}
p.x = 10
p.y += 20                   # compound also works
result = p.x + p.y          # 30
```

Field assignment requires a direct name on the left (`name.field = ...`). Chained mutation through a literal or call expression is not supported.

Element write `a[i] = v` works on `array<T>` and `list<T>`; full collection mutation is available on `list<T>` / `set<T>` / `map<K,V>`. See `Collections.md` §Mutation.

## Functions

### Multi-line definition

`fun name(args):` followed by an indented block. `return expr` returns a value. A function that reaches the end of its body without `return` returns `none`.

```
fun add(a, b):
    return a + b
```

Type annotations are optional. Return type uses `:T` for arguments or `-> T` for result type:

```
fun add(a: int, b: int) -> int:
    return a + b
```

The single-line expression form `name(args) = expr` from expression mode coexists in the same file — see `Basics.md`.

### Extension

A receiver before `.` in the parameter list defines an extension function — callable only via piped syntax.

```
fun x.double():
    return x * 2

y = 21.double()    # 42

fun x:real.tripple()->real:
    return x * 3
    
y = 21.tripple() # 63
```

The single-line form `x.double() = x * 2` works equivalently.

### Return

`return expr` exits the function immediately. Bare `return` returns `none`. `return` is also a value expression — see [Control flow](#return-as-expression).

```
fun clamp(x, lo, hi):
    if x < lo: return lo
    if x > hi: return hi
    return x
```

### Lambdas

`fun` doubles as a lambda keyword. `fun(args): expr` for named parameters, `fun: expr` for implicit `it` / `it1`, `it2`. The body may itself be a block.

```
doubled  = [1,2,3].map(fun: it * 2)
sum      = [1,2,3].fold(0, fun(acc, x): acc + x)

filtered = items.fold(0, fun(acc, x):
    if x > 0: return acc + x
    return acc
)
```

`rule` from expression mode also works.

## Control flow

### if / elif / else

```
if x > 0:
    print('positive')
elif x < 0:
    print('negative')
else:
    print('zero')

# as expression
sign = if x > 0: 1 elif x < 0: -1 else: 0
```

As an expression `if` requires `else`. Branch values unify by LCA.

### for

Iterates over any `enumerable<T>` (array, list, set, map). As a statement returns `none`.

```
for item in [1,2,3]:
    print(item)

sum = 0
for item in xs:
    sum += item
```

Iterating a `map<K,V>` yields a `{key, value}` pair per entry:

```
for kv in m:
    print(kv.key, kv.value)
```

Mutating the iterated collection inside the loop raises a runtime exception (`InvalidOperationException: Collection was modified`). Use `xs.toList()` for an explicit snapshot.

### while

```
i = 0
while i < 10:
    print(i)
    i += 1
```

### break / continue

`break` exits the innermost loop. `continue` skips to the next iteration.

```
for item in items:
    if item < 0: continue
    if item > 100: break
    process(item)
```

Both are also valid as expressions for early exit chained with `??`:

```
for item in xs:
    v = item ?? continue       # skip none values
    sum += v
```

### when

Pattern matching. With a subject — value-based; without — condition-based.

```
# value-based — matches when subject == armValue
result = when x:
    1: 'one'
    2: 'two'
    else: 'other'

# condition-based — matches first arm whose condition is true
result = when:
    x > 0: 'positive'
    x < 0: 'negative'
    else: 'zero'
```

Arms can be multi-line:

```
result = when category:
    'A':
        log('category A')
        processA()
    'B':
        processB()
    else: defaultProcess()
```

Rules:

* **As expression** — requires `else`. Arms unify by LCA.
* **As statement** — `else` optional. If no arm matches and no `else`, execution continues with no effect.
* **Colon after subject** is optional: `when x` and `when x:` are equivalent.

### return as expression

`return` is also a value expression — useful for early exit chained with `??`. Bare `return` returns `none`.

```
fun process(x):
    value = x ?? return none
    return value + 1
```

## Type narrowing

A guard that exits the current block narrows the variable for the remaining statements in that block. A guard is `if condition: <exit>` where `<exit>` is `return`, `break`, `continue` or `oops(...)` (always-throwing call — bottom type).

```
fun first(xs: int?[]):
    if xs.count() == 0: return -1
    head = xs[0]
    if head == none: return -1
    return head + 1            # head is int here, not int?
```

For `break` / `continue` in a loop body the variable narrows for the rest of the same iteration. Algebraic rule: narrowing applies the *negation* of the guard condition to all statements following the guard within the same block.

Supported guard forms: `x == none`, `x != none`, conjunctions (`and`), comparisons against bool literals. Field paths (`s.x == none`) narrow the field.

## Error handling

Statement mode adds block forms of `try` / `catch` / `anyway`. The expression-mode `try expr catch expr` form continues to work (see `Basics.md`).

```
x = try:
        riskyOperation()
    catch:
        0

x = try:
        riskyOperation()
    catch e:
        log(e.message)
        fallback()

x = try:
        riskyOperation()
    catch e:
        log(e.message)
        0
    anyway:
        cleanup()
```

Error variable `e` is `{message: text, data: any}`. `anyway` runs on both success and failure; its value is discarded. If `anyway` throws, the original error propagates.

`try` without `catch` (only `anyway`) is allowed — the error propagates after `anyway` runs.

## I/O

```
print('hello')                 # newline appended
print('hello', end = '')       # no newline

line = readLine()              # read a line from stdin; returns text
ch   = readChar()              # read a single char
```
