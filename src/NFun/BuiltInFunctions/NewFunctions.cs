using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NFun.Interpritation.Functions;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
    public class AddFunction : GenericFunctionBase
    {
        private readonly GenericConstrains[] _genericDefenitions = {
            GenericConstrains.Arithmetical
        };

        public override GenericConstrains[] GenericDefenitions => _genericDefenitions;

        public AddFunction(string name) : base(name, VarType.Generic(0), VarType.Generic(0), VarType.Generic(0)) { }

        public AddFunction() : this(CoreFunNames.Add) { }

        public override object Calc(object[] args) => args.Get<double>(0) + args.Get<double>(1);
    }

    public class MapFunction : GenericFunctionBase
    {
        public override GenericConstrains[] GenericDefenitions 
            =>new[]
        {
            GenericConstrains.Any,
            GenericConstrains.Any,
        };

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
