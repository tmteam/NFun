using System.Collections.Generic;
using System.Linq;
using NFun.Exceptions;
using NFun.ParseErrors;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes {

public class StructInitSyntaxNode : ISyntaxNode {
    public FunnyType OutputType { get; set; }
    public int OrderNumber { get; set; }
    public bool IsInBrackets { get; set; }
    public Interval Interval { get; set; }
    public T Accept<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
    public readonly IReadOnlyList<FieldDefinition> Fields;
    public IEnumerable<ISyntaxNode> Children => Fields.Select(e => e.Node);

    public StructInitSyntaxNode(List<EquationSyntaxNode> equations, Interval interval) {
        var fields = new List<FieldDefinition>(equations.Count);
        foreach (var equation in equations)
        {
            if (equation.TypeSpecificationOrNull != null)
                throw Errors.StructFieldSpecificationIsNotSupportedYet(equation.TypeSpecificationOrNull.Interval);
            fields.Add(new FieldDefinition(equation.Id, equation.Expression));
        }

        Fields = fields;
        Interval = interval;
    }
}

public class FieldDefinition {
    public string Name { get; }
    public ISyntaxNode Node { get; }

    public FieldDefinition(string name, ISyntaxNode node) {
        Name = name;
        Node = node;
    }
}

}