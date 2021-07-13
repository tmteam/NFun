using System;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpretation.Nodes
{
    internal class FunArgumentExpressionNode : IExpressionNode
    {
        public static FunArgumentExpressionNode CreateWith(ISyntaxNode node)
        {
            switch (node)
            {
                case NamedIdSyntaxNode varNode:
                    return new FunArgumentExpressionNode(
                        name: varNode.Id,
                        type: node.OutputType,
                        interval: node.Interval);
                case TypedVarDefSyntaxNode typeVarNode:
                    return new FunArgumentExpressionNode(
                        name: typeVarNode.Id,
                        type: typeVarNode.FunnyType,
                        interval: node.Interval);
                default:
                    throw ErrorFactory.InvalidArgTypeDefinition(node);
            }
        }

        private FunArgumentExpressionNode(string name, FunnyType type, Interval interval)
        {
            Type = type;
            Interval = interval;
            Name = name;
        }

        public string Name { get; }
        public Interval Interval { get; }
        public FunnyType Type { get; }


        public object Calc() => throw new InvalidOperationException();

        public override string ToString() => $"{Name}: {Type}";
    }
}