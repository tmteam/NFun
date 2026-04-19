using System.Linq;
using NFun.Interpretation.Functions;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Types;

namespace NFun.Interpretation;

/// <summary>
/// Detects whether a body can be solved without the full TIC constraint graph.
/// A "simple" body uses ONLY primitive types — no arrays, structs, optionals, lambdas.
/// Side effect: populates ResolvedSignature on operator/function nodes for reuse by SimplePrimitiveSolver.
/// </summary>
internal static class SimpleExpressionDetector {
    internal static bool IsSimpleBody(SyntaxTree tree, IFunctionRegistry functions) {
        foreach (var node in tree.Children) {
            switch (node) {
                case EquationSyntaxNode eq:
                    if (eq.TypeSpecificationOrNull != null
                        && !IsSimpleTypeSyntax(eq.TypeSpecificationOrNull.TypeSyntax))
                        return false;
                    if (!IsSimpleExpression(eq.Expression, functions))
                        return false;
                    break;
                case VarDefinitionSyntaxNode vd:
                    if (!IsSimpleTypeSyntax(vd.TypeSyntax))
                        return false;
                    break;
                case UserFunctionDefinitionSyntaxNode:
                    return false;
                case TypeDeclarationSyntaxNode:
                    return false;
                default:
                    continue;
            }
        }
        return true;
    }

    private static bool IsSimpleExpression(ISyntaxNode node, IFunctionRegistry functions) =>
        node switch {
            GenericIntSyntaxNode => true,
            IpAddressConstantSyntaxNode => true,
            ConstantSyntaxNode c => c.OutputType.BaseType switch {
                BaseFunnyType.Bool or BaseFunnyType.Real or BaseFunnyType.Char
                    or BaseFunnyType.Ip => true,
                _ when c.OutputType.IsNumeric() => true,
                _ => false
            },
            NamedIdSyntaxNode => true,
            BinOperatorSyntaxNode bin =>
                IsSimpleOperator(bin, functions)
                && IsSimpleExpression(bin.Left, functions)
                && IsSimpleExpression(bin.Right, functions),
            UnaryOperatorSyntaxNode un =>
                IsSimpleOperator(un, functions)
                && IsSimpleExpression(un.Operand, functions),
            FunCallSyntaxNode call =>
                IsSimpleFunction(call, functions)
                && call.Args.All(a => IsSimpleExpression(a, functions)),
            IfThenElseSyntaxNode ite =>
                ite.Ifs.All(c => IsSimpleExpression(c.Condition, functions)
                              && IsSimpleExpression(c.Expression, functions))
                && IsSimpleExpression(ite.ElseExpr, functions),
            ComparisonChainSyntaxNode cc =>
                cc.Operands.All(o => IsSimpleExpression(o, functions)),
            _ => false
        };

    /// <summary>Resolve + cache signature on BinOp node. Returns true if primitive-compatible.</summary>
    private static bool IsSimpleOperator(BinOperatorSyntaxNode bin, IFunctionRegistry functions) {
        var sig = functions.GetOrNull(bin.Id, 2);
        bin.ResolvedSignature = sig;
        if (sig is PureGenericFunctionBase) return true;
        return sig is IConcreteFunction && HasOnlyPrimitiveTypes(sig);
    }

    /// <summary>Resolve + cache signature on UnaryOp node.</summary>
    private static bool IsSimpleOperator(UnaryOperatorSyntaxNode un, IFunctionRegistry functions) {
        var sig = functions.GetOrNull(un.Id, 1);
        un.ResolvedSignature = sig;
        if (sig is PureGenericFunctionBase) return true;
        return sig is IConcreteFunction && HasOnlyPrimitiveTypes(sig);
    }

    /// <summary>Resolve + cache signature on FunCall node.</summary>
    private static bool IsSimpleFunction(FunCallSyntaxNode call, IFunctionRegistry functions) {
        // Note: IsOperator=true FunCallSyntaxNodes (e.g. range, get, slice from [1..5], x[i])
        // are NOT handled by IsSimpleOperator (which only handles BinOperatorSyntaxNode/UnaryOperatorSyntaxNode).
        // They must be resolved and checked here like any other function call.
        var sig = functions.GetOrNull(call.Id, call.Args.Length);
        call.ResolvedSignature = sig;
        if (sig is PureGenericFunctionBase) return true;
        return sig is IConcreteFunction && HasOnlyPrimitiveTypes(sig);
    }

    private static bool HasOnlyPrimitiveTypes(IFunctionSignature sig) {
        if (!IsPrimitiveBaseType(sig.ReturnType.BaseType)) return false;
        for (int i = 0; i < sig.ArgTypes.Length; i++)
            if (!IsPrimitiveBaseType(sig.ArgTypes[i].BaseType)) return false;
        return true;
    }

    private static bool IsPrimitiveBaseType(BaseFunnyType t) =>
        t is BaseFunnyType.Bool or BaseFunnyType.Char or BaseFunnyType.Ip or BaseFunnyType.Any
            or BaseFunnyType.Real or BaseFunnyType.Int16 or BaseFunnyType.Int32 or BaseFunnyType.Int64
            or BaseFunnyType.UInt8 or BaseFunnyType.UInt16 or BaseFunnyType.UInt32 or BaseFunnyType.UInt64;

    private static bool IsSimpleTypeSyntax(TypeSyntax syntax) =>
        syntax is TypeSyntax.EmptyType or TypeSyntax.Named;
}
