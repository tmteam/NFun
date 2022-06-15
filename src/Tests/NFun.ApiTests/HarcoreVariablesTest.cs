using System.Linq;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests; 

public class HardcoreVariablesTest {
    [Test]
    public void GetAllVariables_CheckValues() =>
        "out1 = (10.0*x).toText()".AssertRuntimes(runtime =>
        {
            var allVariables = runtime.Variables;
            Assert.AreEqual(2, allVariables.Count);

            var xVar = allVariables.Single(v => v.Name == "x");
            var out1Var = allVariables.Single(v => v.Name == "out1");

            Assert.AreEqual(false, xVar.IsOutput);
            Assert.AreEqual(FunnyType.Real, xVar.Type);

            Assert.AreEqual(true, out1Var.IsOutput);
            Assert.AreEqual("", out1Var.Value);
            Assert.AreEqual(FunnyType.Text, out1Var.Type);    
        });

    [Test]
    public void TryGetVariables_CheckValues() =>
        "out1 = (10.0*x).toText()".AssertRuntimes(
            runtime =>
            {

                var xVar = runtime["x"];
                Assert.IsNotNull(xVar);
                var out1Var = runtime["out1"];
                Assert.IsNotNull(out1Var);

                Assert.AreEqual(false, xVar.IsOutput);
                Assert.AreEqual(FunnyType.Real, xVar.Type);

                Assert.AreEqual(true, out1Var.IsOutput);
                Assert.AreEqual("", out1Var.Value);
                Assert.AreEqual(FunnyType.Text, out1Var.Type);
            });


    [Test]
    public void GetVariables_VariableDoesNotExist_ReturnsNull() =>
        "out1 = (10.0*x).toText()"
            .AssertRuntimes(runtime => Assert.IsNull(runtime["missingVar"]));

    [Test]
    public void SetClrValue_OutputVariableChanged() =>
        "out1 = (10.0*x).toText()".AssertRuntimes(runtime =>
        {
            var xVar = runtime["x"];
            var out1Var = runtime["out1"];
            xVar.Value = 42.0;
            runtime.Run();

            Assert.AreEqual(42.0, xVar.FunnyValue);
            Assert.AreEqual("420", out1Var.Value);
        });

    [TestCase("@name(true)\r x:int\r    y = x", "x", "name", true)]
    [TestCase("@foo\r@bar(123)\r x:int\r  z = x", "x", "bar", 123)]
    [TestCase("some = 1\r\r@foo(123.5)\r \r x:int\r z = x", "x", "foo", 123.5)]
    [TestCase("@name(false)\r \r    y = x", "y", "name", false)]
    [TestCase("@foo\r@bar('')\r z = x", "z", "bar", "")]
    [TestCase("some = 1\r\r@foo(0)\r \r y = x*3", "y", "foo", 0)]
    public void AttributeWithValue_ValueIsCorrect(
        string expression
      , string variable,
        string attribute, object value) =>
        expression.AssertRuntimes(runtime =>
        {
            var varInfo = runtime[variable];
            Assert.IsNotNull(varInfo);

            var actual = varInfo.Attributes.SingleOrDefault(v => v.Name == attribute);
            Assert.IsNotNull(actual);
            Assert.AreEqual(value, actual.Value);    
        });

    [TestCase("@private\r x:int\r    y = x", "x", new[] { "private" })]
    [TestCase("@foo\r@bar\r x:int\r  z = x", "x", new[] { "foo", "bar" })]
    [TestCase("some = 1\r@foo\r@bar\r x:int\r z = x", "x", new[] { "foo", "bar" })]
    [TestCase("@start\r x:int\r 1*x", "x", new[] { "start" })]
    [TestCase("@private\r y = x", "y", new[] { "private" })]
    [TestCase("@foo\r z = x", "z", new[] { "foo" })]
    [TestCase("@z\r z = x", "z", new[] { "z" })]
    [TestCase("@foo\r@bar\r z = x", "z", new[] { "foo", "bar" })]
    [TestCase("some = 1\r@foo\r@bar\r z = x", "z", new[] { "foo", "bar" })]
    [TestCase("some = 1\r@foo\r@bar\r@aaa\r z = x", "z", new[] { "foo", "bar", "aaa" })]
    [TestCase("@start\rsome = 1\r@foo\r@bar\r@aaa\r z = x", "some", new[] { "start" })]
    [TestCase("@start\rsome = 1\r z = x", "z", new string[0])]
    [TestCase("@start\r 1*x", "out", new[] { "start" })]
    [TestCase("1*x", "out", new string[0])]
    public void ValuelessAttributeOnVariables(
        string expression
      , string variable,
        string[] attribute) =>
        expression.AssertRuntimes(runtime =>
        {
            var varInfo = runtime[variable];
            Assert.IsNotNull(varInfo);
            CollectionAssert.AreEquivalent(attribute, varInfo.Attributes.Select(v => v.Name));
            Assert.IsTrue(varInfo.Attributes.All(a => a.Value == null));      
        });
}