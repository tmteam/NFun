using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class TypedVarDefSyntaxNode : ISyntaxNode
    {
        public int OrderNumber { get; set; }
        public FunnyType OutputType { get; set; }

        public string Id { get; }
        public FunnyType FunnyType { get; }

        public TypedVarDefSyntaxNode(string id, FunnyType funnyType, Interval interval)
        {
            Id = id;
            FunnyType = funnyType;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];

    }
}