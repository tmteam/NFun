using System;
using System.Collections;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Types;

namespace NFun.BuiltInFunctions
{

    public class AddTextFunction: FunctionWithManyArguments
    {
        public AddTextFunction(string name) : base(name, 
            VarType.Text,VarType.Text,VarType.Anything){}

        public override object Calc(object[] args) => args[0] + ToStringSmart(args[1]);

        private static string ToStringSmart(object o)
        {
            if (o is IEnumerable en && !(o is string))
                return '['+string.Join(",", en.Cast<object>().Select(ToStringSmart)) + ']';
            return o.ToString();
        }
        
    }

  

   
    public class FloorFunction : FunctionWithManyArguments {
        public FloorFunction () : base("floor", VarType.Int32, VarType.Real){}
        public override object Calc(object[] args) => Convert.ToInt32(Math.Floor(((double)args[0])));
    }
    
    public class CeilFunction : FunctionWithManyArguments {
        public CeilFunction () : base("ceil", VarType.Int32, VarType.Real){}
        public override object Calc(object[] args) => Convert.ToInt32(Math.Ceiling(((double)args[0])));
    }
    public class RoundToIntFunction: FunctionWithManyArguments {
        public RoundToIntFunction() : base("round", VarType.Int32,VarType.Real){}
        public override object Calc(object[] args) => (int)Math.Round(((double)args[0]));
    }
    
    public class RoundToRealFunction: FunctionWithManyArguments {
        public RoundToRealFunction() : base("round", VarType.Real,VarType.Real,VarType.Int32){}
        public override object Calc(object[] args) => Math.Round((double)args[0],(int)args[1]);
    }
    
    public class SignFunction: FunctionWithManyArguments {
        public SignFunction() : base("sign", VarType.Int32,VarType.Real){}
        public override object Calc(object[] args) =>  Math.Sign(((double)args[0]));
    }
    
    public class PiFunction : FunctionWithManyArguments {
        public PiFunction() : base("pi", VarType.Real){}
        public override object Calc(object[] args) => Math.PI;
    }
    public class EFunction : FunctionWithManyArguments {
        public EFunction() : base("e",  VarType.Real){}
        public override object Calc(object[] args) => Math.E;
    }
}