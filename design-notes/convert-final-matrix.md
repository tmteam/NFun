# `convert(value):T` — Final Matrix (PRAGMATIC)

**Model**: `convert(x):T` throws on runtime failure; `convert(x):T?` returns `none` on the same failure. Compile error only for conversions without an algebraic morphism.

**Refinements from review:**
- C-style int↔bool/real↔bool (`0/NaN/null=false`, `non-zero=true`; `false=0, true=1`).
- real→int truncates silently for fractional, throws/none on overflow.
- ip→integer only into types preserving non-negative natural representation (u32/u64/i64).

---

## Legend

| Symbol | Meaning |
|---|---|
| **I** | **Implicit lift** — already free via subtyping. `convert` allowed as no-op. |
| **✓** | **Total** — always succeeds, no runtime failure possible. |
| **⚠** | **Total but lossy** — always succeeds, but data may be lost silently (truncation, precision). `:T?` adds no rescue. |
| **🪂** | **Soft-fallible** — `convert(x):T` throws `FunnyRuntimeException` on failure; `convert(x):T?` returns `none`. |
| **✗** | **Compile error** — no morphism exists. `:T?` does NOT rescue. FU error suggests alternative. |

**Combined marks** (e.g. `⚠🪂`): both apply depending on input — silently lossy in normal range, fails on overflow.

---

## 1. Primitive × Primitive

### 1.1 Numerics

Rows = source. Columns = target.

| from\to    | u8  | u16 | u32 | u64 | i16 | i32 | i64 |  real  |
|------------|-----|-----|-----|-----|-----|-----|-----|--------|
| **u8**     |  I  |  I  |  I  |  I  |  I  |  I  |  I  |   I    |
| **u16**    |  🪂 |  I  |  I  |  I  |  🪂 |  I  |  I  |   I    |
| **u32**    |  🪂 |  🪂 |  I  |  I  |  🪂 |  🪂 |  I  |   I    |
| **u64**    |  🪂 |  🪂 |  🪂 |  I  |  🪂 |  🪂 |  🪂 |  ⚠    |
| **i16**    |  🪂 |  🪂 |  🪂 |  🪂 |  I  |  I  |  I  |   I    |
| **i32**    |  🪂 |  🪂 |  🪂 |  🪂 |  🪂 |  I  |  I  |   I    |
| **i64**    |  🪂 |  🪂 |  🪂 |  🪂 |  🪂 |  🪂 |  I  |  ⚠    |
| **real**   | ⚠🪂 | ⚠🪂 | ⚠🪂 | ⚠🪂 | ⚠🪂 | ⚠🪂 | ⚠🪂 |   I    |

**Rules:**
- Widening (any → wider) per subtype lattice: **I**.
- Numeric narrowing: **🪂** (throws on overflow, `:T?` gives none on overflow).
- `u64→real` and `i64→real`: **⚠** (always succeeds, precision loss above 2^53 — silent).
- `real→integer`: **⚠🪂** — silently truncates fractional part, throws/none on overflow.

### 1.2 bool ↔ numeric (C-style)

| from\to    | u8 | u16 | u32 | u64 | i16 | i32 | i64 | real | bool |
|------------|----|-----|-----|-----|-----|-----|-----|------|------|
| **bool**   | ✓  |  ✓  |  ✓  |  ✓  |  ✓  |  ✓  |  ✓  |  ✓   |  I   |
| **u8**     |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |
| **u16**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |
| **u32**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |
| **u64**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |
| **i16**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |
| **i32**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |
| **i64**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |
| **real**   |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  ✓   |

**Semantics (C-style):**
- `bool → int`: `false → 0`, `true → 1`. ✓ total.
- `int → bool`: `0 → false`, any non-zero → `true`. ✓ total.
- `real → bool`: `0.0 → false`, `±0.0 → false`, **`NaN → false`**, finite non-zero → `true`. ✓ total.

### 1.3 char ↔ numeric

`char` = UTF-16 code unit (uint16-backed).

| from\to  | u8 | u16 | u32 | u64 | i16 | i32 | i64 | real | bool | char |
|----------|----|-----|-----|-----|-----|-----|-----|------|------|------|
| **char** | 🪂 |  ✓  |  ✓  |  ✓  | 🪂  |  ✓  |  ✓  |  ✓   |  ✗   |  I   |
| **u8**   |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  ✓   |
| **u16**  |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  ✓   |
| **u32**  |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  🪂  |
| **u64**  |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  🪂  |
| **i16**  |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  🪂  |
| **i32**  |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  🪂  |
| **i64**  |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  🪂  |
| **real** |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  🪂  |

**Rules:**
- `char → u16/u32/u64/i32/i64/real`: ✓ (code unit always fits).
- `char → u8/i16`: 🪂 (high code units overflow).
- `char ↔ bool`: ✗ (no canonical mapping).
- `u8/u16 → char`: ✓ (any u8/u16 is a valid code unit, including surrogates — confirmed per .NET semantics).
- `u32+ / i* / real → char`: 🪂 (must fit u16 code-unit range; signed must be non-negative).
- `bool → char`: ✗.

### 1.4 ip ↔ numeric (REFINED — non-negative preservation)

IP = IPv4 = 4 bytes ≡ unsigned 32-bit value `[0, 2^32-1]`.

| from\to    | u8 | u16 | u32 | u64 | i16 | i32 | i64 | real | bool | char |
|------------|----|-----|-----|-----|-----|-----|-----|------|------|------|
| **ip**     | ✗  |  ✗  |  ✓  |  I  |  ✗  |  **✗**  |  ✓  |  ✗   |  ✗   |  ✗   |
| **u32**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  …   |  ✓   |
| **u64**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  …   |  🪂  |
| **i32**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  …   |  🪂  |
| **i64**    |  … |  …  |  …  |  …  |  …  |  …  |  …  |  …   |  …   |  …   |  🪂  |
| others     |  ✗ |  ✗  |  ✗  |  ✗  |  ✗  |  ✗  |  ✗  |  ✗   |  ✗   |  ✗   |

**Rules (per review):**
- `ip → u32`: **✓** (natural IPv4 representation).
- `ip → u64`: **I** (widening from u32).
- `ip → i64`: **✓** (widening, u32 max < i64 max, always positive).
- `ip → i32`: **✗** **COMPILE ERROR** (would produce negative for high IPs — loses non-negative algebraic identity).
- `ip → u8/u16/i16`: ✗ (insufficient bits).
- `ip → real/bool/char`: ✗ (no morphism).
- `u32 → ip`: ✓ (total, u32 ↔ 4 bytes).
- `u64 → ip`: 🪂 (must fit u32 = `[0, 2^32-1]`).
- `i32 → ip`: 🪂 (must be non-negative).
- `i64 → ip`: 🪂 (must be non-negative AND fit u32).
- `u8/u16/i16/real/bool/char → ip`: ✗.
- `text → ip`: 🪂 (parse).

### 1.5 Universal: text, any

| from\to | text | any |
|---|---|---|
| Any primitive (u8…real, bool, char, ip) | **✓** (toText) | **I** |
| **text** | I | I |
| **any** | ✓ | I |
| Any composite (T[], struct, opt) | ✓ (toText) | I |

- `X → text`: **✓** universally (toText is always defined).
- `X → any`: **I** (top of lattice).
- `any → text`: ✓.
- `any → X` (X ≠ text, ≠ any): **🪂** (runtime tag dispatch may fail).

### 1.6 text ↔ everything (parsing)

| Direction | Class | Notes |
|---|---|---|
| `text → u8/u16/u32/u64/i16/i32/i64/real` | **🪂** | `int.Parse(invariant)` — throws on format or overflow |
| `text → bool` | **🪂** | accepts `"true"`/`"false"`/`"1"`/`"0"` (case-insensitive), else throws |
| `text → ip` | **🪂** | `IPAddress.Parse` |
| `text → char` | **🪂** | only if `len == 1`, else throws |
| `text → text` | **I** | identity |
| `text → byte[]` | **✓** | Unicode encoding (UTF-16 LE), total |
| `byte[] → text` | **✓** | Unicode decode, invalid bytes get replacement char |
| `text → char[]` | **I** | text IS char[] |

---

## 2. Composite types

### 2.1 Arrays `T[] → U[]`

```
class(T[] → U[]) = class(T → U)            (lift per element)
```

| Element class | Array class |
|---|---|
| `T → U` is **I** | **I** |
| `T → U` is **✓** | **✓** |
| `T → U` is **⚠** | **⚠** |
| `T → U` is **🪂** | **🪂** — `:U[]` throws on first failing element; `:U?[]` gives `[some/none/some/...]` |
| `T → U` is **✗** | **✗** |

**Examples:**
- `convert(["1","2","3"]):int[]` → `[1,2,3]` (parse succeeds for each).
- `convert(["1","x","3"]):int[]` → throws at element `"x"`.
- `convert(["1","x","3"]):int?[]` → `[some 1, none, some 3]` (per-element try).

Notes:
- The try-target for arrays of fallible-element-conversions is `U?[]` (array of optionals), NOT `U[]?`. Individual elements may fail; the whole array doesn't.
- `byte[] ↔ text` are special-cased in §1.6.
- `T[] → text`: ✓ (toText of array repr).
- `T[] → any`: I.

### 2.2 Structs

```
S{f₁:A₁,…,fₘ:Aₘ}   →   T{f₁:B₁,…,fₙ:Bₙ}        (n ≤ m, every target field must exist on source)

class(S → T) = worst-case combination of class(Aᵢ → Bᵢ) over the target fields
```

| Case | Class |
|---|---|
| Target has field missing on source | **✗** |
| All shared fields **I** | **I** (= regular width subtyping; `MR5Bug6` fix already enforces narrow apparent shape) |
| All shared fields ✓/I (no 🪂/⚠) | **✓** |
| Any shared field **🪂** | **🪂** — throws on first per-field failure, or `:T?` rescues whole struct |
| Any shared field **⚠** | **⚠** |
| Any shared field **✗** | **✗** |

- `struct → primitive`: ✗.
- `primitive → struct`: ✗.
- `struct → text`: ✓ (toText).
- `struct → any`: I.

### 2.3 Optional `opt(T)`

```
opt(A) → opt(B)        class(A → B)                  (apply through wrapper; none preserved)
opt(A) → B             🪂 always                      (none can't become B; if source is none, throws/rescued)
A      → opt(B)        class(A → B), then lift       (=I if class is I)
opt(A) → text          ✓ (toText handles none)
opt(A) → any           I
opt(A) → byte[]        ✗ (no canonical repr for none)
```

`convert(x:opt(int)):int` is **🪂** — throws if x is `none`. User can write `convert(x):int?` (preserves none) or `convert(x!):int` (force-unwrap then convert).

### 2.4 `any → T`

| Target | Class |
|---|---|
| `text` | **✓** (toText) |
| `any` | **I** |
| Any other concrete type | **🪂** (runtime tag check; throws/none) |

---

## 3. The `:T` vs `:T?` rule

For every cell in the matrix:

| Cell class | `convert(x):T` behavior | `convert(x):T?` behavior |
|---|---|---|
| **I** | identity / lift, returns `T` | identity / lift, returns `T?` |
| **✓** | always succeeds, returns `T` | always succeeds, returns `T?` |
| **⚠** | always succeeds with silent data loss, returns `T` | same, lifted to `T?` |
| **🪂** | throws `FunnyRuntimeException` on failure | returns `none` on failure |
| **✗** | **compile error** | **compile error** (the `?` doesn't rescue) |

The `?` on the target type **only** affects 🪂 cells. It does not create morphisms.

---

## 4. Bugs eliminated by this design

| Current behavior | New behavior |
|---|---|
| `out:int = convert(true)` raw `InvalidOperationException` (MR5Bug2) | ✓ — works (returns 1) |
| `convert("foo"):int` raw exception text | 🪂 — clean `FunnyRuntimeException` "cannot parse 'foo' as int" |
| `convert("foo"):int?` (today: same exception) | 🪂 → `none` |
| `convert(int64-big):int32` silent or random | 🪂 — clean `FunnyRuntimeException`; `:int32?` → none |
| `convert(1.5):int` (currently: maybe truncates, maybe throws) | ⚠ — silently truncates to 1 (documented) |
| `convert(opt(int)-none):int` | 🪂 throws; `:int?` → none |
| `convert(myip):int` (i32) | **✗** compile error: "ip → i32 is not allowed (negative values); use `:uint` or `:long`" |
| `convert(struct):int` | ✗ compile error |

---

## 5. New compile errors introduced

The compile errors are limited to **structurally impossible** conversions. Examples:

```
FU<N>: cannot convert <source> to <target>.
       No conversion rule exists between these types.
       <hint based on case>

Hints:
  ip → i32             →  "Use :uint (natural) or :long (widening) instead — i32 would lose
                            non-negative property for high IPs."
  struct → primitive   →  "Structs are not convertible to scalars. Access a specific field."
  primitive → struct   →  "Scalars are not convertible to structs. Construct one with {f=…}."
  bool ↔ char          →  "No canonical bool/char mapping. Use `if(b) /'1' else /'0'` or similar."
  bool → ip            →  "Booleans cannot become IPs."
  real → char          →  "Real values are not codepoints; convert via integer first."
  text → struct/T[]    →  "Parsing into structures requires a dedicated parser function."
```

Critically: **`convert("foo"):int` does NOT become a compile error** — text→int is 🪂 (allowed, may fail at runtime). The compile error is only when the morphism doesn't exist at all.

---

## 6. tryParse / similar functions

Per review: keep them as readable aliases. `tryParse(s):int?` is sugar for `convert(s):int?` — semantically identical but more self-documenting at call sites where intent matters.

---

## 7. Spec edits to `Specs/Functions.md` lines 50-96

Replace the current 4-section "Conversion tables" with:
1. **Morphism classes** — explainer for I/✓/⚠/🪂/✗ and the `:T?` rule (§3).
2. **Primitive matrix** — collapse §1.1–§1.6 into a single table.
3. **Composite rules** — condense §2.
4. **Optional rules** — §2.3 + §3 together.
5. **C-style int↔bool/real↔bool** — explicit note (§1.2).
6. **ip ↔ integer special rules** — §1.4 (the i32 rejection is non-obvious).

---

## 8. Implementation roadmap

1. Add a `ConvertClass IsConvert(FunnyType from, FunnyType to) → { I, Total, Lossy, Soft, Impossible }` helper.
2. In `ConvertFunction.CreateConcrete`:
   - Detect target opt-ness. If target is `opt(U)`, classify `(from, U)` and use try-wrap if class == Soft. If Impossible → FU error.
   - If target is `T` (non-opt), classify `(from, T)`. If Total/Lossy/I → run as today. If Soft → run as today (existing throws stay, but with cleaner exception messages). If Impossible → FU error.
   - Remove the `throw new InvalidOperationException(...)` catch-all (MR5Bug2 fixed).
3. Add C-style converters:
   - `bool ↔ int*` (`false=0`, `true=1`; non-zero=`true`).
   - `real ↔ bool` (`0/NaN=false`, else `true`).
4. Add `ip → i32` rejection in `IsConvert` (this is the only currently-working pair that becomes Impossible).
5. Verify `real → int` truncation semantics match (`(int)1.5 = 1` per current `Convert.ToInt32` behaviour; overflow throws).
6. Audit existing converters in `ConvertFunction.cs` and tag each by class for the table.

---

## 9. Test plan

- **Unit (Nfun.UnitTests)**: one assertion per matrix cell — `IsConvert(from, to) == expectedClass`.
- **Syntax (NFun.SyntaxTests)**:
  - One ✓ pass case + one 🪂 throws case per soft-fallible cell.
  - For each 🪂 cell: `:T?` returns none on the same failing input.
  - One ✗ case per impossible cell (FU error code + intelligible message).
  - C-style int↔bool/real↔bool round-trips.
  - `ip → i32` rejection (FU error mentions `:uint` / `:long` alternatives).
- **Specific regression**: MR5Bug2 — `out:int = convert(true)` returns 1, no raw exception.
