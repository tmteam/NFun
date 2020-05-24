using System;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class FunArgumentExpressionNode : IExpressionNode
    {
        public static  FunArgumentExpressionNode CreateWith(ISyntaxNode node)
        {
            switch (node)
            {
                case NamedIdSyntaxNode varNode:
                    return new FunArgumentExpressionNode(
                        name:     varNode.Id, 
                        type:     node.OutputType, 
                        interval: node.Interval);
                case TypedVarDefSyntaxNode typeVarNode:
                    return new FunArgumentExpressionNode(
                        name:     typeVarNode.Id, 
                        type:     typeVarNode.VarType, 
                        interval: node.Interval);
                default:
                    throw ErrorFactory.InvalidArgTypeDefenition(node);
            }
        }

        private FunArgumentExpressionNode(string name, VarType type, Interval interval)
        {
            Type = type;
            Interval = interval;
            Name = name;
        }

        public string Name { get; }
        public Interval Interval { get; }
        public VarType Type { get; } 
        
        
        public object Calc() => throw new InvalidOperationException();
        
        public override string ToString() => $"{Name}: {Type}";
    }
}