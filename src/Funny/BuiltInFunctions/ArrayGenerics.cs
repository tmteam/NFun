using System.Collections;
using System.Linq;
using Funny.Interpritation.Functions;
using Funny.Types;

namespace Funny.BuiltInFunctions
{
    
    public class RepeatGenericFunctionDefenition : GenericFunctionBase
    {
        public RepeatGenericFunctionDefenition() : base("repeat",
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Generic(0), 
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            var first = args[0];
            return Enumerable.Repeat(first, (int) args[1]);
        }
    }
    public class ConcatGenericFunctionDefenition: GenericFunctionBase
    {
        public ConcatGenericFunctionDefenition() : base("concat", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)))
        {
        }

        public override object Calc(object[] args)
        {
            var arrLeft = (args[0] as IEnumerable).Cast<object>();
            var arrRight = (args[1] as IEnumerable).Cast<object>();

            return arrLeft.Concat(arrRight);
        }
    }
    public class TakeGenericFunctionDefenition: GenericFunctionBase
    {
        public TakeGenericFunctionDefenition() : base("take", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            return (args[0] as IEnumerable).Cast<object>().Take((int) args[1]);
        }
    }
    
    public class SkipGenericFunctionDefenition: GenericFunctionBase
    {
        public SkipGenericFunctionDefenition() : base("skip", 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.ArrayOf(VarType.Generic(0)), 
            VarType.Int)
        {
        }

        public override object Calc(object[] args)
        {
            return (args[0] as IEnumerable).Cast<object>().Skip((int) args[1]);
        }
    }

}