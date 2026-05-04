# NFun-Lang: Indent Rules Specification

## Core Principle

Indentation defines block structure. The rules are designed to prevent common bugs
while being flexible enough for different coding styles.

## Rules

### 1. Tabs vs Spaces

A file uses either tabs OR spaces for indentation, never both.

- The **first indented line** in a file determines the style (tab or space).
- All subsequent indentation in the same file must use the same style.
- **Mixed tabs and spaces in one file → COMPILE ERROR.**

```
# OK: spaces throughout
fun foo():
    return 1

# OK: tabs throughout  
fun foo():
→   return 1

# ERROR: file started with spaces, then used tab
fun foo():
    x = 1
→   return x    # ERROR: mixed tabs and spaces
```

### 2. Indent Size

Any indent size is valid (1, 2, 3, 4, 8 spaces, or 1 tab).

The **first indent** after each `:` determines the expected indent size for that block.
All statements in the same block must use the **exact same indent level**.

```
# OK: 2-space indent
fun foo():
  return 1

# OK: 4-space indent
fun bar():
    return 2

# OK: different sizes in different blocks (not recommended but valid)
fun outer():
    if true:
      return 1    # inner block uses 2 spaces — OK (different block)
    return 0
```

### 3. Inconsistent Dedent → ERROR

Dedent must return to an **exact prior indent level**. Dedenting to a level that
was never opened is a compile error.

```
# ERROR: indent 4, dedent to 2 (2 was never an indent level)
fun foo():
    x = 1
  return x        # ERROR: indent level 2 was never established

# OK: indent 4, dedent to 0
fun foo():
    x = 1
return_val = foo()  # OK: back to level 0
```

### 4. Empty Lines Inside Blocks

Empty lines (lines with only whitespace or nothing) are **ignored**.
They do not affect block structure.

```
fun foo():
    x = 1
    
    y = 2         # OK: empty line between statements
    
    return x + y
```

### 5. Comment Lines Inside Blocks

Lines containing only a comment (`# ...`) are **ignored** regardless of their
indentation. Comments are formatting, not code.

```
fun foo():
    x = 1
# this comment has no indent — OK, ignored
    y = 2
        # this comment is over-indented — OK, ignored
    return x + y
```

### 6. Multiple Dedent

Multiple indent levels can close at once. Each level produces a separate
DEDENT token.

```
fun foo(x):
    if x > 0:
        if x > 100:
            return 100
        return x          # dedent from level 12 to level 8
    return 0              # dedent from level 8 to level 4
result = foo(42)          # dedent from level 4 to level 0
```

### 7. Trailing Whitespace

Trailing whitespace at the end of a line is **ignored**.
It does not affect semantics.

```
fun foo():    
    return 1      
```
(Spaces after `:` and after `1` — ignored.)

### 8. Line Continuation

Expressions inside unclosed brackets `()`, `[]`, `{}` can span multiple lines.
Indentation inside brackets is **ignored** (free-form).

```
result = [
    1, 2, 3,
        4, 5, 6,     # indent doesn't matter inside []
]

point = {
    x = 1,
    y = 2
}

value = max(
    a + b,
    c + d
)
```

Outside brackets, each line is a separate statement. No `\` continuation.

### 9. Colon and Blocks

`:` at the end of a line (before newline) starts an indented block.
`:` in the middle of a line is part of a type annotation or single-line form.

```
# Block form: colon before newline
if x > 0:
    return x

# Single-line form: colon in middle
if x > 0: return x

# Type annotation: colon between name and type
fun foo(x: int):
    return x
```

### 10. Tab Width

For error reporting and indent comparison, **1 tab = 1 indent unit**.
Tabs are not converted to spaces. A file using tabs compares tab counts directly.

A file using spaces compares space counts directly.
Mixed comparison never happens (Rule 1 prevents it).

## Summary Table

| Situation | Behavior |
|-----------|----------|
| Tabs and spaces in same file | **ERROR** |
| Any indent size (1,2,3,4,8) | **OK** |
| Dedent to non-existent level | **ERROR** |
| Empty lines in block | **Ignored** |
| Comment-only lines | **Ignored** (any indent) |
| Multiple dedent at once | **OK** (multiple DEDENT tokens) |
| Trailing whitespace | **Ignored** |
| Lines inside `()` `[]` `{}` | **Free-form** (indent ignored) |
| Colon before newline | **Starts block** |
| Colon mid-line | **Type annotation or single-line form** |

## Error Messages

| Error | Message |
|-------|---------|
| Mixed tabs/spaces | `Indentation error: file uses {spaces\|tabs}, but {tabs\|spaces} found at line N` |
| Inconsistent dedent | `Indentation error: unindent does not match any outer indentation level at line N` |
| Missing indent after `:` | `Expected indented block after ':' at line N` |
