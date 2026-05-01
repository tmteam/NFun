# NFun — Expression Evaluator for .NET

Embeddable expression language with rich type system, absolute type inference, and LINQ-like collections.

```
PM> Install-Package NFun
```

## Quick Start

```cs
// Simple evaluation
double d = Funny.Calc<double>("2 * 10 + 1"); // 21
bool b = Funny.Calc<bool>("false and (2 > 1)"); // false

// With input model
string alias = Funny.Calc<User, string>(
    "if(age < 18) name else 'Mr. {name}'", user);

// Multiple inputs and outputs
var person = new Person(birthYear: 2000);
Funny.CalcContext(@"
    age = 2022 - birthYear
    cars = [
        { name = 'lada',   cost = 1200, power = 102 },
        { name = 'camaro', cost = 5000, power = 275 }
    ]", person);

// Low-level API
var runtime = Funny.Hardcore.Build("y = 2x + 1");
runtime["x"].Value = 42;
runtime.Run();
var result = runtime["y"].Value; // 85
```

## Key Features

**Operators & Arithmetic**
```py
y1 = 2*(x//2 + 1) / (x % 3 - 1)**0.5 + 3x   # arithmetic, implicit multiplication
y2 = (x | y & 0xF0FF << 2) ^ 0x1234            # bitwise
y3 = x and false or not (y > 0)                  # logical
```

**If-expressions**
```py
y = if(age > 18)
        if(weight > 100) 'heavy'
        if(weight > 50)  'normal'
        else 'light'
    if(age > 16) 'teen'
    else 'child'
```

**Collections & Higher-Order Functions**
```py
out = [1,2,3,4,5]
    .filter(rule it > 2)
    .map(rule it * 10)
    .reverse()                  # [50, 40, 30]
```

**Structs & Named Types**
```py
type point = {x: int, y: int}
p: point = {x = 3, y = 4}
dist = (p.x**2 + p.y**2)**0.5
```

**Optional Types & Type Narrowing**
```py
user:{name: text, age: int}? = none
greeting = if(user == none) 'Guest'
           if(user.age > 18) 'Hello, {user.name}'
           else 'Hi, {user.name}'
```

**Strict Type System with Full Inference**
```py
y = 2x                              # x and y inferred as Int32
z:int = y * x                       # explicit annotation
m:real[] = [1,2,3].map(rule it/2)   # [0.5, 1.0, 1.5]
```

## Dialect Customization

```cs
var runtime = Funny.Hardcore
    .WithDialect(
        integerPreferredType: IntegerPreferredType.I64,
        integerOverflow: IntegerOverflow.Unchecked,
        optionalTypesSupport: OptionalTypesSupport.Enabled,
        namedTypesSupport: NamedTypesSupport.Enabled)
    .Build("y = 2x + 1");
```

## Documentation

- [Built-in Functions](https://github.com/tmteam/NFun/blob/master/Specs/Functions.md)
- [Operators](https://github.com/tmteam/NFun/blob/master/Specs/Operators.md)
- [Types](https://github.com/tmteam/NFun/blob/master/Specs/Types.md)
- [Arrays](https://github.com/tmteam/NFun/blob/master/Specs/Arrays.md)
- [Structs](https://github.com/tmteam/NFun/blob/master/Specs/Structs.md)
- [Optional Types](https://github.com/tmteam/NFun/blob/master/Specs/Optionals.md)
- [Named Types](https://github.com/tmteam/NFun/blob/master/Specs/NamedTypes.md)
- [All Specifications](https://github.com/tmteam/NFun/tree/master/Specs)
