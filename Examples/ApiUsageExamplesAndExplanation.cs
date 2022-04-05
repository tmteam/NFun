using System;
using NFun.Exceptions;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace NFun.Examples {
// Here are examples of using the API. See SyntaxExamplesAndExplanation.cs
// for a better understanding of syntax
//
// Each test in this file is a separate tutorial. Study them sequentially, starting from the first
public class ApiUsageExamples {
    [Test]
    // Calculation of constants 
    public void ConstantCalculation() {
        object a = Funny.Calc("'Hello world'");
        Assert.AreEqual("Hello world", a);

        //Let's calculate something
        object b = Funny.Calc("5*20+10");
        Assert.AreEqual(110, b);

        //You can specify the output type
        int c = Funny.Calc<int>("10*(5+15)");
        Assert.AreEqual(200, c);

        //The type inference algorithm can calculate an expression
        //for different types on same expression.
        double d = Funny.Calc<double>("10*(5+15)");
        Assert.AreEqual(200.0, d);

        //We can do this not only with numbers
        //Here we do it with strings:
        string s = Funny.Calc<string>("'what\\'s up'.reverse()");
        Assert.AreEqual("pu s'tahw", s);
        //With arrays:
        int[] arr1 = Funny.Calc<int[]>("[1,2,3].reverse()");
        CollectionAssert.AreEquivalent(new[] { 3, 2, 1 }, arr1);
        //With arrays of strings:
        string[] arr2 = Funny.Calc<string[]>("[1,2,3].map(rule 'item {it}')");
        CollectionAssert.AreEquivalent(new[] { "item 1", "item 2", "item 3" }, arr2);
        //And even complex models
        User user = Funny.Calc<User>("{id = 112, age = 42, name = 'peter'.toUpper(), cars = []}");
        Assert.AreEqual(112, user.Id);
        Assert.AreEqual(42, user.Age);
        Assert.AreEqual("PETER", user.Name);
    }
    [Test]
    // Single value calculation 
    public void CalculationWithInputData() {
        // let's assume that we have a complex user model
        var user = new User {
            Age = 42, Id = 112, Name = "Alice",
            Cars = new[] {
                new Car { Power = 140, Id = 321, Price = 5000 },
                new Car { Power = 315, Id = 322, Price = 7200 }
            }
        };
        //take the user's model as an input.
        //Here we сalculate the value based on the values of the 'Age' and 'Id' properties from the input
        //To do this, just use the property names of the input model in expression
        object isAdult = Funny.Calc("age>=18 and id==112", user);
        Assert.AreEqual(true, isAdult);

        //You can specify the output type
        bool isAdultTyped = Funny.Calc<User, bool>("age>=18 and id==112", user);
        Assert.AreEqual(true, isAdultTyped);
        
        //You can access not only the primitive properties of the original model
        int ownCarsCount = Funny.Calc<User, int>("if(age>=18) cars.count() else -1", user);
        Assert.AreEqual(2, ownCarsCount);

        //and do complex calculations
        //Let's calculate the total cost of the user's cars if he is an adult
        long totalPrice = Funny.Calc<User, long>(
            "if(age>=18) cars.sum(rule it.price)" +
            "else 0", user);
        Assert.AreEqual(12200, totalPrice);

        //Let's calculate the total cost of powerful cars 
        //following expression equals to C#:
        //user.Cars.Where(c=>c.Power > 300).Sum(c=>c.Price);
        long powerfullCarsTotalPrice = Funny.Calc<User, long>("cars.filter(rule it.power>300).sum(rule it.price)",
            user);
        Assert.AreEqual(7200, powerfullCarsTotalPrice);
    }
    
    // Multiple values calculation
    [Test]
    public void MultipleCalculations() {
        var user = GetUserModel();
        //you can calculate several expressions at a time
        
        //To do this, you need to use the output model.
        //The nfun script will set values to the open properties of the output model
        //In this example, the model 'outputs' has the properties 'Adult', 'Price', 'NameAndId'
        Outputs result = Funny.CalcMany<User, Outputs>(
            @"Adult = age>18
              Price = cars.sum(rule it.price)", user);
        Assert.AreEqual(true, result.Adult);
        Assert.AreEqual(12200, result.Price);
    }
    /*
     * ********** Fluent API ********** 
     *
     * The above examples uses default settings of nfun. 
     * Fluent Api allows you to override them, like
     * - add custom functions
     * - add custom constants
     * - override default syntax
     * - override default semantics
    */
    [Test]
    // FluentApi. Add constants and functions 
    public void FluentApi_CustomConstantsAndFunctions() {
        //let's add custom constant to script
        var result = Funny
                     .WithConstant("foo", 42)
                     .Calc<int>("foo+1");
        Assert.AreEqual(result, 43);

        // You can connect several WithConstant methods into chain,
        // Every WithConstant call returns 'builder' object
        var builder = Funny.WithConstant("foo", 42).WithConstant("bar", 100);
        var result2 = builder.Calc<int>("foo + bar");
        Assert.AreEqual(result2, 142);

        //You can do the same with custom function

        //assume we have custom function (method or Func<...>)
        Func<int, int, int> myFunctionMin = (i1, i2) => Math.Min(i1, i2);
        
        object a = Funny
                   .WithConstant("foo", 42)
                   .WithFunction("myMin", myFunctionMin)
                   // now you can use 'myMin' function and 'foo' constant in script!
                   .Calc("myMin(foo,123) == foo");
        Assert.AreEqual(true, a);
    }

    [Test]
    // FluentApi. Syntax and semantic customization 
    public void FluentApi_Dialects() {
        //Also you may override fun-dialect for your needs

        //Let's deny if expression at all! 
        var builder = Funny.WithDialect(IfExpressionSetup.Deny);

        //now you cannot launch script with such an expression
        Assert.Throws<FunnyParseException>(() => builder.Calc("if(2>1) true else false"));

        //Default type of numbers '1' and '2' - is Integer
        // let's make them Real!
        object sumResult = Funny
                           .WithDialect(integerPreferredType: IntegerPreferredType.Real)
                           .Calc("1 + 2");
        Assert.IsInstanceOf<double>(sumResult); //now preferred type of INTEGER constants is REAL


        //lets turn all 'real' arithmetics to decimal
        object decimalResult = Funny
                               .WithDialect(realClrType: RealClrType.IsDecimal)
                               .Calc("0.2 + 0.3");
        Assert.AreEqual(new decimal(0.5), decimalResult);


        //now lets allow integer "overflow" operations
        var uintResult = Funny
                         .WithDialect(integerOverflow: IntegerOverflow.Unchecked)
                         .Calc<uint>("0xFFFF_FFFF + 1");
        Assert.AreEqual((uint)0, uintResult);
    }

    [Test]
    // FluentApi. Configuring script execution 
    public void FluentApi_Build() {
        //BuildForCalcXXX methods allow you to get a customized calculator object for further use.
        //This object is immutable and can be reused, which greatly speeds up the interpretation of expressions
        var calculator = Funny
                         .WithConstant("foo", 42)
                         .BuildForCalc<User, long>();

        var user = GetUserModel();

        var res1 = calculator.Calc("foo + age - id", user);
        var res2 = calculator.Calc("foo + 2* age", user);
        //it is also possible to build a lambda from a calculator
        var lambda = calculator.ToLambda("foo + age - id");
        //and calculate it later
        var res3 = lambda(user);

        //You can build different calculators for different input output types combination using following methods:
        //.BuildForCalc<TInput>()
        //.BuildForCalc<TInput,TOutput>()
        //.BuildForCalcConstant()
        //.BuildForCalcConstant<TOutput>()
        //.BuildForCalcMany<TInput, TOutput>()
        //.BuildForCalcManyConstants<TOutput>()
    }
    
    
    /*
    * ********** Hardcore API ********** 
    *
    * Use HardcoreApi to access all low level features 
    * It allows you to work directly with variables, internal values, and the type inference algorithm
    */
    
    [Test]
    // HardcoreApi. Getting information about variables
    public void HardcoreApi_variableInformation() {
        var runtime = Funny.Hardcore.Build(
            "y:real= a-b; " +
            "out= 2*y/(a+b)"
        );
        //The runtime object contains all the information about the script.
        //Types, names of variables, functions, etc
        Assert.AreEqual(4, runtime.Variables.Count);
        //Here we look at all the information about the "out" variable    
        var outVar = runtime["out"];
        Assert.AreEqual("out", outVar.Name);
        Assert.AreEqual(FunnyType.Real, outVar.Type);
        Assert.AreEqual(0, outVar.Value);
        Assert.AreEqual(true, outVar.IsOutput);
    }
    
    [Test]
    // HardcoreApi. Run arbitrary script
    public void HardcoreApi_RunScript() {
        var runtime = Funny.Hardcore.Build(
            "y:real= a-b; " +
            "out= 2*y/(a+b)"
        );
        // Set inputs
        runtime["a"].Value = 30;
        runtime["b"].Value = 20;
        // Run script
        runtime.Run();
        // Get outputs
        Assert.AreEqual(0.4, runtime["out"].Value);
    }
    
    [Test]
    // HardcoreApi. Run arbitrary script many times
    public void HardcoreApi_RunScriptManytimes() {
        var runtime = Funny.Hardcore.Build(
            "y:real= a-b; " +
            "out= 2*y/(a+b)"
        );
        // Runtime object can be ran many times, but it is not thread-safe
        IFunnyVar avar   = runtime["a"];
        IFunnyVar bvar   = runtime["b"];
        IFunnyVar yvar   = runtime["y"];
        IFunnyVar outvar = runtime["out"];

        for (double i = 0; i < 100; i++)
        {
            //set input values
            avar.Value = i;
            bvar.Value = 42.0;
            // run script
            runtime.Run();
            // get output values
            object y = yvar.Value;
            object o = outvar.Value;
        }
    }
    // HardcoreApi. Optimization for multiple script runs
    [Test]
    public void HardcoreApi_RunScriptOptimizations() {
        var runtime = Funny.Hardcore.Build(
            "y:real= a-b; " +
            "out= 2*y/(a+b)"
        );
        //In this example, we are speeding up the re-launch of the script
        
        // Accessing the Value property, from previous example is a quite expensive operation.
        // In case of reuse, it is better to create and use access methods

        //Create methods for setting values 
        Action<double> aSetter = runtime["a"].CreateSetterOf<double>();
        Action<double> bSetter = runtime["b"].CreateSetterOf<double>();
        //Create methods for getting values
        Func<double> yGetter = runtime["y"].CreateGetterOf<double>();
        Func<double> outGetter = runtime["out"].CreateGetterOf<double>();

        for (var i = 0; i < 100; i++)
        {
            //set values to input variables
            aSetter(i);
            bSetter(42.0);

            runtime.Run();

            //access values from output variables
            double y = yGetter();
            double o = outGetter();
        }
    }
    // HardcoreApi. Fine tuning
    [Test]
    public void HardcoreApi_ConfigurateRuntime() {
        // You can pre-configure the runtime builder
        User user = GetUserModel();
        FunnyRuntime rt = Funny.Hardcore
                               .WithConstant("foo", 42)
                               .WithConstant("user", user)
                               .WithFunction<User, bool>("isAdult", user => user.Age >= 18)
                               .WithApriori<double>("x") //variable 'x' has to be type of REAL
                               .Build("adult = user.isAdult(); ans = if(adult) x else 100");

        rt["x"].Value = 42.0;
        rt.Run();
        //user.isAdult() - preset function is called, with a preset constant
        //so 'adult' should be equal 'true'
        //if(adult) x else 100 - 'x' has type of 'real', so ans type also should be 'real'    
        Assert.AreEqual(true, rt["adult"].Value);
        Assert.IsInstanceOf<double>(rt["ans"].Value);
        Assert.AreEqual(42.0, rt["ans"].Value);
    }
    
    //StringTemplate. if you need only to calculate a string
    [Test]
    public void StringTemplate() {
        // String template build is similar to hardcore build, but returns a single string result
        // There is no need to use the quotation mark symbol around the expression,
        // and there is no need to escape the quotation marks inside
        var inputString = "Name: '{name}', Age: {2020 - birthYear}, 2*2={2*2}";

        var calculator = Funny.Hardcore.BuildStringTemplate(inputString);
        calculator["name"].Value = "Kate";
        calculator["birthYear"].Value = 1990;
        var resultString = calculator.Calculate();
        Assert.AreEqual(resultString, "Name: 'Kate', Age: 30, 2*2=4");
    }

    [Test]
    public void ErrorsHandling() {
        //There are three exceptions that are thrown by nfun:

        // FunnyParseException - Parsing errors (incorrect syntax) 
        Assert.Throws<FunnyParseException>(() => Funny.Calc("-abc-"));
        Assert.Throws<FunnyParseException>(
            () => Funny
                  .WithConstant("foo", 42)
                  .Calc<User, Outputs>("-abc-", GetUserModel()));
        Assert.Throws<FunnyParseException>(() => Funny.Hardcore.Build("-abc-"));

        // FunnyRuntimeException - Execution errors
        Assert.Throws<FunnyRuntimeException>(() => Funny.Calc("[1,2,3][4]"));
        Assert.Throws<FunnyRuntimeException>(() => Funny.Hardcore.Build("[1,2,3][4]").Run());

        //FunnyInvalidUsageException - Api usage errors: FunnyInvalidUsageException
        Assert.Throws<FunnyInvalidUsageException>(
            () => Funny.Calc<User, ModelWithoutParameterlessCtor>("age+1", GetUserModel()));
    }

    /// <summary>
    /// Create sample user model
    /// </summary>
    private static User GetUserModel() =>
        new User {
            Age = 42, Id = 112, Name = "Alice",
            Cars = new[] {
                new Car { Power = 140, Id = 321, Price = 5000 },
                new Car { Power = 315, Id = 322, Price = 7200 }
            }
        };
}

class ModelWithoutParameterlessCtor {
    public ModelWithoutParameterlessCtor(int i) { }
}

class Outputs {
    public bool Adult { get; set; }
    public double Price { get; set; }
    public string NameAndId { get; set; }
}

class User {
    public int Age { get; set; }
    public string Name { get; set; }
    public long Id { get; set; }
    public Car[] Cars { get; set; }
}

class Car {
    public long Id { get; set; }
    public double Power { get; set; }
    public long Price { get; set; }
}

}