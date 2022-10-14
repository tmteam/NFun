using System;
using System.Collections.Generic;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;

namespace NFun.Interpretation.Nodes; 
//todo - it is not an expression node
internal class FunArgumentExpressionNode : IExpressionNode {
    public static FunArgumentExpressionNode CreateWith(ISyntaxNode node) =>
        node switch {
            NamedIdSyntaxNode varNode => new FunArgumentExpressionNode(
                name: varNode.Id, type: 
                node.OutputType,
                interval: node.Interval),
            TypedVarDefSyntaxNode typeVarNode => new FunArgumentExpressionNode(
                name: typeVarNode.Id,
                type: typeVarNode.FunnyType, 
                interval: node.Interval),
            _ => throw Errors.InvalidArgTypeDefinition(node)
        };

    private FunArgumentExpressionNode(string name, FunnyType type, Interval interval) {
        Type = type;
        Interval = interval;
        Name = name;
    }

    public string Name { get; }
    public Interval Interval { get; }
    public FunnyType Type { get; }
    public IEnumerable<IExpressionNode> Children => Array.Empty<IExpressionNode>();

    public object Calc() => throw new InvalidOperationException();
    public IExpressionNode Clone(ICloneContext context) => this;
    public override string ToString() => $"{Name}: {Type}";
}