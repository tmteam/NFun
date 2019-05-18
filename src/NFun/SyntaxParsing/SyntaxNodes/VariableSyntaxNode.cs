using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class VariableSyntaxNode : ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int NodeNumber { get; set; }

        public VariableSyntaxNode(string id, Interval interval)
        {
            Id = id;
            Interval = interval;
        }

        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Var;
        public string Id { get; }
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);    
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];

        public override string ToString() => $"({NodeNumber}) {Id}:{OutputType}";
    }
}