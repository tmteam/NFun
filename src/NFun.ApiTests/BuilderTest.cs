using System;
using NFun.Interpritation.Functions;
using NFun.TestTools;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ApiTests
{
    public class BuilderTest
    {
        [Test]
        public void WithFunctions_AddTwoConcreteFunctionsWithSameSignature_throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Funny
                    .Hardcore
                    .WithFunction(new PapaFunction("mama"))
                    .WithFunction(new MamaFunction("mama")));
        }

        [Test]
        public void WithFunctions_ConcreteAndGenericFunctionsWithSameSignature_throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                Funny
                    .Hardcore
                    .WithFunction(new PapaFunction("mama"))
                    .WithFunction(new GenericWithNoArgFunction("mama")));
        }

        [Test]
        public void WithFunctions_functionWithSameSignatureExists_functionIsOverrided()
        {
            Assert.AreEqual(PapaFunction.PapaReturn,
                Funny
                    .Hardcore
                    .WithFunction(new PapaFunction("myFun")) //override base function
                    .Build("y = myFun()")
                    .Calc()
                    .GetValueOf("y"));
        }


        [Test]
        public void CreateWithCustomDictionary()
        {
            var dictionary = NFun.BaseFunctions.DefaultDictionary.CloneWith(new MamaFunction("mama"));
            Funny.Hardcore
                .WithFunctions(dictionary)
                .Build("y = mama()")
                .Calc().AssertReturns("y", MamaFunction.MamaReturn);

        }

        class GenericWithNoArgFunction : GenericFunctionBase
        {
            public GenericWithNoArgFunction(string name) : base(name, VarType.Generic(0))
            {
            }

            protected override object Calc(object[] args)
                => throw new InvalidOperationException();
        }

        class PapaFunction : FunctionWithManyArguments
        {
            public const string PapaReturn = "papa is here";

            public PapaFunction(string name) : base(name, VarType.Text)
            {
            }

            public override object Calc(object[] args) => PapaReturn;
        }

        class MamaFunction : FunctionWithManyArguments
        {
            public const string MamaReturn = "mama called";

            public MamaFunction(string name) : base(name, VarType.Text)
            {
            }

            public override object Calc(object[] args) => MamaReturn;
        }
    }
}
