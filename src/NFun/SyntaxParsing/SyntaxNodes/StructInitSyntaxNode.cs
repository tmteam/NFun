using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tic.SolvingStates;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class StructInitSyntaxNode: ISyntaxNode
    {
        public StructInitSyntaxNode(List<EquationSyntaxNode> equations, Interval interval)
        {
            Fields = equations.Select(e=>new FieldDefenition(e.Id,e.Expression)).ToList();
            Interval = interval;
        }
        public VarType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public IReadOnlyList<FieldDefenition> Fields;
        public IEnumerable<ISyntaxNode> Children => Fields.Select(e=>e.Node);
    }

    public class FieldDefenition
    {
        public FieldDefenition(string name, ISyntaxNode node)
        {
            Name = name;
            Node = node;
        }

        public string Name { get; }
        public ISyntaxNode Node { get; }
    }
}