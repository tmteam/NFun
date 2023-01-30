using System;
using System.Collections.Generic;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes;
internal class FunArgumentDeclarationRuntimeNode : IRuntimeNode {
    public static FunArgumentDeclarationRuntimeNode CreateWith(ISyntaxNode node) =>
        node switch {
            NamedIdSyntaxNode varNode => new FunArgumentDeclarationRuntimeNode(
                name: varNode.Id, type:
                node.OutputType,
                interval: node.Interval),
            TypedVarDefSyntaxNode typeVarNode => new FunArgumentDeclarationRuntimeNode(
                name: typeVarNode.Id,
                type: typeVarNode.FunnyType,
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
