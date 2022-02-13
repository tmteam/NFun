# NFun Structures 

A structure is a data structure that contains a fixed set of named values.
Each such value is called a **field**. Each field has an identifier, type, and value.

## Structure initialization `{}` 

Initlalizes structure with specified fields 

To create a structure, you need to list the field id (separated by commas or on separate lines) inside curly brackets and initialize them with values in the same way as output variables are initialized
```
a = {
	name = 'foo'
}

b = {
	name = 'foo'.reverse()
	age = 42
}

d = { name = 'foo',age = 42 }

x = {} #empty struct
```

## Field access  

The `.` operator is used to access the value of the structure field.

```
id =  user.name #Access to field 'name' in struct 'user'

z =  user.stats.coasts # Nested access
```

## Immutability

The structure is an immutable value. After the structure is created, you can
neither change the values of its fields, nor change their composition

```py
a = { age = 13}

a.age = 42 #error. Struct cannot be modified
```

## Equality

Two structs are equal if they has same list of fields (id and types) and all the values of these fields are equal respectively 

```py

a = { name = 'Kate', age = 31}
b = { age = 31, name = 'Kate'}
c = { name = 'Kate'}


res1 = a == b # true
res2 = c == b # false
```

## Default value

Default value for struct of some type - is a structure in which all fields are initialized with the default value

```py

a = default
b:int = a.age # 0
```

## Conversion of structures

For information about converting structures, see the boring section **Types**

