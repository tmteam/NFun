# NFun Basics

NFun is an embedded scripting language for .NET — the host application defines inputs, executes the script, and reads outputs. A script is a sequence of variable initialisations, statements, function definitions, and comments.

This document covers what's common to all NFun scripts. Per-area references:

- `Statements.md` — indented blocks, control flow (`for`/`while`/`if`-as-statement), mutable variables, multi-line function bodies.
- `IndentRules.md` — tabs vs spaces, block-opening rules.
- `UserFunctions.md` — user-defined functions: arguments, defaults, varargs, generic.
- `Annotations.md` — variable annotations `@name`.
- `Operators.md`, `Types.md`, `Collections.md`, `Structs.md`, `Texts.md`, `Optionals.md`, `Rules.md`, `NamedTypes.md`, `Functions.md` — per-feature reference.

### Basic definitions

Output variable (output) is a named value calculated in the script

```py
x = 1 # 'x' is the output
```

Input variable (input) - a named value known before the script execution

```py
x = i + 1 # 'i' is the input
```

Expression is a description of calculating a value of some type. Numbers, text, formulas, and function calls are all particular cases of expressions

```py
(i-12).toText() # expression with type of text
```

Initialization of the variable (output) - assigning the value of the expression to the output
```py
x = (2*(3-1)).toText() #value '4' type 'text' is assigned to the output 'x'
```
Type declaration - explicit indication of the type of the variable
```py
i:int # type declaration for input i
x:text = i.toText() # type declaration for x output combined with its initialization
```
User function - the function described in the script
```py
myFunc(a,b) = 2*a+b #example of a custom function
```
User functions can have default parameter values:
```py
greet(name:text, greeting:text = 'Hello') = '{greeting} {name}'
y = greet('Alice')               # 'Hello Alice'
z = greet('Bob', 'Hi')           # 'Hi Bob'
```

# Nfun script

An NFun script consists of:
- Variable initialisation (and reassignment) — `x = expr`
- Input variable type declarations — `i:int`
- Statements (`if`, `for`, `while`, field/element assignment) — see `Statements.md`
- User function definitions — single-line (`f(x) = expr`) or multi-line (`fun f(x): ... return expr`)
- Comments
- Variable annotations — `@name`

Each top-level item starts on a new line. `;` is equivalent to a line break (except inside a comment).

The script is executed top to bottom. Input type declarations and variable initialisations must precede first use. User function definitions may appear anywhere.

Script example:
```py
# Inputs: a,b,myInput
# Outputs: incrementResult, someSum, isBig
# Custom function: sumOf3(x,y,z)

a:int; b:int # Declaration of the types of input variables a and b

incrementResult = a+1 # Initializing the output incrementResult
someSum:int64 = sumOf3(a,b,1) #Calling the user function

isBig = someSum > myInput #Using an undeclared input

@best_variable_ever  
theTruth = true # output 'theTruth' has annotation 'best_variable_ever' whatever that means

sumOf3(x,y,z) = x+y+z #userFunction sumOf3

```

# Script execution

Regardless of the API with which the script is called, the script lifecycle is divided into several stages. This separation may be hidden from you using the API, but this information may be useful for understanding the work of Nfun

1) **Construction**. Checking the correctness of the script and calculating the types of all expressions in the script
2) **Setting inputs**. The Client code sets the values of the inputs
3) **Execution**. Calculating the values of all outputs
4) Getting variable values by client code 

After construction, the script can be repeatedly executed with new input values. Each execution starts from a clean state — no hidden state is carried between runs.

# Variables

Variable names can contain Unicode letters, digits, emoji, and the `_` symbol.
The first character must be a Unicode letter, emoji, or `_` (not a digit).

For example: `x`, `my_Name`, `id32`, `сумма`, `数量`, `größe`, `α`, `★`, `🎉`, `player_⭐`

The identifier rules are:
- **Start character**: Unicode categories `Lu`, `Ll`, `Lt`, `Lm`, `Lo` (letters), `Nl` (letter numbers), `So` (symbols, including emoji), or `_`
- **Continuation character**: all of the above, plus `Mn` (nonspacing marks), `Mc` (combining marks), `Nd` (decimal digits), `Pc` (connector punctuation)
- **Excluded**: currency symbols (`$`, `€`), math symbols (`+`, `=`), punctuation, control characters, format characters (zero-width joiners)
- **Surrogate pairs**: emoji above U+FFFF (like `🎉`, `🚀`) are supported
- **No normalization**: code points are compared as-is (`é` as U+00E9 and `e` + U+0301 are different identifiers)

Nfun is case insensitive for variables, but you cannot create two variables whose names differ only by case

```py
x = 123
y = X+1 # Error. it is not possible to create an input X, 
        # since the variable x has already been declared
```

Input variables are read-only inside the script (only the host application supplies their values). Output variables can be initialised and reassigned freely — see `Statements.md` §Reassignment and §Compound assignment. Struct and collection mutation is available per type — see `Structs.md` §Mutation and `Collections.md` §Mutation.

### Output variables

Each output variable is initialized by an expression.
The output type can be declared only during initialization

```py
x = 2*3 # the output of 'x' is initialized with the expression '2*3'

y:text = (x+input).toText() # output 'y' of type 'text' with explicit type declartion 
                            # is initialized with an expression
```

The output can be (repeatedly) used in the expressions following after its initialization

```py
a = 42
b = a+1
```

### Anonymous output

If there is only one output variable, it can be initialized simply by writing an expression (without specifying the name and the '=' symbol). 
In this case, an anonymous output named 'out' will be created

```py
2*(3-1) # is equivalent to 'out = 2*(3-1)'
```
```py
x ** 2 # is equivalent to 'out = x ** 2'
```
```py
x:text
x.reverse() # is equivalent to 'out = x.reverse()'
```


### Input variables

Any uninitialized variable in the script is considered an input. 
```py
x = i +1 # 'i' is the input.
```
So the input cannot be initialized
```py
x = i +1 
i = 2 # Error. The input cannot be initialized
```
The input variable may have explicit type declaration
```py
inputName:type #input type declaration 'inputName' with type 'type'
```
The type declaration of the input must be described strictly before its first use

```py
i:int # i has the type int
x = i +1
```

The input variable can be repeatedly used in the calculation of output variables

```py
x = i + 2*i
y = i/2
```

## Comments

Comment - the text following the `#` character and ending with the end of the line (the character `;` does not interrupt the comment). The comment text is completely ignored by the interpreter:

```py
# this is a comment

y = 1+2 # this is also a comment

# the following code will be ignored: z = 5+4
```

# Expressions

An expression is a description of calculating a value of some type.

An expression is anything from
- literal (discrete, ip, numeric, character, textual, or `none`)
- variable
- template text — `'hello {name}'`
- function call
- application of the operator (`[]`, `?[`, `*`, `+`, `()`, `.`, `?.`, `??`, `!`, `and`, `or`, `>>`, `|`, and so on)
- default value - `default`
- anonymous function - `rule`
- array initializer `[...]`
- structure initializer `{...}`
- conditional expression `if-else`

At the time of script execution - the resulting type of each expression is strictly known and specific (with the exception for generic user functions)
This is also true for nested expressions (all expression nodes have a strict type at the time of execution)

When reading an expression, line breaks are ignored

```py
y = # start of expression
12
*3 ;; # ';' symbol also ignored as it is similar to the line break 
+1 -2 # end of expression
z = true and false
```

### Expressions: Discrete literal

Discrete literals have the **bool** type
There are two discrete literals - `true` and `false` defining logical truth and false, respectively

### Expressions: Ip literal

Ip address literals have the **ip** type. It represent IPv4 ip-address

```
a = 127.0.0.1
b = 192.168.0.1
c = 0xFF.0.0xA.0xFA
```

### Expressions: Numeric literal

The numbers in NFun can be written in various ways. Supported
- delimiters `_` ,
- decimal literals `.`,
- negative numbers `-`
- bitness modifiers `0x` and `0b`

```py
123   # integer literal
-123  # integer literal
1_234 # integer literal with separator

0x123     # hexadecimal literal
-0x123    # hexadecimal literal
0x123_456 # hexadecimal literal with separator

0b1010     # binary literal
-0b1010    # binary literal
0b1010_0011 # binary literal with separator

123.456  # real literal
-123.456 # real literal

123_456.7_89 # real literal with separator

2.5e-3     # scientific notation real literal
1E+10      # uppercase E and explicit positive exponent
1_000e2_0  # scientific notation with separator
```

A real literal always has the `real` type.
For other numeric literals, the same symbol may have different types depending on the context:
```py
i = 1      # int32 since this type is 'preferred' for integer literals

j:byte = 1 # byte since the type is explicitly specified

k = 1      # real, since it only participates in decimal division in the next line
m = 1/k

r = 1.5 #real
```

### Implicit multiplication

When a numeric literal is immediately followed (without whitespace) by a variable name or `(`, an implicit `*` operator is inserted:

```py
y = 2x         # equivalent to 2*x
y = 3(x + 2)   # equivalent to 3*(x + 2)
y = 0.5x       # equivalent to 0.5*x
y = 2 x        # error! No space allowed
y = 2sin(x)    # error! Implicit multiplication before a function call is not allowed
y = 2(sin(x))  # equivalent to 2*sin(x)
```

### Expressions: Character literal

Character literals have the **char** type. Syntax: `/` prefix + single character in single quotes

```py
a = /'a'                         # char
b = /'\\n'                       # escape sequences same as in text literals
```

Must contain exactly one character. `/''` and `/'ab'` are errors.

### Conditional expression `if-else`

This section covers the expression form. The indented statement form (`if cond: ... elif: ... else: ...`) lives in `Statements.md` §if / elif / else.

In Nfun, `if-else` (expression form) always returns a value

```py
result = if(condition1) expression1 else elseExpression
```
here:
- **condition1** is a bool type expression
- **expression1** is the resulting expression if condition is true
- **elseExpression** is the resulting expression if all conditions are false

Since **elseExpression** can also be an `if-else` expression, many branches of conditions can be implemented:

```py
result = 
	if(c1) e1 # if c1 is true, the result will be e1
	else if(c2) e2 # otherwise, if c2 is true, the result will be e2
	else if(c3) e3 # otherwise, if c3 is true, the result will be e3
	# there can be as many branches as you like
	else elseExpression # otherwise (if all conditions are false), 
	                    # then the result is elseExpression
```

For the case of multiple branches, - Nfun offers a compact if-if-else syntax.
To do this, you can simply replace `else if` with `if`:

```py
result = 
    if(c1) e1 # if c1 is true, the result will be e1
    if(c2) e2 # otherwise, if c2 is true, the result will be e2
    if(c3) e3 # otherwise, if c3 is true, the result will be e3
    # there can be as many branches as you like
    else elseExpression # otherwise (if all conditions are false), 
                        # then the result will be elseExpression
```

Examples:
```py
a =
    if(x>0) 'positive'
    if(x<0) 'negative'
    else 'zero'

b =
    if(a == 'positive' and flag)
        if(day == 1) 'mon'
        if(day == 2) 'tue'
        if(day == 3) 'wed'
        if(day == 4) 'thu'
        if(day == 5) 'fri'
        if(day == 6) 'sat'
        if(day == 7) 'sun'
        else '???'
    else 'some day'
```

If one branch is `none` and the other is a value, the result type becomes optional

```py
c = if(found) 42 else none   # int?
d = if(flag) none else 'ok'  # text?
```

### Error expression `oops()`

`oops()` is a built-in function that throws a runtime error. It acts as a bottom type — fits any expression context.

```py
oops()                    # throw with default message "oops"
oops('not found')         # throw with custom message
oops('fail', errorData)   # throw with message and data payload
```

Common patterns:

```py
value = x ?? oops('x is missing')          # unwrap-or-throw
y = if(x > 0) x else oops('must be > 0')   # assertion
```

`oops()` in dead branches is not evaluated (lazy), same as `if-else` branches.

### Error handling expression `try/catch`

`try/catch` is an expression. It evaluates the try branch; if a runtime error occurs, evaluates the catch branch instead.

```py
y = try riskyExpr catch fallbackExpr
```

Type of result = LCA(try branch, catch branch). Catch branch is lazy (not evaluated on success).

```py
y = try oops('fail') catch 0          # 0
y = try 42 catch oops('unreachable')  # 42 — catch not evaluated
```

With error object access:

```py
y = try oops('bad') catch(e) e.message    # 'bad'
```

`e` is a struct `{message: text, data: any}`, scoped to the catch expression only.

Nested try/catch:

```py
y = try (try oops() catch oops('inner')) catch 0    # 0
```

### Expressions: Default value `default`

Each type has a default value

This value is equal to:
- zero for any numeric type
- empty array for arrays
- empty collection for `list<T>` / `array<T>` / `fixedArray<T>` / `set<T>` / `map<K,V>`
- " (empty text) for `text`
- `none` for any optional type `T?`
- `none` for `any` (`any ≡ any?` — any-typed slots can hold `none`)
- a structure with a 'default' value for each field in the structure type
- a function that returns the `default` value for an anonymous function
- 0.0.0.0 for `ip`

use the keyword `default` to get this value

```py
y:real = default #0.0
a = if(1>2) true else default #false

b = if(input>0) [1,2,3] else default

notDefinedFunction(a,b) = default
```

### Expressions: Function call

A function call is an expression that returns the result of executing a function with the specified arguments
There are two forms of function call syntax - classic and reverse

Classic:

```py
#For a function without arguments
functionName()

#For a function with N -arguments
functionName(arg1, arg2...argN)
# or
functionName(arg1, arg2...argN,) # with trailing comma
```

Reverse:

```py
#For a function with 1 argument, the argument
arg1.functionName()

#For a function with N arguments
arg1.functionName(arg2, arg3,...argN)
# or
arg1.functionName(arg2, arg3,...argN,) # with trailing comma
```
Where functionName is the function name, and arg1,arg2, argN are the first second and n-th arguments of the call, respectively

```py
i = reverse("hello") #'olleh'
j = max(1,max(2,3)) #3
k = 'hello'.concat(' world').reverse() #'dlrow olleh'
m = i.unite(k) #'olehdrw ' (set union — unique elements from both arrays)
```

### Expressions not described in this document

The remaining expressions require a more detailed description and are described in the following boring documents:

- Operators
- Arrays
- Collections (`list`, `set`, `map`, ...)
- Texts
- Structures
- Rules (Anonymous functions)
- Optionals
- Statements (control flow, mutation, indented blocks)

# User functions

User functions are described in `UserFunctions.md` — single-line `f(x) = expr` form, multi-line `fun f(x): … return expr` form, named arguments, default values, varargs, keyword-only arguments, and generic functions.

# Variable annotations

Annotations attach metadata to a variable, readable from the host application. Syntax `@name` or `@name(arg)` on the line immediately before the variable. See `Annotations.md`.
