using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Funny.Interpritation;
using Funny.Interpritation.Functions;
using Funny.Runtime;
using Funny.Types;

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

    public class MaxOfIntFunction : FunctionBase
    {
        public MaxOfIntFunction() : base("max", VarType.IntType, VarType.IntType, VarType.IntType)
        {
        }

        public override object Calc(object[] args) 
            => Math.Max((int) args[0], (int) args[1]);
    }
    public class MaxOfRealFunction : FunctionBase
    {
        public MaxOfRealFunction () : base("max", VarType.RealType, VarType.RealType, VarType.RealType)
        {
        }

        public override object Calc(object[] args) 
            => Math.Max((double) args[0], (double) args[1]);
    }
    
    public class MinOfIntFunction : FunctionBase
    {
        public MinOfIntFunction() : base("min", VarType.IntType, VarType.IntType, VarType.IntType)
        {
        }

        public override object Calc(object[] args) 
            => Math.Min((int) args[0], (int) args[1]);
    }
    public class MinOfRealFunction : FunctionBase
    {
        public MinOfRealFunction () : base("min", VarType.RealType, VarType.RealType, VarType.RealType)
        {
        }

        public override object Calc(object[] args) 
            => Math.Min((double) args[0], (double) args[1]);
    }

  
    
}