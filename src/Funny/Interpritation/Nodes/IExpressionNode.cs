using System.Collections.Generic;
using Funny.Runtime;

namespace Funny.Interpritation.Nodes
{
    public interface IExpressionNode
    {
        VarType Type { get; }
        
        object Calc();
        
    }
}