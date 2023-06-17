namespace NFun.SyntaxTests.Structs;

using NUnit.Framework;
using TestTools;
using Types;

public class StructLcaTest {

    [Test]
    public void FunLca1() =>
        @"
            f1 = rule it.age + it.size
            f2 = rule it.age

            f3 = if(true) f1 else f2

            out = f3({age = 42, size = 15})
        ".Calc().AssertReturns("out", 57);

    [Test]
    public void FunLca2() =>
        @"
            f1 = rule it.age + it.size
            f2 = rule it.age

            f3 = if(true) f1 else f2

            out:int = f3({age = 42, size = 15})
        ".Calc().AssertReturns("out", 57);

    [Test]
    public void FunLca3() =>
        @"
        f1 = rule it.size
        f2 = rule it.age
        f3 = if(true) f1 else f2
        out = f3({age = 42, size = 15})
    ".Calc().AssertReturns("out", 15);

    [Test]
    public void FunLca4() =>
        "(if(true) rule it.size else rule it.age)({age = 42, size = 15})"
            .Calc().AssertReturns("out", 15);

    [Test]
    public void FunLca5() =>
        "out:int =(if(true) rule it.age + it.size else rule it.age)({age = 42, size = 15})"
            .Calc().AssertReturns("out", 57);

    [Test]
    public void FunLca6() =>
        @"
        f1 = rule it.size
        f2 = rule it.age
        f3 = if(true) f1 else if(false) f2 else rule it.size+ it.age
        out = f3({age = 42, size = 15})
    ".Calc().AssertReturns("out", 15);

    [Test]
    public void IfLca1() =>
        @"
            x = if(true)
            {
	            age = 42
	            name = 'vasa'
	            size = 100
            }
            else
            {
	            age = 42
            }
            out = x.age
        ".Calc().AssertReturns("out", 42);

    [Test]
    public void IfLca2() =>
        @"
        x =
	        if(true) { age = 0x1 }
	        else { age = 42.0 }
         out = x.age
    ".Calc().AssertReturns("out", 1.0);

    [Test]
    public void IfLca3() =>
        @"
    x =
	    if(true) { age = 0x1 }
	    else if(false) { age = 'name' }
	    else { age = 42.0 }

    out = x.age
    ".Calc().AssertResultIs("out", typeof(object));

    [Test]
    public void IfLca4() {
        var expr = @"
        x =
	        if(true) { age = in1 }
	        else if(false) { age = in2 }
	        else { age = in3 }

        out:real = x.age #ok. :in1 == :in2 == :in3 == :real";
        var runtime = Funny.Hardcore.Build(expr);
        Assert.AreEqual(FunnyType.Real, runtime["in1"].Type);
        Assert.AreEqual(FunnyType.Real, runtime["in2"].Type);
        Assert.AreEqual(FunnyType.Real, runtime["in3"].Type);
        runtime["in1"].Value = 41.0;
        runtime["in2"].Value = 42.0;
        runtime["in3"].Value = 43.0;
        runtime.Calc();
        Assert.AreEqual(runtime["out"].Value, 41.0);
    }

    [Test]
    public void ArrayLca1() =>
        @"
        arr = [{age=42}, {age = 42, size = 15}, {age = 1, size = 2, name = 'vasa'}]
        out = arr[0].age"
            .Calc().AssertReturns(42);

    [Test]
    public void ArrayLca2() =>
        @"
        arr = [{age=42}, {age = 42, size = 15}]
        out:real = arr[0].age"
            .Calc().AssertReturns(42.0);

    [Test]
    public void Fcd1() {
        var expr = @"
            fun1(x,y) = x.age + y.size
            out = fun1(a,a)
        ";
        var hr = Funny.Hardcore.Build(expr);
        var type = hr["a"].Type;
        Assert.AreEqual(BaseFunnyType.Struct, type.BaseType);
        Assert.AreEqual(2, type.StructTypeSpecification);
        Assert.AreEqual(BaseFunnyType.Real,  type.StructTypeSpecification["age"]);
        Assert.AreEqual(BaseFunnyType.Real,  type.StructTypeSpecification["size"]);
    }

    [Test]
    public void Fcd2() =>
        @"
            fun1(x,y) = x.age + y.size
            a = {age = 42, size = 54, name = 'kate'}
            out = fun1(a,a)
        ".Calc().AssertResultHas("out", 96);

    [TestCase(
        @"
            x = if(true)
            {
	            age = 42
	            name = 'vasa'
	            size = m
            }
            else
            {
	            age = 42
            }
            out = x.name")]
    [TestCase(
        @"
            f1 = rule it.age + it.size
            f2 = rule it.age

            f3 = if(true) f1 else f2
            out = f3({age = 42}) #error")]
    [TestCase(
        @"
            f1 = rule it.size
            f2 = rule it.age
            f3 = if(true) f1 else f2
            out = f3({age = 42})")]
    [TestCase(
 @"
    arr = [{age=42}, {age = 42, size = 15}]
    out:real = arr[0].size")]
    [TestCase("y = [{a =1.0},{id = 31},{id = 42}][0]")]
    public void ObviousFails(string expr) =>
        expr.AssertObviousFailsOnParse();
}
