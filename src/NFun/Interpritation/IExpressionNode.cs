using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public interface IExpressionNode
    {
        void Apply(IExpressionNodeVisitor visitor);
        Interval Interval { get; }
        VarType Type { get; }
        object Calc();
    }
}