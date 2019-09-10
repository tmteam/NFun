using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;
using NFun.Types;

namespace NFun.TypeInference
{
    public class ApplyTiResultsExitVisitor: ExitVisitorBase{

        public override bool Visit(ArraySyntaxNode node)
        {
            if (!node.Children.Any()) return true;

            //Check equality of all array items
            var arrayType = node.Children.First().OutputType;
            foreach (var child in node.Children)
            {
                if (child.OutputType != arrayType)
                    throw ErrorFactory.VariousArrayElementTypes(child);
            }

            return true;
        }
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