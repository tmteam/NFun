using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class NumberSyntaxNode : ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int NodeNumber { get; set; }

        public NumberSyntaxNode(string value, Interval interval)
        {
            Value = value;
            Interval = interval;
        }

        public NumberSyntaxNode(object value, VarType varType, Interval interval)
        {
            OutputType = varType;
            Interval = interval;
            Value = value;
        }

        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Number;
        public object Value { get; }
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];
    }
}