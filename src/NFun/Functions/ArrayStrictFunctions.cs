using System.Linq;
using NFun.Interpretation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.Functions
{
    public class AverageFunction : FunctionWithSingleArg
    {
        public AverageFunction() : base("avg", FunnyType.Real, FunnyType.ArrayOf(FunnyType.Real)) { }
        public override object Calc(object a) =>
            ((IFunnyArray)a).As<double>().Average();
    }
}
