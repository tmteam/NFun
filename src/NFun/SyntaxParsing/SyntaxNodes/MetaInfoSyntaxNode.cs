using System;
using System.Collections.Generic;
using System.Text;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class MetaInfoSyntaxNode: ISyntaxNode
    {
        public NamedIdSyntaxNode NamedIdSyntaxNode { get; }

        public MetaInfoSyntaxNode(NamedIdSyntaxNode namedIdSyntaxNode)
        {
            NamedIdSyntaxNode = namedIdSyntaxNode;
            Interval = NamedIdSyntaxNode.Interval;
            OrderNumber = NamedIdSyntaxNode.OrderNumber;
            IsInBrackets = false;
        }
        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IEnumerable<ISyntaxNode> Children => new[] {NamedIdSyntaxNode};
    }
}
