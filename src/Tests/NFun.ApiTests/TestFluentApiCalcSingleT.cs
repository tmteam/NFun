using System;
using System.Collections.Generic;
using NFun.Exceptions;
using NUnit.Framework;

namespace NFun.ApiTests {

public class TestFluentApiCalcSingleT {
    [TestCase("age", 13)]
    [TestCase("Age", 13)]
    [TestCase("(Age == 13) and (Name == 'vasa')", true)]
    [TestCase("(AGE == 13) and (NAME == 'vasa')", true)]
    [TestCase("(age == 13) and (name == 'vasa')", true)]
    [TestCase("(age != 13) or (name != 'vasa')", false)]
    [TestCase("name.reverse()", "asav")]
    [TestCase("'{name}{Age}'.reverse()", "31asav")]
    [TestCase("'{name}{age}'.reverse()", "31asav")]
    [TestCase("'{name}{age}'.reverse()=='31asav'", true)]
    [TestCase("'mama'=='{name}{age}'.reverse()", false)]
    [TestCase("'hello world'", "hello world")]
    [TestCase("out:any ='hello world'", "hello world")]
    [TestCase("1", 1.0)]
    [TestCase("ids.count(fun it>2)", 2)]
    [TestCase("ids.filter(fun it>2)", new[] { 101, 102 })]
    [TestCase("out:int[]=ids.filter(fun it>age).map(fun it*it)", new[] { 10201, 10404 })]
    [TestCase("ids.reverse().join(',')", "102,101,2,1")]
    [TestCase("['Hello','world']", new[] { "Hello", "world" })]
    [TestCase("ids.map(fun it.toText())", new[] { "1", "2", "101", "102" })]
    public void GeneralUserInputModelTest(string expr, object expected) =>
        CalcInDifferentWays(
            expr, expected, new UserInputModel(
                name: "vasa",
                age: 13,
                size: 13.5,
                iq: 50,
                ids: new[] { 1, 2, 101, 102 }));

    [Test]
    public void InputFieldIsCharArray() =>
        CalcInDifferentWays(
            "[letters.reverse()]", new[] { "test" }
          , new ModelWithCharArray2 { Letters = new[] { 't', 's', 'e', 't' } });
        
    [Test]
    public void OutputTypeIsStruct_returnsFunnyStruct() {
        var str = Funny.Calc(
            "{name = 'alaska'}", new UserInputModel("vasa"));
        Assert.IsInstanceOf<IReadOnlyDictionary<string, object>>(str);
        var rs = str as IReadOnlyDictionary<string, object>;
        Assert.AreEqual(1, rs.Count);
        Assert.AreEqual("alaska", rs["name"]);
    }
    
    [Test]
    public void OutputTypeIsText_returnsString() {
        var str = Funny.Calc("'Text'", new UserInputModel());
        Assert.IsInstanceOf<string>(str);
        Assert.AreEqual("Text", str);
    }
    

    [Test]
    public void ReturnsComplexIntArrayConstant() {
        var result = Funny.Calc(
            "[[[1,2],[]],[[3,4]],[[]]]", new UserInputModel("vasa", 13, size: 21, balance: Decimal.Zero, iq: 1, 1, 2, 3, 4));
        Assert.AreEqual(
            new[] {
                new[] { new[] { 1, 2 }, Array.Empty<int>() },
                new[] { new[] { 3, 4 } },
                new[] { Array.Empty<int>() }
            }, result);
    }

    [TestCase("")]
    [TestCase("x:int;")]
    [TestCase("x:int = 2")]
    [TestCase("a = 12; b = 32; x = a*b")]
    [TestCase("y = name")]
    public void NoOutputSpecified_throws(string expr)
        => Assert.Throws<FunnyParseException>(() => Funny.Calc(expr, new UserInputModel("vasa")));


    [TestCase("age>someUnknownvariable")]
    public void UseUnknownInput_throws(string expression) =>
        Assert.Throws<FunnyParseException>(
            () =>
                Funny.Calc(expression, new UserInputModel(age: 22)));

    [TestCase("age>AGE")]
    public void UseDifferentInputCase_throws(string expression) =>
        Assert.Throws<FunnyParseException>(() => Funny.Calc(expression, new UserInputModel(age: 22)));
    
    private static void CalcInDifferentWays<TInput>(string expr, object expected, TInput input) {
        //CALC
        var result1 = Funny.Calc<TInput>(expr, input);
        //calculator+calc
        var calculator = Funny.BuildForCalc<TInput>();
        var result2 = calculator.Calc(expr, input);
        var result3 = calculator.Calc(expr, input);
        var result4 = Funny.WithConstant("SomeNotUsedConstant", 42).Calc(expr, input);
        var result5 = Funny
                      .WithConstant("SomeNotUsedConstant", 42)
                      .BuildForCalc<TInput>()
                      .Calc(expr, input);

        //lambda
        var lambda1 = calculator.ToLambda(expr);
        var result6 = lambda1(input);
        var result7 = lambda1(input);

        var lambda2 = calculator.ToLambda(expr);
        var result8 = lambda2(input);
        var result9 = lambda2(input);

        Assert.AreEqual(expected, result1);
        Assert.AreEqual(expected, result2);
        Assert.AreEqual(expected, result3);
        Assert.AreEqual(expected, result4);
        Assert.AreEqual(expected, result5);
        Assert.AreEqual(expected, result6);
        Assert.AreEqual(expected, result7);
        Assert.AreEqual(expected, result8);
        Assert.AreEqual(expected, result9);
    }
}

}