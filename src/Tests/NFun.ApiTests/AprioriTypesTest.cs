using System;
using NFun.Interpretation.Functions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests {

public class AprioriTypesTest {
    [Test]
    public void AprioriInputOfStringSpecified_CalcsWithCorrectType() {
        var runtime = Funny.Hardcore
                           .WithApriori<string>("x")
                           .Build("y = x");

        var res = runtime.Calc("x", "test");
        res.AssertReturns("y", "test");
        Assert.AreEqual(FunnyType.Text, runtime["x"].Type);
        Assert.AreEqual(FunnyType.Text, runtime["y"].Type);
    }

    [Test]
    public void AprioriInputOfIntSpecified_inputTypeIsCorrect() {
        var runtime = Funny.Hardcore
                           .WithApriori<int>("x")
                           .Build("y:int64 = x+1");
        Assert.AreEqual(FunnyType.Int32, runtime["x"].Type);
    }

    [Test]
    public void AprioriInputOfDecimalLateSpecified_inputTypeIsCorrect() {
        var runtime = Funny.Hardcore
                           .WithDialect(realClrType: RealClrType.IsDecimal)
                           .WithApriori<decimal>("x")
                           .Build("y = x+1");
        Assert.AreEqual(FunnyType.Real, runtime["x"].Type);
    }

    [Test]
    public void InputVariableSpecifiedAndDoesNotConflict_AprioriInputCalcs() {
        var runtime = Funny.Hardcore
                           .WithApriori<string>("x")
                           .Build("x:text; y = x");
        var res = runtime.Calc("x", "test");
        res.AssertReturns("y", "test");
        var input = runtime["x"];
        var output = runtime["y"];
        Assert.IsFalse(input.IsOutput);
        Assert.IsTrue(output.IsOutput);
        Assert.AreEqual(FunnyType.Text, input.Type, "input");
        Assert.AreEqual(FunnyType.Text, output.Type, "output");
    }

    [Test]
    public void OutputVarSpecifiedAndDoesNotConflict_AprioriOutputCalcs() {
        var runtime = Funny.Hardcore
                           .WithApriori<string>("y")
                           .Build("x:text; y = x");

        var res = runtime.Calc("x", "test");
        res.AssertReturns("y", "test");
        Assert.AreEqual(FunnyType.Text, runtime["x"].Type, "input");
        Assert.AreEqual(FunnyType.Text, runtime["y"].Type, "output");
    }


    [Test]
    public void AprioriVariableDoesNotUsed_Calculates() {
        var runtime = Funny.Hardcore
                           .WithApriori<string>("alfa")
                           .Build("x:text; y = x");

        var res = runtime.Calc("x", "test");
        res.AssertReturns("y", "test");
        Assert.AreEqual(FunnyType.Text, runtime["x"].Type, "input");
        Assert.AreEqual(FunnyType.Text, runtime["y"].Type, "output");
    }

    [Test]
    public void OutputVarSpecifiedWithDifferentAprioriType_throws() =>
        TestHelper.AssertObviousFailsOnParse(
            () =>
                Funny.Hardcore.WithApriori("y", FunnyType.Text).Build("y:int = x"));

    [Test]
    public void InputVarSpecifiedWithAprioriOutputConflict_throws() =>
        TestHelper.AssertObviousFailsOnParse(
            () =>
                Funny.Hardcore.WithApriori("y", FunnyType.Text).Build("x:int; y = x"));

    [Test]
    public void OutputVarSpecifiedHasInputNameWithSameName_throws() =>
        TestHelper.AssertObviousFailsOnParse(
            () =>
                Funny.Hardcore.WithApriori("x", FunnyType.Text).Build("x:int; y = x"));

    [Test]
    public void OutputVarSpecifiedHasInputAprioriType_Calculates()
        => Assert.DoesNotThrow(() => Funny.Hardcore.WithApriori("x", FunnyType.Text).Build("y:text = x"));


    [Test]
    public void SpecifyTwoSameOutputs_throws() {
        var builder = Funny.Hardcore.WithApriori("y", FunnyType.Text);
        Assert.Throws<ArgumentException>(() => builder.WithApriori("y", FunnyType.Text));
    }

    [Test]
    public void SpecifyTwoSameInputs_throws() {
        var builder = Funny.Hardcore.WithApriori("x", FunnyType.Bool);
        Assert.Throws<ArgumentException>(() => builder.WithApriori("x", FunnyType.Text));
    }

    [Test]
    public void SpecifyVarWithSameNameAsFunction_RepeatConcatTest()
        => Funny.Hardcore
                .WithApriori<int>("count")
                .Build("out:text = name.repeat(count).flat()")
                .Calc(("count", 3), ("name", "foo"))
                .AssertReturns("foofoofoo");

    [Test]
    public void SpecifyVarWithSameNameAsFunction2_RepeatConcatTest()
        => Funny.Hardcore
                .WithApriori<int>("count")
                .Build("if (count>0) name.repeat(count).flat() else 'none'")
                .Calc(("count", 3), ("name", "foo"))
                .AssertReturns("foofoofoo");
}

}