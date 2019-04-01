using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public interface IExpressionNode
    {
        VarType Type { get; }
        
        object Calc();
        
    }
}