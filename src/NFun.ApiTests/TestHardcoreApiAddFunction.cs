using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests {

public class TestHardcoreApiAddFunction {
    [Test]
    public void CustomNonGenericFunction_CallsWell() {
        string customName = "lenofstr";
        string arg = "some very good string";
        var runtime = Funny.Hardcore
            .WithFunction(
                new FunctionMock(
                    args => ((IFunnyArray)args[0]).Count,
                    customName,
                    FunnyType.Int32,
                    FunnyType.Text))
            .Build($"y = {customName}('{arg}')");

        runtime.Calc().AssertReturns("y", arg.Length);
    }

    [TestCase("[0x1,2,3,4]", new[] { 1, 3 })]
    [TestCase("[0x0,1,2,3,4]", new[] { 0, 2, 4 })]
    [TestCase("['0','1','2','3','4']", new[] { "0", "2", "4" })]
    [TestCase("[0.0]", new[] { 0.0 })]
    public void CustomGenericFunction_EachSecond_WorksFine(string arg, object expected) {
        string customName = "each_second";
        var runtime = Funny.Hardcore
            .WithFunction(
                new GenericFunctionMock(
                    args => new EnumerableFunnyArray(((IEnumerable<object>)args[0])
                        .Where((_, i) => i % 2 == 0), FunnyType.Any),
                    customName,
                    FunnyType.ArrayOf(FunnyType.Generic(0)),
                    FunnyType.ArrayOf(FunnyType.Generic(0))))
            .Build($"y = {customName}({arg})");
        runtime.Calc().AssertReturns("y", expected);
    }

    [Test]
    public void IsVarNameCapital_returnsBool() =>
        Funny
            .Hardcore
            .WithFunction(new LogFunction())
            .Build("y = 1.writeLog('hello')")
            .Calc().AssertReturns("y", 1);

    [Test]
    public void Use1ArgLambda() =>
        Funny.Hardcore
            .WithFunction("sqra", (int i) => i * i)
            .Build("y = sqra(10)")
            .Calc()
            .AssertReturns("y", 100);

    [Test]
    public void Use2ArgLambda() =>
        Funny
            .Hardcore
            .WithFunction("conca", (string t1, string t2) => t1 + t2)
            .Build("y = 'hello'.conca(' ').conca('world')")
            .Calc().AssertReturns("y", "hello world");

    [Test]
    public void Use3ArgLambda() =>
        Funny.Hardcore
            .WithFunction("conca", (string t1, string t2, string t3)
                => t1 + t2 + t3)
            .Build("y = conca('1','2','3')")
            .Calc().AssertReturns("y", "123");

    [Test]
    public void Use4ArgLambda() =>
        Funny.Hardcore
            .WithFunction("conca", (string t1, string t2, string t3, string t4)
                => t1 + t2 + t3 + t4)
            .Build("y = conca('1','2','3','4')")
            .Calc().AssertReturns("y", "1234");

    [Test]
    public void Use5ArgLambda() =>
        Funny
            .Hardcore
            .WithFunction("conca", (string t1, string t2, string t3, string t4, string t5)
                => t1 + t2 + t3 + t4 + t5)
            .Build("y = conca('1','2','3','4','5')")
            .Calc()
            .AssertReturns("y", "12345");

    [Test]
    public void Use6ArgLambda() =>
        Funny
            .Hardcore
            .WithFunction("conca", (string t1, string t2, string t3, string t4, string t5, string t6)
                => t1 + t2 + t3 + t4 + t5 + t6)
            .Build("y = conca('1','2','3','4','5','6')")
            .Calc().AssertReturns("y", "123456");

    [Test]
    public void Use7ArgLambda() {
        Funny
            .Hardcore
            .WithFunction("conca", (string t1, string t2, string t3, string t4, string t5, string t6, string t7)
                => t1 + t2 + t3 + t4 + t5 + t6 + t7)
            .Build("y = conca('1','2','3','4','5','6','7')")
            .Calc()
            .AssertReturns("y", "1234567");
    }
}

public class LogFunction : GenericFunctionBase {
    public LogFunction() : base("writeLog", FunnyType.Generic(0), FunnyType.Generic(0), FunnyType.Text) { }

    // T Log<T>(T, string)
    protected override object Calc(object[] args) {
        Console.WriteLine(args[1]);
        return args[0];
    }
}

public class GenericFunctionMock : GenericFunctionBase {
    private readonly Func<object[], object> _calc;

    public GenericFunctionMock(
        Func<object[], object> calc, string name, FunnyType returnType,
        params FunnyType[] argTypes) : base(name, returnType, argTypes) {
        _calc = calc;
    }

    protected override object Calc(object[] args) => _calc(args);
}

public class FunctionMock : FunctionWithManyArguments {
    private readonly Func<object[], object> _calc;

    public FunctionMock(Func<object[], object> calc, string name, FunnyType returnType, params FunnyType[] argTypes)
        : base(name, returnType, argTypes) {
        _calc = calc;
    }

    public override object Calc(object[] args) => _calc(args);
}

}