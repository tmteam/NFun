using System;
using NFun;
using NFun.Interpritation.Functions;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ModuleTests
{
    public class BuilderTest
    {
        [Test]
        public void WithFunctions_AddTwoConcreteFunctionsWithSameSignature_throws()
        {
            Assert.Throws<InvalidOperationException>(()=>FunBuilder
                .With("y = mama()")
                .WithFunctions(new PapaFunction("mama"))
                .WithFunctions(new MamaFunction("mama")));
        }
        [Test]
        public void WithFunctions_ConcreteAndGenericFunctionsWithSameSignature_throws()
        {
            Assert.Throws<InvalidOperationException>(() => FunBuilder
                .With("y = mama()")
                .WithFunctions(new PapaFunction("mama"))
                .WithFunctions(new GenericWithNoArgFunction("mama")));
        }
      
        [Test]
        public void WithFunctions_functionWithSameSignatureExists_functionIsOverrided()
        {
            var dictionary = NFun.BaseFunctions.CreateDefaultDictionary();
            Assert.AreEqual(PapaFunction.PapaReturn, FunBuilder
                .With("y = myFun()")
                .WithFunctions(new PapaFunction("myFun")) //override base function
                .Build()
                .Calculate()
                .GetValueOf("y"));
        }
        [Test]
        public void TryAddToCustomDictionary_functionWithSameSignatureExists_returnsFalse()
        {
            var dictionary = NFun.BaseFunctions.CreateDefaultDictionary();
            dictionary.TryAdd(new MamaFunction("myFun"));
            Assert.IsFalse(dictionary.TryAdd(new PapaFunction("myFun")));
        }
        [Test]
        public void AddOrThrowToCustomDictionary_functionWithSameSignatureExists_throws()
        {
            var dictionary = NFun.BaseFunctions.CreateDefaultDictionary();
            dictionary.AddOrThrow(new PapaFunction("myFun"));
            Assert.Throws<InvalidOperationException>(()=> dictionary.AddOrThrow(new MamaFunction("myFun")));
        }
        [Test]
        public void CreateWithCustomDictionary()
        {
            var dictionary = NFun.BaseFunctions.CreateDefaultDictionary();
            dictionary.TryAdd(new MamaFunction("mama"));
            Assert.AreEqual(MamaFunction.MamaReturn, FunBuilder
                .With("y = mama()")
                .With(dictionary)
                .Build()
                .Calculate()
                .GetValueOf("y"));
        }
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
    class MamaFunction: FunctionWithManyArguments
    {
        public const string MamaReturn = "mama called";

        public MamaFunction(string name) : base(name, VarType.Text)
        {
        }

        public override object Calc(object[] args) => MamaReturn;
    }
}
