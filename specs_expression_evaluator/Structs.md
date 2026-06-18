# NFun Structures 

A structure is a data structure that contains a fixed set of named values.
Each such value is called a **field**. Each field has an identifier, type, and value.

## Structure initialization `{}` 

Initializes structure with specified fields.

To create a structure, you need to list the field id (separated by commas or on separate lines) inside curly brackets and initialize them with values in the same way as output variables are initialized
```
a = {
	name = 'foo'
}

b = {
	name = 'foo'.reverse()
	age = 42
}

d = { name = 'foo',age = 42, }

x = {} #empty struct
```

## Field access  

The `.` operator is used to access the value of the structure field.

```
id =  user.name #Access to field 'name' in struct 'user'

z =  user.stats.coasts # Nested access
```

## Field names

Any identifier can be a field name, including primitive-type keywords (`int`, `real`, `bool`, `text`, `char`, etc.) — context disambiguates

```py
c = {real = 1.0, imag = 2.0}     # field named 'real' of type real
y = c.real                       # 1.0
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

The "list of fields" is the *stored* shape of the value, not the declared type's
field list. A type annotation acts like a C# interface or Go interface: it
narrows the static slot via width subtyping (see Types.md §Struct), but the
runtime value retains every field of the literal it was initialized from.
Equality, hashing, `in`, and `intersect` all read the stored shape — so an
extra field on one side rules out equality even when both sides share the
same declared type:

```py
a:{x:int} = {x = 1, y = 2}
b:{x:int} = {x = 1}
res = a == b   # false — a still stores y, b does not
```

## Default value

Default value for struct of some type - is a structure in which all fields are initialized with the default value

```py

a = default
b:int = a.age # 0
```

## Conversion of structures

For information about converting structures, see the boring section **Types**

