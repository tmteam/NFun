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
            //Old code for composite upcast support:
            /*
            if (node.OutputType != VarType.Anything) 
                return true;
            
            if (node.Ifs.Any(i => i.Expression.OutputType == VarType.Anything))
                return true;
            if (node.ElseExpr.OutputType == VarType.Anything)
                return true;
                
            throw ErrorFactory.VariousIfElementTypes(node);
            */
            
            //If upcast is denied:
            if (node.OutputType == VarType.Anything) 
                return true;
            if (node.Ifs.Any(i => i.Expression.OutputType != node.OutputType)|| node.ElseExpr.OutputType!= node.OutputType)
                throw ErrorFactory.VariousIfElementTypes(node);
            return true;
        }
        
        public override bool Visit(ArraySyntaxNode node)
        {
            // if upcast is denied
            // This code is active because composite upcast works not very well...
            var elementType = node.OutputType.ArrayTypeSpecification.VarType;
            if (elementType == VarType.Anything) 
                return true;

            if (node.Children.All(i => i.OutputType == elementType))
                return true;
            
            throw ErrorFactory.VariousArrayElementTypes(node);
        }
    }
}