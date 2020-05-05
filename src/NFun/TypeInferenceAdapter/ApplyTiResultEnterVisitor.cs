using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public class ApplyTiResultEnterVisitor: EnterVisitorBase
    {
        private readonly TypeInferenceResults _solving;
        private readonly TicTypesConverter _tiToLangTypeConverter;

        public ApplyTiResultEnterVisitor(TypeInferenceResults solving, TicTypesConverter tiToLangTypeConverter)
        {
            _solving = solving;
            _tiToLangTypeConverter = tiToLangTypeConverter;
        }

        public override VisitorEnterResult Visit(EquationSyntaxNode node)
        {
            var type = _solving.GetVariableType(node.Id);
            node.OutputType = _tiToLangTypeConverter.Convert(type);
            return VisitorEnterResult.Continue;
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
            //var overload = _solving.GetFunctionOverload(node.OrderNumber, _tiToLangTypeConverter);
            //node.SignatureOfOverload = overload;
            return  result;
        }

        protected override VisitorEnterResult DefaultVisit(ISyntaxNode node)
        {
            var type = _solving.GetSyntaxNodeTypeOrNull(node.OrderNumber);
            if(type==null)
                node.OutputType = VarType.Empty;
            else
                node.OutputType = _tiToLangTypeConverter.Convert(type);
            
            return VisitorEnterResult.Continue;
        }


        public override VisitorEnterResult Visit(UserFunctionDefenitionSyntaxNode node) 
            => VisitorEnterResult.Continue;
    }
}