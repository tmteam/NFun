using NFun.SyntaxParsing.SyntaxNodes;

namespace NFun.SyntaxParsing.Visitors;

public class SetNodeNumberVisitor : EnterVisitorBase {
    private int _lastNum;
    public int LastUsedNumber => _lastNum;

    /// <summary>
    /// True if all visited nodes are primitive-compatible (no arrays, structs, optionals, lambdas).
    /// Set during the numbering pass — zero additional cost.
    /// Used by SimplePrimitiveSolver to skip gate check.
    /// </summary>
    public bool IsSimpleBody { get; private set; } = true;

    public SetNodeNumberVisitor(int startNum) => _lastNum = startNum;

    protected override DfsEnterResult DefaultVisitEnter(ISyntaxNode node) {
        node.OrderNumber = _lastNum++;
        if (IsSimpleBody) CheckSimple(node);
        return DfsEnterResult.Continue;
    }

    private void CheckSimple(ISyntaxNode node) {
        switch (node) {
            case GenericIntSyntaxNode:
            case IpAddressConstantSyntaxNode:
            case NamedIdSyntaxNode:
            case BinOperatorSyntaxNode:
            case UnaryOperatorSyntaxNode:
            case FunCallSyntaxNode:
            case IfThenElseSyntaxNode:
            case IfCaseSyntaxNode:
            case ComparisonChainSyntaxNode:
            case EquationSyntaxNode:
            case VarDefinitionSyntaxNode:
            case SyntaxTree:
                break; // primitive-compatible
            case ConstantSyntaxNode c:
                if (!c.OutputType.IsNumeric()
                    && c.OutputType.BaseType is not (Types.BaseFunnyType.Bool
                        or Types.BaseFunnyType.Real or Types.BaseFunnyType.Char
                        or Types.BaseFunnyType.Ip))
                    IsSimpleBody = false;
                break;
            default:
                // ArraySyntaxNode, StructInitSyntaxNode, AnonymFunctionSyntaxNode,
                // SuperAnonymFunctionSyntaxNode, StructFieldAccessSyntaxNode,
                // DefaultValueSyntaxNode, TypeDeclarationSyntaxNode, etc.
                IsSimpleBody = false;
                break;
        }
    }
}
