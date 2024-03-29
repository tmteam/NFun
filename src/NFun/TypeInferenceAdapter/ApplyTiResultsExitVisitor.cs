using System.Linq;
using NFun.ParseErrors;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;

namespace NFun.TypeInferenceAdapter; 

public class ApplyTiResultsExitVisitor : ExitVisitorBase {
    public override bool Visit(IfThenElseSyntaxNode node) {
        //If upcast is denied:
        if (node.OutputType == FunnyType.Any)
            return true;
        if (node.Ifs.Any(i => i.Expression.OutputType != node.OutputType) ||
            node.ElseExpr.OutputType != node.OutputType)
            throw Errors.VariousIfElementTypes(node);
        return true;
    }

    public override bool Visit(ArraySyntaxNode node) {
        // if upcast is denied
        // This code is active because composite upcast works not very well...
        var elementType = node.OutputType.ArrayTypeSpecification.FunnyType;
        if (elementType == FunnyType.Any)
            return true;
        if (node.Expressions.Count == 0)
            return true;
        var firstElementType = node.Expressions[0].OutputType;
        if (node.Children.All(i => i.OutputType == firstElementType))
            return true;

        throw Errors.VariousArrayElementTypes(node);
    }
}