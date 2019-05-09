using System.Collections.Generic;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Parsing
{
    public class TypedVarDefSyntaxNode : ISyntaxNode
    {
        public string Id { get; }
        public VarType VarType { get; }

        public TypedVarDefSyntaxNode(string id, VarType varType, Interval interval)
        {
            Id = id;
            VarType = varType;
            Interval = interval;
        }
        public bool IsInBrackets { get; set; }
        public SyntaxNodeType Type => SyntaxNodeType.Var;
        public Interval Interval { get; set; }
        public T Visit<T>(ISyntaxNodeVisitor<T> visitor) => visitor.Visit(this);
        
        public IEnumerable<ISyntaxNode> Children => new ISyntaxNode[0];

    }
}