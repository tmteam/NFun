using System.Collections.Generic;
using Funny.Runtime;
using Funny.Types;

namespace Funny.Interpritation.Nodes
{
    public interface IExpressionNode
    {
        VarType Type { get; }
        
        object Calc();
        
    }
}