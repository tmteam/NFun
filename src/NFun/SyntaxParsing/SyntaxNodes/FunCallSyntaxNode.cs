using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class FunCallSyntaxNode: ISyntaxNode
    {
        public FunCallSyntaxNode(string value, ISyntaxNode[] args, Interval interval, bool isOperator = false)
        {
            Value = value;
            Args = args;
            Interval = interval;
            IsOperator = isOperator;
        }

        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Fun;
        public string Value { get; }
        public ISyntaxNode[] Args { get; }
        public Interval Interval { get; set; }
        public bool IsOperator { get; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        
        public IEnumerable<ISyntaxNode> Children => Args;

    }
}