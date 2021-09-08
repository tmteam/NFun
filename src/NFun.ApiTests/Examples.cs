using System;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests {

public class Examples {
    [Test]
    public void ConstantCalculation() {
        // Examples of constant calculations
        object a = Funny.Calc("'Hello world'");
        Assert.AreEqual("Hello world", a);

        //Now let's calculate something
        object b = Funny.Calc("12*24+11");
        Assert.AreEqual(299, b);

        //You can specify the output type
        int c = Funny.Calc<int>("12*24+11");
        Assert.AreEqual(299, c);

        //The type inference algorithm can calculate an expression
        //for different types.
        double d = Funny.Calc<double>("12*24+11");
        Assert.AreEqual(299.0, d);

        //We can do this not only with numbers
        string s = Funny.Calc<string>("'what\\'s up'.reverse()");
        Assert.AreEqual("pu s'tahw", s);

        string[] arr = Funny.Calc<string[]>("[1,2,3].map(rule 'item {it}')");
        CollectionAssert.AreEquivalent(new[] { "item 1", "item 2", "item 3" }, arr);

        User user = Funny.Calc<User>("{id = 112, age = 42, name = 'peter'.toUpper(), cars = []}");
        Assert.AreEqual(112, user.Id);
        Assert.AreEqual(42, user.Age);
        Assert.AreEqual("PETER", user.Name);
    }

    [Test]
    public void CalculationWithInputData() {
        var user = new User {
            Age = 42, Id = 112, Name = "Alice",
            Cars = new[] {
                new Car { Power = 140, Id = 321, Price = 5000 },
                new Car { Power = 315, Id = 322, Price = 7200 }
            }
        };
        //Let's take the user's model as an input, and calculate something
        object isAdult = Funny.Calc("age>=18", user);
        Assert.AreEqual(true, isAdult);

        //You can specify the output type
        int ownCarsCount = Funny.Calc<User, int>("if(age>=18) cars.count() else -1", user);
        Assert.AreEqual(2, ownCarsCount);

        //and do complex calculations
        //Let's calculate the total cost of the user's cars if he is an adult
        long totalPrice = Funny.Calc<User, long>(
            "if(age>=18) " +
            "  cars.sum(rule it.price) " +
            "else " +
            "  0", user);
        Assert.AreEqual(12200, totalPrice);

        //Let's calculate the total cost of machines powerful machines of the user
        long powerfullCarsTotalPrice = Funny.Calc<User, long>(
            "cars.filter(rule it.power>300).sum(rule it.price)",
            user);
        Assert.AreEqual(7200, powerfullCarsTotalPrice);
    }

    [Test]
    public void MultipleCalculations() {
        var user = new User {
            Age = 42, Id = 112, Name = "Alice",
            Cars = new[] {
                new Car { Power = 140, Id = 321, Price = 5000 },
                new Car { Power = 315, Id = 322, Price = 7200 }
            }
        };
        //you can calculate several expressions at a time
        Outputs result = Funny.CalcMany<User, Outputs>(
            @"Adult = age>18
              Price = cars.sum(rule it.price)
              NameAndId = 'id: {id}, name: {name.toUpper()}'", user);
        Assert.AreEqual(true, result.Adult);
        Assert.AreEqual(12200, result.Price);
        Assert.AreEqual("id: 112, name: ALICE", result.NameAndId);
    }

    [Test]
    public void FluentApi() {
        //The above examples are suitable just for basic cases
        //Use the Fluent Api for more complex scenarios.
        //It allows you to add functions, constants, override default behaviors and
        //increase performance

        //You may add custom constants and custom functions
        var a = Funny
                .WithConstant("lala", 42)
                .WithConstant("booboo", 2)
                .WithFunction<int, int, int, int>(
                    "minOf3",
                    (i1, i2, i3) => Math.Min(Math.Min(i1, i2), i3))
                .Calc("minOf3(lala,booboo,123) == booboo");
        Assert.AreEqual(true, a);

        //Also you may override fun-dialiect for your needs
        var sumResult = Funny.WithDialect(
                                 Dialects.ModifyOrigin(
                                     integerPreferredType: IntegerPreferredType.Real))
                             .Calc("1 + 2");
        Assert.IsInstanceOf<double>(sumResult);


        //BuildForCalcXXX methods allow you to get a customized calculator object for further use.
        //This object is immutable and can be reused, which greatly speeds up the interpretation of expressions
        var calculator = Funny
                         .WithConstant("lala", 42)
                         .BuildForCalc<User, long>();

        var user = GetDefaultUser();

        var res1 = calculator.Calc("lala + age - id", user);
        var res2 = calculator.Calc("lala + 2* age", user);
        //it is also possible to build a lambda from a calculator
        var lambda = calculator.ToLambda("lala + age - id");
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

    private static User GetDefaultUser() {
        var user = new User {
            Age = 42, Id = 112, Name = "Alice",
            Cars = new[] {
                new Car { Power = 140, Id = 321, Price = 5000 },
                new Car { Power = 315, Id = 322, Price = 7200 }
            }
        };
        return user;
    }

    [Test]
    public void HardcoreApi() {
        //Use HardcoreApi to access all the features of nfun, 
        //It allows you to work directly with variables, internal values, and the type inference algorithm

        var runtime = Funny.Hardcore.Build("y:real= a-b; out= 2*y/(a+b)");

        //The runtime object contains all the information about the script. Types, names of variables, functions, etc
        Assert.AreEqual(4, runtime.Variables.Count);

        var outVar = runtime["out"];
        Assert.AreEqual("out", outVar.Name);
        Assert.AreEqual(FunnyType.Real, outVar.Type);
        Assert.AreEqual(0, outVar.Value);
        Assert.AreEqual(true, outVar.IsOutput);

        // Input variable values can and should be set manually
        runtime["a"].Value = 30;
        runtime["b"].Value = 20;
        runtime.Run();
        Assert.AreEqual(0.4, runtime["out"].Value);

        //runtime object can be run many times, but it is not thread-safe
        var avar = runtime["a"];
        var bvar = runtime["b"];
        var yvar = runtime["y"];
        var outvar = runtime["out"];

        for (double i = 0; i < 100; i++)
        {
            //Setup input values
            avar.Value = i;
            bvar.Value = 42.0;
            //Run script
            runtime.Run();
            //Get current values
            object y = yvar.Value;
            object o = outvar.Value;
        }

        //Accessing the Value property is an expensive operation.
        //In case of reuse, it is better to use access methods
        Action<double> aSetter = runtime["a"].GetTypedSetter<double>();
        Action<double> bSetter = runtime["b"].GetTypedSetter<double>();
        Func<double> yGetter = runtime["y"].GetTypedGetter<double>();
        Func<double> outGetter = runtime["out"].GetTypedGetter<double>();

        for (var i = 0; i < 100; i++)
        {
            aSetter(i);
            bSetter(42.0);
            //Run script
            runtime.Run();
            //Get result values
            double y = yGetter();
            double o = outGetter();
        }

        //You can also pre-configure the runtime builder
        User user = GetDefaultUser();
        FunnyRuntime rt = Funny.Hardcore
                               .WithConstant("foo", 42)
                               .WithConstant("user", user)
                               .WithFunction<User, bool>("isAdult", user => user.Age >= 18)
                               .WithApriori<double>("x") //variable 'x' should be type of real
                               .Build("adult = user.isAdult(); ans = if(adult) x else 100");

        rt["x"].Value = 42.0;
        rt.Run();
        //user.isAdult() #here a preset function is called, with a preset constant
        //so 'adult' should be equal 'true'
        //if(adult) x else 100 # here 'x' has type of 'real', so ans type aslo should be 'real'    
        Assert.AreEqual(true, rt["adult"].Value);
        Assert.IsInstanceOf<double>(rt["ans"].Value);
        Assert.AreEqual(42.0, rt["ans"].Value);
    }
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