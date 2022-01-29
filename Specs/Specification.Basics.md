# Nfun Basics

## Basic definitions

Output variable (output) is a named value calculated in the script

```
x = 1 # x is the output
```

Input variable (input) - a named value known before the script execution

```
x = i + 1 # i is the input
```

Expression is a description of calculating a value of some type. Numbers, text, formulas, and function calls are all particular cases of expressions
```
2*(i-12).toText() #expression, type text
```

Initialization of the variable (output) - assigning the value of the expression to the output
```
x = 2*(3-1).toText() #value '4' type 'text' is assigned to the output 'x'
```
Type declaration - explicit indication of the type of the variable
```
i:int #type declaration for input i
x:text = i.toText() #type declaration for x output
```

Custom function - the function described in the script
```
myFunc(a,b) = 2*a+b #example of a custom function
```

## Nfun script

The Nfun script can only consist of the following elements:
- Initialization (assignment) of output variables
- Declarations of input variable types
- Descriptions of user functions
- Comments

Each of these elements begins with a new line, with the symbol ';' is the full equivalent of a line break.

The exception is the 'Comment', which can not only start with a new line, but can also be placed at the end of any of the script lines. Symbol ';' does not end the comment.

The script is executed from top to bottom. This means that:
- The declaration of the input variable (if it exists) must be placed before any use of it
- Initialization of the output variable must be placed before any use of it

A custom function can be declared both before or after its use

Script example :
```

# Inputs: a,b,myInput
# Outputs: incrementResult, someSum, isBig
# Custom function: sumOf3(x,y,z)

a:int; b:int # Declaration of the types of input variables a and b

incrementResult = a+1 # Initializing the output incrementResult
someSum:int64 = sumOf3(a,b,1) #Calling the user function
isBig = someSum > myInput #Using an undeclared input

sumOf3(x,y,z) = x+y+z

```

## Script execution

Script Lifecycle:
1) Script construction: checking the correctness of the script and calculating the types of all expressions in the script
2) Setting inputs: The calling code sets the values of the inputs
3) Script execution
4) Getting the output values

After construction, the script can be repeatedly run with the setting of new input values.
At the same time, it is guaranteed that there is no hidden, changing state between successive launches, since all variables are unchanged.

## Variables

Variable names consist of uppercase and lowercase latin letters, numbers, and the _ symbol. The first character must be a letter

For example: 'x', 'my_Name', 'id32'

Nfun is case sensitive for variables, but you cannot create two variables whose names differ only by case

Example
```
x = 123
y = X+1 #error. it is not possible to create an input X, since the variable x has already been declared
```

All variables in NFun are 'immutable', which means that output variables can only be initialized once,
and input variables cannot be initialized at all.

### Output variables

Each output variable is initialized by an expression.
The output type can be declared only when initialized

Example
```
x = 2*3 # the output of 'x' is initialized with the expression '2*3'
y:text = (x+input).toText() # output 'x' with type real is initialized with a complex expression
```

If there is only one output variable, it can be initialized simply by writing an expression (without specifying the name and the '=' symbol). In this case, an anonymous exit named 'out' will be created

```

2*(3-1) # is equivalent to out = 2*(3-1)
```

The output can be (repeatedly) used in the expressions following after its initialization

```
a = 42
b = a+1
```

### Input variables

Any uninitialized variable in the script is considered an input. The input cannot be initialized

```
x = i +1 # i is the input.
i = 2 # error. The input cannot be initialized
```

The input variable type declaration is declared as
```
inputName:type #input type declaration 'inputName' with type 'type'
```
The type declaration of the input must be described strictly before its first use

```
i:int # i has the type int
x = i +1
```

The input variable can be repeatedly used in the calculation of output variables

```
x = i + 2*i
y = i/2
```

## Comments

Comment - the text following the # character and ending with the end of the line (the character ; does not interrupt the comment). The comment text is completely ignored by the interpreter:

```
# this is a comment

y = 1+2 # this is also a comment

# the following code will be ignored: z = 5+4
```

## Expressions

An expression is a description of calculating a value of some type.

An expression is anything from
- literal (discrete, numeric, or textual)
- variable
- template text
- function call
- application of the operator ([], *, +, (), ., and, or, >>, |, and so on)
- default value - 'default'
- anonymous function - 'rule'
- array initializer [...]
- structure initializer {...}
- conditional expression 'if-else'

At the time of script execution - the resulting type of each expression is strictly known and specific (with the exception for generalized user functions)
This is also true for nested expressions (all expression nodes have a strict type at the time of execution)

When reading an expression, line breaks are ignored

```
y = # start of expression
12
*3
+1 -2 # end of expression
z = true and false
```

### Discrete literal

Discrete literals have the bool type
There are two discrete literals - 'true' and 'false' defining logical truth and false, respectively

### Numeric literal

The numbers in NFun can be written in various ways. Supported
- delimiters '_' ,
- decimal literals '.',
- negative numbers '-'
- bitness modifiers '0x' and '0b'

```
123 # integer literal
-123 # integer literal

1_234 # integer literal with separator
0x123 # hexadecimal literal
-0x123 # hexadecimal literal
0x123_456 # hexadecimal literal with separator
0b123 # binary literal
-0b123 # binary literal
0b123_456 # binary literal with separator

123.456 # real literal
-123.456 # real literal

123_456.7_89 # real literal with separator
```

A real literal always has the real type.
For other numeric literals, the same symbol may have different types depending on the context:

```
i = 1 # int32 since this type is 'preferred' for integer literals

j:byte = 1 #byte since the type is explicitly specified

k = 1 #real, since it only participates in decimal division in the next line
m = 1/k

r = 1.5 #real
```

### Conditional expression if-else

In Nfun, 'if-else' is an expression, i.e. it always returns a value

```
result = if(condition1) expression1 else elseExpression
```
here 'condition1' is a bool type expression,
'expression1' is the resulting expression if condition is true,
'elseExpression' is the resulting expression if all conditions are false

Since 'elseExpression' can also be an 'if-else' expression, many branches of conditions can be implemented:

```
result = 
	if(condition1) expression1 # if condition1 is true, the result will be expression1
	else if(condition2) expression2 # otherwise, if condition2 is true, the result will be expression2
	else if(condition3) expression3 # otherwise, if condition3 is true, the result will be expression3
	#there can be as many branches as you like
	else elseExpression # otherwise (if all condition#i is false), then the result will be elseExpression
```

For the case of multiple branches, - Nfun offers a compact if-if-else syntax.
To do this, you can simply replace 'else if' with 'if':

```
result = 
if(condition1) expression1 # if condition1 is true, the result will be expression1
if(condition2) expression2 # otherwise, if condition2 is true, the result will be expression2
if(condition3) expression3 # otherwise, if condition3 is true, the result will be expression3
#there can be as many branches as you like
else elseExpression # otherwise (if all condition#i is false), then the result will be elseExpression
```
### Default value 'default'

Each data type has a default value

This value is equal to:
- zero for any numeric type
- " for text
- empty array for arrays
- new object() for any
- a structure with a 'default' value for each field in the structure type
- a function that returns the 'default' value for an anonymous function

use the keyword 'default' to get this value

Example:

```
y:real = default #0.0
a = if(1>2) true else default #false

b = if(input>0) [1,2,3] else default

notDefinedFunction(a,b) = default
```

### Lambda-expression 'rule'

The NFun supports anonymous functions - 'rules'.

The expression following the keyword 'rule' is the body of such a function. If an anonymous function uses one input variable, then its name inside the expression is "it"

If there are two or more, then their names are 'it1', 'it2'... 'itN'

```
f = rule it*2 # expression for an anonymous function multiplying the input argument by 2

f(2) # 4

a = [1,2,3,4].filter(rule>2) # using a rule to filter an array. Only elements strictly larger than 2 will be selected
x = [-1,-2,0,1,2,3].add(rule it1+it2) #sum of all array elements
```

There is an extended form of rule-expression, with the ability to specify the types of arguments and or the type of return value 

```
rule(arg1:type1, arg2:type2... argN:type):rtype = expression
```
here
arg1,arg2..argN - the names of the 1st 2nd ... N-th arguments
(optional) type 1,type 2...type 2 - types of the 1st 2nd ... N-th arguments
(optional) rtype - function return type
expression - function expression (body) with local variables arg1,arg2..argN

Example:
```
a = [1,2,3].all(rule(i) = i >0) # check that all numbers in the array are positive
b = [1,2,3].all(rule(i:int):bool = i >0) # check that all numbers in the array are positive (all types explicitly specified)
x = [-1,-2,0,1,2,3]
.filter(rule(i)= i>0)
.fold(rule(a:int,b)= a+b) #the sum of all positive integers of the array
```

### Function calls

A function call is an expression that returns the result of executing a function with the specified arguments
There are two forms of function call syntax - classic and reverse


Classic:

```

#For a function without arguments
functionName()

#For a function with arguments H
functionName(arg1, arg2, arg3, ...argn)
```

Reverse:

```

#For a function with 1 argument, the argument
arg1.functionName()

#For a function with N arguments
arg1.functionName(arg2, arg3,...argN)
```
Where functionName is the function name, and arg1,arg2, argN are the first second and n-th arguments of the call, respectively


Examples:

```
i = reverse("hello") #'olleh'
j = max(1,max(2,3)) #3
k = 'hello'.concat ('world').inverse() #'olle dlrow'
m = i.union(k) #'olleholle'
```

### Expressions not included in this document

The remaining expressions require a more detailed description and are described in the following documents:

- Operators
- Arrays
- Lines
- Structures

## Custom functions

In the body of the script, you can describe the function available for calling in the script

```
functionName(arg1:type1,arg2:type2...argN:type):rtype = expression
```
here:
functionName - the name of the function
arg1,arg2..argN - 1st,2nd ... n-th names of arguments
(optional) type1,type2...typeN - types of the 1st 2nd ... N-th argument
(optional) rtype - function return type
expression - function expression (body) with local variables arg1,arg2..argN

Only function arguments can be used as variables in the function body. The inputs and outputs of the script are not visible for the function 


Examples:

```
Sum of 3(a,b,c) = a+b+c

a = sum(1,2,3) #6:int

b = maximum real(1,2,3) #3:real

maxOfReal(a,b,c):real = max(max(a,b),c)

firstPositiveNumber(a:int[]) = if(a. any(rule>0)) a.filter(rule>0)[0] the rest is by default

c = [-1,3,0,-4,5].The first positive number() #3

```

### Specific and generalized user functions

In the simplest case, all types of function arguments are strictly defined. This is possible when types are uniquely defined from an expression and/or when types are specifically specified

Such functions are called "specific"

Example

```
divideBy2(a) = a/2 # the function takes a:real and returns real

multiplyReal(a: real, b) = a*b # the function takes a: real, b: real and returns real

maxOf3(a,b,c):int =max(a,b).max(c) # the function takes a:int, b:int, c:int and returns int
```

If the function is valid for various types (including with some restrictions on them), such a function is called "generic".

Consider an example of a function of the sum of 3 terms:

```
threeSum(a,b,c) = a+b+c
```

The + operator is applicable to int32, int64, uint32, uint64, real types.
This means that both the argument types and the return value type are equal to each other and belong to the same range

If you call such a function with different types , then various operations will be performed:

For the real case, it will be real-addition, for int32, it will be int32-addition, etc.:


```

a:int = threeSum(1,2,3)

barg1: real; barg2:real; barg3:real
b = threeSum(barg1, barg2, barg3)

```

Other examples of generic user-defined functions:

```
firstItem(a) = if(a.size()>0) a[0] else default

t = 'hello'.firstItem() #\'h'
m = [1,2,3].firstItem() # 1

increment(a) = a+1

i = 0.increment() #1:int
j = 1.5.increment() #1.5:real
k:uint = 12.increment() #13:uint
```
