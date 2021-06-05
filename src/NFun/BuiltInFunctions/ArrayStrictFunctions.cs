using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class AverageFunction : FunctionWithSingleArg
    {
        public AverageFunction() : base("avg", FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real)) { }
        public override object Calc(object a) =>
            ((IFunArray)a).As<double>().Average();
    }
}
