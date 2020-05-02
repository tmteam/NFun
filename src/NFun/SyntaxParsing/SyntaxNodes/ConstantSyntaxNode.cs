using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class ConstantSyntaxNode : ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public bool StrictType { get; }
        
        public ConstantSyntaxNode(object value, VarType varType, Interval interval, bool strictType)
        {
            OutputType = varType;
            Interval = interval;
            StrictType = strictType;
            Value = value;
        }

        public string ClrTypeName => Value?.GetType().Name;
        public bool IsInBrackets { get; set; }
        public object Value { get; }
        public Interval Interval { get; set; }
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];

        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    }
}