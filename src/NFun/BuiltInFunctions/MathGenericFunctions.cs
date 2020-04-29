using System;
using System.Collections;
using System.Linq;
using NFun.Interpritation.Functions;
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


}