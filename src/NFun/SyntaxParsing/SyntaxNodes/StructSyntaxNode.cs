using System.Collections.Generic;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class StructSyntaxNode: ISyntaxNode
    {
        public StructSyntaxNode(List<EquationSyntaxNode> equations, Interval interval)
        {
            Equations = equations;
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
        private List<EquationSyntaxNode> Equations;
        public IEnumerable<ISyntaxNode> Children => Equations;
    }
}