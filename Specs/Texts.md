# NFun Text

Text is an array of characters. Semantically equivalent to the String type in most programming languages.

Text is immutable - this means that after creating the text, it cannot be changed - only create a new one based on the previous one

The text is an alias for the "char[]" type and inherits all arrays properties and arrays operators

## Text Literals

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

## Text Templates

The text may contain one or more expression templates enclosed in curly brackets. When resolving such text into the result, templates are replaced with text representations of the results of expressions

```
y = 'one plus two equals {1+2} !' # 'one plus two equals 3'
z = 'your name is {name} and surname is {surname}'
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
b = 'hello'[0] # \'h'

c = 'hello'[0:2] #'hel'
d = 'hello'[2:3] #'ll'

e:text = default # '' or []
f = '' == [] # true
g = 'test' == 'test' #true
```

The only difference between a regular array (not characters) is that the text (or array of characters) is comparable

```py
a = 'Antony' < 'John' #true

c = [1,2,3] < [4,5,6] # error. regular arrays cannot be compared
```

