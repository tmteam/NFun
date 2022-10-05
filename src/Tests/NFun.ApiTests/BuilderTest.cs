using System;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests; 

public class BuilderTest {
    [Test]
    public void WithFunctions_AddTwoConcreteFunctionsWithSameSignature_throws() {
        Assert.Throws<InvalidOperationException>(
            () =>
                Funny
                    .Hardcore
                    .WithFunction(new PapaFunction("mama"))
                    .WithFunction(new MamaFunction("mama"))
                    .Build("x"));
    }

    [Test]
    public void WithFunctions_ConcreteAndGenericFunctionsWithSameSignature_throws() {
        Assert.Throws<InvalidOperationException>(
            () =>
                Funny
                    .Hardcore
                    .WithFunction(new PapaFunction("mama"))
                    .WithFunction(new GenericWithNoArgFunction("mama"))
                    .Build("x"));
    }

    [Test]
    public void WithFunctions_functionWithSameSignatureExists_functionIsOverrided() =>
        Funny
            .Hardcore
            .WithFunction(new PapaFunction("myFun")) //override base function
            .Build("y = myFun()")
            .AssertRuntimes(r => Assert.AreEqual(PapaFunction.PapaReturn, r.Calc().Get("y")));


    class GenericWithNoArgFunction : GenericFunctionBase {
        public GenericWithNoArgFunction(string name) : base(name, FunnyType.Generic(0)) { }

        protected override object Calc(object[] args)
            => throw new InvalidOperationException();
    }

    class PapaFunction : FunctionWithManyArguments {
        public const string PapaReturn = "papa is here";

        public PapaFunction(string name) : base(name, FunnyType.Text) { }

        public override object Calc(object[] args) => new TextFunnyArray(PapaReturn);
    }

    class MamaFunction : FunctionWithManyArguments {
        public const string MamaReturn = "mama called";

        public MamaFunction(string name) : base(name, FunnyType.Text) { }

        public override object Calc(object[] args) => new TextFunnyArray(MamaReturn);
    }
}