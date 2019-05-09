using System.Collections.Generic;
using NFun.Tokenization;

namespace NFun.Parsing
{
    public class AnonymCallSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode Defenition { get; }
        public ISyntaxNode Body { get; }

        public AnonymCallSyntaxNode(ISyntaxNode defenition, ISyntaxNode body, Interval interval)
        {
            Defenition = defenition;
            Body = body;
            Interval = interval;
        }

        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.AnonymFun;
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => new[] {Defenition, Body};
    }
}