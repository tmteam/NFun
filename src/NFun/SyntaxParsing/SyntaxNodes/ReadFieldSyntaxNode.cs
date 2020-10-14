using System.Collections.Generic;
using System.Linq;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.SyntaxParsing.SyntaxNodes
{
    public class ReadFieldSyntaxNode : ISyntaxNode
    {
        public ReadFieldSyntaxNode(string fieldName)
        {
            FieldName = fieldName;
        }

        public VarType OutputType { get; set; }
        
        public string FieldName { get; }
        public int OrderNumber { get; set; }
        public bool IsInBrackets { get; set; }
        public Interval Interval { get; set; }
        public T Accept<T>(ISyntaxNodeVisitor<T> visitor)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ISyntaxNode> Children => Enumerable.Empty<ISyntaxNode>();
    }
}