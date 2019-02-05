using System.Collections.Generic;

namespace Funny.Interpritation
{
    public interface IExpressionNode
    {
        IEnumerable<IExpressionNode> Children { get; }
        double Calc();
        
    }
}