# NFun List of Standard Functions

Most functions may be applied for different types of operands. To simplify the description, we will give names to some sets of types:

| Name          | Types                                                           | Formal constrains                                   |
|---------------|-----------------------------------------------------------------|-----------------------------------------------------|
| All           | All types                                                       | T => any                                            |
| Integers      | `int64`, `int32`,`int16`,  `uint64`, `uint32`, `uint16`, `byte` | T => (int64 &#124; uint64)                          |
| Numbers       | `real`, **[Integers]**                                          | T => real                                           |
| Signed        | `int64`, `int32`, `int16`                                       | int16 => T => int64                                 |
| ArithIntegers | `int64`, `int32`,         `uint64`, `uint32`                    | (int32 &#124; uint32) => T => (int64 &#124; uint64) |       
| Arithmetics   | `real`, `int64`, `int32`,         `uint64`, `uint32`            | (int32 &#124; uint32) => T => real                  | 
| Comparables   | `text`, **[Numbers]**                                           | T is IComparable                                    |

## Concrete math functions

| Function                | Returns	                                                         |
|-------------------------|------------------------------------------------------------------|
| sqrt(x:real):real       | the square root of a specified number                            |
| pow(a:real,b:real):real | a specified number raised to the specified power                 |
| cos(x:real):real        | the cosine of the specified angle                                |
| sin(x:real):real        | the sine of the specified angle                                  |
| tan(x:real):real        | the tangent of the specified angle                               |
| atan(x:real):real       | the angle whose tangent is the specified number                  |
| atan2(x:real):real      | the angle whose tangent is the quotient of two specified numbers |
| asin(x:real):real       | the angle whose sine is the specified number                     |
| acos(x:real):real       | the angle whose cosine is the specified number	                  |
| exp(x:real):real        | e raised to the specified power	                                 |
| log(x:real):real        | the natural (base e) logarithm of a specified number             |
| log(a:real,b:real):real | the logarithm of a specified number in a specified base.         |
| log10(x:real):real      | the base 10 logarithm of a specified number                      |
| avg(x:real[]):real      |                                                                  |

## Generic functions
| Function          | Constrains     | Returns	 |
|-------------------|----------------|----------|
| abs(a:T):T        | Signed, `real` |          |
| min(a:T,b:T):bool | Comparables    |          |
| max(a:T,b:T):bool | Comparables    |          |
| convert(a:TA):TR  |                |          |

## Generic Array Functions
| Function                              | Constrains | Returns	 |
|---------------------------------------|------------|----------|
| first(a:T[]):T                        |            |          |
| last(a:T[]):T                         |            |          |
| count(a:T[]):int                      |            |          |
| count(a:T[], b:rule(T):bool):int      |            |          |
| sum(a:T[], b:rule(T):TR):TR           |            |          |
| max(a:T[]):T                          |            |          |
| min(a:T[]):T                          |            |          |
| median(a:T[]):T                       |            |          |
| sort(a:T[]):T[]                       |            |          |
| sortDescending(a:T[]):T[]             |            |          |
| sort(a:T[],b:rule(T):R):T[]           |            |          |
| sortDescending(a:T[],b:rule(T):R):T[] |            |          |
| find(a:T[], b:T):int                  |            |          |
| chunk(a:T[],b:int):T[]                |            |          |
| flat(a:T[][]):T[]                     |            |          |
| fold(a:T[],b:rule(T,T):T):T           |            |          |
| fold(a:T[],b:TR,b:rule(TR,T):TR):TR   |            |          |
| unite(a:T[],b:T[]):T[]                |            |          |
| unique(a:T[],b:T[]):T[]               |            |          |
| intersect(a:T[],b:T[]):T[]            |            |          |
| concat(a:T[],b:T[]):T[]               |            |          |
| append(a:T[],b:T):T[]                 |            |          |
| except(a:T[],b:T[]):T[]               |            |          |
| map(a:T[],b:rule(T):TR):TR[]          |            |          |
| any(a:T[],b:rule(T):bool):bool        |            |          |
| all(a:T[],b:rule(T):bool):bool        |            |          |
| filter(a:T[],b:rule(T):bool):T[]      |            |          |
| repeat( a:T,b:int):T[]                |            |          |
| reverse(a:T[]):T[]                    |            |          |
| take(a:T[],b:int):T[]                 |            |          |
| skip(a:T[],b:int):T[]                 |            |          |


## Text Functions
| Function                       | Returns	                                                                                                                |
|--------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| toText(a:any):text             | text presentation of given value                                                                                        |
| concat(a:any[]):text           | concatenation of text representations of given values                                                                   |
| concat(a:any,b:any):text       | concatenation of text representations of given values                                                                   |
| concat(a:any,b:any,c:any):text | concatenation of text representations of given values                                                                   |
| format(a:text,b:any[]):text    | replaces the format item in a specified text with the text representation of a corresponding value in a specified array |
| trim(a:text):text              | removes all leading and trailing white-space characters from the current text                                           |
| trimStart(a:text):text         | removes all the leading white-space characters from the current text                                                    |
| trimEnd(a:text):text           | removes all the trailing white-space characters from the current string                                                 |
| toUpper(a:text):text           | a copy of this string converted to uppercase                                                                            |
| toLower(a:text):text           | a copy of this string converted to lowercase                                                                            |
| split(t:text,sep:text):text[]  | splits a text into subtexts that are based on the provided text separator. Empty entries are removed                    |
| join(a:any[],b:text):text      | concatenation of text representations of an value array, using the specified separator between each element.            |
