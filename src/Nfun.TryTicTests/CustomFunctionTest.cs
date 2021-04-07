using System;
using NFun;
using NFun.Interpritation.Functions;
using NFun.Types;
using NUnit.Framework;

namespace NFun.ModuleTests
{
    public class CustomFunctionTest
    {
        [Test]
        public void IsVarNameCapital_returnsBool()
        {
            var runtime = FunBuilder.With("y = 1.writeLog('hello')")
                .WithFunctions(new LogFunction()).Build();
            var result = runtime.Calculate();
            Assert.AreEqual(1.0, result.GetValueOf("y"));
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
}
