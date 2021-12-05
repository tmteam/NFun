using NFun.TestTools;
using NUnit.Framework;

namespace NFun.SyntaxTests.SyntaxDialect {

[TestFixture]
public class GenericIntConstantIsI32Test : GenericIntConstantTestBase<int> {
    public GenericIntConstantIsI32Test() : base(IntegerPreferredType.I32) { }
    protected override int Convert(int value) => value;
}

[TestFixture]
public class GenericIntConstantIsI64Test : GenericIntConstantTestBase<long> {
    public GenericIntConstantIsI64Test() : base(IntegerPreferredType.I64) { }
    protected override long Convert(int value) => value;
}

[TestFixture]
public class GenericIntConstantIsRealTest : GenericIntConstantTestBase<double> {
    public GenericIntConstantIsRealTest() : base(IntegerPreferredType.Real) { }
    protected override double Convert(int value) => value;

    [TestCase("y = x+3", 2.5, 5.5)]
    [TestCase("y = x+-2", 1.5, -0.5)]
    [TestCase("y = x*-2", 1.5, -3.0)]
    [TestCase("y = x*3.0", 2.5, 7.5)]
    [TestCase("y = x-3", 2.5, -0.5)]
    public void RealEquation(string expression, object input, object expected)
        => Build(expression).Calc("x", input).AssertResultHas("y", expected);


    [TestCase("[1,'2',3.0,4,5.2, true, false, 7.2]", new object[] { 1.0, "2", 3.0, 4.0, 5.2, true, false, 7.2 })]
    [TestCase("[1,'23',4.0,0x5, true]", new object[] { 1.0, "23", 4.0, 5, true })]
    public void AnonymousConstantArrayTest(string expr, object expected)
        => Calc(expr).AssertOut(expected);


    [TestCase("a=1; b=2; c=3;", new[] { "a", "b", "c" }, new object[] { 1.0, 2.0, 3.0 })]
    [TestCase("a = 1; b = 2; c = 3", new[] { "a", "b", "c" }, new object[] { 1.0, 2.0, 3.0 })]
    [TestCase("a=1; b = if (a==1) 'one' else 'foo'; c=45;", new[] { "a", "b", "c" }, new object[] { 1.0, "one", 45.0 })]
    [TestCase("a=1; b = if (a == 0) 0 else 1; c = 1", new[] { "a", "b", "c" }, new object[] { 1.0, 1.0, 1.0 })]
    [TestCase("a=0; b = cos(a); c = sin(a)", new[] { "a", "b", "c" }, new object[] { 0.0, 1.0, 0.0 })]
    [TestCase("a = [1,2,3,4].max(); b = [1,2,3,4].min()", new[] { "a", "b" }, new object[] { 4.0, 1.0 })]
    [TestCase("a =[0..10][1]; b=[0..5][2]; c=[0..5][3];", new[] { "a", "b", "c" }, new object[] { 1.0, 2.0, 3.0 })]
    public void SomeConstantInExpression(string expr, string[] outputNames, object[] constantValues) {
        var calculateResult = Calc(expr);
        for (var item = 0; item < outputNames.Length; item++)
        {
            var val = constantValues[item];
            var name = outputNames[item];
            calculateResult.AssertResultHas(name, val);
        }
    }
}

}