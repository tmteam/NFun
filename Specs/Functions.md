# NFun List of Standard Functions

Most functions may be applied for different types of operands. To simplify the description, we will give names to some sets of types:

| Name          | Types                                                           | Formal constrains                                   |
|---------------|-----------------------------------------------------------------|-----------------------------------------------------|
| All           | All types                                                       | T => any                                            |
| Integers      | `int64`, `int32`,`int16`,  `uint64`, `uint32`, `uint16`, `byte` | T => (int64 &#124; uint64)                          |
| Numbers       | `real`, **[Integers]**                                          | T => real                                           |
| Signed        | `int64`, `int32`, `int16`                                       | int16 => T => int64                                 |
| Arithmetics   | `real`, `int64`, `int32`,         `uint64`, `uint32`            | (int32 &#124; uint32) => T => real                  | 
| Comparables   | `text`, `char`, **[Numbers]**                                   | T is IComparable                                    |

## Concrete math functions

| Function              | Returns	                                                                     |
|-----------------------|------------------------------------------------------------------------------|
| sqrt(real):real       | the square root of a specified number                                        |
| pow(real,b:real):real | a specified number raised to the specified power                             |
| cos(real):real        | the cosine of the specified angle                                            |
| sin(real):real        | the sine of the specified angle                                              |
| tan(real):real        | the tangent of the specified angle                                           |
| atan(real):real       | the angle whose tangent is the specified number                              |
| atan2(real):real      | the angle whose tangent is the quotient of two specified numbers             |
| asin(real):real       | the angle whose sine is the specified number                                 |
| acos(real):real       | the angle whose cosine is the specified number	                              |
| exp(real):real        | e raised to the specified power	                                             |
| log(real):real        | the natural (base e) logarithm of a specified number                         |
| log(real,real):real   | the logarithm of a specified number in a specified base.                     |
| log10(real):real      | the base 10 logarithm of a specified number                                  |
| avg(real[]):real      | Computed average of an array of real numbers. Throws if input array is empty |

## Generic functions
| Function       | Constrains     | Returns	                                                                                                                                   |
|----------------|----------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| abs(T):T       | Signed, `real` | the absolute value                                                                                                                         |
| min(T,T):bool  | Comparables    | first or second argument, whichever is smaller. If any argument is equal to NaN (in case if T is real and real is double), NaN is returned |
| max(T,T):bool  | Comparables    | first or second argument, whichever is bigger. If any argument is equal to NaN (in case if T is real and real is double), NaN is returned  |
| convert(TA):TR | ----           | Converts argument of type TA to type TR if it is possible. For more information, see the conversion table                                  |

### Convertion tables for `convert` function

#### Useless converions

| Argument type | Result Type                | Description                                                                  |
|---------------|----------------------------|------------------------------------------------------------------------------|
| All           | Same type as argument type | Do nothing. Returns argument                                                 |
| All           | Argument type descendant   | Returns converted argument. Equals to `result:TR = argument`                 |
| All           | `text`                     | Returns text representation of an argument. Equals to `toText` function call |

#### Serialization (Result type is `byte[]`)
| Argument type | Returns                                                                                 |
|---------------|-----------------------------------------------------------------------------------------|
| `Character`   | array with 2 element - [lo,hi] bytes of unicode representation                          |
| `byte`        | array with single element (given argument)                                              |
| `bool`        | array with single element wich is `1` if argument is `true`, `0` if argument is `false` |
| Integers      | array with N elements from Little-endian encoding                                       |
| `real`        | array with 4 elements from Little-endian double floating number encoding                |
| `text`        | Encodes a set of characters from the specified text with Unicode encoding               |
| `char`        | Encodes single characters with Unicode encoding                                         |
| `ip`          | Encodes ip address as sequence of bytes                                                 |

#### Serialization (Result type is `byte[]`)
Same as Serialization to `byte[]`, but returns bit array

#### Deserialization (Argument type is `byte[]`)

| Result type | Returns                                                                                                               |
|-------------|-----------------------------------------------------------------------------------------------------------------------|
| `Character` | if array size is 1, returns ascii decoded symbol. If array size is 2 returns Unicode encoded symbol. throws otherwise |
| `bool`      | `false` if arguments first element is `0`, `true` if arguments first element is `1`                                   |
| Integers    | Decodes integer number from litle endian array                                                                        |
| `real`      | Decodes real double float number from litle endian array                                                              |
| `text`      | Decodes input Unicode sequence of bytes into text                                                                     |
| `char`      | Decodes single characters with Unicode encoding                                                                       |
| `ip`        | Decodes ip address                                                                                                    |

#### Parsing (Argument type is `text`)

| Result type | Returns                                                                                             |
|-------------|-----------------------------------------------------------------------------------------------------|
| `bool`      | `true` if text equals 'true' or '1', `false` if text equals 'false' or '0'. Raises `Oops` otherwise |
| Integers    | Parse integer number. Raises `Oops` otherwise if it is impossible                                   |
| `real`      | Parse real number with invarant culture. Raises `Oops` otherwise if it is impossible                |
| `ip`        | Parse Ip. Raises `Oops` otherwise if it is impossible                                               |

## Generic Array Functions

### Appliable to any arrays (without type constrains)
| Function                           | Returns	                                                                                                                                                                 |
|------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| first(T[]):T                       | first element of array. Throws if array is empty                                                                                                                         |
| last(T[]):T                        | last element of array. Throws if array is empty                                                                                                                          |
| count(T[]):int                     | array size                                                                                                                                                               |
| count(T[], rule(T):bool):int       | the number of array elements for which the rule is satisfied                                                                                                             |
| find(T[], T):int                   | index of the first element equal to the specified, -1 if no such element found                                                                                           |
| chunk(T[],n:int):T[][]             | Chunks (splits) the array into many smaller arrays with size of n. Last array may have size less than n. Throws if n is non positive                                     |
| flat(T[][]):T[]                    | Array with sequential enumeration of elements of all specified subarays                                                                                                  |
| fold(T[],rule(T,T):T):T            | Applies an accumulator rule over an array. Throws if given array is empty                                                                                                |
| fold(T[],seed:TR,rule(TR,T):TR):TR | Applies an accumulator rule over an array. The specified seed value is used as the initial accumulator value, and the specified rule is used to select the result value. |
| unite(T[],T[]):T[]                 | the set union of two arrays                                                                                                                                              |
| unique(T[],T[]):T[]                | array of elements that are contained in only one of the arrays                                                                                                           |
| intersect(T[],T[]):T[]             | array of elements that are contained in both arrays                                                                                                                      |
| concat(T[],T[]):T[]                | concatenation of two arrays                                                                                                                                              |
| append(T[],T):T[]                  | array of elements from given array and specified element in the tail of resulting array                                                                                  |
| except(T[],T[]):T[]                | array of elements that are contained in first array and not contained in second                                                                                          |
| map(T[],rule(T):TR):TR[]           | Projects each element of a given into a new form by given rule                                                                                                           |
| any(T[],rule(T):bool):bool         | `true` if the specified rule is satisfied at least for single element of given array. `false` if it is not, or array is empty                                            |
| all(T[],rule(T):bool):bool         | `true` if the specified rule is satisfied for all elements of given array. `false` if it is not, or array is empty                                                       |
| filter(T[],rule(T):bool):T[]       | an array consisting of elements of the original array for which the rule is satisfied                                                                                    |
| repeat( T,n:int):T[]               | an array in which the specified element is repeated n times                                                                                                              |
| reverse(T[]):T[]                   | reversed array                                                                                                                                                           |
| take(T[],n:int):T[]                | takes first n elements of given array. Equals to `[:n]` operator call                                                                                                    |
| skip(T[],n:int):T[]                | array, where first n elements of given array are skipped. Equals to `[n:]` operator call                                                                                 |

| Function                          | Constrains                 | Returns	                                                                                          |
|-----------------------------------|----------------------------|---------------------------------------------------------------------------------------------------|
| sum(T[], rule(T):TR):TR           | Arithmetics                | the sum of all the elements of the array                                                          |
| max(T[]):T                        | Comparables                | the maximum element in array                                                                      |
| min(T[]):T                        | Comparables                | the minimum element in array                                                                      |
| median(T[]):T                     | Comparables                | median element                                                                                    |
| sort(T[]):T[]                     | Comparables                | sorted array                                                                                      |
| sortDescending(T[]):T[]           | Comparables                | sorted array in reverse order                                                                     |
| sort(T[],rule(T):R):T[]           | T is All, R is Comparables | sorted array, where the element being compared is obtained by the specified rule                  |
| sortDescending(T[],rule(T):R):T[] | T is All, R is Comparables | Sorted array in reverse order, where the element being compared is obtained by the specified rule |

## Text Functions
| Function                          | Returns	                                                                                                                |
|-----------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| toText(any):text                  | text presentation of given value                                                                                        |
| concat(any[]):text                | concatenation of text representations of given values                                                                   |
| concat(any,any):text              | concatenation of text representations of given values                                                                   |
| concat(any,any,any):text          | concatenation of text representations of given values                                                                   |
| format(text,any[]):text           | replaces the format item in a specified text with the text representation of a corresponding value in a specified array |
| trim(text):text                   | removes all leading and trailing white-space characters from the current text                                           |
| trimStart(text):text              | removes all the leading white-space characters from the current text                                                    |
| trimEnd(text):text                | removes all the trailing white-space characters from the current string                                                 |
| toUpper(text):text                | a copy of this string converted to uppercase                                                                            |
| toLower(text):text                | a copy of this string converted to lowercase                                                                            |
| split(text,separator:text):text[] | splits a text into subtexts that are based on the provided text separator. Empty entries are removed                    |
| join(any[],separator:text):text   | concatenation of text representations of an value array, using the specified separator between each element.            |
