using System.Linq;
using NFun.Types;

namespace NFun.Interpretation.Functions
{
    public abstract class PureGenericFunctionBase : GenericFunctionBase
    {
        protected PureGenericFunctionBase(string name, GenericConstrains constrains,  int argsCount) 
            : base(name, constrains, FunnyType.Generic(0), Enumerable.Repeat(FunnyType.Generic(0),argsCount).ToArray())
        {
        }
    }
}