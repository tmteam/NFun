# NFun User Functions

A user function is a callable named in the script body. Two forms coexist.

## Single-line form

```py
functionName(arg1:type1, arg2:type2)->rtype = expression
```

* **functionName** — function name
* **arg1, arg2..argN** — argument names. Trailing comma supported
* *(optional)* **type1..typeN** — argument types
* *(optional)* **rtype** — return type (`->` or `:`)
* **expression** — function body

Only function arguments are visible inside the body. Script inputs and outputs are not.

```py
sumOf3(a,b,c) = a+b+c

a = sumOf3(1,2,3) #6:int

b = maxOfReal(1,2,3) #3:real

maxOfReal(a,b,c,):real = max(max(a,b),c)

firstPositiveNumber(a:int[]) = if(a.any(rule it>0)) a.filter(rule it>0)[0] else default

c = [-1,3,0,-4,5].firstPositiveNumber() #3
```

## Multi-line form

Indented body with `return`. See `Statements.md` §Functions for the full syntax.

```py
fun functionName(arg1:type1, arg2:type2)->rtype:
    return expression
```

## Named arguments

Arguments can be passed by name in any order.

```py
f(a,b,c) = a*100 + b*10 + c
y = f(c=3, a=1, b=2)          # 123
y = f(1, c=3, b=2)            # 123 — positional then named
y = 1.f(c=3, b=2)             # 123 — pipe-forward with named
y = max(b=1, a=5)             # 5   — works for built-in functions too
y = range(from=1, to=5)       # [1,2,3,4,5]
```

## Default values

Arguments with `= expr` after the name get a default value. Required args must come before defaults.

```py
f(a, b=10) = a + b
y = f(5)           # 15  — b uses default
y = f(5, 20)       # 25  — b overridden
y = f(a=5)         # 15  — all-named with default
y = f(5, b=20)     # 25  — mixed

greet(a, b=10, c=20) = a+b+c
y = greet(1, c=5)  # 16  — skip middle default by name
```

## Varargs (params)

The `...` prefix collects extra positional args into an array.

```py
f(a, ...rest) = a + rest.sum()
y = f(10, 1, 2, 3)           # 16
y = f(10)                     # 10  — empty rest
y = f(a=10, rest=[1,2,3])    # 16  — named array for rest

g(a, b=0, ...rest) = a+b+rest.sum()
y = g(1, 2, 3, 4)            # 10  — defaults + params
```

## Keyword-only arguments

Arguments declared after `...` can only be passed by name.

```py
join(...items, sep=' ') = items.map(rule it.toText()).fold('', rule(a,b) = a.concat(sep).concat(b))
y = join(1, 2, 3)            # ' 1 2 3' — sep uses default
y = join(1, 2, 3, sep='-')   # '-1-2-3' — sep by name only

f(a, ...rest, verbose=false) = if(verbose) rest.count() else a + rest.sum()
y = f(1, 2, 3)               # 6       — verbose=false (default)
y = f(1, 2, 3, verbose=true) # 2       — verbose by name only, rest=[2,3]
```

Keyword-only args must have defaults (they can't be filled positionally).

## Specific and generic functions

When all argument types are uniquely determined — either from the expression or explicitly annotated — the function is **specific**:

```py
divideBy2(a) = a/2                                # a:real, returns real
multiplyReal(a: real, b) = a*b                    # a:real, b:real, returns real
maxOf3(a,b,c)->int = max(a,b).max(c)              # all int
```

When the function is valid for a range of types it is **generic**. The argument types and return type are bound by the operators used in the body.

```py
threeSum(a,b,c) = a+b+c
```

`+` works on `int32`, `int64`, `uint32`, `uint64`, `real`, so `threeSum` is generic in `T => real`. The same call site picks the appropriate concrete type:

```py
a:int = threeSum(1,2,3)

barg1: real; barg2:real; barg3:real
b = threeSum(barg1, barg2, barg3)
```

More examples:

```py
firstItem(a) = if(a.count()>0) a[0] else default

t = 'hello'.firstItem() #'h'
m = [1,2,3].firstItem() # 1

increment(a) = a+1

i = 0.increment()    #1:int
j = 1.5.increment()  #1.5:real
k:uint = 12.increment() #13:uint
```
