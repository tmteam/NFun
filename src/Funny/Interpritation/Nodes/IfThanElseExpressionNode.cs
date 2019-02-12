using System.Collections.Generic;
using System.Linq;
using Funny.Runtime;

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
        public object Calc()
        {
            foreach (var ifCase in _ifCaseNodes)
            {
                if (ifCase.IsSatisfied())
                    return ifCase.Calc();
            }

            return _elseNode.Calc();
        }
        public VarType Type => VarType.NumberType;

    }
}