# NFun Collections

A collection is an ordered or hashed group of elements of the same type. NFun has five collection shapes:

| Identifier      | Description                                                              |
|-----------------|--------------------------------------------------------------------------|
| `list<T>`       | growable sequence — `[1,2,3]` literal default                            |
| `array<T>`      | fixed-length, element-mutable. Lang-mode `int[]` annotation              |
| `fixedArray<T>` | immutable sequence — LINQ result shape                                   |
| `set<T>`        | unordered unique elements. T must be Immutable                           |
| `map<K,V>`      | hashed key→value lookup. K must be Immutable                             |

All collections are **invariant** in their element type at parameter position — a `list<int>` is not a `list<real>` even though `int ≤ real`. LCA in expression position is element-wise: `LCA(list<int>, list<real>) = list<real>`. Type annotations stay on `int[]`; dedicated `list<T>` / `set<T>` / `map<K,V>` annotation syntax is deferred.

Lists and arrays interoperate via the subtype rule `list<T> ≤ T[]`. Equality is by value across kinds: `[1,2,3] == list(1,2,3)` is `true`.

## Initialization

### By enumeration `[,]`

The `[,]` literal creates a collection with the listed elements. Trailing comma supported.

```
[element0, element1, ... elementN]
[element0, element1, ... elementN,]
```

Example:

```py
a = [1, 2, 3]
b = ['hello', ' ', 'world',]
c = []                          # list<any> — see §Empty literal below
```

The literal binds to whatever the context demands:

```py
a:int[] = [1,2,3]               # array<int>
a       = [1,2,3]               # list<int>  (no context → list)
```

### Empty literal `[]`

`[]` resolves to `list<any>` without context. With annotation or first-usage it narrows:

```py
out         = []                # list<any>
out:int[]   = []                # array<int>
a           = []
b           = a.concat([1,2,3]) # a: list<int>
```

For empty `set<T>` / `map<K,V>` / `array<T>` use `default`:

```py
out:int[] = default             # []
```

### Range `[a..b]`

Two-argument form:

```
[a..b]
```

`a`, `b` are any of `Numbers`. Ascending if `a ≤ b`, descending otherwise; bounds are **inclusive**.

```py
a = [1..5]      #[1,2,3,4,5]
a = [5..1]      #[5,4,3,2,1]
b = [-1..3]     #[-1,0,1,2,3]
c = [1.2..3.2]  #[1.2, 2.2, 3.2]
d = [1.2..3]    #[1.2, 2.2]
```

### Range with step `[a..b step s]`

Three-argument form. `s` is the difference between successive elements.

```py
a = [1..7 step 2]        # [1,3,5,7]
b = [7..1 step 2]        # [7,5,3,1]
c = [1.0..3.0 step 0.5]  # [1.0, 1.5, 2.0, 2.5, 3.0]
```

### Factory functions

| Function                     | Returns                                                    |
|------------------------------|------------------------------------------------------------|
| `list(x0,...,xN)`            | growable list of arity 1..16. The bracket literal `[x0,...,xN]` has no arity cap                                          |
| `array(x0,...,xN)`           | fixed-length mutable array. Arity 1..16                    |
| `fixedArray(x0,...,xN)`      | immutable result-shape array. Arity 1..16                  |
| `set(x0,...,xN)`             | unique elements; T Immutable. Arity 1..16                  |
| `__mkMap({key,value}, ...)`  | hashed map; K Immutable. Pair-struct args                  |

## Equality

Two collections are equal iff they contain the same number of elements and the elements are pairwise equal.

```py
i1:int[] = [1,2,3]
i2:int[] = [1,2,3]
i3:int[] = [3,2,1]
r:real[] = [1,2,3]

res1 = i1 == i2     # true
res2 = i2 == i3     # false
res3 = r == i1      # true  — cross-numeric, value-wise
res4 = [1,2,3] == list(1,2,3)   # true — cross-kind
```

## Indexing `a[i]`

```
a[i]
```

Returns the `i`-th element of `a`. `i` has type `int32`; indexing is zero-based.

```py
array = [1,4,0,3]

e = array[0]    # 1
j = array[1]    # 4
```

Out-of-range index throws a runtime exception. Available on `array<T>`, `list<T>`, `fixedArray<T>`. Not available on `set<T>` or `map<K,V>` — use `m.get(k)` / `s.contains(x)` instead.

## Slices `a[b:e]` / `a[b:e:s]`

```
a[b:e]
```

Returns a subarray from index `b` (inclusive) to `e` (inclusive). `b` and `e` are `int32`.

```py
array = [1,4,0,3]

e = array[0:2]  # [1,4,0]
j = array[2:3]  # [0,3]
k = array[1:1]  # [4]
```

`b` may be omitted (defaults to 0); `e` may be omitted (defaults to last index):

```py
array = [1,4,0,3]

e = array[:2]   # [1,4,0]
j = array[3:]   # [3]
k = array[2:]   # [0,3]
```

The step form `a[b:e:s]` takes every `s`-th element. `s = 1` is the default and can be omitted.

```py
array = [0,1,2,3,4,5,6,7,8,9,10]

a = array[1:7:2]  # [1,3,5,7]
b = array[1::2]   # [1,3,5,7,9]
c = array[::4]    # [0,4,8]
d = array[:4:3]   # [0,3]
```

## Membership operator `in`

```
A in B
```

Returns `true` iff `B` contains the element `A`.

```py
array = [1,2,3]

b = 1 in array      # true
c = 0 in array      # false
d = not 1 in array  # false
```

## Mutation

Each shape exposes a different mutation surface. Signatures live in `Functions.md` §Collection Functions.

### `array<T>`

Element write:

```py
a:int[] = [1,2,3]
a[1] = 42           # [1, 42, 3]
```

Length is fixed — no `add` / `remove`.

### `list<T>`

Element write plus growth:

```py
a = list(1,2,3)
a.add(4)            # [1, 2, 3, 4]
a.remove(2)         # true  — removed first matching element
a.removeAt(0)       # returns 1; list is now [3, 4]
a.removeLast()      # returns 4:int?
a.clear()           # list is now empty
```

### `set<T>`

T must be Immutable. `tryAdd` / `tryRemove` return `bool` — `true` iff the set actually changed.

```py
s = set(1,2,3)
s.tryAdd(4)         # true — added
s.tryAdd(2)         # false — already present
s.tryRemove(1)      # true
s.contains(3)       # true
s.count()           # 3
```

### `map<K,V>`

K must be Immutable. Read and write are method-only — there is no `m[k]` syntax (keys can be any Immutable type and don't read as "index"; symmetric method-only access avoids asymmetric read/write).

```py
m = __mkMap({key=1, value=10}, {key=2, value=20})

v = m.get(1)            # 10:int?  — none when absent
v = m.get(99) ?? 0      # 0        — natural fallback
m.setKey(3, 30)         # adds or overwrites
m.tryAddKey(3, 99)      # false    — already present, no overwrite
m.removeKey(1)          # 10:int?  — value of removed entry
m.containsKey(2)        # true
```

`tryGet` / `tryRemoveKey` return `{value:V, success:bool}` — use when you need to disambiguate "absent" from "present with a none value":

```py
r = m.tryGet(99)
out = if(r.success) r.value else -1
```

The `Key` suffix on map-mutation methods avoids collision with `set.tryRemove(item)`.

### During iteration

Mutating the iterated collection inside a `for` loop raises a runtime exception — see `Statements.md` §for.

## LINQ

Every shape satisfies `enumerable<T>`, so the standard library functions apply uniformly:

```py
[1,2,3].count()                          # 3
list(1,2,3).map(rule it * 2)             # [2, 4, 6]
set(1,2,3).filter(rule it > 1)           # [2, 3]
__mkMap({key=1, value=10}).count()       # 1
```

A `map<K,V>` is an `enumerable<{key:K, value:V}>` — LINQ treats each entry as a pair-struct:

```py
m.map(rule it.value).sum()              # sum of all values
m.filter(rule it.key > 0).count()
```

LINQ results materialise as an immutable array shape. Most ops (`.filter`, `.sort`, `.reverse`, `.concat`, `.sum`, …) return `T[]` (the legacy ee-mode immutable array, which flows into `T[]` parameter slots transparently). `.map` is the exception — it returns `fixedArray<T>`, which is a strict subtype of `T[]` and cannot flow into a `T[]` slot without `.toArray()`:

```py
fun even(xs:int[]):int[] = xs.filter(rule it % 2 == 0)        # OK — returns int[]
fun double(xs:int[]):int[] = xs.map(rule it * 2).toArray()
                                              # ^^ required — .map returns fixedArray<T>
```

Convert with `.toArray()` / `.toList()` / `.toSet()` to feed a mutable slot.

## Iteration

`for x in xs:` works for any `enumerable<T>` — array, list, set, map. For `map<K,V>` it yields `{key:K, value:V}` per entry:

```py
for kv in m:
    total += kv.value
```

## Conversions

| Function          | Source                        | Returns         |
|-------------------|-------------------------------|-----------------|
| `.toList()`       | `enumerable<T>`               | `list<T>`       |
| `.toArray()`      | `enumerable<T>`               | `array<T>`      |
| `.toFixedArray()` | `enumerable<T>`               | `fixedArray<T>` |
| `.toSet()`        | `enumerable<T>`, T Immutable  | `set<T>`        |

Implicit conversion follows the subtype hierarchy `list → array → fixedArray → enumerable`. The reverse direction needs an explicit `.toX()`.

## Alias semantics

Collections are reference types — `b = a` makes `b` and `a` point at the same backing storage.

```py
a = list(1,2,3)
b = a
b.add(4)            # a is now [1,2,3,4]
```

Same model as mutable struct field assignment.
