# NFun. The larva of the programming language

Expressions Evaluator for .NET

To install NFun, run the following command in the Package Manager Console:

```
PM> Install-Package NFun 
```

## What is the NFun?

This is an expression evaluator for .net . It supports working with mathematical expressions as well as with collections, strings and structures. NFun is similar to NCalc but with a rich type system

```
# Creates an expression, that accepts single input 'x' and calculates single output 'y' as result
y1 = 2x+1 

# Strings
y2 = 'a+b = {a+b}, c = {c}'.toUpper() 

# Arrays as input
y3 = a[0] + a[1]

# Linq
y4 = a.reverse().filter (rule it>2)

# Working with structs
y5 = if(a.hasName and a.age <> 42) a.name 
	else a.alias[0]  
```

## Guide

[API - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/ApiUsageExamplesAndExplanation.cs)

[Syntax - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/SyntaxExamplesAndExplanation.cs)

## Current state

Now nfun is in pre-release stage  

## Usage example

```
// Fluent api:

TODO

// Hardcore-script api:

var runtime = Funny.Hardcore.Build("y = 2*x+1");

foreach(var variable in runtime.Variables)
	Console.WriteLine($"Variable {variable.Name}:{variable.Type} {variable.IsOutput?"[OUTPUT]":"[INPUT]"}");

runtime["x"].Value = 42;
runtime.Run();

Console.WriteLine($"Result: {runtime["y"].Value}");
```
