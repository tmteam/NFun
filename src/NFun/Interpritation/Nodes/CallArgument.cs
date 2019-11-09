using System;
using NFun.ParseErrors;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.SyntaxNodes;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Interpritation.Nodes
{
    public class CallArgument 
    {
        public static  CallArgument CreateWith(ISyntaxNode node)
        {
            switch (node)
            {
                case VariableSyntaxNode varNode:
                    return new CallArgument(
                        name:     varNode.Id, 
                        type:     node.OutputType, 
                        interval: node.Interval);
                case TypedVarDefSyntaxNode typeVarNode:
                    return new CallArgument(
                        name:     typeVarNode.Id, 
                        type:     typeVarNode.VarType, 
                        interval: node.Interval);
                default:
                    throw ErrorFactory.InvalidArgTypeDefenition(node);
            }
        }

        public CallArgument(string name, VarType type, Interval interval)
        {
            Type = type;
            Interval = interval;
            Name = name;
        }

        public string Name { get; }
       
        public Interval Interval { get; }
        public VarType Type { get; } 
        
        
        public override string ToString() => $"{Name}: {Type}";
    }
}