using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes {

internal class IfElseExpressionNode : IExpressionNode {
    private readonly IExpressionNode[] _ifExpressionNodes;
    private readonly IExpressionNode[] _conditionNodes;
    private readonly IExpressionNode _elseNode;

    public IfElseExpressionNode(
        IExpressionNode[] ifExpressionNodes,
        IExpressionNode[] conditionNodes,
        IExpressionNode elseNode,
        Interval interval,
        FunnyType type) {
        _ifExpressionNodes = ifExpressionNodes;
        _conditionNodes = conditionNodes;
        _elseNode = elseNode;

        Type = type;
        Interval = interval;
    }

    public object Calc() {
        for (var index = 0; index < _ifExpressionNodes.Length; index++)
        {
            if ((bool)_conditionNodes[index].Calc())
                return _ifExpressionNodes[index].Calc();
        }

        return _elseNode.Calc();
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
}

}