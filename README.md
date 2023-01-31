# NFun. Expressions Evaluator for .NET

To install NFun, run the following command in the Package Manager Console:

```js
PM> Install-Package NFun 
```

## What is the NFun?

This is an expression evaluator or a mini-script language for .net. It supports working with mathematical expressions as well as with collections, strings, hi-order functions and structures. NFun is quite similar to NCalc but with a rich type system and linq support.
See the ['How to / specifications'](https://github.com/tmteam/NFun#how-to) section for details

Nfun can perform simple evaluations
```cs
  double d = Funny.Calc<double>(" 2 * 10 + 1 ") // 21  
  bool b   = Funny.Calc<bool>("false and (2 > 1)") // false

  // 'age' and 'name' are properties from input 'User' model 
  string userAlias = Funny.Calc<User,string> (
                       "if(age < 18) name else 'Mr. {name}' ", 
                        inputUser)  
```
as well as complex, with multiple composite inputs and outputs
```cs   
  // Evaluate many values and set them into 'Person' object's properties 
  // inputs and outputs 'age', 'cars' and 'birthYear' are properties of 'Person' object 
  var personModel = new Person(birthYear: 2000);
  
  Funny.CalcContext(@"   
     age = 2022 - birthYear 
     cars = [
   	    { name = 'lada',   cost = 1200, power = 102 },
   	    { name = 'camaro', cost = 5000, power = 275 }
     ]
     ", personModel);
  
  Assert.Equal(22,   personModel.Age);
  Assert.Equal(2,    personModel.Cars.Count());
  Assert.Equal(1200, personModel.Cars[0].Cost);
  
```
Low-level hardcore API is also supported
```cs
  var runtime = Funny.Hardcore.Build("y = 2x+1");

  runtime["x"].Value = 42; //Write input data
  runtime.Run(); //Run script
  var result = runtime["y"].Value //collect results
  
  Console.WriteLine("Script contains these variables:"
  foreach(var variable in runtime.Variables)
     Console.WriteLine(
        "{variable.Name}:{variable.Type} {variable.IsOutput?"[OUTPUT]":"[INPUT]"}");
```

## Key features

- Arithmetic, Bitwise, Discreet operators
```py	
  # Arithmetic operators: + - * / % // ** 
  y1 = 2*(x//2 + 1) / (x % 3 -1)**0.5 + 3x
  
  # Bitwise:     ~ | & ^ << >> 
  y2 = (x | y & 0xF0FF << 2) ^ 0x1234
	
  # Discreet:    and or not > >= < <= == !=
  y3 = x and false or not (y>0)
```

- If-expression
```py
  simple  = if (x>0) x else if (x==0) 0 else -1
  complex = if (age>18)
                if (weight>100) 1
                if (weight>50)  2
                else 3
            if (age>16) 0
            else       -1     
```
- User functions and generic arithmetics
```py
  sum3(a,b,c) = a+b+c
  
  r:real = sum3(1,2,3)
  i:int  = sum3(1,2,3)
```
- Array, string, numbers, structs and hi-order fun support
```py
  out = {
    name = 'etaK'.reverse()
    values = [0xFF0F, 2_000_000, 0b100101]
    items = [1,2,3].map(rule 'item {it}')
  }
```
- Strict type system and type-inference algorithm
```py
  y = 2x
  z:int = y*x
  m:real[] = [1,2,3].map(rule it/2)
```
- Double or decimal arithmetics
- Syntax and semantic customization
- Built-in functions
- Comments

## How to

[API - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/ApiUsageExamplesAndExplanation.cs)

[Syntax - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/SyntaxExamplesAndExplanation.cs)

[Built in functions](https://github.com/tmteam/NFun/blob/master/Specs/Functions.md)

----
Boring specification is better than no specification

[Boring specification: Basics](https://github.com/tmteam/NFun/blob/master/Specs/Basics.md)

[Boring specification: Operators](https://github.com/tmteam/NFun/blob/master/Specs/Operators.md)

[Boring specification: Arrays](https://github.com/tmteam/NFun/blob/master/Specs/Arrays.md)

[Boring specification: Texts (Strings)](https://github.com/tmteam/NFun/blob/master/Specs/Texts.md)

[Boring specification: Structs](https://github.com/tmteam/NFun/blob/master/Specs/Structs.md)

[Boring specification: Rules (Anonymous functions)](https://github.com/tmteam/NFun/blob/master/Specs/Rules.md)

[Boring specification: Types](https://github.com/tmteam/NFun/blob/master/Specs/Types.md)


```                                                                                                           
     ';,                                                                                ;;      
   'lO0l                                                                               'dKkc    
  c0Xk;                                                                                  cOXk:  
'dXKc                 ';;;,'                                        ',;;;'                'dXKl 
dNKc                     ',;;;;,'                              ',;;;;,'                     oXXl
XNo                          '',;;;,'                      ',;;;,''                         'kW0
WK:                               ,:::,                  ,:::,                               lNX
W0;                            ',;;;;,                    ,;;;;,'                            cXN
W0;                        ',;;;,'                            ',;;;,'                        lXN
NXl                    ,;;;;,'                                    ',;;;;,                    dWK
OWO,                  ','        ',;;;,                  ,;;;,'        ','                  :KNd
:0Nk,                        ',;;;,'                        ',;;;,'                        :0Nx,
 ,xX0c                  ',;;;,,'                                ',,;;;,'                 'oKKo' 
   cOKO:               ,;,'                                          ',;,               l0Kx;   
     :dc                                                                               'lo;     
                                                                                                
                                                                                                
                                                                                                
                                     ,;;,              ';;,                                     
                                    ;:;;:,            ,:;;:;                                    
                                  ';:,  ,:;          ;:,  ,:;'                                   
                                 ';:'    ,:;'      ';:,    '::'                                  
                                 ',       ','      ','      ','                                  

```
