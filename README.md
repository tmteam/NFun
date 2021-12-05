# NFun. Expressions Evaluator for .NET

To install NFun, run the following command in the Package Manager Console:

```
PM> Install-Package NFun 
```

## What is the NFun?

This is an expression evaluator for .net . It supports working with mathematical expressions as well as with collections, strings and structures. NFun is quite similar to NCalc but with a rich type system.
See the 'How to' section for details

Nfun can perform as simple calculations
```
double d = Funny.Calc<double>("2*10+1") //21  
string s = Funny.Calc<string>("'Hello world'.reverse()") //"dlrow olleH"

string n  = Funny.Calc<User,int> ("if(age < 18) name else 'Mr. {name}')  
string n2 = Funny.Calc<User,int> ("if(age > 18) cars.sum(rule it.cost) else 0")
```

and quite complex, with multiple inputs and outputs as well
```
User u = Funny.Calc<MyInput,User>(@"
	out = {
		hasName = inputName.count()==0
		age = 2020- birthYear
		name = if(inputName.count()==0) 'no name' else inputName
		cars = [
			{name = 'lada', cost = 1200, power = 102},
			{name = 'camaro', cost = 5000, power = 275}
		]
	}");
				     
```
A low-level API is also supported
```
var runtime = Funny.Hardcore.Build("y = 2*x+1");

foreach(var variable in runtime.Variables)
	Console.WriteLine($"Variable {variable.Name}:{variable.Type} {variable.IsOutput?"[OUTPUT]":"[INPUT]"}");

runtime["x"].Value = 42;
runtime.Run();

Console.WriteLine($"Result: {runtime["y"].Value}");

```

## Key features

- Arithmetic, Bitwise, Boolean operators
```	
	# regular arithmetics operators: + - * / % // ** 
	y1 = 2*(x//2 + 1) / (x % 3 -1)**0.5
	
	# Bitwise operators: ~ | & ^ << >> 
	y2 = (x | y & 0xF0FF << 2) ^ 0x1234
	
	# Boolean:    and or not > >= < <= == !=
	y3 = x and false or not (y>0)
```

- If-expression
```
	out = if (x>0) x else if (x==0) 0 else -1
```

- Array, string, numbers, structs and hi-order fun support
```
  out = {
    name = 'etaK'.reverse()
    values = [0xFF0F, 2_000_000, 0b100101]
    items = [1,2,3].map(rule 'item {it}')
  }
```

- Strict type system and type-inference algorithm 

```
    y = 2*x
    z:real = y*x
    t = 'hello'
    m = [1,2,3].map(rule it/2) # m has type of 'array of real'
```

## How to 

[API - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/ApiUsageExamplesAndExplanation.cs)

[Syntax - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/SyntaxExamplesAndExplanation.cs)

## Current state

Nfun is stable and ready for use in the production code. 
5000 green tests in this repository are guarantees of this