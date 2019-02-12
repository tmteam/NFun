using System;
using Funny.Interpritation;
using Funny.Runtime;

namespace Funny.BuiltInFunctions
{
    public class AddFunction: FunctionBase
    {
        public AddFunction() : base("add", 2){}
        public override VarType CalcType() => VarType.NumberType;

        public override double Calc(double[] args) => args[0] + args[1];
    }

    public class AbsFunction : FunctionBase
    {
        public AbsFunction() : base("abs", 1){}
        public override VarType CalcType() => VarType.NumberType;

        public override double Calc(double[] args) => args[0] > 0 ? args[0] : -args[0];
    }

    public class CosFunction : FunctionBase
    {
        public CosFunction() : base("cos", 1){}
        public override VarType CalcType() => VarType.NumberType;

        public override double Calc(double[] args) => Math.Cos(args[0]);
    }
    public class SinFunction : FunctionBase
    {
        public SinFunction() : base("sin", 1){}
        public override VarType CalcType() => VarType.NumberType;

        public override double Calc(double[] args) => Math.Sin(args[0]);
    }
    public class TanFunction : FunctionBase
    {
        public TanFunction() : base("tan", 1){}
        public override VarType CalcType() => VarType.NumberType;

        public override double Calc(double[] args) => Math.Tan(args[0]);
    }
    
    public class PiFunction : FunctionBase
    {
        public PiFunction() : base("pi", 0){}
        public override VarType CalcType() => VarType.NumberType;

        public override double Calc(double[] args) => Math.PI;
    }
    public class EFunction : FunctionBase
    {
        public EFunction() : base("e", 0){}
        public override VarType CalcType() => VarType.NumberType;

        public override double Calc(double[] args) => Math.E;
    }
    
}