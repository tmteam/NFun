# Specs Pending Correction — Bug Hunt Round 3

SC-2, SC-3, SC-5, SC-6 applied in commit `<spec round 3 corrections>`. SC-1
and SC-4 remain — they require either implementation work or a behaviour
change rather than a spec rewrite.

---

## SC-1 — `zip` / `head` / `tail` / `average` in errors but not in docs

**Files**: `specs_lang/Functions.md`, `specs_lang/Collections.md` (LINQ section).

**Spec claim**: These names are not documented as available functions anywhere in `specs_lang/`.

**Runtime**: Calling `[1,2,3].zip([4,5,6])` returns `FU783 "Invalid function call argument for 'zip', but was: list<Any>"`. The error implies the function exists but with wrong-typed args. Same shape for `head`, `tail`, `average`.

**Recommendation**: Either (a) implement and document these — clearly intentional given the targeted error message, or (b) change the error code to `FU870` ("function not found") for these names. Currently the diagnostic misleads users into thinking they're calling the wrong arity.

---

## SC-2 — `list<T>` / `set<T>` / `map<K,V>` annotation syntax

**Files**: `specs_lang/Types.md` (Collection shapes table), `specs_lang/Collections.md`.

**Spec claim**: The Types.md collection shapes table uses these identifiers as if they were parseable annotations: `list<T>`, `array<T>`, `fixedArray<T>`, `set<T>`, `map<K,V>`. Collections.md line 13 contains a fine-print note that the dedicated annotation syntax is deferred, but nothing in Types.md flags it.

**Runtime**: `y:list<int>`, `y:array<int>`, `y:set<int>`, `y:fixedArray<int>`, `y:map<int,int>` all reject with `FU609` parse errors. Only `y:int[]` (giving `array<T>` in lang mode) actually parses.

**Recommendation**: Add a prominent note at the top of Types.md §Collection shapes:

> Currently only the array form `T[]` parses as a type annotation. The `list<T>` / `set<T>` / `map<K,V>` / `fixedArray<T>` / `enumerable<T>` shapes are illustrative — dedicated annotation syntax is deferred. Use `T[]` annotation and rely on inference for the other shapes.

---

## SC-3 — `hex` vs `HEX` text format

**File**: `specs_lang/Texts.md` §Format specifiers / Named specifiers table.

**Spec claim**: The table lists `hex` (lowercase hex digits) and `HEX` (uppercase) as separate format specifiers, with examples `{255:hex}` → `ff` and `{255:HEX}` → `FF`.

**Runtime**: Both produce identical UPPERCASE output. `'{0xab:hex}'` → `'AB'`, `'{0xab:HEX}'` → `'AB'`.

**Recommendation**: Either (a) implement `hex` as lowercase to make the table meaningful, or (b) collapse the two rows to one row labelled `hex` / `HEX` and note "uppercase output; the casing of the specifier itself is ignored".

---

## SC-4 — `none` as a type identifier

**Files**: `specs_lang/Types.md`, `specs_lang/Optionals.md`.

**Spec claim**: `none` is documented purely as a literal value. Types.md does not list `none` as a type identifier.

**Runtime**: CLI prints `x:none = none` for `x = none`, `y:none = none` for `if(true) none else none`, struct field types include `{x:none}`. So `none` IS an internal type — just not addressable via the user-facing annotation syntax (`y:none = ...` fails at parse).

**Recommendation**: Either (a) document the inferred-only type `none` in Types.md / Optionals.md, or (b) render these inferred slots as `any?` / `T?` so the user surface matches the spec. Option (b) is cleaner — users never need to think about `none` as a type.

---

## SC-5 — LINQ result type inconsistency

**File**: `specs_lang/Collections.md` §LINQ.

**Spec claim**: "LINQ results materialise as `fixedArray<T>` (immutable). The subtype direction is `array → fixedArray`, so a `fixedArray<T>` cannot flow into a `T[]` slot ..."

**Runtime**:
- `[1,2,3].map(rule it*2)` → `fixedArray<Int32>` ✓ (matches spec)
- `[1,2,3].filter(rule it>1)` → `Int32[]` ✗
- `[3,1,2].sort()` → `Int32[]` ✗
- `[1,2,3].reverse()` → `Int32[]` ✗
- `[1,2,3].concat([4])` → `Int32[]` ✗

Most LINQ ops actually return `T[]` (ee-mode immutable array), not `fixedArray<T>`. Only `.map` returns the new shape.

**Recommendation**: Either (a) align all LINQ ops to return `fixedArray<T>` (consistent), or (b) update the spec to say "LINQ results materialise as an immutable array shape — usually `T[]`, with some newer ops (`.map`) returning `fixedArray<T>`. Both flow into `T[]` parameter slots, but `.toArray()` is required to feed a `T[]` return type if the caller's pipeline started with `.map`."

---

## SC-6 — `a[i] += v` compound assignment

**Files**: `specs_lang/Operators.md` §Assignment Operators, `specs_lang/Statements.md`.

**Spec claim**: Compound LHS forms listed as `name` and `name.field`. Indexed write `a[i] = v` is documented for plain `=`, but compound `a[i] += v` is silently absent from the table.

**Runtime**: `x:int[] = [1,2,3]; x[0] += 100` fails with "Left side of compound assignment must be an identifier". This is by design but readers will guess wrong from the `name.field` case (which DOES support compound).

**Recommendation**: Add a one-line note to Operators.md §Assignment Operators below the table:

> Indexed LHS (`a[i] += v`) is not supported for compound forms — use `a[i] = a[i] + v` instead.
