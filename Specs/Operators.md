# NFun operators

Nfun provides the following types of operators:
* Arithmetic Operators
* Relational Operators
* Logical Operators
* Bitwise Operators
* Misc Operators

Most operators may be applied for different types of operands. To simplify the description, we will give names to some sets of types:

| Name          | Types                                              | Formal constrains                                   |
|---------------|----------------------------------------------------|-----------------------------------------------------|
| All           | All types                                          | T => any                                            |
| Integers      | int64, int32, int16,  uint64, uint32, uint16, byte | T => (int64 &#124; uint64)                          |
| Numbers       | real, **[Integers]**                               | T => real                                           |
| Signed        | int64, int32, int16                                | int16 => T => int64                                 |
| ArithIntegers | int64, int32,         uint64, uint32               | (int32 &#124; uint32) => T => (int64 &#124; uint64) |       
| Arithmetics   | real, int64, int32,         uint64, uint32         | (int32 &#124; uint32) => T => real                  | 
| Comparables   | text, **[Numbers]**                                | T is IComparable                                    |

## Operators Precedence in NFun

Operator precedence determines the grouping of terms in an expression and decides how an expression is evaluated.
Certain operators have higher precedence than others.
For example, the multiplication operator has a higher precedence than the addition operator.

For example:
```
x = 7 + 3 * 2 
```
here, x is assigned 13, not 20 because operator * has a higher precedence than +, so it first gets multiplied with 3*2 and then adds into 7.

Following list shows operators precedence:
operators with the highest precedence appear at the top of the list, those with the lowest appear at the bottom.
Within an expression, higher precedence operators will be evaluated first.

| Operators          | Explanation                          |
|--------------------|--------------------------------------|
| () [] . -(*unary*) | *Various*                            |
| ** ~               | Exponentiation, bitwise NOT          |
| * / // %           | Multiplication, divisions, remainder | 
| + -                | Addition, subtraction                |
| <<  \>\>           | Bitwise Shifts                       |
| &                  | Bitwise AND                          |
| ^                  | Bitwise XOR                          |
| &#124;             | Bitwise OR                           |
| in == != > < >= <= | Comparisons, membership              |
| not                | Logical NOT                          |
| and                | Logical AND                          |
| xor                | Logical XOR                          |
| or                 | Logical OR                           |
| rule               | Anonymous function                   |
| =                  | Variable initialization              |


## Arithmetic Operators

The following table shows all the arithmetic operators supported by the NFun language. 

Assume variable A holds 6 and variable B holds 4 then:

| Operator    | Types       | Description	                                       | Example      |
|-------------|-------------|----------------------------------------------------|--------------|
| +           | Arithmetics | Adds two operands.	                                | A + B = 10   |
| −           | Arithmetics | Subtracts second operand from the first.	          | B − A = -2   |
| *           | Arithmetics | Multiplies both operands.	                         | A * B = 24   |
| %           | Arithmetics | Modulus Operator - remainder of after an division. | A % B = 2    |
| //          | Integers    | Divides integer numerator by de-numerator.	        | A / B = 1.5  |
| /           | real	       | Divides real numerator by de-numerator.	           | A //B = 1    |
| **          | real        | Raising the base A to the power of B               | A**B  = 1296 |
| − *(unary)* | Signed      | Multiply expression by -1.	                        | −A = -6      |

## Relational Operators
The following table shows all the relational operators supported by NFun. 
All of them returns true if condition is satisfied, and false otherwise

Assume variable A holds 10 and variable B holds 20 then:

| Operator | Types       | Description	                                                                                     | Example            |
|----------|-------------|--------------------------------------------------------------------------------------------------|--------------------|
| ==       | All         | Equals true if the values of two operands are equal.	                                            | (A == B) is false. |
| !=       | All         | Equals true if the values of two operands are not equal.	                                        | (A != B) is true.  |
| \>       | Comparables | Equals true if the value of left operand is greater than the value of right operand. 	           | (A > B) is false.  |
| <        | Comparables | Equals true if the value of left operand is less than the value of right operand.                | (A < B) is true.   |
| \>=      | Comparables | Equals true if the value of left operand is greater than or equal to the value of right operand. | (A >= B) is false. |
| <=       | Comparables | Equals true if the value of left operand is less than or equal to the value of right operand. 	  | (A <= B) is true.  |

## Logical Operators
The Following table shows all the logical operators supported by NFun language. 

Assume variable A holds true and variable B holds false, then:

| Operator       | Type | 	  Description	                                                               | Example             |
|----------------|------|-------------------------------------------------------------------------------|---------------------|
| and	           | bool | If both the operands are true, then the condition becomes true.			            | (A and B) is false. |
| or	            | bool | If any of the two operands is true, then the condition becomes true.	         | (A or B)  is true.  |
| xor	           | bool | If two operands are not equal to each other, then the condition becomes true. | (A xor B) is true.  |
| not *(unary)*	 | bool | Reverses the logical state of its operand.                                    | (not A)   is false. |

The truth tables for 'and', 'or', 'xor' and 'not' is as follows:

| p     | q      | p and q | p or q	 | p xor q | not p |
|-------|--------|---------|---------|---------|-------|
| false | 	false | false	  | false	  | false   | true  |
| false | 	true  | false	  | true	   | true    | true  |
| true  | 	true  | true	   | true	   | false   | false |
| true  | 	false | false	  | true	   | true    | false |

## Bitwise Operators

Bitwise operator works on bits and perform bit-by-bit operation.
The following table lists the bitwise operators supported by NFun. 

Assume variable A has type of **byte** and holds 60 (0b0011_1100) 
and variable B has type of **byte** holds 13 (0b0000_1101), then:

| Operator     | Type     | Description	                                                                     | Example                       |
|--------------|----------|----------------------------------------------------------------------------------|-------------------------------|
| &	           | Integers | Binary AND Operator copies a bit to the result if it exists in both operands.	   | A & B = 12 = 0b0000_1100      |
| &#124;	      | Integers | Binary OR Operator copies a bit if it exists in either operand.	                 | A &#124; B = 61 = 0b0011_1101 |
| ^	           | Integers | Binary XOR Operator copies the bit if it is set in one operand but not both.	    | A ^ B = 49 = 0b0011_0001      |
| ~ *(unary)*	 | Integers | Binary One's Complement Operator is unary and has the effect of 'flipping' bits. | 	~A = ~60 = 0b1100_0011       |

Bitshift operators takes **[Integers]** type as left operand and result. Right operand has type of byte

| Operator | Description	                                                                                                              | Example                       |
|----------|---------------------------------------------------------------------------------------------------------------------------|-------------------------------|
| <<	      | Binary Left Shift Operator. The left operands value is moved left by the number of bits specified by the right operand.	  | A << 2 = 240 = 0b1111_0000    |
| \>\>	    | Binary Right Shift Operator. The left operands value is moved right by the number of bits specified by the right operand. | 	A \>\> 2 = 15  = 0b0000_1111 |

Assume A = 60 and B = 13 in binary format, they will be as follows:
```
A      = 0b0011_1100
B      = 0b0000_1101
--------------------
~A     = 0b1100_0011
A << 2 = 0b1111_0000
A >> 2 = 0b0000_1111
--------------------
A & B  = 0b0000_1100
A | B  = 0b0011_1101
A ^ B  = 0b0011_0001
```


## Misc Operators

The following operators perform special actions and are described in detail in the relevant sections of the specification

Here we give only a superficial description of them

| Operator       | Described in | Description                                                                                                                                                                                                       | Example                   |
|----------------|--------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------|
| rule *(unary)* | Basics       | Returns an anonymous function with the body specified in the operand. The arguments of this function are called 'it' (for the case of a function with one argument) or it1, it2... for the case of many arguments | [1,2,3].filter(rule it>1) |
| =              | Basics       | Initialization operator. Initializes left side variable with values from right side operand                                                                                                                       | a = 42; c = [a,2,3]       |
| .              | Structures   | Field access operator                                                                                                                                                                                             | a = user.name             |
| in             | Arrays       | Membership operator. Returns true if the element (left operand) is contained in the array (right operand)                                                                                                         | 1 in [1,2,3]              |
| []             | Arrays       | Index Operator. Selects an element from the array (left operand) that is at the specified position (in-brackets operand)                                                                                          | [1,0,2][2]                |
| [:] , [::]     | Arrays       | Slice operator. Creates subarray from origin array (left operand) with specific range (in-bracets operands 'start' and 'end') inclisive                                                                           | [1,2,3,4,5][1:3]          |
