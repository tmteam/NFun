# NFun. Expressions Evaluator for .NET

To install NFun, run the following command in the Package Manager Console:

```
PM> Install-Package NFun 
```

## What is the NFun?

This is an expression evaluator for .net. It supports working with mathematical expressions as well as with collections, strings and structures. NFun is quite similar to NCalc but with a rich type system.
See the 'How to' section for details

Nfun can perform simple evaluations
```
double d = Funny.Calc<double>("2*10+1") //21  
string s = Funny.Calc<string>("'Hello world'.reverse()") //"dlrow olleH"
bool b   = Funny.Calc<bool>("true and not true") //false
```
as well as complex, with multiple composite inputs and outputs
```
# 'age' and 'name' are properties from input type 'User' 
string userAlias = Funny.Calc<User,string> (
                       "if(age < 18) name else 'Mr. {name}', 
                        inputUser)  

# hi order function used here
int carsTotalPrice = Funny.Calc<User,int> (
                        "if(age > 18) cars.sum(rule it.cost) else 0", 
                         inputUser)
   
   
# Evaluate many values and put it into result 'OutputUser' object
# 'age', and 'cars' are properties of 'OutputUser' 
# 'birthYear' is property of 'input' object                           

OutputUser u = Funny.CalcMany<MyInput,OutputUser>(@"   
   age = 2020 - birthYear
   cars = [
   	{ model = 'lada',   cost = 1200, power = 102 },
   	{ model = 'camaro', cost = 5000, power = 275 }
   ]
", input);
```
Low-level hardcore API is also supported
```
  var runtime = Funny.Hardcore.Build("y = 2*x+1");

  //Write input data
  runtime["x"].Value = 42;
  //Run script
  runtime.Run();
  //Collect values
  Console.WriteLine($"Result: " + runtime["y"].Value);
  //Analyze script
  foreach(var variable in runtime.Variables)
  {
    Console.WriteLine("Script contains these variables:"
    Console.WriteLine(
  	    "{variable.Name}:{variable.Type} {variable.IsOutput?"[OUTPUT]":"[INPUT]"}");
  }
```

## Key features

- Arithmetic, Bitwise, Discreet operators
```	
  # Arithmetic operators: + - * / % // ** 
  y1 = 2*(x//2 + 1) / (x % 3 -1)**0.5
	
  # Bitwise:     ~ | & ^ << >> 
  y2 = (x | y & 0xF0FF << 2) ^ 0x1234
	
  # Discreet:    and or not > >= < <= == !=
  y3 = x and false or not (y>0)
```

- If-expression
```
  simple  = if (x>0) x else if (x==0) 0 else -1
  complex = if (age>18)
                if (weight>100) 1
                if (weight>50)  2
                else 3
            if (age>16) 0
            else       -1     
```
- User functions and generic arithmetics
```
  sum3(a,b,c) = a+b+c
  
  r:real = sum3(1,2,3)
  i:int  = sum3(1,2,3)
  v = sum3(1,2,3
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
  m = [1,2,3].map(rule it/2) # 'm' has type of 'real[]'
```
- Built-in functions
- Syntax and semantic customization
- And lot more Funny stuff...

## How to 

[API - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/ApiUsageExamplesAndExplanation.cs)

[Syntax - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/SyntaxExamplesAndExplanation.cs)

## Current state
Production-ready with 5000 green tests and a lot of plans.  