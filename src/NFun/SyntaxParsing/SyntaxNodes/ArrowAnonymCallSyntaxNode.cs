using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class ArrowAnonymCallSyntaxNode : ISyntaxNode
    {
        public ISyntaxNode[] ArgumentsDefenition { get; }    
        public ISyntaxNode Defenition { get; }
        public ISyntaxNode Body { get; }

        public ArrowAnonymCallSyntaxNode(ISyntaxNode defenition, ISyntaxNode body, Interval interval)
        {
            if (defenition is ListOfExpressionsSyntaxNode list)
                //it can be comlex: (x1,x2,x3)=>...
                ArgumentsDefenition = list.Expressions;
            else
                //or primitive: x1 => ...
                ArgumentsDefenition = new[] {defenition};
            
            Defenition = defenition;
            Body = body;
            Interval = interval;
        }

        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        public IEnumerable<ISyntaxNode> Children => ArgumentsDefenition.Append(Body);
    }
}