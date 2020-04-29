using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public class ApplyTiResultEnterVisitor: EnterVisitorBase
    {
        private readonly FinalizationResults _solving;
        private readonly ITypeInferenceResultsInterpriter _tiToLangTypeConverter;

        public ApplyTiResultEnterVisitor(FinalizationResults solving, ITypeInferenceResultsInterpriter tiToLangTypeConverter)
        {
            _solving = solving;
            _tiToLangTypeConverter = tiToLangTypeConverter;
        }

        public override VisitorEnterResult Visit(EquationSyntaxNode node)
        {
            var type = _solving.GetVariableNode(node.Id)?.State;
            if (type == null)
                node.OutputType = VarType.Empty;
            else
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
            var type = _solving.GetSyntaxNodeOrNull(node.OrderNumber)?.State;
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