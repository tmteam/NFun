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
        public override VisitorEnterResult Visit(IfThenElseSyntaxNode node){
            var result = DefaultVisit(node);
            return result;
        }
        public override VisitorEnterResult Visit(FunCallSyntaxNode node)
        {
            var result = DefaultVisit(node);
            if (result != VisitorEnterResult.Continue)
                return result;
            
            //Get overload from Ti - algorithm
            var overload = _solving.GetFunctionOverload(node.OrderNumber, _tiToLangTypeConverter);
            node.SignatureOfOverload = overload;
            return  result;
        }

        protected override VisitorEnterResult DefaultVisit(ISyntaxNode node)
        {
            var type = _solving.GetNodeTypeOrEmpty(node.OrderNumber, _tiToLangTypeConverter);
            
            node.OutputType = type;
            
            return VisitorEnterResult.Continue;
        }


        public override VisitorEnterResult Visit(UserFunctionDefenitionSyntaxNode node)
        {
            return VisitorEnterResult.Continue;
        }

    }
}