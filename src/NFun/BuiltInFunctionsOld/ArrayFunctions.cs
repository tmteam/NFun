using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Interpritation.Functions;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.Types;

namespace NFun.BuiltInFunctions
{
  
    

    public class AverageFunction : FunctionBase
    {
        public AverageFunction(): base("avg", VarType.Real, VarType.ArrayOf(VarType.Real)){}
        public override object Calc(object[] args) => 
            ((IFunArray)args[0]).As<double>().Average();
    }
    
   
    
    public class CountFunction : FunctionBase
    {
        public CountFunction(): base("count", VarType.Int32, VarType.ArrayOf(VarType.Anything))
        {
            
        }

        public override object Calc(object[] args) 
            => ((IFunArray)args[0]).Count;
    }
}