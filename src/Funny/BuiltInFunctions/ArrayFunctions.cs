using System.Collections;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Types;

namespace Funny.BuiltInFunctions
{
    public class MultiMaxRealFunction: FunctionBase{
        public MultiMaxRealFunction() : base("max",VarType.RealType, VarType.ArrayOf(VarType.RealType))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<double>().Max();
    }
    public class MultiMaxIntFunction: FunctionBase{
        public MultiMaxIntFunction() : base("max",VarType.IntType, VarType.ArrayOf(VarType.IntType))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<int>().Max();
    }
    public class AverageFunction : FunctionBase
    {
        public AverageFunction(): base("avg", VarType.RealType, VarType.ArrayOf(VarType.RealType)){}
        public override object Calc(object[] args) => (args[0] as IEnumerable).Cast<double>().Average();
    }
    public class LengthFunction : FunctionBase
    {
        public LengthFunction(): base("length", VarType.IntType, VarType.ArrayOf(VarType.AnyType))
        {
            
        }

        public override object Calc(object[] args) 
            => (args[0] as IEnumerable).Cast<object>().Count();
    }
}