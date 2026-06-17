# NFun Variable Annotations

Annotations attach metadata to a variable. The interpreter ignores them; the host application reads them from the runtime.

## Syntax

`@name` on the line immediately before the variable initialisation or input-type declaration.

```py
@supermega
a = 42                # output 'a' carries annotation 'supermega'
```

```py
@required
i:int                 # input 'i' carries annotation 'required'
```

Annotation names follow the same rules as variable names — Unicode letters, digits, emoji, `_`. First character must be a letter, emoji, or `_`.

Empty lines between the annotation and the variable are not allowed; comments are fine.

## Multiple annotations

Each annotation starts on its own line.

```py
@required
@veryFun
i: int                # input 'i' has annotations 'required' and 'veryFun'
```

## Annotation arguments

Each annotation can carry **one** literal argument — numeric, binary, or text — in parentheses.

```py
@id('foo')
@private
a = 42                # 'id' carries value 'foo'; 'private' has no value
```

```py
@maxLen(255)
text:text
```
