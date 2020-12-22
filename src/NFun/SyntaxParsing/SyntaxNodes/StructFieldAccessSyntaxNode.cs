using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class StructFieldAccessSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode Child { get; }
        public string FieldName { get; }

        public StructFieldAccessSyntaxNode(ISyntaxNode child, string fieldName, Interval interval)
        {
            Child = child;
            FieldName = fieldName;
            Interval = interval;
        }

        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ISyntaxNode> Children  => new ISyntaxNode[]{Child};
    }
}