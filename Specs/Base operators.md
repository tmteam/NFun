https://www.tutorialspoint.com/cprogramming/c_operators.htm

# NFun operators

An operator is a symbol that tells the nfun to perform specific function. Nfun provides the following types of operators −

Arithmetic Operators
Relational Operators
Logical Operators
Bitwise Operators
Array Operators
Misc Operators


Most operators may be applied for different types of operands. To simplify the description, we will give names to some sets of types:

```
Name                 Types                                               Formal constrains
Numbers      | real, int64, int32, int16,  uint64, uint32, uint16, byte | T=>real          
Integers     |       int64, int32, int16,  uint64, uint32, uint16, byte | T->int96       
SignIntegers |       int64, int32, int16                                | int16=>T->int96       
ArithIntegers|       int64, int32,         uint64, uint32               | uint24->T->int96       
Arithmetics  | real, int64, int32,         uint64, uint32               | uint24->T=>real  
Comparables  | text, [Numbers]                                          | T:IComparable
All          | all types												   | T=>any  
```


## Arithmetic Operators

The following table shows all the arithmetic operators supported by the NFun language. 
Assume variable A holds 6 and variable B holds 4 then:

```
Operator     Types         Description	                                                  Example
+        | Arithmetics |  Adds two operands.	                                       | A + B = 10
−        | Arithmetics |  Subtracts second operand from the first.	                   | B − A = -2
*        | Arithmetics |  Multiplies both operands.	                                   | A * B = 24
%        | Arithmetics |  Modulus Operator and remainder of after an integer division. | A % B = 2
//       | Integers    |  Divides integer numerator by de-numerator.	               | A / B = 1.5
/        | real	       |  Divides real numerator by de-numerator.	                   | A //B = 1
**       | real        |  Raising the base A to the power of B                         | A**B  = 1296 

```

TODO : Arithmetic operators such as *-+ will produce an error in case of a discharge overflow


## Relational Operators
The following table shows all the relational operators supported by NFun. 
All of them returns true if condition is satisfied, and false otherwise
Assume variable A holds 10 and variable B holds 20 then:
```
Operator	 Types            Description	                                                                                    Example
==	     | All         | Equals true if the values of two operands are equal or not.	                                  |  (A == B) is not true.
!=	     | All         | Equals true if the values of two operands are equal or not.	                                  |  (A != B) is true.
>	     | Comparables | Equals true if the value of left operand is greater than the value of right operand. 	          |  (A > B) is not true.
<	     | Comparables | Equals true if the value of left operand is less than the value of right operand. 	              |  (A < B) is true.
>=	     | Comparables | Equals true if the value of left operand is greater than or equal to the value of right operand. |	 (A >= B) is not true.
<=	     | Comparables | Equals true if the value of left operand is less than or equal to the value of right operand. 	  |  (A <= B) is true.
```

## Logical Operators
The Following table shows all the logical operators supported by NFun language. 

Assume variable A holds true and variable B holds false, then:
```
Operator  type	  Description	                                                                                                Example
and	    | bool | Called Logical AND operator. If both the operands are true, then the condition becomes true.			   | (A and B) is false.
or	    | bool | Called Logical OR Operator.  If any of the two operands is true, then the condition becomes true.	       | (A or B) is true.
xor	    | bool | Called Logical XOR Operator. If two operands are not equal to each other, then the condition becomes true.| (A xor B) is true.
not	    | bool | Called Logical NOT Operator. Reverses the logical state of its operand.                                   | !(A and B) is true.
```
The truth tables for 'and', 'or', 'xor' and 'not' is as follows:
```
  p   |   q   |  p and q |   p or q	 |   p xor q  | not p
false |	false |   false	 |    false	 |    false   | true
false |	true  |   false	 |    true	 |    true    | 
true  |	true  |   true	 |    true	 |    false   | false
true  |	false |   false	 |    true	 |    true    |
```

## Bitwise Operators

Bitwise operator works on bits and perform bit-by-bit operation.
The following table lists the bitwise operators supported by NFun. Assume variable 'A' has type of int32 and holds 60 (0b0011_1100) and variable 'B' holds 13 (0b0000_1101), then:

```
Operator   Type           Description	                                                                     Example
&	    |  Integers    | Binary AND Operator copies a bit to the result if it exists in both operands.	  | A & B = 12 = 0b0000_1100
|	    |  Integers    | Binary OR Operator copies a bit if it exists in either operand.	              | A | B = 61 = 0b0011_1101
^	    |  Integers    | Binary XOR Operator copies the bit if it is set in one operand but not both.	  | A ^ B = 49 = 0b0011_0001
~	    |  Integers    | Binary One's Complement Operator is unary and has the effect of 'flipping' bits. |	~A = ~60 = -0111101

Following operators takes [ArithIntegers] type as left operand and result. Right operand has type byte

Operator      Description	                                                                                                                    Example
<<	    |   Binary Left Shift Operator. The left operands value is moved left by the number of bits specified by the right operand.	  | A << 2 = 240 = 0b1111_0000
>>	    |   Binary Right Shift Operator. The left operands value is moved right by the number of bits specified by the right operand. |	A >> 2 = 15  = 0b0000_1111

```
Assume A = 60 and B = 13 in binary format, they will be as follows:

-----------------
A = 0b0011_1100
B = 0b0000_1101
-----------------

```
~A     = 0b1100_0011
A << 2 = 0b1111_0000
A >> 2 = 0b0000_0000
A & B  = 0b0000_1100
A | B  = 0b0011_1101
A ^ B  = 0b0011_0001
```

TODO : Bitwise operators such as >> << will produce an error in case of a discharge overflow



## Operators Precedence in NFun

Operator precedence determines the grouping of terms in an expression and decides how an expression is evaluated. 
Certain operators have higher precedence than others
For example, the multiplication operator has a higher precedence than the addition operator.

For example, x = 7 + 3 * 2; here, x is assigned 13, not 20 because operator * has a higher precedence than +, so it first gets multiplied with 3*2 and then adds into 7.


Here, operators with the highest precedence appear at the top of the list, those with the lowest appear at the bottom. 
Within an expression, higher precedence operators will be evaluated first.
```
0: () [] [:] [::] [..] [,] . -
1: ** ~
2: * / // %
3: + - << >> 
4: & ^ in == != > < >= <=
5: and not
6: or xor |
```
