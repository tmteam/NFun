using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class VarDefinitionSyntaxNode : ISyntaxNode
    {
        public FunnyType OutputType { get; set; }
        public int OrderNumber { get; set; }

        public string Id { get; }
        public FunnyType FunnyType { get; }
        public VarAttribute[] Attributes { get; }

        public VarDefinitionSyntaxNode(TypedVarDefSyntaxNode node, VarAttribute[] attributes = null)
        {
            Id = node.Id;
            FunnyType = node.FunnyType;
            Attributes = attributes??new VarAttribute[0];
            Interval = node.Interval;
        }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public override string ToString() => Id + ":" + OutputType;
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];

    }
}