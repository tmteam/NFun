using NFun.Parsing;
using NFun.SyntaxParsing.Visitors;

namespace NFun.HindleyMilner
{
    public class ApplyHmResultVisitor: EnterVisitorBase
    {
        private readonly FunTypeSolving _solving;

        public ApplyHmResultVisitor(FunTypeSolving solving)
        {
            _solving = solving;
        }

        protected override VisitorResult DefaultVisit(ISyntaxNode node)
        {
            var type = _solving.GetNodeTypeOrEmpty(node.NodeNumber);
            node.OutputType = type;
            return VisitorResult.Continue;
        }

        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            return VisitorResult.Skip;
        }

    }
}