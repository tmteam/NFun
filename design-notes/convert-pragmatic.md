# `convert(value):T` — Pragmatic / Try-Semantics Design

> **Designer's stance.** Optional target types unlock *soft failure semantics*.
> `convert(x):T` is strict (throws on dynamic failure). `convert(x):T?` is "try"
> (returns `none` on dynamic failure). The user's type annotation chooses the
> failure mode — no separate `tryParse` family is needed. Same call site,
> different annotation, different behavior.

Status: design proposal for GH #51 (convert validation) and MR5Bug2
(raw `InvalidOperationException` in `ConvertFunction.cs:94`).

---

## 1. Philosophy

NFun already has a first-class notion of *absence* — `none` — and an existing
language-level discipline (`?`, `??`, `!`, `?.`, `?[`) that makes "the thing
might not be there" pleasant to write. Conversion is the largest source of
*dynamic* failure in the language (parsing, overflow, encoding loss), and today
each failure is just an unrecoverable `Oops`. Combining the two: a target type
annotation `T?` is a structurally faithful way to say "I accept that this might
fail." The annotation drives the failure mode at the call site — there is one
function (`convert`), and the user's declared expectation chooses whether
failure is a `FunnyRuntimeException` or a `none` value. This collapses the
`convert` / `tryParse` axis into one orthogonal concept (target optionality) and
keeps it consistent with the rest of the optional pipeline: `convert(s):int? ??
0` works exactly the same way as `arr?[i] ?? 0`. Static-impossible conversions
(no rule defined for the source/target pair) are still rejected at compile
time — the try mode only catches *runtime* failures.

### Three families of failure

| Family | When | Strict `T` | Try `T?` |
|---|---|---|---|
| **Static rejection** | No conversion rule exists for `S → T` (e.g. `bool → ip`, `struct → int`) | `FU` compile error | `FU` compile error — `T?` doesn't rescue *impossible* conversions |
| **Runtime soft failure** | Parse / overflow / format mismatch / encoding loss / out-of-shape array (e.g. wrong-length byte[] → ip) | `FunnyRuntimeException` | `none` |
| **Runtime hard failure** | Should not happen if conversion rule says it succeeds — bug indicator (e.g. OOM, broken invariant) | `FunnyRuntimeException` | `FunnyRuntimeException` (still propagates) |

The interesting boundary is **soft vs hard**. A predictable, well-classified
runtime fault (overflow, parse, format, encoding) is "soft" and caught by `T?`.
An unpredictable fault (OOM, internal invariant violation) is "hard" and still
escapes — the user can't recover from those anyway and silencing them would hide
bugs.

---

## 2. Conversion matrix

Legend:
- **✓** — works, no runtime failure possible
- **⚠** — works but with caveat (encoding loss, exact-match required, ordering by little-endian, etc.)
- **✗** — static rejection at `FU` compile time
- **🪂** — soft runtime failure. `convert(x):T` throws, `convert(x):T?` returns `none`
- **💥** — hard runtime failure (always throws even with `T?` target). Reserved for "this should never happen" cases.

Source rows × Target columns. `T?` rows/cols are handled in section 5; this
table is the *unwrapped* source / unwrapped target matrix.

| Source ↓ / Target → | byte | int16 | int32 | int64 | uint16 | uint32 | uint64 | real | bool | char | text | ip | byte[] (Ser) | bool[] (Bits) | T[] | struct | any |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **byte (uint8)**  | ✓ id | ✓ widen | ✓ widen | ✓ widen | ✓ widen | ✓ widen | ✓ widen | ✓ widen | 🪂 0/1 only† | ⚠ ASCII | ✓ toText | ✗ | ✓ [b] | ✓ bits | ✗ | ✗ | ✓ |
| **int16**         | 🪂 narrow | ✓ id | ✓ widen | ✓ widen | 🪂 narrow (sign/range) | 🪂 narrow | 🪂 narrow | ✓ widen | 🪂 0/1 only | 🪂 codepoint range | ✓ toText | ✗ | ✓ LE 2B | ✓ bits | ✗ | ✗ | ✓ |
| **int32**         | 🪂 narrow | 🪂 narrow | ✓ id | ✓ widen | 🪂 narrow | 🪂 narrow | 🪂 narrow | ✓ widen | 🪂 0/1 only | 🪂 codepoint range | ✓ toText | ⚠ raw bits → IPv4 | ✓ LE 4B | ✓ bits | ✗ | ✗ | ✓ |
| **int64**         | 🪂 narrow | 🪂 narrow | 🪂 narrow | ✓ id | 🪂 narrow | 🪂 narrow | 🪂 narrow (sign) | ⚠ precision loss > 2^53 | 🪂 0/1 only | 🪂 codepoint range | ✓ toText | ⚠ low 32 bits → IPv4 | ✓ LE 8B | ✓ bits | ✗ | ✗ | ✓ |
| **uint16**        | 🪂 narrow | 🪂 narrow | ✓ widen | ✓ widen | ✓ id | ✓ widen | ✓ widen | ✓ widen | 🪂 0/1 only | ⚠ direct codepoint | ✓ toText | ✗ | ✓ LE 2B | ✓ bits | ✗ | ✗ | ✓ |
| **uint32**        | 🪂 narrow | 🪂 narrow | 🪂 narrow | ✓ widen | 🪂 narrow | ✓ id | ✓ widen | ✓ widen | 🪂 0/1 only | 🪂 codepoint range | ✓ toText | ⚠ raw bits → IPv4 | ✓ LE 4B | ✓ bits | ✗ | ✗ | ✓ |
| **uint64**        | 🪂 narrow | 🪂 narrow | 🪂 narrow | 🪂 narrow (sign) | 🪂 narrow | 🪂 narrow | ✓ id | ⚠ precision loss > 2^53 | 🪂 0/1 only | 🪂 codepoint range | ✓ toText | ⚠ low 32 bits → IPv4 | ✓ LE 8B | ✓ bits | ✗ | ✗ | ✓ |
| **real**          | 🪂 NaN/Inf/range | 🪂 NaN/Inf/range | 🪂 NaN/Inf/range | 🪂 NaN/Inf/range | 🪂 NaN/Inf/range | 🪂 NaN/Inf/range | 🪂 NaN/Inf/range | ✓ id | ✗ | ✗ | ✓ toText (invariant) | ✗ | ✓ LE 8B | ✓ bits | ✗ | ✗ | ✓ |
| **bool**          | ✓ 0/1 | ✓ 0/1 | ✓ 0/1 | ✓ 0/1 | ✓ 0/1 | ✓ 0/1 | ✓ 0/1 | ✓ 0/1 | ✓ id | ✗ | ✓ toText | ✗ | ✓ [0]/[1] | ✓ 1 bit | ✗ | ✗ | ✓ |
| **char**          | ⚠ low byte (ASCII range OK; high glyphs lose info) | 🪂 surrogate overflow | ⚠ Unicode bytes | ⚠ Unicode bytes | 🪂 surrogate overflow | ⚠ Unicode bytes | ⚠ Unicode bytes | ✗ | ✗ | ✓ id | ✓ toText (single char) | ✗ | ✓ 1-2B Unicode | ✓ bits | ✗ | ✗ | ✓ |
| **text** (char[]) | 🪂 parse byte | 🪂 parse int16 | 🪂 parse int32 | 🪂 parse int64 | 🪂 parse uint16 | 🪂 parse uint32 | 🪂 parse uint64 | 🪂 parse real (invariant) | 🪂 only 'true'/'false'/'0'/'1' | 🪂 len ≠ 1 fails | ✓ id | 🪂 parse "192.168.0.1" | ⚠ Unicode encode | ⚠ Unicode encode bits | element-wise (see §5) | ✗ | ✓ |
| **ip**            | ✗ | ✗ | ⚠ raw 4 bytes → int32 | ✓ widen via 4 bytes | ✗ | ⚠ raw 4 bytes → uint32 | ✓ widen | ✗ | ✗ | ✗ | ✓ toText (dotted) | ✓ id | ✓ 4-byte LE | ✓ 32 bits | ✗ | ✗ | ✓ |
| **byte[]**        | 🪂 len ≠ 1 | 🪂 deser LE (len ≥ 2) | 🪂 deser LE (len ≥ 4) | 🪂 deser LE (len ≥ 8) | 🪂 deser LE | 🪂 deser LE | 🪂 deser LE | 🪂 deser LE (len ≥ 8) | 🪂 len ≠ 1 | 🪂 len ∉ {1,2} | ✓ Unicode decode | 🪂 len ≠ 4 | ✓ id | ✓ bits | element-wise | ✗ | ✓ |
| **bool[]**        | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | 🪂 len ≠ 1 | ✗ | 🪂 toText of each | ✗ | 🪂 pack bits → bytes | ✓ id | element-wise | ✗ | ✓ |
| **U[]** (other)   | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ toText (literal repr) | ✗ | (only via U=primitive) | (only via U=primitive) | ✓ element-wise | ✗ | ✓ |
| **struct**        | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ toText (literal repr) | ✗ | ✗ | ✗ | ✗ | ⚠ width-compat only (see §5) | ✓ |
| **any**           | 🪂 dynamic dispatch | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | ✓ toText | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | 🪂 dynamic | ✓ id |
| **T?** (any opt)  | see §5 unwrap rules — strict fails on `none`, opt-target lifts |

†*"0/1 only"*: integer → bool currently does not exist in `ConvertFunction.cs`
(no rule). Marking it as 🪂 reflects what would happen if we *added* the rule
with the documented `bool → 0/1` semantics inverted. If we keep the current
behavior of "no rule", these cells become ✗ in the strict column. The
pragmatic recommendation is to *add* the rule and make `T?` rescue it.

### Reading the table

- Cells with a single mark are unambiguous.
- A 🪂 cell behaves differently depending on the target's optionality:
  - `convert(x):T` — `FunnyRuntimeException` with a useful message.
  - `convert(x):T?` — `none`.
- `✗` cells stay rejected even with a `T?` target. *The annotation only changes
  failure mode for dynamically possible conversions.* If the language can prove
  at compile time that the conversion can never succeed, asking for "try"
  semantics is meaningless and the user gets a clear `FU` error.
- ⚠ cells are *lossy but deterministic* — they don't throw, so `T?` adds no
  rescue. They are listed here so the user understands the conversion isn't
  ideal; the optional annotation is then equivalent to a plain implicit lift
  `T → T?`.

---

## 3. Try semantics spec

A conversion `convert(v):T` produces a result of type `T`. A conversion
`convert(v):T?` produces a result of type `T?` and treats *soft runtime
failures* as a `none` value rather than as an exception. The decision is made
once, at `CreateConcrete` time: when the requested target type is `Optional(U)`,
the runtime selects the converter for `S → U` and wraps it in a `try/catch`
that maps a documented set of exception types to `none`. When the requested
target is non-optional, the converter is used as-is and exceptions propagate as
today.

The set of "soft" exception types that get translated to `none`:

- `FormatException` (parsing)
- `OverflowException` (numeric narrowing, char surrogate, etc.)
- `ArgumentException` / `ArgumentOutOfRangeException` (bad shape — e.g.
  wrong-length byte[] when deserializing to a fixed-width primitive)
- `NFun.Exceptions.FunnyRuntimeException` when raised from inside the
  converter itself (e.g. `AsByteArray` array-too-long, MR5Bug2 line 94's
  generic case if it ever fires for a known-soft cause)

Hard exceptions (`OutOfMemoryException`, `StackOverflowException`,
`NullReferenceException`, `InvalidCastException` from the CLR layer indicating
a bug, …) **always propagate**, even with a `T?` target — these are not
recoverable program states.

### Per-target failure matrix

| Source → Target | Can fail? | `convert(x):T` on failure | `convert(x):T?` on failure | Notes |
|---|---|---|---|---|
| `text → int32` | yes (parse) | throw `FunnyRuntimeException` wrapping `FormatException` | return `none` | classic TryParse |
| `text → real` | yes (parse) | throw | `none` | invariant culture |
| `text → bool` | yes (only `'true'`/`'false'`/`'0'`/`'1'`, case-insensitive) | throw | `none` | |
| `text → ip` | yes (format) | throw | `none` | `IPAddress.Parse` |
| `text → char` | yes (length ≠ 1) | throw | `none` | |
| `int64 → byte` | yes (overflow) | throw `OverflowException`-wrapped | `none` | applies to any numeric narrowing |
| `real → int*` | yes (NaN / Inf / range) | throw | `none` | |
| `byte[] → int32` | yes (length < 4) | throw | `none` | bounds in `BitConverter` |
| `byte[] → ip` | yes (length ≠ 4) | throw | `none` | `AsByteArray` already validates |
| `char → int16` | yes (surrogate overflow) | throw `OverflowException` | `none` | existing code throws |
| `int → real` | **no** | n/a — never fails | n/a — `T?` is just an implicit lift | "free" optional |
| `byte → int32` | **no** (widening) | n/a | n/a — `T?` is implicit lift | |
| `bool → byte` | **no** | n/a | n/a | |
| `T → text` | **no** (toText) | n/a | n/a — `text?` lift only | |
| `T → any` | **no** | n/a | n/a — `any?` is just `any` (any accepts none) | trivially total |
| `struct → struct'` (width-compat) | **no** at runtime (structural compat checked at compile time) | n/a | n/a | |

### Conversions where `T?` is "just" a lift (no try semantics gained)

For total conversions (rows above marked **no**), `convert(v):T?` is
semantically identical to `convert(v):T` followed by the implicit `T → T?` lift.
This is a feature, not a wart — the user can uniformly write `:T?` and pay
nothing extra for the conversions that can never fail. It also means
`convert(v):T?` is always a well-defined operation on the input type, no matter
what `T` happens to be.

---

## 4. Numeric narrowing

`convert(1000):byte` is the canonical example. The pragmatic design picks
**check + throw / none** over saturation:

- `convert(1000):byte` → throws `FunnyRuntimeException` (wraps
  `OverflowException` with message `"Cannot convert 1000 to type byte"`).
- `convert(1000):byte?` → returns `none`.
- `convert(255):byte` → `255`, OK.
- `convert(-1):byte` → throws / `none` (negative for unsigned).
- `convert(255.7):byte` → throws / `none` (real with fractional part is *not*
  silently truncated — we treat it as a soft failure; this is stricter than
  C# but consistent with NFun's "no surprise conversion" stance).
- `convert(255.0):byte` → `255`, OK (exact-integer real).
- `convert(NaN):int32` → throws / `none`.
- `convert(real.PositiveInfinity):int64` → throws / `none`.

**Why not saturate?** Saturation hides data loss silently. NFun has *strong*
typing as a design pillar — overflow should be visible. With the `T?`
annotation now available, the user has an ergonomic way to say "I want clamping
to 0 on overflow": `convert(big):byte? ?? 0`. Saturation would force every user
who *doesn't* want clamping to add their own overflow check.

**Real → integer fractional part.** Truncation is intentional only when the
fractional part is zero. For `convert(2.5):int32` the user should write
`convert(floor(2.5)):int32` or `convert(round(2.5)):int32` — explicit.
`T?` rescues accidents.

---

## 5. Composite types

### Array `T[] → U[]`

Element-wise convert: `convert([1,2,3]):real[]` = `[1.0, 2.0, 3.0]`.

- Static reject if `T → U` is statically rejected.
- Runtime failure mode is the **same** as the element conversion's failure mode
  — `convert(['1','x','3']):int[]` throws on element 1.
- `convert(['1','x','3']):int[]?` → `none` (whole-array rescue — first failing
  element makes the array become `none`).
- `convert(['1','x','3']):int?[]` → `[1, none, 3]` (per-element rescue —
  failures become `none` *inside* the array).
- `convert(['1','x','3']):int?[]?` → `[1, none, 3]` (both: per-element rescue
  in a never-failing array container).

This is consistent with optional-array placement rules in `Optionals.md` and
gives the user two clearly distinguished tools.

### text ↔ byte[] (Unicode encoding)

- `convert(text):byte[]` — Unicode (UTF-16 LE) bytes. Cannot fail in practice
  (any `char[]` round-trips). Implicit-lift to `byte[]?`.
- `convert(byte[]):text` — Unicode decode. Cannot fail in the .NET
  `Encoding.Unicode.GetString` sense — invalid surrogates produce the Unicode
  replacement character `U+FFFD` rather than throwing. We classify this as **no
  failure** in the `T?` sense — if the user wants to detect malformed input,
  they need a separate `decodeStrict` (out of scope for `convert`).

If a future stricter encoding mode (`UTF8.GetString` with throw-on-invalid) is
added, that becomes a 🪂 cell.

### struct conversion

- `convert(s):S2` where both are struct types: allowed **iff** `S` is
  width-compatible with `S2` (`S` has all of `S2`'s fields, with each field
  convertible). This is just NFun's existing structural subtyping carried into
  `convert`. Cannot fail at runtime — it's all checked at compile time. The
  `T?` annotation degenerates to an implicit lift.
- `convert(s):int` and other primitive targets — ✗ static reject. Structs do
  not project onto scalars.
- `convert(s):text` — ✓ via `toText`-style literal repr (existing behavior
  through the "Useless conversions" branch). No failure.

### `opt(T) → T` — the `!` operator's space

- `convert(x:int?):int` — semantically equivalent to `x!`. The cell is
  **🪂** with the *soft failure* being "x is `none`":
  - `convert(x):int` where `x = none` → throws
    (`"Cannot convert none to int"`).
  - `convert(x):int?` where `x = none` → `none` (degenerate — both source and
    target are optional; this is just identity).
- `convert(x:int?):real` — chains: unwrap, then convert. Strict throws on
  `none`; opt target returns `none`.
- `convert(x:int?):real?` — preserves `none` through the conversion. If `x =
  none`, result is `none`. If `x = 42`, result is `42.0`.

This means `convert(opt, T)` subsumes `!`-followed-by-`convert` and is more
flexible than either in isolation.

---

## 6. Failure-coalesce ergonomics

The whole point of the design is that `convert(_):T?` slots into the existing
`??` machinery naturally. Idiomatic patterns:

```py
# 1. Parse with default
port = convert(arg):int? ?? 8080

# 2. Multi-source fallback (CLI > env > default)
endpoint:ip = convert(cliArg):ip? ?? convert(envVar):ip? ?? 127.0.0.1

# 3. Validate-and-narrow (use narrowing on the optional result)
n = convert(userInput):int?
if (n != none and n > 0) processPositive(n)
else logError()

# 4. Per-element parse with skip
nums:int?[] = ['1','x','3'].map(rule convert(it):int?)   # [1, none, 3]
valid = nums.filterNotNull()                              # [1, 3]

# 5. Try a chain of conversions
asNumber:real? = convert(input):real?
asBool:bool? = convert(input):bool?
result = asNumber ?? (asBool == true ? 1.0 : 0.0)
```

The pattern `convert(x):T? ?? default` is the NFun-native expression of C#'s
`int.TryParse(s, out var n) ? n : default` — same intent, half the syntax,
zero out-parameters.

---

## 7. Spec changes (`Specs/Functions.md`)

Edits to apply when this design is accepted:

- **Add new top-level subsection** under `### Convertion tables for convert
  function` titled **"Try mode via optional target"**:
  > When the target type is `T?` (optional), `convert` runs in *try mode*:
  > any documented runtime conversion failure produces `none` instead of an
  > exception. When the target type is `T` (non-optional), `convert` runs in
  > *strict mode*: failures raise `FunnyRuntimeException`. The set of failures
  > that the `T?` annotation rescues is exactly the "soft" set: parse errors,
  > overflow, format mismatch, encoding-shape mismatch, force-unwrap of `none`.
  > Unrecoverable errors (out-of-memory, internal invariant violations) always
  > propagate regardless of target optionality.

- **Modify** the "Parsing" table (lines 89–96): replace "Raises `Oops`
  otherwise" with two columns "Strict target" / "Optional target":

  | Result type | Strict target `T`      | Optional target `T?`            |
  |-------------|------------------------|----------------------------------|
  | `bool`      | throws on bad input    | `none` on bad input              |
  | Integers    | throws on bad input    | `none` on bad input              |
  | `real`      | throws on bad input    | `none` on bad input              |
  | `ip`        | throws on bad input    | `none` on bad input              |

- **Modify** the Deserialization table (lines 77–87) similarly — `char` case
  ("throws otherwise" for wrong-length array) becomes try-rescued by `Character?`.

- **Add** "Numeric narrowing" table:

  | Source | Target | Strict | Optional |
  |--------|--------|--------|----------|
  | wider int | narrower int (value out of range) | throws | `none` |
  | real (NaN/Inf) | any integer | throws | `none` |
  | real (out of range / non-exact-integer) | any integer | throws | `none` |
  | signed negative | any unsigned | throws | `none` |

- **Add** to "Useless conversions" table a row for `Optional source`:

  | Argument type | Result Type | Description |
  |---------------|-------------|-------------|
  | `T?` (none)   | `T` (strict) | throws — equivalent to `!` on none |
  | `T?` (none)   | `T?`         | returns `none` (identity) |
  | `T?` (value)  | `T`          | unwraps and converts |
  | `T?` (value)  | `T?`         | unwraps, converts, re-wraps |

- **Modify** GH issue #51 follow-up: cross-reference this design doc.

---

## 8. Backward compatibility risk

**Existing strict callers are unchanged.** Today `convert(x):T` throws on
failure, and that behavior is preserved exactly. The only change visible to
existing code is the *message wrapping* — line 94's raw
`InvalidOperationException` becomes a `FunnyRuntimeException` with a better
message (already in MR5Bug2 scope). Tests that match on exception *type* may
need updating from `InvalidOperationException` to `FunnyRuntimeException`; tests
that match on message will see the new "Cannot convert … to type …" format.

**No code that previously compiled stops compiling.** `T?` target was already
legal — it just gave you a "lifted" conversion when there happened to be a
total `T` conversion, and a confusing FU error otherwise. Now `T?` is well
defined for *every* pair where the unwrapped `S → T` is statically possible.

**Risk surface:**

| Risk | Mitigation |
|------|------------|
| User writes `convert(x):int?` thinking it returns `int` | Documented in spec; `??` makes the resulting `int?` ergonomic. Type system catches misuse at compile time (can't pass `int?` where `int` is expected). |
| Silent data loss via try mode | Try mode requires an *explicit* `?` annotation. Default strict behavior is unchanged. |
| Test breakage from line 94 exception type change | Already required by MR5Bug2 — converging on `FunnyRuntimeException`. |
| Hidden hard exceptions | Documented allow-list of catchable types; everything else propagates. |

---

## 9. Code changes scope

### `src/NFun/Functions/ConvertFunction.cs` (~80 lines net add)

1. **At entry of `CreateConcrete`** (`+~15 lines`): detect optional target,
   unwrap, remember the flag:

   ```csharp
   var to = concreteTypes[0];
   var isTryMode = to.IsOptional;          // new FunnyType helper
   var effectiveTo = isTryMode ? to.OptionalElement : to;
   // ...existing body uses effectiveTo
   return wrapForTryMode(concreteConverter, isTryMode);
   ```

2. **New helper** `WrapForTryMode(ConcreteConverter inner, bool isTryMode,
   FunnyType outerTo)` (`+~25 lines`): if `isTryMode`, return a new
   `ConcreteConverter` whose `Calc` catches the soft-exception allow-list and
   returns the runtime's `none` sentinel; otherwise return `inner` unchanged.

3. **Soft-exception allow-list** (`+~10 lines`): a static `IsSoftFailure(Exception)`
   matching `FormatException`, `OverflowException`, `ArgumentException`,
   `ArgumentOutOfRangeException`, and `FunnyRuntimeException`. *Not* matching
   `OutOfMemoryException`, `StackOverflowException`, `NullReferenceException`,
   `InvalidCastException`.

4. **MR5Bug2 fix** (`~5 lines`): replace line 94's raw
   `InvalidOperationException` with `throw new FunnyRuntimeException(...)`.
   Use the `FU#` error code series (TBD when wiring into `Errors`).

5. **`opt(T) → T` rule** (`+~20 lines`): explicit unwrap path. New
   `if (from.IsOptional && !to.IsOptional)` branch that builds a converter:
   `o => o is None ? throw new FunnyRuntimeException("Cannot convert none to
   " + to) : innerConvert(o)`.

6. **`opt(T) → U?` chaining** (`+~10 lines`): if both are optional, propagate
   `none`; otherwise unwrap, convert, rewrap. Can be expressed by composing
   the above with the try-mode wrapper.

### `src/NFun/Types/FunnyType.cs` or equivalent (~5 lines)

- Confirm `IsOptional` / `OptionalElement` accessors exist (they do — used
  throughout TIC). No new types needed.

### Tests (`src/Tests/NFun.SyntaxTests/Conversions/`)

- ~30 new tests covering each 🪂 cell × {strict throws, opt returns none}.
- ~10 tests for array element-wise behavior (`U[] → V[]?` vs `U[] → V?[]` vs
  `U[] → V?[]?`).
- ~5 tests for opt-source unwrap (`convert(none):int` throws,
  `convert(none):int?` is `none`).
- ~5 tests for ✗ cells: confirm `FU` compile error even with `?` target.

**Total estimate**: ~120 lines production + ~250 lines tests. No TIC changes
required — the dispatch is entirely in `CreateConcrete` because the target
type is fully known there.

---

## 10. Pros / Cons

### vs strict-only (status quo)

**Pros**
- Single function, two failure modes — pleasing orthogonality.
- `??` integration is natural; matches `?[`, `?.` patterns.
- Eliminates the need for a separate `tryParse` namespace.
- "Parse with default" becomes a one-liner.
- Forward-compatible: every new converter rule automatically gains try mode.
- MR5Bug2 fix falls out naturally (raw exception goes away when wrapping is
  introduced).

**Cons**
- Two failure modes to teach. New users must internalize "annotation chooses
  behavior".
- Allow-list of catchable exceptions is a maintenance surface — adding a new
  source of failure means deciding "soft or hard?". Mitigated by clear
  documentation and a single helper to extend.
- Subtle: `convert(x):int? ?? 0` looks similar to but is *not* the same as
  `convert(x):int default 0` — the latter doesn't exist; users may invent
  syntax that doesn't.

### vs separate `tryParse` function family

**Pros**
- No namespace pollution (`tryParseInt`, `tryParseReal`, …).
- Generic — every target type, including new ones (e.g. `decimal` when added),
  gets try semantics for free.
- Polymorphic at the call site: `convert(x):T?` works in generic code where
  `T` is itself a type parameter.
- Drives semantics from the existing optional type machinery (TIC, narrowing,
  `??`) rather than introducing parallel mechanisms.
- Failure boundary is *visible in the type* (`T?` vs `T`) — not hidden inside
  a function name.

**Cons**
- Less discoverable via tab-completion than `tryParseInt`.
- Static and runtime failure modes are differentiated only by spec familiarity
  — newcomers may be surprised that `convert(s):bool?` rescues parse errors
  but `convert(b):ip?` does NOT rescue the (static-impossible) bool-to-ip
  conversion. Mitigated by clear FU error message: *"convert from bool to ip
  is structurally impossible; the `?` target rescues runtime failures only."*

### Net judgment

The pragmatic design pays a small teaching tax for substantial gains in
ergonomics, generality, and consistency with NFun's existing optional
discipline. It is the natural extension of `?.` / `?[` / `??` to the
conversion domain, and it makes the language feel more coherent.
