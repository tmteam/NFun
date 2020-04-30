using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class MapFunction : GenericFunctionBase
    {
        public MapFunction() : base("map",
            VarType.ArrayOf(VarType.Generic(1)),
            VarType.ArrayOf(VarType.Generic(0)),
            VarType.Fun(VarType.Generic(1), VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arr = (IFunArray)args[0];
            var map = args[1] as FunctionBase;

            var res = ImmutableFunArray.By(arr.Select(a => map.Calc(new[] { a })));
            return res;
        }
    }
}
