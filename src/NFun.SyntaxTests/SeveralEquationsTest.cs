using System.Linq;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.SyntaxTests {

[TestFixture]
public class SeveralEquationsTest {
    [TestCase("y = 1.0\r z = true", 1.0, true)]
    [TestCase("y = 2\r z=3", 2, 3)]
    [TestCase("y = 2*3\r z= 1 + (1 + 4)/2 - (3 +2)*(3 -1)", 6, -6.5)]
    public void TwinConstantEquations(string expr, object expectedY, object expectedZ) =>
        expr.AssertReturns(("y", expectedY), ("z", expectedZ));

    [TestCase("y = x*0.5\r z=3", 2, 1.0, 3)]
    [TestCase("y = x/2\r z=2*x", 2, 1.0, 4.0)]
    public void TwinEquationsWithSingleVariable(string expr, double x, object expectedY, object expectedZ) =>
        expr.Calc("x", x).AssertReturns(("y", expectedY), ("z", expectedZ));

    [TestCase("x:real\r y = x\r z=x", new[] { "x" })]
    [TestCase("y = 1\r z=2", new string[0])]
    [TestCase("x:real\r y = x/2\r z=2*x", new[] { "x" })]
    [TestCase("in1:real; in2:real; y = in1/2\r z=2+in2", new[] { "in1", "in2" })]
    [TestCase("in1:real; in2:real;y = in1/2 + in2\r z=2 + in2", new[] { "in1", "in2" })]
    [TestCase("y = 1\r z=2", new string[0])]
    [TestCase("y = x/2\r z=2*x", new[] { "x" })]
    [TestCase("y = in1/2\r z=2+in2+ in1", new[] { "in1", "in2" })]
    [TestCase("y = in1/2 + in2\r z=2 + in2", new[] { "in1", "in2" })]
    [TestCase("x:real \r y = x\r z=y", new[] { "x" })]
    [TestCase("a:real \r y = a*a\r z=y", new[] { "a" })]
    [TestCase("y = 1.0\r z=y", new string[0])]
    [TestCase("y = x/2\r z=2*y", new[] { "x" })]
    [TestCase("y = x/2\r z=2*y+x", new[] { "x" })]
    [TestCase("y = in1/2\r z=2*y+in2", new[] { "in1", "in2" })]
    [TestCase("y = in1/2 + in2\r z=2*y + in2", new[] { "in1", "in2" })]
    public void TwinEquations_inputVarablesListIsCorrect(string expr, string[] inputNames) {
        var inputs = expr.Build().Variables.Where(i => !i.IsOutput);
        Assert.IsTrue(inputs.All(i => i.Type == FunnyType.Real));
        CollectionAssert.AreEquivalent(inputNames, inputs.Select(i => i.Name));
    }

    [TestCase("y = 1.0\r z=y", 1.0, 1.0)]
    [TestCase("y = 1\r z=y/2", 1.0, 0.5)]
    [TestCase("y = true\r z=y", true, true)]
    [TestCase("y = 1\r z=y", 1, 1)]
    [TestCase("y = 1\r z=y*2", 1, 2)]
    public void TwinDependentConstantEquations_CalculatesCorrect(string expr, object expectedY, object expectedZ) =>
        expr.AssertReturns(("y", expectedY), ("z", expectedZ));


    [TestCase(2, "x:real\r y = x\r z=y", 2, 2)]
    [TestCase(2, "y = x/2\r z=2*y", 1, 2)]
    [TestCase(2, "y = x/2\r z=2*y+x", 1, 4)]
    public void TwinDependentEquationsWithSingleVariable_CalculatesCorrect(
        double x, string expr, double expectedY, double expectedZ) =>
        expr.Calc("x", x).AssertReturns(("y", expectedY), ("z", expectedZ));

    [TestCase("o1 = 1\r o2=o1\r o3 = 0", 1, 1, 0)]
    [TestCase("o1 = 1\r o2 = o1+1\r o3=2*o1*o2", 1, 2, 4)]
    [TestCase("o1 = 1\r o3 = 2.0 \r o2 = o3", 1, 2.0, 2.0)]
    [TestCase("o3 = 2 \ro2 = o3*2 \ro1 = o2*2\r ", 8, 4, 2)]
    public void ThreeDependentConstantEquations_CalculatesCorrect(string expr, object o1, object o2, object o3) =>
        expr.AssertReturns(("o1", o1), ("o2", o2), ("o3", o3));

    [TestCase(2, "x:real\r o1 = x\r o2=o1\r o3 = 0", 2.0, 2.0, 0)]
    [TestCase(2, "o1 = x/2\r o2 = o1+1\r o3=2*o1*o2", 1.0, 2.0, 4.0)]
    [TestCase(2, "o1 = x/2\r o3 = x \ro2 = o3", 1.0, 2.0, 2.0)]
    public void ThreeDependentEquationsWithSingleVariable_CalculatesCorrect(
        object x, string expr, object o1, object o2, object o3) =>
        expr.Calc("x", x).AssertReturns(("o1", o1), ("o2", o2), ("o3", o3));

    [Test]
    public void DependentCaseableEquations() =>
        "yPub = 2\r y2 = 3 +yPub".AssertReturns(("yPub", 2), ("y2", 5));

    [Test]
    public void ComplexDependentConstantsEquations_CalculatesCorrect() =>
        @"o1 = 1
            o2 = o1*2
            o3 = o2*2
            o4 = o1/2
            o5 = 0
            o6 = o4+o3
            o7 = true
            o8 = o7 xor true"
            .AssertReturns(
                ("o1", 1.0),
                ("o2", 2.0),
                ("o3", 4.0),
                ("o4", 0.5),
                ("o5", 0),
                ("o6", 4.5),
                ("o7", true),
                ("o8", false));

    [TestCase("o1 = o2\r o2=o1")]
    [TestCase("o1 = o3\r o2 = o1\r o3 = o2")]
    [TestCase("o0 = 3\r o1 = o3+o0\r o2 = o1\r o3 = o2")]
    [TestCase("y1 = 3\r y1 = 4")]
    [TestCase("set a=4")]
    [TestCase("y1 = 3 y2 = 4")]
    [TestCase("y1 = 3 y1 = 4")]
    public void ObviouslyFails(string expr) => expr.AssertObviousFailsOnParse();
}

}