using System;
using Funny.Interpritation;
using Funny.Interpritation.Functions;
using Funny.Runtime;

namespace Funny.BuiltInFunctions
{
    public class AddFunction: FunctionBase
    {
        public AddFunction() : base("add", VarType.RealType,VarType.RealType,VarType.RealType){}
        public override object Calc(object[] args) => Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]);
    }

    public class AbsFunction : FunctionBase
    {
        public AbsFunction() : base("abs", VarType.RealType,VarType.RealType){}
        public override object Calc(object[] args)
        {
            var val = Convert.ToDouble(args[0]);
            return val > 0 ? val : -val;
        }
    }

    public class CosFunction : FunctionBase
    {
        public CosFunction() : base("cos", VarType.RealType, VarType.RealType){}

        public override object Calc(object[] args) => Math.Cos(Convert.ToDouble(args[0]));
    }
    public class SinFunction : FunctionBase
    {
        public SinFunction() : base("sin", VarType.RealType,VarType.RealType){}

        public override object Calc(object[] args) => Math.Sin(Convert.ToDouble(args[0]));
    }
    public class TanFunction : FunctionBase
    {
        public TanFunction() : base("tan", VarType.RealType, VarType.RealType){}

        public override object Calc(object[] args) => Math.Tan(Convert.ToDouble(args[0]));
    }
    
    public class PiFunction : FunctionBase
    {
        public PiFunction() : base("pi", VarType.RealType){}

        public override object Calc(object[] args) => Math.PI;
    }
    public class EFunction : FunctionBase
    {
        public EFunction() : base("e",  VarType.RealType){}
        public override object Calc(object[] args) => Math.E;
    }
    
}