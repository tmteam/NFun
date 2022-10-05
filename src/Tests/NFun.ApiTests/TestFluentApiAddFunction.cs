using System;
using NFun.Interpretation.Functions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests; 

public class TestFluentApiAddFunctionTest {
    [Test]
    public void SingleVariableFunction() {
        var calculator = Funny
                         .WithFunction("myHello", (int i) => $"Hello mr #{i}")
                         .WithFunction("myInc", (int i) => i + 1)
                         .BuildForCalc<ModelWithInt, string>();

        Func<ModelWithInt, string> lambda = calculator.ToLambda("out = myHello(myInc(id))");

        var result = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result, "Hello mr #43");

        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "Hello mr #2");
    }
    
    [Test]
    public void SingleVariableFunctionConstructionEarlyConstruction() {
        var calculator = Funny
                         .WithFunction("myDec", (int i) => new decimal(i))
                         .WithDialect(realClrType: RealClrType.IsDecimal)
                         .BuildForCalc<ModelWithInt, decimal>();

        Func<ModelWithInt, decimal> lambda = calculator.ToLambda("out = myDec(id)*2");

        var result = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result, new decimal(84));
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, new decimal(2));
    }
    
    [Test]
    public void SingleVariableFunctionConstructionLateConstruction() {
        var calculator = Funny
                         .WithDialect(realClrType: RealClrType.IsDecimal)
                         .WithFunction("myDec", (int i) => new decimal(i))
                         .BuildForCalc<ModelWithInt, decimal>();

        Func<ModelWithInt, decimal> lambda = calculator.ToLambda("out = myDec(id)*2");

        var result = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result, new decimal(84));
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, new decimal(2));
    }

    [Test]
    public void SingleVariableFunctionEarlyConstruction_2() {
        var calculator = Funny
                         .WithFunction("myDec", (Decimal i) => (int)Math.Round(i))
                         .WithDialect(realClrType: RealClrType.IsDecimal)
                         .BuildForCalc<ModelWithInt, int>();

        Func<ModelWithInt, int> lambda = calculator.ToLambda("out = myDec(42.5*id)-2");

        var result = lambda(new ModelWithInt { id = 2 });
        Assert.AreEqual(result, 83);
    }
    
    [Test]
    public void SingleVariableFunctionLateConstruction_2() {
        var calculator = Funny
                         .WithDialect(realClrType: RealClrType.IsDecimal)
                         .WithFunction("myDec", (Decimal i) => (int)Math.Round(i))
                         .BuildForCalc<ModelWithInt, int>();

        Func<ModelWithInt, int> lambda = calculator.ToLambda("out = myDec(42.5*id)-2");

        var result = lambda(new ModelWithInt { id = 2 });
        Assert.AreEqual(result, 83);
    }

    [Test]
    public void TwoVariablesFunction() {
        var calculator = Funny
                         .WithFunction("totxt", (int i1, int i2) => $"{i1},{i2}")
                         .BuildForCalc<ModelWithInt, string>();

        var lambda = calculator.ToLambda("totxt(id,1)");

        var result1 = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result1, "42,1");
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "1,1");
    }

    [Test]
    public void ThreeVariablesFunction() {
        var calculator = Funny
                         .WithFunction(
                             "totxt",
                             (int i1, int i2, int i3) => $"{i1},{i2},{i3}")
                         .BuildForCalc<ModelWithInt, string>();

        var lambda = calculator.ToLambda("totxt(id,2,3)");

        var result1 = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result1, "42,2,3");
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "1,2,3");
    }

    [Test]
    public void FourVariablesFunction() {
        var calculator = Funny
                         .WithFunction(
                             "totxt",
                             (int i1, int i2, int i3, int i4) => $"{i1},{i2},{i3},{i4}")
                         .BuildForCalc<ModelWithInt, string>();

        var lambda = calculator.ToLambda("totxt(id,2,3,4)");

        var result1 = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result1, "42,2,3,4");
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "1,2,3,4");
    }

    [Test]
    public void FiveVariablesFunction() {
        var calculator = Funny
                         .WithFunction(
                             "totxt",
                             (int i1, int i2, int i3, int i4, int i5) => $"{i1},{i2},{i3},{i4},{i5}")
                         .BuildForCalc<ModelWithInt, string>();

        var lambda = calculator.ToLambda("totxt(id,2,3,4,5)");

        var result1 = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result1, "42,2,3,4,5");
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "1,2,3,4,5");
    }

    [Test]
    public void SixVariablesFunction() {
        var calculator = Funny
                         .WithFunction(
                             "totxt",
                             (int i1, int i2, int i3, int i4, int i5, int i6) => $"{i1},{i2},{i3},{i4},{i5},{i6}")
                         .BuildForCalc<ModelWithInt, string>();

        var lambda = calculator.ToLambda("totxt(id,2,3,4,5,6)");

        var result1 = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result1, "42,2,3,4,5,6");
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "1,2,3,4,5,6");
    }

    [Test]
    public void SevenVariablesFunction() {
        var calculator = Funny
                         .WithFunction(
                             "totxt",
                             (int i1, int i2, int i3, int i4, int i5, int i6, int i7) =>
                                 $"{i1},{i2},{i3},{i4},{i5},{i6},{i7}")
                         .BuildForCalc<ModelWithInt, string>();

        var lambda = calculator.ToLambda("totxt(id,2,3,4,5,6,7)");

        var result1 = lambda(new ModelWithInt { id = 42 });
        Assert.AreEqual(result1, "42,2,3,4,5,6,7");
        var result2 = lambda(new ModelWithInt { id = 1 });
        Assert.AreEqual(result2, "1,2,3,4,5,6,7");
    }

    [Test]
    public void CompositeAccess() {
        var calculator = Funny
                         .WithFunction("myHello", (int i) => $"Hello mr #{i}")
                         .WithFunction("csumm", (ComplexModel m) => m.a.id + m.b.id)
                         .BuildForCalc<ModelWithInt, int>();

        var lambda = calculator.ToLambda(
            @"csumm(
                            {
                                a= {id= 10}
                                b= {id= 20}
                            })");

        var result = lambda(new ModelWithInt { id = 42 });

        Assert.AreEqual(result, 30);
    }
}