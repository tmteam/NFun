# NFun Rules. Hi order, anonymous functions

## Super-anonymous syntax

The expression following the keyword 'rule' is the body of anonymous function with no more than three arguments

If an anonymous function uses one input variable (argument), then the  name of this variable inside the expression is `it`

If there are two or three arguments, then their names are `it1`, `it2`, `it3`, respectively

```py
f = rule it*2 # expression for an anonymous function multiplying the input argument by 2

f(2) # 4

a = [1,2,3,4].filter(rule it>2) # using a rule to filter an array 
                             # only elements strictly larger than 2 will be selected
x = [-1,-2,0,1,2,3].add(rule it1+it2) #sum of all array elements

c = (rule 42)() #42

```
The number of arguments of the rule is calculated from the place of use and the body of the anonymous function
```py
a = rule 42   # zero arguments
b = rule it*2 # one argument
c = rule it1+ it2 # two arguments
d = rule it3      # three arguments

e = [1,2].map(rule it*2) # one argument 
f = [1,2].map(rule 42)   # one argument 

g = [1,2].fold(rule 42)      # two arguments 
h = [1,2].fold(rule it1+it2) # two arguments
i = [1,2].fold(rule it)      # ERRROR
```

## Annotated syntax
There is an extended form of rule-expression, with the ability to specify the types of arguments and or the type of return value

```py
rule(arg1:type1, arg2:type2... argN:type):rtype = expression
```
here
* **arg1,arg2..argN** - the names of the 1st 2nd ... N-th arguments
* *(optional)* **type1,type2...type2** - types of the 1st 2nd ... N-th arguments
* *(optional)* **rtype** - function return type
* **expression** - function expression (body) with local variables arg1,arg2..argN

the trailing comma after last argument is supported

```py
a = rule(a) = a+1
b = rule(a,b) = a+b
c = rule() = 42 

d = [1,2,3].all(rule(i) = i >0)          # check that all numbers in the array are positive
e = [1,2,3].all(rule(i:int):bool = i >0) # check that all numbers in the array are positive 
                                         # (all types explicitly specified)
f = [0..3].fold(rule(a,b,)= a+b) #the sum of all integers of the array       

g = [-1,-2,0,1,2,3]
       .filter(rule(i)= i>0)
       .fold(rule(a:int,b)= a+b) #the sum of all positive integers of the array
```
## Capturing variables

All variables from the external scope are visible inside the anonymous function. The external variable used inside the **rule** expression is called **captured**
```py
a = 2
b = [1..100].filter(rule it % a == 0) # a is captured variable inside rule expression
```

## Equality

Two rules are equal to each other if and only if comparable variables are initialized with the same value.

```py
a = rule it>2
b = a

res1 = a==b #true

c = rule it>2

res2 = a==c #false

```
## Default value

Default value for rule of some type - is a function that returns the default return value regardless of the values of the arguments passed

```py

a = default
b:int = a(1,2,3) # 0
```

## Conversion of rules

For information about converting rules, see the boring section **Types**
