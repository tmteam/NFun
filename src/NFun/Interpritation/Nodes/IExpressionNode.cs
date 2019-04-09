using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public interface IExpressionNode
    {
        Interval Interval { get; }
        VarType Type { get; }
        object Calc();
                
    }
}