# Math Sugar

NFun supports Unicode math notation to make formulas more readable.

## Constants

| Symbol  | Value               | Type   |
|---------|---------------------|--------|
| `π`     | 3.14159265358979... | `real` |
| `∞`     | positive infinity   | `real` |

`π` and `∞` are built-in constants (a regular identifier always visible in scope).

```py
area = π * r²          # circle area
y = ∞                  # positive infinity
y = -∞                 # negative infinity
y = 2∞ + 1 == ∞        # true
```

## Operator Aliases

Unicode comparison operators are aliases for their ASCII equivalents:

| Unicode | ASCII  | Description      |
|---------|--------|------------------|
| `≤`     | `<=`   | less or equal    |
| `≥`     | `>=`   | greater or equal |
| `≠`     | `!=`   | not equal        |

They have the same precedence and behavior as the ASCII versions.

```py
y = 3 ≤ x and x ≤ 10   # range check
y = π ≤ 4               # true
y = ∞ ≠ 0               # true
y = 1 ≤ x ≤ 10          # equivalent to (1 ≤ x) and (x ≤ 10)
```

## Superscript Powers

Superscript digits `²` `³` `⁴` `⁵` `⁶` `⁷` `⁸` `⁹` are postfix power operators, equivalent to `**`:

```py
y = x²                  # x ** 2
y = x³                  # x ** 3
y = 3²                  # 9 (int)
y = 2.5²                # 6.25
```
Type behavior matches `**`: integer base with integer exponent returns integer.

Consecutive superscript digits are forbidden (ambiguous — would it mean chaining or multi-digit exponent?):

```py
y = 2²³                 # error!
```

Superscripts bind tighter than any binary operator:

```py
y = (x + 1)²            # (x + 1) ** 2
y = x² + x + 1          # (x ** 2) + x + 1
y = x² * 2              # (x ** 2) * 2
```

## Implicit Multiplication

You may skip `*` right after an integer or real constant when followed by a variable or `(`:

```py
y = 2x                  # 2 * x
y = 2π                  # 2 * π
y = 2∞                  # 2 * ∞ = ∞
y = 1.5x                # 1.5 * x
y = 2(x + 1)            # 2 * (x + 1)
```

Superscripts on the right operand bind first:

```py
y = 2x²                 # 2 * (x**2)
```

Implicit multiplication before a function call is forbidden:

```py
y = 2sin(x)             # error — use 2 * sin(x)
```

`0x` and `0b` prefixes are reserved for hex and binary literals:

```py
y = 0xFF                # hex literal 255
y = 0b1010              # binary literal 10
```
