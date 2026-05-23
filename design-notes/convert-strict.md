# `convert(value):T` — Strict / Minimalist Design

> Author posture: **STRICT**. A static-typed language must reject unsafe conversions at compile time. When in doubt, REJECT. `convert` is a *typed total function w.r.t. its return type*: it produces a value of `T` or throws — never silently invents `none`, never silently saturates, never silently rounds.

---

## 1. Philosophy

`convert` is a **typed coercion** operator, not a fallible parser. Its contract is: *"given a value of type `S`, produce a value of type `T`; if no total or well-defined coercion exists between `S` and `T`, the program is ill-typed."* Conversions whose failure mode is *runtime* (parsing text, narrowing wider integers, deserialising byte arrays) survive only because there is **no other way** to obtain a value of `T` from `S`; everything that *can* be detected at compile time (struct→bool, real→ip, bool→char, …) **must** be rejected at compile time with a dedicated `FU` error. The optional target `T?` does **not** alter the contract of `convert` — `convert(s):T?` first computes `convert(s):T`, then lifts the result; failure is still a runtime exception. "Try parse" semantics belong to a separate `tryParse(text):T?` function that the runtime *guarantees* never to throw, and whose contract is *"return `none` instead of throwing"*.

The driving asymmetry: **a lift from `T` to `T?` is free and total; a contract from "or throws" to "or returns `none`" is a semantic change** that callers must opt into by choosing a different function. Conflating the two makes optional types a hiding place for bugs.

---

## 2. Conversion matrix

Legend:

| Mark | Meaning |
|---|---|
| `=` | Identity (same type, or already-implicit subtype). No code generated beyond a no-op. |
| `✓` | Allowed, total at runtime (no possible failure for any value of `S`). |
| `⚠` | Allowed, **runtime-fallible** (overflow, parse error, encoding error). Throws `FunnyRuntimeException` on failure. |
| `✗` | **Rejected at compile time** with an `FU` error (see §4 for codes). |
| `T` | Allowed only via `toText` semantics (uniform `Any → text`). |

Rows = source `S`. Columns = target `T`. Numeric implicit-widening cells use `=` because they are already legal *without* `convert` — `convert` is then a no-op pass-through, kept for syntactic uniformity.

### 2.1. Scalar → Scalar

|  S \ T   | byte | int16 | int32 | int64 | uint16 | uint32 | uint64 | real | bool | char | text | ip | any |
|---------:|:----:|:-----:|:-----:|:-----:|:------:|:------:|:------:|:----:|:----:|:----:|:----:|:--:|:---:|
| **byte**   |  =   |   =   |   =   |   =   |   =    |   =    |   =    |  =   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **int16**  |  ⚠   |   =   |   =   |   =   |   ⚠    |   ⚠    |   ⚠    |  =   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **int32**  |  ⚠   |   ⚠   |   =   |   =   |   ⚠    |   ⚠    |   ⚠    |  =   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **int64**  |  ⚠   |   ⚠   |   ⚠   |   =   |   ⚠    |   ⚠    |   ⚠    |  ⚠   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **uint16** |  ⚠   |   ⚠   |   =   |   =   |   =    |   =    |   =    |  =   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **uint32** |  ⚠   |   ⚠   |   ⚠   |   =   |   ⚠    |   =    |   =    |  =   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **uint64** |  ⚠   |   ⚠   |   ⚠   |   ⚠   |   ⚠    |   ⚠    |   =    |  ⚠   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **real**   |  ✗   |   ✗   |   ✗   |   ✗   |   ✗    |   ✗    |   ✗    |  =   |  ✗   |  ✗   |  T   | ✗  |  =  |
| **bool**   |  ✗   |   ✗   |   ✗   |   ✗   |   ✗    |   ✗    |   ✗    |  ✗   |  =   |  ✗   |  T   | ✗  |  =  |
| **char**   |  ✗   |   ✗   |   ✗   |   ✗   |   ✗    |   ✗    |   ✗    |  ✗   |  ✗   |  =   |  T   | ✗  |  =  |
| **ip**     |  ✗   |   ✗   |   ✗   |   ✗   |   ✗    |   ✗    |   ✗    |  ✗   |  ✗   |  ✗   |  T   | =  |  =  |

Notes on the scalar block:

- **`real → integer*` is `✗`** (compile error), even though the current implementation silently truncates. Truncation is a *semantic decision* (truncate? round? banker's-round?) that belongs in named functions: `floor`, `ceil`, `round`, plus a separate `truncate(real):int64`. `convert` does not pick one for you.
- **`bool ↔ integer` is `✗`**. There is no canonical bool/int isomorphism — `0/1` is a serialisation choice, not a type identity. Users who want it write `if(b) 1 else 0`.
- **`char ↔ integer` is `✗`**. The current `(char→int*)` path returns Unicode code units via byte-pair gymnastics; the inverse uses `Convert.ToByte` for `byte`. Neither is the obvious operation a caller wants. Code points belong in `codePoint(char):int32` / `fromCodePoint(int32):char`.
- **`ip ↔ integer` is `✗`**. The current implementation packs/unpacks via `BitConverter.ToInt32` (raw byte order, no endian guarantee). `ip → uint32` is a useful operation but should be a named function `ipToUint32(ip):uint32` with explicit byte order — not `convert`.
- **`* → text` is `T`** uniformly. This is `toText`-equivalent; we keep it because no caller can be surprised by it, and removing it would break a large body of existing code. (Alternative considered: remove from `convert`, route to `toText` only. Rejected for backwards compatibility — see §8 Cons.)
- **`* → any` is `=`** by the type system (everything lifts to `any` already; `convert` is a tautology).
- The `=` cells on the integer-widening diagonal exist because numeric widening is already implicit (see `Types.md` lines 113–115); `convert` is allowed but redundant.

### 2.2. Composite → byte array (serialisation)

| S | T = `byte[]` |
|---|---|
| `byte`           | ⚠ — single-element array `[v]` |
| `int16`/`uint16` | ✓ — 2 bytes little-endian |
| `int32`/`uint32` | ✓ — 4 bytes little-endian |
| `int64`/`uint64` | ✓ — 8 bytes little-endian |
| `real`           | ✓ — 8 bytes IEEE-754 little-endian |
| `bool`           | ✓ — `[1]` / `[0]` |
| `char`           | ✓ — 2 bytes UTF-16 LE |
| `text`           | ✓ — UTF-16 LE bytes |
| `ip`             | ✓ — 4 bytes network order |
| `T[]` where T ≠ byte | ✗ — use `.flat(map(rule convert(it):byte[]))` explicitly |
| `struct`         | ✗ — serialisation of structs requires an explicit format (`toJson`, `toBson`) |
| `opt(T)`         | ✗ — `none` has no canonical byte representation |
| `any`            | ✗ — dispatched serialisation must be opt-in (`serialiseAny`) |

The `⚠` for `byte → byte[]` exists only because the choice "lift to a one-element array vs. error" is debatable; we keep the current behaviour but flag it.

### 2.3. Composite → bit array (`bool[]`)

Same matrix as §2.2 with bytes split into bits. Same `✗` rows for struct / opt / any.

### 2.4. `byte[]` → T (deserialisation)

| T | from `byte[]` |
|---|---|
| `byte`/`int16`/`uint16`/`int32`/`uint32`/`int64`/`uint64`/`real` | ⚠ — fixed-width decode; **throws if length ≠ expected** |
| `char`                | ⚠ — 1 byte (ASCII) or 2 bytes (UTF-16); throws otherwise |
| `bool`                | ⚠ — `bytes[0] == 1` → true, `0` → false; **throws on other values** |
| `text`                | ✓ — UTF-16 LE decode (total; produces a string for any byte sequence, surrogate pairs handled) |
| `ip`                  | ⚠ — requires exactly 4 bytes |
| `T[]` where T ≠ byte | ✗ — no canonical element layout |
| `struct` / `opt(T)`   | ✗ |

**Strict change from current behaviour**: integer deserialisers must validate length (currently they silently zero-pad short arrays and ignore excess bytes — a bug, MR-class).

### 2.5. `text` → T (parsing)

| T | from `text` |
|---|---|
| All numeric (byte..uint64, real) | ⚠ — `T.Parse(invariant)`; throws `FunnyRuntimeException` on format/overflow |
| `bool` | ⚠ — accepts `"true"`, `"false"`, `"1"`, `"0"` (case-insensitive for letters); throws otherwise |
| `ip`   | ⚠ — `IPAddress.Parse`; throws on malformed input |
| `char` | ⚠ — text of length 1 → that char; throws otherwise |
| `text` | = |
| `T[]`  | ✗ — parsing arrays needs an explicit separator / format function |
| `struct` / `opt(T)` | ✗ — see §3 for `opt(T)` |
| `byte[]` | covered by §2.2 (text→byte[] serialisation) |
| `any`  | = (implicit lift) |

### 2.6. Composite ↔ composite

| Scenario | Verdict |
|---|---|
| `S[] → T[]` where `S → T` is allowed in §2.1 (i.e. cell is `=`, `✓`, or `⚠`) | ✓ / ⚠ — elementwise; failure mode inherited from element conversion |
| `S[] → T[]` where `S → T` is `✗` | ✗ at compile time (the element conversion is ill-typed, the array conversion is too) |
| `S[] → T[]` where `S → T` is `T` (i.e. only via toText) | ✗ — toText on arrays is `toText(arr) = "[1,2,3]"`, not element-wise. Use `arr.map(rule convert(it):text)` |
| `text → byte[]` | ✓ (§2.2 row) |
| `byte[] → text` | ✓ (§2.4 row) |
| `text → char[]` | = (text is `char[]` by definition) |
| `S{...} → T{...}` (struct → struct) | See §5.2 |
| `T → T?` (any T) | ✓ — implicit lift; `convert` is a no-op |
| `T? → T` | ✗ — `convert(x:T?):T` is **rejected at compile time** (see §3 below). Use `x!` or `x ?? default`. |
| `T? → U?` where `T → U` is allowed in §2.1 | ✓ / ⚠ — element-wise (`none` passes through unchanged) |
| `any → T` for any concrete T | ⚠ — runtime type test; throws if actual type mismatched. This is the **only** way `any → T` can be requested; static-rejecting it would make `any` useless. |

---

## 3. Optional semantics — the central question

### Rule (one sentence)

> `convert(s):T?` is desugared as `convert(s):T` followed by the implicit lift `T → T?`. **Failure of the inner conversion throws** — it does **not** produce `none`.

### Consequence

```py
a:int? = convert("not-a-number")   # THROWS FunnyRuntimeException at runtime
                                    # "Cannot convert 'not-a-number' to type int"
```

The optional target does **not** create a safety net. `int?` advertises *"the value may be absent"*, not *"any operation that produces an `int` may instead produce `none`"*. Conflating the two erases the difference between:

1. A genuine optional value (the database column was NULL),
2. A computation that failed (the string was malformed).

Erasing this distinction makes every `int?` ambiguous and forces every consumer to guess. The cure is worse than the disease.

### Mirror cases

```py
b:int?  = convert(42)              # 42 lifted to int?      — ok
c:int?  = convert(some_text)       # some_text:text → int, throws if malformed
d:int?  = convert(maybe_int :int?) # = maybe_int            — element-wise, none passes through
e:int?  = convert(none)            # ✗ COMPILE ERROR. convert needs a known S — none is not.
                                    #    Workaround: `none :int?` is already an int?, no convert needed.
```

### `T? → T` is rejected at compile time

```py
x:int? = 42
y:int  = convert(x)   # ✗ FU8101. Use `x!` or `x ?? 0`.
```

Rationale: this exact operation already has two purpose-built operators (`!`, `??`) whose semantics are crystal clear. Letting `convert` do it silently would be a third spelling with subtly different error wording — a maintenance trap.

### "Try" semantics live elsewhere

Callers who want "parse, return none on failure" use a separate function:

```py
tryParse(text):T?      # never throws — none on any failure
a:int? = tryParse("not-a-number")    # none
b:int? = tryParse("42")              # 42
```

`tryParse` is a thin wrapper, but its **type signature** advertises its semantics. That is the whole point.

---

## 4. Failure model

`convert` has exactly three failure surfaces:

1. **Static rejection** — the (`S`, `T`) pair is not in the matrix above. Reported as a single new error code:
   - **`FU8101 ConvertNotDefined`** — `"convert: no conversion defined from <S> to <T>"`. (Today: thrown as `InvalidOperationException` from `ConvertFunction.cs` line 94 — escapes the FU error pipeline entirely. This is the MR5Bug2 trigger and the proximate cause of GH-51.)
   - **`FU8102 ConvertOptionalUnwrap`** — `"convert: cannot implicitly unwrap T? to T. Use '!' or '??'."` (Subcase of 8101, separated for actionable diagnostics.)
   - **`FU8103 ConvertNeedsExplicitNumericRounding`** — `"convert: real → integer requires explicit rounding. Use floor/ceil/round/truncate."` (Subcase of 8101, separated to point the user at the right named function.)

2. **Runtime overflow / format / encoding** — `FunnyRuntimeException` wrapping the underlying CLR exception. Message format: `"Cannot convert <value> to type <T>"` (unchanged from today).

3. **Runtime type test failure** for `any → T` — `FunnyRuntimeException` `"Cannot convert <value> of type <S> to type <T>"`.

That's it. There is no fourth path that swallows the error and returns `none`.

---

## 5. Composite types

### 5.1. Arrays (`T[] → U[]`)

Element-wise. Allowed iff the element conversion `T → U` is allowed by §2.1; the array conversion inherits the element's failure mode (`✓` element → `✓` array, `⚠` element → `⚠` array, `✗` element → `✗` array). Special-cased rows: `text ↔ byte[]` (§2.2/§2.4 wins over element-wise `char ↔ byte`, because the unit is the whole array).

`convert(a:int[]):real[]` is the canonical use — and it is **already** legal without `convert` (covariant element widening per `Types.md` §Type-conversion-rules). `convert` here is a redundant no-op the parser still accepts for symmetry.

### 5.2. Structs

Struct → struct is allowed iff the target is a **width-subtype** of the source (target's fields are a subset of source's, with each shared field's type implicitly convertible). This is already legal without `convert` (`Types.md` line 112). `convert` between structs is therefore either:

- **No-op pass-through** when the conversion is implicit — allowed.
- **`✗ FU8101`** otherwise. We deliberately do **not** add "shape-matching" coercion (i.e., `{a:int, b:text} → {a:real, b:text}` via field-wise convert), because:
  1. It hides which field caused a runtime failure.
  2. It conflicts with width-subtyping when source has extra fields of incompatible types.
  3. Users who want field-wise conversion can write the struct literal explicitly: `{a = convert(src.a):real, b = src.b}`.

Struct → scalar / scalar → struct: **`✗`**, no exceptions.

### 5.3. `opt(T) → T` — does `convert` duplicate `!`?

**No, because `convert(x:T?):T` is `✗`** (FU8102). Forcing unwrap is exclusively the job of `!` (throws on `none`) and `??` (provides a fallback). `convert` deliberately stays out of this — having three operators that do the same thing with different error messages is a known anti-pattern (cf. PHP's `intval`/`(int)`/`settype` trio).

The inverse direction (`T → T?`) is also not `convert`'s job — the language already does it implicitly (`Types.md` line 116). `convert` accepts the call as a no-op for syntactic uniformity but generates no runtime work.

---

## 6. Spec changes needed (`Specs/Functions.md` lines 50–96)

- **Replace line 50** (`convert(TA):TR | --- | ...`) with a row that explicitly names the matrix and references a new sub-section "Conversion matrix" (the §2 of this document).
- **Delete the "Useless conversions" table (lines 56–61)** — its three rules become:
  - "Same type" → trivially the `=` diagonal of the matrix.
  - "Argument type descendant" → covered by the type system's implicit subtyping; `convert` is a no-op.
  - "`text` target" → preserved as the `T` column ("uniform toText").
- **Tighten the "Serialization to `byte[]`" table (lines 63–72)**:
  - Remove the duplicate `char` row (lines 65 and 71 both describe char serialisation, inconsistently — keep only the UTF-16 LE 2-byte form).
  - Add explicit `✗` rows for `T[]`, `struct`, `opt(T)`, `any` — currently silently `throw InvalidOperationException`.
- **Fix the typo "Serialization (Result type is `byte[]`)" header at line 74** — should read "Serialization (Result type is `bool[]`)".
- **Tighten "Deserialization" (lines 77–87)**:
  - Add "**throws if input length ≠ expected width**" to every integer/real row (currently the implementation silently pads/truncates).
  - Remove the buggy fallback in `CreateDeserializerOrNull` that returns text-as-bytes for `to.IsText` (line 304 of `ConvertFunction.cs` returns bytes, not text — a copy-paste from the serialiser).
- **Tighten "Parsing" (lines 89–96)**:
  - Add a new top-of-section note: *"`convert(text:text):T?` does NOT swallow parse failures. Use `tryParse(text):T?` for that semantics."*
  - Add `char` parsing row (text of length 1 → char; throws otherwise).
- **Add a new sub-section "Compile-time rejections"** listing every `✗` cell with the FU error code (8101 / 8102 / 8103).
- **Add a one-liner under "Generic functions"**: *"`convert` is not a substitute for `!`, `??`, `floor`/`ceil`/`round`, `toCodePoint`, `fromCodePoint`, or `tryParse`. Each has a dedicated function."*

---

## 7. Code changes scope (`ConvertFunction.cs`)

Rough estimate, current file = 343 lines.

| Change | Lines |
|---|---|
| Replace the `throw new InvalidOperationException` at line 94 with proper FU8101 emission via the function-selector pipeline (move the static-rejection decision out of `CreateConcrete` and into a `CanConvert(S, T)` predicate consulted at TIC-resolve time). | ~+50 / -10 |
| Add `IsStaticallyRejected(from, to)` table-driven predicate (one switch over `(BaseFunnyType, BaseFunnyType)` pair, plus composite recursion). | ~+80 |
| Remove the `real → integer` switch branches from `VarTypeConverter` *for `convert`'s code path* (keep them for ABI/marshalling). Easiest: have `convert` consult `IsStaticallyRejected` before calling `VarTypeConverter.GetConverterOrNull`. | ~+5 / -0 |
| Remove `CreateFromCharConverterOrNull` (lines 183–214) entirely — `char → int*` is now `✗`. | ~-32 |
| Slim `CreateToIpConverterOrNull` / `CreateFromIpConverterOrNull` — drop `int* ↔ ip` branches (lines 100–103, 163–167). Keep `byte[] ↔ ip` and `text → ip`. | ~-25 |
| Tighten `CreateDeserializerOrNull` to throw on wrong-length input instead of silently padding (`AsByteArray` helper at 328 already throws when too long; mirror the check for too-short). | ~+15 |
| Fix the bogus `to.IsText ? ... → bytes` branch at line 304. | ~-1 / +1 |
| Add the `opt(T)` handling: `convert(x):T?` → call inner `convert(x):T` then lift; `convert(x:T?):T` → reject FU8102. | ~+40 |
| New unit tests covering each `✗` cell — separate file, not counted here. | (test file) |

**Net delta**: roughly **+150 / −70 = +80 lines** in `ConvertFunction.cs`, plus one new error code (3 lines in `Errors.*.cs`), plus a TIC-side hook so static rejections surface as compile errors rather than `InvalidOperationException`s from `CreateConcrete`.

The largest semantic refactor is the last item: today `CreateConcrete` is allowed to throw (line 94), and the harness treats it as an unhandled exception. The strict design **requires** static rejection to flow through `FU` codes, which means `ConvertFunction` needs a "can I produce a concrete?" pre-check called from the function selector. That is the only architecturally non-trivial change.

---

## 8. Pros / Cons vs. a permissive alternative

A "permissive" counter-design would: accept `real → int` (silent truncation), `char ↔ int` (code point), `bool ↔ int` (`0`/`1`), and have `convert(s):T?` return `none` on failure.

### Pros of strict

- **One mental model**: `convert` always throws, never lies. The type signature *is* the contract.
- **Errors caught early**: ~10 entire `✗` rows move from runtime to compile time. Every one of them is a class of `MR`-bug that today escapes test coverage.
- **No semantic forks**: there is exactly one way to round, one way to unwrap, one way to "try parse". Each has a dedicated, discoverable name.
- **`int?` stays meaningful**: a value of `int?` is `none` *only* because the data-source said so, never because a conversion silently failed.
- **Removes the `InvalidOperationException` escape hatch** at `ConvertFunction.cs:94` — the proximate cause of GH-51 and MR5Bug2.
- **Smaller blast radius for future changes**: every cell is named, every failure has a code, the matrix can be exhaustively tested.

### Cons of strict

- **Breaks existing scripts** that rely on `convert(real_val):int` truncating. Migration: rewrite as `truncate(real_val)` (or `floor`, depending on intent). Estimated user-visible breakage: 5–15 idioms in the existing test corpus.
- **`convert(text):int?` no longer doubles as `tryParse`**, which is what the user's question shows is a *plausible* expectation. The counter-argument: that expectation is exactly what the strict design wants to dispel. The cost is one documentation paragraph and one new `tryParse` function.
- **More verbose for code that genuinely wants 0/1-as-bool**: `if(b) 1 else 0` vs. `convert(b):int`. The verbose form is also clearer at the call site; this is a feature.
- **Three new FU codes** (8101/8102/8103) to maintain. Worth it — actionable error messages are the whole reason to have a static type system.
- **TIC-side wiring**: the function-selector must consult `IsStaticallyRejected` instead of relying on `CreateConcrete` throwing. One-time cost (~50 lines), zero ongoing cost.
- **`* → text` via `T` mark is a compromise** — the strict-est position would route all to-text via `toText` and forbid it on `convert`. Rejected as gratuitous breakage. Tracked as a future cleanup.

### Net

The strict design eliminates an entire class of silent bugs at the cost of a small migration. The escape valve (`tryParse`, `truncate`, `floor`, `codePoint`, etc.) is cheap to provide and **makes intent visible at the call site** — exactly the property a static-typed language exists to deliver.
