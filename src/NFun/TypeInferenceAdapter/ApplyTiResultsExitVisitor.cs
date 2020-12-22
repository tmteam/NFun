using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.TypeInferenceAdapter
{
    public class ApplyTiResultsExitVisitor: ExitVisitorBase{

        public override bool Visit(IfThenElseSyntaxNode node)
        {
            if (node.OutputType != VarType.Anything) 
                return true;
            
            if (node.Ifs.Any(i => i.Expression.OutputType == VarType.Anything))
                return true;
            if (node.ElseExpr.OutputType == VarType.Anything)
                return true;
                
            throw ErrorFactory.VariousIfElementTypes(node);
        }
    }
}