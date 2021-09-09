# NFun. The larva of the programming language

Expressions Evaluator for .NET

To install NFun, run the following command in the Package Manager Console:

```
PM> Install-Package NFun 
```

## What is the NFun?

This is an expression evaluator for .net . It supports working with mathematical expressions as well as with collections, strings and structures. NFun is similar to NCalc but with a rich type system

```
// Fluent Api

double d = Funny.Calc<double>("2*10+1") //21  
string s = Funny.Calc<string>("'Hello world'.reverse()") //"dlrow olleH"

string n = Funny.Calc<User,int> (@"if(age > 18) cars.sum(rule it.cost) 
				     	else 0")
User u = Funny.Calc<MyInput,User>(@"
	age = 2020- birthYear
	name = if(inputName.count()==0) 'no name' else inputName
	out = {
		hasName = inputName.count()==0
		age = age
		name = name
		cars = [{
			{name = 'lada', cost = 1200, power = 102},
			{name = 'camaro', cost = 5000, power = 275}
		}]
	}");
				     
```

```
//Low level api

var runtime = Funny.Hardcore.Build("y = 2*x+1");

foreach(var variable in runtime.Variables)
	Console.WriteLine($"Variable {variable.Name}:{variable.Type} {variable.IsOutput?"[OUTPUT]":"[INPUT]"}");

runtime["x"].Value = 42;
runtime.Run();

Console.WriteLine($"Result: {runtime["y"].Value}");

```

## Guide

[API - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/ApiUsageExamplesAndExplanation.cs)

[Syntax - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/SyntaxExamplesAndExplanation.cs)

## Current state

Now nfun is in pre-release stage  
