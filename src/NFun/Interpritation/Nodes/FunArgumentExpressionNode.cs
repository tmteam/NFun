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
                case VariableSyntaxNode varNode:
                    return new FunArgumentExpressionNode(
                        name: varNode.Id, 
                        type: node.OutputType, 
                        interval: node.Interval, 
                        isStrictlyType: false);
                case TypedVarDefSyntaxNode typeVarNode:
                    return new FunArgumentExpressionNode(
                        name: typeVarNode.Id, 
                        type: typeVarNode.VarType, 
                        interval: node.Interval, 
                        isStrictlyType: true);
                default:
                    throw ErrorFactory.InvalidArgTypeDefenition(node);
            }
        }
        public string Name { get; }
        public Interval Interval { get; }
        public bool IsStrictlyType { get; }
        
        private FunArgumentExpressionNode(string name, VarType type, Interval interval, bool isStrictlyType)
        {
            Type = type;
            Interval = interval;
            IsStrictlyType = isStrictlyType;
            Name = name;
        }
        
        public VarType Type { get; } 
        
        public object Calc() => throw new InvalidOperationException();
        
        public override string ToString() => $"{Name}: {Type}";
    }
}