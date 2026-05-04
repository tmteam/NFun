# NFun-Lang

NFun-Lang is the indent-based language mode of NFun. It extends expression mode with multi-line functions, control flow, mutable variables, pattern matching, and I/O.

All expression-mode features (types, operators, arrays, structs, optionals, math sugar, anonymous functions) work in lang mode unchanged. This document covers lang-mode additions only.

File extension: `.fun`

---

## Blocks

`:` starts a block. If an expression follows on the same line — single-line block. If a newline follows — multi-line indented block.

```py
# Single-line
if x > 0: print('positive')

# Multi-line
if x > 0:
    print('positive')
    x += 1
```

Indentation rules: see `IndentRules.md`.

---

## Functions

### Multi-line functions

`fun` defines a function with an indented body. `return` is required for returning a value. Without `return`, the function returns `none`.

```py
fun add(a, b):
    return a + b

fun greet(name):
    print('hello ' + name)    # no return → none
```

Type annotations are optional:

```py
fun add(a: int, b: int) -> int:
    return a + b
```

### Expression functions

Single-line expression syntax from expression mode:

```py
add(a, b) = a + b
square(x) = x * x
```

### Extension functions

Receiver before `.` defines an extension function. The receiver becomes the first parameter. Extension functions are callable only via piped syntax (`5.double()`), regular functions only via direct call (`double(5)`). Both can coexist with the same name.

```py
fun x.double():
    return x * 2

fun x.add(n):
    return x + n

y = 21.double()       # 42
z = 10.add(5)         # 15
```

Expression-mode syntax:

```py
x.double() = x * 2
x:int.isEven() = x % 2 == 0
```

Built-in functions (`sort`, `count`, `map`, etc.) are always callable via piped syntax regardless of extension function rules.

### Return

`return` exits the function immediately. Bare `return` (no expression) returns `none`.

```py
fun clamp(x, lo, hi):
    if x < lo: return lo
    if x > hi: return hi
    return x
```

`return` is also valid as an expression — useful for early exit patterns:

```py
fun process(x):
    value = x ?? return none   # if x is none, return none
    return value + 1
```

### Lambdas

`fun` doubles as lambda keyword. `fun(args): expr` for named parameters, `fun: expr` for implicit `it`.

```py
doubled = [1,2,3].map(fun: it * 2)
sum = [1,2,3].fold(0, fun(acc, x): acc + x)

# Block lambda
filtered = items.fold(0, fun(acc, x):
    if x > 0:
        return acc + x
    return acc
)
```

`rule` from expression mode also works: `[1,2,3].map(rule it * 2)`.

---

## Variables

### Assignment and reassignment

Variables can be reassigned. The type is inferred as the widest type across all assignments.

```py
x = 42
x = x + 1            # reassignment
x = 0                 # reassignment again
```

### Compound assignment

```py
x += 5                # x = x + 5
x -= 3                # x = x - 3
x *= 2                # x = x * 2
x /= 4               # x = x / 4
x //= 2              # x = x // 2
x %= 3                # x = x % 3
```

---

## Control Flow

### if / elif / else

```py
if x > 0:
    print('positive')
elif x < 0:
    print('negative')
else:
    print('zero')

# Single-line
if x > 0: print('positive')

# As expression
result = if x > 0: 'pos' else: 'non-pos'
```

### for

Iterates over an array. Statement — returns `none`.

```py
for item in [1, 2, 3]:
    print(item)

# Single-line
for item in [1, 2, 3]: print(item)

# Accumulator pattern
sum = 0
for item in [1, 2, 3, 4, 5]:
    sum += item
```

### while

Repeats while condition is true. Statement — returns `none`.

```py
x = 0
while x < 10:
    print(x)
    x += 1
```

### break / continue

`break` exits the innermost loop. `continue` skips to the next iteration.

```py
for item in items:
    if item < 0: continue
    if item > 100: break
    process(item)
```

Both are valid as expressions for early exit with `??`:

```py
for item in [1, none, 3, none, 5]:
    v = item ?? continue       # skip none values
    sum += v

for item in [1, 2, none, 4]:
    v = item ?? break          # stop at first none
    sum += v
```

### when

Pattern matching. Value-based (with subject) or condition-based (without subject).

```py
# Value-based
result = when x:
    1: 'one'
    2: 'two'
    else: 'other'

# Condition-based
result = when:
    x > 0: 'positive'
    x < 0: 'negative'
    else: 'zero'
```

Colon after subject is optional:

```py
result = when x
    1: 'one'
    2: 'two'
    else: 'other'
```

As statement (no `else` required):

```py
when command:
    'quit': break
    'help': showHelp()
```

Multi-line arms:

```py
result = when category:
    'A':
        log('category A')
        processA()
    'B':
        log('category B')
        processB()
    else: defaultProcess()
```

---

## Error Handling

### try / catch / anyway

Expression-mode `try expr catch expr` works unchanged. Lang mode adds block forms.

```py
# Block try-catch
x = try:
        riskyOperation()
    catch:
        0

# With error variable
x = try:
        riskyOperation()
    catch e:
        log(e.message)
        fallback()

# try-catch-anyway
x = try:
        riskyOperation()
    catch e:
        log(e.message)
        0
    anyway:
        cleanup()

# try-anyway (no catch — error propagates after anyway runs)
try:
    riskyOperation()
anyway:
    cleanup()
```

Error variable `e` is a struct: `{message: text, data: any}`.

`anyway` always executes regardless of success or failure. Its result is discarded. If `anyway` throws, the original error propagates.

---

## I/O

```py
print('hello')                 # prints with newline
print('hello', end = '')       # prints without newline

line = readLine()              # reads a line from stdin
ch = readChar()                # reads a single character
```

---

## @Test Annotations

`@Test` marks a function as a test case. `@Test(args...)` provides parameterized arguments.

```py
@Test(1, 2, 3)
@Test(0, 0, 0)
@Test(-1, 1, 0)
fun testAdd(a, b, expected):
    assertEqual(add(a, b), expected)

@Test
fun testBasic():
    assert(1 + 1 == 2)
    assertEqual(max(3, 5), 5)
    assertType(42, 'int')
```

Test functions (available via test kit):

| Function | Description |
|----------|-------------|
| `assert(cond)` | Fails if false |
| `assertEqual(a, b)` | Fails if not equal |
| `assertNotEqual(a, b)` | Fails if equal |
| `assertType(val, name)` | Fails if runtime type name doesn't match |
