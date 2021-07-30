using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class StructInitSyntaxNode: ISyntaxNode
    {
        public StructInitSyntaxNode(List<EquationSyntaxNode> equations, Interval interval)
        {
            var fields = new List<FieldDefinition>(equations.Count);
            foreach (var equation in equations)
            {
                if (equation.TypeSpecificationOrNull != null)
                    throw FunnyParseException.ErrorStubToDo("Field type specification is not supported yet");
                fields.Add(new FieldDefinition(equation.Id,equation.Expression));
            }

            Fields = fields;
            Interval = interval;
        }
        public FunnyType OutputType { get; set; }
        public int OrderNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);

        public readonly IReadOnlyList<FieldDefinition> Fields;
        public IEnumerable<ISyntaxNode> Children => Fields.Select(e=>e.Node);
    }

    public class FieldDefinition
    {
        public FieldDefinition(string name, ISyntaxNode node)
        {
            Name = name;
            Node = node;
        }

        public string Name { get; }
        public ISyntaxNode Node { get; }
    }
}