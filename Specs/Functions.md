# NFun List of Standard Functions

Most functions may be applied for different types of operands. To simplify the description, we will give names to some sets of types:

| Name          | Types                                                                    | Formal constrains                                   |
|---------------|--------------------------------------------------------------------------|-----------------------------------------------------|
| All           | All types                                                                | T => any                                            |
| Integers      | `int64`, `int32`,`int16`, `int8`, `uint64`, `uint32`, `uint16`, `byte`   | T => (int64 &#124; uint64)                          |
| Floats        | `float32`, `float64` (≡`real`)                                           | float32 => T => real                                |
| Numbers       | **[Floats]**, **[Integers]**                                             | T => real                                           |
| Signed        | `real`, `float32`, `int64`, `int32`, `int16`, `int8`                     | int8 => T => real                                   |
| Arithmetics   | `real`, `float32`, `int64`, `int32`, `uint64`, `uint32`                  | (int32 &#124; uint32) => T => real                  |
| Comparables   | `text`, `char`, **[Numbers]**                                            | T is IComparable                                    |

## Built-in constants

| Constant | Type | Value |
|----------|------|-------|
| `π`      | real | 3.14159265... |
| `∞`      | real | positive infinity |

## Math functions

All math functions are **generic over Floats**: input and output share the same float
type T ∈ {`float32`, `float64`} (with `float64 ≡ real`). Float32 backings use
`System.MathF` (single-precision); float64/real backings use `System.Math` (double)
or `System.Decimal` per `RealClrType` dialect.

| Function              | Constrains | Returns	                                                                |
|-----------------------|------------|------------------------------------------------------------------------------|
| sqrt(T):T             | Floats     | the square root of a specified number                                        |
| cos(T):T              | Floats     | the cosine of the specified angle                                            |
| sin(T):T              | Floats     | the sine of the specified angle                                              |
| tan(T):T              | Floats     | the tangent of the specified angle                                           |
| atan(T):T             | Floats     | the angle whose tangent is the specified number                              |
| atan2(y:T, x:T):T     | Floats     | the angle whose tangent is the quotient of two specified numbers             |
| asin(T):T             | Floats     | the angle whose sine is the specified number                                 |
| acos(T):T             | Floats     | the angle whose cosine is the specified number	                              |
| exp(T):T              | Floats     | e raised to the specified power	                                             |
| log(T):T              | Floats     | the natural (base e) logarithm of a specified number                         |
| log(T,T):T            | Floats     | the logarithm of a specified number in a specified base.                     |
| log10(T):T            | Floats     | the base 10 logarithm of a specified number                                  |
| avg(T[]):T            | Floats     | Computed average of an array of floats. Throws if input array is empty       |
| round(T,digits:int):T | Floats     | the value rounded to the specified number of digits                          |
| ceil(T):T             | Floats     | the smallest integer greater than or equal to the argument. `ceil(7.03)` → `8.0` |
| floor(T):T            | Floats     | the largest integer less than or equal to the argument. `floor(7.03)` → `7.0`    |
| range(from:T, to:T):T[]    | Numbers | array of numbers from `from` to `to` inclusive. `range(1,3)` → `[1,2,3]`. Descending if from > to |
| range(from:T, to:T, step:T):T[] | Numbers | array with step. `range(1,10,3)` → `[1,4,7,10]`                  |

## Generic functions
| Function       | Constrains     | Returns	                                                                                                                                   |
|----------------|----------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| abs(T):T       | Signed, `real` | the absolute value                                                                                                                         |
| sign(T):int32  | Signed, `real` | -1 if negative, 0 if zero, +1 if positive. Always returns `int32`. NaN propagates per IEEE 754.                                            |
| min(T,T):T     | Comparables    | first or second argument, whichever is smaller. If any argument is equal to NaN (in case if T is real and real is double), NaN is returned |
| max(T,T):T     | Comparables    | first or second argument, whichever is bigger. If any argument is equal to NaN (in case if T is real and real is double), NaN is returned  |
| convert(TA):TR | ----           | Converts an argument of type `TA` to type `TR`. See the conversion specification below.                                  |

### `convert` specification

Every conversion `convert(value):T` is classified into one of five **classes** by the pair (source type, target type). The class determines static and runtime behavior:

| Class | Symbol | Static (compile time) | Runtime |
|-------|--------|-----------------------|---------|
| Implicit | **I** | accepted | no-op (already free via subtyping) |
| Total    | **✓** | accepted | always succeeds |
| Lossy    | **⚠** | accepted | always succeeds; data silently lost (truncation, precision) |
| Soft     | **🪂** | accepted | `convert(x):T` throws `Oops` on failure; `convert(x):T?` returns `none` on the same failure |
| None     | **✗** | **compile error** (`FU`); `:T?` does NOT rescue | — |

The `?` on a target type **only** affects 🪂 conversions: it replaces the runtime throw with `none`. It does not create morphisms, so ✗ conversions stay rejected even with `:T?`.

#### Primitive matrix

Rows = source. Columns = target. (Aliases: `byte ≡ uint8`, `sbyte ≡ int8`, `int ≡ int32`, `uint ≡ uint32`, `real ≡ float64`, `text ≡ char[]`.)

| from\to    | u8 | u16 | u32 | u64 | i8 | i16 | i32 | i64 | f32 | real | bool | char | text | ip |
|------------|----|-----|-----|-----|----|-----|-----|-----|-----|------|------|------|------|----|
| **u8**     | I  | I   | I   | I   | 🪂 | I   | I   | I   | I   | I    | ✓    | ✓    | ✓    | ✗  |
| **u16**    | 🪂 | I   | I   | I   | 🪂 | 🪂  | I   | I   | I   | I    | ✓    | ✓    | ✓    | ✗  |
| **u32**    | 🪂 | 🪂  | I   | I   | 🪂 | 🪂  | 🪂  | I   | ⚠   | I    | ✓    | 🪂   | ✓    | ✓  |
| **u64**    | 🪂 | 🪂  | 🪂  | I   | 🪂 | 🪂  | 🪂  | 🪂  | ⚠   | ⚠    | ✓    | 🪂   | ✓    | 🪂 |
| **i8**     | 🪂 | 🪂  | 🪂  | 🪂  | I  | I   | I   | I   | I   | I    | ✓    | 🪂   | ✓    | ✗  |
| **i16**    | 🪂 | 🪂  | 🪂  | 🪂  | 🪂 | I   | I   | I   | I   | I    | ✓    | 🪂   | ✓    | ✗  |
| **i32**    | 🪂 | 🪂  | 🪂  | 🪂  | 🪂 | 🪂  | I   | I   | ⚠   | I    | ✓    | 🪂   | ✓    | 🪂 |
| **i64**    | 🪂 | 🪂  | 🪂  | 🪂  | 🪂 | 🪂  | 🪂  | I   | ⚠   | ⚠    | ✓    | 🪂   | ✓    | 🪂 |
| **f32**    | ⚠🪂| ⚠🪂 | ⚠🪂 | ⚠🪂 | ⚠🪂| ⚠🪂 | ⚠🪂 | ⚠🪂 | I   | I    | ✓    | 🪂   | ✓    | ✗  |
| **real**   | ⚠🪂| ⚠🪂 | ⚠🪂 | ⚠🪂 | ⚠🪂| ⚠🪂 | ⚠🪂 | ⚠🪂 | ⚠🪂 | I    | ✓    | 🪂   | ✓    | ✗  |
| **bool**   | ✓  | ✓   | ✓   | ✓   | ✓  | ✓   | ✓   | ✓   | ✓   | ✓    | I    | ✗    | ✓    | ✗  |
| **char**   | 🪂 | ✓   | ✓   | ✓   | 🪂 | 🪂  | ✓   | ✓   | ✓   | ✓    | ✗    | I    | ✓    | ✗  |
| **text**   | 🪂 | 🪂  | 🪂  | 🪂  | 🪂 | 🪂  | 🪂  | 🪂  | 🪂  | 🪂   | 🪂   | 🪂   | I    | 🪂 |
| **ip**     | ✗  | ✗   | ✓   | I   | ✗  | ✗   | **✗**| ✓  | ✗   | ✗    | ✗    | ✗    | ✓    | I  |
| **any**    | 🪂 | 🪂  | 🪂  | 🪂  | 🪂 | 🪂  | 🪂  | 🪂  | 🪂  | 🪂   | 🪂   | 🪂   | ✓    | 🪂 |

Reading the cells:
- **Widening** within the numeric subtype lattice (`u8 ≤ u16 ≤ u32 ≤ u64`, `i8 ≤ i16 ≤ i32 ≤ i64`, `u8 ≤ i16 ≤ i32 ≤ i64`, `u16 ≤ i32 ≤ i64`, `u32 ≤ i64`, small ints `≤ f32`, `f32 ≤ real`) → **I**.
- **Narrowing** → **🪂** (throws on overflow; `:T?` gives `none`).
- **`f32 / real → integer`** → **⚠🪂** — fractional part silently truncated (`1.5 → 1`); throws/`none` on overflow.
- **`u64 → real`, `i64 → real`, wide-int → f32**, **`real → f32`** → **⚠** — silent precision loss (f32 mantissa = 24 bits, real ≈ 53 bits).
- **`bool ↔ numeric`** is **C-style**: `false ↔ 0`, `true ↔ 1` (back-direction); `int → bool`: `0 → false`, any non-zero → `true`. `real → bool`: `0.0/±0.0/NaN → false`, finite non-zero → `true`. All total.
- **`char ↔ numeric`**: `char` is a UTF-16 code unit. `char → u16+/i32+/real` is **✓**; `char → u8/i8/i16` is **🪂** (overflow). `u8/u16 → char` is **✓** (every u8/u16 is a valid code unit, including surrogates). Wider integer or signed → `char` is **🪂**.
- **`bool ↔ char`** → **✗** (no canonical mapping).
- **`X → text`** is **✓** universally (equivalent to `toText(X)`).
- **`text → X`** (X ≠ text) is **🪂** (parse; `int.Parse(invariant)`, `bool` accepts `"true"`/`"false"`/`"1"`/`"0"` case-insensitive, `ip` via `IPAddress.Parse`, `char` only if `len == 1`).
- **`ip ↔ integer`**: only into types preserving the non-negative natural representation. `ip → u32` ✓ (exact), `ip → u64/i64` ✓/I (widening), **`ip → i32` ✗** (compile error — would produce negative for high IPs; use `:uint` or `:long`). `ip → u8/u16/i16/real` ✗. Reverse: `u32 → ip` ✓, `u64/i32/i64 → ip` 🪂 (must fit `[0, 2³²-1]`), narrower or non-integer → ✗.
- **`X → any`** is **I**; **`any → X`** (X ≠ text, ≠ any) is **🪂** (runtime tag dispatch).

#### Composite rules

| Pair | Class |
|---|---|
| `T → T` (same type) | **I** |
| `T → any` | **I** |
| `T → text` | **✓** (= `toText(T)`) |
| `T[] → U[]` | class of `T → U`, lifted element-wise. For 🪂: `:U[]` throws on first failing element, `:U?[]` returns `[some/none/...]`. |
| `S{f₁:A₁, …} → T{f₁:B₁, …, fₙ:Bₙ}` | each target field `fᵢ` must exist on source; class is the worst (per ordering ✗ > 🪂 > ⚠ > ✓ > I) of `class(Aᵢ → Bᵢ)` |
| Target field missing on source | **✗** |
| `struct ↔ primitive`, `primitive ↔ struct` | **✗** |
| `opt(A) → opt(B)` | class of `A → B` (applied through wrapper, `none` preserved) |
| `opt(A) → B` (non-opt target) | **🪂** (throws/none if source is `none`) |
| `A → opt(B)` | class of `A → B`, result lifted into `opt` |
| `opt(A) → text/any` | inherits `✓` / `I` |
| `opt(A) → byte[]` | **✗** (no canonical byte representation for `none`) |

#### Serialization (`X → byte[]` and `byte[] → X`)

`byte[]` is treated as a target/source like any other composite — classes apply per the matrix:

| Pair | Class | Encoding |
|---|---|---|
| `text → byte[]` | **✓** | UTF-16 LE |
| `byte[] → text` | **✓** | UTF-16 LE decode (invalid bytes → replacement char) |
| numeric → `byte[]` | **✓** | little-endian, native width (u8=1, u16=2, u32=4, u64=8, i16=2, i32=4, i64=8, real=8) |
| `byte[] → numeric` | **🪂** | requires exact length match for the target width |
| `bool → byte[]` | **✓** | `[1]` / `[0]` |
| `byte[] → bool` | **🪂** | length 1, value 0 or 1 |
| `char → byte[]` | **✓** | 2 bytes UTF-16 LE |
| `byte[] → char` | **🪂** | length 1 (ASCII) or 2 (UTF-16) |
| `ip → byte[]` | **✓** | 4 bytes network order |
| `byte[] → ip` | **🪂** | length 4 |
| `T[] → byte[]` (non-byte T) | **✗** | use `arr.flat(map(...))` |
| `struct/opt → byte[]` | **✗** | use `toJson` or similar |

Conversion to bit array `bool[]` follows the same matrix as `byte[]` with bytes split into bits.

#### Failure mode summary

```
convert(x):T          — runtime: throws Oops on 🪂 failure; compile error on ✗
convert(x):T?         — runtime: returns none on 🪂 failure; compile error on ✗
convert(x:opt(S)):T   — runtime: throws on none; compile error on ✗
convert(x:opt(S)):T?  — runtime: none stays none; compile error on ✗
convert(x!):T         — force-unwrap source first; then per (S, T) class
```

#### Implementation status

The matrix above is the **specified** behavior. The current runtime implements
all primitive ↔ primitive cells, plus `opt(A)` source/target propagation,
`any → T` runtime tag dispatch, and `byte[]` (de)serialization (strict-length).

**Not yet implemented** — falls back to a compile-time `FU887` reject:

| Pair | Status |
|---|---|
| `T[] → U[]` element-wise when neither `T` nor `U` is `byte`/`bool` | deferred (e.g. `text[] → int[]`) |
| `S{...} → T{...}` width-subtyping field-wise convert via `convert()` | deferred (assignment-level width subtyping at `:T = ...` boundary IS supported per `Specs/Types.md` §Type casting) |

Width subtyping at assignment (`b:{x:int} = {x=1,y=2}`) is unaffected — it
goes through the type-inference path, not `convert()`. Only the explicit
`convert(value):T[]` / `convert(value):T{...}` forms are pending. The
implementation gap is tracked in test cases marked
`[Ignore("convert-deferred: complex composite conversions")]`.

## Generic Array Functions

### Appliable to any arrays (without type constrains)
| Function                           | Returns	                                                                                                                                                                 |
|------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| first(arr:T[]):T                   | first element of array. Throws if array is empty                                                                                                                         |
| last(arr:T[]):T                    | last element of array. Throws if array is empty                                                                                                                          |
| set(arr:T[],index:int,value:T):T[] | copy of array with element at index replaced                                                                                                                             |
| count(T[]):int                     | array size                                                                                                                                                               |
| count(T[], rule(T)->bool):int       | the number of array elements for which the rule is satisfied                                                                                                             |
| find(T[], T):int                   | index of the first element equal to the specified, -1 if no such element found                                                                                           |
| chunk(T[],n:int):T[][]             | Chunks (splits) the array into many smaller arrays with size of n. Last array may have size less than n. Throws if n is non positive                                     |
| flat(T[][]):T[]                    | Array with sequential enumeration of elements of all specified subarays                                                                                                  |
| fold(T[],rule(T,T)->T):T            | Applies an accumulator rule over an array. Throws if given array is empty                                                                                                |
| fold(T[],seed:TR,rule(TR,T)->TR):TR | Applies an accumulator rule over an array. The specified seed value is used as the initial accumulator value, and the specified rule is used to select the result value. |
| unite(T[],T[]):T[]                 | the set union of two arrays                                                                                                                                              |
| unique(T[],T[]):T[]                | array of elements that are contained in only one of the arrays                                                                                                           |
| intersect(T[],T[]):T[]             | array of elements that are contained in both arrays                                                                                                                      |
| concat(T[],T[]):T[]                | concatenation of two arrays                                                                                                                                              |
| append(T[],T):T[]                  | array of elements from given array and specified element in the tail of resulting array                                                                                  |
| except(T[],T[]):T[]                | array of elements that are contained in first array and not contained in second                                                                                          |
| map(T[],rule(T)->TR):TR[]           | Projects each element of a given into a new form by given rule                                                                                                           |
| any(T[]):bool                      | `true` if the array is non-empty                                                                                                                                         |
| any(T[],rule(T)->bool):bool         | `true` if the specified rule is satisfied at least for single element of given array. `false` if it is not, or array is empty                                            |
| all(T[],rule(T)->bool):bool         | `true` if the specified rule is satisfied for all elements of given array. `true` if array is empty (vacuous truth)                                                      |
| filter(T[],rule(T)->bool):T[]       | an array consisting of elements of the original array for which the rule is satisfied                                                                                    |
| repeat(element:T,count:int):T[]    | an array in which the specified element is repeated n times. Throws if count is negative                                                                                 |
| reverse(T[]):T[]                   | reversed array                                                                                                                                                           |
| take(T[],n:int):T[]                | takes first n elements of given array. Equals to `[:n]` operator call                                                                                                    |
| skip(T[],n:int):T[]                | array, where first n elements of given array are skipped. Equals to `[n:]` operator call                                                                                 |

| Function                          | Constrains                 | Returns	                                                                                          |
|-----------------------------------|----------------------------|---------------------------------------------------------------------------------------------------|
| sum(T[]):T                        | Arithmetics                | the sum of all the elements of the array                                                          |
| sum(T[], rule(T)->TR):TR           | Arithmetics                | the sum of transformed elements                                                                   |
| max(T[]):T                        | Comparables                | the maximum element in array. Throws if array is empty                                            |
| min(T[]):T                        | Comparables                | the minimum element in array. Throws if array is empty                                            |
| median(T[]):T                     | Comparables                | median element. Throws if array is empty                                                          |
| sort(T[]):T[]                     | Comparables                | sorted array                                                                                      |
| sortDescending(T[]):T[]           | Comparables                | sorted array in reverse order                                                                     |
| sort(T[],rule(T)->R):T[]           | T is All, R is Comparables | sorted array, where the element being compared is obtained by the specified rule                  |
| sortDescending(T[],rule(T)->R):T[] | T is All, R is Comparables | Sorted array in reverse order, where the element being compared is obtained by the specified rule |

## Text Functions
| Function                          | Returns	                                                                                                                |
|-----------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| toText(any):text                  | text presentation of given value                                                                                        |
| trim(text):text                   | removes all leading and trailing white-space characters from the current text                                           |
| trimStart(text):text              | removes all the leading white-space characters from the current text                                                    |
| trimEnd(text):text                | removes all the trailing white-space characters from the current string                                                 |
| toUpper(text):text                | a copy of this string converted to uppercase                                                                            |
| toLower(text):text                | a copy of this string converted to lowercase                                                                            |
| split(text,separator:text):text[] | splits a text into subtexts that are based on the provided text separator. Empty entries are removed                    |
| join(any[],separator:text):text   | concatenation of text representations of an value array, using the specified separator between each element.            |

## Error Functions

| Function | Returns |
|----------|---------|
| oops():T | Throws `FunnyRuntimeException("oops")`. Return type is generic — can be used in any context. |
| oops(text):T | Throws `FunnyRuntimeException` with the given message. |
| oops(text, any):T | Throws `FunnyRuntimeException` with message and data payload. |

## Formatting Functions

| Function | Returns |
|----------|---------|
| toNumText(real, decimals:int=2, minDigits:int=0, thousands:bool=false, forceZeros:bool=true):text | formatted number. `toNumText(3.14, decimals=4)` → `'3.1416'` |
| toHexText(int64):text | hexadecimal representation. `toHexText(255)` → `'FF'` |
| toBinText(int64):text | binary representation. `toBinText(42)` → `'101010'` |
| toSciText(real, uppercase:bool=true):text | scientific notation. `toSciText(3.14)` → `'3.140000E+000'` |
| padLeftText(text, width:int):text | pads with spaces on the left. `padLeftText('hi', 10)` → `'        hi'` |
| padRightText(text, width:int):text | pads with spaces on the right. `padRightText('hi', 10)` → `'hi        '` |
| padCenterText(text, width:int):text | pads with spaces on both sides. `padCenterText('hi', 10)` → `'    hi    '` |
