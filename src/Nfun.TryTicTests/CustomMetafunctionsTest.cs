using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Types;
using NUnit.Framework;

namespace Nfun.ModuleTests
{
    
    [TestFixture]
    class CustomMetafunctionsTest
    {
        [TestCase("x:anything; isCapital(x)", "x", false)]
        [TestCase("X:anything; isCapital(X)", "X", true)]
        [TestCase("isCapital(x)","x",false)]
        [TestCase("isCapital(X)", "X", true)]
        public void IsVarNameCapital_returnsBool(string expr, string inputName, object expected)
        {
            var runtime = FunBuilder.With(expr).WithFunctions(new IsVarNameCapitalMetafunction()).Build();
            var result = runtime.Calculate(VarVal.New(inputName, 42));
            Assert.AreEqual(expected, result.GetValueOf("out"));
        }

        [Test]
        public void GetCurrentValue_CorrectValueWithCorrectType()
        {
            var expr = "y = 123; g = y.curValue()";
            
            var runtime = FunBuilder.With(expr).WithFunctions(new GetCurValue()).Build();
            var gOut = runtime.Outputs.Single(o => o.Name == "g");
            Assert.AreEqual(VarType.Real, gOut.Type);
            var result = runtime.Calculate();
            Assert.AreEqual(123, result.GetValueOf("g"));
        }

        [Ignore("self reference behaviour is denied now")]
        [Test]
        public void PreviousOr_CounterExample()
        {
            var expr = "y = y.previousOr(0)+1";

            var runtime = FunBuilder.With(expr).WithFunctions(new PreviousOr()).Build();
            var yOut = runtime.Outputs.Single(o => o.Name == "y");
            Assert.AreEqual(VarType.Real, yOut.Type);
            var result_step1 = runtime.Calculate();
            Assert.AreEqual(0, result_step1.GetValueOf("y"));

            var result_step2 = runtime.Calculate();
            Assert.AreEqual(1, result_step2.GetValueOf("y"));
            
            var result_step3 = runtime.Calculate();
            Assert.AreEqual(2, result_step3.GetValueOf("y"));
        }

    }
    public class PreviousOr : GenericMetafunction
    {
        public PreviousOr() : base("previousOr", VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        public override object Calc(object[] args)
        {
            var source = args[0] as VariableSource;
            if (source.Value == null)
                return args[1];
            else return source.Value;
        }
    }
    public class GetCurValue : GenericMetafunction
    {
        public GetCurValue() : base("curValue",  VarType.Generic(0), VarType.Generic(0))
        {
        }

        public override object Calc(object[] args)
        {
            var source = args[0] as VariableSource;
            return source.Value;
        }
    }
    public class IsVarNameCapitalMetafunction : GenericMetafunction
    {
        public IsVarNameCapitalMetafunction() : base("isCapital", VarType.Bool, VarType.Generic(0))
        {
        }

        public override object Calc(object[] args)
        {
            var source = args[0] as VariableSource;
            return source.Name.ToUpper() == source.Name;
        }
    }
}
