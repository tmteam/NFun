using System;
using System.Collections.Generic;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Types;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;
internal class FunArgumentDeclarationRuntimeNode : IRuntimeNode {
    public static FunArgumentDeclarationRuntimeNode CreateWith(ISyntaxNode node, FunnyType? ticResolvedType = null) =>
        node switch {
            NamedIdSyntaxNode varNode => new FunArgumentDeclarationRuntimeNode(
                name: varNode.Id, type:
                node.OutputType,
                interval: node.Interval),
            // Prefer the TIC-solved type when available — it resolves user-defined named types
            // and aliases via the customTypes registry (which the TypeSyntaxResolver fallback
            // here doesn't have). Fallback chain: TIC-solved → node.OutputType → raw resolver.
            TypedVarDefSyntaxNode typeVarNode => new FunArgumentDeclarationRuntimeNode(
                name: typeVarNode.Id,
                type: ticResolvedType ??
                    (typeVarNode.OutputType.BaseType != BaseFunnyType.Empty
                        ? typeVarNode.OutputType
                        : TypeInferenceAdapter.TypeSyntaxResolver.Resolve(typeVarNode.TypeSyntax)),
                interval: node.Interval),
            _ => throw Errors.InvalidArgTypeDefinition(node)
        };

    private FunArgumentDeclarationRuntimeNode(string name, FunnyType type, Interval interval) {
        Type = type;
        Interval = interval;
        Name = name;
    }

    public string Name { get; }
    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IRuntimeNode> Children => Array.Empty<IExpressionNode>();

    public IRuntimeNode Clone(ICloneContext context) => this;
    public override string ToString() => $"{Name}: {Type}";
}
