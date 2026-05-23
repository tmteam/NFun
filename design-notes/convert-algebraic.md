# convert(value):T — Algebraic / Type-Theoretic Design

> **Position**: type-theory designer. `convert` is not a bag of ad-hoc runtime tricks — it is
> a function over the NFun type lattice. A conversion exists between two types iff there is a
> meaningful **morphism** in the algebra. Partial morphisms surface their partiality **in the
> return type**, not in runtime exceptions.

---

## 1. Philosophy

`convert(value):T` is the user-facing exponent of a single algebraic operation:

> *a morphism `m : S → T` between two members of the NFun type lattice.*

There are exactly three flavours of morphism — and the type system already knows which is which:

1. **Implicit lift (`I`)** — the morphism is the identity on values, since `S ≤ T` already
   holds in the lattice (e.g. `int → real`, `T → T?`, `T → any`, `int[] → real[]`). `convert`
   for these pairs is allowed, but it is a no-op — present only for readability and explicit
   type annotation.
2. **Total morphism (`T`)** — a function `S → T` that succeeds for **every** value of `S`
   (bijection or surjection). Example: `char ↔ uint16` via codepoint, `int32 → int64`,
   `char → text` (single-element array).
3. **Partial morphism (`P`)** — a function `S → T?` that may produce `none`. The partiality
   is **part of the type**: `convert(value):T` for a partial pair is **statically rejected**.
   The user must spell `convert(value):T?` and decide what to do with `none` (`??`, `!`, narrowing).

The fourth cell of the matrix is **`✗`** — no morphism exists in the algebra at all (e.g.
`bool → ip`). The compiler rejects these unconditionally; widening the target to `T?`
does *not* rescue them, because there is no morphism to be partial in the first place.

This is what makes the design **honest**: by reading the type of a `convert` expression you
already know whether it can yield `none` — there are no silent runtime exceptions in well-typed
programs.

---

## 2. Morphism classification matrix

Legend (cell value = morphism class for `convert(value):T`):

| Symbol | Meaning |
|--------|---------|
| `I`    | Implicit lift — already free by `≤`. `convert` allowed as no-op. |
| `T`    | Total morphism — always succeeds, returns the target type directly. |
| `P`    | Partial morphism — `convert(v):S` **REJECTED**, requires `convert(v):S?`. |
| `✗`    | No morphism — statically rejected even with `T?` target. |

Rows = **source type** of `value`. Columns = **target type** of `convert`.
Aliases: `byte ≡ uint8`, `int ≡ int32`, `uint ≡ uint32`, `text ≡ char[]`.

### 2.1 Primitive × primitive

| from \ to  | u8 | u16 | u32 | u64 | i16 | i32 | i64 | real | bool | char | text  | ip  | any |
|------------|----|-----|-----|-----|-----|-----|-----|------|------|------|-------|-----|-----|
| **u8**     | I  | I   | I   | I   | I   | I   | I   | I    | ✗    | T    | T     | ✗   | I   |
| **u16**    | P  | I   | I   | I   | P   | I   | I   | I    | ✗    | T    | T     | ✗   | I   |
| **u32**    | P  | P   | I   | I   | P   | P   | I   | I    | ✗    | P    | T     | T   | I   |
| **u64**    | P  | P   | P   | I   | P   | P   | P   | I    | ✗    | P    | T     | T   | I   |
| **i16**    | P  | P   | P   | P   | I   | I   | I   | I    | ✗    | P    | T     | ✗   | I   |
| **i32**    | P  | P   | P   | P   | P   | I   | I   | I    | ✗    | P    | T     | T   | I   |
| **i64**    | P  | P   | P   | P   | P   | P   | I   | I    | ✗    | P    | T     | T   | I   |
| **real**   | P  | P   | P   | P   | P   | P   | P   | I    | ✗    | ✗    | T     | ✗   | I   |
| **bool**   | T  | T   | T   | T   | T   | T   | T   | T    | I    | ✗    | T     | ✗   | I   |
| **char**   | T  | T   | T   | T   | T   | T   | T   | ✗    | ✗    | I    | T     | ✗   | I   |
| **text**   | P  | P   | P   | P   | P   | P   | P   | P    | P    | P    | I     | P   | I   |
| **ip**     | ✗  | ✗   | T   | T   | ✗   | T   | T   | ✗    | ✗    | ✗    | T     | I   | I   |
| **any**    | P  | P   | P   | P   | P   | P   | P   | P    | P    | P    | T(*)  | P   | I   |

(*) Every value in NFun has a canonical text rendering (`toText`), so even `any → text` is total.

#### Notes on the primitive matrix

- **Widening (any unsigned/signed → larger unsigned/signed/real)** is `I`. Lattice already
  permits it; `convert` is a no-op cast for readability.
- **Narrowing** between numeric types is `P` — the value may overflow. The compiler refuses
  `convert(int64):int32` and demands `:int32?`.
- **bool → numeric** is **`T`** (not `I`): `false → 0`, `true → 1`. Definite bijection from
  `{false, true}` to `{0, 1}`. The reverse (`numeric → bool`) is `✗` — there is no canonical
  total function (no convention beats "any nonzero → true" silently).
- **char ↔ unsigned integers** is **`T`** in both directions for types wide enough to hold a
  UTF-16 code unit (`u16+`). For narrower targets (`u8`) it is also `T` only because we treat
  the narrow domain as already-covering ASCII — but to keep the rule purely algebraic, **`u8 →
  char`** is `T` (every byte yields a valid char) and **`char → u8`** is `P` (a multi-byte
  char overflows). The above matrix follows the *strict* reading: `char → u8/i16/...` is `T`
  only if the code unit fits unsigned-bitness; otherwise overflow. We classify all `char →
  integer` cells as `T` because the runtime convention in the lattice is "raise on overflow
  unless you ask for `?`". *To be 100% honest, future cleanup should split these into
  `T` for `≥ u16` and `P` for `u8`.* For now we match the current `CreateFromCharConverterOrNull`
  behavior, which **throws on overflow**, i.e. is actually partial — so under strict design
  these too become `P`.
- **text → anything except text** is `P` everywhere (parsing can fail), including `text → char`
  (multi-char input).
- **ip → numeric**: an IPv4 address is exactly 4 bytes ≡ `uint32`. The bijection makes
  `ip ↔ uint32 / int32 / uint64 / int64` total. To narrower integers it is `✗` (not P! — the
  algebra has no meaning for "an IP address as 16 bits"; the user should slice the byte array).
- **`any` source**: the runtime tag may be anything, so every conversion that goes through a
  type test is `P`. Only the universal `text` rendering and identity-to-`any` remain free.

### 2.2 Composite × composite

Composites are derived purely from the primitive matrix by **lifting the morphism through
the type constructor**.

| Pair                         | Class | Algebraic rule |
|------------------------------|-------|----------------|
| `A[] → B[]`                  | class(A→B) | "Whatever class A → B is, the same lifts to arrays." |
| `A[] → B`                    | `✗` (unless `B=any` ⇒ `I`, or `B=text` and `A=char` ⇒ `I`) | No projection from a sequence to a scalar. |
| `A → B[]`                    | `✗` (unless `B=A` ⇒ `T` for singleton lift — *not provided by default*) | No injection from a scalar to an array. |
| `S{f₁:A₁,…} → S{f₁:B₁,…}`    | meet of class(Aᵢ→Bᵢ) over the *target* field set | Width subtyping: missing target field on source ⇒ `✗`. Each shared field's morphism lifts; the worst lift wins. |
| `S{...} → primitive`         | `✗` | A record is not a scalar. |
| `primitive → S{...}`         | `✗` | A scalar is not a record. |
| `opt(A) → opt(B)`            | class(A→B), but `T` always weakens to `T` (none is preserved) | Apply morphism to the wrapped value. `none` round-trips. |
| `opt(A) → B`                 | promoted to `P` regardless of class(A→B) | Unwrapping is partial: a `none` cannot become a non-optional `B`. So `convert(opt(A)):B` REJECTED — must be `:B?`. |
| `A → opt(B)`                 | class(A→B), with target widening (see §3) | Same morphism, then implicit lift. |
| `any → T`                    | `P` for every concrete `T` ≠ `any` | Runtime tag may not match. |
| `T → any`                    | `I` | Top of the lattice. |
| `rule(...) → rule(...)`      | `✗` outside contravariant/covariant lattice — no general convert | Functions have no canonical morphism beyond subtyping (`I` only when already `≤`). |

#### Composite worked examples

```
convert([1,2,3]:int[]):real[]      # I  (numeric widening lifts pointwise)
convert([1,2,3]:int[]):int8[]      # P  (rejected unless target is int8?[])
convert([1,2,3]:int[]):int8?[]     # T  (lift uses partial morphism, none on overflow per element)
convert(s:{x:int}):{x:real}        # I  (depth-covariant)
convert(s:{x:int, y:bool}):{x:int} # I  (width-covariant)
convert(s:{x:int}):{y:int}         # ✗  (no morphism — target field y missing on source)
convert(o:int?):int                # rejected → must be convert(o:int?):int? = I (identity)
convert(o:int?):real?              # I (lift via depth covariance)
convert(a:any):int                 # rejected → must be convert(a:any):int?  = P
```

---

## 3. Optional semantics — how target `T` vs `T?` controls the rejection

The morphism class is determined by the **source** and the **unwrapped target**. The optional
modifier on the target type controls only whether `P`-class morphisms are accepted:

| Target shape | Morphism class on `(source, unwrapped target)` | Result |
|--------------|------------------------------------------------|--------|
| `T`          | `I`                                            | identity / lift, returns `T`     |
| `T`          | `T` (total)                                    | always succeeds, returns `T`     |
| `T`          | `P` (partial)                                  | **STATIC REJECT** — `FU<n>`: "<source>→<target> is partial; use `convert(x):<target>?` and handle `none`" |
| `T`          | `✗`                                            | **STATIC REJECT** — no morphism  |
| `T?`         | `I`                                            | identity then lift `T → T?`      |
| `T?`         | `T` (total)                                    | total convert then lift          |
| `T?`         | `P` (partial)                                  | returns `none` on failure, value on success — this is the **only** way to ask for a fallible conversion |
| `T?`         | `✗`                                            | **STATIC REJECT** — no morphism even with `?` |

The asymmetry is the whole point: `?` on the target type **does not magically create morphisms**.
It only changes how the type system communicates "may fail" to the user.

If the source is already optional `opt(S)`, the morphism is *applied through the wrapper*:
the value branch runs `m : S → ?`, the none branch is preserved as `none`. This means:

- `convert(opt(S)):opt(T)` runs `m` on the value side; class is the same as `S → T`.
- `convert(opt(S)):T` is **always** rejected (unwrap is itself partial — there is no value
  to convert in the `none` branch). Forcing this requires `! ` (`convert(s!):T`).

### 3.1 Five examples

```py
# Example 1 — implicit identity through lift
x:int = 42
y = convert(x):real        # T   (well-typed: int → real is I)

# Example 2 — total morphism (bool → byte is total)
b:bool = true
y = convert(b):byte        # T   (=> 1)

# Example 3 — partial without ? — REJECTED
y = convert("not-a-number"):int
# FU<n>: text → int is a partial morphism. Use `convert(value):int?` and
#        handle `none` with `??` or `!`.

# Example 4 — same call, made honest
y:int? = convert("not-a-number"):int?      # => none
z      = convert("42"):int? ?? 0          # => 42

# Example 5 — optional source unwrapped through composition
o:int? = 42
y = convert(o):real        # FU<n>: cannot convert opt(int) to non-optional real.
                           # Either `convert(o):real?` (preserves none) or `convert(o!):real`
y = convert(o):real?       # ok, returns real? (none stays none, 42 → 42.0)
```

---

## 4. Lattice diagram (primitives, with morphism labels)

Edges in the **subtype lattice** are labelled with the morphism class for the *forward* (down→up,
narrowest→widest) direction; the *reverse* (up→down) direction class is shown after the slash.
Free upward edges are all `I`; the reverse is `P` unless noted.

```
                          any
                           ^
                           | I / P
                           |
            +----+----+----+----+----+----+
            |    |    |    |    |    |    |
          char  bool  ip  real  …
            |   T/✗   |    ^
            |         T    | I/P
            |         |    |
            +---T-->[u16]<-+  (char ↔ uXX wide-enough; ip ↔ u32/u64 total)
                           |
                          i64    (i64 → real: I,    real → i64: P)
                           ^
                       I/P |
                          i32
                           ^
                       I/P |
                          i16
                           ^
                       I/P |     (signed chain)
                          (none, but i16 is the bottom of signed)

                          u64    (u64 → real: I,    real → u64: P)
                           ^
                       I/P |
                          u32
                           ^
                       I/P |
                          u16
                           ^
                       I/P |
                           u8    (bottom of unsigned chain)

   text  ──── T ────►  (anything via toText)
   text  ──── P ────►  numerics, bool, char, ip   (parsing)
```

Highlights of the diagram:

- Every upward step in the existing subtype lattice is `I` (free implicit) downward-to-upward;
  the reverse step is `P` (overflow possible).
- The `bool ↔ numeric` and `char ↔ numeric` edges are *not* in the lattice (they live outside
  it). They are total morphisms in one direction only.
- `text` sits as a *parsing/printing hub*: `T → text` total via `toText`, `text → T` partial
  via parsing.
- `ip` connects only to `uint32` / `int32` / `uint64` / `int64` / `text` / byte-arrays — all by
  total morphisms.

---

## 5. Composite types — algebraic treatment

### 5.1 Arrays `T[] → U[]`

```
convert : (A → B)  ⟹  (A[] → B[])
class(A[] → B[]) = class(A → B)
```

- If element conversion is **total**, the array conversion is total (map). If it's `I`, the
  whole thing is `I`.
- If element conversion is **partial**, the array conversion is partial. Per element. The
  algebra forces the target to be `U?[]` (array of optional) — not `U[]?` (optional array).
  Difference matters: `U?[]` lets *individual elements* fail; `U[]?` would mean the *whole
  array* fails, which is not what element-wise convert does.
- `convert(["1", "x", "3"]):int[]` → REJECTED.
  `convert(["1", "x", "3"]):int?[]` → `[some 1, none, some 3]`.

### 5.2 Structs

Width + depth subtyping define the only structural morphism.

```
S = {f₁:A₁, …, fₘ:Aₘ}      T = {f₁:B₁, …, fₙ:Bₙ}    (n ≤ m, every fᵢ in T must appear in S)

class(S → T) = ⨅ class(Aᵢ → Bᵢ)        (worst class wins over the *target* fields)

— missing target field on source ⟹ ✗ (no morphism exists)
— extra source fields ⟹ ignored (width subtyping)
```

`convert` for structs is the same operation as assignment — it has no new powers. Re-shaping
records (renaming, computing new fields) is the job of *expressions*, not `convert`.

### 5.3 Optional `opt(T)` — through the wrapper

```
opt(A) ----[m : A → B]----> opt(B)        class = class(A → B)
opt(A) ----[m : A → B]----> B             FORBIDDEN (unwrap is partial)
A      ----[m : A → B]----> opt(B)        class(A → B), then implicit lift
```

This is the algebraic statement of "covariance through the optional functor". The conversion
preserves `none` in the source automatically; no special-cased runtime path.

### 5.4 `any → T`

```
class(any → T) = P            for every concrete T (except T = any, which is I)
```

There is no static information about the runtime tag of an `any`, so every concrete extraction
is partial. Always rejected without `?`.

### 5.5 Functions `rule(...)→...`

Outside the existing subtyping rules (variance), there is no canonical morphism between two
function types. `convert` on a function value is therefore `I` when the lattice already
permits the assignment, and `✗` otherwise. No partial path is offered — building a coerced
wrapper is the user's job (lambdas).

---

## 6. Why this design — the "honesty" argument

The current `convert` implementation throws `FunnyRuntimeException` on failure. That means a
well-typed program can still **crash** because of a string that doesn't parse, an `int64` that
doesn't fit in an `int8`, or an `any` whose runtime tag is wrong. The compiler had the data
to know "this might fail" — but the type didn't say so. The user finds out at run-time, in
production.

Under this algebraic design, **every fallible `convert` has an optional return type**, and the
compiler refuses to let you ignore that fact. Whenever a program type-checks:

- A `convert(x):T` returning a non-optional `T` is *provably* total. No crash possible.
- A `convert(x):T?` is *visibly* fallible — the caller must handle `none` (`??`, `!`, or
  narrowing). No silent crash possible.
- A `convert(x):T` for a non-existent morphism never compiles. No "runtime cannot convert
  Xxxx to Yyyy" exception is ever reached.

This is the principle behind Rust's `TryFrom` (partial) vs `From` (total), Haskell's `read`
returning `Maybe a`, and Swift's `Int?(string)` initializer. NFun gains the same guarantee:
*partiality belongs to the type signature*.

---

## 7. Spec changes — `Specs/Functions.md`

Replace lines 50–96 (the four ad-hoc tables) with a single morphism-class table that **is** the
spec. Concrete edits:

- Line 50, `convert(TA):TR` row: change "see the conversion table" → "see §Conversion. The
  conversion is rejected at compile time if the source/target pair has no morphism, or has a
  partial morphism and the target is not optional."
- Replace §"Useless converions" (sic), §"Serialization", §"Deserialization", §"Parsing" with:

  1. **§Morphism classification table** — paste the matrix from §2.1 here verbatim.
  2. **§Optional rule** — paste the table from §3 verbatim.
  3. **§Composite lifting rule** — bullet list from §5 (one line per composite shape).
  4. **§Compile-time errors** — enumerate the two new errors:
     - `FU<n>` *Partial conversion requires optional target* — fires on `P` cell with
       non-optional target. Message includes the suggested fix:
       `convert(value):<T>?`.
     - `FU<n>` *No conversion between <S> and <T>* — fires on `✗` cell.
- Drop the "Useless conversions" paragraph entirely — implicit lift handles those by
  subtyping; we keep them in the matrix as `I` for completeness.
- Move the **serialization specifics** (byte layout of `real`, ascii-vs-unicode for `char`)
  to a new appendix `Specs/Conversions.md` — they are *runtime semantics* of total morphisms,
  not type rules.

---

## 8. Implementation impact — `src/NFun/Functions/ConvertFunction.cs`

Today, `CreateConcrete` builds a runtime function for any pair and throws
`InvalidOperationException` when it can't. Under this design, **compile-time** rejection
moves into `CreateConcrete`:

```text
public override IConcreteFunction CreateConcrete(FunnyType[] concreteTypes, …) {
    var to   = concreteTypes[0];
    var from = concreteTypes[1];

    var morphism = ConvertMorphism.Classify(from, to);
    //   Identity | Total | Partial | None

    switch (morphism.Class) {
        case Identity: return Noop(from, to);
        case Total:    return TotalConverter(morphism.Apply, from, to);
        case Partial:
            if (to.IsOptional) return PartialConverter(morphism.Apply, from, to);
            // Compile-time error — reject here, before the converter is ever invoked
            throw new FunCompileException(
                $"Cannot convert {from} to {to}: this is a partial conversion. " +
                $"Use `convert(value):{to}?` and handle `none`.");
        case None:
            throw new FunCompileException($"No conversion exists between {from} and {to}.");
    }
}
```

New supporting code:

- **`ConvertMorphism`** static helper with two public entry points:
  - `bool IsTotalMorphism(FunnyType from, FunnyType to)` — true iff the cell is `I` or `T`.
  - `MorphismClassification Classify(FunnyType from, FunnyType to)` — returns
    `(class, Func<object,object> apply)`. Drives codegen.
- **`PartialConverter`** wraps the existing `Func<object,object>` in a try/catch that returns
  `none` (the runtime representation) instead of throwing.
- Existing helpers (`CreateParserOrNull`, `CreateFromCharConverterOrNull`, etc.) are reused,
  but each is tagged with its class. The big switch-block in `CreateConcrete` is replaced by a
  table-driven dispatch from `ConvertMorphism`.

TIC has nothing to change — `convert` stays a `GenericFunctionBase` with two unconstrained
generics. The rejection happens after TIC resolves both generics. (Optional: also write a
pre-TIC check that adds a constraint "to is optional whenever from→to is partial"; but this is
strictly nicer error messages, not required.)

---

## 9. Pros / Cons vs other positions

### vs **strict** design (forbid even more — e.g. require `convert` only between identical types and have separate `parse`/`narrow`/`unwrap` functions)

| | strict | algebraic | pragmatic |
|-|--------|-----------|-----------|
| compile-time rejections | most | many (every `P` without `?`) | none |
| function-name clutter | `parse`, `tryParse`, `narrow`, `tryNarrow`, … | one (`convert`) | one |
| silent runtime crashes | impossible | impossible in well-typed programs | possible |
| user types fewer chars | no | yes | yes |
| onboarding curve | steep | shallow (rule fits in one table) | trivial |

The algebraic position is the **middle road**: keeps the one-function surface that newcomers
expect, but uses optionality to carry the failure modality. The strict position is
philosophically purer but linguistically heavier.

### vs **pragmatic** design (keep the current code — throw on overflow / parse failure)

Pros of algebraic over pragmatic:
- Removes a whole class of production-time runtime errors.
- Forces partial conversions to be **visible** at the call site.
- Spec shrinks from four tables (useless, serialize, deserialize, parse) to one matrix that
  captures every legitimate pair.
- Implementation simpler: one classifier replaces the big chained-`if` in `CreateConcrete`.

Cons:
- **Breaking change.** Code like `i:int = convert("42")` now requires `:int?` plus a `??` or
  `!`. Mitigation: provide a migration shim `parseOrThrow(text):T` (existing semantics) for one
  release; emit a `FUWarn<n>` deprecation on the old pattern.
- Some users prefer "easy mode" where they trust their input. Counter: those users add `!`
  once per call site (`convert(x):int?!`), and gain a stack-trace site they actually wrote
  themselves.
- Slightly more typing for very simple expressions. The savings on long-term debugging more
  than pay for that cost.

---

## 10. Open questions (deliberately deferred)

- **`char → u8`**: the strict reading says `P` (multi-byte chars overflow); current runtime
  also throws. The matrix above marks it `T` to mirror existing behavior. Decide and align
  before shipping — preferable to flip it to `P` for consistency.
- **`text → text`**: currently `I` (identity). If we ever introduce non-`char[]` text
  representations (e.g. a `string` distinct type), revisit.
- **Lossy lift `real → int64`?** Currently `P` in the matrix. One could argue `T` with explicit
  truncation semantics (e.g. floor), but this would make `convert` *sometimes* a rounding
  operator. The honest position is `P`: real includes NaN, infinity, and out-of-range values
  that no integer represents; the user should write `convert(r):int64?` and decide.
- **Custom user morphisms**: should there be a way to *register* a new `(S → T)` total morphism
  from user code? Out of scope for this design — but the classifier should be open to extension
  by registering more entries in the matrix.

---

## TL;DR

`convert(value):T` is a function over the type lattice. There exists at most one *kind* of
morphism between any pair `(S, T)`: identity-by-subtyping, total, partial, or none.

- If it's **total or identity**, `convert(v):T` returns `T`.
- If it's **partial**, `convert(v):T` is **rejected at compile time** — the user must write
  `convert(v):T?`, which returns `none` on failure.
- If **no morphism exists**, `convert(v):T` and `convert(v):T?` are both rejected.

The matrix in §2 *is* the new spec. The implementation in §8 is a small refactor of one file.
The promise to the user is the single sentence at the top: **partiality is in the type**.
