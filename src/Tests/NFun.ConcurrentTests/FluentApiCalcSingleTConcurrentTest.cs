using NFun.TestTools;
using NUnit.Framework;

namespace NFun.ConcurrentTests;

public class FluentApiCalcSingleTConcurrentTest {
    [TestCase("age", 13)]
    [TestCase("Age", 13)]
    [TestCase("(Age == 13) and (Name == 'vasa')", true)]
    [TestCase("(age != 13) or (name != 'vasa')", false)]
    [TestCase("name.reverse()", "asav")]
    [TestCase("'{name}{age}'.reverse()", "31asav")]
    [TestCase("'{name}{age}'.reverse()=='31asav'", true)]
    [TestCase("'mama'=='{name}{age}'.reverse()", false)]
    [TestCase("out:any ='hello world'", "hello world")]
    [TestCase("1", 1)]
    [TestCase("ids.count(rule it>2)", 2)]
    [TestCase("ids.filter(rule it>2)", new[] { 101, 102 })]
    [TestCase("out:int[]=ids.filter(rule it>age).map(rule it*it)", new[] { 10201, 10404 })]
    [TestCase("ids.reverse().join(',')", "102,101,2,1")]
    [TestCase("['Hello','world']", new[] { "Hello", "world" })]
    [TestCase("ids.map(rule it.toText())", new[] { "1", "2", "101", "102" })]
    public void GeneralUserInputModelTest(string expr, object expected) =>
        expr.CalcSingleUntypedInDifferentWays(expected, new UserInputModel(
            name: "vasa",
            age: 13,
            size: 13.5,
            iq: 50,
            ids: new[] { 1, 2, 101, 102 }));

    [Test]
    public void InputFieldIsCharArray() =>
        "[letters.reverse()]".CalcSingleUntypedInDifferentWays(new[] { "test" }
            , new ModelWithCharArray2 { Letters = new[] { 't', 's', 'e', 't' } });

    [Test]
    public void InputStructFieldsAreNotCaseSensitive()
        => "imOdeL.nAMe".CalcSingleTypedInDifferentWays(
            expected: "peter",
            input: new ContextModel1(42, imodel: new UserInputModel(name: "peter")));
}
