using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.TypeInference.Solving;

namespace NFun.TypeInference
{
    public class ApplyTiResultEnterVisitor: EnterVisitorBase
    {
        private readonly TiResult _solving;
        private readonly TiToLangTypeConverter _tiToLangTypeConverter;

        public ApplyTiResultEnterVisitor(TiResult solving, TiToLangTypeConverter tiToLangTypeConverter)
        {
            _solving = solving;
            _tiToLangTypeConverter = tiToLangTypeConverter;
        }
        public override VisitorResult Visit(IfThenElseSyntaxNode node){
            var result = DefaultVisit(node);
            return result;
        }
        public override VisitorResult Visit(FunCallSyntaxNode node)
        {
            var result = DefaultVisit(node);
            if (result != VisitorResult.Continue)
                return result;
            
            //Get overload from Ti - algorithm
            var overload = _solving.GetFunctionOverload(node.OrderNumber, _tiToLangTypeConverter);
            node.SignatureOfOverload = overload;
            return  result;
        }

        protected override VisitorResult DefaultVisit(ISyntaxNode node)
        {
            var type = _solving.GetNodeTypeOrEmpty(node.OrderNumber, _tiToLangTypeConverter);
            
            node.OutputType = type;
            
            return VisitorResult.Continue;
        }


        public override VisitorResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            return VisitorResult.Continue;
        }

    }
}