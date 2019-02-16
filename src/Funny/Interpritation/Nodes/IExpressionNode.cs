using System.Collections.Generic;
using Funny.Runtime;

namespace Funny.Interpritation.Nodes
{
    public interface IExpressionNode
    {
        IEnumerable<IExpressionNode> Children { get; }
        VarType Type { get; }
        
        object Calc();
        
    }
}