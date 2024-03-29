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
        node.OutputType = _tiToLangTypeConverter.Convert(type);
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


    public override DfsEnterResult Visit(UserFunctionDefinitionSyntaxNode node)
        => DfsEnterResult.Continue;
}
