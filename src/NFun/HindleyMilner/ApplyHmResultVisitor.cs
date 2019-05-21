using System.ComponentModel;
using System.Reflection;
using NFun.HindleyMilner;
using NFun.HindleyMilner.Tyso;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.HindleyMilner
{
   
    public class ApplyHmResultVisitor: EnterVisitorBase
    {
        private readonly FunTypeSolving _solving;
        private readonly SolvedTypeConverter _solvedTypeConverter;

        public ApplyHmResultVisitor(FunTypeSolving solving, SolvedTypeConverter solvedTypeConverter)
        {
            _solving = solving;
            _solvedTypeConverter = solvedTypeConverter;
        }

        protected override VisitorResult DefaultVisit(ISyntaxNode node)
        {
            var type = _solving.GetNodeTypeOrEmpty(node.NodeNumber, _solvedTypeConverter);
            
            node.OutputType = type;
            
            return VisitorResult.Continue;
        }


        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            return VisitorResult.Continue;
        }

    }
}