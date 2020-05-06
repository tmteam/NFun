using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class GenericIntSyntaxNode : ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }

        public GenericIntSyntaxNode(object value, bool isHexOrBin, Interval interval)
        {
            Interval = interval;
            Value = value;
            IsHexOrBin = isHexOrBin;
        }
        public object Value { get; }
        public bool IsHexOrBin { get; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    }
}