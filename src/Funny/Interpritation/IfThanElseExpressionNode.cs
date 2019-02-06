using System.Collections.Generic;
using System.Linq;

namespace Funny.Interpritation
{
    public class IfThanElseExpressionNode: IExpressionNode
    {
        private readonly IfCaseExpressionNode[] _ifCaseNodes;
        private readonly IExpressionNode _elseNode;

        public IfThanElseExpressionNode(IfCaseExpressionNode[] ifCaseNodes, IExpressionNode elseNode)
        {
            _ifCaseNodes = ifCaseNodes;
            _elseNode = elseNode;
        }

        public IEnumerable<IExpressionNode> Children 
            => _ifCaseNodes.Append(_elseNode);
        public double Calc()
        {
            foreach (var ifCase in _ifCaseNodes)
            {
                if (ifCase.IsSatisfied())
                    return ifCase.Calc();
            }

            return _elseNode.Calc();
        }
    }
}