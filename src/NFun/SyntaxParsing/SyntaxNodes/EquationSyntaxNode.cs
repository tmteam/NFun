using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class EquationSyntaxNode : ISyntaxNode
    {
        
        public string Id { get; }
        public ISyntaxNode Expression { get; }
        public VarAttribute[] Attributes { get; }

        public EquationSyntaxNode(string id, int start, ISyntaxNode expression, VarAttribute[] attributes)
        {
            Id = id;
            Expression = expression;
            Attributes = attributes;
            IsInBrackets = false;
            Interval = Interval.New(start, expression.Interval.Finish);
        }

        public VarType OutputType { get; set; }
        public int NodeNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Equation;
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        
        public IEnumerable<ISyntaxNode> Children => new[]{Expression};

    }
}