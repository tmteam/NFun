using System;
using NFun.Exceptions;
using NFun.Types;
using NUnit.Framework;

namespace NFun.Examples {

public class SyntaxExamplesAndExplanation {
    [Test]
    public void Basics() { /*        
        Input variables called 'Inputs'
        Output variables called 'Outputs'
        
        The names of the inputs and outputs and their types are determined automatically
        Comments starts with '#'
        */
        var r1 = Funny.Hardcore.Build(
            @"
                # Here is the comment
                y = 10*x +1 # Here is an expression
            ");
        Assert.AreEqual(false, r1["x"].IsOutput);
        Assert.AreEqual(true, r1["y"].IsOutput);

        // You can skip the name of the output if there is only one expression.
        // The anonymous output gets the name 'out'
        var r2 = Funny.Hardcore.Build("10 * x + 1");
        Assert.AreEqual(true, r2["out"].IsOutput);

        // NFun language Is CasE-SEnsiTiVe. But you cannot create two variables with different case
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.Build("x + X"));

        // The inputs are not necessary, for constant calculation
        var r3 = Funny.Hardcore.Build("10+1");
        r3.Run();
        Assert.AreEqual(11, r3["out"].Value);

        // NFun is sensitive to the fact that each variable definition starts with a new line.
        // In this case, ';' is the equivalent of a line break
        var r5 = Funny.Hardcore.Build(
            @"
                x=1
                y = 10
            ");
        Assert.AreEqual(true, r5["x"].IsOutput && r5["y"].IsOutput);

        var r6 = Funny.Hardcore.Build("x=1; y = 10");
        Assert.AreEqual(true, r6["x"].IsOutput && r6["y"].IsOutput);

        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.Build("x=1 y = 10"));
    }

    [Test]
    public void Arithmetics() { /*
         Arithmetic operations
         y = x+1 # Summation
         y = x-1 # Subtraction
         y = x*2 # Multiplication
         y = x/2 # Division
         y = x%3 # Remainder of the division
         y = x//2 # Integer division
         y = x**2 # Pow
         
         The priorities of operations are normal: 
                      1) **
                      2) / * % //
                      3) + -
         Brackets are used to indicate priorities */
        Assert.AreEqual(9, Funny.Calc<double>("(10//2 + (1-12) %3) ** 2"));
    }

    [Test]
    public void Flow() {
        // Script may contains multiple input and outputs
        var r1 = Funny.Hardcore.Build(
            @"
                sum = x1+x2
                dif = x1-x2
            ");
        r1["x1"].Value = 10;
        r1["x2"].Value = 5;
        r1.Run();
        Assert.AreEqual(15, r1["sum"].Value);
        Assert.AreEqual(5, r1["dif"].Value);

        // Outputs can be used in further calculations
        Funny.Hardcore.Build(
            @"
                d  = b**2 - 4*a*c
                y1 = (-b + d**0.5) /2*a # 'd' used
                y2 = (-b - d**0.5) /2*a # 'd' used
            ");
    }

    [Test]
    public void FunctionBasics() {
        // There are lot of built in functions
        var r1 = Funny.Hardcore.Build("y = cos(x)");
        // The syntax for calling functions is standard
        var r2 = Funny.Hardcore.Build("y = abs (tan (max(x1,x2)))");

        // Reverse function syntax.
        // The first argument of the function call can be pulled out before the functions (pipe-forward):
        // Origin: y = cos(x)
        // Equal to: y = x.cos() 

        var r3 = Funny.Hardcore.Build(
            @"
                # Origin: y = max(x2, abs(tan(cos(x1))))
                # Equals to:
                y = x1.cos().tan().abs().max(x2)
            ");
        //Some other arithmetics functions:
        //cos(0),sin(0),acos(1),asin(0),atan(0),atan2(0,1)
        //tan(0),exp(0),log(1,10),log(1),log10(1),ceil(7.03),floor(7.03),
        //round(1.66666,1),round(1.222,2),round(1.66666), sign(-5)

        //lots of other functions are described below
    }

    [Test]
    public void Types() { /*
         NFun has strict static type system
         Inputs and outputs can be of the following types:
         - int16
         - int32 (int)
         - int64
         - uin8 (byte)
         - uint16
         - uint32
         - uint64
         - real
         - bool
         - text
         - Arrays, like int[], real[], text[], etc
         - Functions, like fun(int, int):int
         - Structs, like {age:int, name:text}  
   
        You don't need to specify the types of variables. They are calculates automatically from the usage.*/
        var r1 = Funny.Hardcore.Build("y = 2*a+1");
        Assert.AreEqual(FunnyType.Int32, r1["a"].Type);
        Assert.AreEqual(FunnyType.Int32, r1["y"].Type);

        var r2 = Funny.Hardcore.Build("[1,2,3]");
        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.Int32), r2["out"].Type);

        var result = Funny.Calc("['1','2','Hi']");
        CollectionAssert.AreEqual(new[] { "1", "2", "Hi" }, result as string[]);

        // But if you really want to, you can specify the types explicitly
        var r3 = Funny.Hardcore.Build(
            @"
                a:real
                y = 2*a+1
            ");
        Assert.AreEqual(FunnyType.Real, r3["a"].Type);
        Assert.AreEqual(FunnyType.Real, r3["y"].Type);

        //Or
        var r4 = Funny.Hardcore.Build("y:real = 2*a+1");
        Assert.AreEqual(FunnyType.Real, r4["a"].Type);
        Assert.AreEqual(FunnyType.Real, r4["y"].Type);

        //the type of the input variable must be determined before using the variable
        Assert.Throws<FunnyParseException>(
            () => Funny.Hardcore.Build(
                @"
                    y = x+1
                    x: int # ERROR. type x is declared after its use
                "));
    }

    [Test]
    public void Numbers() { /*
        The usual, hexadecimal and binary forms
        of writing with bit separation are supported for writing numbers 

        y = 1      #1,  int
        y = 0xf    #15, int
        y = 0b101  #5, int
        y = 1.0    #1, real
        y = 1.51   #1.51, real
        y = 123_321 #123321 int
        y = 123_321_000 #123321000 int
        y = 12_32_1.1 #12321.1, real
        y = 0x123_321 #Something big, int
        
        Constant values are not always strictly related to the type
        The preferred types are shown above,
        y:byte = 1 
        y:uint16 = 0x1 
        y:byte = 1.0 # ERROR */

        Assert.AreEqual(1, Funny.Calc<byte>("1"));
        Assert.AreEqual(1000, Funny.Calc<UInt32>("1_000"));
        Assert.AreEqual(-1, Funny.Calc<long>("-0x1"));
        Assert.Throws<FunnyParseException>(() => Funny.Calc<long>("-1.0"));

        //By default, integer constants resolves as integer
        Assert.AreEqual(FunnyType.Int32, Funny.Hardcore.Build("1")["out"].Type);

        //but this behaviour can be changed with dialect settings
        //for example we can change it to "integer constants resolves as reals".
        var runtime = Funny.Hardcore
                           .WithDialect(Dialects.ModifyOrigin(integerPreferredType: IntegerPreferredType.Real))
                           .Build("1");
        Assert.AreEqual(FunnyType.Real, runtime["out"].Type);
        //But we are talking only about the PREFERRED type.
        //Integer constants can still be used as other types if necessary
        var runtime2 = Funny.Hardcore
                            .WithDialect(Dialects.ModifyOrigin(integerPreferredType: IntegerPreferredType.Real))
                            .Build("y:byte[] = [1,2,3]");
        Assert.AreEqual(FunnyType.ArrayOf(FunnyType.UInt8), runtime2["y"].Type);
    }

    [Test]
    public void BinaryOperations() { /*
         Binary constants: true, false
         
         Binary operators:
         ==, !=, >, <, >=, >=     
         not, and, or ,xor
         */

        Assert.True(Funny.Calc<bool>("0==0"));
        Assert.True(Funny.Calc<bool>("not 'a'=='b'"));
        Assert.True(Funny.Calc<bool>("'a' != 'b'"));
        Assert.True(Funny.Calc<bool>("not false"));
        Assert.True(Funny.Calc<bool>("12>1 and 1<2 and 1>=1 and 42.0<=43"));
        Assert.True(
            Funny.Calc<bool>(
                @"
                    x1= 12; x2 = 24; x3 =-1; x4 = false 
                    out = x1!=0 and (x2>0 or (x3<0 xor not x4))
                "));
    }

    [Test]
    public void IfOperator() { /*
            'if-else' is always an expression that returns a value. Every 'if' ends with 'else' branch
             you can write it in usual syntax:
             
             out = if (condition) result1
                else if(condition2) result2
                else result 3
             
             but 'else if' can be replaced with just 'if' keyword:   
             
             out = 
                if (condition) result1
                if (condition2) result2
                else result 3 */

        Assert.AreEqual(42, Funny.Calc("x = 42; out =if (x < 0 ) 0 else x"));

        //# Anonymous view and if are friends:
        Funny.Hardcore.Build(
            @"
                if (x>12) 'so big'
                if (x<3)  'so small'
                else      'it\'s ok!'
            ");

        //Margins are not important, but they may help in perception
        Assert.AreEqual(
            1, Funny.Calc(
                @"
                    x = 0; x2 = 12
                    out = if (x < 0) 
                            if (x ==0)
                                if (x2==300) 15
                                if (x2==400) 32
                                else 0
                            if (x ==1)
                                if (x2==500) 100
                                else 44
                            else 42
                        else 1
                "));
        //if can be used as usual expression 
        Assert.AreEqual(
            "42 is more than zero", Funny.Calc(
                @"
                    x = 42;
                    out = '{x} is '.concat(
                            if (x < 0) 'less than'
                            if (x > 0) 'more than'
                            else 'equal to')
                        .concat(' zero')
                "));

        //If-syntax is customizable. You may deny 'if expression' at all,
        //or allow only 'if - else if - else' style
        Assert.Throws<FunnyParseException>(
            () => Funny.Hardcore
                       .WithDialect(Dialects.ModifyOrigin(IfExpressionSetup.Deny))
                       .Build("if(true) 1 else 0"));

        Assert.Throws<FunnyParseException>(
            () => Funny.Hardcore
                       .WithDialect(Dialects.ModifyOrigin(IfExpressionSetup.IfElseIf))
                       .Build(
                           @"
                                if(true)  1 
                                if(false) 0 
                                else 0
                            "));
    }

    [Test]
    public void BitwiseOperations() { /*
         Bitwise operators
         |    # bit or
         &    # bit and
         ^    # bit xor
         ~    # bit inverse
         >>   # bitshift right
         <<   # bitshift left
        
        >>, << operators allowed for types int32, uint32, int64, uiny64
        |,&,^,~ operators allowed for any integer types*/

        Assert.IsTrue(Funny.Calc<bool>("x1 = 42; out = (x1 & (1 << 5)) !=0  #x1 has 5th bit"));
    }

    [Test]
    public void UserFunctions() {
        /*
         You can set your own function inside the script
         sum(x1,x2) = x1+ x2

         Argument types and the resulting type are also derived from of use:
         lessThan5(x) = x<5

         The argument types and the return type can be specified explicitly:
         tostring(x:int):text =
                if (x== 0) 'zero'
                if (x== 1) 'one'
                if (x== 2) 'two'
                else 'not supported'
         */

        //Example: calculation of the roots of the quadratic equation
        var runtime = Funny.Hardcore.Build(
            @"
                des(a, b, c) = b**2 - 4*a*c

                x1 = if (des(a,b,c) >=0)
                        (-b + des(a,b,c)**0.5)/(2*a)
                     else -1

                x2 = if (des(a,b,c) >0)
                        (-b - des(a,b,c)**0.5)/(2*a)
                     else -1
            ");
        runtime["a"].Value = 1;
        runtime["b"].Value = 10;
        runtime["c"].Value = 0;
        runtime.Run();
        Assert.AreEqual(0, runtime["x1"].Value);
        Assert.AreEqual(-10, runtime["x2"].Value);

        //User function cannot refer to a global input or output variable
        Assert.Throws<FunnyParseException>(
            () => Funny.Hardcore.Build(
                @"
                    f(x) = x + x2 # ERROR. The variable x2 is not an input for the function f

                    y = f(x1) + x2
                "));
    }

    [Test]
    public void Arrays() { /*
        # Initialization:
        y:int[] = [1,2,3,4]      # [1,2,3,4]  type: int[]
        y = ['a','b','foo']# ['a','b','foo'] type: text[]
        y = [1..4] 	   # [1,2,3,4]  type: int[]
        y = [1..7 step 2]      # [1,3,5,7]  type: int[]
        y = [1..2.5 step 0.5]  # [1.0,1.5,2.0,2.5]  type: real[]
        
        # In:
        y = 1 in [1,2,3,4]		# true
        y = 0 in [1..4] 		# false
        
        # Access:
        y = [0..10][0]  # 0
        y = [0..10][1]  # 1
        y = (x[5]+ x[4])/3
        
        # Slices:
        #[start:end:step]
        y = [0..10][1:3] #[1,2,3]
        y = [0..10][7:]  #[7,8,9,10]
        y = [0..10][:2]  #[0,1,2]
        y = [0..10][:]     #[0..10]
        y = [0..10][1:5:2] #[1,3,5]
        
        # Functions:
        y = [1,2,3,4].concat([3,4,5,6])     # [1,2,3,4,3,4,5,6]
        y = [1,2,3,4].intersect([3,4,5,6])  # [3,4]
        y = [1,2,3,4].except([3,4,5,6])     # [1,2]
        y = [1,2,3,4].unite([3,4,5,6])      # [1,2,3,4,5,6]
        y = [1,2,3,4].unique([3,4,5,6])     # [1,2,5,6]
        y = [1,100,4].find(100) # 1
        y = [1,2,3,4].take(2)   # [1,2]
        y = [1,2,3,4].skip(2)   # [3,4]
        y = [1,2,3,4].max()     # 4
        y = [1,2,3,4].min()     # 1
        y = [1,2,3,4].median()  # 2
        y = [1,2,3,4].avg()     # 2.5
        y = [1,2,3,4].sum()     # 10
        y = [1,2,3,4].count()   # 4
        y = [1,2,3,4].any()     # true
        y = [3,1,3,4].sort()    # [1,3,3,4]
        y = [1,2,3,4].reverse() # [4,3,2,1]
        
        y = [1..10].chunk(3)    # [[1,2,3],[4,5,6],[7,8,9],[10]]
        y = [[1, 2], [101, 102]].flat() # [1,2,101,102] 
        y = [].any() # false        
        y = 1.repeat(3) # [1,1,1]
         */
        CollectionAssert.AreEquivalent(
            new[] { 1, 3, 5 }, Funny.Calc<int[]>("[0..10][1:5:2]"));

        Assert.IsFalse(Funny.Calc<bool>("[].any()"));
        CollectionAssert.AreEquivalent(
            new[] { 1.0, 1.5, 2.0, 2.5 },
            Funny.Calc<double[]>("[1..2.5 step 0.5]"));
        CollectionAssert.AreEquivalent(
            new[] { "fo", "ba", "fo", "ba", "fo", "ba" },
            Funny.Calc<string[]>("['fo','ba'].repeat(3).flat()"));
    }

    [Test]
    public void Texts() {
        Assert.AreEqual("string constant", Funny.Calc("'string constant'"));
        Assert.AreEqual("hello world", Funny.Calc("'hello '.concat('world')"));
        Assert.AreEqual("hello world", Funny.Calc("'hello '.concat('world')"));
        //control characters \’ \” \t \n \r \f \{ \}
        Assert.AreEqual("name: 'vasa'", Funny.Calc("'name: \\'vasa\\''"));

        //Interpolation
        Assert.AreEqual("241*2= 482", Funny.Calc("'241*2= {241*2}'"));
        Assert.AreEqual(
            "21*2= 42, arr = [1,2,42]",
            Funny.Calc("x = 42; out = '21*2= {x}, arr = {[1,2,x]}'"));

        //text is an array of characters and all operations with arrays are applicable to texts
        Funny.Hardcore.Build("x:text; y = x.reverse()"); //will expand the input text
        Assert.AreEqual("ttt", Funny.Calc("'string constant'.filter(rule it == 't')"));

        //special text functions  
        //join
        Assert.AreEqual("one, two, three", Funny.Calc("['one', 'two', 'three'].join(', ')"));
        //split
        CollectionAssert.AreEquivalent(
            new[] { "1", "2", "3" },
            Funny.Calc<string[]>("'1,2,3'.split(',')"));
        //trim XXX
        Assert.AreEqual("hey", Funny.Calc("' hey '.trim()"));
        Assert.AreEqual("hey ", Funny.Calc("' hey '.trimStart()"));
        Assert.AreEqual(" hey", Funny.Calc("' hey '.trimEnd()"));
        //toUpper
        Assert.AreEqual("HEY", Funny.Calc("'hey'.toUpper()"));
        //toLower
        Assert.AreEqual("hey", Funny.Calc("'Hey'.toLower()"));
    }

    [Test]
    public void LinqAndRules() {
        /*
         # Sometimes you need to select elements from the array that satisfy a certain condition. To do this, use the filter function

            [1,2,3,4,5].filter(rule it>3) #[4,5]
            
            filter-rule is a condition followed by “rule”-keyword.
            Inside the rule, ‘it’ is the element for which the rule is applied
            
            # Other similar functions:
            [1,2,3,4,5].count(rule it>3) # number of elements satisfying the rule
            [1,2,3,4,5].any (rule it>3) # does at least one element satisfy the rule
            [1,2,3,4,5].all(rule it>3) # does all elements satisfy the rule
            
            # In the map(rule) function, rules play a more interesting role.
            # this function converts each element of the array according to the specified rule
            [1,2,3,4,5].map(rule it*2) #multiplying each element by 2 [2,4,6,8,10]
            
            # Another function is count(rule), which counts the number of elements that satisfy the rule:
            [1,2,3,4,5].count(rule it>3) #2
            
            # sum - allows you to get sum of element with projection
            [1,2,3].sum(rule it*it) #14 
         */

        Assert.AreEqual(
            64, Funny.Calc<double>(
                @"
                    [1,2,3,4]
                        .filter(rule it>2)
                        .map(rule it**3)
                        .max()
                "));

        Assert.AreEqual(1, Funny.Calc("[5,1,2].sort(rule it)[0]"));
    }

    [Test]
    public void Structs() {
        // NFun supports structures
        var runtime = Funny.Hardcore.Build(
            @"
                # initialization
                user = {
                    age = 12, 
                    name = 'Kate'
                    cars = [
                        #single-line initialization
                        { name = 'Creta',   id = 112, power = 140, price = 5000},
                        { name = 'Camaro', id = 113, power = 353, price = 10000} 
                    ]
                }
                # field access
                userName = user.name
                # linq operations
                slowestCar = user.cars.sort(rule it.power)[0].name
                totalCost = user.cars.sum(rule it.price)
            ");
        runtime.Run();
        Assert.AreEqual("Creta", runtime["slowestCar"].Value);
        Assert.AreEqual(15000, runtime["totalCost"].Value);
    }
}

}