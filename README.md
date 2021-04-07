# NFun
Not yet a program language

Expressions Evaluator for .NET

To install NFun, run the following command in the Package Manager Console:

```
PM> Install-Package NFun 
```

## What is the NFun?

It is expressions Evaluator for .NET. This is similar to the NCalc package, but it allows you to work not only with mathematical expressions, but also with complex types - arrays, structures, strings, etc.


```
# Creates an expression, that accepts single input 'x' and calculates single output 'y' as result
y = 2*x+1 

# Strings
y = 'a+b = {a+b}, c = {c}'.toUpper() 

# Arrays as input
y = a[0] + a[1]

# Linq
y = a.reverse().filter {it>2}

# Working with structs
y = if(a.hasName) a.name 
	else a.alias[0]  
```

## NFun is not production ready yet

Now nfun is in early betta stage. This means that the API will change in the future, and not all the functionality is implemented. 

## Usage example

```
// NFun Api will change soon

// The Api shown in the example is only used for development purposes

var runtime = FunBuilder.Build("y = 2*x+1");

Assert.AreEqual(1, runtime.Inputs.Length);
Assert.AreEqual(1, runtime.Outputs.Length);

var result = runtime.Calculate(VarVal.New("x", 42.0));

Assert.AreEqual(VarVal.New("x", 85.0),result);
```
