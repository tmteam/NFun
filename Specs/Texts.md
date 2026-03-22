# NFun Text

Text is an array of characters. Semantically equivalent to the String type in most programming languages.

Text is immutable - this means that after creating the text, it cannot be changed - only create a new one based on the previous one

The text is an alias for the "char[]" type and inherits all arrays properties and arrays operators

## Single Quote Text Literal

A text literal is zero to several characters inside single(") or double ("") quotes written in one line

```
'hello world'
```

Text literals always have the type 'text'

The following escaped characters may be used inside quotation marks :


| Escaped | Produced                                                                 |
|---------|--------------------------------------------------------------------------|
| \\\\    | \                                                                        |
| \n      | New line                                                                 |
| \r      | Carriage return. Note that \r is not equivalent to the newline character |
| \"      | "                                                                        |
| \'      | '                                                                        |
| \t      | Tabulation                                                               |
| \\{     | {                                                                        |
| \\}     | }                                                                        |
| \\$     | $                                                                        |

If a text literal is written using single quotes, then double quotes inside it can not be escaped. And vice versa:

```
y = 'Kate said: "hi"!'
z = "Kate said: 'hi'!"
```

Special quotes ’ ” are also supported

```
y = ”Kate said: "hi"!”
z = ’Kate said: 'hi'!’
```

## Triple Quote Text Literal

Triple quote text literal use `'''` or `"""` delimiters and support multiline content with automatic indentation trimming (dedent).

```py
y = '''
hello
world
'''
# y == 'hello\nworld'
```

1. A newline is required after the opening `'''` or `"""` — content cannot start on the same line
2. The closing `'''` or `""""` must be on its own line (with optional leading whitespace)
3. The indentation of the closing `'''` or `"""` defines the **baseline** — this whitespace prefix is stripped from every content line
4. A content line with less indentation than the baseline is a compile error (blank lines are exempted)
5. Using different indentation characters across lines is a compile error — if the baseline uses spaces, all content lines must use spaces (and vice versa for tabs)

The indentation of the closing delimiter determines how much whitespace is stripped:

Extra indentation beyond baseline is preserved:

```py
y = '''
    hello
      world
    '''
# y == 'hello\n  world'
```

The same escape sequences as in regular strings are supported (`\n`, `\t`, `\\`, `\'`, `\"`, `\{`, `\}`).

Single quotes can appear inside `"""` and double quotes can appear inside `'''` without escaping:

```py
a = """
it's here
"""

b = '''
say "hi"
'''
```

`\r\n` line endings in content are normalized to `\n`.

### Error cases

```py
y = '''hello'''         # error: newline required after opening
y = '''
hello                   # error: unclosed triple-quoted string
y = '''
  hello
 world
  '''                   # error: 'world' has less indent than baseline
```

## Text Templates

The text may contain one or more expression templates enclosed in curly brackets. 
When resolving such text into the result, templates are replaced with text representations of the results of expressions

It works for single and triple quote text literals:
```
x = 'one plus two equals {1+2} !' # 'one plus two equals 3'
y = 'your name is {name} and surname is {surname}'
z = """
    hello {name}
    how is it going?
    """
```

N `$` signs before the opening quote shift the interpolation trigger to N `$` + `{`. Bare `{` becomes literal.

| Syntax     | Interpolation | `{`       | `$`     |
|------------|---------------|-----------|---------|
| `'...'`    | `{expr}`      | `\{`      | literal |
| `$'...'`   | `${expr}`     | literal   | `\$`    |
| `$$'...'`  | `$${expr}`    | literal   | literal |

Works for all quote styles: `'`, `"`, `'''`, `"""`.

```py
json = $'{"name": "${name}"}'

config = $'''
    {
        "host": "${host}",
        "port": ${port}
    }
    '''

report = $$'''
    Price: $100
    Customer: $${customer}
    '''
```

## Text Type 

The 'text' type is used to describe the text

```
y:text = 'hello world'
```
'text' is not a separate type. This is an alias for the 'array of chars' type.
This means that any text is an array of characters, and any array of characters is text. So any operations with arrays are applicable to the text
Thus, all the equality rules, default values, and type conversions are identical to the usual rules for arrays.
```
a = 'test'.reverse() #tset
b = 'hello'[0] # /'h'

c = 'hello'[0:2] #'hel'
d = 'hello'[2:3] #'ll'

e:text = default # '' or []
f = '' == [] # true
g = 'test' == 'test' #true
```

Since text is `char[]`, an array of char literals produces text:

```py
word = [/'h', /'e', /'l', /'l', /'o']  # text "hello"
word == 'hello'                         # true
```

The only difference between a regular array (not characters) is that the text (or array of characters) is comparable

```py
a = 'Antony' < 'John' #true

c = [1,2,3] < [4,5,6] # error. regular arrays cannot be compared
```

