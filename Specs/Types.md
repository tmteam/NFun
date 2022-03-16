# NFun Type System

Nfun is a strongly typed language with static typing. This means that each expression has a type before the execution.
If the expression is inconsistent by type, such a script will not be assembled (you will get an error at the script assembly stage)

```py
a = 'some text'
b = 'some other text'
c = a * b # error. * operator is not appliable to text types
```

## Type identifier
Type identifier is the type name used in the code

An example of such an identifier would be `bool` for boolean type, `text` for an array of characters (string), etc
Some types have multiple identifiers like `byte` and `uint8` for a byte, and `int32` and `int` for a 32-bit integer

Some types do not have an identifier at all. It is true for  `char`, `rule` and `struct`


## Primitive types

| Name            | Identifier      | Description                                                                                              | Example                            |
|-----------------|-----------------|----------------------------------------------------------------------------------------------------------|------------------------------------|
| Any             | `any`           | Any value                                                                                                | `y:any = if(true) 12 else 'test' ` |
| Boolean         | `bool`          | Discrete value                                                                                           | `y:bool = true or false `          |
| Character       | ---             | Symbol. Cannot be set explicitly                                                                         | `y = 'text'[0]`                    |
| Byte            | `uint8` `byte`  | Integer value [0..255]                                                                                   | `y:byte = 123; z:uint8 = 0xFF`     |
| Unsigned int 16 | `uint16`        | Integer value [0..65535]                                                                                 | `y:uint16 = 123 `                  |
| Unsigned int 32 | `uint32` `uint` | Integer value [0..4294967295]                                                                            | `y:uint32 = 123 `                  |
| Unsigned int 64 | `uint64`        | Integer value [0..18446744073709551615]                                                                  | `y:uint64 = 123 `                  |
| Signed int 16   | `int16`         | Integer value [-32768..32767]                                                                            | `y:int16 = 123 `                   |
| Signed int 32   | `int32` `int`   | Integer value [-2147483648..2147483647]                                                                  | `y:int32 = 123 `                   |
| Signed int 64   | `int64`         | Integer value [-140737488355328..140737488355327]                                                        | `y:int64 = 123 `                   |
| Natural value   | `real`          | Non-integer numeric value . Depending on the settings, it can be either a double or a decimal clr number | `y:real = 123.5 `                  |

## Generalized types

### Arrays and `text` (strings)

Array is an ordered sequence of elements of the same type.
The count of array elements is known and unchangeable

The type of the array element is covariant,
that means: `a[]` is convertable to `b[]` if `a` is convertable to `b`
```py
y:byte[] = [1,2,3]
out:int[] = y
```
You can read more about arrays in the boring **Arrays** section

The `text` is an alias for an array of characters, but the comparison operator is allowed on the `text` type

## Functional type rule

A higher-order function, i.e. an anonymous function created via rule-syntax is an expression with the type of `rule`
```
rule(T1,T2...TN)->TR
```

Here T1..Tn - types of the 1st..Nth elements. `Tr` is the return type of the function

The types of function arguments are covariant,
that means: `rule(Ta)->TR` is converted to `rule(Tb)->TR` if `Ta` is converted to `Tb`

The return type of the function is contravariant
, that is, `rule(T)->Ta` is converted to `rule(T)->Tb` if `Tb` is converted to `Ta`

## Struct type struct

The struct type describes a set of named fields

The field types are invariant, which means that `{name:Ta}` is convertable to `{name:Tb}` only if `Ta` is `Tb`

The set of fields of the structure is being expanded. This means that struct A is convertible to struct B if A contains all the fields of structure B (with the same names and types)
At the same time, structure A can contain any number of additional fields.

## Type casting

Objects of different types can be implicitly cast to each other, which means that you can use an object of a origin type (type descendant)
for use where the target type (type ancestor) is expected

```py
a:int = 123
b:real = a # int is convertable to real
c:any = b # real is converable to any
```

### Type conversion rules

- Objects of any type are reducible to the type `any`
- Array A is convertable to array B if the types of their elements are convertable
- Function rule  A is convertable to function rule B if
  - number of function arguments are equal
  - each type of argument A is convertible to the appropriate argument of B
  - the return type of B is convertable to the return type of A
- struct A  is convertible to struct B if A contains all the fields of the structure B (with the same names and types)
- All numeric types are convertible to the real type
- Unsigned integer types of low bitness are convertable to any integer types of higher bitness
- Signed integer types of low bitness are reduced to Signed integer types of higher bitness


This knowledge can be displayed on the following graph
```
any
 ↑
 |<--array
 |<--rule
 |<--struct
 |
 |<--char
 |<--bool
 |
 |<--real
       ↑
       |<-------------int64<-----int32<-----int16
       |                 ↑            ↑         ↑
       |<---uint64<-----uint32<------uint16<----byte
```

## Generalized integer constants

For decimal literals, the same symbol may have different types depending on the context:

```py
a:int = 1
b:int64 = 1
c:real = 1
```
here the expression '1' had a different type depending on the context.

Each integer constant has the following characteristics:
- most specific possible type 
- most abstract possible type
- preferred type

The most abstract possible type for all integer constants is real

The most specific possible type for all integer constants is the type with the minimum maximum value, 

for example:

```py
byte if the constant <=255
int16 if the constant <=32767
uint16 if the constant <= 65535

etc.
```

The preferred type for all integer constants is int32
(can be redefined in the interpretation settings), but only if this preferred type is reduced to the most specific possible type
Otherwise, there is no preferred constant type.


## Generalized functions and generalized operators

Functions and operators can be used not only for specific types, but also for a 'range' of types - for types satisfying certain constraints - 'type constraints'

For example, the multiplication operator `*` is applicable for the types `real`, `uint64`,`int64`, `uint32`, `int32`
(formally: `(uint32 | int32)=>T=>real`)

```py
a:int = 1*2
b:int64 = 1*2
c:real = 1*2
```

The function with the use of the multiplication operator will also be generalized:

```py
f(a,b,c) = a*b*c

x:int = f(1,2,3)
y:int64 = f(1,2,3)
z:real = f(1,2,3)
```

In this case, the arguments of the function and its return will have the same type `T` with a formal constraint `(uint32 | int32)=>T=>real`