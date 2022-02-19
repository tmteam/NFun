
# NFun Arrays

Array is an ordered sequence of elements of the same type. The count of array elements is known and unchangeable

Element type - the type of all elements in the array

Array size - the number of elements in the array

Arrays are immutable - this means that you cannot change the elements in the array after receiving the array

Arrays are covariant, which means that if type A is converted to type B, then an array of elements of type A is converted to an array of elements of type Byte

## Initialization

### Initializing an array by enumeration  

`[,]`  operator allows you to create an array with elements specified inside parentheses and separated by commas 

```
[element0, element1, ... elementN]
```
Example:
```
a = [1,2,3]
b = ['hello', ' ', 'world']
c = []
```

### Range Array Initialization  `[..]`

This operator takes two arguments and has folowing syntax:
```
syntax: [a..b] 
```
where `a` and `b` is any of [Numbers] types

if `a` greater than `b` it creates array with range from `a` to `b` **inclusive** 
Each next element in such an array is greater than the previous one by 1

if `b` is greater than `a` it creates reversed array where each next element is less than previous one by 1 started from `b`

Examples:
```

a = [1..5]     #[1,2,3,4,5]
a = [5..1]     #[5,4,3,2,1]
b = [-1..3]    #[-1,0,1,2,3]
c = [1.2..3.2] #[1.2, 2.2, 3.2]
d = [1.2..3]   #[1.2, 2.2] 
```

### Step - Range Array Initialization   

This operator takes three arguments and has folowing syntax:
```
[a..b step s] 
```
where `a`,`b`,`s` is any of [Integers] types

The operator is similar to 'Range Array Initialization Operator' but it allows you to specify the 'step' - difference between each next element

if `a` is greater than `b` it creates array with range from `a` to `b` **inclusive** where each next element is greater than the previous one by 's'

otherwise it creates reversed array 

Examples:
```
a = [1..7 step 2]       # [1,3,5,7]
b = [7..1 step 2]       # [7,5,3,1]
c = [1.0..3.0 step 0.5] # [1.0, 1.5, 2.0, 2.5, 3.0]
```

## Equality

two arrays are equal if and only if they contain the same number of elements, and these elements are equal to each other, respectively

```py

i1:int[] = [1,2,3]
i2:int[] = [1,2,3]
i3:int[] = [3,2,1]
r:real[] = [1,2,3]


res1 = i1 == i2 # true
res2 = i2 == i3 # false
res3 = r == i1  # true
```

## Access

### Get Array Element `[]`  

```
a[i]
```
Allows you to get i-th element in array `a` . 
Here, `i` called 'index' and has type of `int32`.
The enumeration of index starts with 0 

```
array = [1,4,0,3]

e = array[0] #returns 1
j = array[1] #returns 4

``` 

If the index is negative or it is greater or equal to array size - an runtime exception will be thrown 

### Slices  `[:]`   

```
a[b:e] 
```

Allows you to get 'slice' - subarray, that starts from b-th element of origin array and ends with e-th element of origin array `a`

`b` and `e` has type of `int32`. The enumeration starts with 0

```
array = [1,4,0,3]

e = array[0:2] #returns [0,1,2]
j = array[2:3] #returns [4,0]
k = array[1:1] #returns [4]


``` 

if `i` equals zero - it can be skipped
if `j` equals to index of last array element - it also can be skipped

```
array = [1,4,0,3]

e = array[:2] #returns [0,1,2]
j = array[3:] #returns [0,3]
k = array[2:] #returns [1,4,0,3]

``` 
### Slice with step `[::]` 

```
a[b:e:s]
```
allows you to get new array, that takes only every 's'-th element of origin array, starts from b-th element of origin array `a` and ends with e-th element of origin array

`b`, `e`, s has type of int32. The enumeration starts with 0

`b` and/or `e` can be skipped (as in `[:]` operator) if `b` equals 0 and/or `e` equals to index of last array element according.

's' also can be skipped if it equals 1. in this case `[a:b:]` operator equals to slice operator `[a:b]`


Here are some examples
```

array = [0,1,2,3,4,5,6,7,8,9,10]

a = array[1:7:2] #[1,3,5,7]
b = array[1::2] #[1,3,5,7,9]
c = array[::4] #[0,4,8]
d = array[:4:3] #[0,3]

e = array[1:2:] #[1,2]
f = array[5::]  #[5,6,7,8,9,19]
j = array[:2:]  #[0,1,2]
``` 

## Membership operator `in`
```
A in B
```
`in` operators answers the questions: 'does array B contains element A?'. 
If it is, than expression equals true. Expression equals false otherwise.

```
array = [1,2,3]

b = 1 in array # true
c = 0 in array # false
d = not 1 in array # false

```

