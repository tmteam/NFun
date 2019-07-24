using System.ComponentModel;
using System.Linq;
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
        public override VisitorResult Visit(FunCallSyntaxNode node)
        {
            var result = DefaultVisit(node);
            if (result != VisitorResult.Continue)
                return result;
            
            //Get overload from Ti - algorithm
            var overload = _solving.GetFunctionOverload(node.OrderNumber, _solvedTypeConverter);
           /* if (overload == null)
            {
                overload = new FunFunctionSignature(node.OutputType, node.Args.Select(a=>a.OutputType).ToArray());
            }*/
            node.SignatureOfOverload = overload;
            return  result;
        }

        protected override VisitorResult DefaultVisit(ISyntaxNode node)
        {
            var type = _solving.GetNodeTypeOrEmpty(node.OrderNumber, _solvedTypeConverter);
            
            node.OutputType = type;
            
            return VisitorResult.Continue;
        }


        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            return VisitorResult.Continue;
        }

    }
}