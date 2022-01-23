# NFun. Expressions Evaluator for .NET

To install NFun, run the following command in the Package Manager Console:

```
PM> Install-Package NFun 
```

## What is the NFun?

This is an expression evaluator or a mini-script language for .net. It supports working with mathematical expressions as well as with collections, strings, hi-order functions and structures. NFun is quite similar to NCalc but with a rich type system and linq support.
See the ['How to'](https://github.com/tmteam/NFun#how-to) section for details

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
  // Evaluate many values and put it into 'ResultPerson' object 
  // 'age', and 'cars' are properties of 'ResultPeson' 
  ResultPerson u = Funny.CalcMany<MyInput, ResultPerson>(@"   
     age = 2022 - birthYear   # 'birthYear' is property of MyInput object 'input'
     cars = [
   	{ model = 'lada',   cost = 1200, power = 102 },
   	{ model = 'camaro', cost = 5000, power = 275 }
     ]
  ", input);
```
Low-level hardcore API is also supported
```cs
  var runtime = Funny.Hardcore.Build("y = 2*x+1");

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
  y1 = 2*(x//2 + 1) / (x % 3 -1)**0.5
  
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
  y = 2*x
  z:int = y*x
  m:real[] = [1,2,3].map(rule it/2)
```
- Built-in functions
- Syntax and semantic customization
- Comments
- And a lot more Funny stuff

## How to 

[API - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/ApiUsageExamplesAndExplanation.cs)

[Syntax - guide and examples](https://github.com/tmteam/NFun/blob/master/Examples/SyntaxExamplesAndExplanation.cs)

[Specification: Basics](https://github.com/tmteam/NFun/blob/Specification/Specs/Base%20syntax.md)

[Specification: Operators](https://github.com/tmteam/NFun/blob/Specification/Specs/Base%20operators.md)

[Specification: Array syntax and semantics](https://github.com/tmteam/NFun/blob/Specification/Specs/Array%20syntax%20and%20semantics.md)


## Let's make some fun
Now Nfun is in production-ready state with 5000 green tests. Any questions, suggestions, ideas and criticism are welcome.  

```                                                                                                            
      ';,                                                                                ;;       
    'lO0l                                                                               'dKkc     
   c0Xk;                                                                                  cOXk:   
 'dXKc                 ';;;,'                                        ',;;;'                'dXKl  
'dNKc                     ',;;;;,'                              ',;;;;,'                     oXXl 
cXNo                          '',;;;,'                      ',;;;,''                         'kW0;
dWK:                               ,:::,                  ,:::,                               lNXl
kW0;                            ',;;;;,                    ,;;;;,'                            cXNo
xW0;                        ',;;;,'                            ',;;;,'                        lXNl
lNXl                    ,;;;;,'                                    ',;;;;,                    dWK:
,OWO,                  ','        ',;;;,                  ,;;;,'        ','                  :KNd 
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
