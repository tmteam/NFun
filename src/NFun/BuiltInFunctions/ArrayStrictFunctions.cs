using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class AverageFunction : FunctionWithManyArguments
    {
        public AverageFunction() : base("avg", VarType.Real, VarType.ArrayOf(VarType.Real)) { }
        public override object Calc(object[] args) =>
            ((IFunArray)args[0]).As<double>().Average();
    }



    
}
