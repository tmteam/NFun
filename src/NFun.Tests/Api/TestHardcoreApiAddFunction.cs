using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;
using NUnit.Framework;

namespace NFun.Tests.Api
{
    public class TestHardcoreApiAddFunction
    {
        [Test]
        public void CustomNonGenericFunction_CallsWell()
        {
            string customName = "lenofstr";
            string arg = "some very good string";
            var runtime = Funny.Hardcore
                .WithFunctions(
                new FunctionMock(
                    args => ((IFunArray)args[0]).Count, 
                    customName, 
                    VarType.Int32, 
                    VarType.Text))
                .Build($"y = {customName}('{arg}')");
           
            runtime.Calculate().AssertReturns(VarVal.New("y", arg.Length));
        }

        [TestCase("[0x1,2,3,4]", new[] { 1, 3 })]
        [TestCase("[0x0,1,2,3,4]", new[] { 0, 2, 4 })]
        [TestCase("['0','1','2','3','4']", new[] { "0", "2", "4" })]
        [TestCase("[0.0]", new[] { 0.0 })]
        public void CustomGenericFunction_EachSecond_WorksFine(string arg, object expected)
        {
            string customName = "each_second";
            var runtime = Funny.Hardcore
                .WithFunctions(
                    new GenericFunctionMock(
                        args => new EnumerableFunArray(((IEnumerable<object>)args[0])
                            .Where((_, i) => i % 2 == 0), VarType.Anything),
                        customName,
                        VarType.ArrayOf(VarType.Generic(0)),
                        VarType.ArrayOf(VarType.Generic(0))))
                .Build($"y = {customName}({arg})");
            runtime.Calculate().AssertReturns(VarVal.New("y", expected));
        }
        [Test]
        public void IsVarNameCapital_returnsBool()
        {
            var result = Funny.Hardcore
                .WithFunctions(new LogFunction())
                .Build("y = 1.writeLog('hello')")
                .Calculate();
            Assert.AreEqual(1.0, result.GetValueOf("y"));
        }
        [Test]
        public void Use1ArgLambda()
        {
            var result = Funny.Hardcore
                .WithFunction("sqra", (int i) => i*i)
                .Build("y = sqra(10)")
                .Calculate();
            Assert.AreEqual(100, result.GetValueOf("y"));
        }
        [Test]
        [Ignore("Not implemented yet")]
        public void Use2ArgLambda()
        {
            var result = Funny.Hardcore
                .WithFunction("conca", (string t1, string t2) => t1+t2)
                .Build("y = 'hello'.conca(' ').conca('world')")
                .Calculate();
            Assert.AreEqual("hello world", result.GetValueOf("y"));
        }
    }

    public class LogFunction : GenericFunctionBase
    {

        public LogFunction() : base("writeLog", VarType.Generic(0), VarType.Generic(0), VarType.Text)
        {
        }
        // T Log<T>(T, string)
        protected override object Calc(object[] args)
        {
            Console.WriteLine(args[1]);
            return args[0];
        }
    }
    public class GenericFunctionMock: GenericFunctionBase
    {
        private readonly Func<object[], object> _calc;

        public GenericFunctionMock(Func<object[], object> calc,string name, VarType returnType, params VarType[] argTypes) : base(name, returnType, argTypes)
        {
            _calc = calc;
        }

        protected override object Calc(object[] args) => _calc(args);
    }

    public class  FunctionMock: FunctionWithManyArguments
    {
        private readonly Func<object[], object> _calc;

        public FunctionMock(Func<object[], object> calc, string name, VarType returnType, params VarType[] argTypes) 
            : base(name, returnType, argTypes)
        {
            _calc = calc;
        }

        public override object Calc(object[] args) => _calc(args);
    }
}