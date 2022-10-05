using System.Collections.Generic;
using System.Linq;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 

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

    public string DebugName => $"IfElse of {Type}";
    public IEnumerable<IExpressionNode> Children => _conditionNodes.Concat(_ifExpressionNodes).Append(_elseNode);

    public IExpressionNode Clone(ICloneContext context) {
        var ifExpressionsCopy = _ifExpressionNodes.SelectToArray(c => c.Clone(context));
        var ifConditionsCopy = _conditionNodes.SelectToArray(c => c.Clone(context));
        var elseNodeCopy = _elseNode.Clone(context);
        return new IfElseExpressionNode(ifExpressionsCopy, ifConditionsCopy, elseNodeCopy, Interval, Type);
    }

    public FunnyType Type { get; }
    public Interval Interval { get; }
}