using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class ProcArrayInit : ISyntaxNode
    {
        public VarType OutputType { get; set; }
        public int NodeNumber { get; set; }

        public ISyntaxNode From { get; }
        public ISyntaxNode To { get; }
        public ISyntaxNode Step { get; }

        public ProcArrayInit(ISyntaxNode from, ISyntaxNode to, ISyntaxNode step, Interval interval)
        {
            From = @from;
            To = to;
            Step = step;
            Interval = interval;
        }

        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.ProcArrayInit;
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IEnumerable<ISyntaxNode> Children
        {
            get
            {
                yield return From;
                yield return To;
                if (Step != null)
                    yield return Step;
            }
        }
    }
}