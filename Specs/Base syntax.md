Nfun basic constuctions


## Core operators

Besides the operators discussed above, there are a few other important operators:
Unlike the others, they provide the basic functionality of the language

### Simple initalization operator '='   
Initializes left side variable with values from right side operand 

```
a = 42      #a is 42
b = a       #b is 42
c = [1,2,3] #c is [1,2,3]
```	

### Type specification operator  ':'  
Specifies type of variable or functional argument or type that returns from function:

```
a: real = 42 # a has the real type

myFun(x:int, y:int):real = ... #x and y has int type. myFun returns real type 

```

### Structure initialization '{}' 
Initlalizes structure with specified fields 

```
a = {
	name = 'foo'
	age = 42
}

b = {} #empty struct
```

### Field access/ Pipe forward operator  '.'  
Allow to access struct fields, or call functions with pipe forward syntax 

```

b = a.name #access field 'name' in struct 'a'

c = a.min() #equialent to 'c = min(a)

```

### Conditional expression operator 'if-if-else' *
**'if-if-else' syntax may be forbidden in dialect syntax.*** 

'if-if-else' conditional expression with such a syntax

if([condition1]) [expr1]
if([condition2]) [expr2]
...
else [expr3]

Contains at least one condition branch
Tries to calculate first condition. if it equals true - then calculates expr1 and use it as the result of if-expression
otherwise tries to to calculate second condition (if it exists)...
if no condition satisfied - calculates expr3 and uses it as the result
```
a = if(1>2) 'wow'
    if(1>1) 'omg'
    if(1>0) 'Yes' #a equals 'Yes'
    else 'no'   

b = if(a=='Yes') 42 #b equals 42, as a equals 'Yes'
	else 0      
```

''

### hi order function operator 'rule'

Defines an anonymous function. In the case of a single argument function, the name 'it' is used instead of the argument

In the case of multiple variables, the names it1, it2, it3, ... are used

```

f = rule it*2

res = f(10) #20
m = [1..3].map(rule it*2) #[1,2,6]

f2 = rule it1+it2

res2 = f(1,2) #3

m3 = [1..3].fold(rule it1+it2) #6

```

There is another, extended form for 'rule'. In this form, you can specify the names and types of arguments, as well as the return type
```

a = rule(x,y) = x/y
b = rule(x:int,y:int):real = x/y

x = b(42,2) #21.0

m = [1..3].map(rule(x):real = x*2) #[1.0,2.0,6.0]

```

### Variable annotation operator '@'

It allows to annotate variable with meta information     

```
@id('foo')
a = 42 # a has annotation 'id' with 'foo' value
```
 