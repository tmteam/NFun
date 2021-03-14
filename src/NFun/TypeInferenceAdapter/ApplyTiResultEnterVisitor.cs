using System;
using NFun.Exceptions;
using NFun.ParseErrors;
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

        public override VisitorEnterResult Visit(GenericIntSyntaxNode node)
        {
            var type = _solving.GetSyntaxNodeTypeOrNull(node.OrderNumber);
            if(type==null)
                node.OutputType = VarType.Empty;
            else
                node.OutputType = _tiToLangTypeConverter.Convert(type);
            
            return VisitorEnterResult.Continue;
        }

        public override VisitorEnterResult Visit(NamedIdSyntaxNode node)
        {
            return base.Visit(node);
        }

        public override VisitorEnterResult Visit(EquationSyntaxNode node)
        {
            var type = _solving.GetVariableType(node.Id);
            node.OutputType = _tiToLangTypeConverter.Convert(type);
            return VisitorEnterResult.Continue;
        }

        protected override VisitorEnterResult DefaultVisitEnter(ISyntaxNode node)
        {
            var type = _solving.GetSyntaxNodeTypeOrNull(node.OrderNumber);
            if(type==null)
                node.OutputType = VarType.Empty;
            else
                node.OutputType = _tiToLangTypeConverter.Convert(type);
            
            return VisitorEnterResult.Continue;
        }


        public override VisitorEnterResult Visit(UserFunctionDefinitionSyntaxNode node) 
            => VisitorEnterResult.Continue;
    }
}