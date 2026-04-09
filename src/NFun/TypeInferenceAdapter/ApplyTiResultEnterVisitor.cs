using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.SyntaxParsing.Visitors;

namespace NFun.TypeInferenceAdapter;

public class ApplyTiResultEnterVisitor : EnterVisitorBase {
    private readonly TypeInferenceResults _solving;
    private readonly TicTypesConverter _tiToLangTypeConverter;

    public ApplyTiResultEnterVisitor(TypeInferenceResults solving, TicTypesConverter tiToLangTypeConverter) {
        _solving = solving;
        _tiToLangTypeConverter = tiToLangTypeConverter;
    }

    public override DfsEnterResult Visit(EquationSyntaxNode node) {
        var type = _solving.GetVariableType(node.Id);
        var funnyType = _tiToLangTypeConverter.Convert(type);

        // WORKAROUND: ?? and ! must return non-Optional, but TIC resolves T as opt(T) when
        // the Optional element comes from a frozen type annotation (named struct field).
        // Root cause: TIC shares a single TicNode for T in both opt(T) and the return type.
        // IsOptional from opt(T) context leaks to the return.
        if (funnyType.BaseType == Types.BaseFunnyType.Optional
            && node.Expression is SyntaxParsing.SyntaxNodes.FunCallSyntaxNode call
            && call.Id is NFun.Functions.CoreFunNames.NullCoalesce or NFun.Functions.CoreFunNames.ForceUnwrap)
            funnyType = funnyType.OptionalTypeSpecification.ElementType;

        node.OutputType = funnyType;
        return DfsEnterResult.Continue;
    }

    public override DfsEnterResult Visit(NamedIdSyntaxNode node) {
        //TODO it is just workaround. We have to manually setup variable type into VariableSource
        var type = _solving.GetVariableTypeOrNull(node.Id);
        if (type != null)
            node.VariableType = _tiToLangTypeConverter.Convert(type);
        return DefaultVisitEnter(node);
    }
    
    protected override DfsEnterResult DefaultVisitEnter(ISyntaxNode node) {
        var type = _solving.GetSyntaxNodeTypeOrNull(node.OrderNumber);
        node.OutputType = type == null
            ? FunnyType.Empty
            : _tiToLangTypeConverter.Convert(type);

        return DfsEnterResult.Continue;
    }


    public override DfsEnterResult Visit(FunCallSyntaxNode node) {
        var result = DefaultVisitEnter(node);

        // WORKAROUND: Same as Visit(EquationSyntaxNode) — unwrap Optional on ??/! results
        if (node.Id is NFun.Functions.CoreFunNames.NullCoalesce or NFun.Functions.CoreFunNames.ForceUnwrap
            && node.OutputType.BaseType == Types.BaseFunnyType.Optional)
            node.OutputType = node.OutputType.OptionalTypeSpecification.ElementType;

        var resolvedArgs = _solving.GetResolvedCallArgsOrNull(node.OrderNumber);
        if (resolvedArgs != null)
        {
            foreach (var arg in resolvedArgs)
            {
                // Synthetic nodes (params arrays) are not in the tree — apply type manually
                if (arg.OrderNumber >= TicSetupVisitor.SyntheticIdStart)
                {
                    var type = _solving.GetSyntaxNodeTypeOrNull(arg.OrderNumber);
                    arg.OutputType = type == null ? FunnyType.Empty : _tiToLangTypeConverter.Convert(type);
                }
            }
        }
        return result;
    }

    public override DfsEnterResult Visit(UserFunctionDefinitionSyntaxNode node)
        => DfsEnterResult.Continue;
}
