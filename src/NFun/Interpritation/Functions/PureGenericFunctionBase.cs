using System.Linq;
using NFun.Types;

namespace NFun.Interpritation.Functions
{
    public abstract class PureGenericFunctionBase : GenericFunctionBase
    {
        protected PureGenericFunctionBase(string name,  int argsCount) 
            : base(name, VarType.Generic(0), Enumerable.Repeat(VarType.Generic(0),argsCount).ToArray())
        {
        }
        protected PureGenericFunctionBase(string name, GenericConstrains constrains,  int argsCount) 
            : base(name, constrains, VarType.Generic(0), Enumerable.Repeat(VarType.Generic(0),argsCount).ToArray())
        {
        }
    }
}